using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace PitchPitch
{
    struct PointD
    {
        public double X;
        public double Y;

        public PointD(double x, double y)
        {
            X = x; Y = y;
        }

        public static PointD Empty = new PointD(0, 0);

        public bool IsEmpty
        {
            get { return X == 0 && Y == 0; }
        }

        public void Offset(PointD p)
        {
            Offset(p.X, p.Y);
        }
        public void Offset(double x, double y)
        {
            X += x; Y += y;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", X, Y);
        }

        public Point Round()
        {
            Point p = new Point((int)X, (int)Y);
            return p;
        }
    }
}
