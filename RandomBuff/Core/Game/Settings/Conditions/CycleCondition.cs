
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    internal class CycleCondition : Condition
    {
        public override ConditionID ID => ConditionID.Cycle;

        public override void EnterGame(RainWorldGame game)
        {
            currentCycle = game.GetStorySession.saveState.cycleNumber;
            Finished = currentCycle >= cycle;
        }

        public override void SessionEnd(SaveState save)
        {
            currentCycle = save.cycleNumber + 1;
            Finished = currentCycle >= cycle;
            BuffPlugin.LogDebug($"Cycle Condition  {save.cycleNumber+1}:{cycle}");

        }

        public override void SetRandomParameter(float difficulty)
        {
            cycle = (int)Random.Range(Mathf.Lerp(5, 15, difficulty), Mathf.Lerp(10, 30, difficulty));
            BuffPlugin.LogDebug($"Add Cycle Condition {cycle}");
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"({currentCycle}/{cycle})";
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(translator.Translate("Survive {0} cycles"), cycle);
        }


        [JsonProperty]
        private int cycle;

        [JsonProperty] 
        private int currentCycle;
    }
}
