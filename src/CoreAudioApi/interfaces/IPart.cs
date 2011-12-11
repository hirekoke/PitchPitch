using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("AE2DE0E4-5BCA-4F2D-AA46-5D13F8FDB3A9"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPart
    {
        [PreserveSig]
        int GetName([MarshalAs(UnmanagedType.LPWStr)]out string Name);
        [PreserveSig]
        int GetLocalId(out uint Id);
        [PreserveSig]
        int GetGlobalId([MarshalAs(UnmanagedType.LPWStr)]out string GlobalId);
        [PreserveSig]
        int GetPartType(out PartType PartType);
        [PreserveSig]
        int GetSubType(out Guid SubType);
        [PreserveSig]
        int GetControlInterfaceCount(out uint Count);
        [PreserveSig]
        int GetControlInterface(uint nIndex, out IControlInterface InterfaceDesc);
        [PreserveSig]
        int EnumPartsIncoming(out IPartsList Parts);
        [PreserveSig]
        int EnumPartsOutgoing(out IPartsList Parts);
        [PreserveSig]
        int GetTopologyObject(out IDeviceTopology Topology);
        [PreserveSig]
        int Activate(CLSCTX ClsContext, ref Guid refiid, [MarshalAs(UnmanagedType.IUnknown)] out object Object);
        [PreserveSig]
        int RegisterControlChangeCallback(ref Guid refiid, IControlChangeNotify Notify);
        [PreserveSig]
        int UnregisterControlChangeCallback(IControlChangeNotify Notify);
    }
}
