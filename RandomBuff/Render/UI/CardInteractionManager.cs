using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI
{
    internal abstract class CardInteractionManager
    {
        //常量
        const int Double_ClickThreashold = 10;//单击双击阈值（逻辑帧） 

        //卡牌信息
        public virtual BuffCard CurrentFocusCard { get; protected set; }
        protected List<BuffCard> managedCards = new List<BuffCard>();

        //状态变量
        bool slateForDeletion;
        CardInteractionManager _overrideManager;
        public CardInteractionManager SubManager//控制覆盖，用于管理多个交互系统。该变量表示对方为低一级的交互系统
        {
            get => _overrideManager;
            set
            {             
                if (value == null)
                    _overrideManager.overrideDisabled = false;
                _overrideManager = value;
            }
        }
        public bool overrideDisabled;//是否被高级交互系统禁用

        public Vector2 MousePos => new Vector2(Futile.mousePosition.x, Futile.mousePosition.y);

        protected Helper.InputButtonTracker mouseLeftButtonTracker = new Helper.InputButtonTracker(() => Input.GetMouseButton(0), true, true, Double_ClickThreashold);

        protected bool enableMouseInput = true;

        public virtual void Update()
        {
            mouseLeftButtonTracker.Update(out bool singleClick, out bool doubleClick);
            if(enableMouseInput && !overrideDisabled)
            {
                if(singleClick) 
                    OnMouseSingleClick();
                if(doubleClick)
                    OnMouseDoubleClick();
            }
            
            for(int i = managedCards.Count - 1; i >= 0; i--)
            {
                managedCards[i].Update();
            }

            UpdateFocusCard();

            if(SubManager != null && SubManager.slateForDeletion)
            {
                SubManager = null;
            }
        }

        public virtual void GrafUpdate()
        {
            for (int i = managedCards.Count - 1; i >= 0; i--)
            {
                managedCards[i].GrafUpdate();
            }
        }

        protected virtual void OnMouseSingleClick()
        {

        }

        protected virtual void OnMouseDoubleClick()
        {

        }

        protected virtual void UpdateFocusCard()
        {

        }

        public virtual void ManageCard(BuffCard card)
        {
            if(card.interactionManager != null)
                card.interactionManager.DismanageCard(card);

            managedCards.Add(card);
            card.interactionManager = this;
        }

        public virtual void DismanageCard(BuffCard card)
        {
            managedCards.Remove(card);
            card.interactionManager = null;
        }

        public virtual void Destroy()
        {
            slateForDeletion = true;
            SubManager = null;

            foreach(var card in managedCards)
            {
                card.Destroy();
            }
            managedCards.Clear();
        }
    }

    internal class TestBasicInteractionManager : CardInteractionManager
    {
        protected override void UpdateFocusCard()
        {
            foreach(var card in managedCards)
            {
                if(card.LocalMousePos.x > 0 && card.LocalMousePos.x < 1f && card.LocalMousePos.y > 0f && card.LocalMousePos.y < 1f)
                {
                    CurrentFocusCard = card;
                    CurrentFocusCard.SetAnimataorState(BuffCard.AnimatorState.Test_MousePreview);
                    return;
                }
            }

            CurrentFocusCard?.SetAnimataorState(BuffCard.AnimatorState.Test_None);
            CurrentFocusCard = null;
        }

        protected override void OnMouseSingleClick()
        {
            CurrentFocusCard?.OnMouseSingleClick();
        }

        protected override void OnMouseDoubleClick()
        {
            CurrentFocusCard?.OnMouseDoubleClick();
        }
    }

    internal class InGameSlotInteractionManager : CardInteractionManager
    {
        //静态信息
        public static KeyCode ToggleShowButton = KeyCode.Tab;
        public static int maxCardBiasCount = 0;

        public InGameBuffCardSlot Slot { get; private set; }

        public override BuffCard CurrentFocusCard 
        { 
            get => base.CurrentFocusCard; 
            protected set
            {
                if(value != CurrentFocusCard && value != null)
                {
                    if(value.StaticData.BuffType == Core.Buff.BuffType.Positive)
                    {
                        int bias = PositiveBuffCards.IndexOf(value) - PositiveShowIndex;
                        int absBias = Mathf.Abs(bias);
                        if(absBias > maxCardBiasCount)
                        {
                            PositiveShowIndex += (absBias - maxCardBiasCount) * (bias > 0 ? 1 : -1);
                        }
                    }
                    else
                    {
                        int bias = NegativeBuffCards.IndexOf(value) - NegativeShowIndex;
                        int absBias = Mathf.Abs(bias);
                        if (absBias > maxCardBiasCount)
                        {
                            NegativeShowIndex += (absBias - maxCardBiasCount) * (bias > 0 ? 1 : -1);
                        }
                    }
                }
                base.CurrentFocusCard = value;
            }
        }

        State currentState;

        public int PositiveShowIndex { get; protected set; }
        public int NegativeShowIndex { get; protected set; }

        BuffCard exclusiveShowCard;
        public List<BuffCard> PositiveBuffCards { get; } = new List<BuffCard>();
        public List<BuffCard> NegativeBuffCards { get; } = new List<BuffCard>();

        Helper.InputButtonTracker toggleShowButtonTracker = new Helper.InputButtonTracker(() => Input.GetKey(ToggleShowButton), false);
        Helper.InputButtonTracker mouseButtonRightTracker = new Helper.InputButtonTracker(() => Input.GetMouseButton(1), false);

        public InGameSlotInteractionManager(InGameBuffCardSlot slot)
        {
            Slot = slot;
        }

        public override void Update()
        {
            toggleShowButtonTracker.Update(out bool showButtonSingle, out bool _);
            mouseButtonRightTracker.Update(out bool mouseRightSingle, out bool _);

            if (showButtonSingle && !overrideDisabled)
                OnToggleShowButtonSingleClick();

            if (mouseRightSingle && !overrideDisabled)
                OnMouseRightSingleClick();

            base.Update();
        }

        protected override void UpdateFocusCard()
        {
            if(overrideDisabled)
            {
                if (CurrentFocusCard != null)
                    CurrentFocusCard = null;
                return;
            }

            if(currentState == State.Show)
            {
                foreach(var card in managedCards)
                {
                    if (card.LocalMousePos.x > 0 && 
                        card.LocalMousePos.x < 1f && 
                        card.LocalMousePos.y > 0f &&
                        card.LocalMousePos.y < 1f)
                    {
                        CurrentFocusCard = card;
                        return;
                    }
                }

                if (CurrentFocusCard != null) 
                    CurrentFocusCard = null;
            }
            else if(currentState == State.ExclusiveShow)
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
            else if(currentState == State.Hide)
            {
                if(CurrentFocusCard != null)
                    CurrentFocusCard = null;
            }
        }

        public override void ManageCard(BuffCard card)
        {
            base.ManageCard(card);

            if(card.StaticData.BuffType == Core.Buff.BuffType.Positive)
                PositiveBuffCards.Add(card);
            else
                NegativeBuffCards.Add(card);

            if (currentState == State.Hide)
                card.SetAnimataorState(BuffCard.AnimatorState.InGameSlot_Hide);
            else if (currentState == State.Show)
                card.SetAnimataorState(BuffCard.AnimatorState.InGameSlot_Show);
        }

        public override void DismanageCard(BuffCard card)
        {
            base.DismanageCard(card);

            if (card.StaticData.BuffType == Core.Buff.BuffType.Positive)
                PositiveBuffCards.Remove(card);
            else
                NegativeBuffCards.Remove(card);
        }

        public int IndexInManagedCards(BuffCard card)
        {
            return managedCards.IndexOf(card);
        }

        public int IndexInGroupedCards(BuffCard card)
        {
            if(card.StaticData.BuffType == Core.Buff.BuffType.Positive)
                return PositiveBuffCards.IndexOf(card);
            return NegativeBuffCards.IndexOf(card);
        }

        public int IndexBiasInGroupedCards(BuffCard card)
        {
            if (card.StaticData.BuffType == Core.Buff.BuffType.Positive)
                return PositiveBuffCards.IndexOf(card) - PositiveShowIndex;
            return NegativeBuffCards.IndexOf(card) - NegativeShowIndex;
        }

        protected void OnToggleShowButtonSingleClick()
        {
            if (currentState == State.Hide)
                SetState(State.Show);
            else if (currentState == State.Show || currentState == State.ExclusiveShow)
                SetState(State.Hide);
        }

        protected void OnMouseRightSingleClick()
        {
            if (currentState == State.ExclusiveShow)
            {
                Slot.RecoverCardSort(exclusiveShowCard);
                exclusiveShowCard = null;
                SetState(State.Show);
            }
        }

        protected override void OnMouseSingleClick()
        {
            if(currentState == State.Show)
            {
                if (CurrentFocusCard != null)
                {
                    Slot.BringToTop(CurrentFocusCard);
                    exclusiveShowCard = CurrentFocusCard;
                    SetState(State.ExclusiveShow);
                }
            }
            else if(currentState == State.ExclusiveShow)
            {
                exclusiveShowCard.OnMouseSingleClick();
            }
        }

        public void SetState(State newState)
        {
            if (currentState == newState)
                return;

            currentState = newState;
            if(newState == State.Hide)
            {
                Slot.BackDark = false;
                Slot.FrontDark = false;
                foreach (var card in managedCards)
                    card.SetAnimataorState(BuffCard.AnimatorState.InGameSlot_Hide);

                if (SubManager != null) SubManager.overrideDisabled = false;
            }
            else if(newState == State.Show)
            {
                Slot.BackDark = true;
                Slot.FrontDark = false;
                foreach (var card in managedCards)
                    card.SetAnimataorState(BuffCard.AnimatorState.InGameSlot_Show);
                if (SubManager != null) SubManager.overrideDisabled = true;
            }
            else if(newState == State.ExclusiveShow)
            {
                Slot.FrontDark = true;
                exclusiveShowCard.SetAnimataorState(BuffCard.AnimatorState.InGameSlot_Exclusive_Show);

                if (SubManager != null) SubManager.overrideDisabled = true;
            }
        }


        public enum State
        {
            Hide,
            Show,
            ExclusiveShow
        }
    }

    internal class CardPickerInteractionManager : CardInteractionManager
    {
        public static int maxCardBiasCount = 1;

        bool finishSelection;

        public CardPickerSlot Slot { get; private set; }

        public List<BuffCard> MajorCard { get; private set; } = new List<BuffCard>();
        public List<BuffCard> AdditionalCard { get; private set; } = new List<BuffCard>();

        public Dictionary<BuffCard, BuffCard> Additional2MajorMapper { get; private set; } = new Dictionary<BuffCard, BuffCard>();
        public Dictionary<BuffCard, BuffCard> Major2AdditionalMapper { get; private set; } = new Dictionary<BuffCard, BuffCard>();

        public int CardShowIndex { get; private set; }

        public override BuffCard CurrentFocusCard
        {
            get => base.CurrentFocusCard;
            protected set
            {
                if (value != CurrentFocusCard && value != null)
                {
                    int bias = GetCardShowIndex(value, out int _);
                    int absBias = Mathf.Abs(bias);
                    if (absBias > maxCardBiasCount)
                    {
                        CardShowIndex += (absBias - maxCardBiasCount) * (bias > 0 ? 1 : -1);
                        BuffPlugin.Log($"focus on {value.ID}, make show index to {CardShowIndex}, bias {bias}, absBias{absBias}");
                    }
                }
                base.CurrentFocusCard = value;
            }
        }

        public CardPickerInteractionManager(CardPickerSlot slot)
        {
            Slot = slot;
        }

        public void FinishManage()
        {
            foreach(var card in managedCards)
            {
                card.SetAnimataorState(BuffCard.AnimatorState.CardPicker_Show);
            }
            CardShowIndex = Mathf.CeilToInt(MajorCard.Count / 2f);
        }

        public void FinishSelection()
        {
            finishSelection = true;
            foreach(var card in managedCards)
            {
                card.SetAnimataorState(BuffCard.AnimatorState.CardPicker_Disappear);
            }
        }

        public void ManageMajorCard(BuffCard card)
        {
            ManageCard(card);
            MajorCard.Add(card);

            BuffPlugin.Log($"Manage major : {card.ID}");
        }

        public void ManageAddtionalCard(BuffCard card, BuffCard linkedMajorCard)
        {
            ManageCard(card);
            AdditionalCard.Add(linkedMajorCard);

            Additional2MajorMapper.Add(card, linkedMajorCard);
            Major2AdditionalMapper.Add(linkedMajorCard, card);

            BuffPlugin.Log($"Manage additional : {card.ID} linked with : {linkedMajorCard.ID}");
        }

        public override void DismanageCard(BuffCard card)
        {
            if (MajorCard.Contains(card))
                MajorCard.Remove(card);

            if (AdditionalCard.Contains(card))
                Additional2MajorMapper.Remove(card);
            base.DismanageCard(card);
        }

        /// <summary>
        /// 获取卡牌的聚焦坐标
        /// </summary>
        /// <param name="buffCard"></param>
        /// <param name="majorOrAdditional">为0时为普通抽卡，为1时为加强卡主卡，为-1时为加强卡附卡</param>
        /// <returns></returns>
        public int GetCardShowIndex(BuffCard buffCard, out int majorOrAdditional)
        {
            return GetCardIndex(buffCard, out majorOrAdditional) - CardShowIndex;
        }

        public int GetCardIndex(BuffCard buffCard, out int majorOrAdditional)
        {
            if (Additional2MajorMapper.TryGetValue(buffCard, out var major))
            {
                majorOrAdditional = -1;
                return GetCardIndex(major, out var _);
            }
            if (Major2AdditionalMapper.TryGetValue(buffCard, out var _))
            {
                majorOrAdditional = 1;
            }
            else
                majorOrAdditional = 0;
            return MajorCard.IndexOf(buffCard);
        }

        protected override void UpdateFocusCard()
        {
            if(overrideDisabled || finishSelection)
            {
                if (CurrentFocusCard != null)
                    CurrentFocusCard = null;
                return;
            }

            foreach (var card in managedCards)
            {
                if (card.LocalMousePos.x > 0 &&
                    card.LocalMousePos.x < 1f &&
                    card.LocalMousePos.y > 0f &&
                    card.LocalMousePos.y < 1f)
                {
                    CurrentFocusCard = card;
                    return;
                }
            }

            if (CurrentFocusCard != null)
                CurrentFocusCard = null;
        }

        protected override void OnMouseSingleClick()
        {
            if(CurrentFocusCard != null)
            {
                CurrentFocusCard.OnMouseSingleClick();
                if (Major2AdditionalMapper.TryGetValue(CurrentFocusCard, out var additional))
                    additional.OnMouseSingleClick();

                if (Additional2MajorMapper.TryGetValue(CurrentFocusCard, out var major))
                    major.OnMouseSingleClick();
            }
        }

        protected override void OnMouseDoubleClick()
        {
            if(CurrentFocusCard != null)
            {
                CurrentFocusCard.OnMouseDoubleClick();
                Slot.CardPicked(CurrentFocusCard);

                if (Major2AdditionalMapper.TryGetValue(CurrentFocusCard, out var additional))
                {
                    additional.OnMouseDoubleClick();
                    Slot.CardPicked(additional);
                }

                if (Additional2MajorMapper.TryGetValue(CurrentFocusCard, out var major))
                {
                    major.OnMouseDoubleClick();
                    Slot.CardPicked(additional);
                }
            }
        }
    }
}
