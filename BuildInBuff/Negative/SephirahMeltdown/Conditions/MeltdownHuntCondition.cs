using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using Newtonsoft.Json;
using RandomBuff;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuffUtils;

namespace BuiltinBuffs.Negative.SephirahMeltdown.Conditions
{
    internal class MeltdownHuntCondition : IntervalCondition
    {
        public static readonly ConditionID MeltdownHunt = new ConditionID(nameof(MeltdownHunt), true);

        [JsonProperty]
        public CreatureTemplate.Type type;

        [JsonProperty] 
        public int killCount = 1;


        [JsonProperty] 
        private int currentKill = 0;

        public MeltdownHuntCondition()
        {
            BuffEvent.OnCreatureKilled += BuffEvent_OnCreatureKilled;
        }

        private void BuffEvent_OnCreatureKilled(Creature creature, int playerNumber)
        {
            if (creature.Template.type == type || creature.Template.TopAncestor().type == type)
            {
                currentKill++;
                onLabelRefresh?.Invoke(this);
            }
        }


        public override ConditionID ID => MeltdownHunt;
        public override int Exp => 400;
        public override string InRangeDisplayName()
        {
            if (currentKill == killCount)
                Finished = true;
            if(ChallengeTools.creatureNames == null)
                ChallengeTools.CreatureName(ref ChallengeTools.creatureNames);
            return string.Format(BuffResourceString.Get("DisplayName_MeltDownHunt"), ChallengeTools.creatureNames[type.index]);
        }

        public override string InRangeDisplayProgress()
        {
            return $"({currentKill}/{killCount})";
        }
    }
}
