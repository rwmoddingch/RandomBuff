using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    internal class LikeCondition : Condition
    {
        public override ConditionID ID => ConditionID.Like;

        public override int Exp => communityID == CreatureCommunities.CommunityID.Cicadas ? 100 : 150;

        List<CreatureCommunities.CommunityID> exclusiveIDs = new();

        [JsonProperty]
        public CreatureCommunities.CommunityID communityID;

        int counter;

        [JsonProperty]
        float lastLike;

        public LikeCondition()
        {
            exclusiveIDs.Add(CreatureCommunities.CommunityID.None);
            exclusiveIDs.Add(CreatureCommunities.CommunityID.All);
            exclusiveIDs.Add(CreatureCommunities.CommunityID.JetFish);
            exclusiveIDs.Add(CreatureCommunities.CommunityID.Deer);
            exclusiveIDs.Add(CreatureCommunities.CommunityID.GarbageWorms);
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(BuffResourceString.Get("DisplayName_Like"), BuffResourceString.Get(communityID.value, true));
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"({Mathf.FloorToInt(lastLike * 100f)}%)";
        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            conditions ??= new List<Condition>();
            var choices = CreatureCommunities.CommunityID.values.entries.Select((i) => new CreatureCommunities.CommunityID(i)).Where((i) =>!exclusiveIDs.Contains(i)).Where((i) => conditions.OfType<LikeCondition>().All(j => j.communityID != i)).ToList();

            if (choices.Count == 0)
                return ConditionState.Fail;

            communityID = choices[Random.Range(0, choices.Count)];
            choices.Remove(communityID);

            if (choices.Count > 0)
                return ConditionState.Ok_More;
            else
                return ConditionState.Ok_NoMore;
        }

        public override void InGameUpdate(RainWorldGame game)
        {
            if(counter > 0)
                counter--;
            else
            {
                float like = game.session.creatureCommunities.LikeOfPlayer(communityID, game.world.region.regionNumber, 0);
                if(lastLike != like)
                {
                    lastLike = like;
                    onLabelRefresh?.Invoke(this);
                }
                if (Mathf.Approximately(like, 1f))
                    Finished = true;
                else
                    Finished = false;
                counter = 40;
            }
        }
    }
}
