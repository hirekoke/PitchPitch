using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("D9BD72ED-290F-4581-9FF3-61027A8FE532"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IKsJackSinkInformation
    {
        [PreserveSig]
        int GetJackSinkInformation(out KSJACK_SINK_INFORMATION JackSinkInformation);
    }
}
