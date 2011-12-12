using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace PitchPitch.map
{
    class RandomMap : BinaryMap
    {
        internal class RandomMapInfo : MapInfo { }

        public static RandomMapInfo GetMapInfo(int level)
        {
            RandomMapInfo mi = new RandomMapInfo();
            mi.Level = level;
            mi.MapName = Properties.Resources.MenuItem_RandomMap + "-" + level.ToString();
            return mi;
        }

        #region ランダム生成用
        protected double _holeRadMin = 2;
        protected double _holeRadMax = 5;
        protected double _holeRadGap = 3;
        protected double _holeGap = 3;
        #endregion
        
        public RandomMap(int level) : base()
        {
            _mapInfo = GetMapInfo(level);

            Color light, dark, strong;
            ImageUtil.GetRandomColors(out light, out dark, out strong);
            _mapInfo.BackgroundColor = light;
            _mapInfo.ForegroundColor = dark;
            _mapInfo.StrongColor = strong;

            _holeRadMax = 10 - _mapInfo.Level;
            _holeRadMin = 7 - _mapInfo.Level;
            _holeGap = 3 * _mapInfo.Level / 4.0;
            if (_holeGap < 1) _holeGap = 1;
            _holeRadGap = 3 * _mapInfo.Level / 4.0;
            if (_holeRadGap < 1) _holeRadGap = 1;

            _columnNum = 200;
        }

        public override void Init(PitchPitch parent, Size viewSize)
        {
            base.Init(parent, viewSize);

            _rowNum = _needRowNum;

            double holeY = (int)(_needRowNum / 2.0);
            double holeRad = _holeRadMax;

            Random _rand = new Random();
            for (int i = 0; i < _columnNum; i++)
            {
                // 穴の中心
                holeY += _holeGap * (_rand.NextDouble() - 0.5);
                if (holeY - holeRad < 0) holeY = holeRad;
                if (holeY + holeRad > _needRowNum) holeY = _needRowNum - holeRad;
                // 穴の大きさ
                holeRad += _holeRadGap * (_rand.NextDouble() - 0.5);
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
