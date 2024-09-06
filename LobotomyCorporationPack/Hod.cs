using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuffUtils;
using RWCustom;

namespace BuiltinBuffs.Negative.SephirahMeltdown
{
    internal class HodBuffData : SephirahMeltdownBuffData
    {
        public static readonly BuffID Hod = new BuffID(nameof(Hod), true);
        public override BuffID ID => Hod;

        public float SpeedFac => Custom.LerpMap(CycleUse, 0, MaxCycleCount-1, 0.9f, 0.7f);
    }

    internal class HodBuff : Buff<HodBuff,HodBuffData>
    {
        public override BuffID ID => HodBuffData.Hod;

        public HodBuff()
        {
            var fac = HodBuffData.Hod.GetBuffData<HodBuffData>().SpeedFac;
            foreach (var self in (BuffCustom.TryGetGame(out var game) ? game.Players : new List<AbstractCreature>())
                     .Select(i => i.realizedCreature as Player).Where(i => !(i is null)))
            {
                self.slugcatStats.Modify(this, PlayerUtils.Multiply, "corridorClimbSpeedFac", fac);
                self.slugcatStats.Modify(this, PlayerUtils.Multiply, "poleClimbSpeedFac", fac);
                self.slugcatStats.Modify(this, PlayerUtils.Multiply, "runspeedFac", fac);
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            PlayerUtils.UndoAll(this);
        }

        public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self,abstractCreature,world);
            var fac = HodBuffData.Hod.GetBuffData<HodBuffData>().SpeedFac;
            self.slugcatStats.Modify(Instance, PlayerUtils.Multiply, "corridorClimbSpeedFac", fac);
            self.slugcatStats.Modify(Instance, PlayerUtils.Multiply, "poleClimbSpeedFac", fac);
            self.slugcatStats.Modify(Instance, PlayerUtils.Multiply, "runspeedFac", fac);
        }
    }
}
