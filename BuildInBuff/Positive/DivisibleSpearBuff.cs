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
    internal class DivisibleSpearBuff : Buff<DivisibleSpearBuff, DivisibleSpearBuffData>
    {
        public override BuffID ID => DivisibleSpearBuffEntry.DivisibleSpear;
        
        public DivisibleSpearBuff()
        {
        }
    }

    internal class DivisibleSpearBuffData : BuffData
    {
        public override BuffID ID => DivisibleSpearBuffEntry.DivisibleSpear;
    }

    internal class DivisibleSpearBuffEntry : IBuffEntry
    {
        public static BuffID DivisibleSpear = new BuffID("DivisibleSpear", true);

        public static int StackLayer
        {
            get
            {
                return DivisibleSpear.GetBuffData().StackLayer;
            }
        }

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<DivisibleSpearBuff, DivisibleSpearBuffData, DivisibleSpearBuffEntry>(DivisibleSpear);
        }
        
        public static void HookOn()
        {
            On.Player.ThrownSpear += Player_ThrownSpear;
        }

        private static void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig.Invoke(self, spear);
            for (int i = 0; i < 2; i++)
            {
                var dir = spear.throwDir.x == 0 ? new Vector2(i == 0 ? 1 : -1,0) : new Vector2(0, i == 0 ? 1 : -1);

                AbstractSpear absSpaer = new AbstractSpear(spear.room.world, null, spear.abstractPhysicalObject.pos, spear.room.game.GetNewID(), false);
                spear.room.abstractRoom.AddEntity(absSpaer);
                absSpaer.RealizeInRoom();
                var newSpear = absSpaer.realizedObject as Spear;

                Vector2 vector = self.firstChunk.pos + spear.throwDir.ToVector2() * 10f + new Vector2(0f, 4f);

                newSpear.Thrown(self, vector, null, spear.throwDir, Mathf.Lerp(1f, 1.5f, self.Adrenaline), false);

                newSpear.firstChunk.lastPos = newSpear.firstChunk.pos +=  dir * 3;
                newSpear.spearDamageBonus = spear.spearDamageBonus;
                newSpear.firstChunk.vel = spear.firstChunk.vel + dir * 0.25f * spear.firstChunk.vel.magnitude;
            }
        }
    }
}
