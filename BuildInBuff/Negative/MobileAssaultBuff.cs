using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using On.Menu;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;

namespace BuiltinBuffs.Negative
{
    internal class MobileAssaultBuff : Buff<MobileAssaultBuff, MobileAssaultBuffData>
    {
        public override BuffID ID => MobileAssaultIBuffEntry.mobileAssaultBuffID;
    }
    internal class MobileAssaultBuffData : BuffData
    {
        public override BuffID ID => MobileAssaultIBuffEntry.mobileAssaultBuffID;
    }
    internal class MobileAssaultIBuffEntry : IBuffEntry
    {
        public static BuffID mobileAssaultBuffID = new BuffID("MobileAssault", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<MobileAssaultBuff,MobileAssaultBuffData,MobileAssaultIBuffEntry>(mobileAssaultBuffID);
        }

        public static void HookOn()
        {
            On.Lizard.ctor += Lizard_ctor;
            
        }

        private static void Lizard_ctor(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
        {
            orig(self,abstractCreature, world);
            self.jumpModule = new LizardJumpModule(self);
        }
    }
}
