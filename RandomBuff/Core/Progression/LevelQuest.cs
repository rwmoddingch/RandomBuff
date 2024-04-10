using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;
using RWCustom;

namespace RandomBuff.Core.Progression
{
    internal class LevelQuest : BuffQuest
    {
        public override string TypeName => nameof(LevelQuest);
        public override string QuestMessage()
        {
            return string.Format(Custom.rainWorld.inGameTranslator.Translate("Reach Level {} to Unlock"), level);
        }

        public override bool UpdateUnlockedState(BuffPoolManager.WinGamePackage package)
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
    }
}
