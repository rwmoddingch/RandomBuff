using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MonoMod.RuntimeDetour;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuffUtils;
using UnityEngine;

namespace BuiltinBuffs.Expeditions
{
    internal class ExplosionImmunityHook
    {
        public static void HookOn()
        {
            On.Creature.Violence += Creature_Violence;
            On.Explosion.Update += Explosion_Update;
            _ = new Hook(typeof(DamageOnlyExplosion).GetMethod("Update"), typeof(ExplosionImmunityHook).GetMethod(
                "DamageOnlyExplosion_Update",
                BindingFlags.Static | BindingFlags.NonPublic));
            On.Player.Stun += Player_Stun;
            IgnoreStun = false;
        }

        private static void Player_Stun(On.Player.orig_Stun orig, Player self, int st)
        {
            if (IgnoreStun)
            {
                if (new BuffID("unl-explosionimmunity").GetBuffData().StackLayer == 2)
                    st = Mathf.FloorToInt(st * 0.5f);
                if (new BuffID("unl-explosionimmunity").GetBuffData().StackLayer >= 3)
                    return;
            }
            orig(self, st);
        }

        public static bool IgnoreStun;

        private static void DamageOnlyExplosion_Update(Action<DamageOnlyExplosion, bool> orig, DamageOnlyExplosion self, bool eu)
        {
            if (new BuffID("unl-explosionimmunity").GetBuffData().StackLayer >= 2)
                IgnoreStun = true;
            orig(self, eu);
            IgnoreStun = false;
        }


        private static void Explosion_Update(On.Explosion.orig_Update orig, Explosion self, bool eu)
        {
            if (new BuffID("unl-explosionimmunity").GetBuffData().StackLayer >= 2)
                IgnoreStun = true;
            orig(self, eu);
            IgnoreStun = false;
        }

        private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, UnityEngine.Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage,
            Creature.DamageType type, float damage, float stunBonus)
        {
            if (self is Player && type == Creature.DamageType.Explosion)
            {
                var layer = new BuffID("unl-explosionimmunity").GetBuffData().StackLayer;
                if (layer >= 2)
                    damage = 0;
                if (layer >= 3)
                    stunBonus = 0;
            }
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }
    }

    internal class AgilityHook
    {
        public static void HookOn()
        {
            _ = new Hook(typeof(Player).GetProperty("isRivulet").GetGetMethod(),typeof(AgilityHook).GetMethod("Player_isRivulet",BindingFlags.NonPublic | BindingFlags.Static));
        }

        private static bool Player_isRivulet(Func<Player, bool> orig, Player self)
        {
            return orig(self) || new BuffID("unl-agility").GetBuffData().StackLayer >= 2;
        }

    }
}
