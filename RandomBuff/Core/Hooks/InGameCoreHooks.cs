using RandomBuff.Core.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;
using RWCustom;
using UnityEngine;
using RandomBuff.Core.StaticsScreen;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Menu;
using RandomBuff.Core.Game.Settings;
using RandomBuffUtils;

namespace RandomBuff.Core.Hooks
{
    static partial class CoreHooks
    {

        public static void GameSettingSpecialSetup(SaveState saveState, GameSetting gameSetting)
        {
            if (gameSetting.MissionId == "DoomExpress")
            {
                saveState.miscWorldSaveData.moonHeartRestored = false;
                saveState.miscWorldSaveData.pebblesEnergyTaken = false;
            }
            else if (gameSetting.MissionId == "EmergnshyTreatment")
            {
                saveState.miscWorldSaveData.moonRevived = false;
                saveState.miscWorldSaveData.SLOracleState.playerEncounters = 0;
                saveState.miscWorldSaveData.SLOracleState.neuronsLeft = 0;
            }
            else if (gameSetting.MissionId == "TripleAffirmation")
            {
                saveState.deathPersistentSaveData.karma = 9;
                saveState.deathPersistentSaveData.karmaCap = 9;
            }
        }

        public static bool IsCurrentFullRoomSettingNeed(GameSetting gameSetting)
        {
            return gameSetting.MissionId is "DoomExpress" or "EmergnshyTreatment";
        }


        public static void ClampKarmaForBuffMode(ref int karma,ref int karmaCap)
        {
            if (BuffPoolManager.Instance.GameSetting.MissionId is not "TripleAffirmation")
            {
                karmaCap = Mathf.Min(karmaCap, 4);
                karma = Custom.IntClamp(karma, 0, 4);
            }
        }


        internal static void InGameHooksInit()
        {
            On.RainWorldGame.Update += RainWorldGame_Update;
            On.RainWorldGame.Win += RainWorldGame_Win;
            On.RainWorldGame.GhostShutDown += RainWorldGame_GhostShutDown;
            On.SaveState.setDenPosition += SaveState_setDenPosition;
            On.SaveState.ctor += SaveState_setup;
            On.GhostWorldPresence.SpawnGhost += GhostWorldPresence_SpawnGhost;

            On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;

            On.Player.ctor += Player_ctor;
            On.Room.Loaded += Room_Loaded;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature,world);
            if (Custom.rainWorld.BuffMode())
                self.redsIllness = null;
        }

        private static bool lastBuffShowCursor = false;


        private static void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig(self, dt);
            if ((BuffHud.Instance?.NeedShowCursor ?? false))
                Cursor.visible = true;
            else if(lastBuffShowCursor != (BuffHud.Instance?.NeedShowCursor ?? false))
                Cursor.visible = self.devUI != null || !self.rainWorld.options.fullScreen;
            lastBuffShowCursor = (BuffHud.Instance?.NeedShowCursor ?? false);
        }




        private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            if (Custom.rainWorld.BuffMode())
            {
                _ = BuffCustom.TryGetGame(out var game);
                self.roomSettings.placedObjects.RemoveAll(i => i.type.value.Contains("Token"));
                if (!IsCurrentFullRoomSettingNeed(BuffPoolManager.Instance?.GameSetting ??
                                             BuffDataManager.Instance.GetGameSetting(game?.StoryCharacter ??
                                                 Custom.rainWorld.progression.PlayingAsSlugcat)) &&
                    RemoveRoomSettingList.Contains(self.abstractRoom.name))
                {
                    self.roomSettings.roomSpecificScript = false;
                }
            }

            orig(self);
            self.updateList.RemoveAll(i => i is MSCRoomSpecificScript.OE_NPCControl);
        }


        private static readonly HashSet<string> RemoveRoomSettingList = new()
        {
            "SL_C12", "SB_A14", "LF_A03", "SU_A43", "GW_A25", "SI_C02","HR_C01","Rock Bottom",
            "SI_A07", "RM_CORE","MS_CORE","OE_FINAL03","LC_FINAL","SL_AI",
            "SH_GOR02","SI_SAINTINTRO","GW_A24", "SB_E05SAINT",
        };
    
        private static bool GhostWorldPresence_SpawnGhost(On.GhostWorldPresence.orig_SpawnGhost orig, GhostWorldPresence.GhostID ghostID, int karma, int karmaCap, int ghostPreviouslyEncountered, bool playingAsRed)
        {
            if (Custom.rainWorld.BuffMode() && Custom.rainWorld.progression.currentSaveState.cycleNumber == 0)
                return false;
            return orig(ghostID,karma, karmaCap, ghostPreviouslyEncountered, playingAsRed);
        }

        private static void SaveState_setup(On.SaveState.orig_ctor orig, SaveState self, SlugcatStats.Name saveStateNumber, PlayerProgression progression)
        {
            orig(self, saveStateNumber, progression);
            if (Custom.rainWorld.BuffMode())
            {
                self.deathPersistentSaveData.PoleMimicEverSeen = true;
                self.deathPersistentSaveData.ScavTollMessage = true;
                self.deathPersistentSaveData.KarmaFlowerMessage = true;
                self.deathPersistentSaveData.GoExploreMessage = true;
                self.deathPersistentSaveData.ScavMerchantMessage = true;
                self.deathPersistentSaveData.SMEatTutorial = true;
                self.deathPersistentSaveData.SMTutorialMessage = true;
                self.deathPersistentSaveData.TongueTutorialMessage = true;
                self.deathPersistentSaveData.ArtificerMaulTutorial = true;
                self.deathPersistentSaveData.ArtificerTutorialMessage = true;
                self.deathPersistentSaveData.DangleFruitInWaterMessage = true;
                self.deathPersistentSaveData.GateStandTutorial = true;
                self.deathPersistentSaveData.GoExploreMessage = true;
                //self.miscWorldSaveData.SLOracleState.neuronGiveConversationCounter = 1;
                if (saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Rivulet || saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Saint)
                {
                    self.miscWorldSaveData.moonHeartRestored = true;
                    self.miscWorldSaveData.pebblesEnergyTaken = true;
                }
                self.deathPersistentSaveData.karma = SlugcatStats.SlugcatStartingKarma(self.saveStateNumber);
                self.deathPersistentSaveData.karmaCap = 4;
                self.deathPersistentSaveData.theMark = true;
                self.miscWorldSaveData.SLOracleState.playerEncountersWithMark = ((saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear) ? 5 : 2);
                self.miscWorldSaveData.SLOracleState.neuronsLeft = 5;
                //self.miscWorldSaveData.SSaiConversationsHad = 1;
                //self.miscWorldSaveData.cyclesSinceSSai = 10;
                //self.miscWorldSaveData.SSaiThrowOuts = -1;
                progression.miscProgressionData.beaten_Gourmand = true;
                GameSettingSpecialSetup(self,BuffDataManager.Instance.GetGameSetting(saveStateNumber));
                self.dreamsState = null;
            }
        }

        private static void SaveState_setDenPosition(On.SaveState.orig_setDenPosition orig, SaveState self)
        {
            orig(self);
            if (self.progression.rainWorld.BuffMode())
            {

                if (BuffDataManager.Instance.GetGameSetting(self.saveStateNumber).gachaTemplate.NeedRandomStart)
                {
                    string name =
                        ExpeditionGame.ExpeditionRandomStarts(self.progression.rainWorld, self.saveStateNumber);
                    self.denPosition = self.lastVanillaDen = name;
                }
                else if (BuffDataManager.Instance.GetGameSetting(self.saveStateNumber).gachaTemplate.ForceStartPos is {} pos)
                {
                    BuffPlugin.LogDebug($"Force start pos:{pos}");
                    self.denPosition = self.lastVanillaDen = pos;
                }
            }
        }



        private static void RainWorldGame_GhostShutDown(On.RainWorldGame.orig_GhostShutDown orig, RainWorldGame self, GhostWorldPresence.GhostID ghostID)
        {
            //BuffPoolManager.Instance?.WinGame();
            orig(self, ghostID);
        }

        private static void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
        {
            BuffPoolManager.Instance?.WinGame();
            orig(self, malnourished);
        }


        private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            BuffPoolManager.Instance?.Update(self);

        }
    }
}
