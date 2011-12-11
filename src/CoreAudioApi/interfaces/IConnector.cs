using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("9c2c4058-23f5-41de-877a-df3af236a09e"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IConnector
    {
        [PreserveSig]
        int GetType(out ConnectorType Type);
        [PreserveSig]
        int GetDataFlow(out DataFlow Flow);
        [PreserveSig]
        int ConnectTo(IConnector ConnectTo);
        [PreserveSig]
        int Disconnect();
        [PreserveSig]
        int IsConnected(out int bConnected);
        [PreserveSig]
        int GetConnectedTo(out IConnector ConTo);
        [PreserveSig]
        int GetConnectorIdConnectedTo([MarshalAs(UnmanagedType.LPWStr)]out string ConnectorId);
        [PreserveSig]
        int GetDeviceIdConnectedTo([MarshalAs(UnmanagedType.LPWStr)]out string DeviceId);
    }
}
