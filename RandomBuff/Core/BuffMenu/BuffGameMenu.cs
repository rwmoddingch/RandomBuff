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

        internal List<SlugcatStats.Name> slugNameOrders = new ();
        internal List<SlugcatIllustrationPage> slugcatPages = new ();
        internal Dictionary<SlugcatStats.Name, WawaSaveData> saveGameData = new ();

        private bool restartCurrent;
        private bool loaded = false;

        private MenuLabel testLabel;

        internal BuffGameMenuSlot menuSlot;
        internal SlugcatStats.Name CurrentName => slugNameOrders[currentPageIndex];
        internal int currentPageIndex = 0;

        float scrolledPageIndex;
        int targetScrolledPageIndex;
        int intScrolledPageIndex;

        public float scroll;
        public float lastScroll;
        int quedSideInput;

        public float NextScroll
        {
            get => scroll;
        }

        private BuffFile.BuffFileCompletedCallBack callBack;


        public BuffGameMenu(ProcessManager manager, ProcessManager.ProcessID ID) : base(manager, ID)
        {
            SetupSlugNameOrders();
            menuSlot = new BuffGameMenuSlot(this);

            //延迟加载等待存档载入完毕
            callBack = new BuffFile.BuffFileCompletedCallBack(OnDataLoaded);

            if (!manager.rainWorld.BuffMode())//诺普的存档加载
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
                if (!manager.rainWorld.progression.IsThereASavedGame(name))
                {
                    if (BuffDataManager.Instance.GetAllBuffIds(name).Count > 0)
                        BuffDataManager.Instance.DeleteSaveData(name);
                } 
                saveGameData.Add(name, MineFromSave(manager, name));
            }
       
            menuSlot.SetupBuffs(slugNameOrders);
       
            pages = new List<Page>()
            {
                new(this, null, "WawaButtonPage", 0),
                new(this, null, "GameDetailPage", 1),
                new (this, null, "WawaSlugcatPage", 2),
                
            };
           
            for (int i = 0; i < slugNameOrders.Count; i++)
            {
                slugcatPages.Add(new SlugcatIllustrationPage(this, null, i + 1, slugNameOrders[i]));
                pages.Add(slugcatPages[i]);
            }
            //pages.Add();
            //pages.Add();

            pages[0].subObjects.Add(rainEffect = new RainEffect(this, pages[0]));

            InitButtonPage(pages[0]);
            InitGameDetailPage(pages[0]);
            
            continueDetailPage.SetShow(false);
            newGameDetailPage.SetShow(false);

            //pages[0].Container.MoveToFront();
            container.AddChild(menuSlot.Container);
            //detailPage.Container.MoveToFront();

            UpdateSlugcatAndPage();
        }
        //

        BuffContinueGameDetialPage continueDetailPage;
        BuffNewGameDetailPage newGameDetailPage;
        void InitGameDetailPage(Page page)
        {
            page.subObjects.Add(continueDetailPage = new BuffContinueGameDetialPage(this, page, Vector2.zero));
            page.subObjects.Add(newGameDetailPage = new BuffNewGameDetailPage(this, page, Vector2.zero));

        }


        //菜单元素
        HoldButton startButton;
        SimpleButton backButton;
        BigArrowButton prevButton;
        BigArrowButton nextButton;
        //SimpleButton settingButton;
        //CheckBox restartCheckbox;
        SimpleButton jollyToggleConfigMenu;
        void InitButtonPage(Page page)
        {
            page.subObjects.Add(startButton = new HoldButton(this, page, Translate(SlugcatStats.getSlugcatName(CurrentName)), "START", new Vector2(683f, 85f), 40f));
            page.subObjects.Add(backButton = new SimpleButton(this, page, base.Translate("BACK"), "BACK", new Vector2(200f, 668f), new Vector2(110f, 30f)));
            page.subObjects.Add(prevButton = new BigArrowButton(this, page, "PREV", new Vector2(200f, 50f), -1));
            page.subObjects.Add(nextButton = new BigArrowButton(this, page, "NEXT", new Vector2(1116f, 50f), 1));
            //page.subObjects.Add(settingButton = new SimpleButton(this, page, Translate(BuffDataManager.Instance.GetGameSetting(CurrentName).TemplateName),
            //    "SELECT_MODE", new Vector2(683 - 240f, Mathf.Max(30, Custom.rainWorld.options.SafeScreenOffset.y)),
            //    new Vector2(120, 40)));

            if (ModManager.JollyCoop)
                page.subObjects.Add(jollyToggleConfigMenu = new SimpleButton(this, page, Translate("SHOW"), "JOLLY_TOGGLE_CONFIG",
                    new Vector2(1056f, manager.rainWorld.screenSize.y - 100f), new Vector2(110f, 30f)));

            page.subObjects.Add(testLabel = new MenuLabel(this, page, "", new Vector2(manager.rainWorld.screenSize.x / 2 - 250, 484 - 249f - 80f), new Vector2(500, 50), true));
            testLabel.label.alignment = FLabelAlignment.Center;
            testLabel.label.color = MenuColor(MenuColors.White).rgb;
            //float restartTextWidth = SlugcatSelectMenu.GetRestartTextWidth(CurrLang);
            //float restartTextOffset = SlugcatSelectMenu.GetRestartTextOffset(CurrLang);

            //page.subObjects.Add(restartCheckbox = new CheckBox(this, page, this, new Vector2(this.startButton.pos.x + 200f + restartTextOffset, Mathf.Max(30f, manager.rainWorld.options.SafeScreenOffset.y)), restartTextWidth, base.Translate("Restart game"), "RESTART", false));
            //restartCheckbox.label.pos.x += (restartTextWidth - restartCheckbox.label.label.textRect.width - 5f);
        }

        public void SetButtonsActive(bool active)
        {
            startButton.buttonBehav.greyedOut = !active;
            backButton.buttonBehav.greyedOut = !active;
            prevButton.buttonBehav.greyedOut = !active;
            nextButton.buttonBehav.greyedOut = !active;
            //settingButton.buttonBehav.greyedOut = !active;
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

        private bool IsInactive => manager.rainWorld.progression.IsThereASavedGame(CurrentName) && !restartCurrent;

        void UpdateSlugcatAndPage()
        {
            var gameSetting = BuffDataManager.Instance.GetGameSetting(CurrentName);

            startButton.menuLabel.text = Translate(SlugcatStats.getSlugcatName(CurrentName));
            //settingButton.inactive = IsInactive;
            //settingButton.menuLabel.text = Translate(gameSetting.TemplateName);
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
            menuSlot.UpdatePage(currentPageIndex);
            continueDetailPage.ChangeSlugcat(CurrentName);
            newGameDetailPage.SetShow(false);
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
            //settingButton.inactive = IsInactive;
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
                //quedSideInput = Math.Max(-3, quedSideInput - 1);
                targetScrolledPageIndex--;
                PlaySound(SoundID.MENU_Next_Slugcat);
                //UpdateSlugcat();
            }
            else if (message == "NEXT")
            {
                //quedSideInput = Math.Min(3, quedSideInput + 1);
                targetScrolledPageIndex++;
                PlaySound(SoundID.MENU_Next_Slugcat);
                //UpdateSlugcat();
            }
            else if (message == "START")
            {
                PlaySound(SoundID.MENU_Start_New_Game);
                if (manager.rainWorld.progression.IsThereASavedGame(CurrentName))
                {
                    manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat =
                        CurrentName;
                    manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
                    BuffDataManager.Instance.EnterGameFromMenu(CurrentName);
                    manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
                    PlaySound(SoundID.MENU_Start_New_Game);
                }
                else
                {
                    continueDetailPage.SetShow(false);
                    newGameDetailPage.SetShow(true);
                }
            }
            else if(message == "CONTINUE_DETAIL_RESTART")
            {
                manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat =
                        CurrentName;
                manager.rainWorld.progression.WipeSaveState(CurrentName);
                manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;

                continueDetailPage.ChangeSlugcat(CurrentName);
                menuSlot.SetupBuffs(slugNameOrders);
            }
            else if (message == "NEWGAME_DETAIL_NEWGAME")
            {
                manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat =
                        CurrentName;
                manager.rainWorld.progression.WipeSaveState(CurrentName);
                manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;

                BuffDataManager.Instance.EnterGameFromMenu(CurrentName);
                manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
                PlaySound(SoundID.MENU_Start_New_Game);
            }
            else if (message == "JOLLY_TOGGLE_CONFIG")
            {
                JollySetupDialog dialog = new JollySetupDialog(CurrentName, manager, jollyToggleConfigMenu.pos);
                manager.ShowDialog(dialog);
                PlaySound(SoundID.MENU_Switch_Page_In);
            }
        }

        float testDifficulty;
        public override float ValueOfSlider(Slider slider)
        {
            if (slider.ID == BuffGameMenuStatics.DifficultySlider)
                return testDifficulty;
            return 0f;
        }

        public override void SliderSetValue(Slider slider, float f)
        {
            if (slider.ID == BuffGameMenuStatics.DifficultySlider)
                testDifficulty = f;
        }

        public override void Update()
        {
            if (!loaded)
                return;

            base.Update();
            menuSlot.Update();

            lastScroll = scroll; 
            
            //testLabel.text = $"\ntarget:{targetScrolledPageIndex} scrolledPageIndex : {scrolledPageIndex}\nintScrolledPageIndex : {intScrolledPageIndex}\nscroll:{scroll}";
            if (scrolledPageIndex != targetScrolledPageIndex)
            {
                scrolledPageIndex = Mathf.Lerp(scrolledPageIndex, targetScrolledPageIndex, 0.15f);

                int lastIntScrolledPageIndex = intScrolledPageIndex;

                int iterator = targetScrolledPageIndex > scrolledPageIndex ? -1 : 1;
                int start = targetScrolledPageIndex;
                while (true)
                {
                    if(Mathf.Abs(start - scrolledPageIndex) <= 1f)
                    {
                        intScrolledPageIndex = start;
                        break;
                    }
                    start += iterator;
                }

                if (Mathf.Abs(scrolledPageIndex - targetScrolledPageIndex) < 0.001f)
                    scrolledPageIndex = targetScrolledPageIndex;

                if (intScrolledPageIndex != lastIntScrolledPageIndex)
                {
                    currentPageIndex = intScrolledPageIndex;
                    while (currentPageIndex < 0)
                        currentPageIndex += slugcatPages.Count;
                    while (currentPageIndex > slugcatPages.Count - 1)
                        currentPageIndex -= slugcatPages.Count;
                    lastScroll = scroll;
                    UpdateSlugcatAndPage();
                }

                if (intScrolledPageIndex != scrolledPageIndex)
                {
                    scroll = scrolledPageIndex - intScrolledPageIndex;
                }
                else
                {
                    scroll = 0;
                    lastScroll = 0;
                }
            }
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
            menuSlot.GrafUpdate(timeStacker);
        }

        public override void ShutDownProcess()
        {
            menuSlot.Destory();
            base.ShutDownProcess();
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
