using Menu;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotDogGains.Duality
{
    class testStory0Buff : Buff<testStory0Buff, testStory0BuffData>
    {
        public override BuffID ID => testStory0BuffEntry.testStory0ID;
        public override bool Trigger(RainWorldGame game)
        {
            game.manager.nextSlideshow =new  SlideShow.SlideShowID("card_story_0", true);
            game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow);
            this.Destroy();
            return true;
        }
    }
    class testStory0BuffData : BuffData { public override BuffID ID => testStory0BuffEntry.testStory0ID; }
    class testStory0BuffEntry : IBuffEntry
    {
        public static BuffID testStory0ID = new BuffID("testStory0ID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<testStory0Buff, testStory0BuffData, testStory0BuffEntry>(testStory0ID);
        }
        public static void HookOn()
        {

        }

    }
}