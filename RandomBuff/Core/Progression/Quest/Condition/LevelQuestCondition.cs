using Newtonsoft.Json;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;
using RWCustom;

namespace RandomBuff.Core.Progression.Quest.Condition
{
    internal class LevelQuestCondition : QuestCondition
    {
        public override string TypeName => "LevelCondition";
        public override string ConditionMessage()
        {
            return string.Format(BuffResourceString.Get("Quest_Display_LevelQuest"), level);
        }

        public override bool UpdateUnlockedState(WinGamePackage package)
        {
            if (BuffPlayerData.Instance.PlayerLevel >= level)
                return true;
            return false;
        }

        public override bool VerifyData()
        {
            return level > 0;
        }

        [JsonProperty("Level")]
        private int level;

        public int Level => level;
    }
}
