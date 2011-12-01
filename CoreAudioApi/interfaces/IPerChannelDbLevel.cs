using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [Guid("C2F8E001-F205-4BC9-99BC-C13B1E048CCB"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPerChannelDbLevel
    {
        [PreserveSig]
        int GetChannelCount(out uint Channels);
        [PreserveSig]
        int GetLevelRange(uint Channel, out float MinLevelDB, out float MaxLevelDB, out float Stepping);
        [PreserveSig]
        int GetLevel(uint nChannel, out float LevelDB);
        [PreserveSig]
        int SetLevel(uint nChannel, float LevelDB, Guid guidEventContext);
        [PreserveSig]
        int SetLevelUniform(float LevelDB, Guid guidEventContext);
        [PreserveSig]
        int SetLevelAllChannels(IntPtr LevelsDB, ulong cChannels, Guid guidEventContext);
    }

    [Guid("A2B1A1D9-4DB3-425D-A2B2-BD335CB3E2E5"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioBass : IPerChannelDbLevel 
    {
    }

    [Guid("0A717812-694E-4907-B74B-BAFA5CFDCA7B"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioTreble : IPerChannelDbLevel
    {
    }

    [Guid("7FB7B48F-531D-44A2-BCB3-5AD5A134B3DC"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioVolumeLevel : IPerChannelDbLevel
    {
    }
}
