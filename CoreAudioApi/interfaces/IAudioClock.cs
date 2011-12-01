using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    Guid("6f49ff73-6727-49ac-a008-d98cf5e70048"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioClock2
    {
        // methods of IAudioClock
        [PreserveSig]
        int GetFrequency(out UInt64 frequency);
        [PreserveSig]
        int GetPosition(out UInt64 position, out UInt64 QPCPosition);

        // reserved for future use
        [PreserveSig]
        int GetCharacteristics(out uint characteristics);

        // methods of IAudioClock2
        [PreserveSig]
        int GetDevicePosition(out UInt64 devicePosition, out UInt64 QPCPosition);
    }

    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    Guid("f6e4c0a0-46d9-4fb8-be21-57a3ef2b626c"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioClockAdjustment
    {
        [PreserveSig]
        int SetSampleRate(float sampleRate);
    }
}
