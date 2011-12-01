using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    Guid("93014887-242D-4068-8A15-CF5E93B90FE3"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioStreamVolume
    {
        [PreserveSig]
        int GetChannelCount(out uint Count);
        [PreserveSig]
        int SetChannelVolume(uint Index, [MarshalAs(UnmanagedType.R4)] float Level);
        [PreserveSig]
        int GetChannelVolume(uint index, [MarshalAs(UnmanagedType.R4)] out float Level);
        [PreserveSig]
        int SetAllVolumes(uint Count, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.R4)] float[] Volumes);
        [PreserveSig]
        int GetAllVolumes(uint Count, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.R4)] float[] Volumes);
    }
}
