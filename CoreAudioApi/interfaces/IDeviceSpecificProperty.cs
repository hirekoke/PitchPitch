using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("3B22BCBF-2586-4af0-8583-205D391B807C"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDeviceSpecificProperty
    {
        [PreserveSig]
        int GetType(VarEnum VType);
        [PreserveSig]
        int GetValue(out object vValue, ref uint cbValue);
        [PreserveSig]
        int SetValue(object vValue, uint cbValue, Guid guidEventContext);
        [PreserveSig]
        int Get4BRange(out long lMin, out long lMax, long lStepping);
    }
}
