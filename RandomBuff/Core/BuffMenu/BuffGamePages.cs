using JetBrains.Annotations;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RandomBuff.Cardpedia.InfoPageRender;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.UI;
using RandomBuff.Render.UI.BuffCondition;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using static MonoMod.InlineRT.MonoModRule;

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
        bool Show
        {
            get => _targetShowCounter == MaxShowSwitchCounter;
            set => _targetShowCounter = (value ? MaxShowSwitchCounter : 0);
        }
        float ShowFactor => (float)_showCounter / MaxShowSwitchCounter;
        Action showToggleFinishCallBack;

        public BuffContinueGameDetialPage(BuffGameMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            gameMenu = menu;
            myContainer = new FContainer();

            owner.Container.AddChild(myContainer);
            Container.AddChild(dark = new FSprite("pixel") { color = Color.black, alpha = 0f, scaleX = Custom.rainWorld.screenSize.x, scaleY = Custom.rainWorld.screenSize.y, x = Custom.rainWorld.screenSize.x / 2f, y = Custom.rainWorld.screenSize.y / 2f });

            SetupContinueGamePageItems();
        }

        public void SetupContinueGamePageItems()
        {
            currentGameSetting = BuffDataManager.Instance.GetGameSetting(gameMenu.CurrentName);

            Vector2 testPos = new Vector2(683f, 85f) + new Vector2(SlugcatSelectMenu.GetRestartTextOffset(gameMenu.CurrLang), 80f);
            var tabWrapper = new MenuTabWrapper(gameMenu, this);
            subObjects.Add(tabWrapper);
            restartButton = new OpHoldButton(new Vector2(683f + 240f - 120f, Mathf.Max(30, Custom.rainWorld.options.SafeScreenOffset.y)), new Vector2(120, 40), "Restart", 100f);
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

            Show = menu.manager.rainWorld.progression.IsThereASavedGame(name);
            if (Show)
            {
                RefreshMenuItemsForSlugcat();
                Show = true;
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
                dark.alpha = ShowFactor * 0.5f;
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
        HorizontalSlider difficultySlider;

        BigSimpleButton[] conditionButtons;
        SymbolButton[] hiddenToggles;

        SymbolButton randomButton;
        SymbolButton minusButton;
        SymbolButton plusButton;

        SimpleButton jollyToggleConfigMenu;

        ConditionInstance[] conditionInstances = new ConditionInstance[5];
        GameSetting currentGameSetting;

        //状态变量
        int _showCounter = -1;
        int _targetShowCounter;
        bool Show
        {
            get => _targetShowCounter == BuffGameMenuStatics.MaxShowSwitchCounter;
            set => _targetShowCounter = (value ? BuffGameMenuStatics.MaxShowSwitchCounter : 0);
        }
        float ShowFactor => (float)_showCounter / BuffGameMenuStatics.MaxShowSwitchCounter;

        int activeConditionCount;
        bool canAddAnyCondition;

        public BuffNewGameDetailPage(BuffGameMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            gameMenu = menu as BuffGameMenu;
            myContainer = new FContainer();
            owner.Container.AddChild(myContainer);

            Container.AddChild(dark = new FSprite("pixel") { color = Color.black, alpha = 0f, scaleX = Custom.rainWorld.screenSize.x, scaleY = Custom.rainWorld.screenSize.y, x = Custom.rainWorld.screenSize.x / 2f, y = Custom.rainWorld.screenSize.y / 2f });
            SetupNewGamePageItems();
        }

        public void SetupNewGamePageItems()
        {
            var gameSetting = BuffDataManager.Instance.GetGameSetting(gameMenu.CurrentName);

            Vector2 testPos = new Vector2(683f, 85f) + new Vector2(SlugcatSelectMenu.GetRestartTextOffset(gameMenu.CurrLang), 80f);
            subObjects.Add(backButton = new SimpleButton(gameMenu, this, gameMenu.Translate("BACK"), "NEWGAME_DETAIL_BACK", new Vector2(200f, 698f), new Vector2(110f, 30f)));
            subObjects.Add(startGameButton = new HoldButton(gameMenu, this, gameMenu.Translate("NEWGAME"), "NEWGAME_DETAIL_NEWGAME", new Vector2(683f, 85f), 40f));
            subObjects.Add(settingButton = new SimpleButton(gameMenu, this, gameMenu.Translate(BuffDataManager.Instance.GetGameSetting(gameMenu.CurrentName).TemplateName),
                "NEWGAME_DETAIL_SELECT_MODE", new Vector2(683f - 240f, Mathf.Max(30, Custom.rainWorld.options.SafeScreenOffset.y)),
                new Vector2(120, 40)));
            settingButton.menuLabel.text = gameMenu.Translate(gameSetting.TemplateName);

            subObjects.Add(nodeWrapper = new FNodeWrapper(gameMenu, this));

            //难度滑条
            subObjects.Add(difficultySlider = new HorizontalSlider(gameMenu, this, "", new Vector2(470f, 600f), new Vector2(400f, 0f), BuffGameMenuStatics.DifficultySlider, false));

            var easySprite = new FSprite("SaintA", true);
            easySprite.color = new Color(0.2f, 0.75f, 0.2f);
            nodeWrapper.WrapNode(easySprite, new Vector2(680f - 250f, 620f));

            var hardSprite = new FSprite("OutlawA", true);
            hardSprite.color = new Color(0.75f, 0.2f, 0.2f);
            nodeWrapper.WrapNode(hardSprite, new Vector2(680f + 250f, 620f));

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

            Container.MoveToFront();
        }

        public void SetShow(bool show)
        {
            Show = show;
            gameMenu.SetButtonsActive(!show);
            currentGameSetting = BuffDataManager.Instance.GetGameSetting(gameMenu.CurrentName);
            if (show)
                InitConditions();
            else
                QuitToSelectSlug();

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

                pos = Vector2.Lerp(BuffGameMenuStatics.HidePos, Vector2.zero, Helper.LerpEase(ShowFactor));
                //gameMenu.menuSlot.basePos = pos;
                dark.alpha = ShowFactor;
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

                settingButton.menuLabel.text = gameMenu.Translate(gameSetting.TemplateName);
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

        public BuffNewGameMissionPage(BuffGameMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            this.menu = menu;
            gameMenu = menu;
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

            backButton = new SimpleButton(menu, this, menu.Translate("BACK"), "NEWGAME_MISSION_BACK", new Vector2(1200f, 1400f), new Vector2(110f, 30f));
            subObjects.Add(backButton);

        }

        public void SetShow(bool show)
        {
            if (show)
            {
                blackSprite.alpha = 1f;
                backButton.pos.y = 698f;
                pickedMission = null;
                currentGameSetting = BuffDataManager.Instance.GetGameSetting(gameMenu.CurrentName);
            }
            else
            {
                blackSprite.alpha = 0f;
                backButton.pos.y = 1400f;

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
                foreach (var key in MissionRegister.registeredMissions.Keys)
                {
                    if (key.value == missionID)
                    {
                        MissionRegister.registeredMissions.TryGetValue(key, out var mission);
                        string[] info = new string[5];
                        for (int i = 0; i < 5; i++)
                        {
                            if (mission.conditions.Count > i)
                            {
                                info[i] = mission.conditions[i].DisplayName(Custom.rainWorld.inGameTranslator);
                            }
                            else
                            {
                                info[i] = "None";
                            }
                        }
                        missionInfoBox.UpdateMissionInfo(mission.textCol ,Custom.rainWorld.inGameTranslator.Translate(mission.missionName), mission.bindSlug == null? "NOBINDSLUG" : mission.bindSlug.value,info, mission.startBuffSet.ToArray());
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
                currentGameSetting.ClearCondition();
                BuffHookWarpper.CheckAndDisableAllHook();
                for (int i = 0; i < pickedMission.conditions.Count; i++)
                {
                    currentGameSetting.conditions.Add(pickedMission.conditions[i]);
                }
                gameMenu.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat =
                        gameMenu.CurrentName;
                gameMenu.manager.rainWorld.progression.WipeSaveState(gameMenu.CurrentName);
                gameMenu.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
                
                for (int j = 0; j < pickedMission.startBuffSet.Count; j++)
                {
                    BuffDataManager.Instance.GetOrCreateBuffData(pickedMission.startBuffSet[j], true);
                }
                
                gameMenu.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
                currentGameSetting.NewGame();
                gameMenu.PlaySound(SoundID.MENU_Start_New_Game);
            }
            
        }

        public override void Update()
        {
            base.Update();
            backButton.buttonBehav.greyedOut = MissionInfoBox.hasCardOnDisplay;
        }

        public class MissionButton : SimpleButton
        {
            public Mission bindMission;
            public bool active;
            public MissionButton(Mission mission, Menu.Menu menu, MenuObject owner, string displayText, string signal, Vector2 pos, Vector2 size) : base(menu, owner, displayText, signal, pos, size)
            {
                bindMission = mission;
            }

            public void SetShow(bool show)
            {
                pos.y = show ? 450f : 1400f;
            }

            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (bindMission == null)
                {
                    return;
                }
                for (int i = 0; i < 17; i++)
                {
                    this.roundedRect.sprites[i].color = bindMission.textCol;
                }
                this.menuLabel.label.color = bindMission.textCol;
                for (int j = 0; j < 8; j++)
                {
                    this.selectRect.sprites[j].color = bindMission.textCol;
                }

            }

            public override void Update()
            {
                base.Update();
                this.buttonBehav.greyedOut = !active || MissionInfoBox.hasCardOnDisplay;
            }
        }

        public class EmptyRoundRect : MenuDialogBox
        {
            public float foldY;
            public float displayY;
            public EmptyRoundRect(Menu.Menu menu, MenuObject owner, string text, Vector2 pos, Vector2 size, float displayY) : base(menu, owner, text, pos, size)
            {
                darkSprite.RemoveFromContainer();
                foldY = pos.y;
                this.displayY = displayY;
            }

            public void SetShow(bool show)
            {
                roundedRect.fillAlpha = show ? 1 : 0;
                roundedRect.pos.y = show ? displayY : foldY;
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

                holdBox = new EmptyRoundRect(menu, owner, string.Empty, new Vector2(228f, 1400f), new Vector2(920f, 300f), 420f);
                subObjects.Add(holdBox);
                exclusiveHoldBox = new EmptyRoundRect(menu, owner, "", new Vector2(248f, 1440f), new Vector2(180f, 260f), 440f);
                subObjects.Add(exclusiveHoldBox);
                generalHoldBox = new EmptyRoundRect(menu, owner, "", new Vector2(448f, 1440f), new Vector2(660f, 260f), 440f);
                subObjects.Add(generalHoldBox);

                title_exclusive = new FLabel(Custom.GetDisplayFont(), Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese ? "专属使命":"EXCLUSIVE\nMISSION");
                title_exclusive.SetPosition(new Vector2(338f, Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese? 640f : 620f));
                title_exclusive.alpha = 0f;
                Container.AddChild(title_exclusive);
                title_general = new FLabel(Custom.GetDisplayFont(), Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese ? "通用使命":"GENERAL MISSIONS");
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
                foreach (var key in MissionRegister.registeredMissions.Keys)
                {
                    try
                    {
                        MissionRegister.registeredMissions.TryGetValue(key, out var mission);
                        if (mission == null) continue;
                        if (mission.bindSlug != null && mission.bindSlug.value != gameMenu.CurrentName.value)
                        {
                            continue;
                        }

                        var missionButton = new MissionButton(mission, menu, this, Custom.rainWorld.inGameTranslator.Translate(mission.missionName), "MISSIONPICK_" + mission.ID.value, new Vector2(533f + 170f * (i % 3), buttonDisplayY), new Vector2(160f, 120f));
                        if (mission.bindSlug != null && mission.bindSlug.value == gameMenu.CurrentName.value)
                        {
                            exclusiveMission = missionButton;
                            exclusiveMission.pos = new Vector2(258f, buttonDisplayY);
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
                        BuffPlugin.LogException(e);
                        BuffPlugin.LogError("Failed in creating mission button: " + key.value);
                    }
                }

                totalPages = (int)Mathf.Ceil((float)generalMissions.Count / 3);

                if (exclusiveMission == null)
                {
                    exclusiveMission = new MissionButton(null, menu, this, "NO EXCLUSIVE\nMISSION\nFOR THIS SLUG", "NOSIGNAL", new Vector2(258f, buttonDisplayY), new Vector2(160f, 120f));
                    exclusiveMission.active = false;
                    subObjects.Add(exclusiveMission);
                }
            }

            public void RefreshExclusiveMission()
            {
                bool match = false;
                foreach (var key in MissionRegister.registeredMissions.Keys)
                {
                    MissionRegister.registeredMissions.TryGetValue(key, out var mission);
                    if (mission == null) continue;
                    if (mission.bindSlug == null) continue;
                    if (mission.bindSlug != null && mission.bindSlug.value == gameMenu.CurrentName.value)
                    {

                        exclusiveMission.bindMission = mission;
                        exclusiveMission.menuLabel.text = Custom.rainWorld.inGameTranslator.Translate(mission.missionName);
                        exclusiveMission.signalText = "MISSIONPICK_" + mission.ID.value;
                        exclusiveMission.active = true ;
                        
                        match = true;
                        break;
                    }
                }

                if (!match)
                {
                    exclusiveMission.bindMission = null;
                    exclusiveMission.menuLabel.text = Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese? "该角色无\n专属使命":"NO EXCLUSIVE MISSION\nFOR THIS CHARACTER";
                    exclusiveMission.active = false;
                    exclusiveMission.signalText = "NOSIGNAL";
                }
            }

            public void SetShow(bool show)
            {
                if (show)
                {
                    RefreshExclusiveMission();
                    leftFlipButton.pos.y = 470f;
                    rightFlipButton.pos.y = 470f;
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
                    leftFlipButton.pos.y = 1400f;
                    rightFlipButton.pos.y = 1400f;
                    for (int i = 0; i < generalMissions.Count; i++)
                    {
                        generalMissions[i].SetShow(false);
                    }
                }
                title_general.alpha = show? 1f : 0f;
                title_exclusive.alpha = show? 1f : 0f;
                exclusiveMission.SetShow(show);
                holdBox.SetShow(show);
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
                    {
                        return "Kill_Slugcat";
                    }

                    if (Futile.atlasManager.DoesContainElementWithName(path + slugName))
                    {
                        return path + slugName;
                    }

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
            }
        }

        public class MissionInfoBox : PositionedMenuObject
        {
            public EmptyRoundRect holdBox;
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

            public MissionInfoBox(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
            {
                conditionList = new List<FLabel>();
                buffButtons = new List<CardTitleButton>();
                interactionManager = new TestBasicInteractionManager(null);
                holdBox = new EmptyRoundRect(menu, owner, string.Empty, new Vector2(228f, 1400f), new Vector2(920f, 380f), 20f);
                subObjects.Add(holdBox);
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
                    var button = new CardTitleButton(null, menu, this, new Vector2(840f,firstLineY - 36f * j));
                    buffButtons.Add(button);
                    subObjects.Add(button);
                }

                slugIcon = new FSprite("buffassets/missionicons/missionicon_general");
                slugIcon.SetPosition(new Vector2(688f, 360f));
                slugIcon.scale = 1.2f;
                Container.AddChild(slugIcon);

                missionTitle = new FLabel(Custom.GetDisplayFont(), "--");
                missionTitle.SetPosition(new Vector2(688f, 320f));
                Container.AddChild(missionTitle);

                conditionTitle = new FLabel(Custom.GetDisplayFont(), Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese? "使命目标":"MISSION GOALS");
                buffTitle = new FLabel(Custom.GetDisplayFont(), Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese ? "携带卡牌" : "MISSION BUFFS");
                conditionTitle.SetPosition(440f,340f);
                buffTitle.SetPosition(960f, 340f);
                Container.AddChild(conditionTitle);
                Container.AddChild(buffTitle);

                startButton = new HoldButton(menu, this, Custom.rainWorld.inGameTranslator.Translate("START MISSION"),"MISSION_START", new Vector2(688f, 160f), 120f);
                subObjects.Add(startButton);

                darkSprite = new FSprite("pixel");
                darkSprite.alpha = 0f;
                darkSprite.scale = 1400f;
                darkSprite.color = new Color(0.1f, 0.1f, 0.1f);
                darkSprite.SetPosition(new Vector2(693f, 393f));
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
                    for (int i = 0; i < 5; i++)
                    {
                        conditionList[i].y = 1400f;
                    }
                    for (int j = 0; j < 6; j++)
                    {
                        buffButtons[j].SetShow(false);
                    }
                    quitDisplayButton.pos.y = 1800f;
                    startButton.pos.y = 1400f;
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
                    startButton.pos.y = 160f;
                }
                conditionTitle.alpha = show? 1 : 0;
                buffTitle.alpha = show? 1 : 0;
                missionTitle.alpha = show? 1 : 0;
                slugIcon.alpha = show ? 1 : 0;
                holdBox.SetShow(show);
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
            }

            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                interactionManager?.GrafUpdate(timeStacker);
            }

            public class CardTitleButton : SimpleButton
            {
                public static Vector2 buttonSize = new Vector2(240f, 30f);
                public BuffID bindBuff;
                public Color buttonColor;
                public bool active;
                public float origY;
                public CardTitleButton(BuffID bindBuff ,Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu,owner,"--","NOSIGNAL",pos,buttonSize)
                {
                    RefreshBuffInfo(bindBuff);
                    origY = pos.y;
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
                            this.buttonColor = staticData.Color;
                            this.active = true;
                        }
                        else
                        {
                            this.bindBuff = null;
                            this.menuLabel.text = "--";
                            this.buttonColor = new Color(0.2f, 0.2f, 0.2f);
                            this.signalText = "NOSIGNAL";
                            this.active = false;
                        }
                    }
                    else
                    {
                        this.bindBuff = null;
                        this.menuLabel.text = "--";
                        this.buttonColor = new Color(0.2f, 0.2f, 0.2f);
                        this.signalText = "NOSIGNAL";
                        this.active = false;
                    }
                }

                public void SetShow(bool show)
                {
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
                    this.menuLabel.label.color = this.buttonColor;
                    
                    for (int i = 0; i < this.roundedRect.sprites.Length; i++)
                    {
                        this.roundedRect.sprites[i].color = this.buttonColor;
                    }

                    for (int j = 0; j < this.selectRect.sprites.Length; j++)
                    {
                        this.selectRect.sprites[j].color = this.buttonColor;
                    }

                }

                public override void Update()
                {
                    base.Update();
                    this.buttonBehav.greyedOut = !active || MissionInfoBox.hasCardOnDisplay;
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

            subObjects.Add(defaultmodeButton = new SimpleButton(menu, this, "FREE MODE", "DEFAULTMODE", new Vector2(493f, 900f), new Vector2(160f, 200f)));
            subObjects.Add(missionmodeButton = new SimpleButton(menu, this, "MISSION MODE", "MISSIONMODE", new Vector2(733f, 900f), new Vector2(160f, 200f)));
        }

        public void SetShow(bool show)
        {
            Show = show;
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
                modeIntroduction.text = Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese? freeModeIntro_Chi : freeModeIntro;
            }
            else if (missionmodeButton.MouseOver)
            {
                modeIntroduction.text = Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese ? missionModeIntro_Chi : missionModeIntro;
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

        //模式说明内容
        #region
        public static readonly string freeModeIntro = "You can customize the game's difficulty,\nmode and goals freely before starting a new game,\nand can only get cards through card gachas.";
        public static readonly string freeModeIntro_Chi = "你可以在开始新一局游戏之前自由决定游戏难度、模式和目标，\n但只能通过抽卡来获得卡牌。";
        public static readonly string missionModeIntro = "Choose a mission to start a new game.\nYour goals and initial cards are decided by which mission you chose,\nand you can't change the game's difficulty and mode.";
        public static readonly string missionModeIntro_Chi = "选择一个使命以开始一局新游戏。\n你的游戏目标和初始携带卡牌由选择的任务决定，\n且无法自行决定游戏难度和模式。";
        #endregion
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
}
