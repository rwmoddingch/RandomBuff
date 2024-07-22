using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    internal class DeathCondition : Condition
    {
        public override ConditionID ID => ConditionID.Death;
        public override int Exp => 300;
        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> sameConditions)
        {
            deathCount = (int)Random.Range(Mathf.Lerp(5, 15, 1 - difficulty), Mathf.Lerp(10, 30, 1 - difficulty));
            return ConditionState.Ok_NoMore;
        }

        public DeathCondition()
        {
            Finished = true;
        }

        public override void InGameUpdate(RainWorldGame game)
        {
            base.InGameUpdate(game);
            if (game.GetStorySession.saveState.deathPersistentSaveData.deaths + game.GetStorySession.saveState.deathPersistentSaveData.quits !=
               currentDeathCount)
            {
                currentDeathCount = game.GetStorySession.saveState.deathPersistentSaveData.deaths + game.GetStorySession.saveState.deathPersistentSaveData.quits;
                Finished = currentDeathCount <= deathCount;
                onLabelRefresh?.Invoke(this);
            }
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"({currentDeathCount}/{deathCount})";
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(BuffResourceString.Get("DisplayName_Death"), deathCount);
        }


        [JsonProperty]
        public int deathCount;
        [JsonProperty]
        private int currentDeathCount;

    }
}
