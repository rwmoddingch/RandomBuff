using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuiltinBuffs.Duality
{
    internal class MobileArsenalBuff : Buff<MobileArsenalBuff, MobileArsenalBuffData>
    {
        public override BuffID ID => MobileArsenalBuffEntry.mobileArsenalID;
        public MobileArsenalBuff()
        {
            if (StaticWorld.creatureTemplates == null)
                return;
            StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Scavenger).grasps = 10;
        }

        public override void Destroy()
        {
            if (StaticWorld.creatureTemplates == null)
                return;
            StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Scavenger).grasps = 4;
        }
    }

    internal class MobileArsenalBuffData : BuffData
    {
        public override BuffID ID => MobileArsenalBuffEntry.mobileArsenalID;
    }

    internal class MobileArsenalBuffEntry : IBuffEntry
    {
        public static BuffID mobileArsenalID = new BuffID("MobileArsenal", true);

        public void OnEnable()
        {

        }

        public static void HookOn()
        {

        }
    }
}
