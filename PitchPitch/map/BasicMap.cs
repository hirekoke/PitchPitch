using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SdlDotNet.Graphics;

namespace PitchPitch.map
{
    class BasicMap : Map
    {
        public override void Init(PitchPitch parent, Size viewSize)
        {
            base.Init(parent, viewSize);

            _needRowNum = (int)Math.Ceiling(viewSize.Height / (double)_chipData.ChipHeight) + 1;
            _needColumnNum = (int)Math.Ceiling(viewSize.Width / (double)_chipData.ChipWidth) + 1;
            _columnNum = 200;
        }

        public override void Dispose()
        {
            if (_chipData != null) _chipData.Dispose();
            base.Dispose();
        }
    }
}
