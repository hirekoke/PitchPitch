using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using SdlDotNet.Graphics;
using SdlDotNet.Graphics.Sprites;
using SdlDotNet.Input;
using SdlDotNet.Audio;

using SysGraphics = System.Drawing;
using SdlGraphics = SdlDotNet.Graphics;

namespace PitchPitch
{
    /// <summary>
    /// 永続的に使うリソースを取得する
    /// </summary>
    class ResourceManager
    {
        #region デフォルトフォント
        private static SdlGraphics.Font _smallTTFont = null;
        public static SdlGraphics.Font SmallTTFont
        {
            get
            {
                if(_smallTTFont == null)
                    _smallTTFont = new SdlGraphics.Font(Path.Combine(Properties.Resources.Dirname_Font, 
                        Properties.Resources.Font_DefaultTT), 20);
                return _smallTTFont;
            }
        }

        private static SdlGraphics.Font _middleTTFont = null;
        public static SdlGraphics.Font MiddleTTFont
        {
            get
            {
                if (_middleTTFont == null)
                    _middleTTFont = new SdlGraphics.Font(Path.Combine(Properties.Resources.Dirname_Font,
                        Properties.Resources.Font_DefaultTT), 28);
                return _middleTTFont;
            }
        }

        private static SdlGraphics.Font _largeTTFont = null;
        public static SdlGraphics.Font LargeTTFont
        {
            get
            {
                if(_largeTTFont == null)
                    _largeTTFont = new SdlGraphics.Font(Path.Combine(Properties.Resources.Dirname_Font,
                        Properties.Resources.Font_DefaultTT), 32);
                return _largeTTFont;
            }
        }

        private static SdlGraphics.Font _smallPFont = null;
        public static SdlGraphics.Font SmallPFont
        {
            get
            {
                if (_smallPFont == null)
                    _smallPFont = new SdlGraphics.Font(Path.Combine(Properties.Resources.Dirname_Font,
                        Properties.Resources.Font_DefaultP), 18);
                return _smallPFont;
            }
        }

        private static SdlGraphics.Font _middlePFont = null;
        public static SdlGraphics.Font MiddlePFont
        {
            get
            {
                if (_middlePFont == null)
                    _middlePFont = new SdlGraphics.Font(Path.Combine(Properties.Resources.Dirname_Font,
                        Properties.Resources.Font_DefaultP), 24);
                return _middlePFont;
            }
        }

        private static SdlGraphics.Font _largePFont = null;
        public static SdlGraphics.Font LargePFont
        {
            get
            {
                if (_largePFont == null)
                    _largePFont = new SdlGraphics.Font(Path.Combine(Properties.Resources.Dirname_Font,
                        Properties.Resources.Font_DefaultP), 32);
                return _largePFont;
            }
        }

        public static SdlGraphics.Font LoadTTFont(int size)
        {
            return new SdlGraphics.Font(Path.Combine(Properties.Resources.Dirname_Font,
                        Properties.Resources.Font_DefaultTT), size);
        }
        public static SdlGraphics.Font LoadPFont(int size)
        {
            return new SdlGraphics.Font(Path.Combine(Properties.Resources.Dirname_Font,
                        Properties.Resources.Font_DefaultP), size);
        }

        #endregion

        #region 効果音
        private static string getSoundResourceFullName(string resourceName)
        {
            return string.Format("{0}.{1}.{2}", Constants.Namespace, Constants.Dirname_Sound, resourceName);
        }
        private static Sound getSoundResource(string resourceName)
        {
            try
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                byte[] buf = null;
                using (Stream stm = asm.GetManifestResourceStream(getSoundResourceFullName(resourceName)))
                {
                    buf = new byte[stm.Length];
                    stm.Read(buf, 0, buf.Length);
                }
                if (buf != null && buf.Length > 0)
                {
                    return new Sound(buf);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Sound _soundOK = null;
        public static Sound SoundOK
        {
            get
            {
                if (_soundOK == null)
                    _soundOK = getSoundResource("ok.wav");
                return _soundOK;
            }
        }

        private static Sound _soundCancel = null;
        public static Sound SoundCancel
        {
            get
            {
                if (_soundCancel == null)
                    _soundCancel = getSoundResource("cancel.wav");
                return _soundCancel;
            }
        }

        private static SoundDictionary _soundExplosion = null;
        public static SoundDictionary SoundExplosion
        {
            get
            {
                if (_soundExplosion == null)
                {
                    _soundExplosion = new SoundDictionary();
                    for (int i = 0; i < 12; i++)
                    {
                        Sound s = getSoundResource(string.Format("exp{0:D2}.wav", i));
                        _soundExplosion.Add(i.ToString("D2"), s);
                    }
                }
                return _soundExplosion;
            }
        }
        #endregion

        #region 画像
        private static AnimatedSprite _cursorGraphic = null;
        public static AnimatedSprite CursorGraphic
        {
            get
            {
                if (_cursorGraphic == null)
                {
                    SurfaceCollection tmp = LoadSurfaces("cursor.png", new Size(24, 28));
                    AnimationCollection anim = new AnimationCollection();
                    anim.Add(tmp, 100, true);
                    anim.AlphaBlending = true;
                    _cursorGraphic = new AnimatedSprite(anim);
                    _cursorGraphic.Animate = true;
                    _cursorGraphic.AlphaBlending = true;
                }
                return _cursorGraphic;
            }
        }

        private static Dictionary<Color, AnimatedSprite> _coloredCursors = new Dictionary<Color,AnimatedSprite>();
        public static AnimatedSprite GetColoredCursorGraphic(Color c)
        {
            if (_coloredCursors.ContainsKey(c)) return _coloredCursors[c];
            AnimatedSprite a = ImageUtil.CreateColored(CursorGraphic, c);
            _coloredCursors.Add(c, a);
            return a;
        }
        #endregion

        #region 画像読み込みメソッド
        private static string getImgResourceFullName(string resourceName)
        {
            return string.Format("{0}.{1}.{2}", Constants.Namespace, Constants.Dirname_Image, resourceName);
        }

        private static Bitmap getImgResource(string resourceName)
        {
            Bitmap bmp = null;
            try
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                bmp = new Bitmap(asm.GetManifestResourceStream(getImgResourceFullName(resourceName)));
                return bmp;
            }
            catch(Exception)
            {
                if (bmp != null) bmp.Dispose();
                return null;
            }
        }

        public static Surface LoadSurface(string resourceName)
        {
            Surface ret = null;
            using (Bitmap bmp = getImgResource(resourceName))
            {
                ret = new Surface(bmp);
                ret.AlphaBlending = true;
            }
            ret.Update();
            return ret;
        }

        public static SurfaceCollection LoadSurfaces(string[] resourceNames)
        {
            SurfaceCollection ret = new SurfaceCollection();
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            foreach (string resourceName in resourceNames)
            {
                using (Bitmap bmp = new Bitmap(asm.GetManifestResourceStream(getImgResourceFullName(resourceName))))
                {
                    Surface s = new Surface(bmp);
                    s.AlphaBlending = true;
                    s.Update();
                    ret.Add(s);
                }
            }
            ret.AlphaBlending = true;
            return ret;
        }

        public static SurfaceCollection LoadSurfaces(string resourceName, Size tileSize)
        {
            SurfaceCollection ret = new SurfaceCollection();
            ret.AlphaBlending = true;

            using (Surface s = LoadSurface(resourceName))
            {
                for (int i = 0; i < s.Width; i += tileSize.Width)
                {
                    for (int j = 0; j < s.Height; j += tileSize.Height)
                    {
                        Surface ss = new Surface(tileSize.Width, tileSize.Height, 32, s.RedMask, s.GreenMask, s.BlueMask, s.AlphaMask);
                        ss.Transparent = true;
                        ss.AlphaBlending = true;
                        Color[,] tmp = s.GetColors(new Rectangle(i, j, tileSize.Width, tileSize.Height));
                        ss.SetPixels(Point.Empty, tmp);
                        tmp = ss.GetColors(new Rectangle(0, 0, ss.Width, ss.Height));
                        ss.Update();
                        ret.Add(ss);
                    }
                }
            }
            return ret;
        }

        public static SurfaceCollection LoadSurfacesFromFile(string filePath, Size tileSize)
        {
            SurfaceCollection ret = new SurfaceCollection();
            ret.AlphaBlending = true;
            if (!File.Exists(filePath)) return ret;

            using (Bitmap bmp = (Bitmap)Bitmap.FromFile(filePath))
            {
                using (Surface s = new Surface(bmp))
                {
                    s.Lock();
                    for (int i = 0; i < s.Width; i += tileSize.Width)
                    {
                        for (int j = 0; j < s.Height; j += tileSize.Height)
                        {
                            Surface ss = new Surface(tileSize.Width, tileSize.Height, 32, s.RedMask, s.GreenMask, s.BlueMask, s.AlphaMask);
                            ss.Transparent = true;
                            ss.AlphaBlending = true;

                            Color[,] tmp = s.GetColors(new Rectangle(i, j, tileSize.Width, tileSize.Height));
                            ss.Lock();
                            ss.SetPixels(Point.Empty, tmp);
                            tmp = ss.GetColors(new Rectangle(0, 0, ss.Width, ss.Height));
                            ss.Unlock();
                            ss.Update();
                            ret.Add(ss);
                        }
                    }
                    s.Unlock();
                }
            }

            return ret;
        }

        #endregion

        #region Key変換

        public static int GetKeyNum(KeyboardEventArgs e)
        {
            switch (e.Key)
            {
                case Key.One: return 1;
                case Key.Two: return 2;
                case Key.Three: return 3;
                case Key.Four: return 4;
                case Key.Five: return 5;
                case Key.Six: return 6;
                case Key.Seven: return 7;
                case Key.Eight: return 8;
                case Key.Nine: return 9;
                case Key.Zero: return 0;
                default: return -1;
            }
        }

        public static string ConvertToString(Key key)
        {
            switch (key)
            {
                case Key.Escape: return "Esc";
                case Key.Return: return "Return";
                case Key.RightAlt:
                case Key.LeftAlt: return "Alt";
                case Key.RightControl:
                case Key.LeftControl: return "Ctrl";
                case Key.Space: return "Space";
                case Key.Ampersand: return "&";
                case Key.Asterisk: return "*";
                case Key.At: return "@";
                case Key.BackQuote: return "`";
                case Key.Backslash: return @"\";
                case Key.Backspace: return "BS";
                case Key.Caret: return "^";
                case Key.Colon: return ":";
                case Key.Comma: return ",";
                case Key.Delete: return "Del";
                case Key.DollarSign: return "$";
                case Key.DoubleQuote: return "\"";

                case Key.Zero: return "0";
                case Key.One: return "1";
                case Key.Two: return "2";
                case Key.Three: return "3";
                case Key.Four: return "4";
                case Key.Five: return "5";
                case Key.Six: return "6";
                case Key.Seven: return "7";
                case Key.Eight: return "8";
                case Key.Nine: return "9";

                default:
                    return key.ToString();
            }
        }
        #endregion

        public static void Release()
        {
            if (_cursorGraphic != null) _cursorGraphic.Dispose();

            if (_smallTTFont != null) _smallTTFont.Dispose();
            if (_middleTTFont != null) _middleTTFont.Dispose();
            if (_largeTTFont != null) _largeTTFont.Dispose();

            if (_smallPFont != null) _smallPFont.Dispose();
            if (_middlePFont != null) _middlePFont.Dispose();
            if (_largePFont != null) _largePFont.Dispose();

            if (_soundOK != null) _soundOK.Dispose();
            if (_soundCancel != null) _soundCancel.Dispose();
            if (_soundExplosion != null) foreach (Sound s in _soundExplosion.Values) s.Dispose();

            foreach (KeyValuePair<Color, AnimatedSprite> kv in _coloredCursors)
                kv.Value.Dispose();
        }
    }
}
