using System.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using Menu;

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
                self.room.game.manager.nextSlideshow = new SlideShow.SlideShowID("flip_jump_end", true);
                self.room.game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow);
            }
        }
    }
}