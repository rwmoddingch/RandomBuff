using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RandomBuff.Core.Game;
using RandomBuff.Core.Game.Settings.Missions;
using RWCustom;

namespace RandomBuff.Core.Progression
{
    internal class MissionQuest : BuffQuest
    {
        public override string TypeName => nameof(MissionQuest);
        public override string QuestMessage()
        {
            string missionName = "???";
            if (MissionRegister.TryGetMission(new MissionID(missionId), out var mission))
                missionName = mission.MissionName;
            return string.Format(Custom.rainWorld.inGameTranslator.Translate("Complete the Mission {}"),
                Custom.rainWorld.inGameTranslator.Translate(missionName));
        }

        public override bool UpdateUnlockedState(BuffPoolManager.WinGamePackage package)
        {
            return package.missionId == missionId;

        }

        public override bool VerifyData()
        {
            return missionId != null;
        }

        [JsonProperty("MissionID")]
        private string missionId;
    }
}
