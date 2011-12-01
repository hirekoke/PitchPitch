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

        public override int GetHardness(uint chip)
        {
            if (chip == 0) return 0;
            return 1;
        }

        public override void Draw(Surface s, uint chip, Rectangle r)
        {
            if (chip < 0 || chip >= _chipSurfaces.Count) return;
            if (chip == 0) return;

            if (r.Width == _chipWidth && r.Height == _chipHeight)
            {
                Surface ss = _chipSurfaces[(int)chip];
                s.Blit(ss, r);
            }
            else
            {
                Color c = _avgColors[chip];
                s.Fill(r, c);
            }
        }

        public static ImageMapChipData LoadChipData(MapInfo info)
        {
            ImageMapChipData c = new ImageMapChipData();
            string path = System.IO.Path.Combine(info.DirPath, info.ChipFileName);
            c._chipSurfaces = ResourceManager.LoadSurfacesFromFile(path, info.ChipSize);
            c._avgColors = new Dictionary<uint, Color>();

            uint chip = 0;
            foreach (Surface s in c._chipSurfaces)
            {
                c._avgColors.Add(chip, ImageManager.GetAvgColor(s));
                chip++;
            }

            c._chipWidth = info.ChipSize.Width;
            c._chipHeight = info.ChipSize.Height;
            c._backChip = 0; c._wallChip = 1;
            return c;
        }

        public override void Dispose()
        {
            if (_chipSurfaces != null) foreach (Surface s in _chipSurfaces) s.Dispose();
        }
    }
}
