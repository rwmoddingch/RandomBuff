using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.UI;
using RandomBuff.Render.UI.Component;
using RandomBuff.Render.UI.ExceptionTracker;
using RWCustom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RandomBuff.Core.BuffMenu
{
    public static class BuffGameMenuStatics
    {
        public static int DefaultConditionNum = 2;
        public static int MaxShowSwitchCounter = 20;
        public static Vector2 HidePos = new Vector2(0f, -1000f);
        public static Slider.SliderID DifficultySlider = new Slider.SliderID("BuffGameDetial_DifficultySlider", true);

        public static Color BuffGreen = Color.green * 0.8f + Color.blue * 0.2f;
    }


    public class SlugcatIllustrationPage : SlugcatSelectMenu.SlugcatPage
    {
        public SlugcatIllustrationPage(Menu.Menu menu, MenuObject menuObject, int pageIndex, SlugcatStats.Name name) : base(menu, menuObject, pageIndex, name)
        {
            var origMenu = menu;
            var selectMenu = Helper.GetUninit<SlugcatSelectMenu>();
            selectMenu.saveGameData = new();

            this.menu = selectMenu;
            this.menu.manager = origMenu.manager;
            this.menu.container = origMenu.container;
            selectMenu.manager.rainWorld.progression.miscProgressionData.redUnlocked = true;
            AddImage(false);
            this.menu = origMenu;
            slugcatImage.menu = origMenu;

        }

        public new float Scroll(float timeStacker)
        {
            float scroll = (SlugcatPageIndex - (menu as BuffGameMenu).currentPageIndex) - Mathf.Lerp((menu as BuffGameMenu).lastScroll, (menu as BuffGameMenu).scroll, timeStacker);
            if (scroll < MinOffset)
            {
                scroll += (menu as BuffGameMenu).slugcatPages.Count;
            }
            else if (scroll > MaxOffset)
            {
                scroll -= (menu as BuffGameMenu).slugcatPages.Count;
            }
            return scroll;
        }

        public new float NextScroll(float timeStacker)
        {
            float scroll = (SlugcatPageIndex - (menu as BuffGameMenu).currentPageIndex) - Mathf.Lerp((menu as BuffGameMenu).scroll, (menu as BuffGameMenu).NextScroll, timeStacker);
            if (scroll < MinOffset)
            {
                scroll += (menu as BuffGameMenu).slugcatPages.Count;
            }
            else if (scroll > MaxOffset)
            {
                scroll -= (menu as BuffGameMenu).slugcatPages.Count;
            }
            return scroll;
        }
    }

    internal class BuffContinueGameDetialPage : PositionedMenuObject
    {
        static int MaxShowSwitchCounter = 20;
        static Vector2 HidePos = new Vector2(0f, -1000f);

        static FieldInfo OnPressDownField;
        BuffGameMenu gameMenu;
        GameSetting currentGameSetting;

        //菜单元素
        FSprite dark;


        OpHoldButton restartButton;
        List<MenuLabel> conditionLabels = new();
        List<BigSimpleButton> conditionRects = new();

        //状态变量
        int _showCounter = -1;
        int _targetShowCounter;
        public bool Show
        {
            get => _targetShowCounter == MaxShowSwitchCounter;
            private set => _targetShowCounter = (value ? MaxShowSwitchCounter : 0);
        }
        float ShowFactor => (float)_showCounter / MaxShowSwitchCounter;
        Action showToggleFinishCallBack;

        public BuffContinueGameDetialPage(BuffGameMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            gameMenu = menu;
            myContainer = new FContainer();

            owner.Container.AddChild(myContainer);
            lastPos = pos = Vector2.Lerp(BuffGameMenuStatics.HidePos, Vector2.zero, Helper.LerpEase(ShowFactor));
            //Container.AddChild(dark = new FSprite("pixel") { color = Color.black, alpha = 0f, scaleX = Custom.rainWorld.screenSize.x, scaleY = Custom.rainWorld.screenSize.y, x = Custom.rainWorld.screenSize.x / 2f, y = Custom.rainWorld.screenSize.y / 2f });
            SetupContinueGamePageItems();
        }

        public void SetupContinueGamePageItems()
        {
            currentGameSetting = BuffDataManager.Instance.GetGameSetting(gameMenu.CurrentName);

            Vector2 testPos = new Vector2(683f, 85f) + new Vector2(SlugcatSelectMenu.GetRestartTextOffset(gameMenu.CurrLang), 80f);
            var tabWrapper = new MenuTabWrapper(gameMenu, this);
            subObjects.Add(tabWrapper);
            restartButton = new OpHoldButton(new Vector2(683f + 240f - 120f, Mathf.Max(30, Custom.rainWorld.options.SafeScreenOffset.y)), new Vector2(120, 40), BuffResourceString.Get("BuffContinueGamePage_Restart"), 100f);
            restartButton.colorEdge = Color.red;
            restartButton.description = " ";

            if (OnPressDownField == null)
                OnPressDownField = typeof(OpHoldButton).GetField("OnPressDone", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var onPressDown = OnPressDownField.GetValue(restartButton) as OnSignalHandler;
            onPressDown += RestartButton_OnPressDone;
            OnPressDownField.SetValue(restartButton, onPressDown);
            var wrapper = new UIelementWrapper(tabWrapper, restartButton);

            RefreshMenuItemsForSlugcat();
        }

        public void ChangeSlugcat(SlugcatStats.Name name)
        {
            currentGameSetting = BuffDataManager.Instance.GetGameSetting(name);

            SetShow(menu.manager.rainWorld.progression.IsThereASavedGame(name));
            if (Show)
            {
                RefreshMenuItemsForSlugcat();
                SetShow(true);
            }
        }

        void RefreshMenuItemsForSlugcat()
        {
            foreach (var conditionLabel in conditionLabels)
            {
                RemoveSubObject(conditionLabel);
                conditionLabel.RemoveSprites();
            }
            conditionLabels.Clear();

            foreach (var conditionRect in conditionRects)
            {
                RemoveSubObject(conditionRect);
                conditionRect.RemoveSprites();
            }
            conditionRects.Clear();

            if (currentGameSetting == null)
                return;

            for (int i = 0; i < currentGameSetting.conditions.Count; i++)
            {
                string text = currentGameSetting.conditions[i].DisplayName(gameMenu.manager.rainWorld.inGameTranslator) + " " + currentGameSetting.conditions[i].DisplayProgress(gameMenu.manager.rainWorld.inGameTranslator);
                float num = 40f * (float)i;
                var rect = new BigSimpleButton(menu, this, text, "CONTINUE_DETAIL_CONDITION_" + i.ToString(), new Vector2(390f, 350f - num), new Vector2(600f, 30f), FLabelAlignment.Left, true);
                if (currentGameSetting.conditions[i].Finished)
                {
                    var green = new HSLColor(Custom.RGB2HSL(Color.green).x, Custom.RGB2HSL(Color.green).y, Custom.RGB2HSL(Color.green).z);
                    rect.rectColor = green;
                    rect.labelColor = green;
                }
                subObjects.Add(rect);
                conditionRects.Add(rect);
            }
        }

        public void SetShow(bool show)
        {
            Show = show;
        }

        public override void Update()
        {
            base.Update();
            if (_showCounter != _targetShowCounter)
            {
                if (_showCounter < _targetShowCounter)
                    _showCounter++;
                else if (_showCounter > _targetShowCounter)
                    _showCounter--;

                pos = Vector2.Lerp(HidePos, Vector2.zero, Helper.LerpEase(ShowFactor));
                gameMenu.menuSlot.basePos = pos;
                //dark.alpha = ShowFactor * 0.5f;
            }
            else if (showToggleFinishCallBack != null)
            {
                showToggleFinishCallBack();
                showToggleFinishCallBack = null;
            }

           
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
        }

        private void RestartButton_OnPressDone(UIfocusable trigger)
        {
            gameMenu.PlaySound(SoundID.MENU_Start_New_Game);
            Singal(null, "CONTINUE_DETAIL_RESTART");
        }

        public void EscLogic()
        {

        }
    }

    internal class BuffNewGameDetailPage : PositionedMenuObject
    {
        BuffGameMenu gameMenu;
        FNodeWrapper nodeWrapper;

        //菜单组件
        FSprite dark;

        SimpleButton settingButton;
        SimpleButton backButton;
        HoldButton startGameButton;
        //HorizontalSlider difficultySlider;

        BigSimpleButton[] conditionButtons;
        SymbolButton[] hiddenToggles;

        SymbolButton randomButton;
        SymbolButton minusButton;
        SymbolButton plusButton;

        SimpleButton jollyToggleConfigMenu;

        ConditionInstance[] conditionInstances = new ConditionInstance[5];
        GameSetting currentGameSetting;

        
        RandomBuffFlagRenderer flagRenderer;

        //BuffLevelBarDynamic buffLevelBarDynamic;

        CardTitle cardTitle;
        Vector2 titlePos;

        Vector2 flagHangPos;
        Vector2 flagHidePos;

        //状态变量
        int _showCounter = -1;
        int _targetShowCounter;
        int flagControlIndex;
        public bool Show
        {
            get => _targetShowCounter == BuffGameMenuStatics.MaxShowSwitchCounter;
            private set => _targetShowCounter = (value ? BuffGameMenuStatics.MaxShowSwitchCounter : 0);
        }
        float ShowFactor => (float)_showCounter / BuffGameMenuStatics.MaxShowSwitchCounter;

        int activeConditionCount;
        bool canAddAnyCondition;

        public BuffNewGameDetailPage(BuffGameMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            gameMenu = menu as BuffGameMenu;
            myContainer = new FContainer();
            owner.Container.AddChild(myContainer);
            lastPos = pos = Vector2.Lerp(BuffGameMenuStatics.HidePos, Vector2.zero, Helper.LerpEase(ShowFactor));

            Container.AddChild(dark = new FSprite("pixel") { color = Color.black, alpha = 0f, scaleX = Custom.rainWorld.screenSize.x, scaleY = Custom.rainWorld.screenSize.y, x = Custom.rainWorld.screenSize.x / 2f, y = Custom.rainWorld.screenSize.y / 2f });

            flagControlIndex = gameMenu.flagNeedUpdate.Count;
            gameMenu.flagNeedUpdate.Add(false);

            flagRenderer = new RandomBuffFlagRenderer(menu.flag, RandomBuffFlagRenderer.FlagType.Square, RandomBuffFlagRenderer.FlagColorType.Grey);
            flagHangPos = new Vector2(Custom.rainWorld.screenSize.x / 2f - menu.flag.rect.x / 2f, 850f);
            flagHidePos = flagHangPos + Vector2.up * 800f;
            flagRenderer.pos = flagHidePos;
            Container.AddChild(flagRenderer.container);

            //buffLevelBarDynamic = new BuffLevelBarDynamic(Container, new Vector2(200f, 200f), 300f, 1238, 1000);
            //buffLevelBarDynamic.HardSet();
            //buffLevelBarDynamic.setAlpha = 1f;

            SetupNewGamePageItems();
        }

        public void SetupNewGamePageItems()
        {
            var gameSetting = BuffDataManager.Instance.GetGameSetting(gameMenu.CurrentName);

            Vector2 testPos = new Vector2(683f, 85f) + new Vector2(SlugcatSelectMenu.GetRestartTextOffset(gameMenu.CurrLang), 80f);
            subObjects.Add(backButton = new SimpleButton(gameMenu, this, gameMenu.Translate("BACK"), "NEWGAME_DETAIL_BACK", new Vector2(200f, 698f), new Vector2(110f, 30f)));
            subObjects.Add(startGameButton = new HoldButton(gameMenu, this, BuffResourceString.Get("BuffGameMenu_NewGame"), "NEWGAME_DETAIL_NEWGAME", new Vector2(683f, 85f), 40f));
            subObjects.Add(settingButton = new SimpleButton(gameMenu, this, (BuffDataManager.Instance.GetGameSetting(gameMenu.CurrentName).TemplateName),
                "NEWGAME_DETAIL_SELECT_MODE", new Vector2(683f - 240f, Mathf.Max(30, Custom.rainWorld.options.SafeScreenOffset.y)),
                new Vector2(120, 40)));
            subObjects.Add(new SimpleImageButton(gameMenu, this, new Vector2(Custom.rainWorld.options.ScreenSize.x - 80f, 40f), new Vector2(40f, 40f), BuffUIAssets.CardInfo20, "EXTRAINFOPAGE_SHOW"));
            settingButton.menuLabel.text = gameMenu.Translate(gameSetting.TemplateName);

            subObjects.Add(nodeWrapper = new FNodeWrapper(gameMenu, this));

            //难度滑条
            //subObjects.Add(difficultySlider = new HorizontalSlider(gameMenu, this, "", new Vector2(470f, 600f), new Vector2(400f, 0f), BuffGameMenuStatics.DifficultySlider, false));

            //var easySprite = new FSprite("SaintA", true);
            //easySprite.color = new Color(0.2f, 0.75f, 0.2f);
            //nodeWrapper.WrapNode(easySprite, new Vector2(680f - 250f, 620f));

            //var hardSprite = new FSprite("OutlawA", true);
            //hardSprite.color = new Color(0.75f, 0.2f, 0.2f);
            //nodeWrapper.WrapNode(hardSprite, new Vector2(680f + 250f, 620f));

            //条件选择按钮
            conditionButtons = new BigSimpleButton[5];
            hiddenToggles = new SymbolButton[5];
            for (int i = 0; i < conditionButtons.Length; i++)
            {
                float num = 50f * (float)i;
                conditionButtons[i] = new BigSimpleButton(menu, this, "Condition " + i.ToString(), "NEWGAME_DETAIL_CONDITION_" + i.ToString(), new Vector2(360f, 510f - num), new Vector2(600f, 40f), FLabelAlignment.Left, true);
                subObjects.Add(conditionButtons[i]);
                hiddenToggles[i] = new SymbolButton(menu, this, "hiddenopen", "NEWGAME_DETAIL_HIDDEN_" + i.ToString(), new Vector2(970f, 510f - num));
                hiddenToggles[i].size = new Vector2(40f, 40f);
                hiddenToggles[i].roundedRect.size = hiddenToggles[i].size;
                subObjects.Add(hiddenToggles[i]);
            }

            randomButton = new SymbolButton(menu, this, "Sandbox_Randomize", "NEWGAME_DETAIL_RANDOM", new Vector2(430f, 250f));
            randomButton.size = new Vector2(40f, 40f);
            randomButton.roundedRect.size = randomButton.size;
            subObjects.Add(randomButton);
            minusButton = new SymbolButton(menu, this, "minus", "NEWGAME_DETAIL_MINUS", new Vector2(900f, 250f));
            minusButton.size = new Vector2(40f, 40f);
            minusButton.roundedRect.size = minusButton.size;
            subObjects.Add(minusButton);
            plusButton = new SymbolButton(menu, this, "plus", "NEWGAME_DETAIL_PLUS", new Vector2(950f, 250f));
            plusButton.size = new Vector2(40f, 40f);
            plusButton.roundedRect.size = plusButton.size;
            subObjects.Add(plusButton);

            if (ModManager.JollyCoop)
                page.subObjects.Add(jollyToggleConfigMenu = new SimpleButton(gameMenu, this, gameMenu.Translate("SHOW"), "JOLLY_TOGGLE_CONFIG",
                    new Vector2(1056f, gameMenu.manager.rainWorld.screenSize.y - 100f), new Vector2(110f, 30f)));


            Vector2 screenCenter = menu.manager.rainWorld.options.ScreenSize / 2f;
            titlePos = new Vector2(screenCenter.x, screenCenter.y + 300f);
            cardTitle = new CardTitle(Container, BuffCard.normalScale * 0.3f, titlePos + pos);
            cardTitle.RequestSwitchTitle(BuffResourceString.Get(gameSetting.TemplateName));

            Container.MoveToFront();
        }

        public void SetShow(bool show)
        {
            Show = show;
            gameMenu.SetButtonsActive(!show);
            currentGameSetting = BuffDataManager.Instance.GetGameSetting(gameMenu.CurrentName);
            if (show)
            {
                InitConditions();
                settingButton.menuLabel.text = BuffResourceString.Get(currentGameSetting.TemplateName);
            }
            else
                QuitToSelectSlug();
            flagRenderer.Show = show;
            cardTitle.RequestSwitchTitle(show ? BuffResourceString.Get(BuffDataManager.Instance.GetGameSetting(gameMenu.CurrentName).TemplateName) : "");
        }

        public override void Update()
        {
            base.Update();
            //buffLevelBarDynamic.Update();
            cardTitle.Update();
            if (_showCounter != _targetShowCounter)
            {
                if (_showCounter < _targetShowCounter)
                    _showCounter++;
                else if (_showCounter > _targetShowCounter)
                    _showCounter--;

                pos = Vector2.Lerp(BuffGameMenuStatics.HidePos, Vector2.zero, Helper.LerpEase(ShowFactor));
                //buffLevelBarDynamic.pos = pos + new Vector2(200f, 200f);
                cardTitle.pos = pos + titlePos;

                dark.alpha = ShowFactor;
                flagRenderer.pos = Vector2.Lerp(flagHidePos, flagHangPos, Helper.LerpEase(ShowFactor));
            }

            bool needUpdate = Show || flagRenderer.NeedRenderUpdate;
            gameMenu.flagNeedUpdate[flagControlIndex] = needUpdate;

            if (Show || flagRenderer.NeedRenderUpdate)
            {
                flagRenderer.Update();
            }

        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            //buffLevelBarDynamic.GrafUpdate(timeStacker);
            cardTitle.GrafUpdate(timeStacker);
            if (Show || flagRenderer.NeedRenderUpdate)
            {
                flagRenderer.GrafUpdate(timeStacker);
            }

            if (Show && RWInput.CheckPauseButton(0))
            {
                SetShow(false);
                menu.PlaySound(SoundID.MENU_Switch_Page_Out);
                (menu as BuffGameMenu)!.lastPausedButtonClicked = true;

            }
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "NEWGAME_DETAIL_SELECT_MODE")
            {
                var settingButton = sender as SimpleButton;
                var gameSetting = BuffDataManager.Instance.GetGameSetting(gameMenu.CurrentName);
                var list = BuffConfigManager.GetTemplateNameList();
                var index = list.IndexOf(gameSetting.TemplateName) + 1;
                gameSetting.LoadTemplate(list[index == list.Count ? 0 : index]);

                settingButton!.menuLabel.text = BuffResourceString.Get(gameSetting.TemplateName);
                cardTitle.RequestSwitchTitle(BuffResourceString.Get(gameSetting.TemplateName));
                ClearCurrentConditions();
                InitConditions();
            }
            else if (message == "NEWGAME_DETAIL_BACK")
            {
                SetShow(false);
                menu.PlaySound(SoundID.MENU_Switch_Page_Out);
            }
            else if (message.StartsWith("NEWGAME_DETAIL_CONDITION_"))
            {
                int index = int.Parse(message.Split('_').Last());
                RandomPickConditionAt(index);
            }
            else if (message.StartsWith("NEWGAME_DETAIL_HIDDEN_"))
            {
                int index = int.Parse(message.Split('_').Last());
                RandomPickConditionAt(index, true);
            }
            else if (message == "NEWGAME_DETAIL_MINUS")
            {
                UpdateActiveConditionCount(false);
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            }
            else if (message == "NEWGAME_DETAIL_PLUS")
            {
                UpdateActiveConditionCount(true);
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            }
        }

        void RefreshConditionButtonState()
        {
            for (int i = 0; i < conditionButtons.Length; i++)
            {
                if (i < activeConditionCount)
                {
                    hiddenToggles[i].buttonBehav.greyedOut = false;
                    conditionButtons[i].buttonBehav.greyedOut = false;
                    conditionButtons[i].menuLabel.text = conditionInstances[i].GetDisplayText();
                    hiddenToggles[i].symbolSprite.SetElementByName(conditionInstances[i].hide ? "hiddenclose" : "hiddenopen");
                }
                else
                {
                    hiddenToggles[i].buttonBehav.greyedOut = true;
                    hiddenToggles[i].symbolSprite.SetElementByName("hiddenopen");
                    conditionButtons[i].buttonBehav.greyedOut = true;
                    conditionButtons[i].menuLabel.text = "NONE";
                }
            }
            plusButton.buttonBehav.greyedOut = !canAddAnyCondition || activeConditionCount == 5;
            minusButton.buttonBehav.greyedOut = activeConditionCount == 0;
        }

        void InitConditions()
        {
            currentGameSetting.ClearCondition();
            activeConditionCount = BuffGameMenuStatics.DefaultConditionNum;
            for (int i = 0; i < activeConditionCount; i++)
            {
                var result = currentGameSetting.GetRandomCondition();
                conditionInstances[i] = new ConditionInstance(result.condition, false);
                canAddAnyCondition = result.canGetMore;
                if (!result.canGetMore)
                {
                    activeConditionCount = i + 1;
                    break;
                }
            }
            RefreshConditionButtonState();
        }

        void RandomPickConditionAt(int index, bool toggleHide = false)
        {
            var loadedCondition = conditionInstances[index];

            currentGameSetting.RemoveCondition(loadedCondition.condition);
            loadedCondition.condition = currentGameSetting.GetRandomCondition().condition;
            if (toggleHide)
            {
                loadedCondition.hide = !loadedCondition.hide;
                BuffPlugin.Log($"{index} Toggle hide : {conditionInstances[index].hide}");

                if (loadedCondition.hide)
                    menu.PlaySound(SoundID.Slugcat_Ghost_Appear);
                else
                    menu.PlaySound(SoundID.Slugcat_Ghost_Dissappear);
            }
            menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            RefreshConditionButtonState();
        }

        void UpdateActiveConditionCount(bool add)
        {
            if (add)
            {
                if (activeConditionCount == 5)
                    return;
                activeConditionCount++;

                var result = currentGameSetting.GetRandomCondition();
                canAddAnyCondition = result.canGetMore;
                conditionInstances[activeConditionCount - 1] = new ConditionInstance(result.condition, false);
                BuffPlugin.Log($"Add new condition, {result}, {canAddAnyCondition}");
            }
            else
            {
                if (activeConditionCount == 0)
                    return;

                currentGameSetting.RemoveCondition(conditionInstances[activeConditionCount - 1].condition);
                BuffPlugin.Log($"Remove condition : {conditionInstances[activeConditionCount - 1].condition.ID}, setting has {currentGameSetting.conditions.Count} now");
                conditionInstances[activeConditionCount - 1] = null;
                activeConditionCount--;
                canAddAnyCondition = true;
            }
            RefreshConditionButtonState();
        }

        void ClearCurrentConditions()
        {
            activeConditionCount = 0;//清空选择的condition
            for (int i = 0; i < conditionInstances.Length; i++)
            {
                if (conditionInstances[i] == null)
                    break;
                currentGameSetting.RemoveCondition(conditionInstances[i].condition);
            }
        }

        void QuitToSelectSlug()
        {
            gameMenu.SetButtonsActive(true);

            ClearCurrentConditions();
        }

        public void EscLogic()
        {
            SetShow(false);
        }

        class ConditionInstance
        {
            public Condition condition;
            public bool hide;

            public ConditionInstance(Condition condition, bool hide)
            {
                this.condition = condition;
                this.hide = hide;
            }

            public string GetDisplayText()
            {
                if (hide)
                    return "HIDE";
                return condition.DisplayName(Custom.rainWorld.inGameTranslator);
            }
        }
    }

    internal class BuffNewGameMissionPage : PositionedMenuObject
    {
        BuffGameMenu gameMenu;
        GameSetting currentGameSetting;

        FSprite blackSprite;
        SimpleButton backButton;
        public Dictionary<string, string> signalToValue;
        public static Mission pickedMission;
        MissionInfoBox missionInfoBox;
        MissionSheetBox missionSheetBox;
        SimpleImageButton extraInfoButton;

        RandomBuffFlagRenderer flagRenderer;

        TickAnimCmpnt showAnim = AnimMachine.GetTickAnimCmpnt(0, 40, autoStart: false).AutoPause().BindModifier(Helper.EaseInOutCubic);

        int flagControlIndex;
        bool show;

        public bool Show => show;
        Vector2 flagHangPos;
        Vector2 flagHidePos;

        public BuffNewGameMissionPage(BuffGameMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            this.menu = menu;
            gameMenu = menu;

            flagControlIndex = gameMenu.flagNeedUpdate.Count;
            gameMenu.flagNeedUpdate.Add(false);

          

            signalToValue = new Dictionary<string, string>();
            InitMenuElements();
            missionInfoBox = new MissionInfoBox(menu, this, Vector2.zero);
            subObjects.Add(missionInfoBox);
            missionSheetBox = new MissionSheetBox(menu, this, Vector2.zero);
            subObjects.Add(missionSheetBox);


            SetShow(false);
        }

        public void InitMenuElements()
        {
            blackSprite = new FSprite("pixel")
            {
                scale = 1400,
                color = Color.black,
                x = 693f,
                y = 393f,
                alpha = 0f
            };
            Container.AddChild(blackSprite);

            flagRenderer = new RandomBuffFlagRenderer(gameMenu.flag, RandomBuffFlagRenderer.FlagType.OuterTriangle, RandomBuffFlagRenderer.FlagColorType.Silver);
            flagHangPos = new Vector2(Custom.rainWorld.screenSize.x / 2f - gameMenu.flag.rect.x / 2f, 820f);
            flagHidePos = flagHangPos + Vector2.up * 800f;
            flagRenderer.pos = flagHidePos;
            Container.AddChild(flagRenderer.container);

            backButton = new SimpleButton(menu, this, menu.Translate("BACK"), "NEWGAME_MISSION_BACK", new Vector2(1200f, 1400f), new Vector2(110f, 30f));
            subObjects.Add(backButton);

            subObjects.Add(extraInfoButton = new SimpleImageButton(gameMenu, this, new Vector2(Custom.rainWorld.options.ScreenSize.x - 80f, 1040f), new Vector2(40f, 40f), BuffUIAssets.CardInfo20, "EXTRAINFOPAGE_SHOW"));
        }

        public void SetShow(bool show)
        {
            this.show = show;
            flagRenderer.Show = show;
            if (show)
            {
                showAnim.SetTickAndStart(1);
                //blackSprite.alpha = 1f;
                //backButton.pos.y = 698f;
                pickedMission = null;
                currentGameSetting = BuffDataManager.Instance.GetGameSetting(gameMenu.CurrentName);
            }
            else
            {
                showAnim.SetTickAndStart(-1);
                //blackSprite.alpha = 0f;
                //backButton.pos.y = 1400f;

            }
            missionInfoBox.SetShow(show);
            missionSheetBox.SetShow(show);
            gameMenu.SetButtonsActive(!show);
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "NEWGAME_MISSION_BACK")
            {
                SetShow(false);
                missionInfoBox.interactionManager?.Destroy();
                menu.PlaySound(SoundID.MENU_Switch_Page_Out);
            }
            else if (message.StartsWith("MISSIONPICK_"))
            {
                var missionID = Regex.Split(message, "_")[1] ;
                foreach (var key in MissionRegister.GetAllUnlockedMissions())
                {
                    if (key.value == missionID)
                    {
                        MissionRegister.TryGetMission(key, out var mission);
                        string[] info = new string[5];
                        for (int i = 0; i < 5; i++)
                        {
                            if (mission.GameSetting.conditions.Count > i)
                            {
                                info[i] = mission.GameSetting.conditions[i].DisplayName(Custom.rainWorld.inGameTranslator);
                            }
                            else
                            {
                                info[i] = "None";
                            }
                        }
                        missionInfoBox.UpdateMissionInfo(mission.TextCol ,Custom.rainWorld.inGameTranslator.Translate(mission.MissionName), mission.BindSlug == null? "NOBINDSLUG" : mission.BindSlug.value,info, mission.startBuffSet.ToArray());
                        pickedMission = mission;
                        break;
                    }
                }
            }
            else if (message.StartsWith("MISSIONBUFF_"))
            {
                var buffID = Regex.Split(message, "_")[1];
                missionInfoBox.UpdateCardDisplay(true, buffID);
            }
            else if (message == "MISSION_QUITDISPLAY")
            {
                missionInfoBox.UpdateCardDisplay(false, null);
            }
            else if (message == "MISSIONFLIP_LEFT")
            {
                if (missionSheetBox.currentPage > 0)
                    missionSheetBox.currentPage--;
                missionSheetBox.FlipPage(false);
            }
            else if (message == "MISSIONFLIP_RIGHT")
            {
                if (missionSheetBox.currentPage < missionSheetBox.totalPages - 1)
                    missionSheetBox.currentPage++;
                missionSheetBox.FlipPage(true);
            }
            else if (message == "MISSION_START")
            {
                if (pickedMission == null) return;
             
                BuffHookWarpper.CheckAndDisableAllHook();
                gameMenu.manager.rainWorld.progression.WipeSaveState(gameMenu.CurrentName);
                BuffDataManager.Instance.SetGameSetting(gameMenu.CurrentName, currentGameSetting = pickedMission.GameSetting.Clone());
                //currentGameSetting.MissionId = pickedMission.ID.value;
                gameMenu.manager.rainWorld.progression.currentSaveState = null;
                menu.manager.arenaSitting = null;
                gameMenu.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat =
                        gameMenu.CurrentName; 
                gameMenu.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;

                if (ModManager.CoopAvailable)
                {
                    for (int i = 1; i < menu.manager.rainWorld.options.JollyPlayerCount; i++)
                        menu.manager.rainWorld.RequestPlayerSignIn(i, null);

                    for (int j = menu.manager.rainWorld.options.JollyPlayerCount; j < 4; j++)
                        menu.manager.rainWorld.DeactivatePlayer(j);
                }
                currentGameSetting.NewGame();

                for (int j = 0; j < pickedMission.startBuffSet.Count; j++)
                    BuffDataManager.Instance.GetOrCreateBuffData(pickedMission.startBuffSet[j], true);
                
                
                gameMenu.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
                gameMenu.PlaySound(SoundID.MENU_Start_New_Game);
            }
            
        }

        public override void Update()
        {
            base.Update();
            backButton.buttonBehav.greyedOut = MissionInfoBox.hasCardOnDisplay;
            backButton.pos = Vector2.Lerp(new Vector2(1200, 800), new Vector2(1200, 698), showAnim.Get());
            extraInfoButton.pos.y = Mathf.Lerp(1040f, 40f, showAnim.Get());

            bool needUpdate = show || flagRenderer.NeedRenderUpdate;
            gameMenu.flagNeedUpdate[flagControlIndex] = needUpdate;

            flagRenderer.pos = Vector2.Lerp(flagHidePos, flagHangPos, showAnim.Get());

            if (show || flagRenderer.NeedRenderUpdate)
            {
                flagRenderer.Update();
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            blackSprite.alpha = showAnim.Get();

            if(show || flagRenderer.NeedRenderUpdate)
            {
                flagRenderer.GrafUpdate(timeStacker);
            }

            //if (Show && RWInput.CheckPauseButton(0))
            //{
            //    SetShow(false);
            //    menu.PlaySound(SoundID.MENU_Switch_Page_Out);
            //    (menu as BuffGameMenu)!.lastPausedButtonClicked = true;
            //}
        }

        public void EscLogic()
        {
            if (MissionInfoBox.hasCardOnDisplay)
                missionInfoBox.UpdateCardDisplay(false, null);
            else
                Singal(null,"NEWGAME_MISSION_BACK");
        }

        public class MissionButton : SimpleButton
        {
            public Mission bindMission;
            public bool active;
            AnimateComponentBase animCmpnt;
            TickAnimCmpnt selfShow = AnimMachine.GetTickAnimCmpnt(0, 20, autoStart: false).AutoPause().BindModifier(Helper.EaseInOutCubic);

            Vector2 showPos;
            Vector2 hidePos;

            public MissionButton(Mission mission, Menu.Menu menu, MenuObject owner, string displayText, string signal, Vector2 pos, Vector2 size, AnimateComponentBase animCmpnt = null) : base(menu, owner, displayText, signal, pos, size)
            {
                bindMission = mission;
                this.animCmpnt = animCmpnt;

                SetPos(pos);
            }

            public void SetPos(Vector2 pos)
            {
                this.showPos = pos;
                this.hidePos = pos + Vector2.up * 1000f;
            }

            public void SetShow(bool show)
            {
                selfShow.SetTickAndStart(show ? 1 : -1);
                active = show;
                if (animCmpnt != null)
                    return;
                pos.y = show ? 450f : 1400f;
            }


            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                //if (bindMission == null)
                //{
                //    return;
                //}

                float smoothAlpha = selfShow.Get();
                if (animCmpnt != null)
                    smoothAlpha *= animCmpnt.Get();

                menuLabel.label.color = InterpColor(timeStacker, labelColor);
                menuLabel.label.alpha = smoothAlpha;

                Color color = Color.Lerp(Menu.Menu.MenuRGB(Menu.Menu.MenuColors.Black), Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White), Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));
                for (int i = 0; i < 9; i++)
                {
                    roundedRect.sprites[i].color = color;
                }
                float num = 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(buttonBehav.lastSin, buttonBehav.sin, timeStacker) / 30f * 3.1415927f * 2f);
                num *= buttonBehav.sizeBump;
                for (int j = 0; j < 8; j++)
                {
                    selectRect.sprites[j].color = MyColor(timeStacker);
                    selectRect.sprites[j].alpha = num * smoothAlpha;
                }

                for (int i = 0; i < 4; i++)
                {
                    roundedRect.sprites[roundedRect.SideSprite(i)].alpha = smoothAlpha;
                    roundedRect.sprites[roundedRect.CornerSprite(i)].alpha = smoothAlpha;
                }
            }

            public override void Update()
            {
                base.Update();
                this.buttonBehav.greyedOut = !active || MissionInfoBox.hasCardOnDisplay;
                if(animCmpnt != null)
                {
                    pos = Vector2.Lerp(hidePos, showPos, animCmpnt.Get());
                }
            }
        }

        public class EmptyRoundRect : MenuDialogBox
        {
            public float foldY;
            public float displayY;
            AnimateComponentBase animCmpnt;

            public EmptyRoundRect(Menu.Menu menu, MenuObject owner, string text, Vector2 pos, Vector2 size, float displayY, AnimateComponentBase animCmpnt = null) : base(menu, owner, text, pos, size)
            {
                darkSprite.RemoveFromContainer();
                foldY = pos.y;
                this.displayY = displayY;
                this.animCmpnt = animCmpnt;
            }

            public void SetShow(bool show)
            {
                if (animCmpnt != null)
                    return;

                roundedRect.fillAlpha = show ? 1 : 0;
                roundedRect.pos.y = show ? displayY : foldY;
            }

            public override void Update()
            {
                base.Update();
                if (animCmpnt != null)
                {
                    float show = animCmpnt.Get();
                    roundedRect.fillAlpha = show;
                    roundedRect.pos.y = Mathf.Lerp(foldY, displayY, show);
                }
            }
        }

        public class MissionSheetBox : PositionedMenuObject
        {
            BuffGameMenu gameMenu;
            BuffNewGameMissionPage missionPage;

            public int currentPage;
            public int totalPages;
            public static float buttonDisplayY = 450f;
            public static float buttonFoldY = 1400f;

            public FLabel title_exclusive;
            public FLabel title_general;
            public EmptyRoundRect holdBox;
            public EmptyRoundRect exclusiveHoldBox;
            public EmptyRoundRect generalHoldBox;
            public MissionButton exclusiveMission;
            public List<MissionButton> generalMissions;
            public BigArrowButton leftFlipButton;
            public BigArrowButton rightFlipButton;

            public MissionSheetBox(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
            {
                if (menu is BuffGameMenu) this.gameMenu = menu as BuffGameMenu;
                if (owner is BuffNewGameMissionPage) this.missionPage = owner as BuffNewGameMissionPage;
                generalMissions = new List<MissionButton>();
                currentPage = 0;

                //holdBox = new EmptyRoundRect(menu, owner, string.Empty, new Vector2(228f, 1400f), new Vector2(920f, 300f), 420f);
                //subObjects.Add(holdBox);
                exclusiveHoldBox = new EmptyRoundRect(menu, owner, "", new Vector2(248f, 1440f), new Vector2(180f, 260f), 440f, missionPage.showAnim);
                subObjects.Add(exclusiveHoldBox);
                generalHoldBox = new EmptyRoundRect(menu, owner, "", new Vector2(448f, 1440f), new Vector2(660f, 260f), 440f, missionPage.showAnim);
                subObjects.Add(generalHoldBox);

                title_exclusive = new FLabel(Custom.GetDisplayFont(), BuffResourceString.Get("BuffMissionPage_Exclusive"));
                title_exclusive.SetPosition(new Vector2(338f, Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese? 640f : 620f));
                title_exclusive.alpha = 0f;
                Container.AddChild(title_exclusive);
                title_general = new FLabel(Custom.GetDisplayFont(), BuffResourceString.Get("BuffMissionPage_General"));
                title_general.SetPosition(new Vector2(778f, 640f));
                title_general.alpha = 0f;
                Container.AddChild(title_general);

                leftFlipButton = new BigArrowButton(menu, owner, "MISSIONFLIP_LEFT", new Vector2(463f, 1400f), 3);
                rightFlipButton = new BigArrowButton(menu, owner, "MISSIONFLIP_RIGHT", new Vector2(1045f, 1400f), 1);
                subObjects.Add(leftFlipButton);
                subObjects.Add(rightFlipButton);

                InitMissionButtons();
            }

            public void InitMissionButtons()
            {
                int i = 0;
                foreach (var key in MissionRegister.GetAllUnlockedMissions())
                {
                    try
                    {
                        MissionRegister.TryGetMission(key, out var mission);
                        if (mission == null) continue;
                        if (mission.BindSlug != null && mission.BindSlug.value != gameMenu.CurrentName.value)
                        {
                            continue;
                        }

                        var missionButton = new MissionButton(mission, menu, this, Custom.rainWorld.inGameTranslator.Translate(mission.MissionName), "MISSIONPICK_" + mission.ID.value, new Vector2(533f + 170f * (i % 3), buttonDisplayY), new Vector2(160f, 120f), missionPage.showAnim);
                        if (mission.BindSlug != null && mission.BindSlug.value == gameMenu.CurrentName.value)
                        {
                            exclusiveMission = missionButton;
                            exclusiveMission.SetPos(new Vector2(258f, buttonDisplayY));
                            exclusiveMission.active = true;
                            subObjects.Add(exclusiveMission);
                            
                            continue;
                        }

                        generalMissions.Add(missionButton);
                        missionButton.active = true;
                        subObjects.Add(missionButton);
                        
                        i++;
                    }
                    catch (Exception e)
                    {
                        ExceptionTracker.TrackException(e, "Exception in BUffGamePages.InitMissionButtons");
                        BuffPlugin.LogException(e);
                        BuffPlugin.LogError("Failed in creating mission button: " + key.value);
                    }
                }

                totalPages = (int)Mathf.Ceil((float)generalMissions.Count / 3);

                if (exclusiveMission == null)
                {
                    exclusiveMission = new MissionButton(null, menu, this, "NO EXCLUSIVE\nMISSION\nFOR THIS SLUG", "NOSIGNAL", new Vector2(258f, buttonDisplayY), new Vector2(160f, 120f), missionPage.showAnim);
                    exclusiveMission.active = false;
                    exclusiveMission.SetShow(true);
                    subObjects.Add(exclusiveMission);
                }
            }

            public void RefreshExclusiveMission()
            {
                bool match = false;
                foreach (var key in MissionRegister.GetAllUnlockedMissions())
                {
                    MissionRegister.TryGetMission(key, out var mission);
                    if (mission == null) continue;
                    if (mission.BindSlug == null) continue;
                    if (mission.BindSlug != null && mission.BindSlug.value == gameMenu.CurrentName.value)
                    {

                        exclusiveMission.bindMission = mission;
                        exclusiveMission.menuLabel.text = Custom.rainWorld.inGameTranslator.Translate(mission.MissionName);
                        exclusiveMission.signalText = "MISSIONPICK_" + mission.ID.value;
                        exclusiveMission.active = true ;
                        
                        match = true;
                        break;
                    }
                }

                if (!match)
                {
                    exclusiveMission.bindMission = null;
                    exclusiveMission.menuLabel.text = BuffResourceString.Get("BuffMissionPage_No_Exclusive");
                    exclusiveMission.active = false;
                    exclusiveMission.signalText = "NOSIGNAL";
                }
            }

            public void SetShow(bool show)
            {
                if (show)
                {
                    RefreshExclusiveMission();
                    //leftFlipButton.pos.y = 470f;
                    //rightFlipButton.pos.y = 470f;
                    for (int i = 0; i < generalMissions.Count; i++)
                    {
                        if (i == currentPage * 3 || i == currentPage * 3 + 1 || i == currentPage * 3 + 2)
                        {
                            generalMissions[i].SetShow(true);
                        }
                        else generalMissions[i].SetShow(false);
                    }
                }
                else
                {
                    //leftFlipButton.pos.y = 1400f;
                    //rightFlipButton.pos.y = 1400f;
                    for (int i = 0; i < generalMissions.Count; i++)
                    {
                        generalMissions[i].SetShow(false);
                    }
                }
                
                exclusiveMission.SetShow(show);
                //holdBox.SetShow(show);
                exclusiveHoldBox.SetShow(show);
                generalHoldBox.SetShow(show);
            }

            public void FlipPage(bool rightFlip)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (currentPage * 3 + i >= generalMissions.Count) break;
                    generalMissions[currentPage * 3 + i].SetShow(true);
                }

                for (int j = 1; j < 4; j++)
                {
                    if (rightFlip)
                    {
                        if (currentPage * 3 - j < 0) break;
                        generalMissions[currentPage * 3 - j].SetShow(false);
                    }
                    else
                    {
                        if(currentPage * 3 + 2 + j >= generalMissions.Count)  break;
                        generalMissions[currentPage * 3 + 2 + j].SetShow(false);
                    }
                }

            }

            public static string GetIconElement(string slugName, bool exclusive)
            {

                string path = "buffassets/missionicons/missionicon_";
                if (!exclusive)
                {
                    path += "general";
                    Futile.atlasManager.LoadImage(path);
                    return path;
                }
                else
                {
                    if (slugName == "Yellow" || slugName == "White" || slugName == "Red")
                        return "Kill_Slugcat";

                    if (Futile.atlasManager.DoesContainElementWithName(path + slugName))
                        return path + slugName;

                    if (!Directory.Exists(AssetManager.ResolveFilePath(path + slugName)))
                    {
                        path += "general";
                        Futile.atlasManager.LoadImage(path);
                        return path;
                    }
                    else 
                    {
                        path += slugName;
                        Futile.atlasManager.LoadImage(path);
                        return path; 
                    }
                }
            }

            public override void Update()
            {
                base.Update();
                leftFlipButton.buttonBehav.greyedOut = currentPage == 0 || MissionInfoBox.hasCardOnDisplay;
                rightFlipButton.buttonBehav.greyedOut = currentPage >= totalPages - 1 || MissionInfoBox.hasCardOnDisplay;

                float show = missionPage.showAnim.Get();
                leftFlipButton.pos.y = Mathf.Lerp(1400, 470, show);
                rightFlipButton.pos.y = Mathf.Lerp(1400, 470, show);
            }

            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                float show = missionPage.showAnim.Get();
                title_general.alpha = show;
                title_general.SetPosition(title_general.x, Mathf.Lerp(1400, 640, show));
                title_exclusive.alpha = show;
                title_exclusive.SetPosition(title_exclusive.x, Mathf.Lerp(1400, 640, show));
            }
        }

        public class MissionInfoBox : PositionedMenuObject
        {
            public BuffNewGameMissionPage missionPage;
            //public EmptyRoundRect holdBox;
            public FLabel missionTitle;
            public FLabel conditionTitle;
            public FLabel buffTitle;
            public FSprite darkSprite;
            public FSprite slugIcon;
            public List<FLabel> conditionList;
            public List<CardTitleButton> buffButtons;
            public SimpleButton quitDisplayButton;
            public HoldButton startButton;
            public TestBasicInteractionManager interactionManager;

            public float rotateProgress;

            public static bool hasCardOnDisplay;
            public static float lineH = 30f;
            public static float firstLineY = 260f;

            public static float conditionHidePosY = -100f;

            static Vector2 iconShowPos = new Vector2(688f, 360f);
            static Vector2 iconHidePos = iconShowPos + Vector2.down * 1000f;
            static Vector2 missionTitleShowPos = new Vector2(688f, 320f);
            static Vector2 missionTitleHidePos = missionTitleShowPos + Vector2.down * 1000f;
            static Vector2 conditionTitleShowPos = new Vector2(440f, 340f);
            static Vector2 conditionTitleHidePos = conditionTitleShowPos + Vector2.down * 1000f;
            static Vector2 buffTitleShowPos = new Vector2(960f, 340f);
            static Vector2 buffTitleHidePos = buffTitleShowPos + Vector2.down * 1000f;

            public MissionInfoBox(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
            {
                missionPage = owner as BuffNewGameMissionPage;
                conditionList = new List<FLabel>();
                buffButtons = new List<CardTitleButton>();
                interactionManager = new TestBasicInteractionManager(null);
                //holdBox = new EmptyRoundRect(menu, owner, string.Empty, new Vector2(228f, 1400f), new Vector2(920f, 380f), 20f);
                //subObjects.Add(holdBox);
                for (int i = 0; i < 5; i++)
                {
                    var label = new FLabel(Custom.GetDisplayFont(), "");
                    //label.scale = 1.2f;
                    label.SetPosition(new Vector2(440f, firstLineY - lineH * i));
                    conditionList.Add(label);
                    Container.AddChild(label);                   
                }

                for (int j = 0; j < 6; j++)
                {
                    var button = new CardTitleButton(null, menu, this, new Vector2(840f,firstLineY - 36f * j), missionPage.showAnim);
                    buffButtons.Add(button);
                    subObjects.Add(button);
                }

                slugIcon = new FSprite("buffassets/missionicons/missionicon_general");
                slugIcon.SetPosition(iconHidePos);
                slugIcon.scale = 1.2f;
                Container.AddChild(slugIcon);

                missionTitle = new FLabel(Custom.GetDisplayFont(), "--");
                missionTitle.SetPosition(missionTitleHidePos);
                Container.AddChild(missionTitle);

                conditionTitle = new FLabel(Custom.GetDisplayFont(), BuffResourceString.Get("BuffMissionPage_Goal"));
                buffTitle = new FLabel(Custom.GetDisplayFont(), BuffResourceString.Get("BuffMissionPage_Buffs"));
                conditionTitle.SetPosition(conditionTitleHidePos);
                buffTitle.SetPosition(buffTitleHidePos);
                Container.AddChild(conditionTitle);
                Container.AddChild(buffTitle);

                startButton = new HoldButton(menu, this, BuffResourceString.Get("BuffMissionPage_Start"),"MISSION_START", new Vector2(688f, 160f), 120f);
                subObjects.Add(startButton);

                darkSprite = new FSprite("pixel");
                darkSprite.alpha = 0f;
                darkSprite.scale = 1400f;
                darkSprite.color = new Color(0.1f, 0.1f, 0.1f);
                darkSprite.SetPosition(Custom.rainWorld.screenSize / 2f);
                menu.cursorContainer.AddChild(darkSprite);

                quitDisplayButton = new SimpleButton(menu, this, Custom.rainWorld.inGameTranslator.Translate("BACK"), "MISSION_QUITDISPLAY", new Vector2(638f, 1400f), new Vector2(110f, 30f));
                subObjects.Add(quitDisplayButton);
                quitDisplayButton.Container.RemoveFromContainer();
                menu.cursorContainer.AddChild(quitDisplayButton.Container);
            }

            public void SetShow(bool show)
            {
                if (!show)
                {
                    //for (int i = 0; i < 5; i++)
                    //{
                    //    conditionList[i].y = 1400f;
                    //}
                    for (int j = 0; j < 6; j++)
                    {
                        buffButtons[j].SetShow(false);
                    }
                    quitDisplayButton.pos.y = 1800f;
                    //startButton.pos.y = 1400f;
                }
                else
                {
                    for (int i = 0; i < 5; i++)
                    {
                        conditionList[i].text = "None";
                        conditionList[i].y = firstLineY - lineH * i;
                    }
                    for (int j = 0; j < 6; j++)
                    {
                        buffButtons[j].SetShow(true);
                    }
                    slugIcon.element = Futile.atlasManager.GetElementWithName("buffassets/missionicons/missionicon_general");
                    missionTitle.text = "--";
                    missionTitle.color = Color.white;
                    //startButton.pos.y = 160f;
                }
                
                //holdBox.SetShow(show);
            }

            public void UpdateMissionInfo(Color textCol ,string missionName, string bindSlug,string[] conditions, BuffID[] cardIDs)
            {
                missionTitle.text = missionName;
                missionTitle.color = textCol;
                slugIcon.element = Futile.atlasManager.GetElementWithName(MissionSheetBox.GetIconElement(bindSlug, bindSlug == "NOBINDSLUG"? false : true));

                for (int i = 0; i < conditions.Length; i++)
                {
                    conditionList[i].text = conditions[i];
                }

                for (int j = 0; j < buffButtons.Count; j++)
                {
                    if (j < cardIDs.Length)
                    {
                        buffButtons[j].RefreshBuffInfo(cardIDs[j]);
                    }
                    else
                    {
                        buffButtons[j].RefreshBuffInfo(null);
                    }
                }
            }

            public void UpdateCardDisplay(bool display, string newID)
            {
                hasCardOnDisplay = display;
                if (display)
                {
                    quitDisplayButton.pos.y = 100f;
                    darkSprite.alpha = 0.5f;                   
                    var displayCard = new BuffCard(new BuffID(newID));
                    displayCard.Rotation = new Vector3(0f, 0f, 0f);
                    displayCard.Position = new Vector2(693f, 393f);
                    displayCard.Scale = 0.6f;

                    interactionManager.ManageCard(displayCard);
                    Container.AddChild(displayCard.Container);
                    
                }
                else
                {
                    quitDisplayButton.pos.y = -1500f;
                    darkSprite.alpha = 0f;

                    interactionManager.DestroyManagedCard();
                    
                }

                
            }

            public override void Update()
            {
                base.Update();
                interactionManager?.Update();
                startButton.buttonBehav.greyedOut = hasCardOnDisplay || BuffNewGameMissionPage.pickedMission == null;

                float anim = (owner as BuffNewGameMissionPage).showAnim.Get();
                startButton.pos.y = Mathf.Lerp(-200f, 160f, anim);

            }

            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                interactionManager?.GrafUpdate(timeStacker);

                float anim = (owner as BuffNewGameMissionPage).showAnim.Get();
                conditionTitle.alpha = anim;
                conditionTitle.SetPosition(Vector2.Lerp(conditionTitleHidePos, conditionTitleShowPos, anim));

                buffTitle.alpha = anim;
                buffTitle.SetPosition(Vector2.Lerp(buffTitleHidePos, buffTitleShowPos, anim));

                missionTitle.alpha = anim;
                missionTitle.SetPosition(Vector2.Lerp(missionTitleHidePos, missionTitleShowPos, anim));
                
                slugIcon.alpha = anim;
                slugIcon.SetPosition(Vector2.Lerp(iconHidePos, iconShowPos, anim));
                for (int i = 0; i < 5; i++)
                {
                    conditionList[i].alpha = anim;
                    conditionList[i].SetPosition(new Vector2(440f, firstLineY - lineH * i - 1000f * (1f - anim)));
                }
            }

            public class CardTitleButton : SimpleButton
            {
                public static Vector2 buttonSize = new Vector2(240f, 30f);
                public BuffID bindBuff;
                public Color buttonColor;
                public bool active;
                float origY;
                float hideY;
                float shadowAlpha;


                FSprite shadow;
                AnimateComponentBase animCmpnt;

                public CardTitleButton(BuffID bindBuff ,Menu.Menu menu, MenuObject owner, Vector2 pos, AnimateComponentBase animCmpnt = null) : base(menu,owner,"--","NOSIGNAL",pos,buttonSize)
                {
                    this.animCmpnt = animCmpnt;
                    SetPos(pos.y);

                    shadow = new FSprite("Circle20") { /*shader = Custom.rainWorld.Shaders["FlatLight"] ,*/ alpha = 0f, color = Color.black, scale = 0.8f};
                    Container.AddChild(shadow);
                    shadow.MoveBehindOtherNode(menuLabel.label);
                    RefreshBuffInfo(bindBuff);
                }

                public void SetPos(float y)
                {
                    origY = y;
                    hideY = y - 1000f;
                }

                public void RefreshBuffInfo(BuffID buffID)
                {
                    bindBuff = buffID;
                    if (bindBuff != null)
                    {
                        if (BuffConfigManager.Instance.StaticDataLoaded(this.bindBuff))
                        {
                            var staticData = BuffConfigManager.GetStaticData(this.bindBuff);
                            this.menuLabel.text = staticData.GetCardInfo(Custom.rainWorld.inGameTranslator.currentLanguage).info.BuffName;
                            this.signalText = "MISSIONBUFF_" + bindBuff.value;
                            this.buttonColor = staticData.Color * 0.1f;
                            this.buttonColor.a = 1f;
                            this.active = true;
                            shadowAlpha = 1f;
                            shadow.color = staticData.Color;
                            //shadow.scaleX = LabelTest.GetWidth(staticData.GetCardInfo(Custom.rainWorld.inGameTranslator.currentLanguage).info.BuffName) / 20f + 8;
                        }
                        else
                        {
                            this.bindBuff = null;
                            this.menuLabel.text = "--";
                            this.buttonColor = new Color(0.2f, 0.2f, 0.2f);
                            this.signalText = "NOSIGNAL";
                            this.active = false;
                            shadowAlpha = 0f;
                        }
                    }
                    else
                    {
                        this.bindBuff = null;
                        this.menuLabel.text = "--";
                        this.buttonColor = new Color(0.2f, 0.2f, 0.2f);
                        this.signalText = "NOSIGNAL";
                        this.active = false;
                        shadowAlpha = 0f;
                    }
                }

                public void SetShow(bool show)
                {
                    if (animCmpnt != null)
                        return;

                    if (show)
                    {
                        this.pos.y = origY;
                    }
                    else
                    {
                        this.pos.y = 1400f;
                    }
                }

                public override void GrafUpdate(float timeStacker)
                {
                    base.GrafUpdate(timeStacker);

                    float smoothAlpha = 1f;
                    if (animCmpnt != null)
                        smoothAlpha *= animCmpnt.Get();

                    menuLabel.label.color = InterpColor(timeStacker, labelColor);
                    menuLabel.label.alpha = smoothAlpha;

                    Color color = Color.Lerp(Menu.Menu.MenuRGB(Menu.Menu.MenuColors.Black), Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White), Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));
                    for (int i = 0; i < 9; i++)
                    {
                        roundedRect.sprites[i].color = buttonColor;
                        roundedRect.sprites[i].alpha = smoothAlpha;
                    }
                    float num = 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(buttonBehav.lastSin, buttonBehav.sin, timeStacker) / 30f * 3.1415927f * 2f);
                    num *= buttonBehav.sizeBump;
                    for (int j = 0; j < 8; j++)
                    {
                        selectRect.sprites[j].color = buttonColor;
                        selectRect.sprites[j].alpha = num * smoothAlpha;
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        roundedRect.sprites[roundedRect.SideSprite(i)].alpha = smoothAlpha;
                        roundedRect.sprites[roundedRect.CornerSprite(i)].alpha = smoothAlpha;
                    }

                    Vector2 smoothPos = Vector2.Lerp(ScreenLastPos, ScreenPos, timeStacker);
                    shadow.SetPosition(smoothPos + new Vector2(15f, buttonSize.y / 2f));
                    shadow.alpha = smoothAlpha * shadowAlpha;
                    shadow.scale = 0.9f + 0.4f * buttonBehav.sizeBump;
                }

                public override void Update()
                {
                    base.Update();
                    this.buttonBehav.greyedOut = !active || MissionInfoBox.hasCardOnDisplay;
                    if(animCmpnt != null)
                    {
                        pos.y = Mathf.Lerp(hideY, origY, animCmpnt.Get());
                    }
                }
            }
        }
    }

    internal class ModeSelectPage : PositionedMenuObject
    {
        //BuffGameMenu gameMenu;

        public SimpleButton defaultmodeButton;
        public SimpleButton missionmodeButton;
        public FSprite darkSprite;
        public FLabel modeIntroduction;

        public bool Show;
        public float fadeInCounter;
        public float showTextCounter;
        public float lastY;
        public float lastAlpha;
        public float y;

        public ModeSelectPage(BuffGameMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, Vector2.zero)
        {
            this.menu = menu;
            //gameMenu = menu;
            darkSprite = new FSprite("pixel")
            {
                scale = 1400f,
                x = 693f,
                y = 393f,
                alpha = 0f,
                color = Color.black
            };
            Container.AddChild(darkSprite);

            modeIntroduction = new FLabel(Custom.GetDisplayFont(), "") 
            {
                scale = 1.2f,
                x = 693f,
                y = 80f,
                alpha = 0f,
            };
            Container.AddChild(modeIntroduction);

            subObjects.Add(defaultmodeButton = new SimpleButton(menu, this, BuffResourceString.Get("Mode_Free"), "DEFAULTMODE", new Vector2(493f, 900f), new Vector2(160f, 200f)));
            subObjects.Add(missionmodeButton = new SimpleButton(menu, this, BuffResourceString.Get("Mode_Mission"), "MISSIONMODE", new Vector2(733f, 900f), new Vector2(160f, 200f)));

            defaultmodeButton.nextSelectable[2] = missionmodeButton;
            missionmodeButton.nextSelectable[0] = defaultmodeButton;
            Helper.LinkEmptyToSelf(defaultmodeButton);
            Helper.LinkEmptyToSelf(missionmodeButton);
        }

        public void SetShow(bool show)
        {
            Show = show;
            if (show)
                menu.selectedObject = defaultmodeButton;
            else
                (menu as BuffGameMenu).ResetSelectables();
        }

        
        public override void Update()
        {
            base.Update();
            lastY = y;
            lastAlpha = modeIntroduction.alpha;

            if (Show)
            {
                if (fadeInCounter < 1f)
                {
                    fadeInCounter += 0.05f;
                }
                y = Mathf.Sin(0.5f * Mathf.PI * fadeInCounter);
            }

            if (!Show)
            {
                if (fadeInCounter > 0f)
                {
                    fadeInCounter -= 0.05f;
                }
                y = -Mathf.Cos(0.5f * Mathf.PI * fadeInCounter) + 1f;
            }

            if (defaultmodeButton.MouseOver || missionmodeButton.MouseOver)
            {
                if (showTextCounter < 1f)
                {
                    showTextCounter += 0.0625f;
                }
            }
            else
            {
                if (showTextCounter > 0f)
                {
                    showTextCounter -= 0.0625f;
                }
            }

            if (defaultmodeButton.MouseOver)
            {
                modeIntroduction.text = BuffResourceString.Get("Mode_Desc_Free");
            }
            else if (missionmodeButton.MouseOver)
            {
                modeIntroduction.text = BuffResourceString.Get("Mode_Desc_Mission");
            }
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            float num = Mathf.Lerp(lastY, y, timeStacker);
            darkSprite.alpha = 0.5f * num;
            modeIntroduction.y = Mathf.Lerp(900f, 80f, num);
            modeIntroduction.alpha = Mathf.Lerp(lastAlpha, showTextCounter, timeStacker);
            defaultmodeButton.pos.y = Mathf.Lerp(900f, 400f, num);
            missionmodeButton.pos.y = Mathf.Lerp(900f, 400f, num);
        }
    }

    public class FNodeWrapper : PositionedMenuObject
    {
        List<FNode> nodes = new();
        Dictionary<FNode, Vector2> setPositions = new();
        Dictionary<FNode, Vector2> positions = new();
        Dictionary<FNode, Vector2> lastPositions = new();

        public FNodeWrapper(Menu.Menu menu, MenuObject owner) : base(menu, owner, Vector2.zero)
        {
        }

        public void WrapNode(FNode node, Vector2 position)
        {
            if (!nodes.Contains(node))
            {
                nodes.Add(node);
                setPositions.Add(node, position);
                positions.Add(node, position + pos);
                lastPositions.Add(node, position + pos);

                Container.AddChild(node);
            }
            else
                setPositions[node] = position;
        }

        public void UnwarpNode(FNode node)
        {
            if (!nodes.Contains(node))
                return;

            nodes.Remove(node);
            setPositions.Remove(node);
            positions.Remove(node);
            lastPositions.Remove(node);
            Container.RemoveChild(node);
        }

        public void SetPosition(FNode node, Vector2 position)
        {
            if (!nodes.Contains(node))
                return;
            setPositions[node] = position;
        }

        public override void Update()
        {
            base.Update();
            foreach (var node in nodes)
            {
                lastPositions[node] = positions[node];
                positions[node] = setPositions[node] + ScreenPos;
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            foreach (var node in nodes)
            {
                node.SetPosition(Vector2.Lerp(lastPositions[node], positions[node], timeStacker));
            }
        }

        public override void RemoveSprites()
        {
            nodes.Clear();
            setPositions.Clear();
            positions.Clear();
            lastPositions.Clear();
            base.RemoveSprites();
        }
    }

    public class SimpleImageButton : ButtonTemplate
    {
        public RoundedRect roundedRect;
        public RoundedRect selectRect;
        public FSprite sprite;
        public string signalText;
        public Color color;

        public SimpleImageButton(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, string element, string signalText) : base(menu, owner, pos, size)
        {
            this.signalText = signalText;
            roundedRect = new RoundedRect(menu, this, new Vector2(0f, 0f), size, true);
            subObjects.Add(this.roundedRect);
            selectRect = new RoundedRect(menu, this, new Vector2(0f, 0f), size, false);
            subObjects.Add(selectRect);
            this.signalText = signalText;

            sprite = new FSprite(element);
            Container.AddChild(sprite);
        }

        public override void Clicked()
        {
            this.Singal(this, this.signalText);
        }

        public void SetSize(Vector2 newSize)
        {
            size = newSize;
            roundedRect.size = size;
            selectRect.size = size;
        }

        public override void Update()
        {
            base.Update();
            buttonBehav.Update();
            roundedRect.fillAlpha = Mathf.Lerp(0.3f, 0.6f, buttonBehav.col);
            roundedRect.addSize = new Vector2(10f, 6f) * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * 3.1415927f)) * (buttonBehav.clicked ? 0f : 1f);
            selectRect.addSize = new Vector2(2f, -2f) * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * 3.1415927f)) * (buttonBehav.clicked ? 0f : 1f);
        }

        // Token: 0x06002D8B RID: 11659 RVA: 0x0036F07C File Offset: 0x0036D27C
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            Color color = Color.Lerp(Menu.Menu.MenuRGB(Menu.Menu.MenuColors.Black), Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White), Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));
            for (int i = 0; i < 9; i++)
            {
                roundedRect.sprites[i].color = color;
            }
            float num = 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(this.buttonBehav.lastSin, this.buttonBehav.sin, timeStacker) / 30f * 3.1415927f * 2f);
            num *= buttonBehav.sizeBump;
            for (int j = 0; j < 8; j++)
            {
                selectRect.sprites[j].color = this.MyColor(timeStacker);
                selectRect.sprites[j].alpha = num;
            }

            Vector2 smoothPos = Vector2.Lerp(ScreenLastPos, ScreenPos, timeStacker);
            sprite.SetPosition(smoothPos + size / 2f);
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            sprite.RemoveFromContainer();
        }
    }
}
