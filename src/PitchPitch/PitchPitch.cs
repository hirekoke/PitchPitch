using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;

using SdlDotNet.Core;
using SdlDotNet.Graphics;
using SdlDotNet.Input;

using PitchPitch.scene;

namespace PitchPitch
{
    class PitchPitch : IDisposable
    {
        private Surface _screen;

        private Scene _currentScene;
        public Scene CurrentScene { get { return _currentScene; } }

        private gameobj.Player _player;
        public gameobj.Player Player { get { return _player; } }

        #region AudioInput関係
        private bool _deviceRemoved = false;

        private object _audioLockObj = new object();
        private audio.AudioInput _audioInput;
        public audio.AudioInput AudioInput { get { return _audioInput; } }
        private int _selectedDeviceIndex = -1;
        public int SelectedDeviceIndex { get { return _selectedDeviceIndex; } }
        private audio.PitchResult _pitchResult = null;
        /// <summary>
        /// オーディオ解析結果の各種波形を取得
        /// <remarks>取得に時間がかかるので注意</remarks>
        /// </summary>
        public audio.PitchResult PitchResult
        {
            get
            {
                lock (_audioLockObj)
                {
                    return _pitchResult.Copy();
                }
            }
        }
        private audio.ToneResult _toneResult = audio.ToneResult.Default;
        /// <summary>
        /// オーディオ解析結果の値を取得
        /// </summary>
        public audio.ToneResult ToneResult { get { return _toneResult; } }
        #endregion

        public PitchPitch()
        {
            #region Events初期化
            Events.TargetFps = 60;
            Events.Tick += new EventHandler<TickEventArgs>(Tick);
            Events.Quit += new EventHandler<QuitEventArgs>(Quit);
            Events.KeyboardDown += new EventHandler<KeyboardEventArgs>(KeyboardDown);
            #endregion

            #region 画面初期化
            Video.WindowCaption = Properties.Resources.WindowTitle;
            _screen = Video.SetVideoMode(Constants.ScreenWidth, Constants.ScreenHeight, false, false, false, true);
            _screen.AlphaBlending = true;
            #endregion

            _player = new gameobj.Player();

            initAudio();

            _currentScene = new scene.SceneTitle();
            _currentScene.Init(this);
        }

        /// <summary>
        /// AudioInput初期化
        /// </summary>
        private void initAudio()
        {
            _audioInput = new audio.AudioInput();
            _audioInput.DeviceInfoUpdated += (s, e) =>
            {
                if (e.DeviceInfo.FindIndex((audio.DeviceInfo di) => { return di.DeviceId == Config.Instance.DeviceId; }) >= 0)
                {
                    if (!string.IsNullOrEmpty(Config.Instance.DeviceId))
                    {
                        _audioInput.SelectDevice(Config.Instance.DeviceId);
                    }
                }
                else
                {
                    // 繋ぎたいデバイスが消えた
                    _deviceRemoved = true;
                }
            };
            _audioInput.DeviceSelected += (s, e) =>
            {
                Config.Instance.DeviceId = e.Device.Id;
                _selectedDeviceIndex = e.Index;
                _audioInput.StartCapture();
            };
            _audioInput.CaptureStarted += (s, e) => { };
            _audioInput.CaptureStopped += (s, e) => { };
            _audioInput.Disposed += (s, e) => { };
            _audioInput.DataUpdated += (s, e) =>
            {
                lock (_audioLockObj)
                {
                    _pitchResult = e.Pitch;
                    _toneResult = e.Tone;
                }
            };
            _audioInput.UpdateDeviceInfo();
        }

        /// <summary>
        /// メインループ開始
        /// </summary>
        public void Run()
        {
            Events.Run();
        }

        private void KeyboardDown(object sender, KeyboardEventArgs e)
        {
            _currentScene.ProcKeyEvent(e);
        }

        private void dispose()
        {
            if (_scenes != null)
            {
                foreach (KeyValuePair<SceneType, Scene> kv in _scenes)
                {
                    if (kv.Value != null) kv.Value.Dispose();
                }
            }
            if (_player != null) _player.Dispose();
            if (_prevMap != null) _prevMap.Dispose();

            ResourceManager.Release();
            if (_audioInput != null)
            {
                _audioInput.Disposed += (s, e) =>
                {
                    _disposed = true;
                };
                _audioInput.CaptureStopped += (s, e) =>
                {
                    _audioInput.Dispose();
                };
                _audioInput.StopCapture();
            }
        }
        public void Dispose()
        {
            dispose();
        }
        private bool _disposed = false;
        private bool _disposing = false;
        private void Quit(object sender, QuitEventArgs e)
        {
            Quit();
        }

        public void Quit()
        {
            // Events.Close(); // causes crash of vshost32-clr2.exe !!
            _disposing = true;
            dispose();
            Config.Instance.Save();
            while (!_disposed)
            {
                System.Threading.Thread.Sleep(100);
            }
            Events.QuitApplication();
            Environment.Exit(0);
        }

        private void Tick(object sender, TickEventArgs e)
        {
            if (_disposing) return;

            if (_deviceRemoved && 
                _currentScene.SceneType != SceneType.Option && _currentScene.SceneType != SceneType.Title)
            {
                EnterScene(SceneType.Error, 
                    new string[] { "デバイスが切断されました", "Returnを押して確認してください" });
            }
            else
            {
                try
                {
                    _currentScene.Process(null);
                    _currentScene.Draw(Video.Screen);
                    Video.Screen.Update();
                }
                catch (Exception ex)
                {
                    _currentScene.SetAlert(true, ex.Message);
                }
            }
            _deviceRemoved = false;
        }

        private map.Map _prevMap = null;
        public map.Map PrevMap { get { return _prevMap; } }
        private SceneType _prevSceneType = SceneType.Base;
        public void EnterScene(scene.SceneType sceneType, object arg = null)//map.Map map = null)
        {
            _prevSceneType = _currentScene.SceneType;
            scene.Scene s = null;

            if (sceneType == SceneType.GameStage)
            {
                map.Map map = null;
                if (arg != null) map = arg as map.Map;
                if (_prevMap != null && _prevMap != map) _prevMap.Dispose();
                _prevMap = map;
                s = CreateScene(sceneType, this, map);
            }
            else if (sceneType == SceneType.Error)
            {
                s = CreateScene(sceneType, this, (string[])arg);
            }
            else
            {
                s = CreateScene(sceneType, this, null);
            }
            if (s != null) _currentScene = s;
        }

        public void RetryMap()
        {
            if (_prevMap == null)
            {
                EnterScene(SceneType.MapSelect);
            }
            else
            {
                EnterScene(SceneType.GameStage, _prevMap);
            }
        }

        private Dictionary<SceneType, Scene> _scenes;
        public scene.Scene CreateScene(SceneType type, PitchPitch parent, object arg = null)
        {
            if (_scenes == null)
            {
                _scenes = new Dictionary<SceneType, Scene>();
            }

            Scene scene = null;
            if (_scenes.ContainsKey(type))
            {
                scene = _scenes[type];
                switch (type)
                {
                    case SceneType.GameStage:
                        (scene as scene.SceneGameStage).Map = arg as map.Map;
                        break;
                    case SceneType.Error:
                        (scene as scene.SceneError).ErrorMessages = (string[])arg;
                        break;

                }
            }
            else
            {
                switch (type)
                {
                    case SceneType.Error:
                        scene = new SceneError();
                        (scene as scene.SceneError).ErrorMessages = (string[])arg;
                        break;
                    case SceneType.Title:
                        scene = new SceneTitle();
                        break;
                    case SceneType.MapSelect:
                        scene = new SceneMapSelect();
                        break;
                    case SceneType.Option:
                        scene = new SceneOption();
                        break;
                    case SceneType.GameStage:
                        scene = new SceneGameStage(arg as map.Map);
                        break;
                    case SceneType.GameOver:
                        scene = new SceneGameOver();
                        break;
                    default:
                        return null;
                }
                _scenes.Add(type, scene);
            }
            if (scene != null) scene.Init(parent);
            return scene;
        }

    }
}
