using MoreSlugcats;
using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;

namespace BuiltinBuffs.Positive
{
    internal class StoneThrowerBuff : Buff<StoneThrowerBuff, StoneThrowerBuffData>
    {
        public override BuffID ID => StoneThrowerIBuffEntry.stoneThrowerBuffID;
    }

    internal class StoneThrowerBuffData : BuffData
    {
        public override BuffID ID => StoneThrowerIBuffEntry.stoneThrowerBuffID;
    }

    internal class StoneThrowerIBuffEntry : IBuffEntry
    {
        public static BuffID stoneThrowerBuffID = new BuffID("StoneThrower", true);

        public  void OnEnable()
        {
            BuffRegister.RegisterBuff<StoneThrowerBuff, StoneThrowerBuffData, StoneThrowerIBuffEntry>(stoneThrowerBuffID);
        }

        public static void HookOn()
        {
            On.Player.ThrownSpear += Player_ThrownSpear;
            On.Rock.HitSomething += Rock_HitSomething;
        }

        private static bool Rock_HitSomething(On.Rock.orig_HitSomething orig, Rock self, SharedPhysics.CollisionResult result, bool eu)
        {
            var a = orig.Invoke(self, result, eu);
            if (result.obj == null)
            {
                return a;
            }
            if (result.obj is Creature)
            {
                float stunBonus = 45f;
                if (ModManager.MMF && MMF.cfgIncreaseStuns.Value && 
                    (result.obj is Cicada || result.obj is LanternMouse || (ModManager.MSC && result.obj is Yeek)))
                {
                    stunBonus = 90f;
                }
                if (ModManager.MSC && 
                    self.room.game.IsArenaSession && 
                    self.room.game.GetArenaGameSession.chMeta != null)
                {
                    stunBonus = 90f;
                }
                (result.obj as Creature).Violence(self.firstChunk, new Vector2?(self.firstChunk.vel * self.firstChunk.mass), result.chunk, result.onAppendagePos, Creature.DamageType.Blunt, 0.99f, stunBonus);
            }
            return a;
        }


        private static void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig.Invoke(self, spear);
            spear.spearDamageBonus *= 0.5f;
        }
    }
}
