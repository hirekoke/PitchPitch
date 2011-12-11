using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("A09513ED-C709-4d21-BD7B-5F34C47F3947"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IControlChangeNotify
    {
        [PreserveSig]
        int OnNotify(uint SenderProcessId, Guid guidEventContext);
    }
}
