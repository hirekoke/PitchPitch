using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("BB11C46F-EC28-493C-B88A-5DB88062CE98"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioChannelConfig
    {
        [PreserveSig]
        int SetChannelConfig(uint config, Guid guidEventContext);
        [PreserveSig]
        int GetChannelConfig(out uint config);
    }
}
