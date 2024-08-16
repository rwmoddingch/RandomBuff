using RandomBuff.Core.SaveData.BuffConfig;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Cardpedia.Elements.Config
{
    internal class OpCardpediaConfigSlider : OpCardpediaChainBox
    {
        BuffConfigurable bindConfigurable;

        FLabel leftValueLabel;
        FLabel rightValueLabel;
        FLabel currentValueLabel;

        FSprite sliderLine;
        FSprite sliderBlock;
        FSprite valueLabelShadow;

        MouseEvent.MouseEventInstance dragRectEventInstance;

        Vector2 sliderLineStartPos => new Vector2(CardpediaStatics.slider_sliderSpan, -CardpediaStatics.tinyGap * 2f - CardpediaStatics.chainBox_cosmeticRectHeight);
        float sliderLength => rectSize.x - CardpediaStatics.slider_sliderSpan * 2f;

        float left;
        float right;

        //状态变量
        bool dragging;

        float startDraggingDrag;

        float drag;
        float lastDrag;

        AnimBehaviour currentLabelBehaviour;

        public OpCardpediaConfigSlider(BuffConfigurable bindConfigurable, string title, Vector2 pos, float width, float leftValue, float rightValue, IScrollBoxHandler scrollBoxHandler = null) : base(title, pos, new Vector2(width, CardpediaStatics.dropBox_dropButtonHeight + CardpediaStatics.chainBox_cosmeticRectHeight + CardpediaStatics.tinyGap * 3), false, scrollBoxHandler)
        {
            this.left = leftValue;
            this.right = rightValue;
            this.bindConfigurable = bindConfigurable;

            drag = Mathf.InverseLerp(left, right, (float)bindConfigurable.BoxedValue);
            
            dragRectEventInstance = mouseEvent.AddEvent(
                () =>
                    {
                        if (dragging)
                            return MousePos + new Vector2(-100f, 100f);//判定跟随鼠标
                        return sliderLineStartPos + new Vector2(drag * sliderLength - CardpediaStatics.slider_sliderRectWidth * 2f, CardpediaStatics.slider_sliderRectHeight * 2f);
                    },
                () => new Vector2(CardpediaStatics.slider_sliderRectWidth, CardpediaStatics.slider_sliderRectHeight) * (dragging ? 100f : 4f),
                () => currentLabelBehaviour.Anim = true,
                () => currentLabelBehaviour.Anim = false,
                () => startDraggingDrag = drag,
                (d) =>
                    {
                        //BuffPlugin.Log($"Dragging : {d}");
                        dragging = true;
                        float delta = d.x / sliderLength;
                        drag = Mathf.Clamp01(startDraggingDrag + delta);
                        currentValueLabel.text = string.Format("{0:N2}",Mathf.Lerp(left, right, drag));
                        
                    },
                (d) =>
                    {
                        //BuffPlugin.Log($"Finish dragging : {d}");
                        dragging = false;
                        float delta = d.x / sliderLength;
                        drag = Mathf.Clamp01(startDraggingDrag + delta);
                        startDraggingDrag = drag;
                        currentValueLabel.text = string.Format("{0:N2}", Mathf.Lerp(left, right, drag));
                        bindConfigurable.BoxedValue = Mathf.Lerp(left, right, drag);
                    }
                );
            currentLabelBehaviour = new AnimBehaviour(CardpediaStatics.pediaUILightGrey, CardpediaStatics.pediaUILightGrey, Color.white);

            InitSprites();
        }

        public override void InitSprites()
        {
            sliderLine = new FSprite("pixel")
            {
                scaleX = sliderLength,
                scaleY = CardpediaStatics.slider_lineHeight,
                anchorX = 0f,
                anchorY = 0.5f,
                color = CardpediaStatics.pediaUILightGrey
            };
            myContainer.AddChild(sliderLine);

            sliderBlock = new FSprite("pixel")
            {
                scaleX = CardpediaStatics.slider_sliderRectWidth,
                scaleY = CardpediaStatics.slider_sliderRectHeight,
                anchorX = 0.5f,
                anchorY = 0.5f,
                color = CardpediaStatics.pediaUILightGrey,
                shader = Custom.rainWorld.Shaders["MenuTextCustom"]
            };
            myContainer.AddChild(sliderBlock);

            leftValueLabel = new FLabel(Custom.GetFont(), string.Format("{0:N2}", left))
            {
                anchorX = 0.5f,
                anchorY = 0.5f
            };
            myContainer.AddChild(leftValueLabel);

            rightValueLabel = new FLabel(Custom.GetFont(), string.Format("{0:N2}", right))
            {
                anchorX = 0.5f,
                anchorY = 0.5f
            };
            myContainer.AddChild(rightValueLabel);

            valueLabelShadow = new FSprite("Futile_White")
            {
                scaleX = 3f,
                scaleY = 2f,
                color = Color.black,
                shader = Custom.rainWorld.Shaders["FlatLight"]
            };
            myContainer.AddChild(valueLabelShadow);

            currentValueLabel = new FLabel(Custom.GetFont(), string.Format("{0:N2}", Mathf.Lerp(left, right, drag)))
            {
                anchorX = 0.5f,
                _anchorY = 0.5f
            };
            myContainer.AddChild(currentValueLabel);

            base.InitSprites();

            valueLabelShadow.MoveToFront();
            currentValueLabel.MoveToFront();
        }

        public override void Update()
        {
            base.Update();
            lastDrag = drag;
            currentLabelBehaviour.Update();
        }

        public override void RecaculateSpriteScaleAndPos(float timeStacker)
        {
            base.RecaculateSpriteScaleAndPos(timeStacker);
            float smoothDrag = Mathf.Lerp(lastDrag, drag, timeStacker);

            sliderLine.scaleX = sliderLength;
            sliderLine.SetPosition(sliderLineStartPos + new Vector2(0f, -CardpediaStatics.dropBox_dropButtonHeight / 2f));

            float smoothLabelAnim = currentLabelBehaviour.SmoothAnim(timeStacker);

            Vector2 sliderBlockPos = sliderLineStartPos + new Vector2(smoothDrag * sliderLength, -CardpediaStatics.dropBox_dropButtonHeight / 2f);
            sliderBlock.SetPosition(sliderBlockPos);
            sliderBlock.scaleX = CardpediaStatics.slider_sliderRectWidth * Mathf.Lerp(1f, 1.5f, smoothLabelAnim);
            sliderBlock.scaleY = CardpediaStatics.slider_sliderRectHeight * Mathf.Lerp(1f, 1.5f, smoothLabelAnim);
            sliderBlock.color = currentLabelBehaviour.GetColor(timeStacker);

            leftValueLabel.SetPosition(sliderLineStartPos + new Vector2(-CardpediaStatics.slider_sliderSpan / 2f, -CardpediaStatics.dropBox_dropButtonHeight / 2f));
            rightValueLabel.SetPosition(new Vector2(rectSize.x - CardpediaStatics.slider_sliderSpan / 2f, sliderLineStartPos.y - CardpediaStatics.dropBox_dropButtonHeight / 2f));

            Vector2 valueLabelPos = sliderBlockPos + new Vector2(CardpediaStatics.slider_sliderRectWidth / 2f, CardpediaStatics.slider_sliderRectHeight * smoothLabelAnim * 1.5f);
            currentValueLabel.SetPosition(valueLabelPos);
            currentValueLabel.alpha = smoothLabelAnim;
            valueLabelShadow.SetPosition(valueLabelPos);
            valueLabelShadow.alpha = smoothLabelAnim;
        }
    }
}
