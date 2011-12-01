using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("85401FD4-6DE4-4b9d-9869-2D6753A82F3C"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioAutoGainControl
    {
        [PreserveSig]
        int GetEnabled(out int bEnabled);
        [PreserveSig]
        int SetEnabled(int bEnable, Guid guidEventContext);
    }
}
