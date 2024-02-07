using Menu;
using RandomBuff.Render.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;
using RWCustom;
using UnityEngine;
using System.Globalization;
using System.Text.RegularExpressions;
using JollyCoop.JollyMenu;

namespace RandomBuff.Core.BuffMenu
{
    internal class BuffGameMenu : Menu.Menu, CheckBox.IOwnCheckBox
    {
        private RainEffect rainEffect;

        private List<SlugcatStats.Name> slugNameOrders = new ();
        private Dictionary<SlugcatStats.Name, WawaSaveData> saveGameData = new ();

        private bool restartCurrent;
        private bool loaded = false;

        //菜单元素
        private HoldButton startButton;
        private SimpleButton backButton;
        private BigArrowButton prevButton;
        private BigArrowButton nextButton;
        private SimpleButton settingButton;
        private CheckBox restartCheckbox;
        private SimpleButton jollyToggleConfigMenu;


        private MenuLabel testLabel;


        private SlugcatStats.Name CurrentName => slugNameOrders[currentIndex];
        private int currentIndex = 0;

        private BuffFile.BuffFileCompletedCallBack callBack;


        public BuffGameMenu(ProcessManager manager, ProcessManager.ProcessID ID) : base(manager, ID)
        {
            SetupSlugNameOrders();
            //延迟加载等待存档载入完毕
            callBack = new BuffFile.BuffFileCompletedCallBack(OnDataLoaded);

            if (manager.rainWorld.options.saveSlot < 100)//诺普的存档加载
            {
                var lastSlot = manager.rainWorld.options.saveSlot;
                BuffPlugin.Log($"Enter from slot {lastSlot}, To {manager.rainWorld.options.saveSlot += 100}");
                manager.rainWorld.progression.Destroy(lastSlot);
                manager.rainWorld.progression = new PlayerProgression(manager.rainWorld, true, false);
            }
         
        }

        void OnDataLoaded()
        {
            BuffPlugin.LogDebug("Load Completed!");
            loaded = true;
            foreach (var name in slugNameOrders)
            {
                saveGameData.Add(name, MineFromSave(manager, name));
            }

            pages = new List<Page>()
            {
                new (this, null, "WawaPage", 0)
            };

            //构建页面
            pages[0].subObjects.Add(rainEffect = new RainEffect(this, pages[0]));

            pages[0].subObjects.Add(startButton = new HoldButton(this, this.pages[0], Translate(SlugcatStats.getSlugcatName(CurrentName)), "START", new Vector2(683f, 85f), 40f));
            pages[0].subObjects.Add(backButton = new SimpleButton(this, this.pages[0], base.Translate("BACK"), "BACK", new Vector2(200f, 668f), new Vector2(110f, 30f)));
            pages[0].subObjects.Add(prevButton = new BigArrowButton(this, this.pages[0], "PREV", new Vector2(200f, 50f), -1));
            pages[0].subObjects.Add(nextButton = new BigArrowButton(this, this.pages[0], "NEXT", new Vector2(1116f, 50f), 1));
            pages[0].subObjects.Add(settingButton = new SimpleButton(this, this.pages[0], Translate(BuffDataManager.Instance.GetSafeSetting(CurrentName).ID.value),
                "SELECT_MODE", new Vector2(683 - 240f, Mathf.Max(30, Custom.rainWorld.options.SafeScreenOffset.y)),
                new Vector2(120, 40)));
            if(ModManager.JollyCoop)
                pages[0].subObjects.Add(jollyToggleConfigMenu = new SimpleButton(this, this.pages[0], Translate("SHOW"), "JOLLY_TOGGLE_CONFIG", 
                    new Vector2(1056f, manager.rainWorld.screenSize.y - 100f), new Vector2(110f, 30f)));

            pages[0].subObjects.Add(testLabel = new MenuLabel(this, pages[0], "",new Vector2(manager.rainWorld.screenSize.x/2 - 250, 484 - 249f),new Vector2(500,50),true));
            testLabel.label.alignment = FLabelAlignment.Center;
            testLabel.label.color = MenuColor(MenuColors.White).rgb;
            float restartTextWidth = SlugcatSelectMenu.GetRestartTextWidth(CurrLang);
            float restartTextOffset = SlugcatSelectMenu.GetRestartTextOffset(CurrLang);

            pages[0].subObjects.Add(restartCheckbox = new CheckBox(this, this.pages[0], this, new Vector2(this.startButton.pos.x + 200f + restartTextOffset, Mathf.Max(30f, manager.rainWorld.options.SafeScreenOffset.y)), restartTextWidth, base.Translate("Restart game"), "RESTART", false));
            restartCheckbox.label.pos.x += (restartTextWidth - restartCheckbox.label.label.textRect.width - 5f);

            UpdateSlugcat();
        }


        void SetupSlugNameOrders()
        {
            foreach(var entry in SlugcatStats.Name.values.entries)
            {
                if(entry.Contains("Jolly") || entry == SlugcatStats.Name.Night.value || entry == MoreSlugcatsEnums.SlugcatStatsName.Slugpup.value)
                    continue;
                
                slugNameOrders.Add(new SlugcatStats.Name(entry));
            }
        }


        void UpdateSlugcat()
        {
            var safeData = BuffDataManager.Instance.GetSafeSetting(CurrentName);

            startButton.menuLabel.text = Translate(SlugcatStats.getSlugcatName(CurrentName));
            settingButton.inactive = safeData.instance != null && !restartCurrent;
            settingButton.menuLabel.text = Translate(safeData.ID.ToString());
            if(manager.rainWorld.progression.IsThereASavedGame(CurrentName))
            {
                //暂时使用
                var re = SlugcatSelectMenu.MineForSaveData(manager, CurrentName);
                if (re != null)
                {
                    testLabel.label.text =
                        $"{re.shelterName} - Cycle: {re.cycle} - Buff Count: {BuffDataManager.Instance.GetAllBuffIds(CurrentName).Count}";
                }
                else
                {
                    testLabel.label.text = $"UNKNOWN DATA - Buff Count: {BuffDataManager.Instance.GetAllBuffIds(CurrentName).Count}";
                }
            }
            else
            {
                testLabel.label.text = "NEW GAME";
            }
        }


        WawaSaveData MineFromSave(ProcessManager manager, SlugcatStats.Name slugcat)
        {
            if (!manager.rainWorld.progression.IsThereASavedGame(slugcat))
            {
                return null;
            }
            if(manager.rainWorld.progression.currentSaveState != null && manager.rainWorld.progression.currentSaveState.saveStateNumber == slugcat)
            {
                WawaSaveData result = new WawaSaveData();
                result.karmaCap =    manager.rainWorld.progression.currentSaveState.deathPersistentSaveData.karmaCap;
                result.karma =       manager.rainWorld.progression.currentSaveState.deathPersistentSaveData.karma;
                result.food =        manager.rainWorld.progression.currentSaveState.food;
                result.cycle =       manager.rainWorld.progression.currentSaveState.cycleNumber;
                result.hasGlow =     manager.rainWorld.progression.currentSaveState.theGlow;
                result.hasMark =     manager.rainWorld.progression.currentSaveState.deathPersistentSaveData.theMark;
                result.shelterName = manager.rainWorld.progression.currentSaveState.GetSaveStateDenToUse();
                result.karmaRF =     manager.rainWorld.progression.currentSaveState.deathPersistentSaveData.reinforcedKarma;
                return result;
            }
            if (!manager.rainWorld.progression.HasSaveData)
                return null;
            return null;
        }


        public bool GetChecked(CheckBox box)
        {
            return restartCurrent;
        }

        public void SetChecked(CheckBox box, bool c)
        {
            restartCurrent = c;
            settingButton.inactive = BuffDataManager.Instance.GetSafeSetting(CurrentName).instance != null && !restartCurrent;

        }

        public override void Singal(MenuObject sender, string message)
        {
            if(message == "BACK")
            {
                manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                PlaySound(SoundID.MENU_Switch_Page_Out);
            }
            else if (message == "PREV")
            {
                currentIndex--;
                if(currentIndex < 0)
                    currentIndex = slugNameOrders.Count - 1;
                UpdateSlugcat();
            }
            else if (message == "NEXT")
            {
                currentIndex = (currentIndex + 1) % (slugNameOrders.Count);
                UpdateSlugcat();
            }
            else if (message == "START")
            {

                if (!manager.rainWorld.progression.IsThereASavedGame(CurrentName) || restartCurrent)
                {
                    manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat =
                        CurrentName;
                    manager.rainWorld.progression.WipeSaveState(CurrentName);
                    manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;

                }
                else
                {
                    manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat =
                        CurrentName;
                    manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
                }

                BuffDataManager.Instance.StartGame(CurrentName);
                manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
                PlaySound(SoundID.MENU_Start_New_Game);
            }
            else if (message == "SELECT_MODE" && !settingButton.inactive)
            {
                var safeData = BuffDataManager.Instance.GetSafeSetting(CurrentName);
                safeData.ID = new(BuffSettingID.values.entries[
                    safeData.ID.Index == BuffSettingID.values.entries.Count - 1 ? 0 : safeData.ID.Index + 1]);

                settingButton.menuLabel.text = Translate(safeData.ID.ToString());
            }
            else if (message == "JOLLY_TOGGLE_CONFIG")
            {
                JollySetupDialog dialog = new JollySetupDialog(CurrentName, manager, jollyToggleConfigMenu.pos);
                manager.ShowDialog(dialog);
                PlaySound(SoundID.MENU_Switch_Page_In);
            }
        }

        public override void Update()
        {
            if (!loaded)
                return;

            base.Update();
        }


        public override void RawUpdate(float dt)
        {
            if (!loaded)
            {
                manager.blackDelay = 0.1f;
                return;
            }
            base.RawUpdate(dt);
        }



        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
        }

        internal class WawaSaveData
        {
            public int karmaCap;
            public int karma;
            public int food;
            public int cycle;
            public bool hasGlow;
            public bool hasMark;
            public string shelterName;
            public bool karmaRF;
        }
    }
}
