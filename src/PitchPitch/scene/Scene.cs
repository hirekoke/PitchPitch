using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

using SdlDotNet.Input;
using SdlDotNet.Graphics;
using SdlDotNet.Graphics.Sprites;
using SdlDotNet.Audio;

namespace PitchPitch.scene
{
    public enum SceneType
    {
        Base,

        Error,
        Title,
        MapSelect,
        Option,
        GameStage,
        EndlessGameStage,
        GameOver,
    }

    abstract class Scene : IDisposable
    {
        protected PitchPitch _parent;

        protected SceneType sceneType = SceneType.Base;
        public SceneType SceneType
        {
            get { return sceneType; }
        }

        private int _transitionTime = 0;
        private bool _transitionStart = false;
        private long _prevTransitionTick = 0;
        private Surface _transitionBeforeSurface = null;
        protected delegate void SceneTransitionEndEventHandler();
        protected SceneTransitionEndEventHandler _transitionEndDel;

        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <param name="parent">ゲームクラス</param>
        public virtual void Init(PitchPitch parent)
        {
            _parent = parent;
            _prevPressedKey = Key.Print;
            _prevPressTick = Environment.TickCount;

            _prevTransitionTick = Environment.TickCount;
            _transitionStart = false;
            if (_transitionBeforeSurface != null)
            {
                _transitionBeforeSurface.Dispose();
                _transitionBeforeSurface = null;
            }

            SetAlert(false, null);
        }

        protected abstract int procKeyEvent(Key key);
        protected abstract void procMenu(int idx);
        protected abstract void proc(SdlDotNet.Input.KeyboardEventArgs e);

        protected Key _prevPressedKey;
        protected long _prevPressTick = -1;
        protected Key[] _keys = new Key[]
        {
            Key.UpArrow, Key.DownArrow, Key.Return, Key.Escape
        };

        private bool _onAlert = false;
        private Surface _alertSurface = null;
        public void SetAlert(bool on, string message)
        {
            _onAlert = on;
            if (_onAlert && string.IsNullOrEmpty(message)) _onAlert = false;
            if (_alertSurface != null) _alertSurface.Dispose();
            if (!_onAlert) _alertSurface = null;

            if (_onAlert)
            {
                message += "\n\n" + Properties.Resources.Str_AlertTail;
                string[] lines = message.Split('\n');
                Size[] sizes = Array.ConvertAll<string, Size>(lines, (l) => { return ResourceManager.SmallPFont.SizeText(l); });

                int maxWidth = 0; int height = 0;
                foreach (Size s in sizes)
                {
                    if (maxWidth < s.Width) maxWidth = s.Width;
                    height += (int)(s.Height * Constants.LineHeight);
                }

                _alertSurface = new Surface(maxWidth + Constants.AlertPadding * 2, height + Constants.AlertPadding * 2);
                SdlDotNet.Graphics.Primitives.Box box = new SdlDotNet.Graphics.Primitives.Box(
                    Point.Empty, new Size(maxWidth + Constants.AlertPadding * 2 - 1, height + Constants.AlertPadding * 2 - 1));

                _alertSurface.Lock();
                _alertSurface.Draw(box, Constants.AlertBackColor, false, true);
                _alertSurface.Draw(box, Constants.AlertForeColor, false, false);
                _alertSurface.Unlock();

                int y = Constants.AlertPadding; int idx = 0;
                foreach (string l in lines)
                {
                    using (Surface ts = ResourceManager.SmallPFont.Render(l, Constants.AlertForeColor))
                    {
                        if (idx == lines.Length - 1)
                        {
                            _alertSurface.Blit(ts, new Point((int)(_alertSurface.Width / 2.0 - ts.Width / 2.0), y));
                        }
                        else
                        {
                            _alertSurface.Blit(ts, new Point(Constants.AlertPadding, y));
                        }
                        y += (int)(ts.Height * Constants.LineHeight);
                    }
                    idx++;
                }
                _alertSurface.Update();
            }
        }

        public void PlaySeOK()
        {
            ResourceManager.SoundOK.Play();
        }
        public void PlaySeCancel()
        {
            ResourceManager.SoundCancel.Play();
        }

        protected void startTransition(SceneTransitionEndEventHandler del)
        {
            _transitionEndDel = del;
            _transitionTime = Constants.TransitionTime;
            _prevTransitionTick = Environment.TickCount;
            _transitionStart = true;
        }

        /// <summary>
        /// キー処理(イベント)
        /// </summary>
        /// <param name="e"></param>
        public void ProcKeyEvent(SdlDotNet.Input.KeyboardEventArgs e)
        {
            if (e != null)
            {
                if (_onAlert)
                {
                    if (e.Key == Key.Return)
                    {
                        SetAlert(false, "");
                    }
                }
                else
                {
                    int idx = procKeyEvent(e.Key);
                    procMenu(idx);
                }
            }
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        /// <param name="e"></param>
        public void Process(SdlDotNet.Input.KeyboardEventArgs e)
        {
            try
            {
                if (_transitionStart)
                {
                    _transitionTime -= (int)(Environment.TickCount - _prevTransitionTick);
                    _prevTransitionTick = Environment.TickCount;

                    if (_transitionTime <= 0)
                    {
                        SceneTransitionEndEventHandler del = _transitionEndDel;
                        if (del != null)
                        {
                            _transitionStart = false;
                            del();
                        }
                    }
                }

                ProcKeyEvent(e);

                proc(e);

                int idx = -1; bool pressed = false;
                foreach (Key k in _keys)
                {
                    if (Keyboard.IsKeyPressed(k))
                    {
                        pressed = true;
                        if (k == _prevPressedKey)
                        {
                            if (Environment.TickCount - _prevPressTick > Constants.ContinuousKeyTime)
                            {
                                idx = procKeyEvent(k);
                                if (idx >= 0) break;
                            }
                        }
                        else
                        {
                            _prevPressedKey = k;
                            _prevPressTick = Environment.TickCount;
                        }
                    }
                }
                if (!pressed)
                {
                    _prevPressedKey = Key.Print;
                    _prevPressTick = Environment.TickCount;
                }

                procMenu(idx);

            }
            catch (Exception ex)
            {
                SetAlert(true, ex.Message);
            }
        }

        /// <summary>
        /// 画面の描画処理
        /// </summary>
        /// <param name="s">画面</param>
        public void Draw(Surface s)
        {
            try
            {
                if (_transitionStart)
                {
                    if (_transitionBeforeSurface == null)
                    {
                        _transitionBeforeSurface = new Surface(s);
                        _transitionBeforeSurface.Transparent = true;
                        _transitionBeforeSurface.AlphaBlending = true;
                        _transitionBeforeSurface.Alpha = 0;
                    }
                    _transitionBeforeSurface.Alpha = (byte)(255 * _transitionTime / (double)Constants.TransitionTime);
                    s.Fill(Constants.Color_Transition);
                    s.Blit(_transitionBeforeSurface);
                }
                else
                {
                    draw(s);
                }
            }
            catch (Exception ex)
            {
                SetAlert(true, ex.Message);
            }

            if (_onAlert && _alertSurface != null)
            {
                s.Blit(_alertSurface, new Point(
                    (int)(Constants.ScreenWidth / 2.0 - _alertSurface.Width / 2.0),
                    (int)(Constants.ScreenHeight / 2.0 - _alertSurface.Height / 2.0)));
            }
        }

        protected abstract void draw(Surface s);

        public virtual void Dispose()
        {
            if (_alertSurface != null) _alertSurface.Dispose();
            if (_transitionBeforeSurface != null) _transitionBeforeSurface.Dispose();
        }

    }
}
