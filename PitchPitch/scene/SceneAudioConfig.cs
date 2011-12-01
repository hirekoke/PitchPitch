using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using SdlDotNet.Graphics;
using SdlDotNet.Graphics.Primitives;

using PitchPitch.audio;

namespace PitchPitch.scene
{
    class SceneAudioConfig : Scene
    {
        private AudioInput _audioInput;
        private CoreAudioApi.AudioEndpointVolume _volume;

        private SdlDotNet.Graphics.Font _waveFont;

        public SceneAudioConfig() : base()
        {
            sceneType = SceneType.AudioConfig;
            _waveFont = new SdlDotNet.Graphics.Font(
                System.IO.Path.Combine(Properties.Resources.FontDir, Properties.Resources.DefaultTTFont), 12);
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <param name="parent">ゲームクラス</param>
        public override void Init(PitchPitch parent)
        {
            base.Init(parent);

            _audioInput = parent.AudioInput;
        }

        void _volume_OnVolumeNotification(CoreAudioApi.AudioVolumeNotificationData data)
        {
        }

        public override void ProcKeyEvent(SdlDotNet.Input.KeyboardEventArgs e)
        {
            if (e != null)
            {
                int keyNum = ResourceManager.GetKeyNum(e);
                if (keyNum >= 0 && keyNum < _audioInput.DeviceInfos.Count)
                {
                    _audioInput.StopCapture();

                    DeviceInfo selectedDevice = _audioInput.DeviceInfos[keyNum];
                    _audioInput.SelectDevice(selectedDevice.DeviceId);
                }
                else
                {
                    switch (e.Key)
                    {
                        case SdlDotNet.Input.Key.Escape:
                            _parent.EnterScene(SceneType.EndlessGameStage);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 画面の描画処理
        /// </summary>
        /// <param name="s">画面</param>
        public override void Draw(Surface s)
        {
            s.Fill(Color.Black);

            int y = 50;
            int i = 0;
            foreach (DeviceInfo di in _audioInput.DeviceInfos)
            {
                s.Blit(ResourceManager.SmallPFont.Render(i++ + ": " + di.FriendlyName + "<" + (di.DataFlow == CoreAudioApi.EDataFlow.eRender ? "再生" : "録音") + ">", Color.White, true), new Point(10, y));
                y += ResourceManager.SmallTTFont.Height + 2;
            }

            if (_audioInput.CapDevice != null)
            {
                s.Blit(ResourceManager.SmallPFont.Render(
                    _audioInput.CapDevice.Id + " " + _audioInput.CapDevice.FriendlyName,
                    Color.YellowGreen),
                    new Point(10, _parent.Size.Height - 30));
            }

            if (_audioInput.Capturing)
            {
                drawWave(s, _parent.PitchResult, _parent.ToneResult, _waveFont,
                    new Rectangle(10, _parent.Size.Height - 240, _parent.Size.Width - 20, 200));

                drawPitch(s, _parent.ToneResult, ResourceManager.SmallPFont,
                    new Rectangle(_parent.Size.Width - 220, 20, 200, 80));
            }
        }

        private void drawLines(Surface s, List<Point> points, Color c)
        {
            Point prev = Point.Empty;
            foreach (Point p in points)
            {
                if (prev != Point.Empty)
                {
                    s.Draw(new Line(prev, p), c);
                }
                prev = p;
            }
        }

        private void drawWave(Surface s, audio.PitchResult pitch, audio.ToneResult tone, SdlDotNet.Graphics.Font font, Rectangle rect)
        {
            Rectangle gRect = new Rectangle(
                0, 
                ResourceManager.SmallTTFont.Height + 3,
                rect.Width - 1,
                rect.Height - 2 * ResourceManager.SmallTTFont.Height - 6);

            using (Surface sc = new Surface(rect.Size))
            {
                Box box = new Box(Point.Empty, new Size(rect.Width - 1, rect.Height - 1));
                Box gBox = new Box(gRect.Location, gRect.Size);

                // 枠
                sc.Draw(box, Color.White);
                sc.Draw(gBox, Color.White);
                sc.Draw(new Line(new Point(0, (int)(rect.Height / 2.0)), new Point(rect.Width, (int)(rect.Height / 2.0))), Color.White);

                if (pitch != null)
                {
                    double dx = rect.Width / (double)pitch.Length;
                    int width = rect.Width;
                    int height = rect.Height;

                    // x軸ラベル描画
                    double x = 0; double margin = 100; double prevX = -margin;
                    for (int i = 0; i < pitch.Length; i++)
                    {
                        x += dx;
                        if (x - prevX >= margin)
                        {
                            sc.Draw(new Line(new Point((int)x, gRect.Bottom), new Point((int)x, rect.Height)), Color.Red);
                            sc.Blit(font.Render(pitch.Frequency[i].ToString("f2"), Color.Red),
                                new Point((int)x + 1, gRect.Bottom + 2));

                            sc.Draw(new Line(new Point((int)x, 0), new Point((int)x, gRect.Top)), Color.Green);
                            sc.Blit(font.Render(pitch.Time[i].ToString("f0"), Color.Green),
                                new Point((int)x + 1, 2));

                            prevX = x;
                        }
                    }

                    // 波形用点列作成
                    double maxPower = double.MinValue;
                    double maxCorrelation = double.MinValue; double minCorrelation = double.MaxValue;
                    for (int i = 0; i < pitch.Length; i++)
                    {
                        if (maxPower < pitch.Power[i]) maxPower = pitch.Power[i];
                        if (maxCorrelation < pitch.Correlation[i]) maxCorrelation = pitch.Correlation[i];
                        if (minCorrelation > pitch.Correlation[i]) minCorrelation = pitch.Correlation[i];
                    }
                    if (maxCorrelation > -minCorrelation) minCorrelation = -maxCorrelation;
                    else maxCorrelation = -minCorrelation;

                    List<Point> signal = new List<Point>();
                    List<Point> nsdf = new List<Point>();
                    List<Point> power = new List<Point>();

                    x = 0;
                    for (int i = 0; i < pitch.Length; i++)
                    {
                        signal.Add(new Point((int)x, (int)(gRect.Bottom - gRect.Height * (pitch.Signal[i] + 1) / 2.0))); // 入力波
                        nsdf.Add(new Point((int)x, (int)(gRect.Bottom - gRect.Height * (pitch.NSDF[i] + 1) / 2.0))); // NSDF
                        power.Add(new Point((int)x, (int)(gRect.Bottom - gRect.Height * (pitch.Power[i] / (maxPower == 0 ? 1.0 : maxPower))))); // Power
                        x += dx;
                    }

                    // 波形描画
                    drawLines(sc, signal, Color.White);
                    drawLines(sc, nsdf, Color.Green);
                    drawLines(sc, power, Color.Red);
                }
                s.Blit(sc, rect.Location);
            }
        }


        public override void Dispose()
        {
            base.Dispose();

            _waveFont.Dispose();
        }
    }
}
