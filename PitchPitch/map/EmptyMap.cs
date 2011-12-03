using System;
using System.Collections.Generic;
using System.Text;

namespace PitchPitch.map
{
    class EmptyMap : BinaryMap
    {
        public EmptyMap()
        {
            _mapInfo = new MapInfo();
            _mapInfo.Name = "練習用";
            _mapInfo.PlayerVx = 0;
        }

        public override bool HasEnd
        {
            get { return false; }
        }

        public override void Init(PitchPitch parent, System.Drawing.Size viewSize)
        {
            base.Init(parent, viewSize);

            _chips = new List<uint[]>();
            for (int i = 0; i < _needColumnNum; i++)
            {
                uint[] row = new uint[_needRowNum];
                for (int j = 0; j < row.Length; j++)
                {
                    row[j] = 0;
                }
                _chips.Add(row);
            }
        }
    }
}
