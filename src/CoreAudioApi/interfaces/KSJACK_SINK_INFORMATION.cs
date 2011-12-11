using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{/// <summary>
    /// Stores information about an audio jack sink.
    /// </summary>
    /// <remarks>
    /// MSDN Reference: http://msdn.microsoft.com/en-us/library/dd316549.aspx
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct KSJACK_SINK_INFORMATION
    {
        /// <summary>
        /// Specifies the type of connection.
        /// </summary>
        [MarshalAs(UnmanagedType.I4)]
        public Int32 ConnType;

        /// <summary>
        /// Specifies the sink manufacturer identifier.
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public UInt16 ManufacturerId;

        /// <summary>
        /// Specifies the sink product identifier.
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public UInt16 ProductId;

        /// <summary>
        /// Specifies the latency of the audio sink.
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public UInt16 AudioLatency;

        /// <summary>
        /// Specifies whether the sink supports High-bandwidth Digital Content Protection (HDCP).
        /// </summary>
        [MarshalAs(UnmanagedType.Bool)]
        public bool HDCPCapable;

        /// <summary>
        /// Specifies whether the sink supports ACP Packet, ISRC1, or ISRC2.
        /// </summary>
        [MarshalAs(UnmanagedType.Bool)]
        public bool AICapable;

        /// <summary>
        /// Specifies the length of the string in the SinkDescription member.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public byte SinkDescriptionLength;
    }
}
