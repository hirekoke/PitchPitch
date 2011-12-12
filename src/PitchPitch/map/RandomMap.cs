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
        private Random _rand;
        protected int _holeRadMin = 2;
        protected int _holeRadMax = 5;
        #endregion
        
        public RandomMap(int level) : base()
        {
            _rand = new Random();

            _mapInfo = GetMapInfo(level);

            Color light, dark, strong;
            ImageUtil.GetRandomColors(out light, out dark, out strong);
            _mapInfo.BackgroundColor = light;
            _mapInfo.ForegroundColor = dark;
            _mapInfo.StrongColor = strong;

            _holeRadMax = 9 - _mapInfo.Level;
            _holeRadMin = 6 - _mapInfo.Level;

            _columnNum = 200;
        }

        public override void Init(PitchPitch parent, Size viewSize)
        {
            base.Init(parent, viewSize);

            _rowNum = _needRowNum;

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
