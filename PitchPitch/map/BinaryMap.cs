using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SdlDotNet.Graphics;

namespace PitchPitch.map
{
    class BinaryMap : Map
    {
        public override Color ForeColor
        {
            get { return _foreColor; }
            set { _foreColor = value; (_chipData as BinaryChipData).ForeColor = _foreColor; }
        }

        public BinaryMap()
        {
            _chipData = new BinaryChipData();
        }

        public override void Init(PitchPitch parent, Size viewSize)
        {
            base.Init(parent, viewSize);

            _needRowNum = (int)Math.Ceiling(viewSize.Height / (double)_chipData.ChipHeight) + 1;
            _needColumnNum = (int)Math.Ceiling(viewSize.Width / (double)_chipData.ChipWidth) + 1;
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
