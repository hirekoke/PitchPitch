using System;
using System.Collections.Generic;
using System.Text;

namespace PitchPitch.scene
{
    class SceneOption : Scene
    {
        public override void Init(PitchPitch parent)
        {
            base.Init(parent);
        }

        protected override int procKeyEvent(SdlDotNet.Input.Key key)
        {
            return base.procKeyEvent(key);
        }

        protected override void procMenu(int idx)
        {
            base.procMenu(idx);
        }

        public override void Draw(SdlDotNet.Graphics.Surface s)
        {
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
