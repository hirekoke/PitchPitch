using System;
using System.Collections.Generic;
using System.Text;
using SdlDotNet.Input;
using SdlDotNet.Graphics;
using SdlDotNet.Graphics.Sprites;

namespace PitchPitch.scene
{
    class SceneEndlessGameStage : SceneGameStage
    {
        public SceneEndlessGameStage() : base(new map.RandomEndlessMap(3))
        {
            sceneType = SceneType.EndlessGameStage;

            _pauseMenuItems = new MenuItem[]
            {
                new MenuItem(Key.Escape, Properties.Resources.MenuItem_ResumeGame),
                new MenuItem(Key.M, Properties.Resources.MenuItem_MapSelect),
                new MenuItem(Key.T, Properties.Resources.MenuItem_ReturnTitle)
            };
        }

        protected override void procMenuPaused(int idx)
        {
            switch (idx)
            {
                case 0: IsPaused = false; break;
                case 1:
                    _parent.EnterScene(scene.SceneType.MapSelect);
                    break;
                case 2:
                    _parent.EnterScene(scene.SceneType.Title);
                    break;
            }
        }
    }
}
