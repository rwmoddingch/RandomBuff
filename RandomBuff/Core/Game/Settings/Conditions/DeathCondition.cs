using System.Collections.Generic;
using System.Drawing.Drawing2D;
using Newtonsoft.Json;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    public class DeathCondition : Condition
    {
        public override ConditionID ID => ConditionID.Death;
        public override int Exp => (int)Custom.LerpMap(deathCount,5,20,250,100);
        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            if (conditions.Count < 2)
                return ConditionState.Fail_Tmp;

            deathCount = (int)Random.Range(5,20);
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

        public override void OnDestroy()
        {
            base.OnDestroy();
            if(Custom.rainWorld.progression.currentSaveState is not null)
                currentDeathCount = Custom.rainWorld.progression.currentSaveState.deathPersistentSaveData.deaths + Custom.rainWorld.progression.currentSaveState.deathPersistentSaveData.quits;

        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            if(BuffCustom.TryGetGame(out _))
                return $"({currentDeathCount}/{deathCount})";
            return "";
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
