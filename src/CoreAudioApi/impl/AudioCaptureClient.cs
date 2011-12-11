using System;
using System.Collections.Generic;
using System.Text;

using CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;

namespace CoreAudioApi
{
    public class AudioCaptureClient : IDisposable
    {
        private AudioClient _Parent;
        private IAudioCaptureClient _RealClient;
        private object _lockObj = new object();

        #region Constructor
        internal AudioCaptureClient(AudioClient parent, IAudioCaptureClient realClient)
        {
            _Parent = parent;
            _RealClient = realClient;
        }

        public void Dispose()
        {
            if (_RealClient != null)
            {
                lock(_lockObj)
                    Marshal.ReleaseComObject(_RealClient);
            }
        }
        #endregion

        /// <summary>
        /// バッファの中の現在の1パケットを取得する
        /// <remarks>バッファ内の全データを取得するには、NextPacketSizeが0になるまでGetBuffer()を繰り返す必要がある</remarks>
        /// </summary>
        /// <param name="data">取得したデータのバイト列</param>
        /// <param name="numFramesToRead">取得したデータのフレーム数(ブロック単位)</param>
        /// <param name="flags"></param>
        /// <param name="devicePosition"></param>
        /// <param name="qpcPosition"></param>
        public void GetBuffer(
            out byte[] data,
            out uint numFramesToRead,
            out AudioClientBufferFlags flags,
            out ulong devicePosition,
            out ulong qpcPosition)
        {
            lock (_lockObj)
            {
                IntPtr p;
                int hr = _RealClient.GetBuffer(
                    out p, out numFramesToRead,
                    out flags, out devicePosition,
                    out qpcPosition);
                Marshal.ThrowExceptionForHR(hr);
                int byteSize = (int)numFramesToRead * _Parent.MixFormat.nBlockAlign;
                data = new byte[byteSize];
                Marshal.Copy(p, data, 0, byteSize);

                hr = _RealClient.ReleaseBuffer(numFramesToRead);
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        /// <summary>
        /// 次に読めるパケットのサイズを取得する
        /// </summary>
        public uint NextPacketSize
        {
            get
            {
                lock (_lockObj)
                {
                    uint size;
                    int hr = _RealClient.GetNextPacketSize(out size);
                    Marshal.ThrowExceptionForHR(hr);
                    return size;
                }
            }
        }
    }
}
