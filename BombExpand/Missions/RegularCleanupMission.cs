using BuiltinBuffs.Duality;
using BuiltinBuffs.Negative;
using BuiltinBuffs.Positive;
using RandomBuff;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Game.Settings.Missions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreSlugcats;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuffUtils.SimplyOracle;
using RWCustom;
using UnityEngine;
using RandomBuff.Core.Buff;

namespace BuiltinBuffs.Missions
{
    internal class RegularCleanupMission : Mission, IMissionEntry
    {
        public static MissionID regularCleanupMissionID = new MissionID("RegularCleanup", true);
        public override MissionID ID => regularCleanupMissionID;

        public override SlugcatStats.Name BindSlug => null;

        public override Color TextCol => SpearBurster.scanlineCol;

        public override string MissionName => BuffResourceString.Get("Mission_Display_RegularCleanup");

        public RegularCleanupMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new HuntCondition() { type = CreatureTemplate.Type.DaddyLongLegs, killCount = 20 },
                    new WithInCycleCondition() { SetCycle = 5 },
                    new PermanentHoldCondition() { HoldBuff = new BuffID("NoPassDay") },
                },
                gachaTemplate = new RegularCleanUpTemplate()
                {
                    ForceStartPos = "SS_AI",
                    boostCreatureInfos = new List<GachaTemplate.BoostCreatureInfo>()
                    {
                        new GachaTemplate.BoostCreatureInfo()
                        {
                            baseCrit = MoreSlugcatsEnums.CreatureTemplateType.Inspector,
                            boostCrit = CreatureTemplate.Type.DaddyLongLegs,
                            boostCount = 1,
                            boostType = GachaTemplate.BoostCreatureInfo.BoostType.Replace
                        },
                        new GachaTemplate.BoostCreatureInfo()
                        {
                            baseCrit = CreatureTemplate.Type.DaddyLongLegs,
                            boostCrit = CreatureTemplate.Type.DaddyLongLegs,
                            boostCount = 1,
                            boostType = GachaTemplate.BoostCreatureInfo.BoostType.Add,
                            roomName = new []{"SS_I03","SS_A01"}
                        }
              
                    },
                    PocketPackMultiply = 0,
                }
            };
            startBuffSet.Add(BombManiaBuffEntry.bombManiaBuffID);
            startBuffSet.Add(SpearBursterBuffEntry.spearBursterBuff);
            startBuffSet.Add(new BuffID("TurboPropulsion"));
        }

        public void RegisterMission()
        {
            BuffRegister.RegisterGachaTemplate<RegularCleanUpTemplate>(RegularCleanUpTemplate.RegularCleanUp);
            BuffRegister.RegisterMission(regularCleanupMissionID, new RegularCleanupMission());
            InGameTranslatorExtend.AddLoadFolder("buffassets/text/RegularCleanup");
        }

    }

    internal class RegularCleanUpTemplate : NormalGachaTemplate
    {

        public static readonly GachaTemplateID RegularCleanUp = new GachaTemplateID(nameof(RegularCleanUp), true);

        public override GachaTemplateID ID => RegularCleanUp;


        public override void EnterGame(RainWorldGame game)
        {
            foreach (var room in game.world.abstractRooms)
            {
                if (room.roomAttractions[CreatureTemplate.Type.DaddyLongLegs.index] == AbstractRoom.CreatureRoomAttraction.Avoid ||
                    room.roomAttractions[CreatureTemplate.Type.DaddyLongLegs.index] == AbstractRoom.CreatureRoomAttraction.Forbidden)
                    room.roomAttractions[CreatureTemplate.Type.DaddyLongLegs.index] = AbstractRoom.CreatureRoomAttraction.Neutral;
            }

            base.EnterGame(game);
            BuffEvent.OnSeePlayer += OracleHooks_OnSeePlayer;
            BuffEvent.OnLoadConversation += OracleHooks_OnLoadConversation;
        }

        private void OracleHooks_OnLoadConversation(OracleBehavior behavior, Conversation conversation)
        {
            if (behavior.oracle.ID == Oracle.OracleID.SS)
            {
                if (behavior.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 0)
                {
                    conversation.events.Add(new CustomSpecialEvent(conversation, 0, "gravity", 1));
                    conversation.events.Add(new CustomSpecialEvent(conversation, 0, "locked"));
                    conversation.events.Add(new CustomSpecialEvent(conversation, 0, "turnOff", false));
                    conversation.events.Add(new Conversation.WaitEvent(conversation, 100));
                    conversation.events.Add(new CustomSpecialEvent(conversation, 0, "work", 0));
                    conversation.events.Add(new CustomSpecialEvent(conversation, 0, "behavior", SSOracleBehavior.MovementBehavior.Talk));
                    conversation.events.Add(new Conversation.WaitEvent(conversation, 70));

                    var igt = Custom.rainWorld.inGameTranslator;
                    conversation.events.Add(new Conversation.TextEvent(conversation, 59,
                        igt.Translate("Well you creature that doesn't know what to call, it's time for a regular cleanup."), 50));

                    conversation.events.Add(new Conversation.TextEvent(conversation, 0,
                        igt.Translate("I've already provided you with cleaning tools everywhere."), 20));

                    conversation.events.Add(new Conversation.TextEvent(conversation, 0,
                        igt.Translate("You have to finish the task within the time limit. Now stop interrupting my jobs and go finish yours."), 50));

                    conversation.events.Add(new CustomSpecialEvent(conversation, 0, "unlocked"));
                    conversation.events.Add(new CustomSpecialEvent(conversation, 0, "work", 1));
                    conversation.events.Add(new CustomSpecialEvent(conversation, 0, "behavior", SSOracleBehavior.MovementBehavior.Idle));
                    conversation.events.Add(new CustomSpecialEvent(conversation, 0, "gravity", 0));

                }
                behavior.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad++;
            }
        }

        private bool OracleHooks_OnSeePlayer(OracleBehavior behavior)
        {
            return behavior.oracle.ID == Oracle.OracleID.SS;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            BuffEvent.OnSeePlayer -= OracleHooks_OnSeePlayer;
            BuffEvent.OnLoadConversation -= OracleHooks_OnLoadConversation;

        }

    }
}
