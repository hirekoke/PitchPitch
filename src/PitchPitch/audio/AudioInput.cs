using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

using CoreAudioApi;

namespace PitchPitch.audio
{
    class AudioInput : IDisposable
    {
        const int MSIN100NS = 10000;

        // CoreAudioApi
        private AudioClient _audioClient;
        private AudioCaptureClient _capClient;
        private MMDeviceEnumerator _devices = null;
        private MMDevice _capDevice;
        private string _capDeviceId;
        private WAVEFORMATEXTENSIBLE _capFormat;

        public MMDevice CapDevice { get { return _capDevice; } }
        public WAVEFORMATEXTENSIBLE CapFormat { get { return _capFormat; } }

        // 解析器
        private PitchAnalyzer _pitchAnalyzer = new PitchAnalyzer();
        private ToneAnalyzer _toneAnalyzer = new ToneAnalyzer();
        public ToneAnalyzer ToneAnalyzer { get { return _toneAnalyzer; } }

        // スレッド制御用
        private object _lockObj = new object();
        private Thread _thread = null;
        private TimeSpan _sleepTime = new TimeSpan(10 * MSIN100NS);

        // 制御命令キュー
        private Queue<Operation> _opQueue = new Queue<Operation>();
        private bool _capturing = false;
        public bool Capturing
        {
            get { return _capturing; }
        }

        // データ制御用
        private long _prevResetTime = 0;
        private bool _reset = false;

        // データ格納バッファ
        private long _prevAnalyzeTime = 0;
        private List<double> _buffer = new List<double>();
        private List<List<double>> _channelBuffer = new List<List<double>>();

        // 定数
        private const int MAXBUFLEN = 1024; // データ解析する最大長
        private const int RESETLIMIT = 100; // AudioClientをリセットするまでの無音時間のリミット
        private const int ANALYZESPAN = 30; // 解析をする間隔

        // デバイス情報
        private List<DeviceInfo> _deviceInfos = new List<DeviceInfo>();
        public List<DeviceInfo> DeviceInfos
        {
            get
            {
                lock (_lockObj)
                {
                    return new List<DeviceInfo>(_deviceInfos);
                }
            }
        }

        // event
        public event InputDisposedEventHandler Disposed;
        public event DeviceInfoUpdatedEventHandler DeviceInfoUpdated;
        public event DeviceSelectedEventHandler DeviceSelected;
        public event CaptureStartedEventHandler CaptureStarted;
        public event CaptureStoppedEventHandler CaptureStopped;
        public event DataUpdatedEventHandler DataUpdated;

        public AudioInput()
        {
            _devices = new MMDeviceEnumerator();
            _devices.OnDeviceAdded += (s, e) => { UpdateDeviceInfo(); };
            _devices.OnDeviceRemoved += (s, e) => { UpdateDeviceInfo(true); };
            _devices.OnDeviceStateChanged += (s, e) => { UpdateDeviceInfo(); };
            _devices.OnPropertyValueChanged += (s, e) => { UpdateDeviceInfo(); };
            _devices.OnDefaultDeviceChanged += (s, e) => { UpdateDeviceInfo(); };

            _thread = new Thread(new ThreadStart(mainThread));
            _thread.Start();
        }

        public void Dispose()
        {
            lock (_opQueue)
            {
                _opQueue.Enqueue(new Operation(Operation.OprationType.Dispose));
            }
        }


        public void UpdateDeviceInfo(bool needStop = false)
        {
            lock (_opQueue)
            {
                _opQueue.Enqueue(new Operation(Operation.OprationType.UpdateDevices, needStop));
            }
        }
        public void SelectDevice(string devId)
        {
            lock (_opQueue)
            {
                _opQueue.Enqueue(new Operation(Operation.OprationType.SelectDevice, devId));
            }
        }
        public void StopCapture()
        {
            lock (_opQueue)
            {
                _opQueue.Enqueue(new Operation(Operation.OprationType.StopCapture));
            }
        }
        public void StartCapture()
        {
            lock (_opQueue)
            {
                _opQueue.Enqueue(new Operation(Operation.OprationType.StartCapture));
            }
        }

        #region 内部実装
        private void disposeImpl()
        {
            if (_devices != null) _devices.Dispose();

            // イベント発火
            InputDisposedEventHandler del = Disposed;
            if (del != null)
            {
                del.Invoke(this, new InputDisposedEventArgs());
            }
        }

        private void releaseDevice()
        {
            if (_capDevice != null) _capDevice.Dispose();
            if (_capClient != null) _capClient.Dispose();
            if (_audioClient != null) _audioClient.Dispose();
        }

        private void updateDeviceInfoImpl(bool needStop)
        {
            if (needStop)
            {
                stopCaptureImpl();
            }
            lock(_lockObj) {
                _deviceInfos.Clear();
                if (_devices != null)
                {
                    foreach (MMDevice device in _devices.EnumerateAudioEndPoints(EDataFlow.eAll, EDeviceState.DEVICE_STATE_ACTIVE))
                    {
                        AudioEndpointVolume vol = device.AudioEndpointVolume;
                        DeviceInfo info = new DeviceInfo(device.Id, device.FriendlyName, device.DataFlow, device.State);
                        _deviceInfos.Add(info);
                    }
                }
            }

            // イベント発火
            DeviceInfoUpdatedEventHandler del = DeviceInfoUpdated;
            if (del != null)
            {
                List<DeviceInfo> info = this.DeviceInfos;
                del.Invoke(this, new DeviceInfoUpdatedEventArgs(info));
            }
        }

        private void selectDeviceImpl(string devId)
        {
            releaseDevice();

            _capDevice = _devices.GetDevice(devId.Trim());
            int idx = _deviceInfos.FindIndex((di) => { return di.DeviceId == devId; });
            if (_capDevice == null)
            {
#warning 例外
            }
            _capDeviceId = _capDevice.Id;

            // デバイスに適した初期化方法を決定
            AudioClientStreamFlags streamFlags = AudioClientStreamFlags.NoPersist;
            if (_capDevice.DataFlow == EDataFlow.eRender)
                streamFlags = AudioClientStreamFlags.Loopback |
                    AudioClientStreamFlags.EventCallback; // 実際は発生してくれない
          
            // フォーマット
            if(_audioClient != null) _capDevice.ReleaseAudioClient();

            try
            {
                _audioClient = _capDevice.AudioClient;
                _capFormat = _audioClient.MixFormat;
                _pitchAnalyzer.SampleFrequency = (double)(_capFormat.nSamplesPerSec);

                // 初期化
                _audioClient.Initialize(AudioClientShareMode.Shared,
                    streamFlags, 300 /*ms*/ * 10000, 0, _capFormat, Guid.Empty);
                _capClient = _audioClient.AudioCaptureClient;

                // イベント発火
                DeviceSelectedEventHandler del = DeviceSelected;
                if (del != null)
                {
                    del.Invoke(this, new DeviceSelectedEventArgs(_capDevice, idx));
                }
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
#warning エラー処理
                _audioClient = null;
                _capClient = null;
            }
        }

        private void stopCaptureImpl()
        {

            try
            {
                if (_audioClient != null && _capClient != null)
                {
                    _audioClient.Stop();
                }

                // イベント発火
                CaptureStoppedEventHandler del1 = CaptureStopped;
                if (del1 != null)
                {
                    del1.Invoke(this, new CaptureStoppedEventArgs());
                }
                DataUpdatedEventHandler del2 = DataUpdated;
                if (del2 != null)
                {
                    del2.Invoke(this, new DataUpdatedEventArgs(new PitchResult(0, 0), ToneResult.Default));
                }
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
#warning エラー処理
                throw new Exception(ex.Message);
            }
        }

        private void startCaptureImpl()
        {
            stopCaptureImpl();

            if (_audioClient == null || _capClient == null)
            {
                return;
            }

            long defaultDp; long minimumDp;

            try
            {
                _audioClient.GetDevicePeriod(out defaultDp, out minimumDp);
                _sleepTime = new TimeSpan((long)(defaultDp / 4.0));

                clearBuffer();

                _prevResetTime = Environment.TickCount;
                _prevAnalyzeTime = Environment.TickCount;

                _audioClient.Start();

                // イベント発火
                CaptureStartedEventHandler del = CaptureStarted;
                if (del != null)
                {
                    del.Invoke(this, new CaptureStartedEventArgs());
                }
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
#warning エラー処理
            }
        }
        // 今溜まっている分を解析する
        private void analyzeBuffer()
        {
            _prevAnalyzeTime = Environment.TickCount;
            _pitchAnalyzer.SampleFrequency = _capFormat.nSamplesPerSec;

            double[] buf = _buffer.ToArray();
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] /= _capFormat.nChannels;
            }
            PitchResult result = _pitchAnalyzer.Analyze(buf);
            ToneResult tone = _toneAnalyzer.Analyze(result.Pitch, result.Clarity);

            // イベント発火
            DataUpdatedEventHandler del = DataUpdated;
            if (del != null)
            {
                del.Invoke(this, new DataUpdatedEventArgs(result, tone));
            }
        }
        // 不要な分を落とす
        private void truncateBuffer()
        {
            if (_buffer.Count > MAXBUFLEN)
            {
                _buffer.RemoveRange(0, _buffer.Count - MAXBUFLEN);
                foreach (List<double> c in _channelBuffer)
                {
                    c.RemoveRange(0, c.Count - MAXBUFLEN);
                }
            }
        }
        // バッファをクリアする
        private void clearBuffer()
        {
            _buffer.Clear();
            _channelBuffer.Clear();
            for (int i = 0; i < _capFormat.nChannels; i++)
            {
                _channelBuffer.Add(new List<double>());
            }
        }

        private void resetImpl()
        {
            if (!string.IsNullOrEmpty(_capDeviceId))
                selectDeviceImpl(_capDeviceId);
            stopCaptureImpl();
            startCaptureImpl();
            _reset = true;
        }
        #endregion

        #region メインスレッド
        private void mainThread()
        {
            try
            {
                while (true)
                {
                    Operation op = new Operation(Operation.OprationType.None);
                    lock (_opQueue)
                    {
                        if (_opQueue.Count > 0)
                        {
                            op = _opQueue.Dequeue();
                        }
                    }

                    switch (op.OpType)
                    {
                        case Operation.OprationType.Dispose: // 終了条件
                            releaseDevice();
                            disposeImpl();
                            _capturing = false;
                            return;
                        case Operation.OprationType.UpdateDevices: // Device一覧アップデート条件
                            updateDeviceInfoImpl((bool)op.OpArgs[0]);
                            break;
                        case Operation.OprationType.SelectDevice: // Device選択条件
                            selectDeviceImpl(op.OpArgs[0] as string);
                            _capturing = false;
                            break;
                        case Operation.OprationType.StartCapture: // 開始条件
                            startCaptureImpl();
                            _capturing = true;
                            break;
                        case Operation.OprationType.StopCapture: // 解放条件(stop)
                            stopCaptureImpl();
                            _capturing = false;
                            break;
                        default: // その他(GetBuffer)
                            {
                                #region キャプチャ
                                if (_capturing)
                                {
                                    if (_capClient != null)
                                    {
                                        long curTick = Environment.TickCount;
                                        uint size = 0;

                                        try
                                        {
                                            size = _capClient.NextPacketSize;

                                            if (size == 0) // 音が無い or バッファが一定量溜まっていない
                                            {
                                                if (!_reset)
                                                {
                                                    // データが来なくなってから一定時間 -> リセット
                                                    if (curTick - _prevResetTime > RESETLIMIT)
                                                    {
                                                        _prevResetTime = curTick;
                                                        resetImpl();

                                                        DataUpdatedEventHandler del = DataUpdated;
                                                        if (del != null)
                                                        {
                                                            del.Invoke(this, new DataUpdatedEventArgs(new PitchResult(0, 0), ToneResult.Default));
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                _reset = false;
                                                _prevResetTime = curTick;

                                                try
                                                {
                                                    while (_capClient.NextPacketSize > 0)
                                                    {
                                                        byte[] bytes; uint numFrames;
                                                        AudioClientBufferFlags flags;
                                                        UInt64 devicePosition; UInt64 qpcPosition;

                                                        _capClient.GetBuffer(out bytes, out numFrames, out flags, out devicePosition, out qpcPosition);
                                                        //if ((flags & AudioClientBufferFlags.DataDiscontinuity) != 0)
                                                        //    clearBuffer();

                                                        switch (_capFormat.wBitsPerSample)
                                                        {
                                                            case 8:
                                                                get8bitBuf(bytes);
                                                                break;
                                                            case 16:
                                                                get16bitBuf(bytes);
                                                                break;
                                                            case 24:
                                                                get24bitBuf(bytes);
                                                                break;
                                                            case 32:
                                                                get32bitBuf(bytes);
                                                                break;
                                                        }
                                                    }
                                                }
                                                catch (Exception) { }
                                                truncateBuffer();
                                                if (curTick - _prevAnalyzeTime > ANALYZESPAN) analyzeBuffer();
                                            }
                                        }
                                        catch (System.Runtime.InteropServices.COMException ex)
                                        {
#warning エラー処理
                                        }
                                    }
                                }
                                #endregion
                            }
                            break;
                    }
                    Thread.Sleep(_sleepTime);
                }
            }
            catch (ThreadAbortException threadEx)
            {
                Console.WriteLine(threadEx.Message);
            }
        }
        #endregion

        #region byte列からdouble列に変換し、バッファに書き込む
        private void get8bitBuf(byte[] bytes)
        {
            int idx = 0; int sample = 0;
            while (idx < bytes.Length)
            {
                double d = (bytes[idx] + 256) / (double)(255 + 256);

                int c = sample % _capFormat.nChannels;
                _channelBuffer[c].Add(d);
                if (c == 0) _buffer.Add(d);
                else _buffer[_buffer.Count - 1] += d;

                idx++;
                sample++;
            }
            Debug.Assert(idx == bytes.Length, "byte配列の長さがブロック境界と一致しない");
        }
        private void get16bitBuf(byte[] bytes)
        {
            int idx = 0; int sample = 0;
            while (idx < bytes.Length)
            {
                double d = BitConverter.ToInt16(bytes, idx) / (double)(short.MaxValue);

                int c = sample % _capFormat.nChannels;
                _channelBuffer[c].Add(d);
                if (c == 0) _buffer.Add(d);
                else _buffer[_buffer.Count - 1] += d;

                idx += 2;
                sample++;
            }
            Debug.Assert(idx == bytes.Length, "byte配列の長さがブロック境界と一致しない");
        }
        private void get24bitBuf(byte[] bytes)
        {
            int idx = 0; int sample = 0;
            while (idx < bytes.Length)
            {
                char c0 = BitConverter.ToChar(bytes, idx++);
                char c1 = BitConverter.ToChar(bytes, idx++);
                char c2 = BitConverter.ToChar(bytes, idx++);
                double d = (c0 + c1 * 256 + c2 * 256 * 256) / 8388608.0;

                int c = sample % _capFormat.nChannels;
                _channelBuffer[c].Add(d);
                if (c == 0) _buffer.Add(d);
                else _buffer[_buffer.Count - 1] += d;

                sample++;
            }
            Debug.Assert(idx == bytes.Length, "byte配列の長さがブロック境界と一致しない");
        }
        private void get32bitBuf(byte[] bytes)
        {
            int idx = 0; int sample = 0;
            if (_capFormat.SubFormat == CoreAudioApi.AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT)
            {
                while (idx < bytes.Length)
                {
                    double d = (double)BitConverter.ToSingle(bytes, idx);

                    int c = sample % _capFormat.nChannels;
                    _channelBuffer[c].Add(d);
                    if (c == 0) _buffer.Add(d);
                    else _buffer[_buffer.Count - 1] += d;

                    idx += 4;
                    sample++;
                }
                Debug.Assert(idx == bytes.Length, "byte配列の長さがブロック境界と一致しない");
            }
        }
        #endregion

        internal class Operation
        {
            public enum OprationType
            {
                Dispose,
                StopCapture,
                UpdateDevices,
                StartCapture,
                SelectDevice,
                None,
            }

            private OprationType opType;
            private object[] opArgs;
            public OprationType OpType { get { return opType; } }
            public object[] OpArgs { get { return opArgs; } }

            public Operation(OprationType type, params object[] args)
            {
                opType = type;
                opArgs = new object[args.Length];
                Array.Copy(args, opArgs, args.Length);
            }
        }

    }

    #region delegate and event args for AudioInput
    delegate void InputDisposedEventHandler(object s, InputDisposedEventArgs e);
    class InputDisposedEventArgs : EventArgs { }
    delegate void DeviceInfoUpdatedEventHandler(object s, DeviceInfoUpdatedEventArgs e);
    class DeviceInfoUpdatedEventArgs : EventArgs
    {
        public readonly List<DeviceInfo> DeviceInfo;
        public DeviceInfoUpdatedEventArgs(List<DeviceInfo> info)
        {
            DeviceInfo = info;
        }
    }
    delegate void DeviceSelectedEventHandler(object s, DeviceSelectedEventArgs e);
    class DeviceSelectedEventArgs : EventArgs
    {
        public readonly MMDevice Device;
        public readonly int Index;
        public DeviceSelectedEventArgs(MMDevice dev, int index)
        {
            Device = dev;
            Index = index;
        }
    }
    delegate void CaptureStartedEventHandler(object s, CaptureStartedEventArgs e);
    class CaptureStartedEventArgs : EventArgs { }
    delegate void CaptureStoppedEventHandler(object s, CaptureStoppedEventArgs e);
    class CaptureStoppedEventArgs : EventArgs { }
    delegate void DataUpdatedEventHandler(object s, DataUpdatedEventArgs e);
    class DataUpdatedEventArgs : EventArgs
    {
        public readonly PitchResult Pitch;
        public readonly ToneResult Tone;
        public DataUpdatedEventArgs(PitchResult pitchResult, ToneResult toneResult)
        {
            Pitch = pitchResult;
            Tone = toneResult;
        }
    }
    #endregion

    class DeviceInfo
    {
        private string _friendlyName;
        public string FriendlyName { get { return _friendlyName; } }

        private string _deviceId;
        public string DeviceId { get { return _deviceId; } }

        private EDataFlow _dataFlow = EDataFlow.eCapture;
        public EDataFlow DataFlow { get { return _dataFlow; } }

        private EDeviceState _state = EDeviceState.DEVICE_STATE_ACTIVE;
        public EDeviceState State { get { return _state; } }

        public DeviceInfo(string deviceId, string name, EDataFlow flow, EDeviceState state)
        {
            _deviceId = deviceId;
            _friendlyName = name;
            _dataFlow = flow;
            _state = state;
        }

        public override string ToString()
        {
            return string.Format("<{0}> {1} : {2} ({3})",
                _dataFlow == EDataFlow.eCapture ? "録音" : "再生",
                _friendlyName, _state, _deviceId);
        }
    }

    class SessionInfo
    {
        private string _displayName;
        public string DisplayName { get { return _displayName; } }

        private uint _processId;
        public uint ProcessId { get { return _processId; } }

        private Guid _guid;
        public Guid Guid { get { return _guid; } }

        public SessionInfo(uint procId, Guid guid, string name)
        {
            _processId = procId;
            _guid = guid;
            _displayName = name;
        }

        public override string ToString()
        {
            return string.Format("[{0}]{1}({2})",
                _processId, _displayName, _guid);
        }
    }
}
