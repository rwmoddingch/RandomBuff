using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace BuiltinBuffs.Duality
{


    internal class BuzzingBeeBuffEntry : IBuffEntry
    {
        public static BuffID buzzingBeeBuffID = new BuffID("BuzzingBee", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<BuzzingBeeBuffEntry>(buzzingBeeBuffID);
            
        }

        public static void HookOn()
        {
            On.SporePlant.Bee.ctor += Bee_ctor;
        }

        private static void Bee_ctor(On.SporePlant.Bee.orig_ctor orig, SporePlant.Bee self, SporePlant owner, bool angry, Vector2 pos, Vector2 vel, SporePlant.Bee.Mode initMode)
        {
            orig.Invoke(self, owner, angry, pos, vel, initMode);
            self.lifeTime *= 10f;
        }
    }
}
