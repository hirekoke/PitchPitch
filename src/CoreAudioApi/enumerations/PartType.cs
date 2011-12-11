using System;
using System.Collections.Generic;
using System.Text;

namespace CoreAudioApi
{
    /// <summary>
    /// whether a part in a device topology is a connector or subunit
    /// </summary>
    public enum PartType : uint
    {
        /// <summary>
        /// The part is a connector. A connector can represent an audio jack, an internal connection to an integrated endpoint device, or a software connection implemented through DMA transfers.
        /// </summary>
        Connector = 0,
        /// <summary>
        /// The part is a subunit. A subunit is an audio-processing node in a device topology. A subunit frequently has one or more hardware control parameters that can be set under program control. For example, an audio application can change the volume setting of a volume-control subunit.
        /// </summary>
        Subunit = 1,
    }
}
