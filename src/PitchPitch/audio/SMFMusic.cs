using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace PitchPitch.audio.SMF
{
    class SMFException : Exception {
        public SMFException(string msg) : base(msg) { }
    }

    class SMFMusic : Music
    {
        public string Title;
        public string Copyright;

        private SMFHeader _header;
        private List<MusicNote> _points;

        private struct SMFNote
        {
            public bool On;
            public double Pitch;
            public SMFNote(bool on, double pitch)
            {
                On = on; Pitch = pitch;
            }
        }
        public override void Load(string filePath)
        {
            List<List<TrackEvent>> trackEvents = new List<List<TrackEvent>>();
            BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open), Encoding.UTF8);

            _header = readHeader(reader);
            if (!_header.DeltaTimeInPPQN) throw new SMFException("対応していないMIDIファイル:TimeInSec Division");
            if (_header.FormatType == 2) throw new SMFException("対応していないMIDIファイル:Format");

            for (int i = 0; i < _header.TrackNum; i++)
            {
                List<TrackEvent> evs = new List<TrackEvent>();
                readTrack(reader, ref evs);
                trackEvents.Add(evs);
            }

            double maxPitch = double.MinValue;
            double minPitch = double.MaxValue;

            long prevTick = 0;
            double time = 0;
            int tempo = 500000;
            double ppqnInv = 1 / (double)_header.DeltaTime;
            Dictionary<int, Dictionary<int, SMFNote>> channels = new Dictionary<int, Dictionary<int, SMFNote>>();

            _points = new List<MusicNote>();
            while (true)
            {
                if (trackEvents.Count == 0) break;
                if (trackEvents.TrueForAll((lst) => { return lst.Count == 0; })) break;

                int idx = -1; long minTick = long.MaxValue;
                for (int i = 0; i < trackEvents.Count; i++)
                {
                    if (trackEvents[i].Count == 0) continue;
                    long tick = trackEvents[i][0].AbsoluteTick;
                    if (tick < minTick) { idx = i; minTick = tick; }
                }

                if (idx < 0) break;

                TrackEvent fstev = trackEvents[idx][0];
                trackEvents[idx].RemoveAt(0);
                time += (fstev.AbsoluteTick - prevTick) * tempo * ppqnInv / 1000000.0;
                
                switch (fstev.EventType)
                {
                    case EventType.Tempo:
                        tempo = (int)fstev.Params[0];
                        break;
                    case EventType.NoteOn:
                        {
                            int channel = (int)fstev.Params[0];
                            int noteNum = (int)fstev.Params[1];

                            Dictionary<int, SMFNote> noteNums;
                            if (channels.ContainsKey(channel))
                            {
                                noteNums = channels[channel];
                            }
                            else
                            {
                                noteNums = new Dictionary<int, SMFNote>();
                                channels.Add(channel, noteNums);
                            }

                            SMFNote note;
                            if (noteNums.ContainsKey(noteNum))
                            {
                                note = noteNums[noteNum];

                                MusicNote n = new MusicNote();
                                n.TimeInSec = time;
                                n.Pitch = note.Pitch;
                                n.Start = false;
                                _points.Add(n);
                            }

                            note = new SMFNote(true, pitchFromNote(noteNum));
                            if (noteNums.ContainsKey(noteNum))
                                noteNums[noteNum] = note;
                            else
                                noteNums.Add(noteNum, note);

                            MusicNote nn = new MusicNote();
                            nn.TimeInSec = time;
                            nn.Pitch = note.Pitch;
                            nn.Start = true;
                            _points.Add(nn);

                            if (note.Pitch < minPitch) minPitch = note.Pitch;
                            if (note.Pitch > maxPitch) maxPitch = note.Pitch;
                        }
                        break;
                    case EventType.NoteOff:
                        {
                            int channel = (int)fstev.Params[0];
                            int noteNum = (int)fstev.Params[1];

                            Dictionary<int, SMFNote> noteNums;
                            if (channels.ContainsKey(channel))
                            {
                                noteNums = channels[channel];
                            }
                            else
                            {
                                noteNums = new Dictionary<int, SMFNote>();
                                channels.Add(channel, noteNums);
                            }

                            SMFNote note;
                            if (noteNums.ContainsKey(noteNum))
                            {
                                note = noteNums[noteNum];

                                MusicNote n = new MusicNote();
                                n.TimeInSec = time;
                                n.Pitch = note.Pitch;
                                n.Start = false;
                                _points.Add(n);

                                if (note.Pitch < minPitch) minPitch = note.Pitch;
                                if (note.Pitch > maxPitch) maxPitch = note.Pitch;

                                noteNums.Remove(noteNum);
                            }
                        }
                        break;
                }

                prevTick = fstev.AbsoluteTick;
            }

            _length = time;
            MinPitch = minPitch - 30;
            MaxPitch = maxPitch + 30;
        }

        public override IEnumerable<MusicNote> GetNotes()
        {
            foreach (MusicNote d in _points) yield return d;
        }



        private static SMFHeader readHeader(BinaryReader reader)
        {
            SMFHeader header = new SMFHeader();

            char[] chunkIDChars = reader.ReadChars(4);
            string chunkID = new string(chunkIDChars);
            if (chunkID != "MThd")
            {
                throw new SMFException("invalid chunkID");
            }
            uint dataSize = readBENumeric(reader, 4);
            header.FormatType = readBENumeric(reader, 2);
            header.TrackNum = readBENumeric(reader, 2);
            header.DeltaTime = readBENumeric(reader, 2);
            header.DeltaTimeInPPQN = false;
            if ((header.DeltaTime & 0x8000) == 0) // MSBが立っていない
            {
                header.DeltaTimeInPPQN = true;
                header.DeltaTime = header.DeltaTime & 0x7fff;
            }
            else
            {
                header.DeltaTime = (header.DeltaTime & 0x7f00) * (header.DeltaTime & 0x00ff);
            }

            if (dataSize > 6) reader.ReadBytes((int)(dataSize - 6));

            return header;
        }

        private void readTrack(BinaryReader reader, ref List<TrackEvent> trackEvents)
        {
            char[] chunkIDChars = reader.ReadChars(4);
            string chunkID = new string(chunkIDChars);
            if (chunkID != "MTrk")
            {
                throw new SMFException("invalid chunkID");
            }

            long prevEvTick = 0;
            uint dataSize = readBENumeric(reader, 4);
            uint readDataSize = 0;
            byte prevStatusByte = 0;
            while (readDataSize < dataSize)
            {
                uint deltaTime;
                readDataSize += readVarLenNumeric(reader, out deltaTime);
                prevEvTick += deltaTime;

                byte statusByte = reader.ReadByte();

                uint readSize;
                TrackEvent ev;
                switch (statusByte)
                {
                    #region SysEx イベント
                    case 0xf0:
                    case 0xf7:
                        ev = readSysExEvent(statusByte, reader, out readSize);
                        break;
                    #endregion

                    #region メタイベント
                    case 0xff:
                        ev = readMetaEvent(statusByte, reader, out readSize);
                        break;
                    #endregion

                    #region MIDI イベント
                    default:
                        ev = readMidiEvent(statusByte, prevStatusByte, reader, out readSize);
                        prevStatusByte = statusByte;
                        break;
                    #endregion
                }
                readDataSize += readSize;
                if (ev != null)
                {
                    ev.RelativeTick = deltaTime;
                    ev.AbsoluteTick = prevEvTick;
                    trackEvents.Add(ev);
                }
            }
            return;
        }

        private TrackEvent readMidiEvent(byte statusByte, byte prevStatusByte, BinaryReader reader, out uint readDataSize)
        {
            readDataSize = 2;

            byte param1 = 0;
            byte param2 = 0;

            bool runningStatus = (statusByte & 0x80) == 0;
            if (runningStatus)
            {
                param1 = statusByte;
                statusByte = prevStatusByte;
                readDataSize--;
            }
            else
            {
                param1 = reader.ReadByte();
            }

            int channel = (statusByte & 0x0f);
            switch (statusByte >> 4)
            {
                case 0x8: // SMFNote Off
                    {
                        param2 = reader.ReadByte();
                        readDataSize++;

                        int noteNum = param1 & 0x7f;
                        int velocity = param2 & 0x7f;

                        TrackEvent ev = new TrackEvent();
                        ev.EventType = EventType.NoteOff;
                        ev.Params = new object[] { channel, noteNum, velocity };
                        return ev;
                    }
                case 0x9: // SMFNote On
                    {
                        param2 = reader.ReadByte();
                        readDataSize++;

                        int noteNum = param1 & 0x7f;
                        int velocity = param2 & 0x7f;

                        TrackEvent ev = new TrackEvent();
                        ev.EventType = velocity == 0 ? EventType.NoteOff : EventType.NoteOn;
                        ev.Params = new object[] { channel, noteNum, velocity };
                        return ev;
                    }
                case 0xa: // Polyphonic Key Pressure (Aftertouch)
                    {
                        param2 = reader.ReadByte();
                        readDataSize++;

                        int noteNum = param1 & 0x7f;
                        int amount = param2 & 0x7f;
                    }
                    break;
                case 0xb: // Control Change
                    {
                        param2 = reader.ReadByte();
                        readDataSize++;

                        int noteNum = param1 & 0x7f;
                        int amount = param2 & 0x7f;
                    }
                    break;
                case 0xc: // Program Change
                    {
                        int programNum = param1 & 0x7f;
                    }
                    break;
                case 0xd: // Channel Pressure (After-touch)
                    {
                        int amount = param1 & 0x7f;
                    }
                    break;
                case 0xe: // Pitch Wheel Change
                    {
                        param2 = reader.ReadByte();
                        readDataSize++;

                        int value = param1 + param2 << 7;
                    }
                    break;
            }

            return null;
        }

        private TrackEvent readSysExEvent(byte statusByte, BinaryReader reader, out uint readDataSize)
        {
            readDataSize = 0;
            uint dataSize;
            readDataSize += readVarLenNumeric(reader, out dataSize);
            reader.ReadBytes((int)dataSize); // 読み飛ばす
            readDataSize += dataSize + 1 /* statusByte の分 */;
            return null;
        }

        private TrackEvent readMetaEvent(byte statusByte, BinaryReader reader, out uint readDataSize)
        {
            readDataSize = 0;
            byte eventType = reader.ReadByte();
            uint dataSize;
            readDataSize += readVarLenNumeric(reader, out dataSize);
            readDataSize += dataSize + 1 /* eventType */ + 1 /* statusByte */;

            switch (eventType)
            {
                case 0x2f: // Track End
                    return null;
                case 0x51: // Tempo
                    {
                        // 四分音符の長さ(microsec, 10^-6
                        int tempo = (int)(readBENumeric(reader, 3));
                        TrackEvent ev = new TrackEvent();
                        ev.EventType = EventType.Tempo;
                        ev.Params = new object[] { tempo };
                        return ev;
                    }
                case 0x58: // 拍子
                    {
                        byte b1 = reader.ReadByte(); // 分子
                        byte b2 = reader.ReadByte(); // 分母(2のマイナス乗)
                        byte b3 = reader.ReadByte(); // 1拍当たりのMIDIクロック数
                        byte b4 = reader.ReadByte(); // 四分音符の中に入る32分の音符数
                        if (b3 != 0x18 || b4 != 0x08)
                        {
                            throw new SMFException("対応していないMIDIファイル:拍子記号");
                        }
                    }
                    return null;
                case 0x54:
                case 0x00:
                case 0x01:
                case 0x02:
                case 0x03:
                case 0x04:
                case 0x05:
                case 0x06:
                case 0x07:
                case 0x08:
                case 0x09:
                case 0x20:
                case 0x21:
                case 0x59:
                case 0x7f:
                default:
                    reader.ReadBytes((int)dataSize); // 読み飛ばし
                    return null;
            }
        }

        private static uint readVarLenNumeric(BinaryReader reader, out uint value)
        {
            bool next = false;
            value = 0;
            uint len = 0;
            do
            {
                byte b = reader.ReadByte();
                next = (b & 0x80) != 0x00;
                b = (byte)(b & 0x7f);
                value = b | (value << 7);
                len++;
            } while (next);
            return len;
        }

        private static uint readBENumeric(BinaryReader reader, int len)
        {
            uint ret = 0;
            for (int i = 0; i < len; i++)
            {
                byte b = reader.ReadByte();
                ret = (ret << 8) | b;
            }
            return ret;
        }

        private double pitchFromNote(int noteNum)
        {
            if (_notePitches.ContainsKey(noteNum)) return _notePitches[noteNum];

            int toneIdx = noteNum % 12;
            int octave = (int)((noteNum - toneIdx) / 12.0) - 1;
            double pitch = ToneAnalyzer.PitchFromTone(toneIdx, octave);
            _notePitches.Add(noteNum, pitch);
            return pitch;
        }

        private Dictionary<int, double> _notePitches = new Dictionary<int, double>();
    }

    struct SMFHeader
    {
        public uint FormatType;
        public uint TrackNum;
        public uint DeltaTime;
        public bool DeltaTimeInPPQN;
    }

    class TrackEvent
    {
        public long AbsoluteTick;
        public uint RelativeTick;
        public EventType EventType;
        public object[] Params;
    }

    enum EventType
    {
        #region Midi Event
        NoteOff,
        NoteOn,
        NoteAftertouch,
        Controller,
        ProgramChange, // not used
        ChannelAftertouch, // not used
        PitchBend,
        #endregion

        #region SysEx Event
        #endregion

        #region Meta Event
        Tempo,

        #endregion
    }
}
