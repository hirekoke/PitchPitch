using System;
using System.Collections.Generic;
using System.Text;

using CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;

namespace CoreAudioApi
{
    public class AudioRenderClient : IDisposable
    {
        private IAudioRenderClient _RealClient;

        #region Constructor
        internal AudioRenderClient(IAudioRenderClient realClient)
        {
            _RealClient = realClient;
        }

        public void Dispose()
        {
            if (_RealClient != null)
            {
                Marshal.ReleaseComObject(_RealClient);
            }
        }
        #endregion

        public byte[] GetBuffer(uint numFramesRequested)
        {
            IntPtr p;
            int hr = _RealClient.GetBuffer(numFramesRequested, out p);
            Marshal.ThrowExceptionForHR(hr);

            byte[] data = new byte[numFramesRequested];
            Marshal.Copy(p, data, 0, (int)numFramesRequested);
            return data;
        }

        public void ReleaseBuffer(uint numFramesWritten, AudioClientBufferFlags flags)
        {
            int hr = _RealClient.ReleaseBuffer(numFramesWritten, flags);
            Marshal.ThrowExceptionForHR(hr);
        }
    }
}
