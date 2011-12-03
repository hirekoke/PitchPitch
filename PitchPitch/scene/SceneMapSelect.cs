using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;
using PitchPitch.map;
using SdlDotNet.Graphics;
using SdlDotNet.Input;

namespace PitchPitch.scene
{
    using MenuItem = KeyValuePair<Key, string>;

    class SceneMapSelect : Scene
    {
        private Color _foreColor = Color.Black;
        private Color _backColor = Color.White;
        private SdlDotNet.Graphics.Sprites.AnimatedSprite _cursor;

        private MapLoader _loader;

        private List<MapInfo> _mapInfos;
        private SurfaceCollection _mapSurfaces;
        private Rectangle[] _mapRects;
        private int _mapSelectedIdx = 0;
        private SurfaceCollection _mapDrawSurfaces;
        private Rectangle[] _mapDrawRects;
        private int _mapDrawFirstIdx = -1;
        private int _mapDrawLength = -1;

        private string[] _randItems;
        private SurfaceCollection _randSurfaces;
        private Rectangle[] _randRects;
        private int _randSelectedIdx = 0;

        private bool _mapFocus = true;

        private Rectangle _mapRect;
        private Rectangle _randRect;

        public SceneMapSelect()
        {
            sceneType = scene.SceneType.MapSelect;

            _loader = new MapLoader();
            _mapInfos = new List<MapInfo>();

            _keys = new Key[]
            {
                Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow, Key.Return, Key.Escape,
                Key.One, Key.Two, Key.R
            };
            _randItems = new string[] { "練習用", "Random Map", "Endless Map", "Reload Maps" };
        }

        public override void Init(PitchPitch parent)
        {
            base.Init(parent);
            _cursor = ResourceManager.GetColoredCursorGraphic(_foreColor);

            int h = 100;
            _randRect = new Rectangle(60, h, (int)(parent.Size.Width/3.0), parent.Size.Height - h - 30);
            _mapRect = new Rectangle(_randRect.Right + 40, h, parent.Size.Width - _randRect.Width - 160, parent.Size.Height - h - 30);

            _randSurfaces = new SurfaceCollection();
            _randRects = new Rectangle[_randItems.Length];
            ImageManager.CreateStrMenu(_randItems, _foreColor, ref _randSurfaces, ref _randRects, _randRect.Width);

            if(_mapInfos.Count == 0)
                updateMapInfos();
        }

        private void updateMapInfos()
        {
            _mapInfos.Clear();
            _mapInfos = _loader.LoadMapInfos();

            string[] infoStrs = Array.ConvertAll<MapInfo, string>(_mapInfos.ToArray(),
                (mi) => { return mi.Name; });

            _mapSurfaces = new SurfaceCollection();
            _mapRects = new Rectangle[infoStrs.Length];
            ImageManager.CreateStrMenu(infoStrs, _foreColor, ref _mapSurfaces, ref _mapRects, _mapRect.Width);

            int ih = 30;
            if (_mapRects.Length > 0) ih = _mapRects[0].Height;
            _mapDrawLength = (int)Math.Floor(_mapRect.Height / (double)ih);
            if (_mapDrawLength > _mapInfos.Count) _mapDrawLength = _mapInfos.Count;

            _mapDrawFirstIdx = -1;
            _mapSelectedIdx = 0;
            if (_mapInfos.Count == 0) _mapFocus = false;

            updateMapIndex();
        }

        private void updateMapIndex()
        {
            if (_mapDrawFirstIdx >= 0 && _mapDrawFirstIdx <= _mapSelectedIdx && _mapSelectedIdx < _mapDrawFirstIdx + _mapDrawLength)
            {
                // 入ってる
                return;
            }
            else
            {
                // 入っていない
                if (_mapDrawFirstIdx < 0) // 最初
                {
                    _mapDrawFirstIdx = 0;
                }
                else if (_mapSelectedIdx < _mapDrawFirstIdx) // 前にずらす
                {
                    _mapDrawFirstIdx = _mapSelectedIdx;
                }
                else // 後ろにずらす
                {
                    _mapDrawFirstIdx = _mapSelectedIdx + 1 - _mapDrawLength;
                }
            }

            string[] infoStrs = Array.ConvertAll<MapInfo, string>(
                _mapInfos.GetRange(_mapDrawFirstIdx, _mapDrawLength).ToArray(),
                (mi) => { return mi.Name; });
            
            if (_mapDrawSurfaces != null)
                foreach (Surface s in _mapDrawSurfaces) s.Dispose();

            _mapDrawSurfaces = new SurfaceCollection();
            _mapDrawRects = new Rectangle[infoStrs.Length];
            ImageManager.CreateStrMenu(infoStrs, _foreColor, ref _mapDrawSurfaces, ref _mapDrawRects, _mapRect.Width);
        }

        protected override int procKeyEvent(Key key)
        {
            int idx = -1;
            switch (key)
            {
                case Key.UpArrow:
                    if (_mapFocus && _mapRects.Length > 0)
                    {
                        _mapSelectedIdx = (_mapSelectedIdx - 1 < 0 ?
                            (_mapSelectedIdx + _mapRects.Length - 1) : _mapSelectedIdx - 1) % _mapRects.Length;
                        updateMapIndex();
                    }
                    else
                    {
                        _randSelectedIdx = (_randSelectedIdx - 1 < 0 ?
                            (_randSelectedIdx + _randRects.Length - 1) : _randSelectedIdx - 1) % _randRects.Length;
                    }
                    break;
                case Key.DownArrow:
                    if (_mapFocus && _mapRects.Length > 0)
                    {
                        _mapSelectedIdx = (_mapSelectedIdx + 1) % _mapRects.Length;
                        updateMapIndex();
                    }
                    else
                    {
                        _randSelectedIdx = (_randSelectedIdx + 1) % _randRects.Length;
                    }
                    break;
                case Key.RightArrow:
                case Key.LeftArrow:
                    if (_mapFocus || _mapRects.Length > 0) _mapFocus = !_mapFocus;
                    break;
                case Key.Return:
                    idx = 1;
                    break;
                case Key.Escape:
                    idx = 0;
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
                    _parent.EnterScene(SceneType.Title);
                    break;
                case 1:
                    map.Map map = null;
                    if (_mapFocus)
                    {
                        MapInfo info = _mapInfos[_mapSelectedIdx];
                        map = _loader.LoadMap(info);
                        if (map != null)
                        {
                            _parent.EnterScene(scene.SceneType.GameStage, map);
                        }
                        else
                        {
#warning 例外
                        }
                    }
                    else
                    {
                        switch (_randSelectedIdx)
                        {
                            case 0:
                                map = new EmptyMap();
                                _parent.EnterScene(scene.SceneType.GameStage, map);
                                break;
                            case 1:
                                map = new RandomMap();
                                _parent.EnterScene(scene.SceneType.GameStage, map);
                                break;
                            case 2:
                                map = new RandomEndlessMap();
                                _parent.EnterScene(scene.SceneType.EndlessGameStage);
                                break;
                            case 3:
                                updateMapInfos();
                                break;
                        }
                    }
                    break;
            }
        }

        public override void Draw(SdlDotNet.Graphics.Surface s)
        {
            s.Fill(_backColor);

            ImageManager.DrawSelections(s, _randSurfaces, _randRects, _cursor,
                _randRect.Location, (_mapFocus ? -1 : _randSelectedIdx), ImageAlign.MiddleLeft);

            ImageManager.DrawSelections(s, _mapDrawSurfaces, _mapDrawRects, _cursor,
                _mapRect.Location, (_mapFocus ? _mapSelectedIdx - _mapDrawFirstIdx : -1), ImageAlign.MiddleLeft);
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
