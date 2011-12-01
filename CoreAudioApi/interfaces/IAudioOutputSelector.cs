using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("BB515F69-94A7-429e-8B9C-271B3F11A3AB"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioOutputSelector
    {
        [PreserveSig]
        int GetSelection(out uint IdSelected);
        [PreserveSig]
        int SetSelection(uint nIdSelect, Guid guidEventContext);
    }
}
