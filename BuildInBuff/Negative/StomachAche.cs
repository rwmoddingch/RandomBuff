
using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotDogGains.Negative
{

    class StomachAcheBuff : Buff<StomachAcheBuff, StomachAcheBuffData> { public override BuffID ID => StomachAcheBuffEntry.StomachAcheID; }
    class StomachAcheBuffData : RandomBuff.Core.Buff.CountableBuffData
    {
        public override BuffID ID => StomachAcheBuffEntry.StomachAcheID;

        public override int MaxCycleCount => 3;

        //[JsonProperty]
        //public int cycleLeft;

    }
    class StomachAcheBuffEntry : IBuffEntry
    {
        public static BuffID StomachAcheID = new BuffID("StomachAcheID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<StomachAcheBuff, StomachAcheBuffData, StomachAcheBuffEntry>(StomachAcheID);
        }
        public static void HookOn()
        {
            On.Player.ObjectEaten += Player_ObjectEaten;//吃小东西会晕
            On.Player.EatMeatUpdate += Player_EatMeatUpdate;//吃大东西会晕
        }

        private static void Player_EatMeatUpdate(On.Player.orig_EatMeatUpdate orig, Player self, int graspIndex)
        {
            if (self.grasps[graspIndex] == null || !(self.grasps[graspIndex].grabbed is Creature))
            {
                return;
            }
            if (self.eatMeat > 40 && self.eatMeat % 15 == 3) self.Stun(80);
            orig.Invoke(self, self.eatMeat);
        }

        private static void Player_ObjectEaten(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
        {
            StomachAcheBuff.Instance.TriggerSelf(true);
            self.Stun(80);
            orig.Invoke(self, edible);
        }

    }
}
