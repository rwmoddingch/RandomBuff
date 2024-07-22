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
    internal class HuntCondition : Condition
    {
        public override ConditionID ID => ConditionID.Hunt;



        public override int Exp => 20 * killCount;//TODO

        [JsonProperty] 
        public CreatureTemplate.Type type;

        [JsonProperty]
        private int currentKillCount;

        [JsonProperty]
        public int killCount;

        public override void EnterGame(RainWorldGame game)
        {
            base.EnterGame(game);
            BuffEvent.OnCreatureKilled += BuffEvent_OnCreatureKilled;
        }


        private void BuffEvent_OnCreatureKilled(Creature creature, int playerNumber)
        {
            if (creature.Template.type == type || creature.Template.TopAncestor().type == type)
            {
                currentKillCount++;
                if (currentKillCount == killCount)
                    Finished = true;
                onLabelRefresh?.Invoke(this);
            }
        }


        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty,
            List<Condition> conditions = null)
        {

            ChallengeTools.ExpeditionCreature expeditionCreature = GetExpeditionCreature(name,
                ExpeditionData.challengeDifficulty,
                conditions != null
                    ? conditions.OfType<HuntCondition>().Select(i => i.type).ToArray()
                    : Array.Empty<CreatureTemplate.Type>());

            if (expeditionCreature == null)
            {
                return ConditionState.Fail;
            }

            killCount = (int)Mathf.Lerp(3f, 15f,Mathf.Pow(difficulty, 2.5f));
            if (expeditionCreature.points < 7)
                killCount += UnityEngine.Random.Range(3, 6);
            
            if (killCount > expeditionCreature.spawns)
                killCount = expeditionCreature.spawns;
            
            if (killCount > 25) killCount = 25;

            type = expeditionCreature.creature;

            return ConditionState.Ok_More;
        }

        public static ChallengeTools.ExpeditionCreature GetExpeditionCreature(SlugcatStats.Name slugcat, float difficulty, CreatureTemplate.Type[] types)
        {
            if (ChallengeTools.creatureSpawns == null || !ChallengeTools.creatureSpawns.ContainsKey(slugcat.value))
            {
                if (ChallengeTools.creatureScores == null)
                    ChallengeTools.GenerateCreatureScores(ref ChallengeTools.creatureScores);
                if(!ExpeditionGame.unlockedExpeditionSlugcats.Contains(slugcat))
                    ExpeditionGame.unlockedExpeditionSlugcats.Add(slugcat);
                ChallengeTools.ParseCreatureSpawns();
            }

            int num = (int)(25.0 * Math.Pow(difficulty, 2.22));
            List<ChallengeTools.ExpeditionCreature> list = new List<ChallengeTools.ExpeditionCreature>();
            foreach (ChallengeTools.ExpeditionCreature item in ChallengeTools.creatureSpawns[slugcat.value])
            {
                if (Math.Abs(num - item.points) <= Mathf.Lerp(5f, 17f, (float)Math.Pow(difficulty, 2.7)) && !types.Contains(item.creature))
                    list.Add(item);
                
            }
            if(list.Any())
                return list[UnityEngine.Random.Range(0, list.Count - 1)];
            return null;
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"({currentKillCount}/{killCount})";

        }

        public override void SessionEnd(SaveState save)
        {
            base.SessionEnd(save);
            BuffEvent.OnCreatureKilled -= BuffEvent_OnCreatureKilled;
        }

        public override string DisplayName(InGameTranslator translator)
        {
            if (ChallengeTools.creatureNames == null)
                ChallengeTools.CreatureName(ref ChallengeTools.creatureNames);
            return string.Format(BuffResourceString.Get("DisplayName_MeltDownHunt"),killCount, ChallengeTools.creatureNames[type.index]);
        }

    }
}
