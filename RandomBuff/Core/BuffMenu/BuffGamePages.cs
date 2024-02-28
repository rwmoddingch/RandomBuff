using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.UI.BuffCondition;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
            foreach(var conditionLabel in conditionLabels)
            {
                RemoveSubObject(conditionLabel);
                conditionLabel.RemoveSprites();
            }
            conditionLabels.Clear();

            foreach(var conditionRect in conditionRects)
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
                if(currentGameSetting.conditions[i].Finished)
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
            else if(message == "NEWGAME_DETAIL_MINUS")
            {
                UpdateActiveConditionCount(false);
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            }
            else if(message == "NEWGAME_DETAIL_PLUS")
            {
                UpdateActiveConditionCount(true);
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            }
        }

        void RefreshConditionButtonState()
        {
            for(int i = 0; i < conditionButtons.Length; i++)
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
            currentGameSetting.conditions.Clear();
            activeConditionCount = BuffGameMenuStatics.DefaultConditionNum;
            for(int i = 0; i < activeConditionCount; i++)
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

            currentGameSetting.conditions.Remove(loadedCondition.condition);
            loadedCondition.condition = currentGameSetting.GetRandomCondition().condition;
            if(toggleHide)
            {
                loadedCondition.hide = !loadedCondition.hide;
                BuffPlugin.Log($"{index} Toggle hide : {conditionInstances[index].hide}");

                if(loadedCondition.hide)
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

                currentGameSetting.conditions.Remove(conditionInstances[activeConditionCount - 1].condition);
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
                currentGameSetting.conditions.Remove(conditionInstances[i].condition);
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

    public class FNodeWrapper : PositionedMenuObject
    {
        List<FNode> nodes = new();
        Dictionary<FNode, Vector2> setPositions = new();
        Dictionary<FNode, Vector2> positions = new();
        Dictionary<FNode, Vector2> lastPositions= new();

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
            foreach(var node in nodes)
            {
                lastPositions[node] = positions[node];
                positions[node] = setPositions[node] + ScreenPos;
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            foreach(var node in nodes)
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
