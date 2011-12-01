using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("478F3A9B-E0C9-4827-9228-6F5505FFE76A"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IKsJackDescription2
    {
        [PreserveSig]
        int GetJackCount(out uint cJacks);
        [PreserveSig]
        int GetJackDescription(uint nJack, out KSJACK_DESCRIPTION Description);
        [PreserveSig]
        int GetJackDescription2(uint nJack, out KSJACK_DESCRIPTION2 Description2);
    }
}
