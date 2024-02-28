using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RandomBuffUtils;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    internal class HuntCondition : Condition
    {
        public override ConditionID ID => ConditionID.Hunt;

        public override float Exp => 0;

        [JsonProperty]
        public CreatureTemplate.Type type = CreatureTemplate.Type.GreenLizard;

        public HuntCondition()
        {
            BuffEvent.OnCreatureKilled += BuffEvent_OnCreatureKilled;
        }

        private void BuffEvent_OnCreatureKilled(Creature creature, int playerNumber)
        {
            
        }

        public override void SessionEnd(SaveState save)
        {
            
        }

        public override void SetRandomParameter(float difficulty)
        {
            
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            throw new NotImplementedException();

        }

        public override string DisplayName(InGameTranslator translator)
        {
            throw new NotImplementedException();
        }
    }
}
