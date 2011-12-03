using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

using SdlDotNet.Input;
using SdlDotNet.Graphics;
using SdlDotNet.Graphics.Sprites;

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

        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <param name="parent">ゲームクラス</param>
        public virtual void Init(PitchPitch parent)
        {
            _parent = parent;
            _prevPressedKey = Key.Print;
            _prevPressTick = Environment.TickCount;
        }

        protected virtual int procKeyEvent(Key key) { return -1; }
        protected virtual void procMenu(int idx) { }
        protected Key _prevPressedKey;
        protected long _prevPressTick = -1;
        protected Key[] _keys = new Key[]
        {
            Key.UpArrow, Key.DownArrow, Key.Return, Key.Escape
        };

        /// <summary>
        /// キー処理(イベント)
        /// </summary>
        /// <param name="e"></param>
        public virtual void ProcKeyEvent(SdlDotNet.Input.KeyboardEventArgs e)
        {
            if (e != null)
            {
                int idx = procKeyEvent(e.Key);
                procMenu(idx);
            }
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        /// <param name="e"></param>
        public virtual void Process(SdlDotNet.Input.KeyboardEventArgs e)
        {
            ProcKeyEvent(e);

            int idx = -1; bool pressed = false;
            foreach (Key k in _keys)
            {
                if (Keyboard.IsKeyPressed(k))
                {
                    pressed = true;
                    if (k == _prevPressedKey)
                    {
                        if (Environment.TickCount - _prevPressTick > 500)
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

        /// <summary>
        /// 画面の描画処理
        /// </summary>
        /// <param name="s">画面</param>
        public abstract void Draw(Surface s);

        public virtual void Dispose()
        {
        }

        protected void drawPitch(Surface s, audio.ToneResult result,
            SdlDotNet.Graphics.Font font, Rectangle rect)
        {
            // 結果値
            using (Surface sc = new Surface(rect.Size))
            {
                sc.Lock();
                sc.Alpha = 100;
                sc.AlphaBlending = true;
                sc.Fill(Color.DarkGreen);
                sc.Unlock();
                s.Blit(sc, rect.Location);
            }

            using (Surface sc = new Surface(rect.Size))
            {
                sc.Transparent = true;
                sc.Draw(new SdlDotNet.Graphics.Primitives.Box(Point.Empty, new Size(rect.Width - 1, rect.Height - 1)),
                    Color.White, true, false);
                sc.Blit(font.Render("Clarity: " + result.Clarity.ToString("f2"), Color.White, true),
                    new Point(5, 5));
                sc.Blit(font.Render("Pitch  : " + result.Pitch.ToString("f2"), Color.White, true),
                    new Point(5, 5 + ResourceManager.SmallTTFont.Height + 2));
                sc.Blit(font.Render(
                    "Tone   : " + result.Tone + result.Octave.ToString() + " " + (result.PitchDiff > 0 ? "+" : "") + result.PitchDiff.ToString("f2"),
                    Color.White, true),
                    new Point(5, 5 + ResourceManager.SmallTTFont.Height * 2 + 4));

                s.Blit(sc, rect.Location);
            }
        }


    }
}
