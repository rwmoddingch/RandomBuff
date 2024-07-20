using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using Newtonsoft.Json;
using RandomBuffUtils;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    internal class HuntCondition : Condition
    {
        public override ConditionID ID => ConditionID.Hunt;



        public override int Exp => 0;//TODO

        [JsonProperty]
        public CreatureTemplate.Type type = CreatureTemplate.Type.GreenLizard;

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
            List<Condition> sameConditions = null)
        {
            return ConditionState.Fail;//TODO
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
            return string.Format(BuffResourceString.Get("DisplayName_MeltDownHunt"), ChallengeTools.creatureNames[type.index]);
        }

    }
}
