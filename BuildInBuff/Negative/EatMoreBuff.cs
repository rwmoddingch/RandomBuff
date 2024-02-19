using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Positive;
using HUD;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RWCustom;
using UnityEngine;

namespace BuiltinBuffs.Negative
{
    internal class EatMoreBuff : Buff<EatMoreBuff, EatMoreBuffData>
    {
        public override BuffID ID => EatMoreIBuffEntry.eatMoreBuffID;
    }

    class EatMoreBuffData : BuffData
    {
        public override BuffID ID => EatMoreIBuffEntry.eatMoreBuffID;
    }

    class EatMoreIBuffEntry : IBuffEntry
    {
        public static BuffID eatMoreBuffID = new BuffID("EatMore", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<EatMoreBuff,EatMoreBuffData,EatMoreIBuffEntry>(eatMoreBuffID);
        }

        public static void HookOn()
        {
            On.SlugcatStats.SlugcatFoodMeter += SlugcatStats_SlugcatFoodMeter;
        }

        private static IntVector2 SlugcatStats_SlugcatFoodMeter(On.SlugcatStats.orig_SlugcatFoodMeter orig, SlugcatStats.Name slugcat)
        {
            IntVector2 origFoodRequirement = orig.Invoke(slugcat);
            var data = (BuffPoolManager.Instance.GetBuff(eatMoreBuffID) as EatMoreBuff).Data;
            int newHibernateRequirement = origFoodRequirement.y + data.StackLayer;
            int newMaxFoodRequirement = Mathf.Max(newHibernateRequirement, origFoodRequirement.x);

            return new IntVector2(newMaxFoodRequirement, newHibernateRequirement);
        }
    }
}
