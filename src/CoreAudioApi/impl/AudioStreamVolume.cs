using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using CoreAudioApi.Interfaces;

namespace CoreAudioApi
{
    public class AudioStreamVolume : IDisposable
    {
        IAudioStreamVolume _RealVolume;

        internal AudioStreamVolume(IAudioStreamVolume parent)
        {
            _RealVolume = parent;
        }

        public void Dispose()
        {
            if (_RealVolume != null)
            {
                Marshal.ReleaseComObject(_RealVolume);
            }
        }

        public uint ChannelCount
        {
            get
            {
                uint count;
                int hr = _RealVolume.GetChannelCount(out count);
                Marshal.ThrowExceptionForHR(hr);
                return count;
            }
        }

        /// <summary>
        /// get / set channel volume
        /// </summary>
        /// <param name="index">channel index</param>
        public float this[int index]
        {
            get
            {
                float level;
                int hr = _RealVolume.GetChannelVolume((uint)index, out level);
                Marshal.ThrowExceptionForHR(hr);
                return level;
            }
            set
            {
                int hr = _RealVolume.SetChannelVolume((uint)index, value);
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        public float[] GetAllVolumes()
        {
            uint count = ChannelCount;
            float[] levels = new float[count];
            int hr = _RealVolume.GetAllVolumes(count, levels);
            Marshal.ThrowExceptionForHR(hr);
            return levels;
        }
        public void SetAllVolumes(float[] levels)
        {
            uint count = ChannelCount;
            int hr = _RealVolume.SetAllVolumes(count, levels);
            Marshal.ThrowExceptionForHR(hr);
        }
    }
}
