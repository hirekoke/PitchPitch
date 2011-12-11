using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("C3B284D4-6D39-4359-B3CF-B56DDB3BB39C"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioVolumeDuckNotification
    {
        [PreserveSig]
        int OnVolumeDuckNotification(string sessionID, UInt32 countCommunicationSessions);
        [PreserveSig]
        int onVolumeUnduckNotification(string sessionID);
    }
}
