using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using SdlDotNet.Graphics;

namespace PitchPitch.map
{
    struct Chip
    {
        public double X;
        public double Y;
        public Point Idx;
        public PointD ViewPoint;
        public uint ChipData;
        public int Hardness;
    }

    abstract class MapChipData : IDisposable
    {
        protected int _chipWidth = 16;
        protected int _chipHeight = 16;
        public int ChipWidth { get { return _chipWidth; } }
        public int ChipHeight { get { return _chipHeight; } }

        protected uint _wallChip;
        public uint WallChip { get { return _wallChip; } }
        protected uint _backChip;
        public uint BackChip { get { return _backChip; } }

        protected int[] _hardness = null;
        public virtual int[] Hardness { get { return _hardness; } }

        public MapChipData() { }

        public virtual void Draw(Surface s, uint chip, Point p)
        {
            Draw(s, chip, new Rectangle(p.X, p.Y, _chipWidth, _chipHeight));
        }
        public abstract void Draw(Surface s, uint chip, Rectangle r);

        public abstract void Dispose();
    }
}
