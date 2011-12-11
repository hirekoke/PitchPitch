using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMNotificationClient
    {
        [PreserveSig]
        int OnDeviceStateChanged(string strDeviceId, EDeviceState newState);
        [PreserveSig]
        int OnDeviceAdded(string strDeviceId);
        [PreserveSig]
        int OnDeviceRemoved(string strDeviceId);
        [PreserveSig]
        int OnDefaultDeviceChanged(EDataFlow flow, ERole role, string strDefaultDeviceId);
        [PreserveSig]
        int OnPropertyValueChanged(string strDeviceId, PropertyKey key);
    }
}
