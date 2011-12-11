using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("6DAA848C-5EB0-45CC-AEA5-998A2CDA1FFB"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPartsList
    {
        [PreserveSig]
        int GetCount(out uint Count);
        [PreserveSig]
        int GetPart(uint nIndex, out IPart Part);
    }
}
