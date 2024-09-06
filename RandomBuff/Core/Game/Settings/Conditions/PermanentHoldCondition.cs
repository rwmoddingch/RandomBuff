using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Progression.Quest;
using RandomBuff.Core.SaveData;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    public class PermanentHoldCondition : Condition
    {
        public static ConditionID permanentHoldConditionID = new ConditionID("PermanentlyHold", true);
        public override ConditionID ID => permanentHoldConditionID;

        [JsonProperty]
        string id;

        [JsonProperty]
        string name;
        public BuffID HoldBuff
        {
            get => new BuffID(id);
            set
            {
                id = value.value;

                if (BuffConfigManager.GetStaticData(value).CardInfos.TryGetValue(Custom.rainWorld.inGameTranslator.currentLanguage, out var info))
                {
                    name = info.BuffName;
                }
                else
                    name = BuffConfigManager.GetStaticData(value).CardInfos[InGameTranslator.LanguageID.English].BuffName;
            }
        }
        public override int Exp => 200;

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(BuffResourceString.Get("DisplayName_PermanentlyHold"), name);
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return "";
        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            List<BuffID> selections = new List<BuffID>();

            foreach(var idvalue in BuffID.values.entries)
            {
                var id = new BuffID(idvalue);
                if (BuffConfigManager.ContainsId(id))
                {
                    var staticData = BuffConfigManager.GetStaticData(id);

                    if (staticData.BuffType != BuffType.Negative)
                        continue;

                    if (!BuffConfigManager.IsItemLocked(QuestUnlockedType.Card, idvalue))
                        continue;

                    selections.Add(id);
                }
            }

            foreach(var condition in conditions)
            {
                if(condition is PermanentHoldCondition permanentHoldCondition)
                {
                    selections.Remove(permanentHoldCondition.HoldBuff);
                }
            }

            if (selections.Count == 0)
                return ConditionState.Fail;

            var selected = RXRandom.AnyItem(selections);
            HoldBuff = selected;

            return ConditionState.Ok_More;
        }

        int counter = 40;
        public override void InGameUpdate(RainWorldGame game)
        {
            base.InGameUpdate(game);

            if (counter > 80)
                counter = 0;
            else
                counter++;

            if (counter == 0)
            {
                if (!BuffCore.GetAllBuffIds().Contains(HoldBuff))
                    HoldBuff.CreateNewBuff();

                if (!Finished)
                {
                    Finished = true;
                    //onLabelRefresh?.Invoke(this);
                }
            }
        }
    }
}
