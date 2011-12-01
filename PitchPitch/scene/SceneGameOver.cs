﻿using System;
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
        private SurfaceCollection _menuSurfaces = null;
        private Rectangle[] _menuRects = null;
        private int _selectedIdx = 0;
        private MenuItem[] _menuItems;
        private AnimatedSprite _cursor;

        private Color _foreColor = Color.White;
        private Color _backColor = Color.DarkRed;

        public SceneGameOver()
        {
            sceneType = SceneType.GameOver;

            _menuItems = new MenuItem[]
            {
                new MenuItem(Key.R, "Retry Stage"),
                new MenuItem(Key.M, "Select Map"),
                new MenuItem(Key.T, "Return to Title")
            };

            _keys = new Key[]
            {
                Key.UpArrow, Key.DownArrow, Key.Return, Key.Escape,
                Key.M, Key.R, Key.T
            };
        }

        public override void Init(PitchPitch parent)
        {
            _cursor = ResourceManager.GetColoredCursorGraphic(_foreColor);

            _menuSurfaces = new SurfaceCollection();
            _menuRects = new Rectangle[_menuItems.Length];
            ImageManager.CreateStrMenu(_menuItems, _foreColor, ref _menuSurfaces, ref _menuRects, parent.Size.Width);

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
            s.Fill(_backColor);

            int lh = ResourceManager.LargePFont.Height; int sh = ResourceManager.SmallPFont.Height;
            s.Blit(ResourceManager.LargePFont.Render("GAME OVER", _foreColor), new Point(10, 10));

            ImageManager.DrawSelections(s, _menuSurfaces, _menuRects, _cursor, new Point(50, 80),
                _selectedIdx, ImageAlign.TopLeft);
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
