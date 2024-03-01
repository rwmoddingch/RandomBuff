using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Render.UI;
using UnityEngine;
using UnityEngine.Profiling;

namespace BuiltinBuffs.Negative
{
    internal class GamblerBuff : Buff<GamblerBuff,GamblerBuffData>
    {
        public override BuffID ID => GamblerBuffEntry.Gambler;

    }
    internal class GamblerBuffData : BuffData
    {
        public override BuffID ID => GamblerBuffEntry.Gambler;
    }

    internal class GamblerBuffEntry : IBuffEntry
    {
        public static BuffID Gambler = new BuffID("Gambler",true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<GamblerBuff,GamblerBuffData,GamblerBuffEntry>(Gambler);
        }

        public static void LongLifeCycleHookOn()
        {
            On.RandomBuff.Render.UI.CardPickerSlot.ctor += CardPickerSlot_ctor;
            On.RandomBuff.Core.SaveData.BuffConfigManager.GetStaticData += BuffConfigManager_GetStaticData;
        }

        private static BuffStaticData BuffConfigManager_GetStaticData(On.RandomBuff.Core.SaveData.BuffConfigManager.orig_GetStaticData orig, BuffID id)
        {
            var re = orig(id);
            if (inCtor)
            {
                
                re.GetType().GetProperty("BuffType").SetValue(re, BuffType.Positive);
                re.GetType().GetProperty("FaceName").SetValue(re, "Futile_White");
                re.GetType().GetProperty("Color").SetValue(re, Color.black);
                foreach (var m in re.CardInfos)
                {
                    m.Value.Description = "?????????";
                    m.Value.BuffName = "???";
                }
            }

            return re;
        }


        private static bool inCtor = false;

        private static void CardPickerSlot_ctor(On.RandomBuff.Render.UI.CardPickerSlot.orig_ctor orig,
            CardPickerSlot self, BasicInGameBuffCardSlot inGameBuffCardSlot, Action<BuffID> selectCardCallBack, BuffID[] majorSelections, BuffID[] additionalSelections, int numOfChoices)
        {
            inCtor = true;
            orig(self,inGameBuffCardSlot,selectCardCallBack,majorSelections, additionalSelections, numOfChoices);
            inCtor = false;
        }

     
    }
}
