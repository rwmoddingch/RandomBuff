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
        }

        public static bool IsCurrentGameSettingNeed(GameSetting gameSetting)
        {
            return gameSetting.MissionId is "DoomExpress" or "EmergnshyTreatment";
        }
        public static void InGameHooksInit()
        {
            On.RainWorldGame.Update += RainWorldGame_Update;
            On.RainWorldGame.Win += RainWorldGame_Win;
            On.RainWorldGame.GhostShutDown += RainWorldGame_GhostShutDown;
            On.StoryGameSession.ctor += StoryGameSession_ctor;
            On.SaveState.setDenPosition += SaveState_setDenPosition;
            On.SaveState.ctor += SaveState_setup;
            On.GhostWorldPresence.SpawnGhost += GhostWorldPresence_SpawnGhost;

            On.Room.Loaded += Room_Loaded;
        }

        private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            if (Custom.rainWorld.BuffMode())
            {
                self.roomSettings.placedObjects.RemoveAll(i => i.type.value.Contains("Token"));
                self.roomSettings.roomSpecificScript = self.roomSettings.roomSpecificScript && IsCurrentGameSettingNeed(BuffPoolManager.Instance?.GameSetting??
                BuffDataManager.Instance.GetGameSetting(self.world.game.StoryCharacter));
            }

            orig(self);
        }

   

    
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
                self.deathPersistentSaveData.karma = 4;
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
                else if (self.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear)
                {
                    self.denPosition = self.lastVanillaDen = "SU_INTRO01";
                }
            }
        }


        private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
        {
            orig(self, saveStateNumber, game);
            if (game.rainWorld.BuffMode())
            {
                self.saveState.deathPersistentSaveData.karmaCap =
                    Mathf.Min(self.saveState.deathPersistentSaveData.karmaCap, 4);
                self.saveState.deathPersistentSaveData.karma = 
                    Custom.IntClamp(self.saveState.deathPersistentSaveData.karma, 0, 4);
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
