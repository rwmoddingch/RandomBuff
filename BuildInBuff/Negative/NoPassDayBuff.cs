using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MonoMod.RuntimeDetour;

namespace BuiltinBuffs.Negative
{
    internal class NoPassDayBuff : Buff<NoPassDayBuff, NoPassDayBuffData>
    {
        public override BuffID ID => NoPassDayBuffEntry.noPassDayBuffID;

        public NoPassDayBuff()
        {
            BuffEvent.OnGateLoaded += BuffEvent_OnGateLoaded;
        }

        public override void Destroy()
        {
            BuffEvent.OnGateLoaded -= BuffEvent_OnGateLoaded;
        }

        private void BuffEvent_OnGateLoaded(RandomBuffUtils.BuffEvents.BuffRegionGateEvent.RegionGateInstance gateInstance)
        {
            gateInstance.EnergyEnoughToOpen = false;
            BuffUtils.Log("NoPassDayBuff", "BuffEvent_OnGateLoaded");
        }
    }

    internal class NoPassDayBuffData : CountableBuffData
    {
        public override BuffID ID => NoPassDayBuffEntry.noPassDayBuffID;

        public override int MaxCycleCount => 1;
    }

    internal class NoPassDayBuffEntry : IBuffEntry
    {
        public static BuffID noPassDayBuffID = new BuffID("NoPassDay", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<NoPassDayBuff, NoPassDayBuffData, NoPassDayBuffEntry>(noPassDayBuffID);
        }

        public static void HookOn()
        {

            _ = new Hook(typeof(WaterGate).GetProperty("EnergyEnoughToOpen").GetGetMethod(),
                typeof(NoPassDayBuffEntry).GetMethod("Gate_EnergyEnoughToOpen", BindingFlags.Static | BindingFlags.NonPublic));

            _ = new Hook(typeof(ElectricGate).GetProperty("EnergyEnoughToOpen").GetGetMethod(),
                typeof(NoPassDayBuffEntry).GetMethod("Gate_EnergyEnoughToOpen", BindingFlags.Static | BindingFlags.NonPublic));
        }

        private static bool Gate_EnergyEnoughToOpen(Func<RegionGate, bool> orig, RegionGate self)
        {
            return false;
        }
    }
}
