using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod.Cil;
using RandomBuff;
using RandomBuff.Core.Buff;
using RWCustom;

namespace BuiltinBuffs.Negative.SephirahMeltdown
{
    internal class ChesedBuffData : SephirahMeltdownBuffData
    {
        public static readonly BuffID Chesed = new BuffID(nameof(Chesed), true);
        public override BuffID ID => Chesed;

        public float DeathMulti => Custom.LerpMap(CycleUse, 0, MaxCycleCount - 1, 1, 5f);
        public float SpeedMulti => Custom.LerpMap(CycleUse, 0, MaxCycleCount - 1, 1, 2f);
        public float SpeedMulti2 => Custom.LerpMap(CycleUse, 0, MaxCycleCount - 1, 1, 1.5f);


    }

    internal class ChesedBuff : Buff<ChesedBuff,ChesedBuffData>
    {
        public override BuffID ID => ChesedBuffData.Chesed;
    }

    internal class ChesedBuffHook
    {
        public static void HookOn()
        {
            On.Player.DeathByBiteMultiplier += Player_DeathByBiteMultiplier;
            On.VultureAI.OnlyHurtDontGrab += VultureAI_OnlyHurtDontGrab;
            On.Lizard.GetFrameSpeed += Lizard_GetFrameSpeed;
            IL.BigSpider.MoveTowards += BigSpider_MoveTowards;
        }

        private static void BigSpider_MoveTowards(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After, i => i.MatchLdcR4(4.1f));
            c.EmitDelegate<Func<float, float>>(f => f * ChesedBuffData.Chesed.GetBuffData<ChesedBuffData>().SpeedMulti2);
        }

        private static float Lizard_GetFrameSpeed(On.Lizard.orig_GetFrameSpeed orig, Lizard self, float runSpeed)
        {
            return orig(self, runSpeed) * ChesedBuffData.Chesed.GetBuffData<ChesedBuffData>().SpeedMulti;
        }

        private static bool VultureAI_OnlyHurtDontGrab(On.VultureAI.orig_OnlyHurtDontGrab orig, VultureAI self, PhysicalObject testObj)
        {
            if (testObj is Player)
                return true;
            return orig(self, testObj);
        }

        private static float Player_DeathByBiteMultiplier(On.Player.orig_DeathByBiteMultiplier orig, Player self)
        {
            var re = orig(self);
            return re * ChesedBuffData.Chesed.GetBuffData<ChesedBuffData>().DeathMulti;
        }
    }
}
