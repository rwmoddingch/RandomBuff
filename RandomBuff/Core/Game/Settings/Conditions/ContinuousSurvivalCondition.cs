using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RandomBuffUtils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    internal class ContinuousSurvivalCondition : Condition
    {
        public override ConditionID ID => ConditionID.ContinuousSurvival;
        public override int Exp => Mathf.RoundToInt(100 * Mathf.Pow(cycle / 3f, 2));
        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            cycle = Random.Range(3, 10);
            return ConditionState.Ok_NoMore;
        }

        public override void EnterGame(RainWorldGame game)
        {
            base.EnterGame(game);
            if (lastQuit != game.GetStorySession.saveState.deathPersistentSaveData.quits ||
                lastDie != game.GetStorySession.saveState.deathPersistentSaveData.deaths)
            {
                lastQuit = game.GetStorySession.saveState.deathPersistentSaveData.quits;
                lastDie = game.GetStorySession.saveState.deathPersistentSaveData.deaths;
                currentCycle = 0;
            }
            onLabelRefresh?.Invoke(this);
            if (currentCycle >= cycle)
                Finished = true;
        }

        public override void SessionEnd(SaveState save)
        {
            base.SessionEnd(save);
            currentCycle++;
        }

        [JsonProperty] 
        public int cycle;

        [JsonProperty] 
        private int currentCycle;

        [JsonProperty] 
        private int lastQuit;

        [JsonProperty] 
        private int lastDie;

        public override string DisplayProgress(InGameTranslator translator)
        {
            if (BuffCustom.TryGetGame(out _))
                return $"({(Finished?cycle:currentCycle)}/{cycle}";
            return "";
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(BuffResourceString.Get("DisplayName_ContinuousSurvival"), cycle);
        }
    }
}
