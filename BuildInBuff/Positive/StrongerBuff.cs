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
    internal class StrongerBuff : Buff<StrongerBuff, StrongerBuffData>
    {
        public override BuffID ID => StrongerBuffEntry.Stronger;
        
        public StrongerBuff()
        {
        }
    }

    internal class StrongerBuffData : BuffData
    {
        public override BuffID ID => StrongerBuffEntry.Stronger;
    }

    internal class StrongerBuffEntry : IBuffEntry
    {
        public static BuffID Stronger = new BuffID("Stronger", true);

        public static int StackLayer
        {
            get
            {
                return Stronger.GetBuffData()?.StackLayer ?? 0;
            }
        }

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<StrongerBuff, StrongerBuffData, StrongerBuffEntry>(Stronger);
        }
        
        public static void HookOn()
        {
            On.Player.ThrownSpear += Player_ThrownSpear;
        }

        private static void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig.Invoke(self, spear);
            spear.spearDamageBonus *= 1f + 0.5f * StackLayer;
            spear.firstChunk.vel *= 1f + 0.25f * StackLayer;
        }
    }
}
