using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreSlugcats;
using RandomBuffUtils;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    internal class GourmandCondition : Condition
    {
        public override ConditionID ID => ConditionID.Gourmand;
        public override int Exp => 700;
        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> sameConditions)
        {
            if (name != MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
                return ConditionState.Fail;
            return ConditionState.Ok_NoMore;
        }

        public override void InGameUpdate(RainWorldGame game)
        {
            if (game.GetStorySession.saveState.deathPersistentSaveData.winState.GetTracker(
                    MoreSlugcatsEnums.EndgameID.Gourmand, false) is
                WinState.GourFeastTracker tracker)
            {
                int index = 0;
                for (;
                     tracker.currentCycleProgress[index] > 0 && index < tracker.currentCycleProgress.Length;
                     index++) ;

                if (index != currentProgress)
                {
                    currentProgress = index;
                    onLabelRefresh?.Invoke(this);
                    Finished = index == WinState.GourmandPassageTracker.Length;
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

            return "ERROR";
        }


        private int currentProgress = 0;
        public override string DisplayName(InGameTranslator translator)
        {
            return BuffResourceString.Get("DisplayName_Gourmand");

        }
    }
}
