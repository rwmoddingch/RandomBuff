
using MoreSlugcats;
using Newtonsoft.Json;
using RandomBuffUtils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    public class AchievementCondition : Condition
    {
        public override void EnterGame(RainWorldGame game)
        {
            base.EnterGame(game);
            BuffEvent.OnAchievementCompleted += BuffEvent_OnAchievementCompleted;
        }

        private void BuffEvent_OnAchievementCompleted(List<WinState.EndgameID> newFinished, List<WinState.EndgameID> newUnfinished)
        {
            if (newFinished.Contains(achievementID))
                Finished = true;

        }

        public override ConditionID ID => ConditionID.Achievement;

        public override int Exp => 150;

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty,
            List<Condition> conditions = null)
        {
            conditions ??= new List<Condition>();
            var re = WinState.EndgameID.values.entries.Select(i => new WinState.EndgameID(i)).Where(i =>
                conditions.OfType<AchievementCondition>().All(j => j.achievementID != i) && !string.IsNullOrWhiteSpace(WinState.PassageDisplayName(i))).ToList();
            re.Remove(MoreSlugcatsEnums.EndgameID.Mother);
            re.Remove(MoreSlugcatsEnums.EndgameID.Gourmand);

            if (name == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                re.Remove(WinState.EndgameID.Chieftain);
            else if (name == MoreSlugcatsEnums.SlugcatStatsName.Saint)
            {
                re.Remove(WinState.EndgameID.DragonSlayer);
                re.Remove(WinState.EndgameID.Hunter);
                re.Remove(WinState.EndgameID.Outlaw);
                re.Remove(WinState.EndgameID.Scholar);
            }
            else if (name == MoreSlugcatsEnums.SlugcatStatsName.Rivulet)
            {
                re.Remove(WinState.EndgameID.Scholar);
            }

            achievementID = re[Random.Range(0, re.Count)];
            return ConditionState.Ok_More;
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return "";
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(BuffResourceString.Get("DisplayName_Achievement"),
                translator.Translate( string.IsNullOrWhiteSpace(WinState.PassageDisplayName(achievementID)) ? "[MISSING ACHIEVEMENT]" : (WinState.PassageDisplayName(achievementID))));
        }

        //这个必须延后
        ~AchievementCondition()
        {
            BuffEvent.OnAchievementCompleted -= BuffEvent_OnAchievementCompleted;
        }

        

        [JsonProperty]
        public WinState.EndgameID achievementID;
    }
}
