using RandomBuff;

using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;
using Random = UnityEngine.Random;
using RandomBuffUtils;

namespace BuiltinBuffs.Negative
{


    internal class ExplosiveJawIBuffEntry : IBuffEntry
    {
        public static BuffID explosiveJawBuffID = new BuffID("ExplosiveJaw", true);

        public static void HookOn()
        {
            On.Lizard.Violence += Lizard_Violence;
        }

        private static void Lizard_Violence(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source,
            UnityEngine.Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos,
            Creature.DamageType type, float damage, float stunBonus)
        {
            if (hitChunk != null && hitChunk.index == 0 && directionAndMomentum != null &&
                self.HitHeadShield(directionAndMomentum.Value))
            {
                Vector2 vector = hitChunk.pos;
                ExplosionSpawner.SpawnDamageOnlyExplosion(self, vector, self.room, self.effectColor, 1f);
            }

            orig.Invoke(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
        }

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ExplosiveJawIBuffEntry>(
                explosiveJawBuffID);
        }
    }
}
