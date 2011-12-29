using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SdlDotNet.Graphics;
using SdlDotNet.Graphics.Primitives;

namespace PitchPitch.map
{
    class ColorChipData : MapChipData
    {
        protected Color[] _colors = null;

        public ColorChipData()
        {
            _backChip = 0;
            _wallChip = 1;
        }

        public override void Draw(Surface s, uint chip, Rectangle r)
        {
            if (chip != 0)
            {
                if (chip >= 0 && chip < _colors.Length)
                    s.Fill(r, _colors[chip]);
            }
        }

        public override void Dispose() { }

        public static ColorChipData LoadChipData(MapInfo info)
        {
            ColorChipData c = new ColorChipData();
            c._chipWidth = info.ChipDataInfo.Size.Width;
            c._chipHeight = info.ChipDataInfo.Size.Height;
            if (info.ChipDataInfo == null || info.ChipDataInfo.ChipInfos.Count == 0)
            {
                c._colors = new Color[] { Color.White, Color.Black };
                c._hardness = new int[] { 0, 1 };
            }
            else
            {
                c._colors = new Color[info.ChipDataInfo.ChipInfos.Count];
                c._hardness = new int[info.ChipDataInfo.ChipInfos.Count];

                List<Color> defaultColors = new List<Color>();
                if (info.ChipDataInfo.ChipInfos.Exists((ci) => { return !ci.Color.HasValue; }))
                {
                    string mappingPath = System.IO.Path.Combine(info.DirectoryPath, info.Mapping);
                    #region マッピングデータを読み込む
                    if (!string.IsNullOrEmpty(info.Mapping) && System.IO.File.Exists(mappingPath))
                    {
                        using (Bitmap mappingBmp = (Bitmap)Bitmap.FromFile(mappingPath))
                        {
                            using (Surface ms = new Surface(mappingBmp))
                            {
                                Color[,] tmp = ms.GetColors(new Rectangle(0, 0, ms.Width, ms.Height));
                                for (int i = 0; i < tmp.GetLength(0); i++)
                                {
                                    for (int j = 0; j < tmp.GetLength(1); j++)
                                    {
                                        Color dc = tmp[i, j];
                                        defaultColors.Add(dc);
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                }
                int idx = 0;
                foreach (ChipInfo ci in info.ChipDataInfo.ChipInfos)
                {
                    c._colors[idx] = ci.Color.HasValue ? ci.Color.Value : 
                        (defaultColors.Count > idx ? defaultColors[idx] : Color.Blue);
                    c._hardness[idx] = ci.Hardness;

                    idx++;
                }
            }
            return c;
        }
    }
}
