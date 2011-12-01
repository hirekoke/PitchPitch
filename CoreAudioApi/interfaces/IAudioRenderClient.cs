using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioRenderClient
    {
        [PreserveSig]
        int GetBuffer(UInt32 numFramesRequested, out IntPtr data);
        [PreserveSig]
        int ReleaseBuffer(UInt32 numFramesWritten, AudioClientBufferFlags flags);
    }
}
