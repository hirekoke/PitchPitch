using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("DF45AEEA-B74A-4B6B-AFAD-2366B6AA012E"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioMute
    {
        [PreserveSig]
        int SetMute(int bMuted, Guid guidEventContext);
        [PreserveSig]
        int GetMute(out int bMuted);
    }
}
