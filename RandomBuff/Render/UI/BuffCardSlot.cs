using RandomBuff.Core.Buff;
using RandomBuff.Core.BuffMenu;
using RandomBuff.Core.SaveData;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI
{
    internal class BuffCardSlot
    {
        public CardInteractionManager BaseInteractionManager { get; protected set; }
        public FContainer Container { get; protected set; }

        public List<BuffCard> BuffCards { get; protected set; }

        public BuffCardSlot()
        {
            Container = new FContainer();
            BuffCards = new List<BuffCard>();
        }

        public virtual void Update()
        {
            BaseInteractionManager.Update();
        }

        public virtual void GrafUpdate(float timeStacker)
        {
            BaseInteractionManager.GrafUpdate(timeStacker);
        }

        public virtual void AppendCard(BuffCard buffCard)
        {
            BaseInteractionManager.ManageCard(buffCard);
            BuffCards.Add(buffCard);
            Container.AddChild(buffCard.Container);
        }

        public virtual BuffCard AppendCard(BuffID buffID)
        {
            var result = new BuffCard(buffID);
            AppendCard(result);
            return result;
        }

        public virtual void RemoveCard(BuffCard buffCard)
        {
            BaseInteractionManager.DismanageCard(buffCard);
            BuffCards.Remove(buffCard);
        }

        public void ForceRemoveCard_DEBUG(BuffID buffID)
        {
            var cardToRemove = GetCard(buffID);
            if (cardToRemove != null)
            {
                RemoveCard(cardToRemove);
                cardToRemove.Destroy();
            }
            else
            {
                BuffPlugin.LogWarning($"Buff {buffID} not exist in buffcardslot");
            }
        }

        public virtual void RemoveCard(BuffID buffID)
        {
            var cardToRemove = GetCard(buffID);
            if(cardToRemove != null)
            {
                RemoveCard(cardToRemove);
            }
            else
            {
                BuffPlugin.LogWarning($"Buff {buffID} not exist in buffcardslot");
            }
        }

        public BuffCard GetCard(BuffID buffID)
        {
            foreach(var card in BuffCards)
            {
                if(card.ID == buffID) return card;
            }
            return null;
        }
    
        public virtual void BringToTop(BuffCard buffCard)
        {
            Container.RemoveChild(buffCard.Container);
            Container.AddChild(buffCard.Container);
        }

        public virtual void RecoverCardSort(BuffCard buffCard)
        {
            Container.RemoveChild(buffCard.Container);
            Container.AddChildAtIndex(buffCard.Container, BuffCards.IndexOf(buffCard));
        }
    }

    internal class InGameBuffCardSlot : BuffCardSlot
    {
        FSprite darkMask_back;
        FSprite darkMaks_front;

        float targetBackDark;
        public bool BackDark
        {
            get => targetBackDark == 0.4f;
            set
            {
                targetBackDark = value ? 0.4f : 0f;
            }
        }

        float targetFrontDark;
        public bool FrontDark
        {
            get => targetFrontDark == 0.4f;
            set
            {
                targetFrontDark = value ? 0.4f : 0f;
            }
        }

        int CardStartIndex = 1;

        public InGameBuffCardSlot()
        {
            BaseInteractionManager = new InGameSlotInteractionManager(this);
            Container.AddChild(darkMask_back = new FSprite("pixel") { scaleX = Custom.rainWorld.screenSize.x, scaleY = Custom.rainWorld.screenSize.y, color = Color.black, alpha = 0f });
            Container.AddChild(darkMaks_front = new FSprite("pixel") { scaleX = Custom.rainWorld.screenSize.x, scaleY = Custom.rainWorld.screenSize.y, color = Color.black, alpha = 0f });
            darkMask_back.SetPosition(Custom.rainWorld.screenSize / 2f);
            darkMaks_front.SetPosition(Custom.rainWorld.screenSize / 2f);
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (Mathf.Abs(targetBackDark - darkMask_back.alpha) > 0.01f)
                darkMask_back.alpha = Mathf.Lerp(darkMask_back.alpha, targetBackDark, 0.1f);

            if (Mathf.Abs(targetFrontDark - darkMaks_front.alpha) > 0.01f)
                darkMaks_front.alpha = Mathf.Lerp(darkMaks_front.alpha, targetFrontDark, 0.1f);
        }

        public override void Update()
        {
            base.Update();
        }

        public override void AppendCard(BuffCard buffCard)
        {
            BaseInteractionManager.ManageCard(buffCard);
            BuffCards.Add(buffCard);
            Container.AddChildAtIndex(buffCard.Container, CardStartIndex);
        }

        public override void BringToTop(BuffCard buffCard)
        {
            darkMaks_front.MoveToFront();
            Container.RemoveChild(buffCard.Container);
            Container.AddChild(buffCard.Container);
        }

        public override void RecoverCardSort(BuffCard buffCard)
        {
            Container.RemoveChild(buffCard.Container);
            Container.AddChildAtIndex(buffCard.Container, BuffCards.IndexOf(buffCard) + CardStartIndex);
        }

    }

    internal class CardPickerSlot : BuffCardSlot
    {
        public InGameBuffCardSlot InGameBuffCardSlot { get; private set; }//可为null

        public BuffID[] majorSelections;
        public BuffID[] additionalSelections;

        Action<BuffID> selectCardCallBack;
        int numOfChoices;

        //状态变量
        int picedCount;

        public CardPickerSlot(InGameBuffCardSlot inGameBuffCardSlot, Action<BuffID> selectCardCallBack ,BuffID[] majorSelections, BuffID[] additionalSelections, int numOfChoices = 1)
        {
            this.majorSelections = majorSelections;
            this.additionalSelections = additionalSelections;

            this.selectCardCallBack = selectCardCallBack;
            this.numOfChoices = numOfChoices;
            InGameBuffCardSlot = inGameBuffCardSlot;

            BaseInteractionManager = new CardPickerInteractionManager(this);
            if(InGameBuffCardSlot != null)
            {
                InGameBuffCardSlot.BaseInteractionManager.SubManager = BaseInteractionManager;
            }

            for(int i = 0;i < majorSelections.Length;i++)
            {
                var major = new BuffCard(majorSelections[i]);
                major.Position = new Vector2(2000, -100);
                (BaseInteractionManager as CardPickerInteractionManager).ManageMajorCard(major);

                if (additionalSelections[i] != null)
                {
                    var additional = new BuffCard(additionalSelections[i]);
                    additional.Position = new Vector2(2000, -100);

                    BuffCards.Add(additional);
                    Container.AddChild(additional.Container);
                    (BaseInteractionManager as CardPickerInteractionManager).ManageAddtionalCard(additional, major);
                }

                BuffCards.Add(major);
                Container.AddChild(major.Container);
            }
            (BaseInteractionManager as CardPickerInteractionManager).FinishManage();
        }

        public void CardPicked(BuffCard card)
        {
            List<BuffCard> cards = new List<BuffCard>();
            for (int i = 0; i < majorSelections.Length; i++)
            {
                if (additionalSelections[i] == card.ID)
                {
                    cards.Add(card);
                    cards.Add(GetCardOfID(majorSelections[i]));
                }

                if (majorSelections[i] == card.ID)
                {
                    cards.Add(card);
                    if (additionalSelections[i] != null)
                        cards.Add(GetCardOfID(additionalSelections[i]));
                }
            }

            foreach(var buffCard in cards)
            {
                selectCardCallBack.Invoke(buffCard.ID);

                BuffCards.Remove(buffCard);
                if (InGameBuffCardSlot != null)
                {
                    InGameBuffCardSlot.AppendCard(buffCard);
                }
                else
                {
                    buffCard.SetAnimataorState(BuffCard.AnimatorState.CardPicker_Disappear);
                }
            }

            picedCount++;
            if(picedCount == numOfChoices)
            {
                (BaseInteractionManager as CardPickerInteractionManager).FinishSelection();
            }
        }

        BuffCard GetCardOfID(BuffID buffID)
        {
            foreach(var card in BuffCards)
            {
                if(card.ID == buffID)
                    return card;
            }
            return null;
        }
    }

    internal class BuffGameMenuSlot : BuffCardSlot
    {
        public BuffGameMenu Menu { get; private set; }

        public List<List<BuffID>> buffIDPages = new List<List<BuffID>>();
        List<BuffCard> positiveCards = new List<BuffCard>();
        List<BuffCard> negativeCards = new List<BuffCard>();

        public BuffGameMenuSlot(BuffGameMenu buffGameMenu)
        {
            Menu = buffGameMenu;
            BaseInteractionManager = new BuffGameMenuInteractionManager(this);
        }

        public void SetupBuffs(List<SlugcatStats.Name> nameOrders)
        {
            foreach(var name in nameOrders)
            {
                buffIDPages.Add(BuffDataManager.Instance.GetAllBuffIds(name));
                BuffPlugin.Log($"{buffIDPages.Last().Count} buffs for {name}");
            }
        }

        public void UpdatePage(int page)
        {
            foreach(var card in positiveCards)
            {
                card.SetAnimataorState(BuffCard.AnimatorState.BuffGameMenu_Disappear);
            }
            positiveCards.Clear();
            foreach(var card in negativeCards)
            {
                card.SetAnimataorState(BuffCard.AnimatorState.BuffGameMenu_Disappear);
            }
            negativeCards.Clear();
            foreach(var ids in buffIDPages[page])
            {
                CreateCard(ids);
                BuffPlugin.Log($"Display buff card {ids} in page {page}");
            }

            foreach(var card in BuffCards)
            {
                if(card.currentAniamtorState != BuffCard.AnimatorState.BuffGameMenu_Disappear)
                    card.SetAnimataorState(BuffCard.AnimatorState.BuffGameMenu_Show);
            }
        }

        public float Scroll(float timeStacker)
        {
            return Mathf.Lerp(Mathf.Abs(Menu.lastScroll), Mathf.Abs(Menu.scroll), timeStacker);
        }

        public void CreateCard(BuffID id)
        {
            var card = new BuffCard(id);

            if(card.StaticData.BuffType == BuffType.Positive)
                positiveCards.Add(card);
            else
                negativeCards.Add(card);

            AppendCard(card);
        }

        public int GetCurrentIndex(BuffCard card, out int positveOrNegative, out int totalLength)
        {
            if(card.StaticData.BuffType == BuffType.Positive)
            {
                positveOrNegative = 1;
                totalLength = positiveCards.Count;
                return positiveCards.IndexOf(card);
            }
            else
            {
                positveOrNegative = -1;
                totalLength = negativeCards.Count;
                return negativeCards.IndexOf(card);
            }
        }

        public void DestroyCard(BuffCard card)
        {
            RemoveCard(card);
            card.Destroy();
        }
    }
}
