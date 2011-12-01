using System;
using System.Collections.Generic;
using System.Text;

namespace CoreAudioApi
{
    /// <summary>
    /// data-flow direction of an audio stream through a connector
    /// </summary>
    public enum DataFlow : uint
    {
        /// <summary>
        /// Input stream. The audio stream flows into the device through the connector.
        /// </summary>
        In = 0,
        /// <summary>
        /// Output stream. The audio stream flows out of the device through the connector.
        /// </summary>
        Out = 1,
    }
}
