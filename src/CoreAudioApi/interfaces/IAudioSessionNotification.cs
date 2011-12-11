using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;                                                                          

namespace CoreAudioApi.Interfaces
{
    [Guid("641DD20B-4D41-49CC-ABA3-174B9477BB08"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionNotification
    {
        [PreserveSig]
        int OnSessionCreated(IAudioSessionControl2 newSession);
    }
}