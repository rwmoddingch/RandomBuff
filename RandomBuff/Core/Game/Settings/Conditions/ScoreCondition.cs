﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    public class ScoreCondition : Condition
    {
        public override ConditionID ID => ConditionID.Score;
        public override int Exp => (int)(targetScore / Custom.LerpMap(targetScore,200,100,10,7f)); //TODO

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
        }

        public override void HookOn()
        {
            base.HookOn();
            BuffEvent.OnCreatureKilled += BuffEvent_OnCreatureKilled;
        }


        private void BuffEvent_OnCreatureKilled(Creature creature, int playerNumber)
        {
            if (ChallengeTools.creatureSpawns[name.value].FirstOrDefault(i => i.creature == creature.Template.type) is { } crit)
            {
                score += crit.points;
                onLabelRefresh?.Invoke(this);
            }
        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            targetScore = Mathf.RoundToInt(Random.Range(200, 1000f));
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
