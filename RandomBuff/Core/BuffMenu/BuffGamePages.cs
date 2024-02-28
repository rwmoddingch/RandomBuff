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

namespace RandomBuff.Core.BuffMenu
{
    public static class BuffGameMenuStatics
    {
        public static int DefaultConditionNum = 2;
        public static int MaxShowSwitchCounter = 20;
        public static Vector2 HidePos = new Vector2(0f, -1000f);
        public static Slider.SliderID DifficultySlider = new Slider.SliderID("BuffGameDetial_DifficultySlider", true);
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
        OpHoldButton restartButton;
        List<MenuLabel> conditionLabels = new();

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

            if (currentGameSetting == null)
                return;

            for (int i = 0; i < currentGameSetting.conditions.Count; i++)
            {
                string text = currentGameSetting.conditions[i].DisplayName(gameMenu.manager.rainWorld.inGameTranslator) + " " + currentGameSetting.conditions[i].DisplayProgress(gameMenu.manager.rainWorld.inGameTranslator);
                MenuLabel conditionLabel = new MenuLabel(gameMenu, this, text, new Vector2(680f/* - 120f*/, 320f - i * 40f), new Vector2(100f, 30f), true);
                conditionLabel.label.color = currentGameSetting.conditions[i].Finished ? Color.green : MenuColorEffect.rgbWhite;
                conditionLabels.Add(conditionLabel);
                subObjects.Add(conditionLabel);
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
                conditionButtons[i] = new BigSimpleButton(menu, this, "Condition " + (i + 1).ToString(), "NEWGAME_DETAIL_CONDITION_" + (i + 1).ToString(), new Vector2(360f, 510f - num), new Vector2(600f, 40f), FLabelAlignment.Left, true);
                subObjects.Add(conditionButtons[i]);
                hiddenToggles[i] = new SymbolButton(menu, this, "hiddenopen", "NEWGAME_DETAIL_CONDITION_HIDDEN_" + (i + 1).ToString(), new Vector2(970f, 510f - num));
                hiddenToggles[i].size = new Vector2(40f, 40f);
                hiddenToggles[i].roundedRect.size = hiddenToggles[i].size;
                subObjects.Add(hiddenToggles[i]);
            }

            randomButton = new SymbolButton(menu, this, "Sandbox_Randomize", "NEWGAME_DETAIL_CONDITION_RANDOM", new Vector2(430f, 250f));
            randomButton.size = new Vector2(40f, 40f);
            randomButton.roundedRect.size = randomButton.size;
            subObjects.Add(randomButton);
            minusButton = new SymbolButton(menu, this, "minus", "NEWGAME_DETAIL_CONDITION_MINUS", new Vector2(900f, 250f));
            minusButton.size = new Vector2(40f, 40f);
            minusButton.roundedRect.size = minusButton.size;
            subObjects.Add(minusButton);
            plusButton = new SymbolButton(menu, this, "plus", "NEWGAME_DETAIL_CONDITION_PLUS", new Vector2(950f, 250f));
            plusButton.size = new Vector2(40f, 40f);
            plusButton.roundedRect.size = plusButton.size;
            subObjects.Add(plusButton);

            Container.MoveToFront();
        }

        public void SetShow(bool show)
        {
            Show = show;
            gameMenu.SetButtonsActive(!show);
            currentGameSetting = BuffDataManager.Instance.GetGameSetting(gameMenu.CurrentName);
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
            }
            else if (message == "NEWGAME_DETAIL_BACK")
            {
                Show = false;
                gameMenu.SetButtonsActive(true);
            }
        }

        void RefreshConditionButtonState()
        {

        }

        void InitConditions()
        {

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
