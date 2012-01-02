using System;
using System.Collections.Generic;
using System.Text;

namespace PitchPitch.map
{
    class EmptyMap : BinaryMap
    {
        internal class EmptyMapInfo : BuiltinMapInfo { }

        public static EmptyMapInfo GetMapInfo()
        {
            EmptyMapInfo mi = new EmptyMapInfo();
            mi.Level = 3;
            mi.MapName = Properties.Resources.MapName_PracticeMap;
            mi.PlayerVx = 0;
            return mi;
        }

        public EmptyMap()
        {
            _mapInfo = GetMapInfo();
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

        public override long Height
        {
            get
            {
                return _chipData.ChipHeight * _needRowNum;
            }
        }

        public override double GetDefaultY(double xInView)
        {
            return convertIdx2PY((int)(_needRowNum / 2.0));
        }
    }

    class EmptyFixedMap : EmptyMap
    {
        internal class EmptyFixedMapInfo : BuiltinMapInfo { }

        public static new EmptyFixedMapInfo GetMapInfo()
        {
            EmptyFixedMapInfo mi = new EmptyFixedMapInfo();
            mi.Level = 3;
            mi.MapName = Properties.Resources.MapName_PracticeFixedMap;
            mi.PlayerVx = 0;
            mi.PitchType = PitchType.Fixed;
            mi.MaxPitch = 700;
            mi.MinPitch = 180;
            return mi;
        }

        public EmptyFixedMap()
        {
            _mapInfo = GetMapInfo();
        }
    }
}
