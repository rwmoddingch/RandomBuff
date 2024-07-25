using Newtonsoft.Json;
using RandomBuff.Core.Game;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuff.Core.SaveData;
using RWCustom;

namespace RandomBuff.Core.Progression.Quest.Condition
{
    internal class MissionQuestCondition : QuestCondition
    {
        public override string TypeName => "MissionCondition";
        public override string ConditionMessage()
        {
            string missionName = "???";
            if (MissionRegister.TryGetMission(new MissionID(missionId), out var mission))
                missionName = mission.MissionName;
            return string.Format(BuffResourceString.Get("Quest_Display_MissionQuest"),
                Custom.rainWorld.inGameTranslator.Translate(missionName));
        }

        public override bool UpdateUnlockedState(WinGamePackage package)
        {
            return BuffPlayerData.Instance.finishedMission.Contains(missionId);

        }

        public override bool VerifyData()
        {
            return missionId != null;
        }

        [JsonProperty("MissionID")]
        private string missionId;
    }
}
