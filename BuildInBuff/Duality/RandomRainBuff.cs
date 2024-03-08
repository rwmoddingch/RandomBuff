using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;

namespace BuiltinBuffs.Duality
{
   

    internal class RandomRainIBuffEntry : IBuffEntry
    {
        public static BuffID RandomRainBuffID = new BuffID("RandomRain", true);

        public  void OnEnable()
        {
            BuffRegister.RegisterBuff<RandomRainIBuffEntry>(RandomRainBuffID);
        }

        public static void HookOn()
        {
            On.RainWorld.LoadSetupValues += RainWorld_LoadSetupValues;
        }

        private static RainWorldGame.SetupValues RainWorld_LoadSetupValues(On.RainWorld.orig_LoadSetupValues orig, bool distributionBuild)
        {
            var result = orig.Invoke(distributionBuild);
            result.cycleTimeMin = Mathf.CeilToInt(result.cycleTimeMin * 0.1f);
            result.cycleTimeMax = Mathf.CeilToInt(result.cycleTimeMax * 10f);

            return result;
        }
    }
}
