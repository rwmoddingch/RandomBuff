using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;

namespace BuiltinBuffs.Positive
{
    internal class ShortCircuitGateBuff : Buff<ShortCircuitGateBuff, ShortCircuitGateBuffData>
    {
        public override BuffID ID => ShortCircuitGateIBuffEntry.ShortCircuitGateBuffID;
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
            On.DeathPersistentSaveData.CanUseUnlockedGates += DeathPersistentSaveData_CanUseUnlockedGates;
            On.RegionGate.Unlock += RegionGate_Unlock;
        }

        private static void RegionGate_Unlock(On.RegionGate.orig_Unlock orig, RegionGate self)
        {
            orig.Invoke(self);
            //disable Buff
        }

        private static bool DeathPersistentSaveData_CanUseUnlockedGates(On.DeathPersistentSaveData.orig_CanUseUnlockedGates orig, DeathPersistentSaveData self, SlugcatStats.Name slugcat)
        {
            orig.Invoke(self, slugcat);
            return true;
        }
    }
}
