using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;

using SdlDotNet.Graphics;
using SdlDotNet.Graphics.Primitives;
using SdlDotNet.Input;

using PitchPitch.audio;

namespace PitchPitch.scene
{
    using MenuItem = KeyValuePair<Key, string>;

    class SceneGameStage : Scene
    {
        protected map.View _view = new map.View();
        protected map.Map _map;
        public map.Map Map
        {
            get { return _map; }
            set { _map = value; }
        }

        private ToneResult _toneResult;

        #region レイアウト
        protected int _margin = 10;
        protected Rectangle _keyRect;
        protected Rectangle _playerInfoRect;
        protected Rectangle _miniMapRect;
        protected Rectangle _viewRect;
        #endregion
        protected List<KeyValuePair<Rectangle, string>> _whiteKeys = null;
        protected List<KeyValuePair<Rectangle, string>> _blackKeys = null;
        protected Surface _keyboardSurface = null;
        protected Surface _mapSurface = null;

        #region メニュー
        protected SurfaceCollection _pauseMenuSurfaces = null;
        protected Rectangle[] _pauseMenuRects = null;
        protected int _pauseSelectedIdx = 0;
        protected MenuItem[] _pauseMenuItems;

        protected SurfaceCollection _clearMenuSurfaces = null;
        protected Rectangle[] _clearMenuRects = null;
        protected int _clearSelectedIdx = 0;
        protected MenuItem[] _clearMenuItems;
        #endregion

        #region 色
        protected Color _foreColor = Color.Black;
        public Color ForeColor
        {
            get { return _foreColor; }
            set
            {
                _foreColor = value;
                if (_map is map.RandomMap) (_map as map.RandomMap).ForeColor = _foreColor;
                else if (_map is map.RandomEndlessMap) (_map as map.RandomEndlessMap).ForeColor = _foreColor;
                _parent.Player.ForeColor = _foreColor;
                _foreCursor = ResourceManager.GetColoredCursorGraphic(_foreColor);
            }
        }
        protected Color _backColor = Color.White;
        public Color BackColor
        {
            get { return _backColor; }
            set
            {
                _backColor = value;
                _map.BackColor = _backColor;
                _parent.Player.ExplosionColor = _backColor;
                _backCursor = ResourceManager.GetColoredCursorGraphic(_backColor);
            }
        }
        #endregion

        protected SdlDotNet.Graphics.Sprites.AnimatedSprite _foreCursor = null;
        protected SdlDotNet.Graphics.Sprites.AnimatedSprite _backCursor = null;

        /// <summary>Viewの中でのプレイヤー位置(左端=0, 右端=1)</summary>
        protected double _playerXRatio = 0.2;
        /// <summary>マップの前後、衝突判定の無い部分</summary>
        protected int _mapMargin = 500;
        /// <summary>クリアしたかどうか</summary>
        protected bool _isCleared = false;
        /// <summary>ポーズ中かどうか</summary>
        protected bool _isPaused = false;
        public bool IsPaused
        {
            get { return _isPaused; }
            set
            {
                _isPaused = value;
                _parent.Player.IsPaused = value;
            }
        }

        public SceneGameStage(map.Map map)
        {
            sceneType = SceneType.GameStage;
            _map = map;

            _pauseMenuItems = new MenuItem[]
            {
                new MenuItem(Key.Escape, "Resume Game"),
                new MenuItem(Key.R, "Retry Stage"),
                new MenuItem(Key.M, "Select Map"),
                new MenuItem(Key.T, "Return to Title")
            };
            _clearMenuItems = new MenuItem[]
            {
                new MenuItem(Key.M, "Select Map"),
                new MenuItem(Key.T, "Return to Title")
            };

            _keys = new Key[]
            {
                Key.UpArrow, Key.DownArrow, Key.Return,
                Key.Escape, Key.R, Key.M, Key.T
            };
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <param name="parent">ゲームクラス</param>
        public override void Init(PitchPitch parent)
        {
            base.Init(parent);
            
            parent.Player.Init(parent);

            IsPaused = false;
            _isCleared = false;
            _pauseSelectedIdx = 0;
            _clearSelectedIdx = 0;

            #region レイアウト決定
            #region 配置
            int mmw = 300; int mmh = 100; int kw = 80;
            _keyRect = new Rectangle(
                _margin, _margin,
                kw, parent.Size.Height - mmh - _margin * 3);
            _viewRect = new Rectangle(
                _keyRect.Right + 1, _margin,
                parent.Size.Width - _keyRect.Right - 1 - _margin, _keyRect.Height);
            _miniMapRect = new Rectangle(
                parent.Size.Width - _margin - mmw, parent.Size.Height - _margin - mmh,
                mmw, mmh);
            _playerInfoRect = new Rectangle(
                _margin, _keyRect.Bottom + _margin,
                parent.Size.Width - mmw - _margin * 3, mmh);
            #endregion

            #region View/Map/Player
            _mapMargin = (int)(_viewRect.Width * 3 / 4.0);

            _view.Width = _viewRect.Width;
            _view.Height = _viewRect.Height;

            _map.Init(parent, new Size(_view.Width, _view.Height));
            _view.X = -_mapMargin;
            _view.Y = (long)(_map.Height / 2.0 - _view.Height / 2.0);
            _map.SetView(_view);

            _parent.Player.X = (int)(_playerXRatio * _view.Width) - _mapMargin;
            _parent.Player.Y = (int)(_map.Height / 2.0);
            _parent.Player.Vx = _map.MapInfo.PlayerVx;

            _view.X = -_mapMargin;

            ForeColor = _map.MapInfo.ForeColor;
            BackColor = _map.MapInfo.BackColor;
            #endregion
            #endregion

            #region メニュー作成
            _pauseMenuSurfaces = new SurfaceCollection(); _pauseMenuRects = new Rectangle[_pauseMenuItems.Length];
            ImageManager.CreateStrMenu(_pauseMenuItems, _backColor, ref _pauseMenuSurfaces, ref _pauseMenuRects);

            _clearMenuSurfaces = new SurfaceCollection(); _clearMenuRects = new Rectangle[_clearMenuItems.Length];
            ImageManager.CreateStrMenu(_clearMenuItems, _foreColor, ref _clearMenuSurfaces, ref _clearMenuRects);
            #endregion

            _minFreq = Math.Log(Config.Instance.MinFreq);
            _maxFreq = Math.Log(Config.Instance.MaxFreq);
            
            createKeyRects();
            if (_mapSurface != null)
            {
                _mapSurface.Dispose();
                _mapSurface = null;
            }

            #region PID制御係数決定
            _prevYDiff = 0;
            _yDiff = 0;
            _coefP = 0.5;
            _coefI = _coefP * 0.4;
            _coefD = _coefP * 0.01;
            _diffT = 1 / (double)SdlDotNet.Core.Events.TargetFps;
            #endregion
        }

        #region 鍵盤作成
        private void adjustWhiteKey(int idx, double y, double dy, out int ny, out int nh)
        {
            ny = (int)y; nh = (int)dy;

            #region 白鍵のずれ
            switch (idx)
            {
                case 0:
                    ny = (int)(y - dy / 2.0 - dy * 2 / 3.0);
                    nh = (int)(dy * 5 / 3.0);
                    break;
                case 2:
                    ny = (int)(y - dy / 2.0 - dy / 3.0);
                    nh = (int)(dy * 5 / 3.0);
                    break;
                case 4:
                    ny = (int)(y - dy / 2.0);
                    nh = (int)(dy * 5 / 3.0);
                    break;

                case 5:
                    ny = (int)(y - dy / 2.0 - dy * 3 / 4.0);
                    nh = (int)(dy * 7 / 4.0);
                    break;
                case 7:
                    ny = (int)(y - dy);
                    nh = (int)(dy * 7 / 4.0);
                    break;
                case 9:
                    ny = (int)(y - dy / 2.0 - dy / 4.0);
                    nh = (int)(dy * 7 / 4.0);
                    break;
                case 11:
                    ny = (int)(y - dy / 2.0);
                    nh = (int)(dy * 7 / 4.0);
                    break;
            }
            #endregion
        }
        protected void createKeyRects()
        {
            _whiteKeys = new List<KeyValuePair<Rectangle, string>>();
            _blackKeys = new List<KeyValuePair<Rectangle, string>>();
            if (_keyboardSurface != null) _keyboardSurface.Dispose();
            _keyboardSurface = null;

            ToneAnalyzer analyzer = _parent.AudioInput.ToneAnalyzer;
            ToneResult maxTone = analyzer.Analyze(Config.Instance.MaxFreq, 1.0);
            ToneResult minTone = analyzer.Analyze(Config.Instance.MinFreq, 1.0);

            double nearMaxFreq = Math.Log(maxTone.Pitch - maxTone.PitchDiff);
            double nearMinFreq = Math.Log(minTone.Pitch - minTone.PitchDiff);

            double maxY = (nearMaxFreq - _minFreq) / (_maxFreq - _minFreq);
            double minY = (nearMinFreq - _minFreq) / (_maxFreq - _minFreq);
            maxY = _view.Height - maxY * _view.Height;
            minY = _view.Height - minY * _view.Height;
            Console.WriteLine("{0} {1}, {2}, {3}",
                (maxTone.Pitch - maxTone.PitchDiff),
                nearMaxFreq,
                (minTone.Pitch - minTone.PitchDiff),
                nearMinFreq);

            int num = 12 * maxTone.Octave + maxTone.ToneIdx -
                (12 * minTone.Octave + minTone.ToneIdx);
            double dy = (minY - maxY) / (double)num;

            double y = minY;
            int toneIdx = minTone.ToneIdx;
            int oct = minTone.Octave;

            num += 2;
            toneIdx--;
            if (toneIdx < 0)
            {
                toneIdx += 12;
                oct--;
            }
            y += dy;

            int bw = (int)(_keyRect.Width / 2.0);
            int[] blackIdx = { 1, 3, 6, 8, 10 };
            for (int i = 0; i < num; i++)
            {
                string str = string.Format("{0}{1}", ToneAnalyzer.ToneNames[toneIdx], oct);

                if (Array.IndexOf(blackIdx, toneIdx) < 0)
                {
                    // 白鍵
                    int nh, ny;
                    adjustWhiteKey(toneIdx, y, dy, out ny, out nh);
                    if (_whiteKeys.Count > 0)
                    {
                        Rectangle prevR = _whiteKeys[_whiteKeys.Count - 1].Key;
                        nh = prevR.Top - ny;
                    }
                    _whiteKeys.Add(new KeyValuePair<Rectangle, string>(
                        new Rectangle(0, ny, _keyRect.Width - 1, nh),
                        str));
                }
                else
                {
                    // 黒鍵
                    _blackKeys.Add(new KeyValuePair<Rectangle, string>(
                        new Rectangle(0, (int)(y - dy / 2.0), bw, (int)dy),
                        str));
                }
                
                y -= dy;
                toneIdx++;
                if (toneIdx >= 12) { toneIdx = 0; oct++; }
            }
        }
        #endregion

        #region 更新

        #region 自機位置更新
        private const double _vxUnit = 0.0166666666666667;
        private double _minFreq = Math.Log(220);
        private double _maxFreq = Math.Log(880);

        private double _prevYDiff = 0;
        private double _yDiff = 0;
        private double _integral = 0;
        private double _coefP = 0.3;
        private double _coefI = 0;
        private double _coefD = 0;
        private double _diffT = 1; // sec
        private int _maxDiffY = 10;
        protected void updatePlayerPos()
        {
            gameobj.Player player = _parent.Player;

            ToneResult tmp = _parent.ToneResult;
            double pitch = Math.Log(tmp.Pitch);
            double clarity = tmp.Clarity;

            double target = player.Y;
            bool soundOn = false;
            if (_parent.AudioInput.Capturing)
            {
                if (clarity >= 0.90 && clarity <= 1.00)
                    soundOn = true;
            }

            if (soundOn)
            {
                _toneResult = tmp;

                double yr = (pitch - _minFreq) / (_maxFreq - _minFreq);
                target = _view.Height - yr * _view.Height + _view.Y;

                _prevYDiff = _yDiff;
                _yDiff = player.Y - target;
                _integral += _diffT * (_prevYDiff + _yDiff) / 2.0;

                double p = _coefP * _yDiff;
                double i = _coefI * _integral;
                double d = _coefD * (_yDiff - _prevYDiff) / _diffT;
                int pid = (int)(p + i + d);
                player.Y -= (pid > _maxDiffY ? _maxDiffY : (pid < -_maxDiffY ? -_maxDiffY : pid));

                player.Rad -= player.RadDec;
            }
            else
            {
                _prevYDiff = 0; _yDiff = 0;
                player.Rad += player.RadInc;
            }

            #region デバッグ用
            if (Keyboard.IsKeyPressed(Key.UpArrow))
                player.Y--;
            else if (Keyboard.IsKeyPressed(Key.DownArrow))
                player.Y++;
            if (Keyboard.IsKeyPressed(Key.Space))
                player.Rad -= player.RadDec;
            if (Keyboard.IsKeyPressed(Key.RightArrow))
                player.X += 1;
            #endregion

            if (player.Y < _view.Y) player.Y = (int)(_view.Y);
            if (player.Y > _view.Height + _view.Y) player.Y = (int)(_view.Height + _view.Y);

            player.X += (long)player.Vx;
        }
        #endregion

        #region View位置更新
        protected void updateView()
        {
            _view.X = (long)(_parent.Player.X - _playerXRatio * _view.Width);
            _view.Y = (long)(_map.Height / 2.0 - _view.Height / 2.0);

            if (_view.X < -_mapMargin) _view.X = -_mapMargin;
            if (_view.Y < 0) _view.Y = 0;
            if (_map.HasEnd)
            {
                if (_view.X + _view.Width > _map.Width + _mapMargin) _view.X = _map.Width + _mapMargin - _view.Width;
            }
            if (_view.Y + _view.Height > _map.Height) _view.Y = _map.Height - _view.Height;
        }
        #endregion

        #region 衝突判定
        protected void hitTest()
        {
            int mw = _map.ChipData.ChipWidth;
            int mh = _map.ChipData.ChipHeight;
            foreach (map.Chip chip in _map.EnumViewChipData())
            {
                if (chip.Hardness <= 0) continue;

                Point pp = convertToViewCoord(_parent.Player.X, _parent.Player.Y);

                if (_parent.Player.Hit(chip, pp, mw, mh))
                {
                    _parent.Player.Y = (int)_map.GetDefaultY(pp.X);
                    _parent.Player.Rad = _parent.Player.MinRadius;
                    break;
                }
            }
        }
        #endregion

        #region キー処理
        protected virtual void procMenuCleared(int idx)
        {
            switch (idx)
            {
                case 0:
                    _parent.EnterScene(SceneType.MapSelect);
                    break;
                case 1:
                    _parent.EnterScene(scene.SceneType.Title);
                    break;
            }
        }
        protected virtual int procKeyCleared(Key key)
        {
            int idx = -1;
            switch (key)
            {
                case Key.UpArrow:
                    _clearSelectedIdx = (_clearSelectedIdx - 1 < 0 ?
                        (_clearSelectedIdx + _pauseMenuRects.Length - 1) :
                        _clearSelectedIdx - 1) % _clearMenuItems.Length;
                    break;
                case Key.DownArrow:
                    _clearSelectedIdx = (_clearSelectedIdx + 1) % _clearMenuItems.Length;
                    break;
                case Key.Return:
                    idx = _clearSelectedIdx; break;
                default:
                    idx = 0;
                    foreach (MenuItem mi in _clearMenuItems)
                    {
                        if (mi.Key == key) break;
                        idx++;
                    }
                    break;
            }
            return idx;
        }

        protected virtual void procMenuPaused(int idx)
        {
            switch (idx)
            {
                case 0: IsPaused = false; break;
                case 1: _parent.RetryMap(); break;
                case 2:
                    _parent.EnterScene(scene.SceneType.MapSelect);
                    break;
                case 3:
                    _parent.EnterScene(scene.SceneType.Title);
                    break;
            }
        }
        protected virtual int procKeyPaused(Key key)
        {
            int idx = -1;
            switch (key)
            {
                case Key.UpArrow:
                    _pauseSelectedIdx = (_pauseSelectedIdx - 1 < 0 ?
                        (_pauseSelectedIdx + _pauseMenuRects.Length - 1) :
                        _pauseSelectedIdx - 1) % _pauseMenuItems.Length;
                    break;
                case Key.DownArrow:
                    _pauseSelectedIdx = (_pauseSelectedIdx + 1) % _pauseMenuItems.Length;
                    break;
                case Key.Return:
                    idx = _pauseSelectedIdx; break;
                default:
                    idx = 0;
                    foreach (MenuItem mi in _pauseMenuItems)
                    {
                        if (mi.Key == key) break;
                        idx++;
                    }
                    break;
            }
            return idx;
        }

        protected virtual void procKeyDefault(Key key)
        {
            switch (key)
            {
                case Key.Escape: IsPaused = true; break;
            }
        }

        protected override int procKeyEvent(Key key)
        {
            if (_isCleared)
            {
                return procKeyCleared(key);
            }
            else if (_isPaused)
            {
                return procKeyPaused(key);
            }
            else
            {
                procKeyDefault(key);
                return -1;
            }
        }
        protected override void procMenu(int idx)
        {
            if (idx < 0) return;
            if (_isCleared)
            {
                procMenuCleared(idx);
            }
            else if (_isPaused)
            {
                procMenuPaused(idx);
            }
        }
        #endregion

        /// <summary>
        /// 更新処理
        /// </summary>
        public override void Process(KeyboardEventArgs e)
        {
            base.Process(e);

            if (_isPaused)
            {

            }
            else
            {
                // 自機の位置更新
                if (_isCleared && (_map.HasEnd && _parent.Player.X > _map.Width + _parent.Player.Width + _mapMargin))
                {
                    // クリア済み
                }
                else
                {
                    updatePlayerPos();
                }

                // ビューの更新
                updateView();
                _map.SetView(_view);


                if (_map.HasEnd && _parent.Player.X > _map.Width)
                {
                    // Clear
                    _isCleared = true;
                }
                else if(_parent.Player.X < 0)
                {
                    // not started
                }
                else
                {
                    // 衝突判定
                    hitTest();
                }

                // ゲームオーバー判定
                if (_parent.Player.Hp <= 0)
                {
                    _parent.EnterScene(SceneType.GameOver);
                }
            }
        }
        #endregion

        #region 描画

        /// <summary>
        /// 画面の描画処理
        /// </summary>
        /// <param name="s">画面</param>
        public override void Draw(Surface s)
        {
            // 背景枠描画
            s.Fill(_foreColor);

            using (Surface viewSurface = new Surface(_viewRect.Size.Width, _viewRect.Size.Height, 32))
            {
                // マップ描画
                _map.Render(viewSurface);

                // プレイヤー描画
                _parent.Player.Render(viewSurface, convertToViewCoord(_parent.Player.X, _parent.Player.Y));

                if (_isPaused)
                {
                    // ポーズ画面描画
                    renderMenu(viewSurface);
                }
                else if (_isCleared)
                {
                    // クリア画面描画
                    renderClear(viewSurface);
                }
                s.Blit(viewSurface, _viewRect.Location);
            }

            // 鍵盤描画
            renderKeyboard(s);

            // ミニマップ描画
            renderMiniMap(s);

            using (Surface infoSurface = new Surface(_playerInfoRect.Size))
            {
                // プレイヤー情報描画
                renderPlayerInformation(infoSurface);
                s.Blit(infoSurface, _playerInfoRect.Location);
            }
        }

        /// <summary>ポーズ中のメニュー画面を描画する</summary>
        protected virtual void renderMenu(Surface s)
        {
            using (Surface vs = new Surface(s.Size))
            {
                vs.Alpha = 150;
                vs.AlphaBlending = true;
                vs.Fill(_foreColor);
                s.Blit(vs, Point.Empty);
            }
            int y = 10;
            int lh = ResourceManager.LargePFont.Height; int sh = ResourceManager.SmallPFont.Height;
            s.Blit(ResourceManager.LargePFont.Render("PAUSE", _backColor), new Point(10, y));

            y += lh + 5;
            ImageManager.DrawSelections(s, _pauseMenuSurfaces, _pauseMenuRects, 
                _backCursor, new Point(40, y), _pauseSelectedIdx, ImageAlign.MiddleLeft);
        }

        /// <summary>クリア画面を描画する</summary>
        protected virtual void renderClear(Surface s)
        {
            using (Surface vs = new Surface(s.Size))
            {
                vs.Alpha = 150;
                vs.AlphaBlending = true;
                vs.Fill(_backColor);
                s.Blit(vs, Point.Empty);
            }
            int y = 10;
            int lh = ResourceManager.LargePFont.Height; int sh = ResourceManager.SmallPFont.Height;
            s.Blit(ResourceManager.LargePFont.Render("CLEAR", _foreColor), new Point(10, y));
            y += lh + 5;
            ImageManager.DrawSelections(s, _clearMenuSurfaces, _clearMenuRects,
                _foreCursor, new Point(40, y), _clearSelectedIdx, ImageAlign.MiddleLeft);
        }

        protected virtual void renderPlayerInformation(Surface s)
        {
            s.Fill(_foreColor);

            int nline = 3;
            int th = ResourceManager.SmallTTFont.Height * nline + 2 * (nline - 1);
            int m = (int)((s.Height - th) / 2.0);

            int x = _margin;
            int y = m;
            s.Blit(ResourceManager.SmallTTFont.Render(string.Format("Life: {0} / {1}", _parent.Player.Hp, _parent.Player.MaxHp), _backColor), new Point(x, y));
            y += ResourceManager.SmallTTFont.Height + 2;
            s.Blit(ResourceManager.SmallTTFont.Render(string.Format("距離: {0} / {1}", _parent.Player.X, _map.Width), _backColor), new Point(x, y));
            y += ResourceManager.SmallTTFont.Height + 2;
            s.Blit(ResourceManager.SmallTTFont.Render(string.Format("{0}{1} - {2} Hz", _toneResult.Tone, _toneResult.Octave, _toneResult.Pitch), _backColor), new Point(x, y));
        }

        protected virtual void renderKeyboard(Surface s)
        {
            if (_keyboardSurface == null)
            {
                _keyboardSurface = new Surface(_keyRect.Size);
                foreach (KeyValuePair<Rectangle, string> kv in _whiteKeys)
                {
                    Box box = new Box(kv.Key.Location, kv.Key.Size);
                    _keyboardSurface.Draw(box, _backColor, true, true);
                    _keyboardSurface.Draw(box, _foreColor, true, false);
                }
                foreach (KeyValuePair<Rectangle, string> kv in _blackKeys)
                {
                    Box box = new Box(kv.Key.Location, kv.Key.Size);
                    _keyboardSurface.Draw(box, _foreColor, true, true);
                }
            }
            s.Blit(_keyboardSurface, _keyRect.Location);

            bool black = false;
            foreach (KeyValuePair<Rectangle, string> kv in _blackKeys)
            {
                if (kv.Key.Top <= _parent.Player.Y - _view.Y && 
                    _parent.Player.Y - _view.Y <= kv.Key.Bottom)
                {
                    Rectangle r = new Rectangle(kv.Key.Location, kv.Key.Size);
                    r.Offset(_keyRect.Location);
                    Circle cir = new Circle(new Point(r.Right - 14, (int)(r.Y + r.Height / 2.0)), 6);
                    s.Draw(cir, _backColor, true, true);
                    s.Draw(cir, _foreColor, true, false);
                    black = true;
                    break;
                }
            }
            if (!black)
            {
                foreach (KeyValuePair<Rectangle, string> kv in _whiteKeys)
                {
                    if (kv.Key.Top <= _parent.Player.Y - _view.Y &&
                        _parent.Player.Y - _view.Y <= kv.Key.Bottom)
                    {
                        Rectangle r = new Rectangle(kv.Key.Location, kv.Key.Size);
                        r.Offset(_keyRect.Location);
                        Circle cir = new Circle(new Point(r.Right - 14, (int)(r.Y + r.Height / 2.0)), 6);
                        s.Draw(cir, _foreColor, true, true);
                        s.Draw(cir, _backColor, true, false);
                        break;
                    }
                }
            }
        }

        protected virtual void renderMiniMap(Surface s)
        {
            if (_mapSurface == null)
            {
                _mapSurface = new Surface(_miniMapRect.Size);
                _map.RenderMiniMap(_mapSurface);
            }
            s.Blit(_mapSurface, _miniMapRect.Location);
        }
        #endregion

        protected Point convertToViewCoord(long x, long y)
        {
            return new Point((int)(x - _view.X), (int)(y - _view.Y));
        }

        public override void Dispose()
        {
            if (_keyboardSurface != null) _keyboardSurface.Dispose();
            if (_mapSurface != null) _mapSurface.Dispose();
            if (_map != null) _map.Dispose();
            base.Dispose();
        }
    }
}
