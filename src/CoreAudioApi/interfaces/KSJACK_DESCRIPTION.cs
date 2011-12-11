using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    /// <summary>
    /// Describes an audio jack.
    /// </summary>
    /// <remarks>
    /// MSDN Reference: http://msdn.microsoft.com/en-us/library/dd316543.aspx
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct KSJACK_DESCRIPTION
    {
        /// <summary>
        /// Specifies the mapping of the two audio channels in a stereo jack to speaker positions.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public Int32 ChannelMapping;

        /// <summary>
        /// The jack color.
        /// </summary>
        [MarshalAs(UnmanagedType.U4)]
        public UInt32 Color;

        /// <summary>
        /// The connection type.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public Int32 ConnectionType;

        /// <summary>
        /// The geometric location of the jack.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public Int32 GeoLocation;

        /// <summary>
        /// The general location of the jack.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public Int32 GenLocation;

        /// <summary>
        /// The type of port represented by the jack.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public Int32 PortConnection;

        /// <summary>
        /// Indicates whether an endpoint device is plugged into the jack, if supported by the adapter.
        /// </summary>
        [MarshalAs(UnmanagedType.Bool)]
        public bool IsConnected;
    }

    /// <summary>
    /// Describes an audio jack.
    /// </summary>
    /// <remarks>
    /// MSDN Reference: http://msdn.microsoft.com/en-us/library/dd316545.aspx
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct KSJACK_DESCRIPTION2
    {
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        [MarshalAs(UnmanagedType.U4)]
        public UInt32 DeviceStateInfo;

        /// <summary>
        /// Stores the audio jack's capabilities.
        /// </summary>
        /// <remarks>
        /// From Ksmedia.h, the available flags for this are:
        /// 1. JACKDESC2_PRESENCE_DETECT_CAPABILITY (0x00000001)
        /// 2. JACKDESC2_DYNAMIC_FORMAT_CHANGE_CAPABILITY (0x00000002) 
        /// </remarks>
        [MarshalAs(UnmanagedType.U4)]
        public UInt32 JackCapabilities;
    }
}
