/*
  LICENSE
  -------
  Copyright (C) 2007-2010 Ray Molenkamp

  This source code is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this source code or the software it produces.

  Permission is granted to anyone to use this source code for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this source code must not be misrepresented; you must not
     claim that you wrote the original source code.  If you use this source code
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original source code.
  3. This notice may not be removed or altered from any source distribution.
*/
/*
 * modified by hirekoke
 *   - added AudioClient property
 *   - implemented IDisposable interface
 */

using System;
using System.Collections.Generic;
using System.Text;
using CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;

namespace CoreAudioApi
{
    public class MMDevice : IDisposable
    {
        #region Variables
        private IMMDevice _RealDevice;
        private PropertyStore _PropertyStore;
        private AudioMeterInformation _AudioMeterInformation;
        private AudioEndpointVolume _AudioEndpointVolume;
        private AudioSessionManager _AudioSessionManager;
        private AudioClient _AudioClient; // added
        #endregion

        #region Guids
        private static Guid IID_IAudioMeterInformation = typeof(IAudioMeterInformation).GUID; 
        private static Guid IID_IAudioEndpointVolume = typeof(IAudioEndpointVolume).GUID;  
        private static Guid IID_IAudioSessionManager = typeof(IAudioSessionManager2).GUID;
        private static Guid IID_IAudioClient = typeof(IAudioClient).GUID; // added
        #endregion

        #region Init
        private void GetPropertyInformation()
        {
            IPropertyStore propstore;
            Marshal.ThrowExceptionForHR(_RealDevice.OpenPropertyStore(EStgmAccess.STGM_READ, out propstore));
            _PropertyStore = new PropertyStore(propstore);
        }

        private void GetAudioSessionManager()
        {
            object result;
            Marshal.ThrowExceptionForHR(_RealDevice.Activate(ref IID_IAudioSessionManager, CLSCTX.ALL, IntPtr.Zero, out result));
            _AudioSessionManager = new AudioSessionManager(result as IAudioSessionManager2);
        }

        private void GetAudioMeterInformation()
        {
            object result;
            Marshal.ThrowExceptionForHR( _RealDevice.Activate(ref IID_IAudioMeterInformation, CLSCTX.ALL, IntPtr.Zero, out result));
            _AudioMeterInformation = new AudioMeterInformation( result as IAudioMeterInformation);
        }

        private void GetAudioEndpointVolume()
        {
            object result;
            Marshal.ThrowExceptionForHR(_RealDevice.Activate(ref IID_IAudioEndpointVolume, CLSCTX.ALL, IntPtr.Zero, out result));
            _AudioEndpointVolume = new AudioEndpointVolume(result as IAudioEndpointVolume);
        }

        /* added -> */
        private void GetAudioClient()
        {
            object result;
            Marshal.ThrowExceptionForHR(_RealDevice.Activate(ref IID_IAudioClient, CLSCTX.ALL, IntPtr.Zero, out result));
            _AudioClient = new AudioClient(result as IAudioClient);
        }
        /* <- added */
        #endregion

        #region Properties
        /* added -> */
        public AudioClient AudioClient
        {
            get
            {
                if (_AudioClient == null)
                    GetAudioClient();

                return _AudioClient;
            }
        }
        /* <- added */

        public AudioSessionManager AudioSessionManager
        {
            get
            {
                if (_AudioSessionManager == null)
                    GetAudioSessionManager();

                return _AudioSessionManager;
            }
        }

        public AudioMeterInformation AudioMeterInformation
        {
            get
            {
                if (_AudioMeterInformation == null)
                    GetAudioMeterInformation();

                return _AudioMeterInformation;
            }
        }

        public AudioEndpointVolume AudioEndpointVolume
        {
            get
            {
                if (_AudioEndpointVolume == null)
                    GetAudioEndpointVolume();

                return _AudioEndpointVolume;
            }
        }

        public PropertyStore Properties
        {
            get
            {
                if (_PropertyStore == null)
                    GetPropertyInformation();
                return _PropertyStore;
            }
        }

        /* added */
        public WAVEFORMATEXTENSIBLE DeviceFormat
        {
            get
            {
                if (_PropertyStore.Contains(PKEY.PKEY_AudioEngine_DeviceFormat))
                {
                    byte[] bytes = (byte[])_PropertyStore[PKEY.PKEY_AudioEngine_DeviceFormat].Value;
                    int size = Marshal.SizeOf(typeof(WAVEFORMATEXTENSIBLE));
                    IntPtr p = Marshal.AllocHGlobal(size);
                    Marshal.Copy(bytes, 0, p, bytes.Length);
                    object o = new WAVEFORMATEXTENSIBLE();
                    Marshal.PtrToStructure(p, o);
                    return o as WAVEFORMATEXTENSIBLE;
                }
                return null;
            }
        }
        /* -- added */
        private string _id;
        public string Id { get { return _id; } }

        private EDataFlow _dataFlow;
        public EDataFlow DataFlow { get { return _dataFlow; } }

        private EDeviceState _state;
        public EDeviceState State { get { return _state; } }

        private string _friendlyName = "Unknown";
        public string FriendlyName { get { return _friendlyName; } }
        #endregion

        #region Constructor
        internal MMDevice(IMMDevice realDevice)
        {
            _RealDevice = realDevice;

            GetPropertyInformation();

            Marshal.ThrowExceptionForHR(_RealDevice.GetId(out _id));

            IMMEndpoint ep = _RealDevice as IMMEndpoint;
            Marshal.ThrowExceptionForHR(ep.GetDataFlow(out _dataFlow));

            Marshal.ThrowExceptionForHR(_RealDevice.GetState(out _state));

            if (_PropertyStore.Contains(PKEY.PKEY_DeviceInterface_FriendlyName))
                _friendlyName = (string)_PropertyStore[PKEY.PKEY_DeviceInterface_FriendlyName].Value;
        }
        #endregion

        #region IDisposable
        /* added -> */
        public void Dispose()
        {
            if (_RealDevice != null) Marshal.ReleaseComObject(_RealDevice);
        }
        /* <- added */
        #endregion

        public void ReleaseAudioClient()
        {
            if (_AudioClient != null) Marshal.ReleaseComObject(_AudioClient);
        }
    }
}
