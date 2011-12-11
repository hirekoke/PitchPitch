using System;
using System.Collections.Generic;
using System.Text;

namespace CoreAudioApi
{
    public enum AudioClientStreamFlags
    {
        /// <summary>
        /// The audio stream will be a member of a cross-process audio session
        /// </summary>
        CrossProcess = 0x00010000,
        /// <summary>
        /// The audio stream will operate in loopback mode
        /// </summary>
        Loopback = 0x00020000,
        /// <summary>
        /// Processing of the audio buffer by the client will be event driven
        /// </summary>
        EventCallback = 0x00040000,
        /// <summary>
        /// The volume and mute settings for an audio session will not persist across system restarts
        /// </summary>
        NoPersist = 0x00080000,
        /// <summary>
        /// This constant is new in Windows 7. The sample rate of the stream is adjusted to a rate specified by an application.
        /// </summary>
        RateAdjust = 0x00100000,
    }
}
