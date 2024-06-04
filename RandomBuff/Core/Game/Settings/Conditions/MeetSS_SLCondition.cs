using MoreSlugcats;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    internal class MeetSS_SLCondition : Condition
    {
        public override ConditionID ID => ConditionID.MeetSS_SL;

        public override int Exp => 150;

        [JsonProperty]
        public int cycleRequirement;

        [JsonProperty]
        public bool meetSS;

        [JsonProperty]
        public bool meetSL;

        public MeetSS_SLCondition()
        {
            On.Room.ReadyForAI += Room_ReadyForAI;
        }

        ~MeetSS_SLCondition()
        {
            On.Room.ReadyForAI -= Room_ReadyForAI;
        }

        private void Room_ReadyForAI(On.Room.orig_ReadyForAI orig, Room self)
        {
            orig.Invoke(self);
            var oracles = self.updateList.Where((u) => u is Oracle).Select((u) => u as Oracle).ToList();
            if (self.game.IsArenaSession) return;
            if (self.game.rainWorld.progression.currentSaveState.cycleNumber > cycleRequirement)
                return;
            foreach(var o in oracles)
            {
                if (o.ID == Oracle.OracleID.SS && !meetSS)
                {
                    meetSS = true;
                    onLabelRefresh?.Invoke(this);
                }
                if(o.ID == Oracle.OracleID.SL && !meetSL)
                {
                    meetSL = true;
                    onLabelRefresh?.Invoke(this);
                }
            }

            if (meetSS && meetSL)
                Finished = true;
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(BuffResourceString.Get("DisplayName_MeetSS_SL"), cycleRequirement);
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"({(meetSL?"SL":"")} {(meetSS?"SS":"")})";
        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> sameConditions)
        {
            sameConditions ??= new List<Condition>();
            if (ModManager.MSC)
            {
                var timeline = SlugcatStats.SlugcatTimelineOrder().ToList();
                int indexOfArtificer = timeline.IndexOf(MoreSlugcatsEnums.SlugcatStatsName.Artificer);
                int indexOfSofanthiel = timeline.IndexOf(MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel);
                int indexOfThis = timeline.IndexOf(name);

                //BuffPlugin.Log($"MeetSS_SL {indexOfThis}-{indexOfArtificer}-{indexOfSofanthiel}");

                if (indexOfThis >= indexOfArtificer && indexOfThis <= indexOfSofanthiel)
                    return ConditionState.Fail;

                if (sameConditions.Find((i) => i is MeetSS_SLCondition) != null)
                    return ConditionState.Fail;

                cycleRequirement = UnityEngine.Random.Range(15, 25);
                return ConditionState.Ok_NoMore;
            }
            else
            {
                if (sameConditions.Find((i) => i is MeetSS_SLCondition) != null)
                    return ConditionState.Fail;

                cycleRequirement = UnityEngine.Random.Range(15, 25);
                return ConditionState.Ok_NoMore;
            }
        }
    }
}
