using System;
using System.Collections.Generic;
using System.Text;

namespace CoreAudioApi
{
    /// <summary>
    /// the type of connection that a connector is part of
    /// </summary>
    public enum ConnectorType : uint
    {
        /// <summary>
        /// The connector is part of a connection of unknown type
        /// </summary>
        UnknownConnector = 0,
        /// <summary>
        /// The connector is part of a physical connection to an auxiliary device that is installed inside the system chassis (for example, a connection to the analog output of an internal CD player, or to a built-in microphone or built-in speakers in a laptop computer).
        /// </summary>
        PhysicalInternal = 1,
        /// <summary>
        /// The connector is part of a physical connection to an external device. That is, the connector is a user-accessible jack that connects to a microphone, speakers, headphones, S/PDIF input or output device, or line input or output device.
        /// </summary>
        PhysicalExternal = 2,
        /// <summary>
        /// The connector is part of a software-configured I/O connection (typically a DMA channel) between system memory and an audio hardware device on an audio adapter.
        /// </summary>
        SoftwareIO = 3,
        /// <summary>
        /// The connector is part of a permanent connection that is fixed and cannot be configured under software control. This type of connection is typically used to connect two audio hardware devices that reside on the same adapter.
        /// </summary>
        SoftwareFixed = 4,
        /// <summary>
        /// The connector is part of a connection to a network.
        /// </summary>
        Network = 5,
    }
}
