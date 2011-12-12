using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;
using PitchPitch.map;
using SdlDotNet.Graphics;
using SdlDotNet.Input;

namespace PitchPitch.scene
{
    class SceneMapSelect : Scene
    {
        private SdlDotNet.Graphics.Sprites.AnimatedSprite _cursor;
        private SdlDotNet.Graphics.Sprites.AnimatedSprite _strongCursor;

        private MapLoader _loader;

        private Surface _headerSurface = null;
        private Surface _expSurface = null;

        // 読み込みマップ表示用
        private List<MapInfo> _mapInfos;
        private SurfaceCollection _mapSurfaces;
        private Rectangle[] _mapRects;
        private int _mapSelectedIdx = 0;
        private SurfaceCollection _mapDrawSurfaces;
        private Rectangle[] _mapDrawRects;
        private int _mapDrawFirstIdx = -1;
        private int _mapDrawLength = -1;

        // ビルトインマップ表示用
        private string[] _randItems;
        private SurfaceCollection _randSurfaces;
        private Rectangle[] _randRects;
        private int _randSelectedIdx = 0;
        private MapInfo[] _builtinMapInfos;

        // タイトルに戻るメニュー表示用
        private MenuItem[] _escItems = new MenuItem[] { new MenuItem(Key.Escape, Properties.Resources.MenuItem_ReturnTitle) };
        private SurfaceCollection _escSurfaces;
        private Rectangle[] _escRects;

        // カーソル位置記憶
        private bool _mapFocus = true;
        private bool _escFocus = false; 

        private Rectangle _mapRect;  // 読み込んだマップ情報表示領域
        private Rectangle _randRect; // ビルトインマップ情報表示領域
        private Rectangle _escRect;  // タイトルに戻るメニュー表示領域
        private Rectangle _expRect;  // 説明文表示領域

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
            _builtinMapInfos = new MapInfo[]
            {
                EmptyMap.GetMapInfo(),
                RandomMap.GetMapInfo(1),
                RandomMap.GetMapInfo(3),
                RandomMap.GetMapInfo(5),
                RandomEndlessMap.GetMapInfo(3)
            };

            _randItems = new string[_builtinMapInfos.Length + 1];
            for (int i = 0; i < _builtinMapInfos.Length; i++)
            {
                _randItems[i] = _builtinMapInfos[i].MapName;
            }
            _randItems[_randItems.Length - 1] = Properties.Resources.MenuItem_ReloadMap;


            _cursor = ResourceManager.GetColoredCursorGraphic(Constants.DefaultForeColor);
            _strongCursor = ResourceManager.GetColoredCursorGraphic(Constants.DefaultStrongColor);

            #region レイアウト初期化
            Size escSize = ResourceManager.MiddlePFont.SizeText(_escItems[0].ToString());
            _escRect = new Rectangle(
                Constants.ScreenWidth - Constants.RightBottomItemMargin - escSize.Width - Constants.CursorMargin,
                Constants.ScreenHeight - Constants.RightBottomItemMargin - escSize.Height,
                escSize.Width + Constants.CursorMargin, escSize.Height);

            _expRect = new Rectangle(
                Constants.HeaderX + Constants.UnderHeaderMargin,
                Constants.HeaderY + ResourceManager.LargePFont.Height + Constants.HeaderBottomMargin,
                Constants.ScreenWidth - Constants.UnderHeaderMargin * 2,
                ResourceManager.SmallPFont.Height);

            int top = _expRect.Bottom + Constants.SubHeaderBottomMargin;
            int bottom = _escRect.Top - Constants.UnderHeaderMargin;
            _mapRect = new Rectangle(
                Constants.HeaderX + Constants.UnderHeaderMargin + Constants.CursorMargin,
                top,
                (int)((Constants.ScreenWidth - (Constants.HeaderX + Constants.UnderHeaderMargin * 2) - Constants.MenuColumnGap) / 2.0) - Constants.CursorMargin,
                bottom - top);
            _randRect = new Rectangle(
                _mapRect.Right + Constants.MenuColumnGap + Constants.CursorMargin,
                _mapRect.Top, _mapRect.Width, _mapRect.Height);
            #endregion

            _randSurfaces = new SurfaceCollection();
            _randRects = new Rectangle[_randItems.Length];
            ImageUtil.CreateStrMenu(_randItems, Constants.DefaultForeColor,
                ref _randSurfaces, ref _randRects, _randRect.Width);
            _randRects[_randRects.Length - 1].Offset(0, ResourceManager.SmallPFont.Height);

            _escSurfaces = new SurfaceCollection();
            _escRects = new Rectangle[_escItems.Length];
            ImageUtil.CreateStrMenu(_escItems, Constants.DefaultStrongColor, ResourceManager.MiddlePFont,
                ref _escSurfaces, ref _escRects, _escRect.Width, ResourceManager.MiddlePFont.Height);
        }

        public override void Init(PitchPitch parent)
        {
            base.Init(parent);

            // カーソル位置のリセット(タイトル画面に戻った時のみ)
            if (_escFocus)
            {
                _mapFocus = true;
                _mapSelectedIdx = 0;
                _escFocus = false;
            }

            // マップ情報の更新(マップ情報が無い場合のみ)
            if (_mapInfos.Count == 0)
                updateMapInfos();
        }

        /// <summary>
        /// マップ情報読み込み
        /// </summary>
        private void updateMapInfos()
        {
            _mapInfos.Clear();
            _mapInfos = _loader.LoadMapInfos();

            string[] infoStrs = Array.ConvertAll<MapInfo, string>(_mapInfos.ToArray(),
                (mi) => { return mi.MapName; });

            _mapSurfaces = new SurfaceCollection();
            _mapRects = new Rectangle[infoStrs.Length];
            ImageUtil.CreateStrMenu(infoStrs, Constants.DefaultForeColor,
                ref _mapSurfaces, ref _mapRects, _mapRect.Width);

            int ih = 30;
            if (_mapRects.Length > 0) ih = _mapRects[0].Height;
            _mapDrawLength = (int)Math.Floor(_mapRect.Height / (double)ih);
            if (_mapDrawLength > _mapInfos.Count) _mapDrawLength = _mapInfos.Count;

            _mapDrawFirstIdx = -1;
            _mapSelectedIdx = 0;
            if (_mapInfos.Count == 0) _mapFocus = false;

            updateMapIndex();
        }

        /// <summary>
        /// 読み込んだマップ表示のスクロール処理
        /// </summary>
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
                (mi) => { return mi.MapName; });
            
            if (_mapDrawSurfaces != null)
                foreach (Surface s in _mapDrawSurfaces) s.Dispose();

            _mapDrawSurfaces = new SurfaceCollection();
            _mapDrawRects = new Rectangle[infoStrs.Length];
            ImageUtil.CreateStrMenu(infoStrs, Constants.DefaultForeColor,
                ref _mapDrawSurfaces, ref _mapDrawRects, _mapRect.Width);
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
                    else if (_escFocus)
                    {
                        _escFocus = false;
                        _randSelectedIdx = _randRects.Length - 1;
                    }
                    else
                    {
                        if (_randSelectedIdx == 0)
                        {
                            _escFocus = true;
                        }
                        else
                        {
                            _randSelectedIdx = (_randSelectedIdx - 1 < 0 ?
                                (_randSelectedIdx + _randRects.Length - 1) : _randSelectedIdx - 1) % _randRects.Length;
                        }
                    }
                    break;
                case Key.DownArrow:
                    if (_mapFocus && _mapRects.Length > 0)
                    {
                        _mapSelectedIdx = (_mapSelectedIdx + 1) % _mapRects.Length;
                        updateMapIndex();
                    }
                    else if (_escFocus)
                    {
                        _escFocus = false;
                        _randSelectedIdx = 0;
                    }
                    else
                    {
                        if (_randSelectedIdx == _randRects.Length - 1)
                        {
                            _escFocus = true;
                        }
                        else
                        {
                            _randSelectedIdx = (_randSelectedIdx + 1) % _randRects.Length;
                        }
                    }
                    break;
                case Key.RightArrow:
                case Key.LeftArrow:
                    if (_escFocus) _escFocus = false;
                    if (_mapFocus || _mapRects.Length > 0)
                    {
                        _mapFocus = !_mapFocus;
                        if (_mapFocus)
                        {
                            _mapSelectedIdx = _randSelectedIdx;
                            if (_mapSelectedIdx < 0) _mapSelectedIdx = 0;
                            if (_mapSelectedIdx > _mapRects.Length - 1) _mapSelectedIdx = _mapRects.Length - 1;
                        }
                        else if(!_escFocus)
                        {
                            _randSelectedIdx = _mapSelectedIdx;
                            if (_randSelectedIdx < 0) _randSelectedIdx = 0;
                            if (_randSelectedIdx > _randRects.Length - 1) _randSelectedIdx = _randRects.Length - 1;
                        }
                    }
                    break;
                case Key.Return:
                    if (_escFocus) idx = 0;
                    else idx = 1;
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
                case 0: // タイトルに戻る
                    _parent.EnterScene(SceneType.Title);
                    break;
                case 1: // メニュー項目選択
                    map.Map map = null;
                    if (_mapFocus)
                    {
                        #region 読み込みマップメニューの場合
                        MapInfo info = _mapInfos[_mapSelectedIdx];
                        try
                        {
                            map = _loader.LoadMap(info);
                            if (map != null)
                            {
                                _parent.EnterScene(scene.SceneType.GameStage, map);
                            }
                            else
                            {
                                SetAlert(true, Properties.Resources.Str_MapLoadError);
                            }
                        }
                        catch (MapLoadException mex)
                        {
                            SetAlert(true, mex.Message);
                        }
                        #endregion
                    }
                    else
                    {
                        #region ビルトインマップメニューの場合
                        if (_randSelectedIdx == _randItems.Length - 1)
                        {
                            // マップ再読み込み
                            updateMapInfos();
                        }
                        else if(_randSelectedIdx > 0 && _randSelectedIdx < _builtinMapInfos.Length)
                        {
                            MapInfo mi = _builtinMapInfos[_randSelectedIdx];
                            if (mi is RandomMap.RandomMapInfo)
                            {
                                map = new RandomMap(mi.Level);
                                _parent.EnterScene(scene.SceneType.GameStage, map);
                            }
                            else if (mi is EmptyMap.EmptyMapInfo)
                            {
                                map = new EmptyMap();
                                _parent.EnterScene(scene.SceneType.GameStage, map);
                            }
                            else if (mi is RandomEndlessMap.RandomEndlessMapInfo)
                            {
                                _parent.EnterScene(scene.SceneType.EndlessGameStage);
                            }
                        }
                        #endregion
                    }
                    break;
            }
        }

        protected override void proc(KeyboardEventArgs e) { }

        protected override void draw(SdlDotNet.Graphics.Surface s)
        {
            s.Fill(Constants.DefaultBackColor);

            // ヘッダ
            if (_headerSurface == null)
            {
                _headerSurface = ResourceManager.LargePFont.Render(Properties.Resources.HeaderTitle_MapSelect, 
                    Constants.DefaultStrongColor);
            }
            s.Blit(_headerSurface, new Point(Constants.HeaderX, Constants.HeaderY));

            // 説明文
            if (_expSurface == null)
            {
                _expSurface = ResourceManager.SmallPFont.Render(Properties.Resources.Explanation_MapSelect,
                    Constants.DefaultStrongColor);
            }
            s.Blit(_expSurface, _expRect.Location);

            // ビルトインマップメニュー
            ImageUtil.DrawSelections(s, _randSurfaces, _randRects, _cursor,
                _randRect.Location, ((_mapFocus || _escFocus) ? -1 : _randSelectedIdx), ImageAlign.MiddleLeft);

            // 読み込んだマップメニュー
            ImageUtil.DrawSelections(s, _mapDrawSurfaces, _mapDrawRects, _cursor,
                _mapRect.Location, (_mapFocus ? _mapSelectedIdx - _mapDrawFirstIdx : -1), ImageAlign.MiddleLeft);

            // タイトルに戻るメニュー
            ImageUtil.DrawSelections(s, _escSurfaces, _escRects, _strongCursor,
                _escRect.Location, (_escFocus ? 0 : -1), ImageAlign.MiddleLeft);
        }

        public override void Dispose()
        {
            if (_headerSurface != null) _headerSurface.Dispose();
            if (_randSurfaces != null) foreach (Surface s in _randSurfaces) s.Dispose();
            if (_mapDrawSurfaces != null) foreach (Surface s in _mapDrawSurfaces) s.Dispose();
            if (_mapSurfaces != null) foreach (Surface s in _mapSurfaces) s.Dispose();
            if (_escSurfaces != null) foreach (Surface s in _escSurfaces) s.Dispose();
            if (_expSurface != null) _expSurface.Dispose();
            base.Dispose();
        }
    }
}
