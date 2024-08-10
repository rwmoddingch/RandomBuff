using System;
using HotDogBuff;
using System.Runtime.CompilerServices;
using MonoMod;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;
using Random = UnityEngine.Random;
using RandomBuff;
using BuiltinBuffs.Positive;

namespace TemplateGains
{
    //我的回合
    class StillMyTurnBuff : Buff<StillMyTurnBuff, StillMyTurnBuffData> { public override BuffID ID => StillMyTurnBuffEntry.StillMyTurnID; }
    class StillMyTurnBuffData : BuffData { public override BuffID ID => StillMyTurnBuffEntry.StillMyTurnID; }
    class StillMyTurnBuffEntry : IBuffEntry
    {
        public static BuffID StillMyTurnID = new BuffID("StillMyTurnID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<StillMyTurnBuff, StillMyTurnBuffData, StillMyTurnBuffEntry>(StillMyTurnID);
        }
        public static void HookOn()
        {
            On.Rock.HitSomething += Rock_HitSomething;
            On.Spear.HitSomething += MyTurn_HitSomething;

            On.Weapon.Update += Weapon_Update;
        }

        private static void Weapon_Update(On.Weapon.orig_Update orig, Weapon self, bool eu)
        {
            orig.Invoke(self, eu);
            if (self.myTurnWeapon().canWarp && (self.mode == Weapon.Mode.StuckInCreature || BounceSpearBuff.Instance != null))
            {
                WeaponWarp(self,self.myTurnWeapon().player);
            }
        }

        public static void WeaponWarp(Weapon weapon,Player slug)
        {
            weapon.myTurnWeapon().canWarp = false;

            if (slug == null || slug.room == null || !slug.Consious)
                return;

            if (weapon is Spear spear && spear.stuckInObject != null && spear.stuckInObject is Creature creature && creature.room == null)
                return;

            weapon.firstChunk.pos = slug.firstChunk.pos;
            if (slug.FreeHand() != -1)
            {
                slug.SlugcatGrab(weapon, slug.FreeHand());
                weapon.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, weapon.firstChunk);
            }
        }
        private static bool Rock_HitSomething(On.Rock.orig_HitSomething orig, Rock self, SharedPhysics.CollisionResult result, bool eu)
        {
            bool hit = orig.Invoke(self, result, eu);
            if (hit && self.thrownBy is Player player)
                self.myTurnWeapon().CanWarp(player);

            return hit;
        }
        private static bool MyTurn_HitSomething(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
        {
            bool hit = orig.Invoke(self, result, eu);
            if (hit && self.thrownBy is Player player && !(self is ExplosiveSpear) && BounceSpearBuff.Instance == null) 
                self.myTurnWeapon().CanWarp(player);

            return hit;
        }

        
    }
    public class EXWeapon
    {
        public Weapon weapon;
        public Player player;
        public bool canWarp = false;

        public void CanWarp(Player player)
        {
            this.player= player;
            canWarp= true;
        }


        public EXWeapon(Weapon weapon)
        {
            this.weapon = weapon;
        }
    }
    public static class MyTurnWeapon
    {
        private static readonly ConditionalWeakTable<Weapon, EXWeapon> modules = new ConditionalWeakTable<Weapon, EXWeapon>();

        public static EXWeapon myTurnWeapon(this Weapon weapon)=> modules.GetValue(weapon, (Weapon s) => new EXWeapon(weapon));


    }
}
