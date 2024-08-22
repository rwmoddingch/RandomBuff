using Menu.Remix.MixedUI;
using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.CardRender;
using RWCustom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Progression.Quest;
using UnityEngine;
using static RandomBuff.Render.UI.Component.CardPocketSlot;
using Menu;
using RandomBuff.Core.Game;

namespace RandomBuff.Render.UI.Component
{
    internal class CardPocket
    {
        public static float gap = 10f;

        string title;
        public float cardScale;
        public Vector2 size;
        public Vector2 hoverPos;
        public Vector2 anchor;
        public CardPocketCallBack updateSelectedBuffsCallBack;
        public Action<List<BuffRep>> onSelectedBuffChange;
        public Action<bool> toggleShowCallBack;

        bool show = false;
        FContainer container;
        FContainer bottomContainer_2;
        FContainer bottomContainer_1;
        FContainer symbolContainer;
        CardPocketSlot slot;
        public List<BuffRep> currentSelectedBuffs = new List<BuffRep>();
        public List<BuffRep> lastSelectedBuffs = new List<BuffRep>();

        public FContainer Container => container;
        public FContainer SymbolContainer => symbolContainer;
        public FContainer BottomContainer_1 => bottomContainer_1;
        public FContainer BottomContainer_2 => bottomContainer_2;
        public bool Show => show;
        public string Title
        {
            get => title;
            set
            {
                if (title == value)
                    return;
                title = value;
                slot.titleRect.SetTitle(value);
            }
        }

        public CardPocket(List<BuffID> initBuffID, string title, CardPocketCallBack updateSelectedBuffsCallBack, float cardScale, Vector2 size, Vector2 hoverPos, Vector2 anchor,int maxSelectCount = -1)
        {
            this.title = title;
            this.updateSelectedBuffsCallBack = updateSelectedBuffsCallBack;
            this.cardScale = cardScale;
            this.size = size;
            this.hoverPos = hoverPos;
            this.anchor = anchor;


            container = new FContainer();
            bottomContainer_1 = new FContainer();
            bottomContainer_2 = new FContainer();
            symbolContainer = new FContainer();
            slot = new CardPocketSlot(this) { maxSelectedCount = maxSelectCount};

            container.AddChild(bottomContainer_2);
            container.AddChild(bottomContainer_1);
            container.AddChild(slot.Container);
            container.AddChild(symbolContainer);
            //symbolContainer.MoveToFront();
            SetSelectedBuffIDs(initBuffID);

            slot.SetShow(false);
            container.alpha = 0f;
        }

        public void Update()
        {
            slot.Update();
        }

        public void GrafUpdate(float timeStacker)
        {
            slot.GrafUpdate(timeStacker);
            //if (Input.GetKeyDown(KeyCode.P) && showAnim == null)
            //    SetShow(!show);
        }

        TickAnimCmpnt showAnim;
        float startAlpha;
        public void SetShow(bool show)
        {
            if (showAnim != null)
            {
                showAnim.Destroy();
                showAnim = null;
            }

            this.show = show;
            toggleShowCallBack?.Invoke(show);
            if (show)
            {
                startAlpha = container.alpha;
                slot.SetShow(true);
                showAnim = AnimMachine.GetTickAnimCmpnt(0, 20, autoDestroy: true).BindActions(
                    OnAnimGrafUpdate:(a,f) => 
                    {
                        container.alpha = Mathf.Lerp(startAlpha, 1f, a.Get());  
                    },
                    OnAnimFinished:(a) =>
                    {
                        showAnim = null;
                        container.alpha = 1f;
                    });
            }
            else
            {
                startAlpha = container.alpha;
                showAnim = AnimMachine.GetTickAnimCmpnt(0, 20, autoDestroy: true).BindActions(
                   OnAnimGrafUpdate: (a, f) =>
                   {
                       container.alpha = Mathf.Lerp(startAlpha, 0f, a.Get());
                   },
                   OnAnimFinished: (a) =>
                   {
                       showAnim = null;
                       TriggerCallBack();
                       
                       slot.SetShow(false);
                       container.alpha = 0f;
                   });
            }
        }

        void TriggerCallBack()
        {
            List<BuffRep> removedBuffs = new List<BuffRep>();
            List<BuffRep> addedBuffs = new List<BuffRep>();

            foreach(var buffID in currentSelectedBuffs)
            {
                if (!lastSelectedBuffs.Contains(buffID))
                    addedBuffs.Add(buffID);
            }

            foreach(var buffID in lastSelectedBuffs)
            {
                if(!currentSelectedBuffs.Contains(buffID))
                    removedBuffs.Add(buffID);
            }

            updateSelectedBuffsCallBack?.Invoke(currentSelectedBuffs, removedBuffs, addedBuffs);
        }

        public void SetSelectedBuffIDs(List<BuffRep> buffIDs)
        {
            currentSelectedBuffs.Clear();
            lastSelectedBuffs.Clear();
            foreach (var buffID in buffIDs)
            {
                currentSelectedBuffs.Add(buffID);
                lastSelectedBuffs.Add(buffID);
            }
        }

        public void SetSelectedBuffIDs(List<BuffID> buffIDs)
        {
            currentSelectedBuffs.Clear();
            lastSelectedBuffs.Clear();
            foreach (var id in buffIDs)
            {
                var rep = slot.TryGetRep(id,true,true);
                currentSelectedBuffs.Add(rep);
                lastSelectedBuffs.Add(rep);
            }
        }

        public void Destroy()
        {
            container.RemoveFromContainer();
            container.RemoveAllChildren();
            bottomContainer_1.RemoveAllChildren();
            bottomContainer_2.RemoveAllChildren();
            symbolContainer.RemoveAllChildren();
            slot.Destory();
        }

        public IEnumerable<Vector2> GetAllButtonPos()
        {
            return slot.GetAllButtonPos();
        }

        public static int GetRealSelectionCount(List<BuffRep> reps)
        {
            int result = 0;
            foreach (var rep in reps)
            {
                if (rep.stackable)
                    result += rep.stackCount;
                else
                    result++;
            }
            return result;
        }

        public delegate void CardPocketCallBack(List<BuffRep> allselectedBuffIDs, List<BuffRep> removedBuffIDs, List<BuffRep> addedBuffIDs);
    }

    internal class CardPocketSlot : BuffCardSlot
    {
        public CardPocket pocket;
        public RoundRectSprites rectSprite;
        public TitleRect titleRect;
        public List<SideSingleSelectButton> buffTypeSwitchButtons = new List<SideSingleSelectButton>();
        public SideSingleSelectButton closeButton;

        public SideSingleSelectButton unstackButton;
        public SideSingleSelectButton stackButton;
        public FContainer stackButtonContainer;

        public int maxSelectedCount = -1;

        public int cardsInRoll;
        public float externGap;
        public float singleRollHeight;
        public Vector2 actualDisplaySize;
        public Vector2 buffCardSize;

        public Dictionary<BuffID, BuffRep> id2RepMapping = new Dictionary<BuffID, BuffRep>();

        public Dictionary<BuffType, List<BuffRep[]>> buffRolls = new Dictionary<BuffType, List<BuffRep[]>>();
        public Dictionary<BuffType, float> allContentSize = new Dictionary<BuffType, float>();

        public float yPointer;
        public float scrollVel;
        public bool enableScroll = true;
        public BuffType currentType;

        bool mouseInside;

        public Vector2 TopLeftPos => pocket.hoverPos + new Vector2(pocket.anchor.x * pocket.size.x, (1f - pocket.anchor.y) * pocket.size.y);
        public Vector2 BottomLeftPos => pocket.hoverPos + new Vector2(pocket.anchor.x * pocket.size.x, (-pocket.anchor.y) * pocket.size.y);
        public List<BuffRep[]> CurrentTypeRolls => buffRolls[currentType];
        public List<BuffRep> CurrentSelectedBuffs => pocket.currentSelectedBuffs;
        public int CurrentSelections => CardPocket.GetRealSelectionCount(CurrentSelectedBuffs);

        public Dictionary<BuffCard, FSprite> correctSymbolMapper = new Dictionary<BuffCard, FSprite>();

        public CardPocketSlot(CardPocket pocket)
        {
            this.pocket = pocket;
            BaseInteractionManager = new CardPocketInteratctionManager(this);
            HelpInfoProvider = new HelpInfoProvider(this);

            #region 预计算参数

            if(pocket.currentSelectedBuffs.Count > 0)//同步buffrep
            {
                foreach(var selection in pocket.currentSelectedBuffs)
                {
                    id2RepMapping.Add(selection.buffID, selection);
                }
            }

            buffCardSize = new Vector2(CardBasicAssets.RenderTextureSize.x, CardBasicAssets.RenderTextureSize.y) * pocket.cardScale * BuffCard.interactiveScaleBound;
            externGap = buffCardSize.y / 2.5f;
            cardsInRoll = Mathf.FloorToInt(pocket.size.x / (buffCardSize.x + CardPocket.gap));
            singleRollHeight = buffCardSize.y + CardPocket.gap;
            actualDisplaySize = new Vector2(pocket.size.x, pocket.size.y - externGap * 2f);

            foreach (var buffType in Helper.EnumBuffTypes())
                buffRolls.Add(buffType, new List<BuffRep[]>());

            Dictionary<BuffType, List<BuffRep>> currentBuffRolls = new Dictionary<BuffType, List<BuffRep>>() {
                { BuffType.Positive, new List<BuffRep>()},
                { BuffType.Duality, new List<BuffRep>()},
                { BuffType.Negative, new List<BuffRep>()}};

            foreach (var buffIDValue in BuffID.values.entries)
            {
                var id = new BuffID(buffIDValue);
                if (!BuffConfigManager.ContainsId(id) || !BuffPlayerData.Instance.IsCollected(id) ||
                    BuffConfigManager.IsItemLocked(QuestUnlockedType.Card, id.value))
                    continue;
                var staticData = BuffConfigManager.GetStaticData(id);

                var rep = TryGetRep(id);

                currentBuffRolls[staticData.BuffType].Add(rep);
                if (currentBuffRolls[staticData.BuffType].Count == cardsInRoll)
                {
                    buffRolls[staticData.BuffType].Add(currentBuffRolls[staticData.BuffType].ToArray());
                    currentBuffRolls[staticData.BuffType].Clear();
                }
            }

            foreach(var buffType in Helper.EnumBuffTypes())
            {
                if (currentBuffRolls[buffType].Count > 0)
                    buffRolls[buffType].Add(currentBuffRolls[buffType].ToArray());
                allContentSize[buffType] = buffRolls[buffType].Count * (buffCardSize.y + CardPocket.gap) + CardPocket.gap;
            }
            #endregion

            SetBuffType(BuffType.Positive, false);

            rectSprite = new RoundRectSprites(pocket.BottomContainer_1, BottomLeftPos + new Vector2(0f, - CardPocket.gap * 5f/2f), pocket.size +new Vector2(CardPocket.gap, CardPocket.gap * 5f)) { borderColor = Color.white };

            
            titleRect = new TitleRect(pocket.BottomContainer_2, pocket.Title, TopLeftPos + new Vector2(CardPocket.gap, CardPocket.gap * 5f / 2f), SideSingleSelectButton.defaultHeight);

            int n = 0;
            float buttonWidth = 100f;
            foreach (var bufftype in Helper.EnumBuffTypes())
            {
                var newButton = new SideSingleSelectButton(pocket.BottomContainer_2, rectSprite.pos + new Vector2(0f, rectSprite.size.y - (CardPocket.gap + buttonWidth) * n - CardPocket.gap - buttonWidth), buttonWidth, BuffResourceString.Get(bufftype.ToString()), bufftype.ToString(), -90f);
                if(bufftype == currentType)
                    newButton.SetSelected(true);
                newButton.selectedAction += SetBuffTypeButtonClick;
                newButton.groupSelectAction += GroupButtonAction;
                buffTypeSwitchButtons.Add(newButton);
                n++;
            }

            closeButton = new SideSingleSelectButton(pocket.BottomContainer_2, rectSprite.pos + new Vector2(pocket.size.x - CardPocket.gap - 50f, rectSprite.size.y), 50f, "X", "")
            {
                selectedAction = (s) =>
                {
                    this.pocket.SetShow(false);
                    closeButton.SetSelected(false);
                }
            };

            stackButtonContainer = new FContainer() { alpha = 0f};
            pocket.BottomContainer_2.AddChild(stackButtonContainer);

            Vector2 screenSize = Custom.rainWorld.options.ScreenSize;

            unstackButton = new SideSingleSelectButton(stackButtonContainer, new Vector2(screenSize.x / 2f - 120f, 0f), 100f, BuffResourceString.Get("CardPocket_UnstackButton"), "")
            {
                selectedAction = (s) =>
                {
                    (BaseInteractionManager as CardPocketInteratctionManager).TryUnStack();
                    unstackButton.SetSelected(false);
                },
                enableInput = false
            };
            stackButton = new SideSingleSelectButton(stackButtonContainer, new Vector2(screenSize.x / 2f + 20f, 0f), 100f, BuffResourceString.Get("CardPocket_StackButton"), "")
            {
                selectedAction = (s) =>
                {
                    (BaseInteractionManager as CardPocketInteratctionManager).TryStack();
                    stackButton.SetSelected(false);
                },
                enableInput = false
            };
        }

        static CardPocketSlot()
        {
            HelpInfoProvider.CustomProviders += HelpInfoProvider_CustomProviders;
        }

        void SetBuffTypeButtonClick(string key)
        {
            switch (key)
            {
                case "Positive":
                    SetBuffType(BuffType.Positive);
                    break;
                case "Negative":
                    SetBuffType(BuffType.Negative);
                    break;
                case "Duality":
                    SetBuffType(BuffType.Duality);
                    break;
            }
        }

        void GroupButtonAction(SideSingleSelectButton sideSingleSelectButton)
        {
            foreach(var button in buffTypeSwitchButtons)
            {
                if (button != sideSingleSelectButton)
                    button.SetSelected(false);
            }
        }

        public override void Update()
        {
            base.Update();
            rectSprite.Update();
            titleRect.Update();
            ButtonUpdate();

            mouseInside = false;
            Vector2 delta = InputAgency.Current.GetMousePosition() - BottomLeftPos;
            if (delta.x > 0 && delta.x < pocket.size.x && delta.y > 0 && delta.y < pocket.size.y)
                mouseInside = true;
            if (InputAgency.CurrentAgencyType == InputAgency.AgencyType.Gamepad)
                mouseInside = true;


            if (scrollVel != 0f && pocket.Show)
            {
                yPointer += scrollVel;
                yPointer = Mathf.Clamp(yPointer, 0f, allContentSize[currentType]);

                scrollVel = Mathf.Lerp(scrollVel, 0f, 0.15f);
                if (Mathf.Approximately(scrollVel, 0f))
                    scrollVel = 0f;

                UpdateDisplayRoll();
            }

            if (enableScroll && pocket.Show && mouseInside)
            {
                float scroll = InputAgency.Current.GetScroll();
                if (InputAgency.CurrentAgencyType == InputAgency.AgencyType.Default)
                    scroll *= 2.5f;
                if (scroll < 0)
                    scrollVel = Mathf.Lerp(scrollVel, 30f, 0.25f);
                else if (scroll > 0)
                    scrollVel = Mathf.Lerp(scrollVel, -30f, 0.25f);
            }

            //testLabel.text = $"{currentType} - {CurrentTypeRolls.Count}\n{yPointer} - {actualDisplaySize.y} - {allContentSize[currentType]}\n{buffCardSize.x}_{buffCardSize.y}\n{testDisplayMinRoll}->{testDisplayMaxRoll}";
        }

        public void ButtonUpdate()
        {
            foreach (var button in buffTypeSwitchButtons)
                button.Update();
            closeButton.Update();
            stackButton.Update();
            unstackButton.Update();

            InputAgency.Current.GetMainFunctionButton(out _, out var single);
            if (single)
            {
                foreach (var button in buffTypeSwitchButtons)
                    button.OnMouseLeftClick();
                closeButton.OnMouseLeftClick();
                stackButton.OnMouseLeftClick();
                unstackButton.OnMouseLeftClick();
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            rectSprite.GrafUpdate(timeStacker);
            titleRect.GrafUpdate(timeStacker);
            foreach (var button in buffTypeSwitchButtons)
                button.GrafUpdate(timeStacker);
            closeButton.GrafUpdate(timeStacker);
            stackButton.GrafUpdate(timeStacker);
            unstackButton.GrafUpdate(timeStacker);
        }

        public override void AppendCard(BuffCard buffCard)
        {
            base.AppendCard(buffCard);
            var sprite = new FSprite("buffassets/illustrations/correctSymbol") { color = Color.green, alpha = 0f};
            pocket.SymbolContainer.AddChild(sprite);
            correctSymbolMapper.Add(buffCard, sprite);
        }

        public override void RemoveCard(BuffCard buffCard, bool destroyAfterRemove = false)
        {
            correctSymbolMapper[buffCard].RemoveFromContainer();
            base.RemoveCard(buffCard, destroyAfterRemove);
        }

        public FSprite GetCorrectSymbol(BuffCard buffCard)
        {
            return correctSymbolMapper[buffCard];
        }

        public void SetBuffType(BuffType newType,bool updateDisplay = true)
        {
            currentType = newType;
            foreach(var buff in BuffCards.ToArray())
            {
                RemoveCard(buff, true);
            }
            (BaseInteractionManager as CardPocketInteratctionManager)!.ChangeBuffType();
            ScrollToTop();
            if(updateDisplay)
                UpdateDisplayRoll();
        }

        public void ScrollToTop()
        {
            ScrollTo(0f);
        }

        public void ScrollToBottom()
        {
            ScrollTo(1f);
        }

        public void ScrollTo(float percentage, bool resetVel = true)
        {
            yPointer = Mathf.Lerp(0f, allContentSize[currentType], percentage);
            if (resetVel)
                scrollVel = 0f;
        }

        public void StopScroll()
        {
            scrollVel = 0f;
        }

        public void SetScrollEnable(bool enable)
        {
            this.enableScroll = enable;
            if (!enable)
                StopScroll();
        }

        public void UpdateDisplayRoll()
        {
            float downDisplayY = yPointer + actualDisplaySize.y;
            int minRoll = Mathf.Clamp(Mathf.CeilToInt(yPointer / singleRollHeight), 0, CurrentTypeRolls.Count - 1);
            int maxRoll = Mathf.Clamp(Mathf.CeilToInt(downDisplayY / singleRollHeight), 0, CurrentTypeRolls.Count - 1);

            (BaseInteractionManager as CardPocketInteratctionManager)!.UpdateDisplayRoll(minRoll, maxRoll);
        }

        public void ToggleSelectBuff(BuffID buffID)
        {
            ToggleSelectBuff(TryGetRep(buffID));
        }

        public void ToggleSelectBuff(BuffRep buffRep)
        {
            if(CurrentSelectedBuffs.Contains(buffRep))
            {
                CurrentSelectedBuffs.Remove(buffRep);
                if (buffRep.stackable)
                    buffRep.stackCount = 0;
                pocket.onSelectedBuffChange?.Invoke(CurrentSelectedBuffs);
            }
            else if(maxSelectedCount == -1 || CurrentSelections < maxSelectedCount)
            {
                CurrentSelectedBuffs.Add(buffRep);
                if (buffRep.stackable)
                    buffRep.stackCount = 1;
                pocket.onSelectedBuffChange?.Invoke(CurrentSelectedBuffs);
            }
        }

        public bool IsBuffSelected(BuffID buffID)
        {
            return CurrentSelectedBuffs.Contains(TryGetRep(buffID));
        }
        public bool IsBuffSelected(BuffRep buffID)
        {
            return CurrentSelectedBuffs.Contains(buffID);
        }

        public BuffRep TryGetRep(BuffID buffID, bool create = true, bool needRefresh = false)
        {
            if (id2RepMapping.TryGetValue(buffID, out var result))
            {
                if(needRefresh)
                    return result.Refresh();
                return result;
            }
            
            if(create)
            {
                result = new BuffRep(buffID);
                id2RepMapping.Add(buffID, result);
                return result;
            }
            return null;
        }

        public void StackBuff(BuffRep buffRep)
        {
            if (maxSelectedCount != -1 && CurrentSelections >= maxSelectedCount)
                return;

            if (!buffRep.stackable)
                return;
            if(buffRep.stackCount == 0)
            {
                ToggleSelectBuff(buffRep);
            }
            else
            {
                buffRep.stackCount++;
                pocket.onSelectedBuffChange?.Invoke(CurrentSelectedBuffs);
            }
        }

        public void UnStackBuff(BuffRep buffRep)
        {
            if (!CurrentSelectedBuffs.Contains(buffRep))
                return;

            if (!buffRep.stackable)
                return;
            if(buffRep.stackCount > 0)
            {
                buffRep.stackCount--;
                if (buffRep.stackCount == 0)
                    ToggleSelectBuff(buffRep);
                else
                    pocket.onSelectedBuffChange?.Invoke(CurrentSelectedBuffs);
            }
        }

        public void SetShow(bool show)
        {
            if (show)
            {
                ScrollToTop();
                UpdateDisplayRoll();
                //InputAgency.Current.TakeFocus(BaseInteractionManager);
            }
            else
            {
                foreach (var buff in BuffCards.ToArray())
                {
                    RemoveCard(buff, true);
                }
                InputAgency.Current.RecoverLastIfIsFocus(BaseInteractionManager);
            }
            (BaseInteractionManager as CardPocketInteratctionManager).SetShow(show);
            foreach (var button in buffTypeSwitchButtons)
                button.enableInput = show;
            closeButton.enableInput = show;
        }

        public override void Destory()
        {
            id2RepMapping.Clear();
            InputAgency.Current.RecoverLastIfIsFocus(BaseInteractionManager, true);
            base.Destory();
        }

        public IEnumerable<Vector2> GetAllButtonPos()
        {
            if (pocket.Show)
            {
                yield return closeButton.MiddleOfButton();
                foreach (var button in buffTypeSwitchButtons)
                    yield return button.MiddleOfButton();
            }
        }


        float initAlpha;
        TickAnimCmpnt stackButtonShowAnim;
        public void SetStackButtonShow(bool show)
        {
            if (stackButtonContainer == null)
                return;

            if (show && stackButtonContainer.alpha == 1f)
                return;
            if (!show && stackButtonContainer.alpha == 0f)
                return;

            if (stackButtonShowAnim != null)
                stackButtonShowAnim.Destroy();
            initAlpha = stackButtonContainer.alpha;

            stackButton.enableInput = show;
            unstackButton.enableInput = show;

            stackButtonShowAnim = AnimMachine.GetTickAnimCmpnt(0, 20, autoDestroy: true).BindActions(OnAnimGrafUpdate: (t, l) =>
            {
                stackButtonContainer.alpha = Mathf.Lerp(initAlpha, show ? 1f : 0f, t.Get());
            },OnAnimFinished:(t) =>
            {
                stackButtonContainer.alpha = show ? 1f : 0f;
                stackButtonShowAnim = null;
            });
        }

        public bool IgnoreEmtpyClick()
        {
            if (stackButton.enableInput && stackButton.mouseInside)
                return true;
            if (unstackButton.enableInput && unstackButton.mouseInside)
                return true;
            return false;
        }

        private static bool HelpInfoProvider_CustomProviders(HelpInfoProvider.HelpInfoID ID, out string helpInfo, params object[] Params)
        {
            helpInfo = "";
            if (ID == CardPocket_None)
            {
                helpInfo = BuffResourceString.Get("CardPocket_HelpInfo_None");
                return true;
            }
            else if(ID == CardPocket_Hover)
            {
                var buffID = Params[0] as BuffID;
                var staticData = buffID.GetStaticData(); 
                var name = staticData.CardInfos.First().Value;

                if (staticData.CardInfos.TryGetValue(InGameTranslator.LanguageID.English, out var newName))
                    name = newName;
                if (staticData.CardInfos.TryGetValue(Custom.rainWorld.inGameTranslator.currentLanguage, out newName))
                    name = newName;

                helpInfo = string.Format(BuffResourceString.Get("CardPocket_HelpInfo_Hover"), name.BuffName);
                if (staticData.Stackable)
                    helpInfo += string.Format(BuffResourceString.Get("CardPocket_HelpInfo_Hover_Stackable"), name.BuffName);
                return true;
            }
            else if(ID == CardPocket_Exclusive)
            {
                var buffID = Params[0] as BuffID;
                var staticData = buffID.GetStaticData();
                var name = staticData.CardInfos.First().Value;

                if (staticData.CardInfos.TryGetValue(InGameTranslator.LanguageID.English, out var newName))
                    name = newName;
                if (staticData.CardInfos.TryGetValue(Custom.rainWorld.inGameTranslator.currentLanguage, out newName))
                    name = newName;

                helpInfo = string.Format(BuffResourceString.Get("CardPocket_HelpInfo_Exclusive"), name.BuffName);
                if (staticData.Stackable)
                    helpInfo += string.Format(BuffResourceString.Get("CardPocket_HelpInfo_Exclusive_Stackable"), name.BuffName);
                return true;
            }
            return false;
        }


        public class BuffRep
        {
            public BuffID buffID;

            public int stackCount;
            public bool stackable;

            public BuffRep Refresh()
            {
                stackCount = buffID.GetBuffData()?.StackLayer ?? 0;
                return this;
            }

            public BuffRep(BuffID buffID)
            {
                this.buffID = buffID;
                stackable = buffID.GetStaticData().Stackable;
                stackCount = buffID.GetBuffData()?.StackLayer ?? 0;
            }
        }

        public static HelpInfoProvider.HelpInfoID CardPocket_None = new HelpInfoProvider.HelpInfoID("CardPocket_None", true);
        public static HelpInfoProvider.HelpInfoID CardPocket_Hover = new HelpInfoProvider.HelpInfoID("CardPocket_Hover", true);
        public static HelpInfoProvider.HelpInfoID CardPocket_Exclusive = new HelpInfoProvider.HelpInfoID("CardPocket_Exclusive", true);
    }

    internal class CardPocketInteratctionManager : ClickSignalInteractionManager<CardPocketSlot>
    {
        BuffCard exclusiveShowCard;
        CardPocket Pocket => Slot.pocket;

        State currentState = State.Normal;

        Dictionary<int, List<BuffCard>> rollsOfCards = new Dictionary<int, List<BuffCard>>();
        Dictionary<BuffCard, int> card2RollMapping = new Dictionary<BuffCard, int>();

        public CardPocketInteratctionManager(CardPocketSlot slot) : base(slot)
        {
        }

        public override void Update()
        {
            base.Update();
            if(currentState == State.Exclusive)
            {
                exclusiveShowCard._cardRenderer.cardTextBackController.CommitScroll(InputAgency.Current.GetScroll() * (InputAgency.CurrentAgencyType == InputAgency.AgencyType.Default ? 2f : 1f));
            }
        }

        protected override void UpdateFocusCard()
        {
            if (currentState == State.Normal)
            {
                foreach (var card in managedCards)
                {
                    if (card.LocalMousePos.x > 0 &&
                        card.LocalMousePos.x < 1f &&
                        card.LocalMousePos.y > 0f &&
                        card.LocalMousePos.y < 1f)
                    {
                        Slot.HelpInfoProvider.UpdateHelpInfo(CardPocket_Hover, CurrentFocusCard != card, card.ID);
                        CurrentFocusCard = card;
                        return;
                    }
                }

                if (CurrentFocusCard != null)
                {
                    CurrentFocusCard = null;
                }
                Slot.HelpInfoProvider.UpdateHelpInfo(CardPocket_None);
            }
            else if (currentState == State.Exclusive)
            {
                if (exclusiveShowCard != null &&
                    exclusiveShowCard.LocalMousePos.x > 0 &&
                    exclusiveShowCard.LocalMousePos.x < 1f &&
                    exclusiveShowCard.LocalMousePos.y > 0f &&
                    exclusiveShowCard.LocalMousePos.y < 1f)
                {
                    CurrentFocusCard = exclusiveShowCard;
                    return;
                }

                if (CurrentFocusCard != null)
                    CurrentFocusCard = null;
            }
            else if (currentState == State.Hide)
            {
                Slot.HelpInfoProvider.UpdateHelpInfo(HelpInfoProvider.HelpInfoID.None);
                if (CurrentFocusCard != null)
                    CurrentFocusCard = null;
            }
        }

        protected override void OnMouseSingleClick()
        {
            base.OnMouseSingleClick();
            if (currentState == State.Normal)
            {
                if (CurrentFocusCard != null)
                {
                    Slot.BringToTop(CurrentFocusCard);
                    exclusiveShowCard = CurrentFocusCard;
                    SetState(State.Exclusive);
                }
            }
            else if (currentState == State.Exclusive)
            {
                if (CurrentFocusCard == null)
                {
                    if (Slot.IgnoreEmtpyClick())
                        return;

                    Slot.RecoverCardSort(exclusiveShowCard);
                    exclusiveShowCard = null;
                    SetState(State.Normal);
                }
                else
                {
                    exclusiveShowCard.OnMouseSingleClick();
                }
            }
        }

        protected override void OnMouseRightClick()
        {
            base.OnMouseRightClick();
            if(CurrentFocusCard != null)
            {
                Slot.ToggleSelectBuff(CurrentFocusCard.ID);
            }
        }

        public void SetState(State newState, bool forceUpdate = false)
        {
            if(currentState == newState && !forceUpdate) 
                return;

            if(newState == State.Hide)
            {
                rollsOfCards.Clear();
                card2RollMapping.Clear();

                InputAgency.Current.RecoverLastIfIsFocus(this);
                Slot.SetStackButtonShow(false);
            }
            else if(newState == State.Normal)
            {
                foreach (var card in managedCards)
                    card.SetAnimatorState(BuffCard.AnimatorState.CardPocketSlot_Normal);
                Slot.SetScrollEnable(true);
                InputAgency.Current.TakeFocus(this);

                //用于在动画完成时更新鼠标位置
                AnimMachine.GetDelayCmpnt(30, autoDestroy: true).BindActions(OnAnimFinished: (d) =>
                {
                    InputAgency.Current.ResetToDefaultPos();
                });
                Slot.SetStackButtonShow(false);
            }
            else if(newState == State.Exclusive)
            {
                Slot.HelpInfoProvider.UpdateHelpInfo(CardPocket_Exclusive, false, exclusiveShowCard.ID);
                exclusiveShowCard.SetAnimatorState(BuffCard.AnimatorState.CardPocketSlot_Exclusive);
                Slot.SetScrollEnable(false);
                if(exclusiveShowCard.StaticData.Stackable)
                    Slot.SetStackButtonShow(true);
                else
                    Slot.SetStackButtonShow(false);
            }
            currentState = newState;
        }

        public override void DismanageCard(BuffCard card)
        {
            base.DismanageCard(card);
            if (card2RollMapping.ContainsKey(card))
            {
                rollsOfCards[card2RollMapping[card]].Remove(card);
                card2RollMapping.Remove(card);
            }
        }

        public BuffCard ManageCardInRoll(int roll,BuffRep buffrep)
        {
            var card = Slot.AppendCard(buffrep.buffID);
            rollsOfCards[roll].Add(card);
            card2RollMapping.Add(card, roll);
            card.SetAnimatorState(BuffCard.AnimatorState.CardPocketSlot_Normal);
            return card;
        }

        public void UpdateDisplayRoll(int min, int max)
        {
            //移除超过显示区域的卡
            List<int> rollsToRemove = new List<int>();
            foreach(var key in rollsOfCards.Keys)
            {
                if(key < min -1 || key > max + 1)
                    rollsToRemove.Add(key);
            }

            foreach(var roll in rollsToRemove)
            {
                List<BuffCard> cardsToRemove = new List<BuffCard>();

                foreach (var card in rollsOfCards[roll])
                    cardsToRemove.Add(card);

                foreach(var card in cardsToRemove)
                    Slot.RemoveCard(card, true);

                rollsOfCards.Remove(roll);
            }

            //添加应在显示区域内的卡
            int displayMin = Mathf.Clamp(min - 1, 0, Slot.CurrentTypeRolls.Count - 1);
            int displayMax = Mathf.Clamp(max + 1, 0, Slot.CurrentTypeRolls.Count - 1);

            for (int i = displayMin; i <= displayMax; i++)
            {
                if (rollsOfCards.ContainsKey(i))
                    continue;

                rollsOfCards.Add(i, new List<BuffCard>());
                foreach (BuffRep idRep in Slot.CurrentTypeRolls[i])
                    ManageCardInRoll(i, idRep);
            }
        }

        public Vector2 GetInPocketPosition(BuffCard card)
        {
            int roll = card2RollMapping[card];
            int x = rollsOfCards[roll].IndexOf(card);

            //BuffPlugin.Log($"{card.ID}, x:{x}, roll:{roll}");
            return new Vector2(x * (CardPocket.gap + Slot.buffCardSize.x) + (CardPocket.gap + Slot.buffCardSize.x / 2f),
                roll * (Slot.buffCardSize.y + CardPocket.gap));
        }

        public void ChangeBuffType()
        {
            SetState(State.Normal, true);
            rollsOfCards.Clear();
            card2RollMapping.Clear();
        }

        public void SetShow(bool show)
        {
            if(!show)
                SetState(State.Hide);
            else
                SetState(State.Normal);
        }


        public float GetCorrectSymbolAlpha(BuffCard buffCard)
        {
            return Slot.IsBuffSelected(buffCard.ID) ? 1f : 0f;
        }

        public void TryStack()
        {
            if(currentState == State.Exclusive)
            {
                Slot.StackBuff(Slot.TryGetRep(exclusiveShowCard.ID));
            }
        }

        public void TryUnStack()
        {
            if(currentState == State.Exclusive)
            {
                Slot.UnStackBuff(Slot.TryGetRep(exclusiveShowCard.ID));
            }
        }

        public override IEnumerable<Vector2> GetAllFocusableObjectPos()
        {
            foreach(var item in base.GetAllFocusableObjectPos())
                yield return item;
            foreach (var item in Slot.GetAllButtonPos())
                yield return item;
        }

        public enum State
        {
            Hide,
            Normal,
            Exclusive
        }
    }

    internal class CardPocketNormalAnimator : BuffCardAnimator
    {
        public static float symbolBiasScaleBound = BuffCard.interactiveScaleBound * 0.5f * 0.9f;

        CardPocketInteratctionManager pocketInteractionManager;
        Vector2 targetInPocketPos;
        float baseScale;

        Vector2 pos;
        Vector2 lastPos;
         Vector3 targetRotation = Vector3.zero;

        Vector2 TargetPosition => pocketInteractionManager.Slot.TopLeftPos + new Vector2(targetInPocketPos.x, pocketInteractionManager.Slot.yPointer - targetInPocketPos.y - pocketInteractionManager.Slot.externGap);
        float TargetScaleFactor
        {
            get
            {
                float deltaY = targetInPocketPos.y - pocketInteractionManager.Slot.yPointer;
                if (deltaY < 0f)
                    return Mathf.InverseLerp(pocketInteractionManager.Slot.externGap, 0f, -deltaY);
                else if (deltaY > pocketInteractionManager.Slot.actualDisplaySize.y)
                    return Mathf.InverseLerp(pocketInteractionManager.Slot.actualDisplaySize.y + pocketInteractionManager.Slot.externGap, pocketInteractionManager.Slot.actualDisplaySize.y, deltaY);
                else
                    return 1f;
            }
        }
        float ExternScale => (buffCard.CurrentFocused ? 0.05f : 0f);

        public CardPocketNormalAnimator(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            buffCard.Highlight = false;
            buffCard.DisplayDescription = false;
            buffCard.DisplayTitle = true;
            buffCard.DisplayAllGraphTexts = false;
            buffCard.KeyBinderFlash = false;
            buffCard.DisplayStacker = false;

            buffCard.UpdateGrey();
            buffCard.UpdateGraphText();
            

            pocketInteractionManager = buffCard.interactionManager as CardPocketInteratctionManager;
            targetInPocketPos = pocketInteractionManager.GetInPocketPosition(buffCard);
            baseScale = pocketInteractionManager.Slot.pocket.cardScale;

            
            if (buffCard.lastAnimatorState != BuffCard.AnimatorState.CardPocketSlot_Exclusive)
            {
                buffCard.Scale = 0f;
                buffCard.Alpha = 0f;
                lastPos = pos = TargetPosition;
            }
            else
                lastPos = pos = initPosition;
        }

        public override void Update()
        {
            lastPos = pos;
            pos = Vector2.Lerp(pos, TargetPosition, 0.25f);
            //BuffPlugin.Log($"{buffCard.ID} _ targetPos : {TargetPosition.x},{TargetPosition.y} | {targetInPocketPos.x},{targetInPocketPos.y}");
            buffCard.Alpha = Mathf.Lerp(buffCard.Alpha, TargetScaleFactor, 0.25f);
        }

        public override void GrafUpdate(float timeStacker)
        {
            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);
            buffCard.Position = smoothPos;


            if (Mathf.Abs(buffCard.Rotation.x - targetRotation.x) > 0.01f || Mathf.Abs(buffCard.Rotation.y - targetRotation.y) > 0.01f)
                buffCard.Rotation = Vector3.Lerp(buffCard.Rotation, targetRotation, 0.1f);

            if (Mathf.Abs(buffCard.Scale - TargetScaleFactor * baseScale + ExternScale) > 0.01f)
                buffCard.Scale = Mathf.Lerp(buffCard.Scale, TargetScaleFactor * baseScale + ExternScale, 0.1f);


            var sprite = pocketInteractionManager.Slot.GetCorrectSymbol(buffCard);
            sprite.SetPosition(smoothPos + new Vector2(CardBasicAssets.RenderTextureSize.x, -CardBasicAssets.RenderTextureSize.y) * buffCard.Scale * symbolBiasScaleBound);
            sprite.alpha = Mathf.Lerp(sprite.alpha, TargetScaleFactor * pocketInteractionManager.GetCorrectSymbolAlpha(buffCard), 0.1f);
            sprite.scale = buffCard.Scale / BuffCard.normalScale;
        }

    }

    internal class CardPocketExclusiveAnimator : BuffCardAnimator
    {
        Vector2 targetPosition = Custom.rainWorld.screenSize / 2f;
        float targetScale = BuffCard.normalScale;

        Vector3 basicRotation = new Vector3(0f, 360f, 0f);
        Vector3 TargetRotation => basicRotation + (buffCard.CurrentFocused ? new Vector3((30f * buffCard.LocalMousePos.y - 15f) * (flip ? -1f : 1f), -(30f * buffCard.LocalMousePos.x - 15f), 0f) : Vector3.zero);

        bool flip;

        CardPocketInteratctionManager pocketInteractionManager;

        public CardPocketExclusiveAnimator(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            pocketInteractionManager = buffCard.interactionManager as CardPocketInteratctionManager;

            buffCard.Highlight = false;
            buffCard.DisplayDescription = false;
            buffCard.DisplayTitle = true;
            buffCard.DisplayAllGraphTexts = true;
            buffCard.KeyBinderFlash = false;
            buffCard.UpdateGrey();

            buffCard.UpdateGraphText();
            buffCard.StackerAddOne = false;
            if (buffCard.StaticData.Stackable && pocketInteractionManager.Slot.TryGetRep(buffCard.ID).stackCount > 0)
                buffCard.DisplayStacker = true;
            else
                buffCard.DisplayStacker = false;

            
            buffCard.onMouseSingleClick += OnSingleClickFlip;
        }

        public override void Update()
        {
            base.Update();

            if (buffCard.StaticData.Stackable && buffCard.StackerValue != pocketInteractionManager.Slot.TryGetRep(buffCard.ID).stackCount)
            {
                int stackCount = pocketInteractionManager.Slot.TryGetRep(buffCard.ID).stackCount;
                buffCard.StackerValue = stackCount;
                buffCard.DisplayStacker = stackCount > 0;
                buffCard._cardRenderer.cardCameraController.CardDirty = true;
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            if (Mathf.Abs(buffCard.Rotation.x - TargetRotation.x) > 0.01f || Mathf.Abs(buffCard.Rotation.y - TargetRotation.y) > 0.01f)
                buffCard.Rotation = Vector3.Lerp(buffCard.Rotation, TargetRotation, 0.1f);

            if (Mathf.Abs(buffCard.Position.x - targetPosition.x) > 0.01f || Mathf.Abs(buffCard.Position.y - targetPosition.y) > 0.01f)
                buffCard.Position = Vector2.Lerp(buffCard.Position, targetPosition, 0.1f);

            if (Mathf.Abs(buffCard.Scale - targetScale) > 0.01f)
                buffCard.Scale = Mathf.Lerp(buffCard.Scale, targetScale, 0.1f);

            if (Mathf.Abs(buffCard.Alpha - 1f) > 0.01f)
                buffCard.Alpha = Mathf.Lerp(buffCard.Alpha, 1f, 0.1f);

            var sprite = pocketInteractionManager.Slot.GetCorrectSymbol(buffCard);
            sprite.SetPosition(buffCard.Position + new Vector2(CardBasicAssets.RenderTextureSize.x, -CardBasicAssets.RenderTextureSize.y) * buffCard.Scale * CardPocketNormalAnimator.symbolBiasScaleBound);
            sprite.alpha = Mathf.Lerp(sprite.alpha, pocketInteractionManager.GetCorrectSymbolAlpha(buffCard), 0.1f);
            sprite.scale = buffCard.Scale / BuffCard.normalScale;
 
        }

        void OnSingleClickFlip()
        {
            if (!flip)
            {
                basicRotation = new Vector3(0, 180f + 360f, 0f);
                flip = true;
                buffCard.DisplayDescription = true;
            }
            else
            {
                basicRotation = new Vector3(0, 360f, 0f);
                flip = false;
                buffCard.DisplayDescription = false;
            }
        }

        public override void Destroy()
        {
            buffCard.onMouseSingleClick -= OnSingleClickFlip;
        }
    }

    internal class RoundRectSprites
    {
        private int SideSprite(int side)
        {
            return (this.filled ? 9 : 0) + side;
        }

        private int CornerSprite(int corner)
        {
            return (this.filled ? 9 : 0) + 4 + corner;
        }

        private int FillSideSprite(int side)
        {
            return side;
        }

        private int FillCornerSprite(int corner)
        {
            return 4 + corner;
        }

        private int MainFillSprite
        {
            get
            {
                return 8;
            }
        }

        FContainer Container;
        public Vector2 pos;
        Vector2 lastPos;
        public Vector2 size;
        Vector2 lastSize;

        public RoundRectSprites(FContainer ownerContainer, Vector2 pos, Vector2 size, bool filled = true)
        {
            Container = ownerContainer;
            this.pos = this.lastPos = pos;
            this.size = this.lastSize = size;
            this.filled = filled;
            this.sprites = new FSprite[filled ? 17 : 8];
            for (int i = 0; i < 4; i++)
            {
                this.sprites[this.SideSprite(i)] = new FSprite("pixel", true);
                this.sprites[this.SideSprite(i)].scaleY = 2f;
                this.sprites[this.SideSprite(i)].scaleX = 2f;
                this.sprites[this.CornerSprite(i)] = new FSprite("UIroundedCorner", true);
                if (filled)
                {
                    this.sprites[this.FillSideSprite(i)] = new FSprite("pixel", true);
                    this.sprites[this.FillSideSprite(i)].scaleY = 6f;
                    this.sprites[this.FillSideSprite(i)].scaleX = 6f;
                    this.sprites[this.FillCornerSprite(i)] = new FSprite("UIroundedCornerInside", true);
                }
            }
            this.sprites[this.SideSprite(0)].anchorY = 0f;
            this.sprites[this.SideSprite(2)].anchorY = 0f;
            this.sprites[this.SideSprite(1)].anchorX = 0f;
            this.sprites[this.SideSprite(3)].anchorX = 0f;
            this.sprites[this.CornerSprite(0)].scaleY = -1f;
            this.sprites[this.CornerSprite(2)].scaleX = -1f;
            this.sprites[this.CornerSprite(3)].scaleY = -1f;
            this.sprites[this.CornerSprite(3)].scaleX = -1f;
            if (filled)
            {
                this.sprites[this.MainFillSprite] = new FSprite("pixel", true);
                this.sprites[this.MainFillSprite].anchorY = 0f;
                this.sprites[this.MainFillSprite].anchorX = 0f;
                this.sprites[this.FillSideSprite(0)].anchorY = 0f;
                this.sprites[this.FillSideSprite(2)].anchorY = 0f;
                this.sprites[this.FillSideSprite(1)].anchorX = 0f;
                this.sprites[this.FillSideSprite(3)].anchorX = 0f;
                this.sprites[this.FillCornerSprite(0)].scaleY = -1f;
                this.sprites[this.FillCornerSprite(2)].scaleX = -1f;
                this.sprites[this.FillCornerSprite(3)].scaleY = -1f;
                this.sprites[this.FillCornerSprite(3)].scaleX = -1f;
                for (int j = 0; j < 9; j++)
                {
                    this.sprites[j].color = new Color(0f, 0f, 0f);
                    this.sprites[j].alpha = 0.75f;
                }
            }
            for (int k = 0; k < this.sprites.Length; k++)
            {
                this.Container.AddChild(this.sprites[k]);
            }
            if (filled)
                fillAlpha = lasFillAplha = 1f;

            try
            {
                this.Update();
                this.GrafUpdate(0f);
            }
            catch
            {
            }
        }

        public void Update()
        {
            this.lasFillAplha = this.fillAlpha;
            this.lastAddSize = this.addSize;
            lastPos = pos;
            lastSize = size;
        }

        public void GrafUpdate(float timeStacker)
        {
            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);
            Vector2 size = Vector2.Lerp(lastSize, this.size, timeStacker);
            smoothPos -= Vector2.Lerp(this.lastAddSize, this.addSize, timeStacker) / 2f;
            size += Vector2.Lerp(this.lastAddSize, this.addSize, timeStacker);
            smoothPos.x = Mathf.Floor(smoothPos.x) + 0.41f;
            smoothPos.y = Mathf.Floor(smoothPos.y) + 0.41f;
            this.sprites[this.SideSprite(0)].x = smoothPos.x + 1f;
            this.sprites[this.SideSprite(0)].y = smoothPos.y + 7f;
            this.sprites[this.SideSprite(0)].scaleY = size.y - 14f;
            this.sprites[this.SideSprite(1)].x = smoothPos.x + 7f;
            this.sprites[this.SideSprite(1)].y = smoothPos.y + size.y - 1f;
            this.sprites[this.SideSprite(1)].scaleX = size.x - 14f;
            this.sprites[this.SideSprite(2)].x = smoothPos.x + size.x - 1f;
            this.sprites[this.SideSprite(2)].y = smoothPos.y + 7f;
            this.sprites[this.SideSprite(2)].scaleY = size.y - 14f;
            this.sprites[this.SideSprite(3)].x = smoothPos.x + 7f;
            this.sprites[this.SideSprite(3)].y = smoothPos.y + 1f;
            this.sprites[this.SideSprite(3)].scaleX = size.x - 14f;
            this.sprites[this.CornerSprite(0)].x = smoothPos.x + 3.5f;
            this.sprites[this.CornerSprite(0)].y = smoothPos.y + 3.5f;
            this.sprites[this.CornerSprite(1)].x = smoothPos.x + 3.5f;
            this.sprites[this.CornerSprite(1)].y = smoothPos.y + size.y - 3.5f;
            this.sprites[this.CornerSprite(2)].x = smoothPos.x + size.x - 3.5f;
            this.sprites[this.CornerSprite(2)].y = smoothPos.y + size.y - 3.5f;
            this.sprites[this.CornerSprite(3)].x = smoothPos.x + size.x - 3.5f;
            this.sprites[this.CornerSprite(3)].y = smoothPos.y + 3.5f;
            Color color = new Color(1f, 1f, 1f);
            if (this.borderColor != null)
            {
                color = this.borderColor.Value;
            }
            for (int i = 0; i < 4; i++)
            {
                this.sprites[this.SideSprite(i)].color = color;
                this.sprites[this.CornerSprite(i)].color = color;
            }
            if (this.filled)
            {
                this.sprites[this.FillSideSprite(0)].x = smoothPos.x + 4f;
                this.sprites[this.FillSideSprite(0)].y = smoothPos.y + 7f;
                this.sprites[this.FillSideSprite(0)].scaleY = size.y - 14f;
                this.sprites[this.FillSideSprite(1)].x = smoothPos.x + 7f;
                this.sprites[this.FillSideSprite(1)].y = smoothPos.y + size.y - 4f;
                this.sprites[this.FillSideSprite(1)].scaleX = size.x - 14f;
                this.sprites[this.FillSideSprite(2)].x = smoothPos.x + size.x - 4f;
                this.sprites[this.FillSideSprite(2)].y = smoothPos.y + 7f;
                this.sprites[this.FillSideSprite(2)].scaleY = size.y - 14f;
                this.sprites[this.FillSideSprite(3)].x = smoothPos.x + 7f;
                this.sprites[this.FillSideSprite(3)].y = smoothPos.y + 4f;
                this.sprites[this.FillSideSprite(3)].scaleX = size.x - 14f;
                this.sprites[this.FillCornerSprite(0)].x = smoothPos.x + 3.5f;
                this.sprites[this.FillCornerSprite(0)].y = smoothPos.y + 3.5f;
                this.sprites[this.FillCornerSprite(1)].x = smoothPos.x + 3.5f;
                this.sprites[this.FillCornerSprite(1)].y = smoothPos.y + size.y - 3.5f;
                this.sprites[this.FillCornerSprite(2)].x = smoothPos.x + size.x - 3.5f;
                this.sprites[this.FillCornerSprite(2)].y = smoothPos.y + size.y - 3.5f;
                this.sprites[this.FillCornerSprite(3)].x = smoothPos.x + size.x - 3.5f;
                this.sprites[this.FillCornerSprite(3)].y = smoothPos.y + 3.5f;
                this.sprites[this.MainFillSprite].x = smoothPos.x + 7f;
                this.sprites[this.MainFillSprite].y = smoothPos.y + 7f;
                this.sprites[this.MainFillSprite].scaleX = size.x - 14f;
                this.sprites[this.MainFillSprite].scaleY = size.y - 14f;
                for (int j = 0; j < 9; j++)
                {
                    this.sprites[j].alpha = Mathf.Lerp(this.lasFillAplha, this.fillAlpha, timeStacker);
                }
            }
        }

        public void RemoveSprites()
        {
            for (int i = 0; i < this.sprites.Length; i++)
            {
                sprites[i].RemoveFromContainer();
            }
        }

        public Vector2 addSize;

        public Vector2 lastAddSize;

        public float fillAlpha;

        private float lasFillAplha;

        public FSprite[] sprites;

        private bool filled;

        public Color? borderColor;
    }

    internal class HalfRectSprites
    {
        FSprite[] sideSprites;
        FSprite[] cornerSprites;
        FSprite[] fillSideSprite;
        FSprite[] fillCornerSprite;
        FSprite fillSprite;

        Vector2 dir;
        Vector2 perpDir;

        public Vector2 size, lastSize;
        public Vector2 pos, lastPos;
        public Color borderColor;

        public float rotation
        {
            get => Custom.VecToDeg(dir);
            set
            {
                dir = Custom.DegToVec(value);
                perpDir = Custom.PerpendicularVector(dir);
            }
        }

        public HalfRectSprites(FContainer ownerContainer, Vector2 pos, Vector2 size, Color borderColor, float rotation = 90f)
        {
            lastPos = this.pos = pos;
            lastSize = this.size = size;
            this.rotation = rotation;
            this.borderColor = borderColor;

            sideSprites = new FSprite[3];
            cornerSprites = new FSprite[2];
            fillSideSprite = new FSprite[3];
            fillCornerSprite = new FSprite[2];
            for (int i = 0; i < 3; i++)
            {
                sideSprites[i] = new FSprite("pixel", true) {anchorX = 0f, anchorY = 1f, scaleY = 2f, scaleX = 2f};
                ownerContainer.AddChild(sideSprites[i]);
                
                fillSideSprite[i] = new FSprite("pixel", true) { anchorX = 0f, anchorY = 1f, scaleX = 5f, scaleY = 5f, color = Color.black};
                ownerContainer.AddChild(fillSideSprite[i]);
            }
            for(int i = 0;i < 2; i++)
            {
                cornerSprites[i] = new FSprite("UIroundedCorner", true) {  rotation = rotation};
                fillCornerSprite[i] = new FSprite("UIroundedCornerInside", true) { color = Color.black, alpha = 1f};

                ownerContainer.AddChild(cornerSprites[i]);
                ownerContainer.AddChild(fillCornerSprite[i]);
            }

            //sideSprites[0].anchorY = 0f;
            sideSprites[0].rotation = rotation - 180f;
            //sideSprites[2].anchorY = 0f;
            sideSprites[2].rotation = rotation - 180f;
            sideSprites[2].anchorY = 0f;
            sideSprites[2].anchorX = 0f;
            //sideSprites[1].anchorX = 0f;
            sideSprites[1].rotation = rotation - 90f;


            cornerSprites[0].scaleY = -1f;
            cornerSprites[0].rotation = rotation;
            cornerSprites[1].scaleY = -1f;
            cornerSprites[1].rotation = rotation + 90f;

            fillSprite = new FSprite("pixel", true) { anchorX = 0f, anchorY = 0f, rotation = rotation - 90f, color = Color.black };
            ownerContainer.AddChild(fillSprite);

            fillSideSprite[0].rotation = rotation - 180f;
            fillSideSprite[2].rotation = rotation - 180f;
            fillSideSprite[2].anchorY = 0f;
            fillSideSprite[2].anchorX = 0f;
            fillSideSprite[2].scaleX = fillSideSprite[2].scaleY = 6f;
            fillSideSprite[1].rotation = rotation - 90f;
            fillSideSprite[1].anchorX = 0f;
            fillSideSprite[1].anchorY = 0f;

            fillCornerSprite[0].scaleY = -1f;
            fillCornerSprite[0].rotation = rotation;
            fillCornerSprite[1].scaleY = -1f;
            fillCornerSprite[1].rotation = rotation + 90f;

            try
            {
                this.Update();
                this.GrafUpdate(0f);
            }
            catch
            {
            }
        }

        public void Update()
        {
            lastPos = pos;
            lastSize = size;
        }

        public Vector2 LocalToGlobal(float x, float y)
        {
            return dir * x + perpDir * y;
        }

        public void GrafUpdate(float timeStacker)
        {
            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);
            Vector2 size = Vector2.Lerp(lastSize, this.size, timeStacker);
            smoothPos.x = Mathf.Floor(smoothPos.x) + 0.41f;
            smoothPos.y = Mathf.Floor(smoothPos.y) + 0.41f;

            sideSprites[0].SetPosition(smoothPos + LocalToGlobal(1f, 0f));
            sideSprites[0].scaleX = size.y - 7f;

            sideSprites[1].SetPosition(smoothPos + LocalToGlobal(7f, size.y));
            sideSprites[1].scaleX = size.x - 14f;

            sideSprites[2].SetPosition(smoothPos + LocalToGlobal(size.x, 0f));
            sideSprites[2].scaleX = size.y - 7f;

            cornerSprites[0].SetPosition(smoothPos + LocalToGlobal(3.5f, size.y - 3.5f));
            cornerSprites[1].SetPosition(smoothPos + LocalToGlobal(size.x - 3.5f, size.y - 3.5f));

            for (int i = 0; i < 3; i++)
                sideSprites[i].color = borderColor;
            for (int i = 0;i < 2;i++)
                cornerSprites[i].color = borderColor;

            fillSideSprite[0].SetPosition(smoothPos + LocalToGlobal(4f - 0.5f, 0f));
            fillSideSprite[0].scaleX = size.y - 7f;

            fillSideSprite[1].SetPosition(smoothPos + LocalToGlobal(7f, size.y - 7f));
            fillSideSprite[1].scaleX = size.x - 14f;

            fillSideSprite[2].SetPosition(smoothPos + LocalToGlobal(size.x - 4f + 1f, 0f));
            fillSideSprite[2].scaleX = size.y - 7f;

            fillCornerSprite[0].SetPosition(smoothPos + LocalToGlobal(3.5f, size.y - 3.5f));
            fillCornerSprite[1].SetPosition(smoothPos + LocalToGlobal(size.x - 3.5f, size.y - 3.5f));

            fillSprite.SetPosition(smoothPos + LocalToGlobal(7f, 0f));

            fillSprite.scaleX = size.x - 14f;
            fillSprite.scaleY = size.y - 7f;
        }
    }

    internal class SideSingleSelectButton
    {
        public static float defaultHeight = 40f;
        public static float extendWidth = 10f;
        public static float selectedWidth = 55f;

        internal HalfRectSprites rect;
        FSprite darkSprite;
        FLabel title;
        string signal;
        public Action<SideSingleSelectButton> groupSelectAction;
        public Action<string> selectedAction;

        float width;

        Vector2 dir;
        Vector2 perpDir;

        float height;
        float lastHeight;

        internal Vector2 pos;//TopRight;
        Vector2 lastPos;

        bool lastMouseInside;
        public bool mouseInside;
        public bool enableInput = true;

        public bool selected;

        TickAnimCmpnt heightChangeAnim;
        float initHeight;
        float targetHeight;

        float TargetHeight => (selected ? selectedWidth : defaultHeight) + (mouseInside ? extendWidth : 0f);
        public float rotation
        {
            get => Helper.VecToDeg(dir);
            set
            {
                dir = Custom.DegToVec(value);
                perpDir = Custom.DegToVec(value - 90f);
            }
        }


        public SideSingleSelectButton(FContainer ownerContainer, Vector2 bottomLeftAnchorPos, float width, string name, string signal, float rotation = 0f)
        {
            this.width = width;
            this.signal = signal;
            this.rotation = rotation + 90f;
            height = lastHeight = defaultHeight;
            lastPos = pos = bottomLeftAnchorPos;

            rect = new HalfRectSprites(ownerContainer, bottomLeftAnchorPos /*+ LocalToGlobal(width, 0f)*/, new Vector2(width, height), Color.white, rotation + 90f);
            darkSprite = new FSprite("LinearGradient200") { scaleX = width + 2f, rotation = rotation, scaleY = 0.1f , anchorX = 0f, anchorY = 0f, color = Color.black,};
            title = new FLabel(Custom.GetDisplayFont(), name) { rotation = rotation, anchorX = 0.5f, anchorY = 0f };

            ownerContainer.AddChild(darkSprite);
            ownerContainer.AddChild(title);
        }

        public void Update()
        {
            rect.Update();

            lastMouseInside = mouseInside;
            mouseInside = false;
            Vector2 delta = InputAgency.Current.GetMousePosition() - pos;
            float x = Vector2.Dot(dir, delta);
            float y = Vector2.Dot(perpDir, delta);

            if (x > 0f && x < width && y > 0 && y < height)
                mouseInside = true;

            if (!lastMouseInside && mouseInside)
            {
                FireUpHeightChangeAnim();
            }
            else if(lastMouseInside && !mouseInside)
            {
                FireUpHeightChangeAnim();
            }
            rect.pos = pos /*+ LocalToGlobal(width, 0f)*/;
            rect.size.y = height;
        }

        void FireUpHeightChangeAnim()
        {
            if (heightChangeAnim != null)
                heightChangeAnim.Destroy();

            initHeight = height;
            targetHeight = TargetHeight;

            heightChangeAnim = AnimMachine.GetTickAnimCmpnt(0, 10, autoDestroy: true).BindActions(OnAnimUpdate: HeightChangeAnimUpdateFunc, OnAnimFinished: HeightChangeAnimFinishFunc).BindModifier(Helper.LerpEase);
        }

        void HeightChangeAnimFinishFunc(TickAnimCmpnt tickAnimCmpnt)
        {
            height = targetHeight;
            heightChangeAnim = null;
        }

        void HeightChangeAnimUpdateFunc(TickAnimCmpnt tickAnimCmpnt)
        {
            height = Mathf.Lerp(initHeight, targetHeight, tickAnimCmpnt.Get());
        }

        public void OnMouseLeftClick()
        {
            if(mouseInside && !selected && enableInput)
            {
                selected = true;
                groupSelectAction?.Invoke(this);
                selectedAction?.Invoke(signal);
                FireUpHeightChangeAnim();
            }
        }

        public void SetSelected(bool selected)
        {
            this.selected = selected;
            FireUpHeightChangeAnim();
        }

        public void GrafUpdate(float timeStacker)
        {
            rect.GrafUpdate(timeStacker);
            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);
            darkSprite.SetPosition(smoothPos + LocalToGlobal(1f, -1f));
            title.SetPosition(smoothPos + LocalToGlobal(width / 2f, CardPocket.gap / 2f));
        }

        public Vector2 LocalToGlobal(float x, float y)
        {
            return dir * x + perpDir * y;
        }


        public Vector2 MiddleOfButton()
        {
            return pos + LocalToGlobal(width / 2f, CardPocket.gap / 2f);
        }
    }

    internal class TitleRect
    {
        HalfRectSprites rect;
        FSprite darkSprite;
        FLabel title;

        float width;
        float lastWidth;

        Vector2 pos;//TopRight;
        Vector2 lastPos;

        TickAnimCmpnt widthChangeAnim;
        float initWidth;
        float targetWidth;

        public TitleRect(FContainer ownerContainer, string text, Vector2 bottomLeftPos, float height)
        {
            lastPos = pos = bottomLeftPos;
            width = lastWidth = LabelTest.GetWidth(text, true) + CardPocket.gap * 2f;

            rect = new HalfRectSprites(ownerContainer, bottomLeftPos, new Vector2(width, height), Color.white, 90f);
            darkSprite = new FSprite("LinearGradient200") { scaleX = height + 4f, scaleY = 0.1f, anchorX = 0f, anchorY = 0f, color = Color.black };
            title = new FLabel(Custom.GetDisplayFont(), text) { anchorX = 0.5f, anchorY = 0f };

            ownerContainer.AddChild(darkSprite);
            ownerContainer.AddChild(title);
        }

        public void SetTitle(string newTitle)
        {
            title.text = newTitle;
            FireUpWidthChangeAnim();
        }

        void FireUpWidthChangeAnim()
        {
            if (widthChangeAnim != null)
                widthChangeAnim.Destroy();

            initWidth = width;
            targetWidth = LabelTest.GetWidth(title.text, true) + CardPocket.gap * 2f;

            widthChangeAnim = AnimMachine.GetTickAnimCmpnt(0, 10, autoDestroy: true).BindActions(OnAnimUpdate: WidthChangeAnimUpdateFunc, OnAnimFinished: WidthChangeAnimFinishFunc).BindModifier(Helper.LerpEase);
        }

        void WidthChangeAnimFinishFunc(TickAnimCmpnt tickAnimCmpnt)
        {
            width = targetWidth;
            widthChangeAnim = null;
        }

        void WidthChangeAnimUpdateFunc(TickAnimCmpnt tickAnimCmpnt)
        {
            width = Mathf.Lerp(initWidth, targetWidth, tickAnimCmpnt.Get());
        }

        public void Update()
        {
            lastWidth = width;
            lastPos = pos;

            rect.Update();
            rect.pos = pos;
            rect.size.x = width;
        }

        public void GrafUpdate(float timeStacker)
        {
            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);
            darkSprite.SetPosition(smoothPos + new Vector2(2f, 0f));
            title.SetPosition(smoothPos + new Vector2(width / 2f, CardPocket.gap / 2f));
        }
    }
}
