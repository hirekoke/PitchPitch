using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("45d37c3f-5140-444a-ae24-400789f3cbf3"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IControlInterface
    {
        [PreserveSig]
        int GetName([MarshalAs(UnmanagedType.LPWStr)]out string Name);
        [PreserveSig]
        int GetIID(out Guid iid);
    }
}
