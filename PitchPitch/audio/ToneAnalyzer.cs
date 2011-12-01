using System;
using System.Collections.Generic;
using System.Text;

namespace PitchPitch.audio
{
    struct ToneResult
    {
        public string Tone;
        public int Octave;
        public int ToneIdx;
        public double Pitch;
        public double Clarity;
        public double PitchDiff;
        public static ToneResult Default
        {
            get
            {
                ToneResult result = new ToneResult();
                result.Tone = "A ";
                result.ToneIdx = 9;
                result.Octave = 4;
                result.Pitch = 440;
                result.Clarity = 0;
                result.PitchDiff = 0;
                return result;
            }
        }

        public ToneResult Copy()
        {
            ToneResult result = new ToneResult();
            result.Tone = Tone;
            result.ToneIdx = ToneIdx;
            result.Octave = Octave;
            result.Pitch = Pitch;
            result.Clarity = Clarity;
            result.PitchDiff = PitchDiff;
            return result;
        }
    }

    class ToneAnalyzer
    {
        private static string[] _toneNames = { "C ", "C#", "D ", "D#", "E ", "F ", "F#", "G ", "G#", "A ", "A#", "B " };
        public static string[] ToneNames
        {
            get { return _toneNames; }
        }

        private static double[] _cents = null;
        public static double[] Cents
        {
            get
            {
                if (_cents == null)
                {
                    _cents = new double[12];
                    for (int i = 0; i < 12; i++)
                        _cents[i] = Math.Pow(Math.Pow(2, i), 1 / 12.0);
                }
                return _cents;
            }
        }

        private static double[] _rangeCents = null;
        public static double[] RangeCents
        {
            get
            {
                if (_rangeCents == null)
                {
                    _rangeCents = new double[12];
                    for (int i = 0; i < 12; i++)
                        _rangeCents[i] = Math.Pow(Math.Pow(2, i + 0.5), 1 / 12.0);
                }
                return _rangeCents;
            }
        }

        private const double A4 = 440.0;

        public ToneAnalyzer()
        {
            // 初期化
        }

        public static ToneResult FromTone(string toneStr)
        {
            string toneName = toneStr.Substring(0, 2);
            string octStr = toneStr.Substring(2, 1);

            int toneIdx = Array.IndexOf(ToneAnalyzer.ToneNames, toneName);
            int oct = 0;
            if (!int.TryParse(octStr, out oct)) oct = 4;

            return FromTone(toneIdx, oct);
        }
        public static ToneResult FromTone(int toneIdx, int octave)
        {
            ToneResult ret = new ToneResult();
            ret.ToneIdx = toneIdx;
            ret.Tone = _toneNames[toneIdx];
            ret.Octave = octave;
            ret.Clarity = 1.0;

            double f4 = Cents[toneIdx] * A4 / Cents[9];
            if (ret.Octave > 4)
            {
                for (int i = ret.Octave; i > 4; i--)
                    f4 *= 2.0;
            }
            else if (ret.Octave < 4)
            {
                for (int i = ret.Octave; i < 4; i++)
                    f4 /= 2.0;
            }
            ret.Pitch = f4;
            ret.PitchDiff = 0;
            return ret;
        }

        public ToneResult Analyze(double freq, double clarity)
        {
            if (double.IsNaN(freq) || double.IsInfinity(freq)) return ToneResult.Default;

            int octave = 4;
            double ft = freq;
            if (freq < A4)
            {
                while (ft < A4)
                {
                    ft *= 2.0;
                    octave--;
                }
            }
            else if (freq > A4)
            {
                octave = 3;
                while (ft > A4)
                {
                    ft /= 2.0;
                    octave++;
                }
            }
            double a = Math.Pow(2, octave - 4) * A4;

            int aoffset;
            for (aoffset = 0; aoffset < 12; aoffset++)
            {
                double t = a * RangeCents[aoffset];
                if (freq <= t) break;
            }
            if (aoffset >= 3)
            {
                octave++;
                if(aoffset == 12) a *= 2;
            }
            aoffset %= 12;
            double baseTone = a * Cents[aoffset];

            ToneResult result = ToneResult.Default;
            result.ToneIdx = (9 + aoffset) % 12;
            result.Tone = _toneNames[result.ToneIdx];
            result.Octave = octave;
            result.PitchDiff = freq - baseTone;
            result.Pitch = freq;
            result.Clarity = clarity;
            return result;
        }
    }
}
