
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Condition
{
    internal class CycleCondition : BaseCondition
    {
        public override ConditionID ID => ConditionID.Cycle;

        public override void SessionEnd(SaveState save)
        {
            Finished = save.cycleNumber >= cycle;
            BuffPlugin.LogDebug($"Cycle Condition  {save.cycleNumber}:{cycle}");

        }

        public override void SetRandomParameter(float difficulty)
        {
            cycle = (int)Random.Range(Mathf.Lerp(5, 15, difficulty), Mathf.Lerp(10, 30, difficulty));
            BuffPlugin.LogDebug($"Add Cycle Condition {cycle}");
        }

        [JsonProperty]
        private int cycle;
    }
}
