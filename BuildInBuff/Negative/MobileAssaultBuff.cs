using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using On.Menu;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using UnityEngine;

namespace BuiltinBuffs.Negative
{
  
    internal class MobileAssaultIBuffEntry : IBuffEntry
    {
        public static BuffID mobileAssaultBuffID = new BuffID("MobileAssault", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<MobileAssaultIBuffEntry>(mobileAssaultBuffID);
        }

        public static void HookOn()
        {
            On.Lizard.ctor += Lizard_ctor;
            On.Lizard.Update += Lizard_Update;
            
        }

        private static void Lizard_Update(On.Lizard.orig_Update orig, Lizard self, bool eu)
        {
            orig(self,eu);
            if (self.jumpModule == null)
                self.jumpModule = new LizardJumpModule(self);
        }

        private static void Lizard_ctor(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
        {
            orig(self,abstractCreature, world);
            self.jumpModule = new LizardJumpModule(self);
        }
    }
}
