using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using SdlDotNet.Input;
using SdlDotNet.Graphics;
using SdlDotNet.Graphics.Sprites;

namespace PitchPitch.scene
{
    class SceneTitle : Scene
    {
        private SurfaceCollection _menuSurfaces = null;
        private Rectangle[] _menuRects = null;
        private int _selectedIdx = 0;
        private MenuItem[] _menuItems;
        private AnimatedSprite _cursor;

        private Surface _titleSurface = null;
        private Surface _coloredTitleSurface = null;
        private Surface _alertSurface = null;

        public SceneTitle()
        {
            sceneType = SceneType.Title;
            _menuItems = new MenuItem[]
            {
                new MenuItem(Key.S, "Start"),
                new MenuItem(Key.O, "Option"),
                new MenuItem(Key.Q, "Quit")
            };
            
            _keys = new Key[]
            {
                Key.UpArrow, Key.DownArrow, Key.Return, Key.M, Key.O, Key.Q
            };

            _cursor = ResourceManager.GetColoredCursorGraphic(Constants.DefaultForeColor);

            if (_menuSurfaces == null)
            {
                _menuSurfaces = new SurfaceCollection();
                _menuRects = new Rectangle[_menuItems.Length];
                ImageUtil.CreateStrMenu(_menuItems, Constants.DefaultForeColor, ResourceManager.MiddlePFont,
                    ref _menuSurfaces, ref _menuRects, Constants.ScreenWidth);
            }
        }

        public override void Init(PitchPitch parent)
        {
            base.Init(parent);
            _selectedIdx = 0;
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
                    // Select Map
                    _parent.EnterScene(SceneType.MapSelect);
                    break;
                case 1:
                    // Config
                    _parent.EnterScene(SceneType.Option);
                    break;
                case 2:
                    // Quit
                    _parent.Quit();
                    break;
            }
        }

        protected override void proc(KeyboardEventArgs e) { }

        protected override void draw(Surface s)
        {
            s.Fill(Constants.DefaultBackColor);

            if(_titleSurface == null)
                _titleSurface = ResourceManager.LoadSurface("logo.png");

            s.Blit(_coloredTitleSurface == null ? _titleSurface : _coloredTitleSurface,
                new Point((int)(s.Width / 2.0 - _titleSurface.Width / 2.0), 50));

            ImageUtil.DrawSelections(s, _menuSurfaces, _menuRects, _cursor, new Point(0, 70 + _titleSurface.Height), 
                _selectedIdx, ImageAlign.MiddleCenter);

            if (_alertSurface == null)
            {
                _alertSurface = ResourceManager.SmallPFont.Render(
                    Properties.Resources.Explanation_TitleAlert, Constants.DefaultStrongColor);
            }
            s.Blit(_alertSurface,
                new Point((int)(s.Width / 2.0 - _alertSurface.Width / 2.0),
                    s.Height - _alertSurface.Height - 30));
        }

        public override void Dispose()
        {
            if (_titleSurface != null) _titleSurface.Dispose();
            if (_coloredTitleSurface != null) _coloredTitleSurface.Dispose();
            if (_menuSurfaces != null)
            {
                foreach (Surface s in _menuSurfaces) s.Dispose();
            }
            if (_alertSurface != null) _alertSurface.Dispose();
            base.Dispose();
        }
    }
}
