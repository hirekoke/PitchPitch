using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace PitchPitch.audio.MML
{
    class MMLMusic : Music
    {
        private List<MusicNote> _points = new List<MusicNote>();

        private List<Note> _notes = new List<Note>();
        private int _defaultLength;
        private bool _onceOctaveUp;
        private int _curOctave;
        private int _curBpm;
        private double _curTime;
        private int _lenRatio = 8;
        private double _maxP = double.MinValue;
        private double _minP = double.MaxValue;
        private int _onceLenRatio = -1;
        private Stack<int> _repeatStartIdx = new Stack<int>();
        private int _groupNoteStartIdx = -1;
        private int _groupNoteNum = 0;

        public MMLMusic()
        {
        }

        struct Note
        {
            public int ToneIdx;
            public int Octave;
            public double Length;
            public int LenRatio;
            public Note Copy()
            {
                Note ret = new Note();
                ret.ToneIdx = ToneIdx;
                ret.Octave = Octave;
                ret.Length = Length;
                ret.LenRatio = LenRatio;
                return ret;
            }
        }

        public override void Load(string filePath)
        {
            _curTime = 0;
            _points.Clear();
            load(filePath);

            foreach (Note n in _notes)
            {
                if (n.ToneIdx >= 0)
                {
                    double pLen = n.Length;
                    if (n.LenRatio >= 0 && n.LenRatio < 8) pLen *= n.LenRatio / 8.0;

                    MusicNote onNote = new MusicNote();
                    onNote.Pitch = ToneAnalyzer.PitchFromTone(n.ToneIdx, n.Octave);
                    onNote.Start = true;
                    onNote.TimeInSec = _curTime;
                    _points.Add(onNote);

                    MusicNote offNote = new MusicNote();
                    offNote.Pitch = onNote.Pitch;
                    offNote.Start = false;
                    offNote.TimeInSec = _curTime + pLen;
                    _points.Add(offNote);

                    if (_maxP < onNote.Pitch) _maxP = onNote.Pitch;
                    if (_minP > onNote.Pitch) _minP = onNote.Pitch;
                }
                _curTime += n.Length;
            }

            Length = _curTime;

            ToneResult minTone = ToneAnalyzer.Analyze(_minP, 1.0);
            ToneResult maxTone = ToneAnalyzer.Analyze(_maxP, 1.0);

            maxTone.ToneIdx++;
            if (maxTone.ToneIdx >= 12) { maxTone.ToneIdx -= 12; maxTone.Octave++; }
            minTone.ToneIdx--;
            if (minTone.ToneIdx <= 0) { minTone.ToneIdx += 12; minTone.Octave--; }

            MinPitch = ToneAnalyzer.PitchFromTone(minTone.ToneIdx, minTone.Octave);
            MaxPitch = ToneAnalyzer.PitchFromTone(maxTone.ToneIdx, maxTone.Octave);

            _points.Sort((MusicNote n1, MusicNote n2) =>
            {
                if (n1.TimeInSec != n2.TimeInSec) return n1.TimeInSec.CompareTo(n2.TimeInSec);
                if (n1.Start)
                {
                    if (n2.Start) return 0;
                    return 1;
                }
                else
                {
                    if (n2.Start) return -1;
                    return 0;
                }
            });
        }

        private void loadNote(int toneIdx, string line, ref int idx)
        {
            int tone = toneIdx;
            int octave = _curOctave;
            if (_onceOctaveUp) octave++;

            #region シャープ・フラット
            if (toneIdx >= 0 && idx < line.Length - 1)
            {
                switch (line[idx])
                {
                    case '#': // sharp
                    case '+':
                        idx++;
                        tone++;
                        break;
                    case '-': // flat
                        idx++;
                        tone--;
                        break;
                }
            }
            #endregion

            int len = readNum(line, ref idx);
            if (len < 0) len = _defaultLength;

            #region 付点
            int pointNum = 0;
            while (idx < line.Length - 1)
            {
                if (line[idx] != '.') break;
                idx++;
                pointNum++;
            }
            double lenTime = 60 * 4 / ((double)_curBpm * len);
            if (pointNum > 0)
            {
                double lb = lenTime;
                for (int i = 0; i < pointNum; i++)
                {
                    lb /= 2.0;
                    lenTime += lb;
                }
            }
            #endregion

            Note note = new Note();
            note.Length = lenTime;
            note.LenRatio = toneIdx >= 0 ? (_onceLenRatio >= 0 ? _onceLenRatio : (_lenRatio >= 0 ? _lenRatio : 8)) : 8;
            note.ToneIdx = tone;
            note.Octave = octave;
            _notes.Add(note);

            if(toneIdx >= 0) _onceOctaveUp = false;
            _onceLenRatio = -1;
            if (_groupNoteStartIdx >= 0) _groupNoteNum++;
        }

        private void load(string filePath)
        {
            _defaultLength = 4;
            _onceOctaveUp = false;
            _curOctave = 4;
            _curBpm = 120;
            _lenRatio = 8;

            _maxP = double.MinValue;
            _minP = double.MaxValue;

            _groupNoteNum = 0;
            _groupNoteStartIdx = -1;

            _notes.Clear();
            _repeatStartIdx.Clear();

            using (TextReader reader = new StreamReader(filePath, Encoding.UTF8))
            {
                string line = null;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line == "") continue;

                    int idx = 0;
                    while (idx < line.Length)
                    {
                        char c = line[idx];
                        idx++;

                        switch (char.ToLower(c))
                        {
                            #region 音
                            case 'c': loadNote(0, line, ref idx); break;
                            case 'd': loadNote(2, line, ref idx); break;
                            case 'e': loadNote(4, line, ref idx); break;
                            case 'f': loadNote(5, line, ref idx); break;
                            case 'g': loadNote(7, line, ref idx); break;
                            case 'a': loadNote(9, line, ref idx); break;
                            case 'b': loadNote(11, line, ref idx); break;
                            case 'r': loadNote(-1, line, ref idx); break; // 休符
                            #endregion

                            case 'l': // デフォルト長さ
                                {
                                    int num = readNum(line, ref idx);
                                    if (num >= 0) _defaultLength = num;
                                }
                                break;

                            case '&': // タイ
                                break;

                            case 'u': // ピッチベンド
                                {
                                    int num = readNum(line, ref idx);
                                }
                                break;

                            #region 連符
                            case '{': // 連符開始
                                _groupNoteStartIdx = _notes.Count;
                                _groupNoteNum = 0;
                                break;
                            case '}': // 連符終了
                                {
                                    int num = readNum(line, ref idx);
                                    if (num < 0) num = _defaultLength;

                                    if (_groupNoteStartIdx >= 0)
                                    {
                                        double glen = 60 * 4 / (double)(_curBpm * _defaultLength * _groupNoteNum);
                                        for (int i = _groupNoteStartIdx; i < _notes.Count; i++)
                                        {
                                            Note nn = _notes[i].Copy();
                                            nn.Length = glen;
                                            _notes[i] = nn;
                                        }
                                    }

                                    _groupNoteStartIdx = -1;
                                }
                                break;
                            #endregion

                            case 'q': // 音の長さの割合 (n / 8)
                                {
                                    int num = readNum(line, ref idx);
                                    if (num >= 0) _lenRatio = num;
                                }
                                break;
                            case '\'': // 直後のみ長さの割合を変える
                                {
                                    int num = readNum(line, ref idx);
                                    if (num >= 0) _onceLenRatio = num;
                                }
                                break;

                            #region オクターブ
                            case 'o': // オクターブ指定
                                {
                                    int num = readNum(line, ref idx);
                                    if (num >= 0) _curOctave = num;
                                }
                                break;
                            case '>': // オクターブアップ
                                _curOctave++;
                                break;
                            case '<': // オクターブダウン
                                _curOctave--;
                                break;

                            case '^': // 直後のみオクターブアップ
                                _onceOctaveUp = true;
                                break;
                            #endregion

                            #region 音量、無視
                            case 'v': // 音量
                                {
                                    int num = readNum(line, ref idx);
                                }
                                break;
                            case ']': // 音量アップ
                                break;
                            case '[': // 音量ダウン
                                break;
                            case '!': // 直後のみ音量を変える
                                {
                                    int num = readNum(line, ref idx);
                                }
                                break;
                            #endregion

                            case 't': // テンポ
                                {
                                    int num = readNum(line, ref idx);
                                    if (num >= 0) _curBpm = num;
                                }
                                break;

                            #region 繰り返し
                            case '(': // 繰り返し開始
                                _repeatStartIdx.Push(_notes.Count);
                                break;
                            case ')': // 繰り返し終了
                                {
                                    int num = readNum(line, ref idx);
                                    if (num < 0) num = 2; // デフォルトリピート回数は2回
                                    if (_repeatStartIdx.Count > 0)
                                    {
                                        int startIdx = _repeatStartIdx.Pop();
                                        int endIdx = _notes.Count;
                                        for (int r = 1; r < num; r++)
                                        {
                                            for (int i = startIdx; i < endIdx; i++)
                                            {
                                                _notes.Add(_notes[i].Copy());
                                            }
                                        }
                                    }
                                }
                                break;
                            #endregion

                            case '@': // 音色、無視
                                {
                                    int num = readNum(line, ref idx);
                                }
                                break;
                        }
                    }
                }
            }
        }

        private int readNum(string str, ref int idx)
        {
            if (idx >= str.Length) return -1;

            int startIdx = idx;
            while (idx < str.Length)
            {
                if (char.IsNumber(str, idx))
                {
                    idx++;
                }
                else
                {
                    if (idx > startIdx)
                    {
                        string subStr = str.Substring(startIdx, idx - startIdx);
                        int num = 0;
                        if (!int.TryParse(subStr, out num)) num = -1;
                        return num;
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            if (idx > startIdx)
            {
                string subStr = str.Substring(startIdx, idx - startIdx);
                int num = 0;
                if (!int.TryParse(subStr, out num)) num = -1;
                return num;
            }
            else
            {
                return -1;
            }
        }

        public override IEnumerable<MusicNote> GetNotes()
        {
            foreach (MusicNote d in _points) yield return d;
        }
    }
}
