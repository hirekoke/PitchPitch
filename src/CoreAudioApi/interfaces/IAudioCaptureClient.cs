using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioCaptureClient
    {
        [PreserveSig]
        int GetBuffer(
            out IntPtr data, 
            out uint numFramesToRead, 
            out AudioClientBufferFlags flags, 
            out UInt64 devicePosition, 
            out UInt64 qpcPosition);
        [PreserveSig]
        int ReleaseBuffer(uint numFramesRead);
        [PreserveSig]
        int GetNextPacketSize(out uint numFramesInNextPacket);
    }
}
