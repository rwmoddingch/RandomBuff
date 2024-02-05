using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using RandomBuff.Core.BuffMenu;
using RandomBuff.Core.BuffMenu.Test;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;
using UnityEngine;

namespace RandomBuff.Core.Hooks
{
    static class CoreHooks
    {
        public static void OnModsInit()
        {
            On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;

            On.RainWorldGame.Update += RainWorldGame_Update;
            On.RainWorldGame.Win += RainWorldGame_Win;
            On.RainWorldGame.GhostShutDown += RainWorldGame_GhostShutDown;
            On.PlayerProgression.WipeSaveState += PlayerProgression_WipeSaveState;
            On.PlayerProgression.WipeAll += PlayerProgression_WipeAll;
            On.ProcessManager.PreSwitchMainProcess += ProcessManager_PreSwitchMainProcess;

            if (BuffPlugin.DevEnabled)
            {
                On.Menu.MainMenu.ctor += MainMenu_ctor;
                TestStartGameMenu = new("TestStartGameMenu");
                On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess1;
            }

            BuffPlugin.Log("Core Hook Loaded");
        }

        private static void ProcessManager_PostSwitchMainProcess1(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (ID == TestStartGameMenu)
            {
                self.currentMainLoop = new TStartGameMenu(self, ID);
            }
            orig(self, ID);
      
        }

        public static ProcessManager.ProcessID TestStartGameMenu = new ("TestStartGameMenu");
        private static void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);

            float buttonWidth = MainMenu.GetButtonWidth(self.CurrLang);
            Vector2 pos = new Vector2(683f - buttonWidth / 2f, 0f);
            Vector2 size = new Vector2(buttonWidth, 30f);
            self.AddMainMenuButton(new SimpleButton(self, self.pages[0], "BUFF", "BUFF", pos, size), () =>
            {
                self.manager.RequestMainProcessSwitch(TestStartGameMenu);
                self.PlaySound(SoundID.MENU_Switch_Page_In);
            }, 0);
        }

        private static void ProcessManager_PreSwitchMainProcess(On.ProcessManager.orig_PreSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (self.rainWorld.options.saveSlot >= 100 && ID == ProcessManager.ProcessID.MainMenu)
            {
                int lastSlot = self.rainWorld.options.saveSlot;
                self.rainWorld.options.saveSlot -= 100;

                BuffPlugin.Log($"Change slot from {lastSlot} to {self.rainWorld.options.saveSlot}");
                self.rainWorld.progression.Destroy(lastSlot);
                self.rainWorld.progression = new PlayerProgression(self.rainWorld, true, true);
            }
            orig(self, ID);
        }

        private static void PlayerProgression_WipeAll(On.PlayerProgression.orig_WipeAll orig, PlayerProgression self)
        {
            orig(self);
            if (self.rainWorld.options.saveSlot >= 100)
                BuffDataManager.Instance.DeleteAll();

        }

        private static void PlayerProgression_WipeSaveState(On.PlayerProgression.orig_WipeSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber)
        {
            orig(self,saveStateNumber);
            if(self.rainWorld.options.saveSlot >= 100)
                BuffDataManager.Instance.DeleteSaveData(saveStateNumber);
        }

        private static void RainWorldGame_GhostShutDown(On.RainWorldGame.orig_GhostShutDown orig, RainWorldGame self, GhostWorldPresence.GhostID ghostID)
        {
            BuffPoolManager.Instance?.WinGame();
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

        private static void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (BuffPoolManager.Instance != null &&
                self.oldProcess is RainWorldGame game &&
                (ID == ProcessManager.ProcessID.SleepScreen || ID == ProcessManager.ProcessID.Dream))
            {
                BuffPoolManager.Instance.Destroy();
                self.currentMainLoop = new TGachaMenu(ID, game, self);
                //TODO : TEST UI
                ID = TestStartGameMenu;
            }

            orig(self, ID);
            if (self.currentMainLoop is RainWorldGame game2 && 
                BuffPoolManager.Instance == null &&
                game2.rainWorld.options.saveSlot >= 100)
            {
                BuffPoolManager.LoadGameBuff(game2);
            }
        }
    }
}
