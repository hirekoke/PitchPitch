using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Xml;
using SdlDotNet.Graphics;

namespace PitchPitch.map
{
    class ImageMapChipData : MapChipData
    {
        private SurfaceCollection _chipSurfaces;
        private Dictionary<uint, Color> _avgColors;

        public ImageMapChipData()
        {
            _backChip = 0;
            _wallChip = 1;
        }

        public override void Draw(Surface s, uint chip, Rectangle r, ChipResizeMethod m)
        {
            if (chip < 0 || chip >= _chipSurfaces.Count) return;
            if (chip == 0) return;

            switch (m)
            {
                case ChipResizeMethod.Stretch:
                    Color c = _avgColors[chip];
                    s.Fill(r, c);
                    break;
                case ChipResizeMethod.Tile:
                    Surface ss = _chipSurfaces[(int)chip];
                    s.Blit(ss, r);
                    break;
            }
        }

        public static ImageMapChipData LoadChipData(MapInfo info)
        {
            ImageMapChipData c = new ImageMapChipData();
            string path = System.IO.Path.Combine(info.DirectoryPath, info.ChipDataInfo.FileName);
            c._chipSurfaces = ResourceManager.LoadSurfacesFromFile(path, info.ChipDataInfo.Size);
            c._avgColors = new Dictionary<uint, Color>();

            c._hardness = new int[c._chipSurfaces.Count];
            uint chip = 0;
            foreach (Surface s in c._chipSurfaces)
            {
                c._avgColors.Add(chip, ImageUtil.GetAvgColor(s));

                if (info.ChipDataInfo == null || chip > info.ChipDataInfo.ChipInfos.Count - 1)
                {
                    c._hardness[chip] = chip == 0 ? 0 : 1;
                }
                else
                {
                    c._hardness[chip] = info.ChipDataInfo.ChipInfos[(int)chip].Hardness;
                }

                chip++;
            }

            c._chipWidth = info.ChipDataInfo.Size.Width;
            c._chipHeight = info.ChipDataInfo.Size.Height;
            c._backChip = 0; c._wallChip = 1;
            return c;
        }

        public override void Dispose()
        {
            if (_chipSurfaces != null) foreach (Surface s in _chipSurfaces) s.Dispose();
        }
    }
}
