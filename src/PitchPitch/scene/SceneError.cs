using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using SdlDotNet.Graphics;
using SdlDotNet.Input;

namespace PitchPitch.scene
{
    class SceneError : Scene
    {
        private Surface _prevSurface = null;

        private SurfaceCollection _errorMessageSurface = null;
        private Rectangle[] _errorMessageRects = null;

        private string[] _errorMessages = new string[0];
        public string[] ErrorMessages
        {
            get { return _errorMessages; }
            set
            {
                _errorMessages = value;
                if (_errorMessageSurface != null) foreach(Surface s in _errorMessageSurface) s.Dispose();
                _errorMessageSurface = null;
            }
        }

        public SceneError()
        {
            _keys = new Key[] { Key.Return };
        }
        public override void Dispose()
        {
            release();
            base.Dispose();
        }

        private void release()
        {
            if (_prevSurface != null) _prevSurface.Dispose();
            if (_errorMessageSurface != null) foreach (Surface s in _errorMessageSurface) s.Dispose();
            _prevSurface = null;
            _errorMessageSurface = null;
        }

        public override void Init(PitchPitch parent)
        {
            base.Init(parent);
            release();
        }

        protected override int procKeyEvent(SdlDotNet.Input.Key key)
        {
            if (key == SdlDotNet.Input.Key.Return)
            {
                return 0;
            }
            return -1;
        }
        protected override void procMenu(int idx)
        {
            base.procMenu(idx);
            if (idx < 0)
            {

            }
            else
            {
                _parent.EnterScene(scene.SceneType.Option);
            }
        }

        protected override void draw(SdlDotNet.Graphics.Surface s)
        {
            if (_prevSurface == null)
            {
                _prevSurface = new Surface(s);
                _prevSurface.Transparent = true;
                _prevSurface.AlphaBlending = true;
                _prevSurface.Alpha = 64;
            }

            s.Fill(Color.Black);
            s.Blit(_prevSurface, Point.Empty);

            if (_errorMessageSurface == null && _errorMessages != null)
            {
                _errorMessageSurface = new SurfaceCollection();
                _errorMessageRects = new Rectangle[_errorMessages.Length];
                ImageUtil.CreateStrMenu(_errorMessages, Color.White, ref _errorMessageSurface, ref _errorMessageRects, s.Width);
            }
            if (_errorMessageSurface != null)
            {
                ImageUtil.DrawSurfaces(s, _errorMessageSurface, _errorMessageRects, new Point(0, 100), ImageAlign.TopCenter);
            }
        }
    }
}
