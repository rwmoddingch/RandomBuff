using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotDogGains.Positive
{
    class BananaSkinUnderFitBuff : Buff<BananaSkinUnderFitBuff, BananaSkinUnderFitBuffData>{public override BuffID  ID => BananaSkinUnderFitBuffEntry.BananaSkinUnderFitID;}
    class BananaSkinUnderFitBuffData :BuffData{public override BuffID ID => BananaSkinUnderFitBuffEntry.BananaSkinUnderFitID;}
    class BananaSkinUnderFitBuffEntry : IBuffEntry
    {
        public static BuffID BananaSkinUnderFitID = new BuffID("BananaSkinUnderFitID", true);
        public void OnEnable()
        {
            //BuffRegister.RegisterBuff<BananaSkinUnderFitBuff,BananaSkinUnderFitBuffData,BananaSkinUnderFitBuffEntry>(BananaSkinUnderFitID);
        }
            public static void HookOn()
        {
            
        }
    }
}