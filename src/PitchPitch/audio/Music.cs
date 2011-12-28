using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PitchPitch.audio
{
    abstract class Music
    {
        private double _maxPitch;
        public double MaxPitch { get { return _maxPitch; } set { _maxPitch = value; _maxPitchLog = Math.Log(_maxPitch); } }
        private double _minPitch;
        public double MinPitch { get { return _minPitch; } set { _minPitch = value; _minPitchLog = Math.Log(_minPitch); } }

        private double _maxPitchLog = 1;
        private double _minPitchLog = 0;

        protected double _length;
        public double Length { get { return _length; } set { _length = value; } }

        public abstract void Load(string filePath);
        public abstract IEnumerable<MusicNote> GetNotes();

        protected double getY(double pitch, int height, int chipHeight)
        {
            double pitchLog = Math.Log(pitch);
            return height - height * (pitchLog - _minPitchLog) / (double)(_maxPitchLog - _minPitchLog);
        }

        protected double getX(double time, int width, double fps, double vx, int chipWidth)
        {
            return time * fps * vx / (double)chipWidth;
        }

        public Bitmap GetMap(double fps, double vx, int chipWidth, int chipHeight)
        {
            int width = (int)(_length * fps * vx / (double)chipWidth);
            int height = (int)(Constants.StageViewHeight / (double)chipHeight);

            Dictionary<double, int> refCnts = new Dictionary<double, int>();
            Dictionary<double, List<double>> refs = new Dictionary<double, List<double>>();
            Dictionary<double, double> offX = new Dictionary<double, double>();

            int minrad = (int)Math.Ceiling(Program.PitchPitch.Player.MinRadius * 3.0 / (double)chipHeight);
            int maxrad = (int)Math.Ceiling(Program.PitchPitch.Player.MaxRadius * 1.5 / (double)chipHeight);
            int pminw = minrad * 2;
            int pmaxw = maxrad * 2;
            int pmidw = maxrad;
            double coef = 1.2 * vx;

            Pen offPen = new Pen(Color.White, pmaxw);
            Pen onPen = new Pen(Color.White, pminw);
            Pen dipPen = new Pen(Color.White, pmidw);

            offPen.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);
            onPen.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);
            dipPen.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);

            Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.Clear(Color.Black);

                foreach (MusicNote note in GetNotes())
                {
                    List<double> keys = new List<double>(refCnts.Keys);

                    if (note.Start)
                    {
                        if (!refCnts.ContainsKey(note.Pitch)) refCnts.Add(note.Pitch, 0);
                        refCnts[note.Pitch]++;
                        if (refs.ContainsKey(note.Pitch))
                        {
                            refs[note.Pitch] = keys;
                        }
                        else
                        {
                            refs.Add(note.Pitch, keys);
                        }

                        if (keys.Count > 0)
                        {
                            foreach (double d in refs[note.Pitch])
                            {
                                // 横線(off)引く
                                if (offX.ContainsKey(d))
                                {
                                    if (note.TimeInSec > offX[d])
                                    {
                                        double x0 = getX(offX[d], width, fps, vx, chipWidth);
                                        double y0 = getY(d, height, chipHeight);
                                        double x1 = getX(note.TimeInSec, width, fps, vx, chipWidth);
                                        double y1 = y0;
                                        g.DrawLine(offPen, new Point((int)x0, (int)y0), new Point((int)x1 + 1, (int)y1));
                                    }
                                }
                                // 縦線引く
                                if (d != note.Pitch)
                                {
                                    // 前の音
                                    double x0 = getX(note.TimeInSec, width, fps, vx, chipWidth);
                                    double y0 = getY(d, height, chipHeight);
                                    // 次の音
                                    double x1 = x0;
                                    double y1 = getY(note.Pitch, height, chipHeight);

                                    if (y0 < y1)
                                    {
                                        g.FillPolygon(dipPen.Brush, new Point[] {
                                            new Point((int)(x0 - minrad), (int)y0),
                                            new Point((int)(x0 + pmidw), (int)y0),
                                            new Point((int)(x1 + (y1 - y0) * coef), (int)y1),
                                            new Point((int)(x1 - minrad), (int)y1)
                                        });
                                    }
                                    else
                                    {
                                        g.FillPolygon(dipPen.Brush, new Point[] {
                                            new Point((int)(x1 - minrad), (int)y1),
                                            new Point((int)(x1 + (y0 - y1) * coef), (int)y1),
                                            new Point((int)(x0 + pmidw), (int)y0),
                                            new Point((int)(x0 - minrad), (int)y0)
                                        });
                                    }
                                }
                            }
                        }
                        else if (note.TimeInSec > 0)
                        {
                            // 開始までの線
                            double x0 = 0;
                            double y0 = getY(note.Pitch, height, chipHeight);
                            double x1 = getX(note.TimeInSec, width, fps, vx, chipWidth);
                            double y1 = y0;
                            g.DrawLine(offPen, new Point((int)x0, (int)y0), new Point((int)x1, (int)y1));
                        }

                        if (offX.ContainsKey(note.Pitch)) offX[note.Pitch] = note.TimeInSec;
                        else offX.Add(note.Pitch, note.TimeInSec);
                    }
                    else
                    {
                        if (refs.ContainsKey(note.Pitch))
                        {
                            foreach (double d in refs[note.Pitch])
                            {
                                if (refCnts.ContainsKey(d))
                                {
                                    refCnts[d]--;
                                    if (refCnts[d] == 0) refCnts.Remove(d);
                                }
                            }
                            refs.Remove(note.Pitch);
                        }

                        if (offX.ContainsKey(note.Pitch))
                        {
                            // 横線(on)引く
                            double x0 = getX(offX[note.Pitch], width, fps, vx, chipWidth);
                            double y0 = getY(note.Pitch, height, chipHeight);
                            double x1 = getX(note.TimeInSec, width, fps, vx, chipWidth);
                            double y1 = y0;
                            g.DrawLine(onPen, new Point((int)x0, (int)y0), new Point((int)x1, (int)y1));

                            int x2 = (int)(x0 + pmaxw); if (x2 > x1) x2 = (int)x1;
                            g.DrawLine(offPen, new Point((int)x0, (int)y0), new Point((int)x2, (int)y0));

                            g.FillRectangle(Brushes.Black, (int)(x1 + maxrad), 0, width - (int)(x1 + maxrad), height);
                        }

                        if (offX.ContainsKey(note.Pitch)) offX[note.Pitch] = note.TimeInSec;
                        else offX.Add(note.Pitch, note.TimeInSec);
                    }
                }

                // 終了後の横線を引く
                if (refCnts.Count > 0)
                {
                    foreach (double d in refCnts.Keys)
                    {
                        if (offX.ContainsKey(d))
                        {
                            double x0 = getX(offX[d], width, fps, vx, chipWidth);
                            double y0 = getY(d, height, chipHeight);
                            double x1 = getX(Length, width, fps, vx, chipWidth);
                            double y1 = y0;
                            g.DrawLine(offPen, new Point((int)x0, (int)y0), new Point((int)x1, (int)y1));
                        }
                    }
                }
            }

            return bmp;
        }

        public static Music LoadMusic(string filePath)
        {
            double fps = SdlDotNet.Core.Events.TargetFps;
            string ext = System.IO.Path.GetExtension(filePath);
            switch (ext)
            {
                case ".mid":
                    {
                        SMF.SMFMusic music = new SMF.SMFMusic();
                        music.Load(filePath);
                        return music;
                    }
                case ".mml":
                    {
                        MML.MMLMusic music = new MML.MMLMusic();
                        music.Load(filePath);
                        return music;
                    }
                default:
                    return null;
            }
        }
    }

    class MusicNote
    {
        public double TimeInSec;
        public double Pitch;
        public bool Start;
        
        public MusicNote Copy()
        {
            MusicNote ret = new MusicNote();
            ret.TimeInSec = TimeInSec;
            ret.Pitch = Pitch;
            ret.Start = Start;
            return ret;
        }
    }
}
