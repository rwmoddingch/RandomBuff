using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using Newtonsoft.Json;
using RandomBuffUtils;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    internal class CycleScoreCondition : Condition
    {
        public override ConditionID ID => ConditionID.CycleScore;
        public override int Exp => (int)(targetScore / 3f); //TODO

        [JsonProperty]
        public float targetScore;

        private float score;

        public CycleScoreCondition()
        {
            BuffEvent.OnCreatureKilled += BuffEvent_OnCreatureKilled;
        }

        private void BuffEvent_OnCreatureKilled(Creature creature, int playerNumber)
        {
            ChallengeTools.ParseCreatureSpawns();
            if (ChallengeTools.creatureSpawns[ExpeditionData.slugcatPlayer.value]
                    .FirstOrDefault(i => i.creature == creature.Template.type)
                is { } spawnData)
            {
                score += spawnData.points;
                onLabelRefresh?.Invoke(this);
            }
        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> sameConditions)
        {
            targetScore = Mathf.RoundToInt(Mathf.Lerp(20f, 125f, difficulty) / 10f) * 10;
            return ConditionState.Ok_NoMore;
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"({(Finished ? targetScore : score)}/{targetScore})";
        }

        public override string DisplayName(InGameTranslator translator)
        {
            if (score > targetScore)
                Finished = true;
            return string.Format(BuffResourceString.Get("DisplayName_CycleScore"),targetScore);
        }
    }
}
