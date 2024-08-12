using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreSlugcats;
using Newtonsoft.Json;
using RandomBuffUtils;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    public class GourmandCondition : Condition
    {
        public override ConditionID ID => ConditionID.Gourmand;
        public override int Exp => 400;
        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            if (name != MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
                return ConditionState.Fail;
            return ConditionState.Ok_NoMore;
        }

        public override void InGameUpdate(RainWorldGame game)
        {
            if (game.GetStorySession.saveState.deathPersistentSaveData.winState.GetTracker(
                    MoreSlugcatsEnums.EndgameID.Gourmand, false) is
                WinState.GourFeastTracker tracker && tracker.progress != null && tracker.currentCycleProgress != null)
            {
               
                int count = 0;
                for (int index = 0; index < tracker.progress.Length; index++)
                    if (tracker.progress[index] > 0 || tracker.currentCycleProgress[index] > 0)
                        count++;
                if (currentProgress != count)
                {
                    currentProgress = count;
                    onLabelRefresh?.Invoke(this);
                    if (currentProgress == tracker.progress.Length)
                        Finished = true;
                }
            }
        

        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            if (BuffCustom.TryGetGame(out var game) && 
                game.GetStorySession.saveState.deathPersistentSaveData.winState.GetTracker(MoreSlugcatsEnums.EndgameID.Gourmand, false) is
                    { })
            {
                return $"({currentProgress}/{WinState.GourmandPassageTracker.Length})";
            }

            return "";
        }

        [JsonProperty]
        private int currentProgress = 0;
        public override string DisplayName(InGameTranslator translator)
        {
            return BuffResourceString.Get("DisplayName_Gourmand");

        }
    }
}
