using System.Collections.Generic;
using Newtonsoft.Json;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    internal abstract class IntervalCondition : Condition
    {
        protected bool IsCanUse => currentCycle >= minConditionCycle && currentCycle < maxConditionCycle;

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
            => ConditionState.Ok_NoMore;

        public abstract string InRangeDisplayName();
        public abstract string InRangeDisplayProgress();
        

        public override void EnterGame(RainWorldGame game)
        {
            base.EnterGame(game);
            currentCycle = game.GetStorySession.saveState.cycleNumber;
            Failed = currentCycle >= maxConditionCycle && !Finished;
        }

        public override void SessionEnd(SaveState save)
        {
            currentCycle = save.cycleNumber + 1;
            Failed = currentCycle >= maxConditionCycle && !Finished;
        }


        public sealed override string DisplayProgress(InGameTranslator translator) 
        {
            if (IsCanUse || Finished)
                return InRangeDisplayProgress();
            return "";
        }

        public sealed override string DisplayName(InGameTranslator translator)
        {
            if (IsCanUse || Finished)
                return InRangeDisplayName();
            return string.Format(BuffResourceString.Get("DisplayName_Interval_OutRange"),minConditionCycle,maxConditionCycle);
        }

        [JsonProperty]
        private int currentCycle;

        [JsonProperty]
        public int minConditionCycle = 0;

        [JsonProperty]
        public int maxConditionCycle = int.MaxValue;
    }
}
