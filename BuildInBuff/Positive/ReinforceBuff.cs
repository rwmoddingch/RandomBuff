using MoreSlugcats;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BuiltinBuffs.Positive
{
    internal class ReinforceBuff : Buff<ReinforceBuff, ReinforceBuffData>
    {
        public override BuffID ID => ReinforceBuffEntry.reinforceBuffID;

        public override bool Triggerable => Active;
        public override bool Active => !triggeredThisCycle && reachChieftain && roomMeetRequirements;

        bool triggeredThisCycle;
        bool reachChieftain;
        bool roomMeetRequirements;

        public ReinforceBuff()
        {
        }

        public override bool Trigger(RainWorldGame game)
        {
            triggeredThisCycle = true;
            var room = game.world.offScreenDen;

            ScavengerAbstractAI.ScavengerSquad squad = null;
            string idLog = "";
            for(int i = 0;i < 4; i++)
            {
                AbstractCreature scav = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite), null, new WorldCoordinate(room.index, 0, 0, -1), game.GetNewID());
                room.AddEntity(scav);
                idLog += $"{scav.ID.number} ";
                if (squad == null)
                {
                    squad = new ScavengerAbstractAI.ScavengerSquad(scav);

                    squad.targetCreature = game.Players[0];
                    squad.missionType = ScavengerAbstractAI.ScavengerSquad.MissionID.ProtectCreature;

                    (scav.abstractAI as ScavengerAbstractAI).squad = squad;
                }
                else
                {
                    squad.AddMember(scav);
                }
                (scav.abstractAI as ScavengerAbstractAI).ReGearInDen();
                scav.Move(game.Players[0].pos);
            }
            BuffUtils.Log("ReinforceBuff", "Assembly Squad : " + idLog + $"leader : {squad.leader.ID}");

            return false;
        }

        public void UpdateReachChieftain(RainWorldGame game)
        {
            float value = game.session.creatureCommunities.LikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0);
            if (game.StoryCharacter == SlugcatStats.Name.Yellow)
            {
                value = Mathf.InverseLerp(0.42f, 0.9f, value);
            }
            else
            {
                value = Mathf.InverseLerp(0.1f, 0.8f, value);
            }
            value = Mathf.Floor(value * 20f) / 20f;
            reachChieftain = value >= 1f;
            var tracker = game.GetStorySession.saveState.deathPersistentSaveData.winState.GetTracker(WinState.EndgameID.Chieftain, false);
            if (tracker != null)
                reachChieftain = reachChieftain || tracker.GoalFullfilled;

            BuffUtils.Log("ReinforceBuff", $"UpdateReachChieftain : {reachChieftain}");
        }

        public void UpdateRoomRequirements(World world)
        {
            roomMeetRequirements = false;
            if (!world.game.Players[0].Room.shelter && !world.game.Players[0].Room.gate)
            {
                AbstractRoom abstractRoom = world.GetAbstractRoom(world.game.Players[0].pos.room);
                if (!(((abstractRoom != null) ? abstractRoom.AttractionForCreature(CreatureTemplate.Type.Scavenger) : null) == AbstractRoom.CreatureRoomAttraction.Forbidden))
                {
                    roomMeetRequirements = true;
                }
            }
            BuffUtils.Log("ReinforceBuff", $"UpdateRoomRequirements : {roomMeetRequirements}");
            UpdateReachChieftain(world.game);
        }
    }

    internal class ReinforceBuffData : BuffData
    {
        public override BuffID ID => ReinforceBuffEntry.reinforceBuffID;
    }

    internal class ReinforceBuffEntry : IBuffEntry
    {
        public static BuffID reinforceBuffID = new BuffID("Reinforce", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ReinforceBuff, ReinforceBuffData, ReinforceBuffEntry>(reinforceBuffID);
        }

        public static void HookOn()
        {
            On.ScavengerAI.ScavPlayerRelationChange += ScavengerAI_ScavPlayerRelationChange;
            On.ScavengerAI.RecognizeCreatureAcceptingGift += ScavengerAI_RecognizeCreatureAcceptingGift;
            On.ScavengerAI.RecognizePlayerOfferingGift += ScavengerAI_RecognizePlayerOfferingGift;
            On.Player.SpitOutOfShortCut += Player_SpitOutOfShortCut;
        }

       
        private static void Player_SpitOutOfShortCut(On.Player.orig_SpitOutOfShortCut orig, Player self, RWCustom.IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            orig.Invoke(self, pos, newRoom, spitOutAllSticks);
            if (self.abstractCreature == newRoom.game.Players[0])
                ReinforceBuff.Instance.UpdateRoomRequirements(newRoom.world);
        }

        private static void ScavengerAI_RecognizeCreatureAcceptingGift(On.ScavengerAI.orig_RecognizeCreatureAcceptingGift orig, ScavengerAI self, Tracker.CreatureRepresentation subRep, Tracker.CreatureRepresentation objRep, bool objIsMe, PhysicalObject item)
        {
            orig.Invoke(self, subRep, objRep, objIsMe, item);
            if (self.scavenger.room != null)
                ReinforceBuff.Instance.UpdateReachChieftain(self.scavenger.room.game);
        }

        private static void ScavengerAI_RecognizePlayerOfferingGift(On.ScavengerAI.orig_RecognizePlayerOfferingGift orig, ScavengerAI self, Tracker.CreatureRepresentation subRep, Tracker.CreatureRepresentation objRep, bool objIsMe, PhysicalObject item)
        {
            orig.Invoke(self, subRep, objRep, objIsMe, item);
            if (self.scavenger.room != null)
                ReinforceBuff.Instance.UpdateReachChieftain(self.scavenger.room.game);
        }

        private static void ScavengerAI_ScavPlayerRelationChange(On.ScavengerAI.orig_ScavPlayerRelationChange orig, ScavengerAI self, float change, AbstractCreature player)
        {
            orig.Invoke(self, change, player);
            if(self.scavenger.room != null)
                ReinforceBuff.Instance.UpdateReachChieftain(self.scavenger.room.game);
        }
    }
}
