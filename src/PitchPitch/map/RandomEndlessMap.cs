using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SdlDotNet.Graphics;

namespace PitchPitch.map
{
    class RandomEndlessMap : RandomMap
    {
        internal class RandomEndlessMapInfo : BuiltinMapInfo { }

        public static new RandomEndlessMapInfo GetMapInfo(int level)
        {
            RandomEndlessMapInfo mi = new RandomEndlessMapInfo();
            mi.Level = level;
            mi.MapName = Properties.Resources.MapName_EndlessMap;
            mi.HasEnd = false;
            return mi;
        }

        protected int _lastColumnIndex = 0;

        #region ランダム生成用
        private Random _rand;
        protected double _holeY = 0;
        protected double _holeRad = 0;
        #endregion

        public RandomEndlessMap(int level) : base(level)
        {
            _rand = new Random();

            _mapInfo = GetMapInfo(level);

            Color light, dark, strong;
            ImageUtil.GetRandomColors(out light, out dark, out strong);
            _mapInfo.BackgroundColor = light;
            _mapInfo.ForegroundColor = dark;
            _mapInfo.StrongColor = strong;
        }

        public override bool HasEnd { get { return false; } }

        public override void Init(PitchPitch parent, Size viewSize)
        {
            base.Init(parent, viewSize);

            _rowNum = _needRowNum;
            _holeY = _rowNum / 2.0;
            _holeRad = _holeRadMax;
            _chips = new List<uint[]>();
            _lastColumnIndex = 0;
        }

        private void initChips()
        {
            // ぎりぎりしか作っていない
            if (_lastColumnIndex < _xLastIdx)
            {
                for (int i = _lastColumnIndex; i < _xLastIdx; i++)
                {
                    // 穴の中心
                    _holeY += _holeGap * (_rand.NextDouble() - 0.5);
                    if (_holeY - _holeRad < 0) _holeY = _holeRad;
                    if (_holeY + _holeRad > _needRowNum) _holeY = _needRowNum - _holeRad;
                    // 穴の大きさ
                    _holeRad += _holeRadGap * (_rand.NextDouble() - 0.5);
                    if (_holeRad > _holeRadMax) _holeRad = _holeRadMax;
                    if (_holeRad < _holeRadMin) _holeRad = _holeRadMin;

                    uint[] tmp = new uint[_needRowNum];
                    for (int j = 0; j < _needRowNum; j++)
                        tmp[j] = (j < _holeY - _holeRad || j > _holeY + _holeRad) ? (uint)1 : (uint)0;
                    _chips.Add(tmp);
                    _lastColumnIndex++;
                }
                if (_chips.Count > _needColumnNum)
                {
                    _chips.RemoveRange(0, _chips.Count - _needColumnNum);
                }
            }
        }

        public override IEnumerable<Chip> EnumViewChipData()
        {
            initChips();

            double[] vpxs = new double[_xLastIdx - _xFirstIdx];
            double[] vpys = new double[_yLastIdx - _yFirstIdx];
            double[] ppxs = new double[_xLastIdx - _xFirstIdx];
            double[] ppys = new double[_yLastIdx - _yFirstIdx];

            int lidx = _xFirstIdx - (_lastColumnIndex - _chips.Count);
            int rlen = _chips.Count > 0 ? _chips[0].Length : 0;
            for (int i = _xFirstIdx, s = 0; i < _xLastIdx; i++, lidx++, s++)
            {
                if (lidx < 0) continue;
                vpxs[s] = convertIdx2VX(i);
                ppxs[s] = vpxs[s] + view.X;
            }
            for (int j = _yFirstIdx, t = 0; j < _yLastIdx && j < rlen; j++, t++)
            {
                if (j < 0) continue;
                vpys[t] = convertIdx2VY(j);
                ppys[t] = vpys[t] + view.Y;
            }

            lidx = _xFirstIdx - (_lastColumnIndex - _chips.Count);
            for (int i = _xFirstIdx, s=0; i < _xLastIdx; i++, lidx++, s++)
            {
                if (lidx < 0) continue;
                for (int j = _yFirstIdx,t=0; j < _yLastIdx && j < _chips[lidx].Length; j++,t++)
                {
                    if (j < 0) continue;
                    Chip cd = new Chip();
                    cd.XIdx = lidx; cd.YIdx = j;
                    cd.ViewX = vpxs[s];
                    cd.ViewY = vpys[t];
                    cd.ChipData = _chips[lidx][j];
                    if (cd.ChipData >= 0 && cd.ChipData < _chipData.Hardness.Length)
                        cd.Hardness = _chipData.Hardness[cd.ChipData];
                    else
                        cd.Hardness = cd.ChipData == 0 ? 0 : 1;
                    yield return cd;
                }
            }
        }

        protected override void renderMiniMapBackground(Surface s, Rectangle r)
        {
            s.Fill(r, _foreColor);
        }
        protected override void renderMiniMapForeground(SdlDotNet.Graphics.Surface s, Rectangle r)
        {
            using (Surface ts = ResourceManager.LargePFont.Render("Endless Map", _backColor))
            {
                s.Blit(ts, new Point((int)(r.Width / 2.0 - ts.Width / 2.0), (int)(r.Height / 2.0 - ts.Height / 2.0)));
            }
        }
        protected override void renderMiniMapViewBox(SdlDotNet.Graphics.Surface s, Rectangle r) { }

        public override double GetDefaultY(double xInView)
        {
            int xidx = convertV2IdxX(xInView) - (_lastColumnIndex - _chips.Count);
            if (xidx >= 0 && xidx < _chips.Count)
            {
                uint[] row = _chips[xidx];
                uint prev = row[0]; int inIdx = 0; int outIdx = row.Length - 1;
                for (int i = 0; i < row.Length; i++)
                {
                    uint r = row[i];
                    if (prev > 0 && r == 0)
                    {
                        // 穴に入った
                        inIdx = i;
                    }
                    else if (prev == 0 && r > 0)
                    {
                        // 穴から出た
                        outIdx = i - 1;
                        break;
                    }
                    prev = r;
                }
                double idx = (outIdx + inIdx) / 2.0;
                return convertIdx2PY((int)idx);
            }
            else
            {
                return -1;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
