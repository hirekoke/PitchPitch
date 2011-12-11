using System;
using System.Collections.Generic;
using System.Text;

namespace CoreAudioApi
{
    public enum AudioClientShareMode : uint
    {
        /// <summary>
        /// The device will be opened in shared mode and use the WAS format.
        /// </summary>
        Shared = 0,
        /// <summary>
        /// The device will be opened in exclusive mode and use the application specified format.
        /// </summary>
        Exclusive = 1,
    }
}
