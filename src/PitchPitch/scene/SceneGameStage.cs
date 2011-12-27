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
    class SceneGameStage : Scene
    {
        #region ゲーム処理関係
        protected map.View _view = new map.View();
        protected map.Map _map;
        public map.Map Map
        {
            get { return _map; }
            set { _map = value; }
        }

        protected bool _isFixedPitchMap = false;

        protected double _prevX = double.MinValue;

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
                _prevProcTime = Environment.TickCount;

                if (_map.Bgm != null)
                {
                    if (_isPaused && SdlDotNet.Audio.MusicPlayer.IsPlaying)
                    {
                        SdlDotNet.Audio.MusicPlayer.Pause();
                    }
                    else
                    {
                        SdlDotNet.Audio.MusicPlayer.Resume();
                    }
                }
            }
        }

        /// <summary>フラグが立ったら次のフレームでゲームオーバー処理</summary>
        protected bool _isOver = false;
        #endregion

        #region 描画関係

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
        protected Surface _playerInfoSurface = null;

        protected Surface _pauseSurface = null;
        protected Surface _clearSurface = null;

        protected SurfaceCollection _lifeSurfaces = null;
        protected SurfaceCollection _coloredLifeSurfaces = null;

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
        protected Color _strongColor = Color.Red;
        protected Color _backColor = Color.White;
        protected void initColor(Color fore, Color back, Color strong)
        {
            _foreColor = fore;
            _backColor = back;
            _strongColor = strong;

            _map.ForeColor = _foreColor;
            _map.BackColor = _backColor;
            _map.StrongColor = _strongColor;

            _parent.Player.ForeColor = _foreColor;
            _parent.Player.ExplosionColor = _backColor;

            _foreCursor = ResourceManager.GetColoredCursorGraphic(_foreColor);
            _backCursor = ResourceManager.GetColoredCursorGraphic(_backColor);

            if (_keyboardSurface != null) { _keyboardSurface.Dispose(); _keyboardSurface = null; }
            if (_mapSurface != null) { _mapSurface.Dispose(); _mapSurface = null; }
            if (_pauseSurface != null) { _pauseSurface.Dispose(); _pauseSurface = null; }
            if (_clearSurface != null) { _clearSurface.Dispose(); _clearSurface = null; }
            if (_playerInfoSurface != null) { _playerInfoSurface.Dispose(); _playerInfoSurface = null; }

            if (_coloredLifeSurfaces != null)
            {
                foreach (Surface s in _coloredLifeSurfaces) s.Dispose();
                _coloredLifeSurfaces = null;
            }
            _coloredLifeSurfaces = new SurfaceCollection();
            foreach (Surface s in _lifeSurfaces)
            {
                _coloredLifeSurfaces.Add(ImageUtil.CreateColored(s, _backColor, _strongColor));
            }
        }
        #endregion

        protected SdlDotNet.Graphics.Sprites.AnimatedSprite _foreCursor = null;
        protected SdlDotNet.Graphics.Sprites.AnimatedSprite _backCursor = null;
        #endregion

        #region 音関係
        private ToneResult _toneResult;

        protected double _minFreq = 220;
        protected double _maxFreq = 880;
        protected double _minFreqLog = Math.Log(220);
        protected double _maxFreqLog = Math.Log(880);
        #endregion

        public SceneGameStage(map.Map map)
        {
            sceneType = SceneType.GameStage;
            _map = map;

            #region メニューアイテム
            _pauseMenuItems = new MenuItem[]
            {
                new MenuItem(Key.Escape, Properties.Resources.MenuItem_ResumeGame),
                new MenuItem(Key.R, Properties.Resources.MenuItem_RetryStage),
                new MenuItem(Key.M, Properties.Resources.MenuItem_MapSelect),
                new MenuItem(Key.T, Properties.Resources.MenuItem_ReturnTitle)
            };
            _clearMenuItems = new MenuItem[]
            {
                new MenuItem(Key.M, Properties.Resources.MenuItem_MapSelect),
                new MenuItem(Key.T, Properties.Resources.MenuItem_ReturnTitle)
            };
            #endregion

            _keys = new Key[]
            {
                Key.UpArrow, Key.DownArrow, Key.Return,
                Key.Escape, Key.R, Key.M, Key.T
            };

            // 画像読み込み
            _lifeSurfaces = ResourceManager.LoadSurfaces(Constants.Filename_LifeImage, new Size(30, 32));

            #region 配置
            _viewRect = new Rectangle(
                Constants.ScreenWidth - Constants.StageViewWidth - Constants.StageMargin,
                Constants.StageMargin,
                Constants.StageViewWidth,
                Constants.StageViewHeight);
            _keyRect = new Rectangle(
                Constants.StageMargin,
                Constants.StageMargin,
                _viewRect.Left - 1 - Constants.StageMargin,
                Constants.StageViewHeight);
            _miniMapRect = new Rectangle(
                Constants.ScreenWidth - Constants.MiniMapWidth - Constants.StageMargin,
                _viewRect.Bottom + Constants.StageGap,
                Constants.MiniMapWidth,
                Constants.ScreenHeight - Constants.StageMargin - _viewRect.Bottom - Constants.StageGap);
            _playerInfoRect = new Rectangle(
                Constants.StageMargin,
                _viewRect.Bottom + Constants.StageGap,
                _miniMapRect.Left - Constants.StageMargin - Constants.StageGap,
                _miniMapRect.Height);
            #endregion

            #region PID制御係数決定
            _prevYDiff = 0;
            _yDiff = 0;
            _coefP = 0.5;
            _coefI = _coefP * 0.4;
            _coefD = _coefP * 0.01;
            _diffT = 1 / (double)SdlDotNet.Core.Events.TargetFps;
            #endregion
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <param name="parent">ゲームクラス</param>
        public override void Init(PitchPitch parent)
        {
            base.Init(parent);
            
            parent.Player.Init(parent);

            // 各種フラグのリセット
            IsPaused = false;
            _isCleared = false;
            _isOver = false;
            _pauseSelectedIdx = 0;
            _clearSelectedIdx = 0;

            #region 色
            initColor(_map.MapInfo.ForegroundColor, _map.MapInfo.BackgroundColor, _map.MapInfo.StrongColor);
            #endregion

            #region View/Map/Player
            _mapMargin = (int)(_viewRect.Width * 3 / 4.0) * _map.MapInfo.PlayerVx;

            _view.Width = _viewRect.Width;
            _view.Height = _viewRect.Height;

            _map.Init(parent, new Size(_view.Width, _view.Height));
            _view.X = -_mapMargin;
            _view.Y = (long)(_map.Height / 2.0 - _view.Height / 2.0);
            _map.SetView(_view);

            _parent.Player.X = _playerXRatio * _view.Width - _mapMargin;
            _parent.Player.Y = _map.Height / 2.0;
            _parent.Player.Vx = _map.MapInfo.PlayerVx;
            _prevX = _parent.Player.X;

            _view.X = -_mapMargin;

            // BGM
            if (_map.Bgm != null)
            {
                SdlDotNet.Audio.MusicPlayer.Load(_map.Bgm);
                int ivol = (int)(128 * _map.BgmVolume / 100.0);
                ivol = ivol < 0 ? 0 : (ivol > 128 ? 128 : ivol);
                SdlDotNet.Audio.MusicPlayer.Volume = ivol;
            }
            #endregion

            #region メニュー作成
            _pauseMenuSurfaces = new SurfaceCollection(); _pauseMenuRects = new Rectangle[_pauseMenuItems.Length];
            ImageUtil.CreateStrMenu(_pauseMenuItems, _backColor, ref _pauseMenuSurfaces, ref _pauseMenuRects);

            _clearMenuSurfaces = new SurfaceCollection(); _clearMenuRects = new Rectangle[_clearMenuItems.Length];
            ImageUtil.CreateStrMenu(_clearMenuItems, _foreColor, ref _clearMenuSurfaces, ref _clearMenuRects);
            #endregion

            #region ピッチ設定
            if (_map.MapInfo.PitchType == map.PitchType.Fixed)
            {
                _isFixedPitchMap = true;
                _maxFreq = _map.MapInfo.MaxPitch;
                _minFreq = _map.MapInfo.MinPitch;
                if (_map.MapInfo.OctaveLevel != 0)
                {
                    _maxFreq *= Math.Pow(2, _map.MapInfo.OctaveLevel);
                    _minFreq *= Math.Pow(2, _map.MapInfo.OctaveLevel);
                }
            }
            else
            {
                _isFixedPitchMap = false;
                _minFreq = Config.Instance.MinFreq;
                _maxFreq = Config.Instance.MaxFreq;
            }
            _maxFreqLog = Math.Log(_maxFreq);
            _minFreqLog = Math.Log(_minFreq);
            #endregion

            createKeyRects();

            _prevProcTime = Environment.TickCount;
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

            ToneResult maxTone = ToneAnalyzer.Analyze(_maxFreq, 1.0);
            ToneResult minTone = ToneAnalyzer.Analyze(_minFreq, 1.0);

            double nearMaxFreq = Math.Log(maxTone.Pitch - maxTone.PitchDiff);
            double nearMinFreq = Math.Log(minTone.Pitch - minTone.PitchDiff);

            double maxY = (nearMaxFreq - _minFreqLog) / (_maxFreqLog - _minFreqLog);
            double minY = (nearMinFreq - _minFreqLog) / (_maxFreqLog - _minFreqLog);
            maxY = _view.Height - maxY * _view.Height;
            minY = _view.Height - minY * _view.Height;

            int num = 12 * maxTone.Octave + maxTone.ToneIdx -
                (12 * minTone.Octave + minTone.ToneIdx);
            double dy = (minY - maxY) / (double)num;

            double y = minY;
            int toneIdx = minTone.ToneIdx;
            int oct = minTone.Octave;

            num += 3;
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
        protected const double _vxUnit = 0.0166666666666667;

        protected double _prevYDiff = 0;
        protected double _yDiff = 0;
        protected double _integral = 0;
        protected double _coefP = 0.3;
        protected double _coefI = 0;
        protected double _coefD = 0;
        protected double _diffT = 1; // sec
        protected int _maxDiffY = 10;

        protected long _prevProcTime = 0;
        protected virtual void updatePlayerPos()
        {
            gameobj.Player player = _parent.Player;

            ToneResult tmp = _parent.ToneResult.Copy();
            double pitch = Math.Log(tmp.Pitch);
            double clarity = tmp.Clarity;

            double target = player.Y;
            bool soundOn = false;
            if (_parent.AudioInput.Capturing)
            {
                if (clarity >= ToneAnalyzer.ClarityThreshold &&
                    Constants.MinPitch <= tmp.Pitch && tmp.Pitch <= Constants.MaxPitch)
                {
                    soundOn = true;
                }
            }

            if (soundOn)
            {
                _toneResult = tmp;

                double yr = (pitch - _minFreqLog) / (_maxFreqLog - _minFreqLog);
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

            if (player.Y < _view.Y) player.Y = _view.Y;
            if (player.Y > _view.Height + _view.Y) player.Y = _view.Height + _view.Y;

            long tick = Environment.TickCount;
            player.X += player.Vx * (tick - _prevProcTime) * SdlDotNet.Core.Events.TargetFps / 1000.0;
            _prevProcTime = tick;
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

                PointD pp = convertToViewCoord(_parent.Player.X, _parent.Player.Y);

                if (_parent.Player.Hit(chip, pp, mw, mh))
                {
                    double plog = _minFreqLog + (_maxFreqLog - _minFreqLog) * (Constants.StageViewHeight - _parent.Player.Y) / (double)Constants.StageViewHeight;
                    double pitch = Math.Pow(Math.E, plog);
                    ToneResult tone = ToneAnalyzer.Analyze(pitch, 1.0);
                    ResourceManager.SoundExplosion[tone.ToneIdx.ToString("D2")].Play();

                    _parent.Player.Y = _map.GetDefaultY(pp.X);
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
                    {
                        if (_map.Bgm != null) SdlDotNet.Audio.MusicPlayer.Stop();
                        PlaySeOK();
                        startTransition(() => { _parent.EnterScene(scene.SceneType.MapSelect); });
                        break;
                    }
                case 1:
                    {
                        if (_map.Bgm != null) SdlDotNet.Audio.MusicPlayer.Stop();
                        PlaySeOK();
                        startTransition(() => { _parent.EnterScene(scene.SceneType.Title); });
                        break;
                    }
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
                case Key.Escape:
                    idx = 0; break;
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
                case 0:
                    {
                        PlaySeCancel();
                        IsPaused = false; break;
                    }
                case 1:
                    {
                        if (_map.Bgm != null) SdlDotNet.Audio.MusicPlayer.Stop();
                        PlaySeOK();
                        _parent.RetryMap(); break;
                    }
                case 2:
                    {
                        if (_map.Bgm != null) SdlDotNet.Audio.MusicPlayer.Stop();
                        PlaySeOK();
                        startTransition(() => { _parent.EnterScene(scene.SceneType.MapSelect); });
                        break;
                    }
                case 3:
                    {
                        if (_map.Bgm != null) SdlDotNet.Audio.MusicPlayer.Stop();
                        PlaySeOK();
                        startTransition(() => { _parent.EnterScene(scene.SceneType.Title); });
                        break;
                    }
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

        protected override void proc(KeyboardEventArgs e)
        {
            if (_isOver)
            {
                _parent.EnterScene(SceneType.GameOver);
            }
            else if (_isPaused)
            {

            }
            else
            {
                // 自機の位置更新
                if (_isCleared && (_map.HasEnd && _parent.Player.X > _map.Width + _parent.Player.Width + _mapMargin))
                {
                    // クリア済み + 十分な量右に進んだ

                    // end
                    if (_map.Bgm != null) SdlDotNet.Audio.MusicPlayer.Stop();
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
                else if (_parent.Player.X < 0)
                {
                    // not started
                }
                else
                {
                    // 衝突判定
                    hitTest();
                }

                if (_prevX < 0 && _parent.Player.X >= 0)
                {
                    // start
                    if (_map.Bgm != null) SdlDotNet.Audio.MusicPlayer.Play();
                }

                // ゲームオーバー判定
                if (_parent.Player.Hp <= 0)
                {
                    _isOver = true;
                    // end
                    if (_map.Bgm != null) SdlDotNet.Audio.MusicPlayer.Stop();
                }

                _prevX = _parent.Player.X;
            }
        }
        #endregion

        #region 描画

        /// <summary>
        /// 画面の描画処理
        /// </summary>
        /// <param name="s">画面</param>
        protected override void draw(Surface s)
        {
            // 背景枠描画
            s.Fill(_foreColor);

            using (Surface viewSurface = new Surface(_viewRect.Size.Width, _viewRect.Size.Height, 32))
            {
                // マップ描画
                _map.Render(viewSurface, new Rectangle(0, 0, _viewRect.Width, _viewRect.Height));

                // プレイヤー描画
                _parent.Player.Render(viewSurface, convertToViewCoord(_parent.Player.X, _parent.Player.Y).Round());

                if (_isPaused)
                {
                    // ポーズ画面描画
                    renderPause(viewSurface);
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

            // プレイヤー情報描画
            renderPlayerInformation(s);
        }

        /// <summary>ポーズ中画面を描画する</summary>
        protected virtual void renderPause(Surface s)
        {
            using (Surface vs = new Surface(s.Size))
            {
                vs.Alpha = 180;
                vs.AlphaBlending = true;
                vs.Fill(_foreColor);
                s.Blit(vs, Point.Empty);
            }

            if (_pauseSurface == null)
            {
                _pauseSurface = ResourceManager.LargePFont.Render(Properties.Resources.HeaderTitle_Pause, _backColor);
            }
            s.Blit(_pauseSurface, new Point(Constants.HeaderX, Constants.HeaderY));

            ImageUtil.DrawSelections(s, _pauseMenuSurfaces, _pauseMenuRects, 
                _backCursor, new Point(
                    Constants.HeaderX + Constants.UnderHeaderMargin + Constants.CursorMargin,
                    Constants.HeaderY + ResourceManager.LargePFont.Height + Constants.HeaderBottomMargin),
                    _pauseSelectedIdx, MenuItemAlign.MiddleLeft);
        }

        /// <summary>クリア画面を描画する</summary>
        protected virtual void renderClear(Surface s)
        {
            using (Surface vs = new Surface(s.Size))
            {
                vs.Alpha = 180;
                vs.AlphaBlending = true;
                vs.Fill(_backColor);
                s.Blit(vs, Point.Empty);
            }

            if (_clearSurface == null)
            {
                _clearSurface = ResourceManager.LargePFont.Render(Properties.Resources.HeaderTitle_Clear, _foreColor);
            }
            s.Blit(_clearSurface, new Point(Constants.HeaderX, Constants.HeaderY));

            ImageUtil.DrawSelections(s, _clearMenuSurfaces, _clearMenuRects,
                _foreCursor, new Point(
                    Constants.HeaderX + Constants.UnderHeaderMargin + Constants.CursorMargin,
                    Constants.HeaderY + ResourceManager.LargePFont.Height + Constants.HeaderBottomMargin), 
                    _clearSelectedIdx, MenuItemAlign.MiddleLeft);
        }

        private int _playerInfoHeaderMaxWidth = 0;
        protected virtual void renderPlayerInformation(Surface s)
        {
            #region ヘッダ部分初期化
            if (_playerInfoSurface == null)
            {
                _playerInfoHeaderMaxWidth = 0;
                _playerInfoSurface = new Surface(_playerInfoRect.Width, _playerInfoRect.Height);
                _playerInfoSurface.Fill(_foreColor);

                int dh = (int)((_playerInfoRect.Height - Constants.PlayerInfoPadding * 2) / 3.0);

                int x = Constants.PlayerInfoPadding;
                int y = Constants.PlayerInfoPadding + (int)(dh / 2.0 - ResourceManager.SmallPFont.Height / 2.0);

                using (Surface ts = ResourceManager.SmallPFont.Render(Properties.Resources.Str_Life + Properties.Resources.Str_Separator, _backColor))
                {
                    _playerInfoSurface.Blit(ts, new Point(x, y));
                    if (_playerInfoHeaderMaxWidth < ts.Width) _playerInfoHeaderMaxWidth = ts.Width;
                }
                y += dh;
                using (Surface ts = ResourceManager.SmallPFont.Render(Properties.Resources.Str_Pitch + Properties.Resources.Str_Separator, _backColor))
                {
                    _playerInfoSurface.Blit(ts, new Point(x, y));
                    if (_playerInfoHeaderMaxWidth < ts.Width) _playerInfoHeaderMaxWidth = ts.Width;
                }
                y += dh;
                using (Surface ts = ResourceManager.SmallPFont.Render(Properties.Resources.Str_Distance + Properties.Resources.Str_Separator, _backColor))
                {
                    _playerInfoSurface.Blit(ts, new Point(x, y));
                    if (_playerInfoHeaderMaxWidth < ts.Width) _playerInfoHeaderMaxWidth = ts.Width;
                }
            }
            #endregion

            s.Blit(_playerInfoSurface, _playerInfoRect.Location);

            #region 現在の情報を描画
            {
                int dh = (int)((_playerInfoRect.Height - Constants.PlayerInfoPadding * 2) / 3.0);

                int x = _playerInfoRect.X + Constants.PlayerInfoPadding + _playerInfoHeaderMaxWidth;
                int y = _playerInfoRect.Y + Constants.PlayerInfoPadding + (int)(dh / 2.0 - ResourceManager.SmallTTFont.Height / 2.0);

                int px = x;
                int py = _playerInfoRect.Y + Constants.PlayerInfoPadding + (int)(dh / 2.0);
                for (int i = 0; i < _parent.Player.Hp; i++)
                {
                    s.Blit(_coloredLifeSurfaces[0], new Point(px, (int)(py - _coloredLifeSurfaces[0].Height / 2.0)));
                    px += _coloredLifeSurfaces[0].Width;
                }
                for (int i = _parent.Player.Hp; i < _parent.Player.MaxHp; i++)
                {
                    s.Blit(_coloredLifeSurfaces[1], new Point(px, (int)(py - _coloredLifeSurfaces[1].Height / 2.0)));
                    px += _coloredLifeSurfaces[1].Width;
                }

                y += dh;
                using (Surface ts = ResourceManager.SmallTTFont.Render(_toneResult.ToString(), _backColor))
                {
                    s.Blit(ts, new Point(x, y));
                }

                y += dh;
                using (Surface ts = ResourceManager.SmallTTFont.Render(string.Format("{0:F0} ／ {1}", _parent.Player.X, _map.Width), _backColor))
                {
                    s.Blit(ts, new Point(x, y));
                }
            }
            #endregion
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
            Rectangle cr = Rectangle.Empty;
            foreach (KeyValuePair<Rectangle, string> kv in _blackKeys)
            {
                if (kv.Key.Top <= _parent.Player.Y - _view.Y && 
                    _parent.Player.Y - _view.Y <= kv.Key.Bottom)
                {
                    cr = new Rectangle(kv.Key.Location, kv.Key.Size);
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
                        cr = new Rectangle(kv.Key.Location, kv.Key.Size);
                        break;
                    }
                }
            }
            if (!cr.IsEmpty)
            {
                cr.Offset(_keyRect.Location);
                Circle cir = new Circle(new Point(cr.Right - 22, (int)(cr.Y + cr.Height / 2.0)), 10);
                s.Draw(cir, _strongColor, true, true);
                s.Draw(cir, _strongColor, true, false);
            }
        }

        protected virtual void renderMiniMap(Surface s)
        {
            if (_mapSurface == null)
            {
                _mapSurface = new Surface(_miniMapRect.Size);
                _map.RenderMiniMap(_mapSurface, new Rectangle(0, 0, _miniMapRect.Width, _miniMapRect.Height));
            }
            s.Blit(_mapSurface, _miniMapRect.Location);
            _map.RenderMiniMapViewBox(s, _miniMapRect);
        }
        #endregion

        protected PointD convertToViewCoord(double x, double y)
        {
            return new PointD(x - _view.X, y - _view.Y);
        }

        public override void Dispose()
        {
            #region 画像破棄
            if (_keyboardSurface != null) _keyboardSurface.Dispose();
            if (_mapSurface != null) _mapSurface.Dispose();
            if (_pauseSurface != null) _pauseSurface.Dispose();
            if (_clearSurface != null) _clearSurface.Dispose();
            if (_map != null) _map.Dispose();
            if (_lifeSurfaces != null) foreach (Surface s in _lifeSurfaces) s.Dispose();
            if (_coloredLifeSurfaces != null) foreach (Surface s in _coloredLifeSurfaces) s.Dispose();
            if (_playerInfoSurface != null) _playerInfoSurface.Dispose();
            #endregion

            base.Dispose();
        }
    }
}
