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
        #endregion

        #region 色
        public static Color DefaultForeColor = Color.Black;
        public static Color DefaultBackColor = Color.White;
        public static Color DefaultStrongColor = Color.Firebrick;
        public static Color DefaultSelectionColor = Color.PeachPuff;
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
        #endregion
    }
}
