using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SdlDotNet.Graphics;

namespace PitchPitch.map
{
    class BinaryMap : ColorsMap
    {
        public BinaryMap()
        {
            _chipData = new BinaryChipData();
        }

        public override Color ForeColor
        {
            get { return _foreColor; }
            set { _foreColor = value; (_chipData as BinaryChipData).ForeColor = _foreColor; }
        }
    }
}
