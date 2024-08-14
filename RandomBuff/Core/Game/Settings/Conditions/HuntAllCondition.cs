using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    internal class HuntAllCondition : Condition
    {
        public override ConditionID ID => ConditionID.HuntAll;
        public override int Exp => Mathf.RoundToInt(Custom.LerpMap(huntCount, 30, 100, 70, 300, 1.3f));
        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            huntCount = Random.Range(30, 100);
            return ConditionState.Ok_NoMore;
        }

        public override void HookOn()
        {
            base.HookOn();
            BuffEvent.OnCreatureKilled += BuffEvent_OnCreatureKilled;

        }
        private void BuffEvent_OnCreatureKilled(Creature creature, int playerNumber)
        {
            if (!creature.Template.smallCreature && creature.Template.type != CreatureTemplate.Type.Spider)
            {
                currentCount++;
                if (currentCount >= huntCount && !Finished)
                    Finished = true;
                else
                    onLabelRefresh?.Invoke(this);
            }
        }

        [JsonProperty] 
        public int huntCount;

        [JsonProperty]
        private int currentCount;

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"({currentCount}/{huntCount})";
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(BuffResourceString.Get("DisplayName_HuntAll"), huntCount);

        }
    }
}
