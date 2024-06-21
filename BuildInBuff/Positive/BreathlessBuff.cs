using RandomBuff;
using RandomBuff.Core.Game;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RWCustom;
using Random = UnityEngine.Random;
using BuiltinBuffs.Duality;

namespace BuiltinBuffs.Positive
{
    internal class BreathlessBuff : Buff<BreathlessBuff, BreathlessBuffData>
    {
        public override BuffID ID => BreathlessBuffEntry.Breathless;
        
        public BreathlessBuff()
        {
        }
    }

    internal class BreathlessBuffData : BuffData
    {
        public override BuffID ID => BreathlessBuffEntry.Breathless;
    }

    internal class BreathlessBuffEntry : IBuffEntry
    {
        public static BuffID Breathless = new BuffID("Breathless", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<BreathlessBuff, BreathlessBuffData, BreathlessBuffEntry>(Breathless);
        }
        
        public static void HookOn()
        {
            On.Player.Update += Player_Update;
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);

            self.airInLungs = 1f;
        }
    }
}
