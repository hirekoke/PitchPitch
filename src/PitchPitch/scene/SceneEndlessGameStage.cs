using System;
using System.Collections.Generic;
using System.Text;
using SdlDotNet.Input;
using SdlDotNet.Graphics;
using SdlDotNet.Graphics.Sprites;

namespace PitchPitch.scene
{
    using MenuItem = KeyValuePair<Key, string>;

    class SceneEndlessGameStage : SceneGameStage
    {
        public SceneEndlessGameStage() : base(new map.RandomEndlessMap())
        {
            sceneType = SceneType.EndlessGameStage;
        }
    }
}
