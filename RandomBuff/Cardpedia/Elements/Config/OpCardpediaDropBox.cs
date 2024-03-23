using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Cardpedia.Elements.Config
{
    internal class OpCardpediaDropBox : OpCardpediaChainBox
    {
        public float dropButtonWidth;

        public FSprite defaultRectBackground;
        public FLabel defaultRectLabel;

        DropButton[] dropButtons;
        MouseEvent.MouseEventInstance defaultRectEventInstance;

        //状态变量
        public float targetDrop;
        public float drop;
        public float lastDrop;

        AnimBehaviour animBehaviour;

        public Vector2 DropButtonRect => new Vector2(dropButtonWidth - CardpediaStatics.tinyGap, CardpediaStatics.dropBox_dropButtonHeight);
        public Vector2 DropButtonStartPos => new Vector2(rectSize.x - dropButtonWidth, -CardpediaStatics.tinyGap * 2f - CardpediaStatics.chainBox_cosmeticRectHeight);
        public float additionalHeight => (CardpediaStatics.tinyGap + CardpediaStatics.dropBox_dropButtonHeight) * dropButtons.Length;

        public OpCardpediaDropBox(string title, Vector2 pos, float width, string[] list, IScrollBoxHandler scrollBoxHandler = null) : base(title, pos, new Vector2(width, CardpediaStatics.dropBox_dropButtonHeight + CardpediaStatics.chainBox_cosmeticRectHeight + CardpediaStatics.tinyGap * 3), false, scrollBoxHandler)
        {
            dropButtonWidth = width;
            
            defaultRectEventInstance = mouseEvent.AddEvent(
                () => DropButtonStartPos,
                () => DropButtonRect,
                () => { animBehaviour.Flash(); animBehaviour.Anim = true; },
                () => animBehaviour.Anim = false,
                () => Drop(),
                null,
                null);

            dropButtons = new DropButton[list.Length];
            for(int i = 0;i < dropButtons.Length; i++)
            {
                dropButtons[i] = new DropButton(this, list[i], i);
            }
            InitSprites();

            animBehaviour = new AnimBehaviour(CardpediaStatics.pediaUIDarkGrey, CardpediaStatics.pediaUILightGrey, Color.white);

            SetElement(list.First());
        }

        public override void InitSprites()
        {
            defaultRectBackground = new FSprite("pixel")
            {
                scaleX = DropButtonRect.x,
                scaleY = CardpediaStatics.dropBox_dropButtonHeight,
                anchorX = 0f,
                anchorY = 1f,
                color = CardpediaStatics.pediaUIDarkGrey,
                alpha = 1f
            };
            defaultRectLabel = new FLabel(Custom.GetDisplayFont(), "")
            {
                anchorX = 0f,
                anchorY = 1f,
                alignment = FLabelAlignment.Left
            };
            myContainer.AddChild(defaultRectBackground);
            myContainer.AddChild(defaultRectLabel);

            base.InitSprites();

            defaultRectBackground.MoveToFront();
            defaultRectLabel.MoveToFront();
        }

        public override void RecaculateSpriteScaleAndPos(float timeStacker)
        {
            base.RecaculateSpriteScaleAndPos(timeStacker);
            defaultRectBackground.SetPosition(DropButtonStartPos);
            defaultRectLabel.SetPosition(DropButtonStartPos);
        }

        public override void Update()
        {
            base.Update();
            foreach (var dropButton in dropButtons)
                dropButton.Update();

            lastDrop = drop;
            drop = Mathf.Lerp(drop, targetDrop, 0.15f);
            if(Mathf.Approximately(drop, targetDrop))
                drop = targetDrop;

            animBehaviour.Update();
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            foreach (var dropButton in dropButtons)
                dropButton.GrafUpdate(timeStacker);

            defaultRectBackground.color = animBehaviour.GetColor(timeStacker);
        }

        public void SetElement(string element)
        {
            defaultRectLabel.text = element;
            animBehaviour.Flash();
        }

        public void Drop()
        {
            targetDrop = 1f;
            setRectSize = defaultRectSize + new Vector2(0f, additionalHeight);
            scrollBoxHandler?.ResetScrollBoxSize();
        }

        public void UnDrop()
        {
            targetDrop = 0f;
            setRectSize = defaultRectSize;
            scrollBoxHandler?.ResetScrollBoxSize();
        }

        class DropButton
        {
            public OpCardpediaDropBox dropBox;

            string element;
            int index;

            FSprite background;
            FLabel label;
            MouseEvent.MouseEventInstance eventInstance;

            Vector2 DropPosDelta => new Vector2(0f, -(CardpediaStatics.dropBox_dropButtonHeight + CardpediaStatics.tinyGap) * (index + 1));
            Vector2 FullyDropPos => dropBox.DropButtonStartPos + DropPosDelta;

            AnimBehaviour animBehaviour;

            public DropButton(OpCardpediaDropBox dropBox, string element, int index)
            {
                this.dropBox = dropBox;
                this.element = element;
                this.index = index;

                InitSprites();
                eventInstance = dropBox.mouseEvent.AddEvent(
                    () => FullyDropPos,
                    () => dropBox.DropButtonRect,
                    () => { animBehaviour.Flash(); animBehaviour.Anim = true; },
                    () => animBehaviour.Anim = false,
                    () => { dropBox.UnDrop(); dropBox.SetElement(element); },
                    null,
                    null);

                animBehaviour = new AnimBehaviour(CardpediaStatics.pediaUIDarkGrey, CardpediaStatics.pediaUILightGrey, Color.white);
            }

            public void InitSprites()
            {
                background = new FSprite("pixel")
                {
                    scaleX = dropBox.DropButtonRect.x,
                    scaleY = CardpediaStatics.dropBox_dropButtonHeight,
                    anchorX = 0f,
                    anchorY = 1f,
                    color = CardpediaStatics.pediaUIDarkGrey
                };
                label = new FLabel(Custom.GetDisplayFont(), element)
                {
                    anchorX = 0f,
                    anchorY = 1f,
                    alignment = FLabelAlignment.Left
                };
                dropBox.myContainer.AddChild(background);
                dropBox.myContainer.AddChild(label);
            }

            public void Update()
            {
                if (dropBox.targetDrop == 1f && dropBox.drop > 0.5f)
                    eventInstance.enable = true;
                else
                    eventInstance.enable = false;

                animBehaviour.Update();
            }

            public void GrafUpdate(float timeStacker)
            {
                //if (dropBox.lastRectSize == dropBox.rectSize && dropBox.rectSize == dropBox.setRectSize)
                //{
                //    return;
                //}
                //Vector2 smoothSize = Vector2.Lerp(dropBox.lastRectSize, dropBox.rectSize, timeStacker);
                float smoothDrop = Mathf.Lerp(dropBox.lastDrop, dropBox.drop, timeStacker);

                Vector2 setPos = dropBox.DropButtonStartPos + DropPosDelta * smoothDrop;
                
                background.SetPosition(setPos);
                background.color = animBehaviour.GetColor(timeStacker);
                background.alpha = smoothDrop;

                label.SetPosition(setPos);
                label.alpha = smoothDrop;
            }
        }
    }
}
