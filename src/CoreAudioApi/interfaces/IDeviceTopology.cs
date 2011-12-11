using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("2A07407E-6497-4A18-9787-32F79BD0D98F"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDeviceTopology
    {
        [PreserveSig]
        int GetConnectorCount(out uint Count);
        [PreserveSig]
        int GetConnector(uint nIndex, out IConnector Connector);
        [PreserveSig]
        int GetSubunitCount(out uint Count);
        [PreserveSig]
        int GetSubunit(uint nIndex, out ISubunit Subunit);
        [PreserveSig]
        int GetPartById(uint Id, out IPart Part);
        [PreserveSig]
        int GetDeviceId([MarshalAs(UnmanagedType.LPWStr)]out string DeviceId);
        [PreserveSig]
        int GetSignalPath(IPart IPartFrom, IPart IPartTo, int bRejectMixedPaths, out IPartsList Parts);
    }
}
