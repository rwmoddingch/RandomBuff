using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuffUtils;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    internal class ScoreCondition : Condition
    {
        public override ConditionID ID => ConditionID.Score;
        public override int Exp => (int)(targetScore / 10f); //TODO

        [JsonProperty]
        public float targetScore;

        [JsonProperty]
        private float score;

        private SlugcatStats.Name name;


        public override void EnterGame(RainWorldGame game)
        {
            base.EnterGame(game);
            if (ChallengeTools.creatureSpawns == null || !ChallengeTools.creatureSpawns.ContainsKey(game.StoryCharacter.value))
            {
                if (ChallengeTools.creatureScores == null)
                    ChallengeTools.GenerateCreatureScores(ref ChallengeTools.creatureScores);
                if (!ExpeditionGame.unlockedExpeditionSlugcats.Contains(game.StoryCharacter))
                    ExpeditionGame.unlockedExpeditionSlugcats.Add(game.StoryCharacter);
                ChallengeTools.ParseCreatureSpawns();
            }

            name = game.StoryCharacter;
            BuffEvent.OnCreatureKilled += BuffEvent_OnCreatureKilled;
        }

        public override void SessionEnd(SaveState save)
        {
            BuffEvent.OnCreatureKilled -= BuffEvent_OnCreatureKilled;
            base.SessionEnd(save);
        }

        private void BuffEvent_OnCreatureKilled(Creature creature, int playerNumber)
        {
            if (ChallengeTools.creatureSpawns[name.value].FirstOrDefault(i => i.creature == creature.Template.type) is { } crit)
            {
                score += crit.points;
                onLabelRefresh?.Invoke(this);
            }
        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> sameConditions)
        {
            targetScore = Mathf.RoundToInt(Mathf.Lerp(200f, 500f, difficulty) / 10f) * 10;
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
            return string.Format(BuffResourceString.Get("DisplayName_Score"),targetScore);
        }
    }
}
