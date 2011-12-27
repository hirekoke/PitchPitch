using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using SdlDotNet.Graphics;
using SdlDotNet.Graphics.Primitives;
using SdlDotNet.Input;
using PitchPitch.audio;

namespace PitchPitch.scene
{
    class SceneOption : Scene
    {
        private const int IDX_MAXPITCH = 0;
        private const int IDX_MINPITCH = 1;

        private Color _powerColor = Color.FromArgb(100, 100, 100);
        private Color _nsdfColor = Color.FromArgb(50, 50, 255);
        private Color _signalColor = Color.FromArgb(255, 50, 50);

        private SdlDotNet.Graphics.Sprites.AnimatedSprite _cursor = null;
        private SdlDotNet.Graphics.Sprites.AnimatedSprite _headCursor = null;

        private AudioInput _audioInput;

        private enum SelectionState
        {
            Device,
            Calibration,
            Back,
        }
        private SelectionState _state = SelectionState.Device;
        private bool _isCalStarted = false;

        private double _minFreqSum = 0;
        private double _maxFreqSum = 0;
        private Queue<double> _minFreqs;
        private Queue<double> _maxFreqs;

        private List<DeviceInfo> _deviceInfos;
        private SurfaceCollection _devSurfaces;
        private Rectangle[] _devRects;
        private int _devSelectedIdx = 0;
        private SurfaceCollection _devDrawSurfaces;
        private Rectangle[] _devDrawRects;
        private int _devDrawFirstIdx = -1;
        private int _devDrawLength = -1;

        private SurfaceCollection _calSurfaces;
        private Rectangle[] _calRects;
        private string[] _calStrs = new string[] { Properties.Resources.Str_MaxPitch, Properties.Resources.Str_MinPitch };
        private int _calSelectedIdx = 0;

        private Surface _devHeadSurface = null;
        private Surface _calHeadSurface = null;
        private Surface _optSurface = null;
        private Surface _expSurface = null;

        private bool _needUpdate = false;

        private SurfaceCollection _endHeadSurfaces;
        private Rectangle[] _endHeadRects;
        private MenuItem[] _endHeadStrs = new MenuItem[] { new MenuItem(Key.Escape, Properties.Resources.MenuItem_ReturnTitle) };

        private Rectangle _devHeadRect;
        private Rectangle _devRect;

        private Rectangle _calHeadRect;
        private Rectangle _calRect;
        private Rectangle _calMenuRect;
        private Rectangle _calWaveRect;

        private Rectangle _endHeadRect;

        // 現在のピッチ
        private ToneResult _curToneResult;

        public SceneOption()
            : base()
        {
            sceneType = SceneType.Option;

            _deviceInfos = new List<DeviceInfo>();
            _keys = new Key[]
            {
                Key.UpArrow, Key.DownArrow, Key.RightArrow, Key.LeftArrow, 
                Key.Return, Key.Escape//, Key.Space
            };

            Size escSize = ResourceManager.MiddlePFont.SizeText(_endHeadStrs[0].ToString());
            _endHeadRect = new Rectangle(
                Constants.ScreenWidth - Constants.RightBottomItemMargin - escSize.Width - Constants.CursorMargin,
                Constants.ScreenHeight - Constants.RightBottomItemMargin - escSize.Height,
                escSize.Width + Constants.CursorMargin, escSize.Height);

            _devHeadRect = new Rectangle(
                Constants.HeaderX + Constants.UnderHeaderMargin,
                Constants.HeaderY + ResourceManager.LargePFont.Height + Constants.HeaderBottomMargin,
                (int)((Constants.ScreenWidth - (Constants.HeaderX + Constants.UnderHeaderMargin * 2) - Constants.MenuColumnGap) / 2.0),
                ResourceManager.MiddlePFont.Height);

            int devRectTop = _devHeadRect.Bottom + Constants.SubHeaderBottomMargin;
            int devRectBottm = _endHeadRect.Top - Constants.UnderHeaderMargin;
            _devRect = new Rectangle(
                _devHeadRect.X + Constants.CursorMargin,
                devRectTop,
                _devHeadRect.Right - (_devHeadRect.X + Constants.CursorMargin),
                devRectBottm - devRectTop);

            _calHeadRect = new Rectangle(_devRect.Right + Constants.MenuColumnGap, _devHeadRect.Top,
                _devHeadRect.Width, _devHeadRect.Height);
            _calRect = new Rectangle(_calHeadRect.X + Constants.CursorMargin, _devRect.Y,
                _devRect.Width, _devRect.Height);

            _calMenuRect = new Rectangle(_calRect.X, _calRect.Y + (int)(ResourceManager.SmallPFont.Height * Constants.MenuLineHeight),
                _calRect.Width, _calRect.Height - Constants.WaveHeight - Constants.WaveInfoHeight - (int)(ResourceManager.SmallPFont.Height * Constants.MenuLineHeight));
            _calWaveRect = new Rectangle(_calRect.X, _calRect.Bottom - Constants.WaveHeight,
                _calRect.Width, Constants.WaveHeight);

            _cursor = ResourceManager.GetColoredCursorGraphic(Constants.Color_Foreground);
            _headCursor = ResourceManager.GetColoredCursorGraphic(Constants.Color_Strong);

            _calSurfaces = new SurfaceCollection();
            _calRects = new Rectangle[_calStrs.Length];
            ImageUtil.CreateStrMenu(_calStrs, Constants.Color_Foreground, ResourceManager.SmallPFont,
                ref _calSurfaces, ref _calRects, _calRect.Width);

            _endHeadSurfaces = new SurfaceCollection();
            _endHeadRects = new Rectangle[1];
            ImageUtil.CreateStrMenu(_endHeadStrs, Constants.Color_Strong,
                ResourceManager.MiddlePFont,
                ref _endHeadSurfaces, ref _endHeadRects, _endHeadRect.Width);

            _minFreqs = new Queue<double>(Constants.FreqAvgMaxCount);
            _maxFreqs = new Queue<double>(Constants.FreqAvgMaxCount);
        }

        public override void Init(PitchPitch parent)
        {
            base.Init(parent);

            _audioInput = parent.AudioInput;
            _audioInput.DeviceInfoUpdated += (s, e) => { _needUpdate = true; };
            updateDevices();

            _isCalStarted = false;
            _calSelectedIdx = IDX_MAXPITCH;
            _state = SelectionState.Device;
        }

        #region デバイス選択用
        private void updateDevices()
        {
            _deviceInfos.Clear();
            _deviceInfos = new List<DeviceInfo>(_audioInput.DeviceInfos);

            string[] infoStrs = Array.ConvertAll<DeviceInfo, string>(
                _deviceInfos.ToArray(),
                (di) => { return getDeviceDrawString(di); });

            _devSurfaces = new SurfaceCollection();
            _devRects = new Rectangle[infoStrs.Length];
            ImageUtil.CreateStrMenu(infoStrs, Constants.Color_Foreground,
                ref _devSurfaces, ref _devRects, _devRect.Width);

            int ih = 30;
            if (_devRects.Length > 0) ih = _devRects[0].Height;
            _devDrawLength = (int)Math.Floor(_devRect.Height / (double)ih);
            if (_devDrawLength > _devRects.Length) _devDrawLength = _devRects.Length;

            _devDrawFirstIdx = -1;
            _devSelectedIdx = _parent.SelectedDeviceIndex;
            if (_deviceInfos.Count == 0)
            {
                _state = SelectionState.Back;
            }

            updateDevicesIndex();
        }

        private void updateDevicesIndex()
        {
            if (_devDrawFirstIdx >= 0 && _devDrawFirstIdx <= _devSelectedIdx && _devSelectedIdx < _devDrawFirstIdx + _devDrawLength)
            {
                // 入っている
                return;
            }
            else
            {
                // 入っていない
                if (_devDrawFirstIdx < 0) // 最初
                {
                    _devDrawFirstIdx = 0;
                }
                else if (_devSelectedIdx < _devDrawFirstIdx) // 前にずらす
                {
                    _devDrawFirstIdx = _devSelectedIdx;
                }
                else // 後ろにずらす
                {
                    _devDrawFirstIdx = _devSelectedIdx + 1 - _devDrawLength;
                }
            }

            string[] infoStrs = Array.ConvertAll<DeviceInfo, string>(
                _deviceInfos.GetRange(_devDrawFirstIdx, _devDrawLength).ToArray(),
                (di) => { return getDeviceDrawString(di); });

            if (_devDrawSurfaces != null)
                foreach (Surface s in _devDrawSurfaces) s.Dispose();

            _devDrawSurfaces = new SurfaceCollection();
            _devDrawRects = new Rectangle[infoStrs.Length];
            ImageUtil.CreateStrMenu(infoStrs, Constants.Color_Foreground,
                ref _devDrawSurfaces, ref _devDrawRects, _devRect.Width);
        }

        private string getDeviceDrawString(DeviceInfo info)
        {
            return string.Format("[{0}] {1}",
                info.DataFlow == CoreAudioApi.EDataFlow.eCapture ? 
                Properties.Resources.DeviceTypeName_Capture : Properties.Resources.DeviceTypeName_Render,
                info.FriendlyName);
        }
        #endregion

        #region キャリブレーション

        private void startCalibration()
        {
            switch (_calSelectedIdx)
            {
                case IDX_MAXPITCH:
                    _maxFreqs.Clear();
                    _maxFreqSum = 0;
                    break;
                case IDX_MINPITCH:
                    _minFreqs.Clear();
                    _minFreqSum = 0;
                    break;
            }
        }

        private void updateCalibration(double n)
        {
            switch (_calSelectedIdx)
            {
                case IDX_MAXPITCH:
                    _maxFreqs.Enqueue(n);
                    _maxFreqSum += n;
                    if (_maxFreqs.Count > Constants.FreqAvgMaxCount)
                    {
                        for (int i = _maxFreqs.Count; i > Constants.FreqAvgMaxCount; i--)
                        {
                            double d = _maxFreqs.Dequeue();
                            _maxFreqSum -= d;
                        }
                    }
                    break;
                case IDX_MINPITCH:
                    _minFreqs.Enqueue(n);
                    _minFreqSum += n;
                    if (_minFreqs.Count > Constants.FreqAvgMaxCount)
                    {
                        for (int i = _minFreqs.Count; i > Constants.FreqAvgMaxCount; i--)
                        {
                            double d = _minFreqs.Dequeue();
                            _minFreqSum -= d;
                        }
                    }
                    break;
            }
        }

        private double getAvgValue(int idx)
        {
            switch (idx)
            {
                case IDX_MAXPITCH:
                    if (_maxFreqs.Count > 0)
                        return _maxFreqSum / (double)_maxFreqs.Count;
                    else
                        return 0;
                case IDX_MINPITCH:
                    if (_minFreqs.Count > 0)
                        return _minFreqSum / (double)_minFreqs.Count;
                    else
                        return 0;
            }
            return 0;
        }

        private void stopCalibration()
        {
            if (_isCalStarted)
            {
                switch (_calSelectedIdx)
                {
                    case IDX_MAXPITCH:
                        Config.Instance.MaxFreq = getAvgValue(IDX_MAXPITCH);
                        break;
                    case IDX_MINPITCH:
                        Config.Instance.MinFreq = getAvgValue(IDX_MINPITCH);
                        break;
                }
                if (Config.Instance.MinFreq > Config.Instance.MaxFreq)
                {
                    double t = Config.Instance.MaxFreq;
                    Config.Instance.MaxFreq = Config.Instance.MinFreq;
                    Config.Instance.MinFreq = t;
                }
                _isCalStarted = false;
            }
        }
        #endregion

        #region 更新
        protected override int procKeyEvent(SdlDotNet.Input.Key key)
        {
            int idx = -1;
            switch (key)
            {
                case Key.UpArrow:
                    {
                        switch (_state)
                        {
                            case SelectionState.Device:
                                if (_devRects.Length > 0)
                                {
                                    _devSelectedIdx = (_devSelectedIdx - 1 < 0 ?
                                        (_devSelectedIdx + _devRects.Length - 1) : _devSelectedIdx - 1) % _devRects.Length;
                                    updateDevicesIndex();

                                    idx = 2;
                                }
                                break;
                            case SelectionState.Calibration:
                                stopCalibration();
                                if (_calSelectedIdx == 0)
                                {
                                    _state = SelectionState.Back;
                                }
                                _calSelectedIdx = _calSelectedIdx - 1;
                                break;
                            case SelectionState.Back:
                                _state = SelectionState.Calibration;
                                _calSelectedIdx = _calRects.Length - 1;
                                break;
                        }
                    }
                    break;
                case Key.DownArrow:
                    {
                        switch (_state)
                        {
                            case SelectionState.Device:
                                if (_devRects.Length > 0)
                                {
                                    _devSelectedIdx = (_devSelectedIdx + 1) % _devRects.Length;
                                    updateDevicesIndex();

                                    idx = 2;
                                }
                                break;
                            case SelectionState.Calibration:
                                stopCalibration();
                                if (_calSelectedIdx == _calRects.Length - 1)
                                {
                                    _calSelectedIdx = -1;
                                    _state = SelectionState.Back;
                                }
                                else
                                {
                                    _calSelectedIdx = (_calSelectedIdx + 1) % _calRects.Length;
                                }
                                break;
                            case SelectionState.Back:
                                _calSelectedIdx = 0;
                                _state = SelectionState.Calibration;
                                break;
                        }
                    }
                    break;
                case Key.RightArrow:
                case Key.LeftArrow:
                    {
                        switch (_state)
                        {
                            case SelectionState.Device:
                                _state = SelectionState.Calibration;
                                if(_calSelectedIdx < 0) _calSelectedIdx = 0;
                                break;
                            case SelectionState.Calibration:
                                stopCalibration();
                                _state = SelectionState.Device;
                                break;
                            case SelectionState.Back:
                                _state = SelectionState.Device;
                                break;
                        }
                    }
                    break;
                case Key.Return:
                    switch (_state)
                    {
                        case SelectionState.Device:
                            //_state = SelectionState.Calibration;
                            break;
                        case SelectionState.Calibration:
                            break;
                        case SelectionState.Back:
                            idx = 0;
                            break;
                    }
                    break;
                case Key.Escape:
                    stopCalibration();
                    idx = 3;
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
                    PlaySeOK();
                    startTransition(() => { _parent.EnterScene(scene.SceneType.Title); });
                    break;
                case 1:
                    // Calibration
                    break;
                case 2:
                    // Select Device
                    selectDevice();
                    break;
                case 3:
                    PlaySeCancel();
                    startTransition(() => { _parent.EnterScene(scene.SceneType.Title); });
                    break;
            }
        }

        private void selectDevice()
        {
            if (_devSelectedIdx >= 0 && _devSelectedIdx <= _deviceInfos.Count)
            {
                string id = null;
                try
                {
                    if(_audioInput.CapDevice != null)
                        id = _audioInput.CapDevice.Id;
                    _audioInput.StopCapture();

                    DeviceInfo selectedDevice = _deviceInfos[_devSelectedIdx];
                    _audioInput.SelectDevice(selectedDevice.DeviceId);
                }
                catch (Exception)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        _audioInput.SelectDevice(id);
                    }
                }
            }
        }


        protected override void proc(KeyboardEventArgs e)
        {
            if (_needUpdate) updateDevices();

            double pitch = -1;
            if (_audioInput.Capturing)
            {
                if (_parent.ToneResult.Clarity > ToneAnalyzer.ClarityThreshold &&
                    Constants.MinPitch <= _parent.ToneResult.Pitch && _parent.ToneResult.Pitch <= Constants.MaxPitch)
                {
                    _curToneResult = _parent.ToneResult.Copy();
                    pitch = _curToneResult.Pitch;
                }
            }

            if (Keyboard.IsKeyPressed(Key.Return))
            {
                #region キー押し中
                if (_state == SelectionState.Calibration)
                {
                    if (!_isCalStarted)
                    {
                        startCalibration();
                    }
                    _isCalStarted = true;
                    if (pitch >= 0)
                    {
                        updateCalibration(pitch);
                    }
                }
                #endregion
            }
            else
            {
                stopCalibration();
            }
        }
        #endregion

        protected override void draw(SdlDotNet.Graphics.Surface s)
        {
            s.Fill(Constants.Color_Background);

            // タイトル
            if (_optSurface == null)
            {
                _optSurface = ResourceManager.LargePFont.Render(Properties.Resources.HeaderTitle_Option, Constants.Color_Strong);
            }
            s.Blit(_optSurface, new Point(Constants.HeaderX, Constants.HeaderY));

            // 今選択中のドメイン
            if (_state == SelectionState.Device)
            {
                s.Fill(new Rectangle(_devHeadRect.Left, _devRect.Top, _devHeadRect.Width, _devRect.Height), Constants.Color_Selection);
            }
            else if (_state == SelectionState.Calibration)
            {
                s.Fill(new Rectangle(_calHeadRect.Left, _calMenuRect.Top, _calHeadRect.Width, _calMenuRect.Height), Constants.Color_Selection);
            }

            // Audio Device / Calibration Header
            if (_devHeadSurface == null)
            {
                _devHeadSurface = ResourceManager.MiddlePFont.Render(Properties.Resources.HeaderTitle_AudioDevices, Constants.Color_Foreground);
            }
            s.Blit(_devHeadSurface, _devHeadRect.Location);

            if (_calHeadSurface == null)
            {
                _calHeadSurface = ResourceManager.MiddlePFont.Render(Properties.Resources.HeaderTitle_Calibration, Constants.Color_Foreground);
            }
            s.Blit(_calHeadSurface, _calHeadRect.Location);

            if(_expSurface == null)
            {
                _expSurface = ResourceManager.SmallPFont.Render(Properties.Resources.Explanation_Calibration, Constants.Color_Strong);
            }
            s.Blit(_expSurface, new Point(_calHeadRect.X, _calRect.Y));

            // 選択肢
            ImageUtil.DrawSelections(s, _endHeadSurfaces, _endHeadRects, _headCursor,
                _endHeadRect.Location,
                (_state == SelectionState.Back ? 0 : -1), MenuItemAlign.TopLeft);

            ImageUtil.DrawSelections(s, _calSurfaces, _calRects, _cursor,
                _calMenuRect.Location,
                (_state == SelectionState.Calibration ? _calSelectedIdx : -1),
                MenuItemAlign.TopLeft);

            ImageUtil.DrawSelections(s, _devDrawSurfaces, _devDrawRects, _cursor,
                _devRect.Location, 
                _devSelectedIdx - _devDrawFirstIdx, 
                MenuItemAlign.TopLeft);

            // 高音・低音の値
            Surface hs = _calSurfaces[0];
            Surface ls = _calSurfaces[1];
            double maxFreq = Config.Instance.MaxFreq;
            double minFreq = Config.Instance.MinFreq;
            if (_isCalStarted)
            {
                switch (_calSelectedIdx)
                {
                    case IDX_MAXPITCH:
                        maxFreq = getAvgValue(IDX_MAXPITCH);
                        break;
                    case IDX_MINPITCH:
                        minFreq = getAvgValue(IDX_MINPITCH);
                        break;
                }
            }
            using (Surface ts = ResourceManager.SmallPFont.Render(maxFreq.ToString("F1"), Constants.Color_Foreground))
            {
                s.Blit(ts, new Point(_calRects[0].X + hs.Width + _calMenuRect.X + 10, _calRects[0].Y + _calMenuRect.Y));
            }
            using (Surface ts = ResourceManager.SmallPFont.Render(minFreq.ToString("F1"), Constants.Color_Foreground))
            {
                s.Blit(ts,
                    new Point(_calRects[1].X + ls.Width + _calMenuRect.X + 10, _calRects[1].Y + _calMenuRect.Y));
            }

            // 計測中かどうか
            if (_isCalStarted)
            {
                using (Surface ts = ResourceManager.SmallPFont.Render(Properties.Resources.Str_Calibrating, Constants.Color_Strong))
                {
                    s.Blit(ts, new Point(_calMenuRect.X, _calMenuRect.Bottom));
                }
            }

            // 波形
            if (_audioInput.Capturing)
            {
                using (Surface ts = ResourceManager.SmallTTFont.Render(
                    _curToneResult.ToString(), Constants.Color_Strong))
                {
                    s.Blit(ts, new Point(_calMenuRect.X, _calMenuRect.Bottom + ResourceManager.SmallPFont.Height + 5));
                }
                drawWave(s, _parent.PitchResult, _parent.ToneResult, ResourceManager.SmallTTFont, _calWaveRect);
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
            if (pitch != null)
            {
                double dx = rect.Width / (double)pitch.Length;
                int width = rect.Width;
                int height = rect.Height;

                #region 波形用点列作成
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

                double x = rect.X;
                for (int i = 0; i < pitch.Length; i++)
                {
                    signal.Add(new Point((int)x, (int)(rect.Bottom - rect.Height * (pitch.Signal[i] + 1) / 2.0))); // 入力波
                    nsdf.Add(new Point((int)x, (int)(rect.Bottom - rect.Height * (pitch.NSDF[i] + 1) / 2.0))); // NSDF
                    power.Add(new Point((int)x, (int)(rect.Bottom - rect.Height * (pitch.Power[i] / (maxPower == 0 ? 1.0 : maxPower))))); // Power
                    x += dx;
                }
                #endregion

                // 波形描画
                drawLines(s, nsdf, _nsdfColor);
                drawLines(s, power, _powerColor);
                drawLines(s, signal, _signalColor);
            }

            // 枠
            Box box = new Box(rect.Location, rect.Size);
            s.Draw(box, Constants.Color_Foreground, true, false);
            s.Draw(new Line(
                new Point(rect.X, rect.Top + (int)(rect.Height / 2.0)),
                new Point(rect.Right, rect.Top + (int)(rect.Height / 2.0))), Constants.Color_Foreground);
        }


        private void disposeSurfaceCollection(SurfaceCollection sc)
        {
            if (sc != null)
            {
                foreach (Surface s in sc) s.Dispose();
                sc = null;
            }
        }
        public override void Dispose()
        {
            disposeSurfaceCollection(_devSurfaces);
            disposeSurfaceCollection(_devDrawSurfaces);

            disposeSurfaceCollection(_endHeadSurfaces);

            if (_devHeadSurface != null) _devHeadSurface.Dispose();
            if (_calHeadSurface != null) _calHeadSurface.Dispose();
            if (_optSurface != null) _optSurface.Dispose();
            if (_expSurface != null) _expSurface.Dispose();

            base.Dispose();
        }
    }
}
