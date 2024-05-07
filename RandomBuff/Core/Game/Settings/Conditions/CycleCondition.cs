
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
        public override float Exp => cycle * 40;

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
            Finished = currentCycle >= cycle;
        }

        public override void SessionEnd(SaveState save)
        {
            currentCycle = save.cycleNumber + 1;
            Finished = currentCycle >= cycle;
            BuffPlugin.LogDebug($"Cycle Condition  {save.cycleNumber+1}:{cycle}");

        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty,
            List<Condition> sameConditions = null)
        {
            cycle = (int)Random.Range(Mathf.Lerp(5, 15, difficulty), Mathf.Lerp(10, 30, difficulty));
            BuffPlugin.LogDebug($"Add Cycle Condition {cycle}");
            return ConditionState.Ok_NoMore;
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"({currentCycle}/{cycle})";
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(BuffResourceString.Get("DisplayName_Cycle"), cycle);
        }


        [JsonProperty]
        private int cycle;

        [JsonProperty] 
        private int currentCycle;
    }
}
