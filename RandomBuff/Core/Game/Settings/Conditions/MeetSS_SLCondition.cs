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
        private bool meetSS;

        [JsonProperty]
        private bool meetSL;


        [JsonProperty] 
        private int currentCycle;

        public override void EnterGame(RainWorldGame game)
        {
            base.EnterGame(game);
            currentCycle = game.GetStorySession.saveState.cycleNumber;
            if (currentCycle <= cycleRequirement)
            {
                On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
                On.SLOracleBehavior.Update += SLOracleBehavior_Update;

            }
        }

        private void SLOracleBehavior_Update(On.SLOracleBehavior.orig_Update orig, SLOracleBehavior self, bool eu)
        {
           orig(self, eu);
           if (self.hasNoticedPlayer && !meetSL)
           {
               meetSL = true;
               onLabelRefresh?.Invoke(this);
           }
        }

        public override void SessionEnd(SaveState save)
        {
            base.SessionEnd(save);

            if (currentCycle <= cycleRequirement)
            {
                On.SSOracleBehavior.SeePlayer -= SSOracleBehavior_SeePlayer;
                On.SLOracleBehavior.Update -= SLOracleBehavior_Update;
            }

            currentCycle = save.cycleNumber + 1;
        }

        private void SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
        {
            orig(self);
            if (self.oracle.ID == Oracle.OracleID.SS)
            {
                meetSS = true;
                onLabelRefresh?.Invoke(this);
            }
        }

    

       

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(BuffResourceString.Get("DisplayName_MeetSS_SL"), cycleRequirement);
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            if (meetSS && meetSL)
                Finished = true;
            return (Finished ? "" : $"({currentCycle}/{cycleRequirement})") + ((meetSS || meetSL) ? $"({(meetSL ? "Moon" : "")}{(meetSL && meetSS ? " " : "")}{(meetSS ? "FP" : "")})" : "");
        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            conditions ??= new List<Condition>();
            if (ModManager.MSC)
            {
                var timeline = SlugcatStats.SlugcatTimelineOrder().ToList();
                int indexOfArtificer = timeline.IndexOf(MoreSlugcatsEnums.SlugcatStatsName.Artificer);
                int indexOfSofanthiel = timeline.IndexOf(MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel);
                int indexOfThis = timeline.IndexOf(name);

                //BuffPlugin.Log($"MeetSS_SL {indexOfThis}-{indexOfArtificer}-{indexOfSofanthiel}");

                if (indexOfThis >= indexOfArtificer && indexOfThis <= indexOfSofanthiel)
                    return ConditionState.Fail;

                if (conditions.Find((i) => i is MeetSS_SLCondition) != null)
                    return ConditionState.Fail;

                cycleRequirement = UnityEngine.Random.Range(15, 25);
                return ConditionState.Ok_NoMore;
            }
            else
            {
                if (conditions.Find((i) => i is MeetSS_SLCondition) != null)
                    return ConditionState.Fail;

                cycleRequirement = UnityEngine.Random.Range(15, 25);
                return ConditionState.Ok_NoMore;
            }
        }
    }
}
