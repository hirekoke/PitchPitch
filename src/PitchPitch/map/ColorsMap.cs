using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SdlDotNet.Graphics;

namespace PitchPitch.map
{
    class ColorsMap : Map
    {
        public ColorsMap()
        {
            _chipData = new ColorChipData();
        }

        protected Dictionary<uint, List<RectangleF>> _viewRects = new Dictionary<uint, List<RectangleF>>();

        public override void SetView(View view)
        {
            base.SetView(view);
            updateViewChipData();
        }

        protected override void renderForeground(Surface s, Rectangle r)
        {
            foreach (uint chip in enumUsingChips())
            {
                if (chip != 0)
                {
                    foreach (RectangleF rect in enumChipRect(chip))
                    {
                        _chipData.Draw(s, chip, 
                            new Rectangle((int)(r.X + rect.X), (int)(r.Y + rect.Y), (int)(rect.Width), (int)(rect.Height)),
                            ChipResizeMethod.Tile);
                    }
                }
            }
        }

        protected virtual void updateViewChipData()
        {
            _viewRects.Clear();

            uint prevChip = 0; int prevXIdx = int.MinValue;
            RectangleF r = RectangleF.Empty;

            int idx = 0;
            foreach (Chip cd in EnumViewChipData())
            {
                if (idx == 0)
                {
                    r = new RectangleF((float)cd.ViewX, (float)cd.ViewY, _chipData.ChipWidth, _chipData.ChipHeight);
                }
                else
                {
                    if (prevXIdx != cd.XIdx)
                    {
                        if (!r.IsEmpty)
                        {
                            if (!_viewRects.ContainsKey(prevChip)) _viewRects.Add(prevChip, new List<RectangleF>());
                            _viewRects[prevChip].Add(r);
                        }
                        r = new RectangleF((float)cd.ViewX, (float)cd.ViewY, _chipData.ChipWidth, _chipData.ChipHeight);
                    }
                    else
                    {
                        if (prevChip == cd.ChipData)
                        {
                            r.Height += _chipData.ChipHeight;
                        }
                        else
                        {
                            if (!r.IsEmpty)
                            {
                                if (!_viewRects.ContainsKey(prevChip)) _viewRects.Add(prevChip, new List<RectangleF>());
                                _viewRects[prevChip].Add(r);
                            }
                            r = new RectangleF((float)cd.ViewX, (float)cd.ViewY, _chipData.ChipWidth, _chipData.ChipHeight);
                        }
                    }
                }
                prevChip = cd.ChipData;
                prevXIdx = cd.XIdx;
                idx++;
            }

            if (!r.IsEmpty)
            {
                if (!_viewRects.ContainsKey(prevChip)) _viewRects.Add(prevChip, new List<RectangleF>());
                _viewRects[prevChip].Add(r);
            }
        }

        private IEnumerable<uint> enumUsingChips()
        {
            foreach (uint k in _viewRects.Keys)
            {
                yield return k;
            }
        }
        private IEnumerable<RectangleF> enumChipRect(uint chip)
        {
            if (_viewRects.ContainsKey(chip))
            {
                foreach (RectangleF r in _viewRects[chip])
                {
                    yield return r;
                }
            }
            else
            {
                yield break;
            }
        }
    }
}
