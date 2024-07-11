using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RandomBuff;
using RandomBuff.Core.Game.Settings.Conditions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Negative.SephirahMeltdown.Conditions
{
    internal class FixedCycleCondition : Condition
    {
        public static readonly ConditionID FixedCycle = new ConditionID(nameof(FixedCycle), true);
        public override ConditionID ID => FixedCycle;
        public override int Exp => 300;

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
