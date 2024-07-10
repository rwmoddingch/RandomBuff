using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff;
using RandomBuff.Core.Buff;
using RWCustom;
using UnityEngine;

namespace BuiltinBuffs.Negative.SephirahMeltdown
{
    internal class NetzachBuffData : SephirahMeltdownBuffData
    {
        public static readonly BuffID Netzach = new BuffID(nameof(Netzach), true);
        public override BuffID ID => Netzach;

        public float FoodMulti => 1 / Custom.LerpMap(CycleUse, 0, MaxCycleCount-1, 2f, 4f);
    }

    internal class NetzachBuff : Buff<NetzachBuff, NetzachBuffData>
    {
        public override BuffID ID => NetzachBuffData.Netzach;

        public NetzachBuff()
        {

        }
    }

    internal class NetzachHook
    {
        static void HookOn()
        {
            On.Player.AddFood += Player_AddFood;
            On.Player.AddQuarterFood += Player_AddQuarterFood;
        }

        private static bool useOrig = false;
        private static void Player_AddQuarterFood(On.Player.orig_AddQuarterFood orig, Player self)
        {
            if (useOrig)
            {
                orig(self);
                useOrig = false;
                return;
            }
            if (!Foods.ContainsKey(self.playerState.playerNumber))
                Foods.Add(self.playerState.playerNumber, 0);
            Foods[self.playerState.playerNumber] +=
                0.25f * NetzachBuffData.Netzach.GetBuffData<NetzachBuffData>().FoodMulti;
            if (Foods[self.playerState.playerNumber] > 0.25f)
            {
                Foods[self.playerState.playerNumber] -= 0.25f;
                orig(self);
            }
        }

        private static void Player_AddFood(On.Player.orig_AddFood orig, Player self, int add)
        {
            if (!Foods.ContainsKey(self.playerState.playerNumber))
                Foods.Add(self.playerState.playerNumber, 0);
            Foods[self.playerState.playerNumber] += add * NetzachBuffData.Netzach.GetBuffData<NetzachBuffData>().FoodMulti;

            if (Foods[self.playerState.playerNumber] >= 1)
            {
                orig(self, (int)Foods[self.playerState.playerNumber]);
                Foods[self.playerState.playerNumber] %= 1;
            }

            while (Foods[self.playerState.playerNumber] > 0.25f)
            {
                Foods[self.playerState.playerNumber] -= 0.25f;
                useOrig = true;
                self.AddQuarterFood();
            }
        }

        public static readonly Dictionary<int, float> Foods = new Dictionary<int, float>();
    }
}
