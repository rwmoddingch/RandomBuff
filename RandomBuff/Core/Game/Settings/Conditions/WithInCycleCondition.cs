
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    public class WithInCycleCondition : Condition
    {
        public override ConditionID ID => ConditionID.WithInCycle;
        public override int Exp => cycle * 10;

        public int SetCycle
        {
            set
            {
                cycle = value;
            }
        }

        public override void EnterGame(RainWorldGame game)
        {
            currentCycle = game.GetStorySession.saveState.cycleNumber;
            Finished = currentCycle <= cycle;
        }

        public override void SessionEnd(SaveState save)
        {
            currentCycle = save.cycleNumber + 1;
            Finished = currentCycle <= cycle;
            BuffPlugin.LogDebug($"With Cycle Condition  {save.cycleNumber+1}:{cycle}");

        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty,
            List<Condition> conditions = null)
        {
            cycle = (int)Random.Range(5, 30);
            BuffPlugin.LogDebug($"Add With Cycle Condition {cycle}");
            return ConditionState.Ok_NoMore;
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"({currentCycle}/{cycle})";
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(BuffResourceString.Get("DisplayName_WithInCycle"), cycle);
        }


        [JsonProperty]
        private int cycle;

        [JsonProperty] 
        private int currentCycle;
    }
}
