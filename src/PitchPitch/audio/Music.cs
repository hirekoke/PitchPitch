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

            Pen offPen = new Pen(Color.White, 5);
            Pen onPen = new Pen(Color.White, 3);
            Pen dipPen = new Pen(Color.White, 4);

            offPen.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Flat);
            onPen.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Flat);
            dipPen.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Flat);

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
                                // 横線引く
                                if (offX.ContainsKey(d))
                                {
                                    if (note.TimeInSec > offX[d])
                                    {
                                        double x0 = getX(offX[d], width, fps, vx, chipWidth);
                                        double y0 = getY(d, height, chipHeight);
                                        double x1 = getX(note.TimeInSec, width, fps, vx, chipWidth);
                                        double y1 = y0;
                                        g.DrawLine(offPen, new Point((int)x0, (int)y0), new Point((int)x1, (int)y1));
                                    }
                                }
                                // 縦線引く
                                if (d != note.Pitch)
                                {
                                    double x0 = getX(note.TimeInSec, width, fps, vx, chipWidth);
                                    double y0 = getY(d, height, chipHeight);
                                    double x1 = x0;
                                    double y1 = getY(note.Pitch, height, chipHeight);
                                    g.DrawLine(dipPen, new Point((int)x0, (int)y0), new Point((int)x1, (int)y1));
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
                            double x0 = getX(offX[note.Pitch], width, fps, vx, chipWidth);
                            double y0 = getY(note.Pitch, height, chipHeight);
                            double x1 = getX(note.TimeInSec, width, fps, vx, chipWidth);
                            double y1 = y0;
                            g.DrawLine(onPen, new Point((int)x0, (int)y0), new Point((int)x1, (int)y1));
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
