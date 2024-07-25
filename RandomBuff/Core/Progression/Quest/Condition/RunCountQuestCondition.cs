using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;

namespace RandomBuff.Core.Progression.Quest.Condition
{
    internal class RunCountQuestCondition : QuestCondition
    {
        public override string TypeName => "RunCountCondition";
        public override string ConditionMessage()
        {
            return string.Format(
                BuffResourceString.Get("Quest_Display_RunCountQuest"), targetCount);
        }

        public override bool UpdateUnlockedState(WinGamePackage package)
        {
            return BuffPlayerData.Instance.SlotRecord.RunCount >= targetCount;
        }

        public override bool VerifyData()
        {
            return true;
        }

        [JsonProperty(PropertyName = "TargetCount")] 
        private int targetCount;
    }
}
