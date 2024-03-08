using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;
using RandomBuffUtils;

namespace BuiltinBuffs.Negative
{


    internal class InvisibleKillerIBuffEntry : IBuffEntry
    {
        public static BuffID InvisibleKillerBuffID = new BuffID("InvisibleKiller", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<InvisibleKillerIBuffEntry>(InvisibleKillerBuffID);
        }

        public static void HookOn()
        {
            On.LizardGraphics.Update += LizardGraphics_Update;
        }

        private static void LizardGraphics_Update(On.LizardGraphics.orig_Update orig, LizardGraphics self)
        {
            orig.Invoke(self);
            if(self.lizard.Template.type == CreatureTemplate.Type.WhiteLizard)
            {
                BuffUtils.Log(InvisibleKillerBuffID,$"{self.whiteCamoColorAmount},{self.whiteCamoColorAmountDrag}");
                self.whiteCamoColorAmount = 1;
            }
        }
    }
}
