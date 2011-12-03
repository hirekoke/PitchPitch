using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SdlDotNet.Graphics;
using SdlDotNet.Graphics.Primitives;

namespace PitchPitch.map
{
    class BinaryChipData : MapChipData
    {
        protected Color _foreColor = Color.Black;
        public Color ForeColor
        {
            get { return _foreColor; }
            set { _foreColor = value; }
        }

        public BinaryChipData()
        {
            _backChip = 0;
            _wallChip = 1;
            _hardness = new int[]{ 0, 1 };
        }

        public override void Draw(Surface s, uint chip, Rectangle r)
        {
            if (chip != 0)
            {
                s.Fill(r, _foreColor);
            }
        }

        public override void Dispose() { }
    }
}
