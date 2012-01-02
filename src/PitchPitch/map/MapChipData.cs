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
        public int XIdx;
        public int YIdx;
        public double ViewX;
        public double ViewY;
        public uint ChipData;
        public int Hardness;
    }

    enum ChipResizeMethod
    {
        Stretch,
        Tile,
    }

    abstract class MapChipData : IDisposable
    {
        protected int _chipWidth = 16;
        protected int _chipHeight = 16;
        public int ChipWidth { get { return _chipWidth; } set { _chipWidth = value; } }
        public int ChipHeight { get { return _chipHeight; } set { _chipHeight = value; } }

        protected uint _wallChip;
        public uint WallChip { get { return _wallChip; } }
        protected uint _backChip;
        public uint BackChip { get { return _backChip; } }

        protected int[] _hardness = null;
        public virtual int[] Hardness { get { return _hardness; } }

        public MapChipData() { }

        public abstract void Draw(Surface s, uint chip, Rectangle r, ChipResizeMethod resizeMethod);

        public abstract void Dispose();
    }
}
