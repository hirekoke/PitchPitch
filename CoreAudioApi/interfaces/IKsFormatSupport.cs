using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("3CB4A69D-BB6F-4D2B-95B7-452D2C155DB5"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IKsFormatSupport
    {
        [PreserveSig]
        int IsFormatSupported(uint cbFormat, out int bSupported);
        [PreserveSig]
        int GetDevicePreferredFormat(ref KSDATAFORMAT KsFormat);
    }
}
