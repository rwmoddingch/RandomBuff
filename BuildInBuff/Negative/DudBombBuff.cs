using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Negative
{
    internal class DudBombBuff : IBuffEntry
    {
        public static BuffID dudBombID = new BuffID("DudBomb", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<DudBombBuff>(dudBombID);
        }

        public static void HookOn()
        {
            On.ScavengerBomb.Explode += ScavengerBomb_Explode;
        }

        private static void ScavengerBomb_Explode(On.ScavengerBomb.orig_Explode orig, ScavengerBomb self, BodyChunk hitChunk)
        {
            if(Random.value > 0.5f)
            {
                self.ignited = false;
                self.burn = 0f;
            }
            else
                orig.Invoke(self, hitChunk);
        }
    }
}
