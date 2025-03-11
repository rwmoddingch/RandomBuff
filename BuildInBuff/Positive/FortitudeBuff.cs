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
    internal class FortitudeBuff : Buff<FortitudeBuff, FortitudeBuffData>
    {
        public override BuffID ID => FortitudeBuffEntry.Fortitude;
        
        public FortitudeBuff()
        {
        }
    }

    internal class FortitudeBuffData : BuffData
    {
        public override BuffID ID => FortitudeBuffEntry.Fortitude;
    }

    internal class FortitudeBuffEntry : IBuffEntry
    {
        public static BuffID Fortitude = new BuffID("Fortitude", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<FortitudeBuff, FortitudeBuffData, FortitudeBuffEntry>(Fortitude);
        }
        
        public static void HookOn()
        {
            On.Creature.Violence += Creature_Violence;
            On.Player.DeathByBiteMultiplier += Player_DeathByBiteMultiplier;
        }

        private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, UnityEngine.Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage,
            Creature.DamageType type, float damage, float stunBonus)
        {
            if (self is Player player)
            {
                damage *= Mathf.Pow(0.75f, Fortitude.GetBuffData().StackLayer);
                if (type == Creature.DamageType.Bite && damage >= player.Template.instantDeathDamageLimit && Random.value >= Mathf.Pow(0.5f, Fortitude.GetBuffData().StackLayer))
                    damage = 0.5f * player.Template.instantDeathDamageLimit;
            }

            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        private static float Player_DeathByBiteMultiplier(On.Player.orig_DeathByBiteMultiplier orig, Player self)
        {
            float result = orig(self);
            result *= Mathf.Pow(0.5f, Fortitude.GetBuffData().StackLayer);
            return result;
        }
    }
}
