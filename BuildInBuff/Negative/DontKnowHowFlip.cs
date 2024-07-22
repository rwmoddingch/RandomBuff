using System.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using Menu;
using RandomBuff;
using RandomBuffUtils;
using UnityEngine;

namespace HotDogGains.Negative
{
    class DontKnowHowFlipBuff : Buff<DontKnowHowFlipBuff, DontKnowHowFlipBuffData> { public override BuffID ID => DontKnowHowFlipBuffEntry.DontKnowHowFlipID; }
    class DontKnowHowFlipBuffData : CountableBuffData
    {
        public override BuffID ID => DontKnowHowFlipBuffEntry.DontKnowHowFlipID;

        public override int MaxCycleCount => 1;
    }
    class DontKnowHowFlipBuffEntry : IBuffEntry
    {
        public static BuffID DontKnowHowFlipID = new BuffID("DontKnowHowFlipID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<DontKnowHowFlipBuff, DontKnowHowFlipBuffData, DontKnowHowFlipBuffEntry>(DontKnowHowFlipID);

        }

        public static void LoadAssets()
        {
            var slide = new BuffScene.SlideShowData(
                $"BUFF_{DontKnowHowFlipID.GetStaticData().AssetPath}/music/clownSong");
            for (int i = 1; i <= 6; i++)
            {
                var i1 = i;
                BuffScene.RegisterMenuScene(new MenuScene.SceneID($"FlipJump{i}"), (self) =>
                {
                    self.sceneFolder = Path.Combine(DontKnowHowFlipID.GetStaticData().AssetPath, "scenes");
                    self.depthIllustrations.Add(new MenuDepthIllustration(self.menu, self, self.sceneFolder,
                        $"FlipJump{i1}", Vector2.zero, 20, MenuDepthIllustration.MenuShader.Basic));
                });
                slide.scenes.Add(new BuffScene.SlideShowData.SlideShowSceneData() { duringTime = 1f, fadeIn = 0.2f, fadeOut = 0.2f, sceneId = $"FlipJump{i}" });
            }
            BuffScene.RegisterSlideShow(new SlideShow.SlideShowID("flip_jump_end"), slide);
        }

        public static void HookOn()
        {
            On.Player.UpdateAnimation += WhenFlip;
        }

        private static void WhenFlip(On.Player.orig_UpdateAnimation orig, Player self)
        {
            orig.Invoke(self);
            if (self.animation == Player.AnimationIndex.Flip)
            {
                DontKnowHowFlipBuff.Instance.TriggerSelf(true);
                self.Die();
                // (self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.deaths+=1;
                self.room.game.manager.nextSlideshow = new SlideShow.SlideShowID("flip_jump_end", false);
                self.room.game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow);
            }
        }
    }
}