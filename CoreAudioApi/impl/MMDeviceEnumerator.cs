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
 *   - added MMNotificationClient event
 *   - implemented IDisposable interface
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using CoreAudioApi.Interfaces;

namespace CoreAudioApi
{
    //Marked as internal, since on its own its no good
    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class _MMDeviceEnumerator
    {
    }

    //Small wrapper class
    public class MMDeviceEnumerator : IDisposable
    {
        private IMMDeviceEnumerator _realEnumerator = new _MMDeviceEnumerator() as IMMDeviceEnumerator;

        #region event
        /* added -> */
        private MMNotificationClient _notificationClient;

        public event DeviceStateChangedEventHandler OnDeviceStateChanged;
        public event DeviceAddedEventHandler OnDeviceAdded;
        public event DeviceRemovedEventHandler OnDeviceRemoved;
        public event DefaultDeviceChangedEventHandler OnDefaultDeviceChanged;
        public event PropertyValueChangedEventHandler OnPropertyValueChanged;

        internal void FireDeviceStateChangedEvent(DeviceStateChangedEventArgs e)
        {
            DeviceStateChangedEventHandler del = OnDeviceStateChanged;
            if (del != null) del(this, e);
        }
        internal void FireDeviceAddedEvent(DeviceAddedEventArgs e)
        {
            DeviceAddedEventHandler del = OnDeviceAdded;
            if (del != null) del(this, e);
        }
        internal void FireDeviceRemovedEvent(DeviceRemovedEventArgs e)
        {
            DeviceRemovedEventHandler del = OnDeviceRemoved;
            if (del != null) del(this, e);
        }
        internal void FireDefaultDeviceChangedEvent(DefaultDeviceChangedEventArgs e)
        {
            DefaultDeviceChangedEventHandler del = OnDefaultDeviceChanged;
            if (del != null) del(this, e);
        }
        internal void FirePropertyValueChangedEvent(PropertyValueChangedEventArgs e)
        {
            PropertyValueChangedEventHandler del = OnPropertyValueChanged;
            if (del != null) del(this, e);
        }
        /* <- added */
        #endregion

        public MMDeviceCollection EnumerateAudioEndPoints(EDataFlow dataFlow, EDeviceState dwStateMask)
        {
            IMMDeviceCollection result;
            Marshal.ThrowExceptionForHR(_realEnumerator.EnumAudioEndpoints(dataFlow,dwStateMask,out result));
            return new MMDeviceCollection(result);
        }

        public MMDevice GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role)
        {
            IMMDevice _Device = null;
            Marshal.ThrowExceptionForHR(((IMMDeviceEnumerator)_realEnumerator).GetDefaultAudioEndpoint(dataFlow, role, out _Device));
            return new MMDevice(_Device);
        }

        public MMDevice GetDevice(string ID)
        {
            IMMDevice _Device = null;
            Marshal.ThrowExceptionForHR(((IMMDeviceEnumerator)_realEnumerator).GetDevice(ID, out _Device));
            return new MMDevice(_Device);
        }

        public MMDeviceEnumerator()
        {
            if (System.Environment.OSVersion.Version.Major < 6)
            {
                throw new NotSupportedException("This functionality is only supported on Windows Vista or newer.");
            }

            /* added -> */
            _notificationClient = new MMNotificationClient(this);
            int hr = _realEnumerator.RegisterEndpointNotificationCallback(_notificationClient);
            Marshal.ThrowExceptionForHR(hr);
            /* <- added */
        }

        /* added -> */
        public void Dispose()
        {
            if (_notificationClient != null)
            {
                int hr = _realEnumerator.UnregisterEndpointNotificationCallback(_notificationClient);
                Marshal.ThrowExceptionForHR(hr);
                _notificationClient = null;
            }
            if (_realEnumerator != null) Marshal.ReleaseComObject(_realEnumerator);
        }
        /* <- added */
    }
}
