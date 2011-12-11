using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using CoreAudioApi.Interfaces;

namespace CoreAudioApi
{
    #region Event args
    public class SessionDisplayNameChangedEventArgs : EventArgs
    {
        public readonly string DisplayName;
        public readonly Guid EventContext;
        public SessionDisplayNameChangedEventArgs(string newDisplayName, Guid eventContext)
        {
            DisplayName = newDisplayName;
            EventContext = eventContext;
        }
    }
    public class SessionIconPathChangedEventArgs : EventArgs
    {
        public readonly string IconPath;
        public readonly Guid EventContext;
        public SessionIconPathChangedEventArgs(string newIconPath, Guid eventContext)
        {
            IconPath = newIconPath;
            EventContext = eventContext;
        }
    }
    public class SessionSimpleVolumeChangedEventArgs : EventArgs
    {
        public readonly float Volume;
        public readonly bool Mute;
        public readonly Guid EventContext;
        public SessionSimpleVolumeChangedEventArgs(float vol, bool mute, Guid ec)
        {
            Volume = vol;
            Mute = mute;
            ec = EventContext;
        }
    }
    public class SessionChannelVolumeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Each element is a value of type float that specifies the volume level for a particular channel. Each volume level is a value in the range 0.0 to 1.0, where 0.0 is silence and 1.0 is full volume (no attenuation).
        /// </summary>
        public readonly float[] ChannelVolumeArray;
        /// <summary>
        /// The number of the channel whose volume level changed.
        /// </summary>
        public readonly uint ChangedChannel;
        /// <summary>
        /// The event context value. This is the same value that the caller passed to the IChannelAudioVolume::SetChannelVolume or IChannelAudioVolume::SetAllVolumes method in the call that initiated the change in volume level of the channel. 
        /// </summary>
        public readonly Guid EventContext;
        public SessionChannelVolumeChangedEventArgs(float[] volArray, uint channel, Guid ec)
        {
            ChannelVolumeArray = volArray;
            ChangedChannel = channel;
            EventContext = ec;
        }
    }
    public class SessionGroupingParamChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The new grouping parameter for the session. 
        /// </summary>
        public readonly Guid GroupingParam;
        /// <summary>
        /// The event context value. This is the same value that the caller passed to IAudioSessionControl::SetGroupingParam in the call that changed the grouping parameter for the session.
        /// </summary>
        public readonly Guid EventContext;
        public SessionGroupingParamChangedEventArgs(Guid gParam, Guid ec)
        {
            GroupingParam = gParam;
            EventContext = ec;
        }
    }
    public class SessionStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The new session state.
        /// </summary>
        public readonly AudioSessionState State;
        public SessionStateChangedEventArgs(AudioSessionState state)
        {
            State = state;
        }
    }
    public class SessionDisconnectedEventArgs : EventArgs
    {
        /// <summary>
        /// The reason that the audio session was disconnected
        /// </summary>
        public readonly AudioSessionDisconnectReason Reason;
        public SessionDisconnectedEventArgs(AudioSessionDisconnectReason reason)
        {
            Reason = reason;
        }
    }
    #endregion

    #region delegate
    public delegate void SessionDisplayNameChangedEventHandler(object s, SessionDisplayNameChangedEventArgs e);
    public delegate void SessionIconPathChangedEventHandler(object s, SessionIconPathChangedEventArgs e);
    public delegate void SessionSimpleVolumeChangedEventHandler(object s, SessionSimpleVolumeChangedEventArgs e);
    public delegate void SessionChannelVolumeChangedEventHandler(object s, SessionChannelVolumeChangedEventArgs e);
    public delegate void SessionGroupingParamChangedEventHandler(object s, SessionGroupingParamChangedEventArgs e);
    public delegate void SessionStateChangedEventHandler(object s, SessionStateChangedEventArgs e);
    public delegate void SessionDisconnectedEventHandler(object s, SessionDisconnectedEventArgs e);
    #endregion

    internal class AudioSessionEvents : IAudioSessionEvents
    {
        private AudioSessionControl _sessionControl;

        public AudioSessionEvents(AudioSessionControl sessionControl)
        {
            _sessionControl = sessionControl;
        }

        [PreserveSig]
        public int OnDisplayNameChanged(string NewDisplayName, Guid EventContext)
        {
            if (_sessionControl != null)
            {
                SessionDisplayNameChangedEventArgs e = new SessionDisplayNameChangedEventArgs(NewDisplayName, EventContext);
                _sessionControl.FireDisplayNameChangedEvent(e);
            }
            return 0;
        }

        [PreserveSig]
        public int OnIconPathChanged(string NewIconPath, Guid EventContext)
        {
            if (_sessionControl != null)
            {
                SessionIconPathChangedEventArgs e = new SessionIconPathChangedEventArgs(NewIconPath, EventContext);
                _sessionControl.FireIconPathChangedEvent(e);
            }
            return 0;
        }

        [PreserveSig]
        public int OnSimpleVolumeChanged(float NewVolume, bool newMute, Guid EventContext)
        {
            if (_sessionControl != null)
            {
                SessionSimpleVolumeChangedEventArgs e = new SessionSimpleVolumeChangedEventArgs(NewVolume, newMute, EventContext);
                _sessionControl.FireSimpleVolumeChanged(e);
            }
            return 0;
        }

        [PreserveSig]
        public int OnChannelVolumeChanged(uint ChannelCount, IntPtr NewChannelVolumeArray, uint ChangedChannel, Guid EventContext)
        {
            float[] array = new float[ChannelCount];
            Marshal.Copy(NewChannelVolumeArray, array, 0, array.Length);
            Marshal.FreeCoTaskMem(NewChannelVolumeArray);

            if (_sessionControl != null)
            {
                SessionChannelVolumeChangedEventArgs e = new SessionChannelVolumeChangedEventArgs(array, ChangedChannel, EventContext);
                _sessionControl.FireChannelVolumeChanged(e);
            }
            return 0;
        }

        [PreserveSig]
        public int OnGroupingParamChanged(Guid NewGroupingParam, Guid EventContext)
        {
            if (_sessionControl != null)
            {
                SessionGroupingParamChangedEventArgs e = new SessionGroupingParamChangedEventArgs(NewGroupingParam, EventContext);
                _sessionControl.FireGroupingParamChanged(e);
            }
            return 0;
        }

        [PreserveSig]
        public int OnStateChanged(AudioSessionState NewState)
        {
            if (_sessionControl != null)
            {
                SessionStateChangedEventArgs e = new SessionStateChangedEventArgs(NewState);
                _sessionControl.FireStateChanged(e);
            }
            return 0;
        }

        [PreserveSig]
        public int OnSessionDisconnected(AudioSessionDisconnectReason DisconnectReason)
        {
            if (_sessionControl != null)
            {
                SessionDisconnectedEventArgs e = new SessionDisconnectedEventArgs(DisconnectReason);
                _sessionControl.FireSessionDisconnected(e);
            }
            return 0;
        }
    }
}
