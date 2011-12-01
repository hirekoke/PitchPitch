using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("7D8B1437-DD53-4350-9C1B-1EE2890BD938"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioLoudness
    {
        [PreserveSig]
        int GetEnabled(out int bEnabled);
        [PreserveSig]
        int SetEnabled(int bEnable, Guid guidEventContext);
    }
}
