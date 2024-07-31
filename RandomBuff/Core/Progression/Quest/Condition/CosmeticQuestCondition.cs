using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;
using RWCustom;

namespace RandomBuff.Core.Progression.Quest.Condition
{
    internal class CosmeticQuestCondition : QuestCondition
    {
        public override string TypeName => "CosmeticCondition";

        [JsonProperty("CosmeticID")]
        string CosmeticRawId
        {
            set => cosmeticId = new CosmeticUnlockID(value);
        }

        private CosmeticUnlockID cosmeticId;

        public override string ConditionMessage()
        {
            if (!BuffResourceString.TryGet($"CosmeticUnlock_{cosmeticId.value}", out var id))
                id = Custom.rainWorld.inGameTranslator.Translate(cosmeticId.value);
            return string.Format(BuffResourceString.Get("Quest_Display_CosmeticQuest"),id);
        }

        public override bool UpdateUnlockedState(WinGamePackage package)
        {
            return !BuffConfigManager.IsItemLocked(QuestUnlockedType.Cosmetic, cosmeticId.value);
        }

        public override bool VerifyData()
        {
            return cosmeticId.index != -1;
        }
    }
}
