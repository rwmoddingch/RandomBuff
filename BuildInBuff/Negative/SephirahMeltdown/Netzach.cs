using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;

namespace BuiltinBuffs.Negative.SephirahMeltdown
{
    internal class NetzachBuffData : SephirahMeltdownBuffData
    {
        public static readonly BuffID Netzach = new BuffID(nameof(Netzach), true);
        public override BuffID ID => Netzach;

        public float FoodMulti => 1 / Custom.LerpMap(CycleUse, 0, MaxCycleCount-1, 1f, 3f);
    }

    internal class NetzachBuff : Buff<NetzachBuff, NetzachBuffData>
    {
        public override BuffID ID => NetzachBuffData.Netzach;

        public NetzachBuff()
        {
            NetzachHook.Foods.Clear();
        }
    }

    internal class NetzachHook
    {
        public static void HookOn()
        {
            On.Player.AddFood += Player_AddFood;
            On.Player.AddQuarterFood += Player_AddQuarterFood;
            On.SeedCob.HitByWeapon += SeedCob_HitByWeapon;
        }

        private static void SeedCob_HitByWeapon(On.SeedCob.orig_HitByWeapon orig, SeedCob self, Weapon weapon)
        {
            if (weapon == null || self.room == null || self.room.roomSettings == null)
                return;
            if (weapon is Spear && weapon.firstChunk.vel.magnitude < 20f)
            {

                if (UnityEngine.Random.Range(0.5f, 0.8f) < self.freezingCounter)
                    self.spawnUtilityFoods();
                return;
            }
            orig(self,weapon);
        }

        private static bool useOrig = false;
        private static bool useFullOrig = false;
        private static void Player_AddQuarterFood(On.Player.orig_AddQuarterFood orig, Player self)
        {
            if (useOrig)
            {
                useFullOrig = true;
                orig(self);
                useFullOrig = false;
                useOrig = false;
                return;
            }
            if (!Foods.ContainsKey(self.playerState.playerNumber))
                Foods.Add(self.playerState.playerNumber, 0);
            Foods[self.playerState.playerNumber] +=
                0.25f * NetzachBuffData.Netzach.GetBuffData<NetzachBuffData>().FoodMulti;
            BuffUtils.Log(NetzachBuffData.Netzach, $"Add Quarter food, current: {Foods[self.playerState.playerNumber]}, ID:{self.playerState.playerNumber}");

            if (Foods[self.playerState.playerNumber] > 0.25f)
            {
                Foods[self.playerState.playerNumber] -= 0.25f;
                useFullOrig = true;
                orig(self);
                useFullOrig = false;
            }
            else
            {
                self.abstractCreature.world.game.cameras[0].hud.PlaySound(SoundID.HUD_Food_Meter_Fill_Quarter_Plop);
            }
        }

        private static void Player_AddFood(On.Player.orig_AddFood orig, Player self, int add)
        {
            if (!Foods.ContainsKey(self.playerState.playerNumber))
                Foods.Add(self.playerState.playerNumber, 0);

            if (useFullOrig)
            {
                orig(self, add);
                return;
            }
            Foods[self.playerState.playerNumber] += add * NetzachBuffData.Netzach.GetBuffData<NetzachBuffData>().FoodMulti;
            BuffUtils.Log(NetzachBuffData.Netzach,$"Add food, current: {Foods[self.playerState.playerNumber]}, ID:{self.playerState.playerNumber}");
            if (Foods[self.playerState.playerNumber] >= 1)
            {
                orig(self, (int)Foods[self.playerState.playerNumber]);
                Foods[self.playerState.playerNumber] %= 1;
            }

            while (Foods[self.playerState.playerNumber] >= 0.25f)
            {
                Foods[self.playerState.playerNumber] -= 0.25f;
                useOrig = true;
                self.AddQuarterFood();
            }
        }

        public static readonly Dictionary<int, float> Foods = new Dictionary<int, float>();
    }
}
