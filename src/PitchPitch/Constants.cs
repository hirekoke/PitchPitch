using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace PitchPitch
{
    class Constants
    {
        #region 内部文字列
        /// <summary>ネームスペース</summary>
        public const string Namespace = "PitchPitch";
        /// <summary>ディレクトリ名-画像</summary>
        public const string Dirname_Image = "image";
        /// <summary>ディレクトリ名-効果音</summary>
        public const string Dirname_Sound = "se";
        #endregion

        #region 画像ファイル名
        public const string Filename_LifeImage = "life.png";
        public const string Filename_CursorImage = "cursor.png";
        public const string Filename_ExplosionImage = "explosion.png";
        public const string Filename_GameoverImage = "gameover.png";
        public const string Filename_TitleLogoImage = "logo.png";
        public const string Filename_PlayerImage = "player-{0}.png";
        #endregion

        #region 色
        public static readonly Color Color_Transition = Color.Black;
        public static readonly Color Color_Foreground = Color.Black;
        public static readonly Color Color_Background = Color.White;
        public static readonly Color Color_Strong = Color.Firebrick;
        public static readonly Color Color_Selection = Color.LightBlue;

        public static readonly Color Color_AlertBackground = Color.DarkRed;
        public static readonly Color Color_AlertForeground = Color.White;
        #endregion

        #region ゲーム処理
        public const int Time_Transition = 160;
        public const int Time_ContinuousKeyStart = 500;
        public const int Time_ContinuousKeyDiff = 60;

        public const int MinPitch = 50;
        public const int MaxPitch = 16000;
        public const int MaxOctave = 3;
        public const int MinOctave = -3;
        #endregion

        #region 画面レイアウト
        /// <summary>画面サイズ幅</summary>
        public const int ScreenWidth = 800;
        /// <summary>画面サイズ高さ</summary>
        public const int ScreenHeight = 600;

        /// <summary>画面名左上X座標</summary>
        public const int HeaderX = 20;
        /// <summary>画面名左上Y座標</summary>
        public const int HeaderY = 20;
        /// <summary>画面名下マージン</summary>
        public const int HeaderBottomMargin = 20;

        /// <summary>画面名以外のコンテンツのマージン<summary>
        public const int UnderHeaderMargin = 20;
        /// <summary>カーソル幅<summary>
        public const int CursorMargin = 30;
        /// <summary>横に2列以上メニューがある際の間隔<summary>
        public const int MenuColumnGap = 20;
        /// <summary>小見出し下マージン<summary>
        public const int SubHeaderBottomMargin = 10;
        /// <summary>右下に表示するコンテンツの右下座標<summary>
        public const int RightBottomItemMargin = 20;

        /// <summary>波形表示高さ<summary>
        public const int WaveHeight = 100;
        /// <summary>波形情報表示高さ<summary>
        public const int WaveInfoHeight = 80;

        /// <summary>窓内パディング<summary>
        public const int WindowPadding = 10;

        /// <summary>メニュー高さデフォルト<summary>
        public const double MenuLineHeight = 1.4;
        /// <summary>複数行文字列高さデフォルト<summary>
        public const double LineHeight = 1.1;
        #endregion

        #region ゲーム画面レイアウト
        /// <summary>ゲーム画面幅</summary>
        public const int StageViewWidth = 640;
        /// <summary>ゲーム画面高さ</summary>
        public const int StageViewHeight = 450;

        /// <summary>ゲーム画面マージン</summary>
        public const int StageMargin = 10;
        /// <summary>ゲーム画面レイアウト間隔</summary>
        public const int StageGap = 6;

        /// <summary>ミニマップ画面幅</summary>
        public const int MiniMapWidth = 400;

        /// <summary>プレイヤー情報表示枠のパディング</summary>
        public const int PlayerInfoPadding = 2;
        #endregion
    }
}
