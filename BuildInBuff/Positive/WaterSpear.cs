using System;
using System.Runtime.CompilerServices;
using HotDogGains;
using MonoMod;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HotDogBuff
{
    //水中贵族
    class WaterNobleBuff : Buff<WaterNobleBuff, WaterNobleBuffData> { public override BuffID ID => WaterNobleBuffEntry.WaterNobleID; }
    class WaterNobleBuffData : BuffData { public override BuffID ID => WaterNobleBuffEntry.WaterNobleID; }
    class WaterNobleBuffEntry : IBuffEntry
    {
        /// <summary>
        /// 水中贵族
        /// 
        /// 水中扔矛攻击力两倍
        /// -----------我改成提升卡牌数量倍的攻击力了,为了符合叠加的条件
        /// </summary>
        public static BuffID WaterNobleID = new BuffID("WaterNobleID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<WaterNobleBuff, WaterNobleBuffData, WaterNobleBuffEntry>(WaterNobleID);
        }
        public static void HookOn()
        {
            On.Player.ThrowObject += AddWaterPower;
            On.Spear.ChangeMode += WhenSpearCantAttackReset;
            
            On.Player.Update += Player_Update;

            On.Creature.Violence += Creature_Violence;
            On.Lizard.Violence += Lizard_Violence;

        }

        private static void Lizard_Violence(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
        {
            if(source!=null)
            {
                if (source.owner is Weapon && (source.owner as Weapon).FromWater())
                {
                    //WaterNobleBuff.Instance.TriggerSelf(true);
                    damage *= 1 + WaterNobleID.GetBuffData().StackLayer;
                    BuffUtils.Log(WaterNobleID, "Water weapon Damage:" + damage);
                }
            }

            orig.Invoke(self,source,directionAndMomentum,hitChunk,onAppendagePos,type,damage,stunBonus);
        }
        private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (source!=null)
            {
                if (source.owner is Weapon && (source.owner as Weapon).FromWater())
                {
                    //WaterNobleBuff.Instance.TriggerSelf(true);
                    damage *= 1 + WaterNobleID.GetBuffData().StackLayer;
                    BuffUtils.Log(WaterNobleBuffEntry.WaterNobleID,"Water weapon Damage:" + damage);
                }
            }
            orig.Invoke(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        private static void WhenSpearCantAttackReset(On.Spear.orig_ChangeMode orig, Spear self, Weapon.Mode newMode)
        {
            orig.Invoke(self, newMode);
            if (self.mode != Spear.Mode.Thrown) self.FromWater(false);
        }
        private static void AddWaterPower(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
        {
            var spear = self.grasps[grasp].grabbed as Weapon;
            orig.Invoke(self, grasp, eu);
            if (spear != null && self.Submersion > 0)
            {
                spear.FromWater(true);
                BuffUtils.Log(WaterNobleBuffEntry.WaterNobleID,"Water weapon added");
            }

        }
        

        

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (self.Submersion>0)
            {
                for (int i = 0; i < self.grasps.Length; i++)
                {
                    if (self.grasps[i] !=null&& self.grasps[i].grabbed is Weapon)
                    {
                        //self.grasps[i].grabbed 
                    }
                }
            }
        }

        

    }
    public class WaterWeaponData
    {
        public Weapon weapon;
        public bool shootedFromWater = false;

        public WaterWeaponData(Weapon weapon)
        {
            this.weapon = weapon;
        }
    }
    public static   class WaterSpear
    {
        private static readonly ConditionalWeakTable<Weapon, WaterWeaponData>  modules= new ConditionalWeakTable<Weapon, WaterWeaponData>();

        public static WaterWeaponData Trident(this Weapon weapon)
        {
            return modules.GetValue(weapon, (Weapon s) => new WaterWeaponData(weapon));
        }
        public static bool FromWater(this Weapon weapon) => weapon.Trident().shootedFromWater;
        public static bool FromWater(this Weapon weapon,bool newBool) => weapon.Trident().shootedFromWater=newBool;
    }
}
