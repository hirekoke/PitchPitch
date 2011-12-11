using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    Guid("1C158861-B533-4B30-B1CF-E853E51C59B8"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IChannelAudioVolume
    {
        [PreserveSig]
        int GetChannelCount(out int Count);
        [PreserveSig]
        int SetChannelVolume(int index, float Level);
        [PreserveSig]
        int GetChannelVolume(int Index, out float Level);
        [PreserveSig]
        int SetAllVolumes(int Count, float[] Levels);
        [PreserveSig]
        int GetAllVolumes(int Count, out float[] Levels);
    }
}
