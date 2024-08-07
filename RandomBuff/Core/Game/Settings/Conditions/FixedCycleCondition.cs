using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    public class FixedCycleCondition : Condition
    {
        public override ConditionID ID => ConditionID.FixedCycle;
        public override int Exp => 200;

        public int SetCycle
        {
            set => cycle = value;
        }

        public override void EnterGame(RainWorldGame game)
        {
            currentCycle = game.GetStorySession.saveState.cycleNumber;
            Finished = currentCycle == cycle;
        }

        public override void SessionEnd(SaveState save)
        {
            currentCycle = save.cycleNumber + 1;
            Finished = currentCycle == cycle;
      

        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty,
            List<Condition> sameConditions)
        {
            cycle = (int)Random.Range(Mathf.Lerp(5, 15, difficulty), Mathf.Lerp(10, 30, difficulty));
            return ConditionState.Ok_NoMore;
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"({currentCycle}/{cycle})";
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(BuffResourceString.Get("DisplayName_FixCycle"), cycle);
        }


        [JsonProperty]
        private int cycle;

        [JsonProperty]
        private int currentCycle;
    }
}
