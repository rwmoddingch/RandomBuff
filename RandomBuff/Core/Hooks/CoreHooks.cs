using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JollyCoop;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RandomBuff.Cardpedia;
using RandomBuff.Core.Buff;
using RandomBuff.Core.BuffMenu;
using RandomBuff.Core.BuffMenu.Test;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;
using RandomBuff.Core.StaticsScreen;
using RandomBuff.Credit;
using RWCustom;
using UnityEngine;
using static RandomBuff.Core.BuffMenu.BuffGameMenu;

namespace RandomBuff.Core.Hooks
{
    static  partial class CoreHooks
    {
        public static void OnModsInit()
        {
            On.ProcessManager.PreSwitchMainProcess += ProcessManager_PreSwitchMainProcess;
            On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;


            On.PlayerProgression.WipeSaveState += PlayerProgression_WipeSaveState;
            On.PlayerProgression.WipeAll += PlayerProgression_WipeAll;

      
            On.Menu.MainMenu.ctor += MainMenu_ctor;
            On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess1;

            On.Menu.SlugcatSelectMenu.SlugcatUnlocked += SlugcatSelectMenu_SlugcatUnlocked;
            On.Menu.SlugcatSelectMenu.SlugcatPage.Scroll += SlugcatPage_Scroll;
            On.Menu.SlugcatSelectMenu.SlugcatPage.NextScroll += SlugcatPage_NextScroll;

            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;

            On.SlugcatStats.SlugcatUnlocked += SlugcatStats_SlugcatUnlocked;
            On.JollyCoop.JollyCustom.SlugClassMenu += JollyCustom_SlugClassMenu;
            On.JollyCoop.JollyMenu.JollyPlayerSelector.Update += JollyPlayerSelector_Update;
            On.ModManager.ModFolderHasDLLContent += ModManager_ModFolderHasDLLContent;


            InGameHooksInit();


            BuffPlugin.Log("Core Hook Loaded");

            //TestStartGameMenu = new("TestStartGameMenu");

        }

        private static void JollyPlayerSelector_Update(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_Update orig, JollyCoop.JollyMenu.JollyPlayerSelector self)
        {
            orig(self);
            if(self.menu.manager.rainWorld.BuffMode() && self.index == 0)
                self.classButton.GetButtonBehavior.greyedOut = true;

        }

        private static bool ModManager_ModFolderHasDLLContent(On.ModManager.orig_ModFolderHasDLLContent orig, string folder)
        {
            return orig(folder) || Directory.Exists(Path.Combine(folder, "buffplugins"));
        }

        private static void ModApplyer_ApplyModsThread(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(MoveType.After,
                    i => i.MatchLdsfld<ModManager>("InstalledMods"),
                    i => i.MatchLdloc(8),
                    i => i.Match(OpCodes.Callvirt),
                    i => i.MatchLdfld<ModManager.Mod>("path"),
                    i => i.MatchLdstr("plugins"),
                    i => i.Match(OpCodes.Call),
                    i => i.Match(OpCodes.Call),
                    i=>i.Match(OpCodes.Brtrue_S));
                var label = c.Previous.Operand as ILLabel;
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_S,(byte)8);
                c.EmitDelegate<Func<ModManager.ModApplyer, int, bool>>((self, i) =>
                    Directory.Exists(Path.Combine(ModManager.InstalledMods[i].path, "buffplugins")));
                c.Emit(OpCodes.Brtrue_S, label);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static bool SlugcatStats_SlugcatUnlocked(On.SlugcatStats.orig_SlugcatUnlocked orig, SlugcatStats.Name i, RainWorld rainWorld)
        {
            if (!Custom.rainWorld.BuffMode())
                return orig(i, rainWorld);
            return true;
        }

        private static SlugcatStats.Name JollyCustom_SlugClassMenu(On.JollyCoop.JollyCustom.orig_SlugClassMenu orig, int playerNumber, SlugcatStats.Name fallBack)
        {
            if(!Custom.rainWorld.BuffMode())
                return orig(playerNumber,fallBack);

            SlugcatStats.Name name = JollyCustom.JollyOptions(playerNumber).playerClass;
            if (name == null ||
                SlugcatStats.HiddenOrUnplayableSlugcat(name) || 
                (SlugcatStats.IsSlugcatFromMSC(name) && !ModManager.MSC))
            {
                JollyCustom.JollyOptions(playerNumber).playerClass = fallBack;
                name = fallBack;
            }
            return name;
        }

        private static float SlugcatPage_NextScroll(On.Menu.SlugcatSelectMenu.SlugcatPage.orig_NextScroll orig, SlugcatSelectMenu.SlugcatPage self, float timeStacker)
        {
            if (self is SlugcatIllustrationPage page)
                return page.NextScroll(timeStacker);
            return orig(self, timeStacker);
        }

        private static float SlugcatPage_Scroll(On.Menu.SlugcatSelectMenu.SlugcatPage.orig_Scroll orig, SlugcatSelectMenu.SlugcatPage self, float timeStacker)
        {
            if (self is SlugcatIllustrationPage page)
                return page.Scroll(timeStacker);
            return orig(self, timeStacker);
        }

        private static bool SlugcatSelectMenu_SlugcatUnlocked(On.Menu.SlugcatSelectMenu.orig_SlugcatUnlocked orig, SlugcatSelectMenu self, SlugcatStats.Name i)
        {
            if (self.saveGameData.Count == 0)
                return true;
            return orig(self, i);
        }

        private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig(self, cam);
            if (self.rainWorld.BuffMode())
            {
                self.AddPart(new BuffHud(self));
                //self.AddPart(new TConditionHud(self));
            }
        }

        private static void ProcessManager_PostSwitchMainProcess1(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (ID == BuffEnums.ProcessID.TestStartGameMenu)
            {
                self.currentMainLoop = new BuffGameMenu(self, ID);
            }
            else if(ID == BuffEnums.ProcessID.Cardpedia)
            {
                self.currentMainLoop = new CardpediaMenu(self);
            }
            else if(ID == BuffEnums.ProcessID.BuffGameWinScreen)
            {
                self.currentMainLoop = new BuffGameWinScreen(self);
            }
            else if(ID == BuffEnums.ProcessID.CreditID)
            {
                self.currentMainLoop = new BuffCreditMenu(self);
            }
            orig(self, ID);
      
        }

        
        private static void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);

            float buttonWidth = MainMenu.GetButtonWidth(self.CurrLang);
            Vector2 pos = new Vector2(683f - buttonWidth / 2f, 0f);
            Vector2 size = new Vector2(buttonWidth, 30f);
            self.AddMainMenuButton(new SimpleButton(self, self.pages[0], BuffResourceString.Get("MainMenu_Buff"), "BUFF", pos, size), () =>
            {
                self.manager.RequestMainProcessSwitch(BuffEnums.ProcessID.TestStartGameMenu);
                self.PlaySound(SoundID.MENU_Switch_Page_In);
            }, 0);
        }

        private static void ProcessManager_PreSwitchMainProcess(On.ProcessManager.orig_PreSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (self.rainWorld.BuffMode() && ID == ProcessManager.ProcessID.MainMenu)
            {
                int lastSlot = self.rainWorld.options.saveSlot;
                self.rainWorld.options.saveSlot -= 100;

                BuffPlugin.Log($"Change slot from {lastSlot} to {self.rainWorld.options.saveSlot}");
                self.rainWorld.progression.Destroy(lastSlot);
                self.rainWorld.progression = new PlayerProgression(self.rainWorld, true, false);
            }
            orig(self, ID);
        }

        private static void PlayerProgression_WipeAll(On.PlayerProgression.orig_WipeAll orig, PlayerProgression self)
        {
            orig(self);
            BuffDataManager.Instance.DeleteAll();
            BuffPlayerData.LoadBuffPlayerData("",BuffPlugin.saveVersion);
            BuffConfigManager.LoadConfig("", BuffPlugin.saveVersion);
            BuffFile.Instance.DeleteAllFile();

        }

        private static void PlayerProgression_WipeSaveState(On.PlayerProgression.orig_WipeSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber)
        {
            orig(self,saveStateNumber);
            if(self.rainWorld.BuffMode())
                BuffDataManager.Instance.DeleteSaveData(saveStateNumber);
        }


        private static void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (BuffPoolManager.Instance != null &&
                self.oldProcess is RainWorldGame game)
            {
                BuffPoolManager.Instance.Destroy();
                if (BuffDataManager.Instance.GetGameSetting(game.StoryCharacter).gachaTemplate.CurrentPacket.NeedMenu &&
                    (ID == ProcessManager.ProcessID.SleepScreen || ID == ProcessManager.ProcessID.Dream))
                {
                    self.currentMainLoop = new GachaMenu.GachaMenu(ID, game, self);
                    ID = GachaMenu.GachaMenu.GachaMenuID;
                }
            }

            if (ID == ProcessManager.ProcessID.MainMenu)
            {
                if (self.oldProcess is KarmaLadderScreen screen)
                {
                    foreach (var buff in BuffCore.GetAllBuffIds(screen.saveState.saveStateNumber))
                        BuffHookWarpper.DisableBuff(buff, HookLifeTimeLevel.UntilQuit);
                }
                else if(self.oldProcess is RainWorldGame game3)
                {
                    foreach (var buff in BuffCore.GetAllBuffIds(game3.StoryCharacter))
                        BuffHookWarpper.DisableBuff(buff, HookLifeTimeLevel.UntilQuit);
                }
            }
            

            orig(self, ID);
            if (self.currentMainLoop is RainWorldGame game2)
            {
                if (game2.rainWorld.BuffMode())
                {
                    if (BuffPoolManager.Instance == null)
                        BuffPoolManager.LoadGameBuff(game2);
                }
                else
                {
                    BuffHookWarpper.CheckAndDisableAllHook();
                }
            }
        }
    }
}
