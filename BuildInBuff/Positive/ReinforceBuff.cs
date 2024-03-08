using MoreSlugcats;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RWCustom;
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
        static int maxGrasp = 10;

        public override bool Triggerable => Active;
        public override bool Active => !triggeredThisCycle && reachChieftain && roomMeetRequirements;

        bool triggeredThisCycle;
        bool reachChieftain;
        bool roomMeetRequirements;

        public ReinforceBuff()
        {
           
        }

        void Init()
        {
            StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Scavenger).grasps = maxGrasp;
            On.ScavengerAbstractAI.InitGearUp += ScavengerAbstractAI_InitGearUp;
        }

        private void ScavengerAbstractAI_InitGearUp(On.ScavengerAbstractAI.orig_InitGearUp orig, ScavengerAbstractAI self)
        {
            int num = 3;
            int num2 = 40;
            float num3 = 1f;
            if (self.world.game.IsStorySession)
            {
                num2 = (self.world.game.session as StoryGameSession).saveState.cycleNumber;
                if (self.world.game.StoryCharacter == SlugcatStats.Name.Yellow)
                {
                    num2 = Mathf.FloorToInt((float)num2 * 0.75f);
                    num3 = 0.5f;
                }
                else if (self.world.game.StoryCharacter == SlugcatStats.Name.Red)
                {
                    num2 += 60;
                    num3 = 1.5f;
                }
            }
            if (ModManager.MSC && self.world.game.IsArenaSession && self.world.game.GetArenaGameSession.chMeta != null && self.world.game.GetArenaGameSession.chMeta.seed >= 0)
            {
                UnityEngine.Random.InitState(self.parent.ID.RandomSeed);
            }
            bool flag = false;
            if (!ModManager.MSC || (self.world.game.IsStorySession && ((self.world.game.session as StoryGameSession).saveStateNumber == SlugcatStats.Name.Yellow || (self.world.game.session as StoryGameSession).saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Rivulet || (self.world.game.session as StoryGameSession).saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Saint)))
            {
                flag = true;
            }
            int itemCount = Custom.IntClamp((int)(Mathf.Pow(UnityEngine.Random.value, Mathf.Lerp(1.5f, 0.5f, Mathf.Pow(self.parent.personality.dominance, 3f - num3))) * (3.5f + num3)), 0, 4);
            itemCount = Mathf.Clamp((int)(itemCount * maxGrasp / 4f), 0, 10);
            if (ModManager.MSC && self.parent.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite && itemCount < 1)
            {
                itemCount = 1;
            }
            if (itemCount > 0)
            {
                for (int i = 0; i < itemCount; i++)
                {
                    AbstractPhysicalObject abstractPhysicalObject;
                    if (ModManager.MSC && self.parent.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite)
                    {
                        if (UnityEngine.Random.value < 0.5f || flag)
                        {
                            abstractPhysicalObject = new AbstractSpear(self.world, null, self.parent.pos, self.world.game.GetNewID(), true);
                        }
                        else
                        {
                            abstractPhysicalObject = new AbstractSpear(self.world, null, self.parent.pos, self.world.game.GetNewID(), false, true);
                        }
                    }
                    else
                    {
                        abstractPhysicalObject = new AbstractSpear(self.world, null, self.parent.pos, self.world.game.GetNewID(), self.IsSpearExplosive(num2));
                    }
                    self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject);
                    new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject, num, true);
                    num--;
                }
            }
            if (num >= 0 && UnityEngine.Random.value < 0.6f && ((!self.world.singleRoomWorld && ((self.world.game.IsStorySession && ModManager.MSC && self.world.game.GetStorySession.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Saint) || self.world.region.name == "SH" || (self.world.region.name == "SB" && UnityEngine.Random.value < 0.7f))) || (ModManager.MSC && self.parent.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite)))
            {
                AbstractPhysicalObject abstractPhysicalObject2 = new AbstractPhysicalObject(self.world, AbstractPhysicalObject.AbstractObjectType.Lantern, null, self.parent.pos, self.world.game.GetNewID());
                self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject2);
                new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject2, num, true);
                num--;
            }
            if (ModManager.MSC)
            {
                if (num >= 0 && !self.world.singleRoomWorld && (self.world.region.name == "SB" || self.world.region.name == "SL" || self.world.region.name == "MS") && UnityEngine.Random.value < 0.27f)
                {
                    AbstractConsumable abstractConsumable = new AbstractConsumable(self.world, MoreSlugcatsEnums.AbstractObjectType.GlowWeed, null, self.parent.pos, self.world.game.GetNewID(), -1, -1, null);
                    abstractConsumable.isConsumed = true;
                    self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractConsumable);
                    new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractConsumable, num, true);
                    num--;
                }
                if (num >= 0 && !self.world.singleRoomWorld && (self.world.region.name == "LF" || self.world.region.name == "OE") && UnityEngine.Random.value < 0.27f)
                {
                    AbstractConsumable abstractConsumable2 = new AbstractConsumable(self.world, MoreSlugcatsEnums.AbstractObjectType.GooieDuck, null, self.parent.pos, self.world.game.GetNewID(), -1, -1, null);
                    abstractConsumable2.isConsumed = true;
                    self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractConsumable2);
                    new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractConsumable2, num, true);
                    num--;
                }
            }
            if (num >= 0 && Mathf.Pow(UnityEngine.Random.value, num3) < Mathf.InverseLerp(10f, 60f, (float)num2))
            {
                int num5 = Custom.IntClamp((int)(Mathf.Pow(UnityEngine.Random.value, Mathf.Lerp(1.5f, 0.5f, Mathf.Pow(self.parent.personality.dominance, 2f) * Mathf.InverseLerp(10f, 60f, (float)num2))) * 2.5f), 0, 2);
                for (int j = 0; j < num5; j++)
                {
                    AbstractPhysicalObject abstractPhysicalObject3;
                    if (ModManager.MSC && self.parent.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite && !flag && ((self.world.game.IsArenaSession && self.world.game.GetArenaGameSession.chMeta == null) || (UnityEngine.Random.value <= self.parent.personality.aggression / 5f && (self.parent.personality.dominance > 0.6f || self.parent.nightCreature))))
                    {
                        abstractPhysicalObject3 = new AbstractPhysicalObject(self.world, MoreSlugcatsEnums.AbstractObjectType.SingularityBomb, null, self.parent.pos, self.world.game.GetNewID());
                    }
                    else
                    {
                        abstractPhysicalObject3 = new AbstractPhysicalObject(self.world, AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, null, self.parent.pos, self.world.game.GetNewID());
                    }
                    self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject3);
                    new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject3, num, true);
                    num--;
                    if (num < 0)
                    {
                        break;
                    }
                }
            }
            if (num >= 0 && UnityEngine.Random.value < 0.08f)
            {
                AbstractPhysicalObject abstractPhysicalObject4 = new AbstractConsumable(self.world, AbstractPhysicalObject.AbstractObjectType.FirecrackerPlant, null, self.parent.pos, self.world.game.GetNewID(), -1, -1, null);
                self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject4);
                new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject4, num, true);
                num--;
            }
            if (num >= 0 && Mathf.Pow(UnityEngine.Random.value, num3) < (self.world.game.IsStorySession ? Mathf.InverseLerp(40f, 110f, (float)num2) : 0.8f) && UnityEngine.Random.value < 1f / Mathf.Lerp(12f, 3f, self.parent.personality.dominance))
            {
                SporePlant.AbstractSporePlant abstractSporePlant = new SporePlant.AbstractSporePlant(self.world, null, self.parent.pos, self.world.game.GetNewID(), -1, -1, null, false, true);
                new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractSporePlant, num, true);
                self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractSporePlant);
                num--;
            }
            if (num >= 0)
            {
                int num6 = UnityEngine.Random.Range(0, self.carryRocks + 1);
                for (int k = 0; k < num6; k++)
                {
                    AbstractPhysicalObject abstractPhysicalObject5 = new AbstractPhysicalObject(self.world, AbstractPhysicalObject.AbstractObjectType.Rock, null, self.parent.pos, self.world.game.GetNewID());
                    self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject5);
                    new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject5, num, true);
                    num--;
                    if (num < 0)
                    {
                        break;
                    }
                }
            }
            if (ModManager.MSC && self.parent.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite)
            {
                while (num >= 0)
                {
                    AbstractPhysicalObject abstractPhysicalObject6 = new AbstractSpear(self.world, null, self.parent.pos, self.world.game.GetNewID(), self.IsSpearExplosive(num2));
                    self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject6);
                    new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject6, num, true);
                    num--;
                }
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            if (StaticWorld.creatureTemplates == null)
                return;

            StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Scavenger).grasps = 4;
            On.ScavengerAbstractAI.InitGearUp -= ScavengerAbstractAI_InitGearUp;
        }

        public override bool Trigger(RainWorldGame game)
        {
            Init();
            triggeredThisCycle = true;
            var room = game.world.offScreenDen;

            ScavengerAbstractAI.ScavengerSquad squad = null;
            string idLog = "";
            for(int i = 0;i < 4; i++)
            {
                AbstractCreature scav = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate(/*MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite*/CreatureTemplate.Type.Scavenger), null, new WorldCoordinate(room.index, 0, 0, -1), game.GetNewID());
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
