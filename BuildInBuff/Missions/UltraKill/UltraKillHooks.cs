using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuiltinBuffs.Missions.UltraKill
{
    internal static class UltraKillHooks
    {
        public static void HooksOn()
        {
            On.RelationshipTracker.SortCreatureIntoModule += RelationshipTracker_SortCreatureIntoModule;
            On.ArtificialIntelligence.StaticRelationship += ArtificialIntelligence_StaticRelationship;
            On.ArtificialIntelligence.Update += ArtificialIntelligence_Update;
            On.ScavengerAI.LikeOfPlayer += ScavengerAI_LikeOfPlayer;
            On.AgressionTracker.Utility += AgressionTracker_Utility;
        }

        private static float AgressionTracker_Utility(On.AgressionTracker.orig_Utility orig, AgressionTracker self)
        {
            return 1f;
        }

        private static float ScavengerAI_LikeOfPlayer(On.ScavengerAI.orig_LikeOfPlayer orig, ScavengerAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            float result = orig.Invoke(self, dRelation);

            if (dRelation.trackerRep != null && dRelation.trackerRep.representedCreature.realizedCreature != null && dRelation.trackerRep.representedCreature.realizedCreature is Player)
            {
                result = 0f;
            }
            return result;
        }

        private static void ArtificialIntelligence_Update(On.ArtificialIntelligence.orig_Update orig, ArtificialIntelligence self)
        {
            orig.Invoke(self);
            int j = 0;
            while (j < self.creature.world.game.Players.Count)
            {
                if (self.creature.world.game.Players[j].realizedCreature != null && !(self.creature.world.game.Players[j].realizedCreature as Player).dead)
                {
                    if (self.creature.Room != self.creature.world.game.Players[j].Room)
                    {
                        self.tracker.SeeCreature(self.creature.world.game.Players[j]);
                        return;
                    }
                    break;
                }
                else
                {
                    j++;
                }
            }
        }

        private static CreatureTemplate.Relationship ArtificialIntelligence_StaticRelationship(On.ArtificialIntelligence.orig_StaticRelationship orig, ArtificialIntelligence self, AbstractCreature otherCreature)
        {
            var res = orig.Invoke(self, otherCreature);
            if (otherCreature.creatureTemplate.type != CreatureTemplate.Type.Slugcat)
            {
                res.type = CreatureTemplate.Relationship.Type.Ignores;
            }
            return res;
        }

        private static void RelationshipTracker_SortCreatureIntoModule(On.RelationshipTracker.orig_SortCreatureIntoModule orig, RelationshipTracker self, RelationshipTracker.DynamicRelationship relCrit, CreatureTemplate.Relationship newRelationship)
        {
            if(self.AI is LizardAI && relCrit.trackerRep.representedCreature != null && relCrit.trackerRep.representedCreature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.LizardTemplate)
            {
                newRelationship.type = CreatureTemplate.Relationship.Type.Ignores;
            }
            orig.Invoke(self, relCrit, newRelationship);
        }


        public static void HookOff()
        {
            On.RelationshipTracker.SortCreatureIntoModule -= RelationshipTracker_SortCreatureIntoModule;
            On.ArtificialIntelligence.StaticRelationship -= ArtificialIntelligence_StaticRelationship;
            On.ArtificialIntelligence.Update -= ArtificialIntelligence_Update;
            On.ScavengerAI.LikeOfPlayer -= ScavengerAI_LikeOfPlayer;
        }
    }
}
