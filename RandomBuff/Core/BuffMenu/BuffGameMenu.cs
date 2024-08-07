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
using BepInEx;
using Object = UnityEngine.Object;
using RandomBuff.Render.CardRender;
using RandomBuff.Core.BuffMenu.Test;
using RandomBuff.Render.UI.Notification;
using RandomBuff.Core.ProgressionUI;
using RandomBuff.Render.UI.Component;
using RandomBuff.Core.Progression.Quest;
using RandomBuff.Core.BuffMenu.Manual;
using RandomBuff.Core.Game.Settings;

namespace RandomBuff.Core.BuffMenu
{
    internal class BuffGameMenu : Menu.Menu, CheckBox.IOwnCheckBox
    {
        private RainEffect rainEffect;

        internal List<SlugcatStats.Name> slugNameOrders = new ();
        internal List<SlugcatIllustrationPage> slugcatPages = new ();
        internal Dictionary<SlugcatStats.Name, SlugcatSelectMenu.SaveGameData> saveGameData = new ();

        private bool restartCurrent;
        private bool loaded = false;

        private MenuLabel testLabel;

        public List<bool> flagNeedUpdate = new();
        public RandomBuffFlag flag;

        NotificationManager testNotification;
        public BuffExtraInfoPage extraInfoPage;

        internal BuffGameMenuSlot menuSlot;
        internal SlugcatStats.Name CurrentName => slugNameOrders[currentPageIndex];
        internal int currentPageIndex = 0;

        float scrolledPageIndex;
        int targetScrolledPageIndex;
        int intScrolledPageIndex;

        public float scroll;
        public float lastScroll;
        int quedSideInput;

        public float NextScroll => scroll;

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
                saveGameData.Add(name, SlugcatSelectMenu.MineForSaveData(manager, name));
            }

            flag = new RandomBuffFlag(new IntVector2(60, 30), new Vector2(1200f, 500f));
            menuSlot.SetupBuffs(slugNameOrders);
            testNotification = new NotificationManager(this, container, 4);
            
            pages = new List<Page>()
            {

                new(this, null, "WawaButtonPage", 0),
                new(this, null, "GameDetailPage", 1),
                new (this, null, "WawaSlugcatPage", 2),
                new BuffProgressionPage(this, null, 3),
                testNotification,
            };
           
            for (int i = 0; i < slugNameOrders.Count; i++)
            {
                slugcatPages.Add(new SlugcatIllustrationPage(this, null, i + 1, slugNameOrders[i]));
                pages.Add(slugcatPages[i]);
            }

            InitButtonPage(pages[0]);
            container.AddChild(menuSlot.Container);
            InitGameDetailPage(pages[0]);
            continueDetailPage.SetShow(false);
            newGameDetailPage.SetShow(false);

            extraInfoPage = new BuffExtraInfoPage(this, null, "ExtraInfoPage", pages.Count);
            pages.Add(extraInfoPage);

            foreach (var page in pages)
                page.mouseCursor?.BumToFront();

            UpdateSlugcatAndPage();


            FSprite black = new FSprite("pixel") { 
                color = Color.black, 
                scaleX = Custom.rainWorld.options.ScreenSize.x,
                scaleY = Custom.rainWorld.options.ScreenSize.y,
                anchorX = 0f,
                anchorY = 0f,
                x = 0f,
                y = 0f
            };

            container.AddChild(black);
            blackFadeAnim = AnimMachine.GetTickAnimCmpnt(0, 20, autoDestroy: true).BindModifier(Helper.EaseInOutCubic).BindActions(OnAnimUpdate: (anim) =>
            {
                black.alpha = 1f - anim.Get();
                pages[3].Container.alpha = 0f;
                Update();
                GrafUpdate(1f);
            },OnAnimFinished:(anim) =>
            {
                black.RemoveFromContainer();
                pages[3].Container.alpha = 1f;
                blackFadeAnim = null;
            });
        }
        TickAnimCmpnt blackFadeAnim;
        //


        BuffContinueGameDetialPage continueDetailPage;
        BuffNewGameDetailPage newGameDetailPage;
        //--------------测试-----------------
        BuffNewGameMissionPage missionPage;
        ModeSelectPage modeSelectPage;

        void InitGameDetailPage(Page page)
        {
            page.subObjects.Add(continueDetailPage = new BuffContinueGameDetialPage(this, page, Vector2.zero));
            page.subObjects.Add(newGameDetailPage = new BuffNewGameDetailPage(this, page, Vector2.zero));
            //--------------测试-----------------
            page.subObjects.Add(modeSelectPage = new ModeSelectPage(this, page, Vector2.zero));
            page.subObjects.Add(missionPage = new BuffNewGameMissionPage(this, page, Vector2.zero));
            pages[3].Container.MoveToFront();

        }


        //菜单元素
        HoldButton startButton;
        SimpleButton backButton;
        BigArrowButton prevButton;
        BigArrowButton nextButton;
        //SimpleButton settingButton;
        //CheckBox restartCheckbox;
        SimpleButton jollyToggleConfigMenu;
        SimpleButton progressionMenu;
        SimpleButton manualButton;

        public bool lastPausedButtonClicked;
        void InitButtonPage(Page page)
        {
            page.subObjects.Add(startButton = new HoldButton(this, page, Translate(SlugcatStats.getSlugcatName(CurrentName)), "START", new Vector2(683f, 85f), 40f));
            page.subObjects.Add(backButton = new SimpleButton(this, page, base.Translate("BACK"), "BACK", new Vector2(200f, 668f), new Vector2(110f, 30f)));
            page.subObjects.Add(prevButton = new BigArrowButton(this, page, "PREV", new Vector2(200f, 50f), -1));
            page.subObjects.Add(nextButton = new BigArrowButton(this, page, "NEXT", new Vector2(1116f, 50f), 1));

            float y = manager.rainWorld.screenSize.y - 100f;
            if (ModManager.JollyCoop)
            {
                page.subObjects.Add(jollyToggleConfigMenu = new SimpleButton(this, page, Translate("SHOW"), "JOLLY_TOGGLE_CONFIG",
                    new Vector2(1056f, y), new Vector2(110f, 30f)));
                y -= 40f;
            }
            page.subObjects.Add(progressionMenu = new SimpleButton(this, page, BuffResourceString.Get("ProgressionMenu_Show"), "PROGRESSIONMENU_SHOW",
                    new Vector2(1056f, y), new Vector2(110f, 30f)));
            y -= 40f;

            page.subObjects.Add(manualButton = new SimpleButton(this, page, base.Translate("MANUAL"), "MANUAL", new Vector2(1056f, y), new Vector2(110f, 30f)));


            page.subObjects.Add(testLabel = new MenuLabel(this, page, "", new Vector2(manager.rainWorld.screenSize.x / 2 - 250, 484 - 249f - 80f), new Vector2(500, 50), true));
            testLabel.label.alignment = FLabelAlignment.Center;
            testLabel.label.color = MenuColor(MenuColors.White).rgb;

            //float restartTextWidth = SlugcatSelectMenu.GetRestartTextWidth(CurrLang);
            //float restartTextOffset = SlugcatSelectMenu.GetRestartTextOffset(CurrLang);

            //page.subObjects.Add(restartCheckbox = new CheckBox(this, page, this, new Vector2(this.startButton.pos.x + 200f + restartTextOffset, Mathf.Max(30f, manager.rainWorld.options.SafeScreenOffset.y)), restartTextWidth, base.Translate("Restart game"), "RESTART", false));
            //restartCheckbox.label.pos.x += (restartTextWidth - restartCheckbox.label.label.textRect.width - 5f);
            SetSelectables();
        }

        void SetSelectables()
        {
            startButton.nextSelectable[0] = prevButton;
            startButton.nextSelectable[1] = backButton;
            startButton.nextSelectable[2] = nextButton;
            
            prevButton.nextSelectable[1] = backButton;
            prevButton.nextSelectable[2] = startButton;
            
            nextButton.nextSelectable[0] = startButton;
            nextButton.nextSelectable[1] = manualButton;
            
            manualButton.nextSelectable[0] = backButton;
            manualButton.nextSelectable[1] = progressionMenu;
            manualButton.nextSelectable[3] = nextButton;
            
            progressionMenu.nextSelectable[0] = backButton;
            progressionMenu.nextSelectable[1] = jollyToggleConfigMenu;
            progressionMenu.nextSelectable[3] = manualButton;
            
            backButton.nextSelectable[2] = jollyToggleConfigMenu ?? progressionMenu;
            backButton.nextSelectable[3] = prevButton;
            
            if (jollyToggleConfigMenu != null)
            {
                jollyToggleConfigMenu.nextSelectable[0] = backButton;
                jollyToggleConfigMenu.nextSelectable[3] = progressionMenu;
                Helper.LinkEmptyToSelf(jollyToggleConfigMenu);
            }
            selectedObject = startButton;

            Helper.LinkEmptyToSelf(startButton);
            Helper.LinkEmptyToSelf(prevButton);
            Helper.LinkEmptyToSelf(nextButton);
            Helper.LinkEmptyToSelf(manualButton);
            Helper.LinkEmptyToSelf(progressionMenu);
            Helper.LinkEmptyToSelf(backButton);
        }

        public void SetButtonsActive(bool active)
        {
            startButton.buttonBehav.greyedOut = !active;
            backButton.buttonBehav.greyedOut = !active;
            prevButton.buttonBehav.greyedOut = !active;
            nextButton.buttonBehav.greyedOut = !active;
            if(jollyToggleConfigMenu != null) jollyToggleConfigMenu.buttonBehav.greyedOut = !active;
            //settingButton.buttonBehav.greyedOut = !active;
        }

        void SetupSlugNameOrders()
        {
            var select = Helper.GetUninit<SlugcatSelectMenu>();
            select.manager = this.manager;
            select.SetSlugcatColorOrder();
            RemoveCantUse(ref select.slugcatColorOrder);
            slugNameOrders = select.slugcatColorOrder;
        }

        private void RemoveCantUse(ref List<SlugcatStats.Name> list)
        {
            list.Remove(new SlugcatStats.Name("delicious"));
            list.Remove(new SlugcatStats.Name("nightowl"));
            if(!list.Contains(MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel))
                list.Add(MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel);

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
                var re = saveGameData[CurrentName];
                if (re != null)
                {
                    testLabel.label.text = string.Format(BuffResourceString.Get("BuffGameMenu_LoadGame"),
                        re.shelterName, re.cycle, BuffDataManager.Instance.GetAllBuffIds(CurrentName).Count);
                }
                else
                {
                    testLabel.label.text =  string.Format(BuffResourceString.Get("BuffGameMenu_LoadGameUK"), BuffDataManager.Instance.GetAllBuffIds(CurrentName).Count);
                }
            }
            else
            {
                testLabel.label.text = BuffResourceString.Get("BuffGameMenu_NewGame");
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
            if (message == "BACK")
            {
                manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                PlaySound(SoundID.MENU_Switch_Page_Out);
            }
            else if (message == "PREV")
            {
                //quedSideInput = Math.Max(-3, quedSideInput - 1);
                targetScrolledPageIndex--;
                modeSelectPage.SetShow(false);
                PlaySound(SoundID.MENU_Next_Slugcat);
                if (ModManager.CoopAvailable)
                    manager.rainWorld.options.jollyPlayerOptionsArray[0].playerClass = slugNameOrders[(currentPageIndex - 1 + slugNameOrders.Count) % slugNameOrders.Count];
                //UpdateSlugcat();
            }
            else if (message == "NEXT")
            {
                //quedSideInput = Math.Min(3, quedSideInput + 1);
                targetScrolledPageIndex++;
                modeSelectPage.SetShow(false);

                PlaySound(SoundID.MENU_Next_Slugcat);
                if (ModManager.CoopAvailable)
                    manager.rainWorld.options.jollyPlayerOptionsArray[0].playerClass = slugNameOrders[(currentPageIndex+1) % slugNameOrders.Count];
                //UpdateSlugcat();
            }
            else if (message == "START")
            {
                PlaySound(SoundID.MENU_Start_New_Game);
                if (manager.rainWorld.progression.IsThereASavedGame(CurrentName))
                {
                    manager.rainWorld.progression.currentSaveState = null;
                    manager.arenaSitting = null;
                    manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat =
                        CurrentName;
                    manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
                    BuffDataManager.Instance.EnterGameFromMenu(CurrentName);

                    if (ModManager.CoopAvailable)
                    {
                        for (int i = 1; i < manager.rainWorld.options.JollyPlayerCount; i++)
                            manager.rainWorld.RequestPlayerSignIn(i, null);

                        for (int j = manager.rainWorld.options.JollyPlayerCount; j < 4; j++)
                            manager.rainWorld.DeactivatePlayer(j);
                    }

                    manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
                    PlaySound(SoundID.MENU_Start_New_Game);
                }
                else
                {
                    //--------------测试-----------------
                    modeSelectPage.SetShow(true);

                    //continueDetailPage.SetShow(false);
                    //newGameDetailPage.SetShow(true);
                }
            }
            else if (message == "CONTINUE_DETAIL_RESTART")
            {
                manager.rainWorld.progression.currentSaveState = null;
                manager.arenaSitting = null;
                manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat =
                    CurrentName;
                manager.rainWorld.progression.WipeSaveState(CurrentName);
                manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;

                continueDetailPage.ChangeSlugcat(CurrentName);
                menuSlot.SetupBuffs(slugNameOrders);
                UpdateSlugcatAndPage();
            }
            else if (message == "NEWGAME_DETAIL_NEWGAME")
            {
                manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat =
                        CurrentName;

                var setting = BuffDataManager.Instance.GetGameSetting(CurrentName);
                manager.rainWorld.progression.WipeSaveState(CurrentName);
                manager.arenaSitting = null;

                BuffDataManager.Instance.SetGameSetting(CurrentName, setting);
                manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
                if (ModManager.CoopAvailable)
                {
                    for (int i = 1; i < manager.rainWorld.options.JollyPlayerCount; i++)
                        manager.rainWorld.RequestPlayerSignIn(i, null);
                    
                    for (int j = manager.rainWorld.options.JollyPlayerCount; j < 4; j++)
                        manager.rainWorld.DeactivatePlayer(j);
                }
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
            else if (message == "MANUAL")
            {
                ManualDialog dialog = new BuffGameManual(manager, BuffGameManual.topicKeys);
                PlaySound(SoundID.MENU_Player_Join_Game);
                manager.ShowDialog(dialog);
            }
            //--------------测试-----------------
            else if (message == "DEFAULTMODE") 
            {
                modeSelectPage.SetShow(false);
                continueDetailPage.SetShow(false);
                newGameDetailPage.SetShow(true);
            }
            else if (message == "MISSIONMODE")
            {
                modeSelectPage.SetShow(false);
                continueDetailPage.SetShow(false);
                missionPage.SetShow(true);
            }
            else if(message == "PROGRESSIONMENU_SHOW")
            {
                (pages[3] as BuffProgressionPage).ShowProgressionPage();
            }
            else if(message == "EXTRAINFOPAGE_SHOW")
            {
                ShowExtraInfo();
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
            InputAgency.StaticUpdate();

            menuSlot.Update();

            lastScroll = scroll;
            //testLabel.text = $"\ntarget:{targetScrolledPageIndex} scrolledPageIndex : {scrolledPageIndex}\nintScrolledPageIndex : {intScrolledPageIndex}\nscroll:{scroll}";
            testNotification.Update();
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
            
            foreach (var item in flagNeedUpdate)
            {
                if (item)
                {
                    flag.Update();
                    break;
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
            testNotification.GrafUpdate(timeStacker);

            if(Input.GetKeyDown(KeyCode.N) && BuffPlugin.DevEnabled)
            {
                TestFunction();
            }
            
            if (RWInput.CheckPauseButton(0) && manager.nextSlideshow == null && !lastPausedButtonClicked)
            {
                EscLogic();
            }
            lastPausedButtonClicked = RWInput.CheckPauseButton(0);
        }

        public void ShowExtraInfo()
        {
            var gameSetting = (missionPage.Show && BuffNewGameMissionPage.pickedMission != null)
                ? BuffNewGameMissionPage.pickedMission.GameSetting
                : BuffDataManager.Instance.GetGameSetting(CurrentName);
         
            
            var settingDescription = gameSetting.Description;
            if (string.IsNullOrEmpty(settingDescription))
                return;

            string[] entries = Regex.Split(settingDescription, "<ENTRY>");
            for(int i = 0;i < entries.Length - 1; i++)
            {
                if (string.IsNullOrEmpty(entries[i]))
                    continue;

                Color extraColor = Color.white;
                if (entries[i].StartsWith("<Color="))
                {
                    entries[i] = Regex.Replace(entries[i], "<Color=", "");
                    int firstAngleBracketIndex = entries[i].IndexOf('>');
                    string hexVal = entries[i].Substring(0, firstAngleBracketIndex);
                    extraColor = Custom.hexToColor(hexVal);
                    entries[i] = entries[i].Substring(firstAngleBracketIndex + 1, entries[i].Length - firstAngleBracketIndex - 1);
                }


                if (entries[i].Contains("<GachaPositive>"))
                {
                    extraInfoPage.AppendGachaInfo(Regex.Replace(entries[i], "<GachaPositive>", ""), true);
                    //BuffPlugin.Log($"Append gacha info positive");
                }
                else if (entries[i].Contains("<GachaNonPositive>"))
                {
                    extraInfoPage.AppendGachaInfo(Regex.Replace(entries[i], "<GachaNonPositive>", ""), false);
                    //BuffPlugin.Log($"Append gacha info nonpositive");
                }
                else
                    extraInfoPage.AppendInfoEntry(entries[i],color: extraColor);
            }
            extraInfoPage.SetMainInfo(entries.Last());
            extraInfoPage.SetShow(true);
        }

        public void TestFunction()
        {
            manager.RequestMainProcessSwitch(BuffEnums.ProcessID.CreditID);
        }

        public void EscLogic()
        {
            //BuffPlugin.Log($"{currentPage == 3}, {modeSelectPage.Show}, {missionPage.Show}, {newGameDetailPage.Show}");
            string info = "";
            if(currentPage == 3)//progressionPage
            {
                info = "exit progression";
                (pages[3] as BuffProgressionPage).EscLogic();
            }
            else if (modeSelectPage.Show)
            {
                info = "exit modeSelect";
                modeSelectPage.SetShow(false);
            }
            else if (missionPage.Show)
            {
                info = "exit mission";
                missionPage.EscLogic();
            }
            else if (newGameDetailPage.Show)
            {
                info = "exit newGame";
                newGameDetailPage.EscLogic();
            }
            else
            {
                info = "exit";
                manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
            }

            //BuffPlugin.Log(info);
            PlaySound(SoundID.MENU_Switch_Page_Out);
        }

        public void ResetSelectables()
        {
            selectedObject = startButton;
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
