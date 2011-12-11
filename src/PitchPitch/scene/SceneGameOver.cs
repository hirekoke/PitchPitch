using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using SdlDotNet.Graphics;
using SdlDotNet.Graphics.Sprites;
using SdlDotNet.Input;

namespace PitchPitch.scene
{
    using MenuItem = KeyValuePair<Key, string>;

    class SceneGameOver : Scene
    {
        private Surface _prevSurface = null;

        private SurfaceCollection _menuSurfaces = null;
        private Rectangle[] _menuRects = null;
        private int _selectedIdx = 0;
        private MenuItem[] _menuItems;
        private AnimatedSprite _cursor;

        private Color _foreColor = Color.White;
        private Color _backColor = Color.DarkRed;

        private Surface _overSurface = null;
        private Surface _overImgSurface = null;

        public SceneGameOver()
        {
            sceneType = SceneType.GameOver;

            _menuItems = new MenuItem[]
            {
                new MenuItem(Key.R, Properties.Resources.MenuItem_RetryStage),
                new MenuItem(Key.M, Properties.Resources.MenuItem_MapSelect),
                new MenuItem(Key.T, Properties.Resources.MenuItem_ReturnTitle)
            };

            _keys = new Key[]
            {
                Key.UpArrow, Key.DownArrow, Key.Return, Key.Escape,
                Key.M, Key.R, Key.T
            };
        }

        public override void Init(PitchPitch parent)
        {
            if (_prevSurface != null) _prevSurface.Dispose();
            _prevSurface = null;

            if (_overImgSurface == null)
            {
                _overImgSurface = ResourceManager.LoadSurface("gameover.png");
                ImageUtil.SetColor(_overImgSurface, _foreColor);
            }

            _cursor = ResourceManager.GetColoredCursorGraphic(_foreColor);

            _menuSurfaces = new SurfaceCollection();
            _menuRects = new Rectangle[_menuItems.Length];
            ImageUtil.CreateStrMenu(_menuItems, _foreColor, ref _menuSurfaces, ref _menuRects, Constants.ScreenWidth);

            base.Init(parent);
        }

        protected override int procKeyEvent(Key key)
        {
            int idx = -1;
            switch (key)
            {
                case Key.UpArrow:
                    _selectedIdx = (_selectedIdx - 1 < 0 ?
                        (_selectedIdx + _menuItems.Length - 1) :
                        _selectedIdx - 1) % _menuItems.Length;
                    break;
                case Key.DownArrow:
                    _selectedIdx = (_selectedIdx + 1) % _menuItems.Length;
                    break;
                case Key.Return:
                    idx = _selectedIdx; break;
                case Key.Escape:
                    idx = 1; break;
                default:
                    idx = 0;
                    foreach (MenuItem mi in _menuItems)
                    {
                        if (mi.Key == key) break;
                        idx++;
                    }
                    break;
            }
            return idx;
        }

        protected override void procMenu(int idx)
        {
            if (idx < 0) return;

            switch (idx)
            {
                case 0:
                    // RetryMap Map
                    _parent.RetryMap();
                    break;
                case 1:
                    // Select Map
                    _parent.EnterScene(SceneType.MapSelect);
                    break;
                case 2:
                    // Return to Title
                    _parent.EnterScene(SceneType.Title);
                    break;
            }
        }

        public override void Draw(Surface s)
        {
            // ゲームオーバー時画面
            if (_prevSurface == null)
            {
                _prevSurface = new Surface(s);
                _prevSurface.Transparent = true;
                _prevSurface.AlphaBlending = true;
                _prevSurface.Alpha = 64;
            }

            s.Fill(_backColor);
            s.Blit(_prevSurface, Point.Empty);

            // 画像
            s.Blit(_overImgSurface, new Point(s.Size.Width - _overImgSurface.Width - 60, s.Size.Height - _overImgSurface.Height - 60));
            
            // タイトル
            if (_overSurface == null)
            {
                _overSurface = ResourceManager.LargePFont.Render(Properties.Resources.HeaderTitle_GameOver, _foreColor);
            }
            s.Blit(_overSurface, new Point(Constants.HeaderX, Constants.HeaderY));

            // メニュー
            ImageUtil.DrawSelections(s, _menuSurfaces, _menuRects, _cursor,
                new Point(Constants.HeaderX + Constants.UnderHeaderMargin + Constants.CursorMargin,
                    Constants.HeaderY + ResourceManager.LargePFont.Height + Constants.HeaderBottomMargin),
                _selectedIdx, ImageAlign.TopLeft);
        }

        public override void Dispose()
        {
            if (_overImgSurface != null) _overImgSurface.Dispose();
            if (_prevSurface != null) _prevSurface.Dispose();
            if (_overSurface != null) _overSurface.Dispose();
            base.Dispose();
        }
    }
}
