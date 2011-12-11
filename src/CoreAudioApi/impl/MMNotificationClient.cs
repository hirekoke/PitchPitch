using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using CoreAudioApi.Interfaces;
namespace CoreAudioApi
{
    #region EventArgs
    public class DeviceAddedEventArgs : EventArgs
    {
        public readonly string DeviceId;
        public DeviceAddedEventArgs(string deviceId)
        {
            DeviceId = deviceId;
        }
    }
    public class DeviceRemovedEventArgs : EventArgs
    {
        public readonly string DeviceId;
        public DeviceRemovedEventArgs(string deviceId)
        {
            DeviceId = deviceId;
        }
    }
    public class DeviceStateChangedEventArgs : EventArgs
    {
        public readonly string DeviceId;
        public readonly EDeviceState NewState;
        public DeviceStateChangedEventArgs(string deviceId, EDeviceState newState)
        {
            DeviceId = deviceId;
            NewState = newState;
        }
    }
    public class DefaultDeviceChangedEventArgs : EventArgs
    {
        public readonly EDataFlow Flow;
        public readonly ERole Role;
        public readonly string DeviceId;
        public DefaultDeviceChangedEventArgs(EDataFlow flow, ERole role, string defaultDeviceId)
        {
            Flow = flow;
            Role = role;
            DeviceId = defaultDeviceId;
        }
    }
    public class PropertyValueChangedEventArgs : EventArgs
    {
        public readonly string DeviceId;
        public readonly PropertyKey PropertyKey;
        public PropertyValueChangedEventArgs(string deviceId, PropertyKey propertyKey)
        {
            DeviceId = deviceId;
            PropertyKey = propertyKey;
        }
    }
    #endregion

    #region Delegate
    public delegate void DeviceStateChangedEventHandler(object sender, DeviceStateChangedEventArgs e);
    public delegate void DeviceAddedEventHandler(object sender, DeviceAddedEventArgs e);
    public delegate void DeviceRemovedEventHandler(object sender, DeviceRemovedEventArgs e);
    public delegate void DefaultDeviceChangedEventHandler(object sender, DefaultDeviceChangedEventArgs e);
    public delegate void PropertyValueChangedEventHandler(object sender, PropertyValueChangedEventArgs e);
    #endregion

    internal class MMNotificationClient : IMMNotificationClient
    {
        private MMDeviceEnumerator _DeviceEnumerator;

        internal MMNotificationClient(MMDeviceEnumerator deviceEnumerator)
        {
            _DeviceEnumerator = deviceEnumerator;
        }

        [PreserveSig] 
        public int OnDeviceStateChanged(string DeviceId, EDeviceState newState)
        {
            DeviceStateChangedEventArgs arg = new DeviceStateChangedEventArgs(DeviceId, newState);
            _DeviceEnumerator.FireDeviceStateChangedEvent(arg);
            return 0;
        }

        [PreserveSig]
        public int OnDeviceAdded(string DeviceId)
        {
            DeviceAddedEventArgs arg = new DeviceAddedEventArgs(DeviceId);
            _DeviceEnumerator.FireDeviceAddedEvent(arg);
            return 0;
        }

        [PreserveSig]
        public int OnDeviceRemoved(string DeviceId)
        {
            DeviceRemovedEventArgs arg = new DeviceRemovedEventArgs(DeviceId);
            _DeviceEnumerator.FireDeviceRemovedEvent(arg);
            return 0;
        }

        [PreserveSig]
        public int OnDefaultDeviceChanged(EDataFlow flow, ERole role, string strDefaultDeviceId)
        {
            DefaultDeviceChangedEventArgs arg = new DefaultDeviceChangedEventArgs(flow, role, strDefaultDeviceId);
            _DeviceEnumerator.FireDefaultDeviceChangedEvent(arg);
            return 0;
        }

        [PreserveSig]
        public int OnPropertyValueChanged(string DeviceId, PropertyKey key)
        {
            PropertyValueChangedEventArgs arg = new PropertyValueChangedEventArgs(DeviceId, key);
            _DeviceEnumerator.FirePropertyValueChangedEvent(arg);
            return 0;
        }
    }
}
