using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using SdlDotNet.Graphics;
using SdlDotNet.Graphics.Sprites;

namespace PitchPitch
{
    struct MenuItem
    {
        private static string format = "{0}: {1}";

        public SdlDotNet.Input.Key Key;
        public string Name;
        public MenuItem(SdlDotNet.Input.Key key, string name)
        {
            Key = key; Name = name;
        }
        public override string ToString()
        {
            return string.Format(format, Key, Name);
        }
    }

    enum ImageAlign
    {
        TopLeft,
        TopCenter,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
    }

    class ImageUtil
    {
        #region 色変換
        public static Color GetInterpolatedColor(Color c0, Color c1, double ratio)
        {
            double da = ratio * c0.A + (1 - ratio) * c1.A;
            double dr = ratio * c0.R + (1 - ratio) * c1.R;
            double dg = ratio * c0.G + (1 - ratio) * c1.G;
            double db = ratio * c0.B + (1 - ratio) * c1.B;
            int a = (int)da; int r = (int)dr; int g = (int)dg; int b = (int)db;
            a = a < 0 ? 0 : (a > 255 ? 255 : a);
            r = r < 0 ? 0 : (r > 255 ? 255 : r);
            g = g < 0 ? 0 : (g > 255 ? 255 : g);
            b = b < 0 ? b : (b > 255 ? 255 : b);
            return Color.FromArgb(a, r, g, b);
        }
        public static Color GetColor(string s)
        {
            if (string.IsNullOrEmpty(s)) return Color.Transparent;
            Color c = Color.FromName(s);
            if (c.ToArgb() != 0) return c;
            if (s.Length == 0 || s[0] != '#') return Color.Transparent;

            string subStr = s.Substring(1, s.Length - 1);
            int n = Convert.ToInt32(subStr, 16);
            return Color.FromArgb(subStr.Length == 8 ? (n >> 24) : 255, (n & 0xff0000) >> 16, (n & 0x00ff00) >> 8, n & 0xff);
        }

        public static Color ConvertHSBtoARGB(byte alpha, float hue, float saturation, float brightness)
        {
            int maxB;
            int minB;
            int red, green, blue;

            if (brightness <= 0.5f)
            {
                maxB = (int)(brightness * (1.0f + saturation) * 255.0f + 0.01f);
                minB = (int)(brightness * (1.0f - saturation) * 255.0f + 0.01f);
            }
            else
            {
                maxB = (int)((brightness * (1.0f - saturation) + saturation) * 255.0f + 0.01f);
                minB = (int)((brightness + (brightness - 1.0f) * saturation) * 255.0f + 0.01f);
            }

            if (hue < 60.0f)
            {
                red = maxB;
                green = (int)(minB + hue * (float)(maxB - minB) / 60.0f + 0.01f);
                blue = minB;
            }
            else if (hue < 120.0F)
            {
                red = (int)(maxB - (hue - 60.0F) * (maxB - minB) / 60.0f + 0.01f);
                green = maxB;
                blue = minB;
            }
            else if (hue < 180.0F)
            {
                red = minB;
                green = maxB;
                blue = (int)(minB + (hue - 120.0F) * (maxB - minB) / 60.0f + 0.01f);
            }
            else if (hue < 240.0F)
            {
                red = minB;
                green = (int)(maxB - (hue - 180.0F) * (maxB - minB) / 60.0f + 0.01f);
                blue = maxB;
            }
            else if (hue < 300.0F)
            {
                red = (int)(minB + (hue - 240.0F) * (maxB - minB) / 60.0f + 0.01f);
                green = minB;
                blue = maxB;
            }
            else
            {
                red = maxB;
                green = minB;
                blue = (int)(maxB - (hue - 300.0F) * (maxB - minB) / 60.0f + 0.01f);
            }

            return Color.FromArgb((int)alpha, red, green, blue);
        }

        public static void GetRandomColors(out Color light, out Color dark, out Color strong)
        {
            Random r = new Random();
            float h = (float)(r.NextDouble() * 360);
            float hstrong = (h + 180) % 360;
            float s = 0.5f + (float)(r.NextDouble() * 0.4 - 0.2);
            float sstrong = 0.8f + (float)(r.NextDouble() * 0.4 - 0.2);
            float v0 = 0.8f + (float)(0.2 * r.NextDouble());
            float v1 = 0.2f - (float)(0.2 * r.NextDouble());
            float v2 = 0.5f + (float)(r.NextDouble() * 0.2 - 0.1);

            light = ConvertHSBtoARGB(255, h, s, v0);
            dark = ConvertHSBtoARGB(255, h, s, v1);
            strong = ConvertHSBtoARGB(255, hstrong, sstrong, v2);
        }
        public static void GetRandomColors(out Color light, out Color dark)
        {
            Random r = new Random();
            float h = (float)(r.NextDouble() * 360);
            float s = 0.5f + (float)(r.NextDouble() * 0.4 - 0.2);
            float v0 = 0.8f + (float)(0.2 * r.NextDouble());
            float v1 = 0.2f - (float)(0.2 * r.NextDouble());
            light = ConvertHSBtoARGB(255, h, s, v0);
            dark = ConvertHSBtoARGB(255, h, s, v1);
        }

        public static Color GetAvgColor(Surface s)
        {
            s.Lock();
            Color[,] tmp = s.GetColors(new Rectangle(0, 0, s.Width, s.Height));
            s.Unlock();

            double a = 0; double r = 0; double g = 0; double b = 0;
            for (int i = 0; i < tmp.GetLength(0); i++)
            {
                for (int j = 0; j < tmp.GetLength(1); j++)
                {
                    Color pc = tmp[i, j];
                    a += pc.A; r += pc.R; g += pc.G; b += pc.B;
                }
            }
            int num = tmp.GetLength(0) * tmp.GetLength(1);
            if (num == 0) return Color.Transparent;

            a /= (double)num; r /= (double)num; g /= (double)num; b /= (double)num;
            int ia = (int)a; int ir = (int)r; int ig = (int)g; int ib = (int)b;
            ia = ia < 0 ? 0 : (ia > 255 ? 255 : ia);
            ir = ir < 0 ? 0 : (ir > 255 ? 255 : ir);
            ig = ig < 0 ? 0 : (ig > 255 ? 255 : ig);
            ib = ib < 0 ? 0 : (ib > 255 ? 255 : ib);

            return Color.FromArgb(ia, ir, ig, ib);
        }

        public static Surface CreateColored(Surface s, Color c0, Color c1)
        {
            if (s == null) return null;
            Color[,] colors = s.GetColors(new Rectangle(0, 0, s.Width, s.Height));

            for (int i = 0; i < colors.GetLength(0); i++)
            {
                for (int j = 0; j < colors.GetLength(1); j++)
                {
                    Color c = colors[i, j];
                    float br = (c.R / 255.0f + c.G / 255.0f + c.B / 255.0f) / 3.0f;
                    if (br > 0.8f)
                    {
                        br = 1.0f;
                    }
                    else if (br < 0.2f)
                    {
                        br = 0.0f;
                    }
                    int r = (int)((1 - br) * c0.R + br * c1.R);
                    int g = (int)((1 - br) * c0.G + br * c1.G);
                    int b = (int)((1 - br) * c0.B + br * c1.B);
                    r = r < 0 ? 0 : (r > 255 ? 255 : r);
                    g = g < 0 ? 0 : (g > 255 ? 255 : g);
                    b = b < 0 ? 0 : (b > 255 ? 255 : b);
                    colors[i, j] = Color.FromArgb(c.A, r, g, b);
                    Color nc = Color.FromArgb(c.A, r, g, b);
                }
            }

            Surface ns = new Surface(s.Width, s.Height, s.BitsPerPixel, s.RedMask, s.GreenMask, s.BlueMask, s.AlphaMask);
            ns.AlphaBlending = s.AlphaBlending;
            ns.Alpha = s.Alpha;
            ns.Transparent = s.Transparent;
            ns.TransparentColor = s.TransparentColor;

            ns.Lock();
            ns.SetPixels(Point.Empty, colors);
            ns.Unlock();
            ns.Update();
            return ns;
        }

        public static void SetColor(Surface s, Color c)
        {
            if (s == null) return;
            Color[,] colors = s.GetColors(new Rectangle(0, 0, s.Width, s.Height));
            s.Lock();
            for (int i = 0; i < colors.GetLength(0); i++)
            {
                for (int j = 0; j < colors.GetLength(1); j++)
                {
                    colors[i, j] = Color.FromArgb(colors[i, j].A, c.R, c.G, c.B);
                }
            }
            s.SetPixels(Point.Empty, colors);
            s.Unlock();
            s.Update();
        }

        public static Surface CreateColored(Surface os, Color c)
        {
            if (os == null) return null;
            Surface s = new Surface(os);
            SetColor(s, c);
            return s;
        }
        public static SurfaceCollection CreateColored(SurfaceCollection os, Color c)
        {
            if (os == null) return null;
            SurfaceCollection col = new SurfaceCollection();
            foreach (Surface s in os)
            {
                Surface ns = CreateColored(s, c);
                col.Add(ns);
            }
            return col;
        }
        public static AnimatedSprite CreateColored(AnimatedSprite os, Color c)
        {
            if (os == null) return null;

            AnimatedSprite nsprite = new AnimatedSprite();
            foreach (KeyValuePair<string, AnimationCollection> kv in os.Animations)
            {
                string key = kv.Key; AnimationCollection anim = kv.Value;
                AnimationCollection nAnim = new AnimationCollection();
                foreach (Surface s in anim)
                {
                    Surface ns = CreateColored(s, c);
                    nAnim.Add(ns);
                }
                nAnim.Loop = anim.Loop;
                nAnim.Delay = anim.Delay;
                nAnim.FrameIncrement = anim.FrameIncrement;
                nAnim.Alpha = anim.Alpha;
                nAnim.AlphaBlending = anim.AlphaBlending;
                nAnim.AnimateForward = anim.AnimateForward;
                //nAnim.AnimationTime = anim.AnimationTime;
                nAnim.Transparent = anim.Transparent;
                nAnim.TransparentColor = anim.TransparentColor;

                nsprite.Animations.Add(key, nAnim);
            }
            nsprite.AllowDrag = os.AllowDrag;
            nsprite.Alpha = os.Alpha;
            nsprite.AlphaBlending = os.AlphaBlending;
            nsprite.Animate = os.Animate;
            nsprite.AnimateForward = os.AnimateForward;
            nsprite.CurrentAnimation = os.CurrentAnimation;
            nsprite.Frame = os.Frame;
            nsprite.Transparent = os.Transparent;
            nsprite.TransparentColor = os.TransparentColor;
            nsprite.Visible = os.Visible;

            return nsprite;
        }

        public static void SetAlpha(Surface s, float a)
        {
            if (s == null) return;
            Color[,] colors = s.GetColors(new Rectangle(0, 0, s.Width, s.Height));
            s.Lock();
            for (int i = 0; i < colors.GetLength(0); i++)
            {
                for (int j = 0; j < colors.GetLength(1); j++)
                {
                    Color c = colors[i, j];
                    int na = (int)(c.A * a);
                    colors[i, j] = Color.FromArgb(na < 0 ? 0 : (na > 255 ? 255 : na), c.R, c.G, c.B);
                }
            }
            s.SetPixels(Point.Empty, colors);
            s.Unlock();
            s.Update();
        }
        #endregion

        #region メニュー描画
        /// <summary>
        /// 画像群を指定した位置に描画
        /// </summary>
        /// <param name="canvas">描画されるSurface</param>
        /// <param name="surfaces">描画するSurfaceのCollection</param>
        /// <param name="rects">描画する位置<remarks>長さはsurfacesと同じでなければならない</remarks></param>
        /// <param name="offset">オフセット</param>
        /// <param name="align">位置揃えの方法</param>
        public static void DrawSurfaces(Surface canvas, SurfaceCollection surfaces, Rectangle[] rects, Point offset, ImageAlign align=ImageAlign.MiddleCenter)
        {
            Debug.Assert(surfaces.Count == rects.Length, "画像数とレイアウト数が合っていない");

            int idx = 0;
            foreach (Rectangle rect in rects)
            {
                Surface s = surfaces[idx];
                Point p = Point.Empty;
                switch (align)
                {
                    case ImageAlign.TopLeft:
                        p.X = rect.Left;
                        p.Y = rect.Top;
                        break;
                    case ImageAlign.TopCenter:
                        p.X = (int)(rect.Left + rect.Width / 2.0 - s.Size.Width / 2.0);
                        p.Y = rect.Top;
                        break;
                    case ImageAlign.TopRight:
                        p.X = rect.Right - s.Size.Width;
                        p.Y = rect.Top;
                        break;
                    case ImageAlign.MiddleLeft:
                        p.X = rect.Left;
                        p.Y = (int)(rect.Top + rect.Height / 2.0 - s.Size.Height / 2.0);
                        break;
                    case ImageAlign.MiddleCenter:
                        p.X = (int)(rect.Left + rect.Width / 2.0 - s.Size.Width / 2.0);
                        p.Y = (int)(rect.Top + rect.Height / 2.0 - s.Size.Height / 2.0);
                        break;
                    case ImageAlign.MiddleRight:
                        p.X = rect.Right - s.Size.Width;
                        p.Y = (int)(rect.Top + rect.Height / 2.0 - s.Size.Height / 2.0);
                        break;
                    case ImageAlign.BottomLeft:
                        p.X = rect.Left;
                        p.Y = rect.Bottom - s.Size.Height;
                        break;
                    case ImageAlign.BottomCenter:
                        p.X = (int)(rect.Left + rect.Width / 2.0 - s.Size.Width / 2.0);
                        p.Y = rect.Bottom - s.Size.Height;
                        break;
                    case ImageAlign.BottomRight:
                        p.X = rect.Right - s.Size.Width;
                        p.Y = rect.Bottom - s.Size.Height;
                        break;
                }
                p.Offset(offset);
                canvas.Blit(s, p);
                idx++;
            }
        }

        /// <summary>
        /// 画像群を指定した位置に描画
        /// </summary>
        /// <param name="canvas">描画されるSurface</param>
        /// <param name="surfaces">描画するSurfaceのCollection</param>
        /// <param name="rects">描画する位置<remarks>長さはsurfacesと同じでなければならない</remarks></param>
        /// <param name="align">位置揃えの方法</param>
        public static void DrawSurfaces(Surface canvas, SurfaceCollection surfaces, Rectangle[] rects, ImageAlign align = ImageAlign.MiddleCenter)
        {
            DrawSurfaces(canvas, surfaces, rects, Point.Empty, align);
        }

        public static void DrawSelections(Surface canvas, SurfaceCollection surfaces, Rectangle[] rects, AnimatedSprite cursor, Point offset, int selectedIndex = -1, ImageAlign align = ImageAlign.MiddleCenter)
        {
            DrawSurfaces(canvas, surfaces, rects, offset, align);
            if (selectedIndex >= 0 && selectedIndex < rects.Length)
            {
                Rectangle rect = rects[selectedIndex];
                Surface s = surfaces[selectedIndex];

                Point p = Point.Empty;
                switch (align)
                {
                    case ImageAlign.TopLeft:
                        p.X = rect.Left;
                        p.Y = (int)(rect.Top + s.Size.Height / 2.0);
                        break;
                    case ImageAlign.TopCenter:
                        p.X = (int)(rect.Left + rect.Width / 2.0 - s.Size.Width / 2.0);
                        p.Y = (int)(rect.Top + s.Size.Height / 2.0);
                        break;
                    case ImageAlign.TopRight:
                        p.X = rect.Right - s.Size.Width;
                        p.Y = (int)(rect.Top + s.Size.Height / 2.0);
                        break;
                    case ImageAlign.MiddleLeft:
                        p.X = rect.Left;
                        p.Y = (int)(rect.Top + rect.Height / 2.0);
                        break;
                    case ImageAlign.MiddleCenter:
                        p.X = (int)(rect.Left + rect.Width / 2.0 - s.Size.Width / 2.0);
                        p.Y = (int)(rect.Top + rect.Height / 2.0);
                        break;
                    case ImageAlign.MiddleRight:
                        p.X = rect.Right - s.Size.Width;
                        p.Y = (int)(rect.Top + rect.Height / 2.0);
                        break;
                    case ImageAlign.BottomLeft:
                        p.X = rect.Left;
                        p.Y = (int)(rect.Bottom - s.Size.Height / 2.0);
                        break;
                    case ImageAlign.BottomCenter:
                        p.X = (int)(rect.Left + rect.Width / 2.0 - s.Size.Width / 2.0);
                        p.Y = (int)(rect.Bottom - s.Size.Height / 2.0);
                        break;
                    case ImageAlign.BottomRight:
                        p.X = rect.Right - s.Size.Width;
                        p.Y = (int)(rect.Bottom - s.Size.Height / 2.0);
                        break;
                }
                p.X -= cursor.Size.Width;
                p.Y -= (int)(cursor.Size.Height / 2.0);
                p.Offset(offset);
                canvas.Blit(cursor, p);
            }
        }

        public static void DrawSelections(Surface canvas, SurfaceCollection surfaces, Rectangle[] rects, AnimatedSprite cursor, int selectedIndex = -1, ImageAlign align = ImageAlign.MiddleCenter)
        {
            DrawSelections(canvas, surfaces, rects, cursor, Point.Empty, selectedIndex, align);
        }

        public static void DrawSelections(Surface canvas, SurfaceCollection surfaces, Rectangle[] rects, int selectedIndex = -1, ImageAlign align = ImageAlign.MiddleCenter)
        {
            AnimatedSprite cursor = ResourceManager.CursorGraphic;
            DrawSelections(canvas, surfaces, rects, cursor, Point.Empty, selectedIndex, align);
        }

        #endregion

        #region メニュー作成
        public static void CreateStrMenu(string[] items, Color c, SdlDotNet.Graphics.Font font, ref SurfaceCollection surfaces, ref Rectangle[] rects, int width = 300, int height = 30)
        {
            int y = 0; int idx = 0;
            if (height < 0) height = (int)(font.Height * Constants.MenuLineHeight);
            foreach (string mi in items)
            {
                Surface s = font.Render(mi, c, true);
                surfaces.Add(s);
                Rectangle r = new Rectangle(0, y, width, height);
                y = r.Bottom;
                rects[idx++] = r;
            }
        }

        public static void CreateStrMenu(string[] items, Color c, ref SurfaceCollection surfaces, ref Rectangle[] rects, int width = 300, int height = -1)
        {
            CreateStrMenu(items, c, ResourceManager.SmallPFont, ref surfaces, ref rects, width, height);
        }

        public static void CreateStrMenu(MenuItem[] items, Color c, SdlDotNet.Graphics.Font font, ref SurfaceCollection surfaces, ref Rectangle[] rects, int width = 300, int height = -1)
        {
            CreateStrMenu(Array.ConvertAll<MenuItem, string>(items, (mi) =>
            {
                return mi.ToString();
            }), c, font, ref surfaces, ref rects, width, height);
        }

        public static void CreateStrMenu(MenuItem[] items, Color c, ref SurfaceCollection surfaces, ref Rectangle[] rects, int width = 300, int height = -1)
        {
            CreateStrMenu(items, c, ResourceManager.SmallPFont, ref surfaces, ref rects, width, height);
        }
        #endregion

        public static void DrawMultilineString(Surface s, string[] lines, SdlDotNet.Graphics.Font font, Color c, Point p)
        {
            int y = p.Y;
            int fh = (int)(font.Height * Constants.LineHeight);
            foreach (string line in lines)
            {
                using (Surface ts = font.Render(line, c))
                {
                    s.Blit(ts, new Point(p.X, y));
                }
                y += fh;
            }
        }
    }
}
