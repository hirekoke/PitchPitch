using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioClient
    {
        [PreserveSig]
        int Initialize(
            AudioClientShareMode shareMode,
            AudioClientStreamFlags streamFlags,
            long bufferDuration,
            long periodicity,
            WAVEFORMATEXTENSIBLE pFormat,
            ref Guid AudioSessionGuid);
        [PreserveSig]
        int GetBufferSize(out UInt32 numBufferFrames);
        [PreserveSig]
        int GetStreamLatency(out long latency);
        [PreserveSig]
        int GetCurrentPadding(out UInt32 numPaddingFrames);
        [PreserveSig]
        int IsFormatSupported(
            AudioClientShareMode shareMode,
            WAVEFORMATEXTENSIBLE pFormat,
            out WAVEFORMATEXTENSIBLE closestMatch);
        [PreserveSig]
        int GetMixFormat(out WAVEFORMATEXTENSIBLE deviceFormat);
        [PreserveSig]
        int GetDevicePeriod(out long DefaultDevicePeriod, out long minimumDevicePeriod);
        [PreserveSig]
        int Start();
        [PreserveSig]
        int Stop();
        [PreserveSig]
        int Reset();
        [PreserveSig]
        int SetEventHandle(IntPtr eventHandle);

        [PreserveSig]
        int GetService(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
    }
}
