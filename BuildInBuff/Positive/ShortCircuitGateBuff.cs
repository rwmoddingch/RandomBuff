using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuffUtils.BuffEvents;

namespace BuiltinBuffs.Positive
{
    internal class ShortCircuitGateBuff : Buff<ShortCircuitGateBuff, ShortCircuitGateBuffData>
    {
        public override BuffID ID => ShortCircuitGateIBuffEntry.ShortCircuitGateBuffID;

        public override bool Trigger(RainWorldGame game)
        {
            return true;
        }
    }

    internal class ShortCircuitGateBuffData : BuffData
    {
        public override BuffID ID => ShortCircuitGateIBuffEntry.ShortCircuitGateBuffID;
    }

    internal class ShortCircuitGateIBuffEntry : IBuffEntry
    {
        public static BuffID ShortCircuitGateBuffID = new BuffID("ShortCircuitGate", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ShortCircuitGateBuff, ShortCircuitGateBuffData, ShortCircuitGateIBuffEntry>(ShortCircuitGateBuffID);
        }

        public static void HookOn()
        {
            BuffEvent.OnGateOpened += BuffEvent_OnGateOpened;
        }

        private static void BuffEvent_OnGateOpened(BuffRegionGateEvent.RegionGateInstance gateInstance)
        {
            gateInstance.Unlocked = true;
            ShortCircuitGateBuff.Instance.TriggerSelf(true);
        }
    }
}
