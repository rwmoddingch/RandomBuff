using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RandomBuff;
using RandomBuff.Core.Game.Settings.Conditions;
using RWCustom;

namespace BuiltinBuffs.Negative.SephirahMeltdown.Conditions
{
    internal abstract class IntervalCondition : Condition
    {
        protected bool IsCanUse => currentCycle >= minConditionCycle && currentCycle < maxConditionCycle;

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> sameConditions)
            => ConditionState.Ok_NoMore;

        public abstract string InRangeDisplayName();
        public abstract string InRangeDisplayProgress();



        public override void EnterGame(RainWorldGame game)
        {
            currentCycle = game.GetStorySession.saveState.cycleNumber;
        }

        public override void SessionEnd(SaveState save)
        {
            currentCycle = save.cycleNumber + 1;
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
