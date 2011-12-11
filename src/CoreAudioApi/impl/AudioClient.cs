using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using CoreAudioApi.Interfaces;

namespace CoreAudioApi
{
    public class AudioSampleReadyEventArgs : EventArgs
    {
        public AudioSampleReadyEventArgs() { }
    }
    public delegate void AudioSampleReadyEventHandler(object sender, AudioSampleReadyEventArgs e);

    public class AudioClient : IDisposable
    {
        private IAudioClient _RealClient;

        #region Constants
        private static Guid IID_IAudioCaptureClient = typeof(IAudioCaptureClient).GUID;
        private static Guid IID_IAudioClock = typeof(IAudioClock2).GUID;
        private static Guid IID_IAudioClockAdjustment = typeof(IAudioClockAdjustment).GUID;
        private static Guid IID_IAudioRenderClient = typeof(IAudioRenderClient).GUID;
        private static Guid IID_IAudioSessionControl = typeof(IAudioSessionControl2).GUID;
        private static Guid IID_IAudioStreamVolume = typeof(IAudioStreamVolume).GUID;
        private static Guid IID_IChannelAudioVolume = typeof(IChannelAudioVolume).GUID;
        private static Guid IID_IMFTrustedOutput = Guid.Empty;
        private static Guid IID_ISimpleAudioVolume = typeof(ISimpleAudioVolume).GUID;
        #endregion

        private AudioCaptureClient _audioCaptureClient = null;
        private AudioSessionControl _audioSessionControl = null;
        private AudioStreamVolume _audioStreamVolume = null;
        private SimpleAudioVolume _simpleAudioVolume = null;
        private AudioClock _audioClock = null;
        private AudioClockAdjustment _audioClockAdjustment = null;

        private WaitHandle _audioSampleReady = null;
        private RegisteredWaitHandle _audioSampleReadyRegistered = null;
        public event AudioSampleReadyEventHandler OnAudioSampleReady;

        private bool _isStarted = false;
        private bool _isInitialized = false;

        enum CreateEventFlags : uint
        {
            InitialSet = 0x02,
            ManualReset = 0x01,
            None = 0x00,
        }
        enum AccessRight : uint
        {
            Delete = 0x00010000,
            ReadControl = 0x00020000,
            Synchronize = 0x00100000,
            WriteDac = 0x00040000,
            WriteOwner = 0x00080000,
            EventAllAccess = 0x1F0003,
            EventModifyState = 0x0002,
        }

        [DllImport("kernel32.dll", EntryPoint = "CreateEventEx")]
        private static extern IntPtr CreateEventEx(
            IntPtr eventAttributes, [MarshalAs(UnmanagedType.LPTStr)]string name,
            CreateEventFlags flags, AccessRight desiredAccess);

        #region Inits
        private void GetAudioCaptureClient()
        {
            object result;
            int hr = _RealClient.GetService(ref IID_IAudioCaptureClient, out result);
            Marshal.ThrowExceptionForHR(hr);
            _audioCaptureClient = new AudioCaptureClient(this, result as IAudioCaptureClient);
        }
        private void GetAudioSessionControl()
        {
            object result;
            Marshal.ThrowExceptionForHR(_RealClient.GetService(ref IID_IAudioSessionControl, out result));
            _audioSessionControl = new AudioSessionControl(result as IAudioSessionControl2);
        }
        private void GetSimpleAudioVolume()
        {
            object result;
            Marshal.ThrowExceptionForHR(_RealClient.GetService(ref IID_ISimpleAudioVolume, out result));
            _simpleAudioVolume = new SimpleAudioVolume(result as ISimpleAudioVolume);
        }
        private void GetAudioClock()
        {
            object result;
            Marshal.ThrowExceptionForHR(_RealClient.GetService(ref IID_IAudioClock, out result));
            _audioClock = new AudioClock(result as IAudioClock2);
        }
        private void GetAudioClockAdjustment()
        {
            object result;
            Marshal.ThrowExceptionForHR(_RealClient.GetService(ref IID_IAudioClockAdjustment, out result));
            _audioClockAdjustment = new AudioClockAdjustment(result as IAudioClockAdjustment);
        }
        private void GetAudioStreamVolume()
        {
            object result;
            Marshal.ThrowExceptionForHR(_RealClient.GetService(ref IID_IAudioStreamVolume, out result));
            _audioStreamVolume = new AudioStreamVolume(result as IAudioStreamVolume);
        }
        #endregion

        #region Properties
        public AudioCaptureClient AudioCaptureClient
        {
            get
            {
                if (_audioCaptureClient == null) GetAudioCaptureClient();
                return _audioCaptureClient;
            }
        }
        public AudioSessionControl AudioSessionControl
        {
            get
            {
                if (_audioSessionControl == null) GetAudioSessionControl();
                return _audioSessionControl;
            }
        }
        public SimpleAudioVolume SimpleAudioVolume
        {
            get
            {
                if (_simpleAudioVolume == null) GetSimpleAudioVolume();
                return _simpleAudioVolume;
            }
        }
        public AudioClock AudioClock
        {
            get
            {
                if (_audioClock == null) GetAudioClock();
                return _audioClock;
            }
        }
        /// <summary>
        /// <remarks>AudioClockAdjustment must be obtained from an audio client that is initialized with both the AUDCLNT_STREAMFLAGS_RATEADJUST flag and the share mode set to AUDCLNT_SHAREMODE_SHARED. If Initialize is called in an exclusive mode with the AUDCLNT_STREAMFLAGS_RATEADJUST flag, Initialize fails with the AUDCLNT_E_UNSUPPORTED_FORMAT error code.</remarks>
        /// </summary>
        public AudioClockAdjustment AudioClockAdjustment
        {
            get
            {
                if (_audioClockAdjustment == null) GetAudioClockAdjustment();
                return _audioClockAdjustment;
            }
        }
        public AudioStreamVolume AudioStreamVolume
        {
            get
            {
                if (_audioStreamVolume == null) GetAudioStreamVolume();
                return _audioStreamVolume;
            }
        }


        public uint BufferSize
        {
            get
            {
                uint result;
                Marshal.ThrowExceptionForHR(_RealClient.GetBufferSize(out result));
                return result;
            }
        }
        public long StreamLatency
        {
            get
            {
                long result;
                Marshal.ThrowExceptionForHR(_RealClient.GetStreamLatency(out result));
                return result;
            }
        }
        public uint CurrentPadding
        {
            get
            {
                uint result;
                Marshal.ThrowExceptionForHR(_RealClient.GetCurrentPadding(out result));
                return result;
            }
        }
        public WAVEFORMATEXTENSIBLE MixFormat
        {
            get
            {
                WAVEFORMATEXTENSIBLE result = new WAVEFORMATEXTENSIBLE();
                Marshal.ThrowExceptionForHR(_RealClient.GetMixFormat(out result));
                return result;
            }
        }

        public bool IsStarted { get { return _isStarted; } }
        public bool IsInitialized { get { return _isInitialized; } }
        #endregion

        #region Methods

        public void Initialize(
            AudioClientShareMode shareMode,
            AudioClientStreamFlags streamFlags,
            long bufferDuration, long periodicity,
            WAVEFORMATEXTENSIBLE format, Guid audioSessionGuid)
        {
            int hr = _RealClient.Initialize(shareMode, streamFlags, bufferDuration, periodicity, format, ref audioSessionGuid);
            Marshal.ThrowExceptionForHR(hr);

            if ((streamFlags & AudioClientStreamFlags.EventCallback) != 0)
            {
                _audioSampleReady = new AutoResetEvent(false);
                IntPtr eventHandle = CreateEventEx(IntPtr.Zero, "audioSampleReady", CreateEventFlags.None, AccessRight.Synchronize | AccessRight.EventModifyState);
                _audioSampleReady.SafeWaitHandle = new Microsoft.Win32.SafeHandles.SafeWaitHandle(eventHandle, true);

                hr = _RealClient.SetEventHandle(eventHandle);
                Marshal.ThrowExceptionForHR(hr);

                _audioSampleReadyRegistered = ThreadPool.RegisterWaitForSingleObject(
                    _audioSampleReady, new WaitOrTimerCallback(sampleReady), null, -1, false);
            }
            _isInitialized = true;
        }

        private void sampleReady(object state, bool timeout)
        {
            AudioSampleReadyEventHandler del = OnAudioSampleReady;
            if (del != null)
            {
                AudioSampleReadyEventArgs arg = new AudioSampleReadyEventArgs();
                del(this, arg);
            }
            //_audioSampleReadyRegistered = ThreadPool.RegisterWaitForSingleObject(
            //    _audioSampleReady, new WaitOrTimerCallback(test), null, -1, true);
        }

        public bool IsFormatSupported(AudioClientShareMode shareMode, WAVEFORMATEXTENSIBLE format, ref WAVEFORMATEXTENSIBLE closestMatch)
        {
            int hr = _RealClient.IsFormatSupported(shareMode, format, out closestMatch);
            bool ret = false;
            if (hr == 0) ret = true;
            else if (hr == 1) ret = false;
            else Marshal.ThrowExceptionForHR(hr);
            
            return ret;
        }

        public void GetDevicePeriod(out long DefaultDevicePeriod, out long MinimumDevicePeriod)
        {
            long r1; long r2;
            Marshal.ThrowExceptionForHR(_RealClient.GetDevicePeriod(out r1, out r2));
            DefaultDevicePeriod = r1;
            MinimumDevicePeriod = r2;
        }

        public void Start()
        {
            if (!_isStarted)
                Marshal.ThrowExceptionForHR(_RealClient.Start());
            _isStarted = true;
        }
        public void Stop()
        {
            if (_isStarted)
                Marshal.ThrowExceptionForHR(_RealClient.Stop());
            _isStarted = false;
        }
        public void Reset()
        {
            if(_isInitialized)
                Marshal.ThrowExceptionForHR(_RealClient.Reset());
        }
        #endregion

        #region Constructor
        internal AudioClient(IAudioClient realClient)
        {
            _RealClient = realClient;
        }
        #endregion

        public void Dispose()
        {
            if (_audioSampleReadyRegistered != null)
            {
                _audioSampleReadyRegistered.Unregister(_audioSampleReady);
            }
            if (_RealClient != null)
            {
                _RealClient.Stop();
                Marshal.ReleaseComObject(_RealClient);
            }
        }
    }
}
