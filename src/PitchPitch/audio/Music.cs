using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PitchPitch.audio
{
    abstract class Music
    {
        public static readonly Color HoleColor = Color.White;
        public static readonly Color WallColor = Color.Black;
        public static readonly Color CenterColor = Color.Red;
        public static Bitmap GetMappingBmp()
        {
            Bitmap bmp = new Bitmap(3, 1, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            bmp.SetPixel(0, 0, HoleColor);
            bmp.SetPixel(1, 0, WallColor);
            bmp.SetPixel(2, 0, CenterColor);
            return bmp;
        }

        #region 内部クラス定義
        private enum EdgeType
        {
            On,
            Off,
            Dip,
        }
        private class Edge
        {
            private EdgePoint _sp = null;
            private EdgePoint _ep = null;
            public EdgePoint StartPoint { get { return _sp; } set { _sp = value; if (_sp != null) _sp.Edges.Add(this); } }
            public EdgePoint EndPoint { get { return _ep; } set { _ep = value; if (_ep != null) _ep.Edges.Add(this); } }
            public EdgeType Type = EdgeType.On;
            public Edge(EdgePoint sp, EdgePoint ep, EdgeType type)
            {
                StartPoint = sp; EndPoint = ep; Type = type;
            }
        }
        private class EdgePoint
        {
            public Point Point = Point.Empty;
            public List<Edge> Edges = new List<Edge>();
            public EdgePoint(Point p) { Point = p; }
            public EdgePoint(int x, int y) : this(new Point(x, y)) { }
            public EdgePoint(double x, double y) : this((int)x, (int)y) { }
        }
        #endregion

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

        private int _width = 0;
        private int _height = 0;
        private int _chipWidth = 0;
        private int _chipHeight = 0;
        private double _fps = 0;
        private double _vx = 0;

        protected double getY(double pitch)
        {
            double pitchLog = Math.Log(pitch);
            return _height - _height * (pitchLog - _minPitchLog) / (double)(_maxPitchLog - _minPitchLog);
        }
        protected double getX(double time)
        {
            return time * _fps * _vx / (double)_chipWidth;
        }

        private List<Edge> getEdges()
        {
            List<Edge> edges = new List<Edge>();
            Dictionary<double, Edge> pitchEdge = new Dictionary<double, Edge>();
            Dictionary<double, int> refCnts = new Dictionary<double, int>();
            Dictionary<double, List<double>> refs = new Dictionary<double, List<double>>();

            foreach (MusicNote note in GetNotes())
            {
                List<double> keys = new List<double>(refCnts.Keys);

                if (note.Start)
                {
                    if (refs.ContainsKey(note.RoundedPitch))
                        refs[note.RoundedPitch] = keys;
                    else
                        refs.Add(note.RoundedPitch, keys);
                    if (keys.Count > 0)
                    {
                        foreach (double d in keys)
                        {
                            if (refCnts.ContainsKey(d)) refCnts[d]++;
                            else refCnts.Add(d, 1);
                        }
                    }

                    double sx0 = getX(note.TimeInSec);
                    double sy0 = getY(note.RoundedPitch);
                    Edge se = new Edge(new EdgePoint(sx0, sy0), null, EdgeType.On);

                    #region 線を引く
                    if (keys.Count > 0)
                    {
                        foreach (double d in refs[note.RoundedPitch])
                        {
                            if (pitchEdge.ContainsKey(d))
                            {
                                Edge oe = pitchEdge[d];
                                EdgePoint prevEndP = oe.EndPoint;

                                int x = se.StartPoint.Point.X; int y = prevEndP.Point.Y;
                                EdgePoint newP = new EdgePoint(x, y);

                                #region 横線(off)
                                Edge fe = prevEndP.Edges.Find((e) =>
                                {
                                    return (
                                        e.StartPoint == prevEndP &&
                                        e.EndPoint.Point.X == x &&
                                        e.EndPoint.Point.Y == y &&
                                        e.Type == EdgeType.Off);
                                });
                                if (fe == null)
                                {
                                    Edge offe = new Edge(prevEndP, newP, EdgeType.Off);
                                    edges.Add(offe);
                                }
                                else if (fe.EndPoint.Point.X < x)
                                {
                                    fe.EndPoint.Point.X = x;
                                }
                                #endregion

                                #region 縦線
                                if (newP.Point.Y != se.StartPoint.Point.Y)
                                {
                                    Edge de = new Edge(newP, se.StartPoint, EdgeType.Dip);
                                    edges.Add(de);
                                }
                                #endregion
                            }
                        }
                    }
                    else if (note.TimeInSec > 0)
                    {
                        #region 開始までの線
                        double x0 = 0;
                        double y0 = getY(note.RoundedPitch);
                        double x1 = getX(note.TimeInSec);
                        double y1 = y0;
                        edges.Add(new Edge(new EdgePoint(x0, y0), new EdgePoint(x1, y1), EdgeType.Off));
                        #endregion
                    }
                    #endregion

                    if (pitchEdge.ContainsKey(note.RoundedPitch)) pitchEdge[note.RoundedPitch] = se;
                    else pitchEdge.Add(note.RoundedPitch, se);
                }
                else
                {
                    if (refs.ContainsKey(note.RoundedPitch))
                    {
                        foreach (double d in refs[note.RoundedPitch])
                        {
                            if (refCnts.ContainsKey(d))
                            {
                                refCnts[d]--;
                                if (refCnts[d] == 0) refCnts.Remove(d);
                            }
                        }
                        refs.Remove(note.RoundedPitch);
                    }

                    if (!refCnts.ContainsKey(note.RoundedPitch)) refCnts.Add(note.RoundedPitch, 0);
                    else refCnts[note.RoundedPitch]++;

                    #region 線を引く
                    #region 横線(on)
                    if (pitchEdge.ContainsKey(note.RoundedPitch))
                    {
                        double x = getX(note.TimeInSec);
                        double y = getY(note.RoundedPitch); ;
                        Edge e = pitchEdge[note.RoundedPitch];
                        e.EndPoint = new EdgePoint(x, y);

                        edges.Add(e);
                    }
                    #endregion
                    #endregion
                }
            }

            #region 終了後の横線
            if (refCnts.Count > 0)
            {
                foreach (double d in refCnts.Keys)
                {
                    if (pitchEdge.ContainsKey(d))
                    {
                        double x = getX(Length);
                        double y = getY(d);
                        Edge e = pitchEdge[d];
                        if (e.EndPoint == null)
                        {
                            e.EndPoint = new EdgePoint(x, y);
                            edges.Add(e);
                        }
                    }
                }
            }
            #endregion

            pitchEdge.Clear(); pitchEdge = null;
            refCnts.Clear(); refCnts = null;
            refs.Clear(); refs = null;

            return edges;
        }

        private void drawPoint(Graphics g, Brush br, Point p, int xrad, int yrad)
        {
            g.FillPolygon(br, new Point[] {
                new Point(p.X - xrad, p.Y),
                new Point(p.X, p.Y - yrad),
                new Point(p.X + yrad + 1, p.Y),
                new Point(p.X, p.Y + yrad + 1)
            });
        }
        private Bitmap drawEdges(List<Edge> edges, bool drawCenter)
        {
            int minrad = (int)Math.Ceiling(Program.PitchPitch.Player.MaxRadius * 0.5 / (double)_chipHeight);
            int maxrad = (int)Math.Ceiling(Program.PitchPitch.Player.MaxRadius * 1.6 / (double)_chipHeight);
            if(minrad == 0) minrad = 1;

            double coef = _vx;

            Brush onBr = new SolidBrush(HoleColor);
            Brush offBr = new SolidBrush(HoleColor);
            Brush dipBr = new SolidBrush(HoleColor);
            Pen centerPen = new Pen(CenterColor, 1);
            centerPen.SetLineCap(LineCap.Flat, LineCap.Flat, DashCap.Flat);

            Bitmap bmp = new Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.Clear(WallColor);

                #region hole
                foreach (Edge e in edges)
                {
                    Point sp = e.StartPoint.Point;
                    Point ep = e.EndPoint.Point;
                    switch (e.Type)
                    {
                        case EdgeType.On:
                            #region On
                            drawPoint(g, onBr, sp, minrad, maxrad);
                            drawPoint(g, onBr, ep, minrad, maxrad);
                            g.FillRectangle(onBr, sp.X, sp.Y - minrad, ep.X - sp.X, ep.Y - sp.Y + minrad * 2 + 2);
                            #endregion
                            break;

                        case EdgeType.Off:
                            #region Off
                            drawPoint(g, offBr, sp, minrad, maxrad);
                            drawPoint(g, offBr, ep, minrad, maxrad);
                            g.FillRectangle(offBr, sp.X, sp.Y - maxrad, ep.X - sp.X, ep.Y - sp.Y + maxrad * 2 + 2);
                            #endregion
                            break;

                        case EdgeType.Dip:
                            #region Dip
                            double w = Math.Abs(sp.Y - ep.Y) * coef;
                            int xend = (int)(sp.X + w);
                            bool toolong = false; int sx = 0;
                            Edge endEdge = e.EndPoint.Edges.Find((fe) => { return fe.StartPoint == e.EndPoint; });
                            if (endEdge != null)
                            {
                                sx = endEdge.EndPoint.Point.X;
                                if (sx < xend)
                                {
                                    toolong = true;
                                    int tmp = (int)(sp.X + w * 0.4);
                                    if(sx < tmp) sx = tmp;
                                }
                            }

                            List<Point> points = new List<Point>();
                            points.Add(new Point(sp.X + minrad, sp.Y));
                            points.Add(new Point(sp.X - minrad, sp.Y));
                            points.Add(new Point(ep.X - minrad, ep.Y));
                            if (toolong) // 横にはみ出るので台形にする
                            {
                                points.Add(new Point(sx, ep.Y));
                                if(xend != sp.X)
                                    points.Add(new Point(sx, (int)(ep.Y + (sp.Y - ep.Y) * (xend - sx) / w)));
                            }
                            else // 三角形にする
                            {
                                points.Add(new Point(xend, ep.Y));
                            }
                            g.FillPolygon(dipBr, points.ToArray());
                            #endregion
                            break;
                    }
                }
                #endregion

                if (drawCenter)
                {
                    #region center line
                    foreach (Edge e in edges)
                    {
                        Point sp = e.StartPoint.Point;
                        Point ep = e.EndPoint.Point;
                        switch (e.Type)
                        {
                            case EdgeType.On:
                                g.DrawLine(centerPen, sp, ep);
                                break;
                            case EdgeType.Off:
                            case EdgeType.Dip:
                                break;
                        }
                    }
                    #endregion
                }
            }

            return bmp;
        }

        public Bitmap GetMap(double fps, double vx, int chipWidth, int chipHeight, bool drawCenter)
        {
            _fps = fps; _vx = vx;
            _chipWidth = chipWidth; _chipHeight = chipHeight;
            _width = (int)(_length * fps * vx / (double)chipWidth);
            _height = (int)(Constants.StageViewHeight / (double)chipHeight);

            List<Edge> edges = getEdges();
            return drawEdges(edges, drawCenter);
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
        private double _p;
        public double Pitch { set { _p = value; _rp = Math.Round(_p, 4); } get { return _p; } }
        private double _rp;
        public double RoundedPitch { get { return _rp; } }
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
