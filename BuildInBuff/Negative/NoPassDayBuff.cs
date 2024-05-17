using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuiltinBuffs.Negative
{
    internal class NoPassDayBuff : Buff<NoPassDayBuff, NoPassDayBuffData>
    {
        public override BuffID ID => NoPassDayBuffEntry.noPassDayBuffID;

        public NoPassDayBuff()
        {
            RandomBuffUtils.BuffEvent.OnGateLoaded += BuffEvent_OnGateLoaded;
        }

        public override void Destroy()
        {
            RandomBuffUtils.BuffEvent.OnGateLoaded -= BuffEvent_OnGateLoaded;
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
    }
}
