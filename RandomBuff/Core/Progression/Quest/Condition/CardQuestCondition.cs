using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;

namespace RandomBuff.Core.Progression.Quest.Condition
{
    internal class CardQuestCondition : QuestCondition
    {
        public override string TypeName => "CardCondition";
        public override string ConditionMessage()
        {
            return string.Format(
                BuffResourceString.Get(isSingleRun ? "Quest_Display_SingleRunCardQuest" : "Quest_Display_CardQuest"),
                BuffResourceString.Get(isAll ? "all types" : cardType.ToString()), count);
        }



        public override bool UpdateUnlockedState(WinGamePackage package)
        {
            if (isSingleRun)
            {
                if (isAll)
                    return package.buffRecord.totCard >= count;
                switch (cardType)
                {
                    case BuffType.Duality:
                        return package.buffRecord.totDualityCard >= count;
                        break;
                    case BuffType.Positive:
                        return package.buffRecord.totPositiveCard >= count;
                        break;
                    case BuffType.Negative:
                        return package.buffRecord.totNegativeCard >= count;
                        break;
                    default:
                        return false;
                }
            }
            else
            {
                if (isAll)
                    return BuffPlayerData.Instance.SlotRecord.totCard >= count;
                switch (cardType)
                {
                    case BuffType.Duality:
                        return BuffPlayerData.Instance.SlotRecord.totDualityCard >= count;
                        break;
                    case BuffType.Positive:
                        return BuffPlayerData.Instance.SlotRecord.totPositiveCard >= count;
                        break;
                    case BuffType.Negative:
                        return BuffPlayerData.Instance.SlotRecord.totNegativeCard >= count;
                        break;
                    default:
                        return false;
                }
            }
        }

        public override bool VerifyData()
        {
            return count > 0;
        }

        [JsonProperty]
        private string CardType
        {
            set
            {
                if (value == "All")
                    isAll = true;
                else
                    cardType = (BuffType)Enum.Parse(typeof(BuffType), value);
            }
        }

        [JsonProperty(PropertyName = "SingleRun")]
        private bool isSingleRun;

        [JsonProperty(PropertyName = "TargetCount")] 
        private int count;

        private bool isAll;



        private BuffType cardType;
    }
}
