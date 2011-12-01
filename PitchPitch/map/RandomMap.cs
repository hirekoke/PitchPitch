using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace PitchPitch.map
{
    class RandomMap : BinaryMap
    {
        #region ランダム生成用
        private Random _rand;
        protected int _holeRadMin = 2;
        protected int _holeRadMax = 5;
        #endregion
        
        public RandomMap() : base()
        {
            _rand = new Random();
            
            _mapInfo = new MapInfo();
            _mapInfo.Name = "Random";

            _columnNum = 200;
        }

        public override void Init(PitchPitch parent, Size viewSize)
        {
            base.Init(parent, viewSize);

            _rowNum = _needRowNum;
            _mapInfo.Size = new Size(_chipData.ChipWidth * _columnNum, _chipData.ChipHeight * _rowNum);

            int holeY = (int)(_needRowNum / 2.0);
            int holeRad = _holeRadMax;

            for (int i = 0; i < _columnNum; i++)
            {
                // 穴の中心
                holeY += (int)(3 * (_rand.NextDouble() - 0.5));
                if (holeY - holeRad < 0) holeY = holeRad;
                if (holeY + holeRad > _needRowNum) holeY = _needRowNum - holeRad;
                // 穴の大きさ
                holeRad += (int)(3 * (_rand.NextDouble() - 0.5));
                if (holeRad > _holeRadMax) holeRad = _holeRadMax;
                if (holeRad < _holeRadMin) holeRad = _holeRadMin;

                uint[] tmp = new uint[_needRowNum];
                for (int j = 0; j < _needRowNum; j++)
                    tmp[j] = (j < holeY - holeRad || j > holeY + holeRad) ? (uint)1 : (uint)0;
                _chips.Add(tmp);
            }
        }
    }
}
