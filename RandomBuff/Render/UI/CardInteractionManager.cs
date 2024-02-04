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
        const int Double_ClickThreashold = 10;//单击双击阈值（逻辑帧） 

        public BuffCard currentFocusCard;
        protected List<BuffCard> managedCards = new List<BuffCard>();

        public Vector2 MousePos => new Vector2(Futile.mousePosition.x, Futile.mousePosition.y);

        protected bool lastMouseState;
        protected bool mouseState;
        protected int lastMouseButtonUpFrame;

        public virtual void Update()
        {
            lastMouseState = mouseState;
            mouseState = Input.GetMouseButton(0);

            if(lastMouseState && !mouseState)//鼠标按键抬起事件触发
            {
                if(lastMouseButtonUpFrame < Double_ClickThreashold)
                {
                    OnMouseDoubleClick();
                    lastMouseButtonUpFrame = Double_ClickThreashold;//清空计时器
                }
                else
                {
                    lastMouseButtonUpFrame = 0;
                }
            }

            if(lastMouseButtonUpFrame < Double_ClickThreashold)
            {
                lastMouseButtonUpFrame++;
                if(lastMouseButtonUpFrame == Double_ClickThreashold)
                {
                    OnMouseSingleClick();
                }
            }

            for(int i = managedCards.Count - 1; i >= 0; i--)
            {
                managedCards[i].Update();
            }

            UpdateFocusCard();
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
    }

    internal class BasicInteractionManager : CardInteractionManager
    {
        protected override void UpdateFocusCard()
        {
            foreach(var card in managedCards)
            {
                if(card.LocalMousePos.x > 0 && card.LocalMousePos.x < 1f && card.LocalMousePos.y > 0f && card.LocalMousePos.y < 1f)
                {
                    currentFocusCard = card;
                    currentFocusCard.SetAnimataorState(BuffCard.AnimatorState.MousePreview);
                    return;
                }
            }

            currentFocusCard?.SetAnimataorState(BuffCard.AnimatorState.None);
            currentFocusCard = null;
        }

        protected override void OnMouseSingleClick()
        {
            currentFocusCard?.OnMouseSingleClick();
        }

        protected override void OnMouseDoubleClick()
        {
            currentFocusCard?.OnMouseDoubleClick();
        }
    }
}
