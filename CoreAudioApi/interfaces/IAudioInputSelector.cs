using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("4F03DC02-5E6E-4653-8F72-A030C123D598"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioInputSelector
    {
        [PreserveSig]
        int GetSelection(out uint nIdSelected);
        [PreserveSig]
        int SetSelection(uint nIdSelect, Guid guidEventContext);
    }
}
