using RandomBuff.Core.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using MonoMod.RuntimeDetour;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.SaveData.BuffConfig;
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

            BuffCore.OnGameSettingSpecialSetupInternal(saveState, gameSetting);
        }

        public static bool IsCurrentFullRoomSettingNeed(GameSetting gameSetting)
        {
            return gameSetting.MissionId is "DoomExpress" or "EmergnshyTreatment" || BuffCore.IsFullRoomSettingNeedInternal(gameSetting);
        }


        public static bool IsSpRoomSettingNeedRemoveAtNormal(string roomName)
        {
            return RemoveRoomSettingList.Contains(roomName) ||
                   BuffCore.IsSpRoomScriptNeedRemoveAtNormalInternal(roomName);
        }

        public static void ClampKarmaForBuffMode(ref int karma,ref int karmaCap)
        {
            if (BuffCore.OnClampKarmaForBuffModeInternal(ref karma, ref karmaCap))
                return;
            
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


            On.SaveState.setDenPosition += SaveState_setDenPosition;
            On.World.SpawnGhost += World_SpawnGhost;
            On.Room.Loaded += Room_Loaded;

            On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;

            On.Menu.SleepAndDeathScreen.AddPassageButton += SleepAndDeathScreen_AddPassageButton;
            On.Menu.SleepAndDeathScreen.Singal += SleepAndDeathScreen_Singal;


            On.DreamsState.StaticEndOfCycleProgress += DreamsState_StaticEndOfCycleProgress;

            On.SLOracleBehavior.InitCutsceneObjects += SLOracleBehavior_InitCutsceneObjects;

            On.Player.ctor += Player_ctor;
            _ = new Hook(
                typeof(StoryGameSession).GetProperty(nameof(StoryGameSession.RedIsOutOfCycles),
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetGetMethod(),
                (Func<StoryGameSession, bool> orig, StoryGameSession self) =>
                {
                    if (Custom.rainWorld.BuffMode())
                        return false;
                    return orig(self);
                });

            _ = new Hook(typeof(ProcessManager.MenuSetup).GetProperty("LoadInitCondition").GetGetMethod(),
                (Func<ProcessManager.MenuSetup, bool> orig, ProcessManager.MenuSetup self) => orig(self) || self.startGameCondition == BuffEnums.StoryGameInitCondition.BuffStackLoad);

            _ = new Hook(typeof(RainWorldGame).GetProperty("InitialBlackSeconds").GetGetMethod(),
                (Func< RainWorldGame, float> orig, RainWorldGame self) =>
                {
                    if (self.manager.menuSetup.startGameCondition == BuffEnums.StoryGameInitCondition.BuffStackLoad)
                        return 10;
                    return orig(self);
                });
            _ = new Hook(typeof(RainWorldGame).GetProperty("FadeInTime").GetGetMethod(),
                (Func<RainWorldGame, float> orig, RainWorldGame self) =>
                {
                    if (self.manager.menuSetup.startGameCondition == BuffEnums.StoryGameInitCondition.BuffStackLoad)
                        return 0;
                    return orig(self);
                });
            _ = new Hook(typeof(SaveState).GetProperty("SlowFadeIn").GetGetMethod(),
                (Func<SaveState, float> orig, SaveState self) =>
                {
                    if (Custom.rainWorld.BuffMode())
                        return self.malnourished ? 4 : 0.8f;
                    return orig(self);
                });
            On.RainWorld.PostModsInit += RainWorld_PostModsInit;

            On.ScavengerBomb.Update += ScavengerBomb_Update;
            On.ScavengerBomb.Explode += ScavengerBomb_Explode;

        }

        private static void ScavengerBomb_Explode(On.ScavengerBomb.orig_Explode orig, ScavengerBomb self, BodyChunk hitChunk)
        {
            if (self.smoke?.slatedForDeletetion ?? false)
                self.smoke = null;
            orig(self,hitChunk);
        }

        private static void ScavengerBomb_Update(On.ScavengerBomb.orig_Update orig, ScavengerBomb self, bool eu)
        {
            orig(self, eu);
            if (self.smoke?.slatedForDeletetion ?? false)
                self.smoke = null;
        }

        private static void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            On.SaveState.ctor += SaveState_setup;
        }

        private static void SLOracleBehavior_InitCutsceneObjects(On.SLOracleBehavior.orig_InitCutsceneObjects orig, SLOracleBehavior self)
        {
            if (Custom.rainWorld.BuffMode() &&
                self.oracle.room.game.GetStorySession.saveStateNumber != SlugcatStats.Name.Red)
                return;
            orig(self);
        }

        private static void DreamsState_StaticEndOfCycleProgress(On.DreamsState.orig_StaticEndOfCycleProgress orig, SaveState saveState, string currentRegion, string denPosition, ref int cyclesSinceLastDream, ref int cyclesSinceLastFamilyDream, ref int cyclesSinceLastGuideDream, ref int inGWOrSHCounter, ref DreamsState.DreamID upcomingDream, ref DreamsState.DreamID eventDream, ref bool everSleptInSB, ref bool everSleptInSB_S01, ref bool guideHasShownHimselfToPlayer, ref int guideThread, ref bool guideHasShownMoonThisRound, ref int familyThread)
        {
            if (Custom.rainWorld.BuffMode())
            {
                upcomingDream = null;
                return;
            };
            orig(saveState, currentRegion, denPosition, ref cyclesSinceLastDream, ref cyclesSinceLastFamilyDream,
                ref cyclesSinceLastGuideDream, ref inGWOrSHCounter, ref upcomingDream, ref eventDream,
                ref everSleptInSB, ref everSleptInSB_S01, ref guideHasShownHimselfToPlayer, ref guideThread,
                ref guideHasShownMoonThisRound, ref familyThread);
        }





        #region Menu

        private static void SleepAndDeathScreen_AddPassageButton(On.Menu.SleepAndDeathScreen.orig_AddPassageButton orig, SleepAndDeathScreen self, bool buttonBlack)
        {
            orig(self, buttonBlack);
            if (self.manager.rainWorld.BuffMode() && self.saveState != null && BuffDataManager.Instance.GetGameSetting(self.saveState.saveStateNumber).CanStackByPassage && !self.saveState.malnourished)
            {
                if (BuffCore.GetAllBuffIds(self.saveState.saveStateNumber).Any(i => i.GetStaticData().Stackable))
                {
                    self.pages[0].subObjects.Add(new SimpleButton(self, self.pages[0],
                        BuffResourceString.Get("SleepMenu_Stack"),
                        "BUFF_STACK_PASSAGE",
                        new Vector2(self.LeftHandButtonsPosXAdd + self.manager.rainWorld.options.SafeScreenOffset.x,
                            Mathf.Max(self.manager.rainWorld.options.SafeScreenOffset.y, 15f) + 40*2),
                        new Vector2(110f, 30f)));
                
                }

                if (BuffCore.GetAllBuffIds(self.saveState.saveStateNumber).Any())
                {
                    self.pages[0].subObjects.Add(new SimpleButton(self, self.pages[0],
                        BuffResourceString.Get("SleepMenu_UnStack"),
                        "BUFF_UNSTACK_PASSAGE",
                        new Vector2(self.LeftHandButtonsPosXAdd + self.manager.rainWorld.options.SafeScreenOffset.x,
                            Mathf.Max(self.manager.rainWorld.options.SafeScreenOffset.y, 15f) + 40),
                        new Vector2(110f, 30f)));
                }
            }
        }

        private static void SleepAndDeathScreen_Singal(On.Menu.SleepAndDeathScreen.orig_Singal orig, SleepAndDeathScreen self, MenuObject sender, string message)
        {
            orig(self, sender, message);

            if (self.endGameSceneCounter < 1 && self.manager.upcomingProcess is null)
            {
                switch (message)
                {
                    case "BUFF_STACK_PASSAGE":
                        self.proceedWithEndgameID ??= self.winState.GetNextEndGame();
                        if (self.proceedWithEndgameID != null)
                        {
                            self.endgameTokens.Passage(self.proceedWithEndgameID);
                            //self.winState.ConsumeEndGame();
                            //self.manager.rainWorld.progression.SaveWorldStateAndProgression(false);
                            self.manager.RequestMainProcessSwitch(BuffEnums.ProcessID.StackMenu);
                            self.PlaySound(SoundID.MENU_Passage_Button);
                        }

                        break;
                    case "BUFF_UNSTACK_PASSAGE":
                        self.proceedWithEndgameID ??= self.winState.GetNextEndGame();
                        if (self.proceedWithEndgameID != null)
                        {
                            self.endgameTokens.Passage(self.proceedWithEndgameID);
                            //self.winState.ConsumeEndGame();
                            //self.manager.rainWorld.progression.SaveWorldStateAndProgression(false);
                            self.manager.RequestMainProcessSwitch(BuffEnums.ProcessID.UnstackMenu);
                            self.PlaySound(SoundID.MENU_Passage_Button);
                        }
                        break;
                }
            }
        }

        #endregion





        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature,world);
            if (Custom.rainWorld.BuffMode())
                self.redsIllness = null;
        }



        #region HUD

        private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig(self, cam);
            if (self.rainWorld.BuffMode())
            {
                self.AddPart(new BuffHud(self));
                //self.AddPart(new TConditionHud(self));
            }
        }


        private static bool lastBuffShowCursor = false;

        private static void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig(self, dt);
            if ((BuffHud.Instance?.NeedShowCursor ?? false))
                Cursor.visible = true;
            else if (lastBuffShowCursor != (BuffHud.Instance?.NeedShowCursor ?? false))
                Cursor.visible = self.devUI != null || !self.rainWorld.options.fullScreen;
            lastBuffShowCursor = (BuffHud.Instance?.NeedShowCursor ?? false);
        }

        #endregion


        #region StoryState

        private static void World_SpawnGhost(On.World.orig_SpawnGhost orig, World self)
        {
            if (Custom.rainWorld.BuffMode() && (Custom.rainWorld.progression.currentSaveState.cycleNumber == 0 || BuffDataManager.Instance.GetGameSetting(self.game.StoryCharacter).MissionId != null))
                return;
            orig(self);

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
                    IsSpRoomSettingNeedRemoveAtNormal(self.abstractRoom.name))
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


        private static void SaveState_setup(On.SaveState.orig_ctor orig, SaveState self, SlugcatStats.Name saveStateNumber, PlayerProgression progression)
        {
            orig(self, saveStateNumber, progression);
            if (Custom.rainWorld.BuffMode())
            {
                BuffPlugin.LogDebug("Init buff saveState");
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
                GameSettingSpecialSetup(self, BuffDataManager.Instance.GetGameSetting(saveStateNumber));
                //self.dreamsState = null;
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
                else if (BuffDataManager.Instance.GetGameSetting(self.saveStateNumber).gachaTemplate.ForceStartPos is { } pos)
                {
                    BuffPlugin.LogDebug($"Force start pos:{pos}");
                    self.denPosition = self.lastVanillaDen = pos;
                }
            }
        }



        #endregion


        #region PoolManager

        private static void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
        {
            BuffPoolManager.Instance?.WinGame(malnourished);
            orig(self, malnourished);
        }


        private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (self.manager.menuSetup.startGameCondition == BuffEnums.StoryGameInitCondition.BuffStackLoad && self.Players[0].realizedCreature != null)
            {
                self.GetStorySession.saveState.deathPersistentSaveData.winState.ConsumeEndGame();
                self.manager.rainWorld.progression.SaveWorldStateAndProgression(false);
                self.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
                return;
            }

            BuffPoolManager.Instance?.Update(self);
        }

        #endregion


    }
}
