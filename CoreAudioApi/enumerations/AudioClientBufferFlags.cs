using System;
using System.Collections.Generic;
using System.Text;

namespace CoreAudioApi
{
    /// <summary>
    /// flags that indicate the status of an audio endpoint buffer
    /// </summary>
    public enum AudioClientBufferFlags
    {
        None = 0x00,
        /// <summary>The data in the packet is not correlated with the previous packet's device position; this is possibly due to a stream state transition or timing glitch.</summary>
        DataDiscontinuity = 0x01,
        /// <summary>Treat all of the data in the packet as silence and ignore the actual data values. For more information about the use of this flag, see Rendering a Stream and Capturing a Stream.</summary>
        Silent = 0x02,
        /// <summary>The time at which the device's stream position was recorded is uncertain. Thus, the client might be unable to accurately set the time stamp for the current data packet.</summary>
        TimestampError = 0x04
    }
}
