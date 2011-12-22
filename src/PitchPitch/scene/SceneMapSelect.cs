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
        private const int RET_TITLE_OK = 0;
        private const int RET_TITLE_CANCEL = 1;
        private const int MENU_SELECT = 2;
        private const int OCTAVE_CANCEL = 1;
        private const int OCTAVE_OK = 0;

        private SdlDotNet.Graphics.Sprites.AnimatedSprite _cursor;
        private SdlDotNet.Graphics.Sprites.AnimatedSprite _strongCursor;

        private MapLoader _loader;

        private Surface _headerSurface = null;
        private Surface _expSurface = null;
        private Surface _loadingSurface = null;
        private Surface _octaveSelectingSurface = null;
        private Surface _octaveUpSurface = null;
        private Surface _octaveDownSurface = null;

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

        private Map _loadedMap = null;
        private MapInfo _loadingMapInfo = null;
        private Exception _loadedException = null;
        private bool _loading = false;
        private bool _loadEnd = false;
        private bool _loadTransition = false;

        private bool _octaveSelecting = false;
        private int _octave = 0;
        private delegate void octaveSelected();
        private octaveSelected _octaveSelectedDel = null;

        public SceneMapSelect()
        {
            sceneType = scene.SceneType.MapSelect;

            _loader = new MapLoader();
            _loader.OnMapLoaded += (s, e) =>
            {
                _loadedMap = e.Map;
                _loadEnd = true;
                _loading = false;
            };
            _loader.OnMapLoadCanceled += (s, e) =>
            {
                _loadedException = e.Exception;
                _loadedMap = null;
                _loadingMapInfo = null;
                _loadEnd = true;
                _loading = false;
            };

            _mapInfos = new List<MapInfo>();

            _keys = new Key[]
            {
                Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow, Key.Return, Key.Escape,
                Key.One, Key.Two, Key.R
            };
            _builtinMapInfos = new MapInfo[]
            {
                EmptyMap.GetMapInfo(),
                EmptyFixedMap.GetMapInfo(),
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


            _cursor = ResourceManager.GetColoredCursorGraphic(Constants.Color_Foreground);
            _strongCursor = ResourceManager.GetColoredCursorGraphic(Constants.Color_Strong);

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
            ImageUtil.CreateStrMenu(_randItems, Constants.Color_Foreground,
                ref _randSurfaces, ref _randRects, _randRect.Width);
            _randRects[_randRects.Length - 1].Offset(0, ResourceManager.SmallPFont.Height);

            _escSurfaces = new SurfaceCollection();
            _escRects = new Rectangle[_escItems.Length];
            ImageUtil.CreateStrMenu(_escItems, Constants.Color_Strong, ResourceManager.MiddlePFont,
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

            _octaveSelecting = false;
            _octave = 0;
            _octaveSelectedDel = null;
            _loadTransition = false;

            // マップ情報の更新(マップ情報が無い場合のみ)
            if (_mapInfos.Count == 0)
                updateMapInfos();
        }

        public override void SetAlert(bool on, string message)
        {
            base.SetAlert(on, message);
            if (!on)
            {
                _cursor.Animate = true;

                _loading = false;
                _loadEnd = false;
                _loadingMapInfo = null;
                _loadedMap = null;
            }
            else
            {
                _cursor.Animate = false;
            }
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
            ImageUtil.CreateStrMenu(infoStrs, Constants.Color_Foreground,
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
            ImageUtil.CreateStrMenu(infoStrs, Constants.Color_Foreground,
                ref _mapDrawSurfaces, ref _mapDrawRects, _mapRect.Width);
        }

        private int procKeyDefault(Key key)
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
                        else if (!_escFocus)
                        {
                            _randSelectedIdx = _mapSelectedIdx;
                            if (_randSelectedIdx < 0) _randSelectedIdx = 0;
                            if (_randSelectedIdx > _randRects.Length - 1) _randSelectedIdx = _randRects.Length - 1;
                        }
                    }
                    break;
                case Key.Return:
                    if (_escFocus) idx = RET_TITLE_OK;
                    else idx = MENU_SELECT;
                    break;
                case Key.Escape:
                    idx = RET_TITLE_CANCEL;
                    break;
            }
            return idx;
        }
        private int procKeyOctaveSelect(Key key)
        {
            int idx = -1;
            switch (key)
            {
                case Key.UpArrow:
                    if(_octave < Constants.MaxOctave) _octave++;
                    break;
                case Key.DownArrow:
                    if (_octave > Constants.MinOctave) _octave--;
                    break;
                case Key.Escape:
                    idx = OCTAVE_CANCEL;
                    break;
                case Key.Return:
                    idx = OCTAVE_OK;
                    break;
            }
            return idx;
        }

        private void procMenuOctaveSelect(int idx)
        {
            switch (idx)
            {
                case OCTAVE_OK:
                    PlaySeOK();
                    if (_octaveSelectedDel != null)
                    {
                        _loadingMapInfo.OctaveLevel = _octave;
                        _octaveSelectedDel();
                    }
                    break;
                case OCTAVE_CANCEL:
                    PlaySeCancel();
                    _octave = 0;
                    _octaveSelecting = false;
                    _cursor.Animate = true;
                    break;
            }
        }
        private void procMenuDefault(int idx)
        {
            switch (idx)
            {
                case RET_TITLE_OK: // タイトルに戻る
                    PlaySeOK();
                    startTransition(() => { _parent.EnterScene(scene.SceneType.Title); });
                    break;
                case RET_TITLE_CANCEL: // タイトルに戻る
                    PlaySeCancel();
                    startTransition(() => { _parent.EnterScene(scene.SceneType.Title); });
                    break;
                case MENU_SELECT: // メニュー項目選択
                    PlaySeOK();
                    if (_mapFocus)
                    {
                        // ユーザマップメニューの場合
                        _loadingMapInfo = _mapInfos[_mapSelectedIdx];
                        loadMap();
                    }
                    else
                    {
                        // ビルトインマップメニューの場合
                        if (_randSelectedIdx == _randItems.Length - 1)
                        {
                            // マップ再読み込み
                            updateMapInfos();
                        }
                        else if (_randSelectedIdx >= 0 && _randSelectedIdx < _builtinMapInfos.Length)
                        {
                            _loadingMapInfo = _builtinMapInfos[_randSelectedIdx];
                            loadMap();
                        }
                    }
                    break;
            }
        }


        protected override int procKeyEvent(Key key)
        {
            if (_loadTransition) return -1;
            if (_octaveSelecting)
            {
                return procKeyOctaveSelect(key);
            }
            else
            {
                return procKeyDefault(key);
            }
        }
        protected override void procMenu(int idx)
        {
            if (_loadTransition) return;
            if (idx < 0) return;

            if (_octaveSelecting)
            {
                procMenuOctaveSelect(idx);
            }
            else
            {
                procMenuDefault(idx);
            }
        }

        protected void loadMap()
        {
            _octaveSelectedDel = () =>
            {
                try
                {
                    _octaveSelecting = false;
                    _octave = 0;
                    _cursor.Animate = true;
                    _loadEnd = false;
                    _loading = true;
                    _loadTransition = true;
                    _loader.LoadMap(_loadingMapInfo);
                }
                catch (MapLoadException mex)
                {
                    SetAlert(true, mex.Message);
                }
            };

            if (_loadingMapInfo.PitchType == PitchType.Fixed)
            {
                _cursor.Animate = false;
                _octaveSelecting = true;
            }
            else
            {
                _octaveSelectedDel();
            }
        }

        protected override void proc(KeyboardEventArgs e)
        {
            if (_loadEnd)
            {
                if (_loadingMapInfo == null)
                {
                    SetAlert(true, Properties.Resources.Str_MapLoadError);
                }
                else
                {
                    if (_loadingMapInfo.HasEnd)
                    {
                        if (_loadedMap != null)
                        {
                            _parent.EnterScene(scene.SceneType.GameStage, _loadedMap);
                        }
                        else
                        {
                            SetAlert(true, Properties.Resources.Str_MapLoadError);
                        }
                    }
                    else
                    {
                        _parent.EnterScene(scene.SceneType.EndlessGameStage);
                    }
                }
                _loadingMapInfo = null;
                _loadedMap = null;
                _loadEnd = false;
            }
        }


        private void drawLoading(SdlDotNet.Graphics.Surface s)
        {
            s.Fill(Constants.Color_Foreground);
            if (_loadingSurface == null)
            {
                _loadingSurface = ResourceManager.SmallPFont.Render(Properties.Resources.Str_MapLoading, Constants.Color_Background);
            }
            s.Blit(_loadingSurface, new Point(
                (int)(Constants.ScreenWidth / 2.0 - _loadingSurface.Width / 2.0),
                (int)(Constants.ScreenHeight / 2.0 - _loadingSurface.Height / 2.0)));
        }

        private void drawMaps(SdlDotNet.Graphics.Surface s)
        {
            s.Fill(Constants.Color_Background);

            // ヘッダ
            if (_headerSurface == null)
            {
                _headerSurface = ResourceManager.LargePFont.Render(Properties.Resources.HeaderTitle_MapSelect,
                    Constants.Color_Strong);
            }
            s.Blit(_headerSurface, new Point(Constants.HeaderX, Constants.HeaderY));

            // 説明文
            if (_expSurface == null)
            {
                _expSurface = ResourceManager.SmallPFont.Render(Properties.Resources.Explanation_MapSelect,
                    Constants.Color_Strong);
            }
            s.Blit(_expSurface, _expRect.Location);

            // ビルトインマップメニュー
            ImageUtil.DrawSelections(s, _randSurfaces, _randRects, _cursor,
                _randRect.Location, ((_mapFocus || _escFocus) ? -1 : _randSelectedIdx), MenuItemAlign.MiddleLeft);

            // 読み込んだマップメニュー
            ImageUtil.DrawSelections(s, _mapDrawSurfaces, _mapDrawRects, _cursor,
                _mapRect.Location, (_mapFocus ? _mapSelectedIdx - _mapDrawFirstIdx : -1), MenuItemAlign.MiddleLeft);

            // タイトルに戻るメニュー
            ImageUtil.DrawSelections(s, _escSurfaces, _escRects, _strongCursor,
                _escRect.Location, (_escFocus ? 0 : -1), MenuItemAlign.MiddleLeft);
        }

        private void drawOctaveSelecting(SdlDotNet.Graphics.Surface s)
        {
            if (_octaveSelectingSurface == null)
            {
                using (Surface ts = ImageUtil.CreateMultilineStringSurface(new string[] { 
                Properties.Resources.Str_OctaveSelecting,
                null, null, null,
                Properties.Resources.Str_OctaveSelecting_Operation },
                    ResourceManager.SmallPFont, Constants.Color_Background, TextAlign.Center))
                {
                    _octaveSelectingSurface = new Surface(
                        ts.Width + Constants.WindowPadding * 2,
                        ts.Height + Constants.WindowPadding * 2);

                    _octaveSelectingSurface.Fill(Constants.Color_Foreground);
                    _octaveSelectingSurface.Blit(ts, new Point(Constants.WindowPadding, Constants.WindowPadding));
                    _octaveSelectingSurface.Update();
                }

                _octaveUpSurface = ResourceManager.SmallPFont.Render("↑", Constants.Color_Background);
                _octaveDownSurface = ResourceManager.SmallPFont.Render("↓", Constants.Color_Background);
            }

            s.Blit(_octaveSelectingSurface, new Point(
                    (int)(Constants.ScreenWidth / 2.0 - _octaveSelectingSurface.Width / 2.0),
                    (int)(Constants.ScreenHeight / 2.0 - _octaveSelectingSurface.Height / 2.0)));

            int fh = (int)(ResourceManager.SmallPFont.Height * Constants.LineHeight);
            int y = Constants.WindowPadding + 
                (int)(Constants.ScreenHeight / 2.0 - _octaveSelectingSurface.Height / 2.0) + fh;

            if (_octave < Constants.MaxOctave)
            {
                s.Blit(_octaveUpSurface, new Point((int)(Constants.ScreenWidth / 2.0 - _octaveUpSurface.Width / 2.0), y));
            }
            y += fh;

            using (Surface ts = ResourceManager.SmallPFont.Render(_octave.ToString(), Constants.Color_Background))
            {
                s.Blit(ts, new Point((int)(Constants.ScreenWidth / 2.0 - ts.Width / 2.0), y));
            }
            y += fh;

            if (_octave > Constants.MinOctave)
            {
                s.Blit(_octaveDownSurface, new Point((int)(Constants.ScreenWidth / 2.0 - _octaveDownSurface.Width / 2.0), y));
            }
        }

        protected override void draw(SdlDotNet.Graphics.Surface s)
        {
            if (_loading)
            {
                drawLoading(s);
            }
            else
            {
                drawMaps(s);
                if (_octaveSelecting)
                {
                    drawOctaveSelecting(s);
                }
            }
        }

        public override void Dispose()
        {
            if (_headerSurface != null) _headerSurface.Dispose();
            if (_randSurfaces != null) foreach (Surface s in _randSurfaces) s.Dispose();
            if (_mapDrawSurfaces != null) foreach (Surface s in _mapDrawSurfaces) s.Dispose();
            if (_mapSurfaces != null) foreach (Surface s in _mapSurfaces) s.Dispose();
            if (_escSurfaces != null) foreach (Surface s in _escSurfaces) s.Dispose();
            if (_expSurface != null) _expSurface.Dispose();
            if (_loadingSurface != null) _loadingSurface.Dispose();
            if (_octaveUpSurface != null) _octaveUpSurface.Dispose();
            if (_octaveDownSurface != null) _octaveDownSurface.Dispose();
            if (_octaveSelectingSurface != null) _octaveSelectingSurface.Dispose();
            base.Dispose();
        }
    }
}
