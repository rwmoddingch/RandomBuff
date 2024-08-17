using RandomBuff.Core.SaveData.BuffConfig;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using static RandomBuff.Cardpedia.Elements.Config.OpCardpediaChainBox;

namespace RandomBuff.Cardpedia.Elements.Config
{
    internal class OpCardpediaTwoValue : OpCardpediaChainBox
    {
        BuffConfigurable bindConfigurable;
        BuffConfigurableAcceptableTwoValue twoValueAcceptable;

        MouseEvent.MouseEventInstance eventInstance;
        AnimBehaviour animBehaviour;

        FSprite toggleButton;
        FLabel label;

        public OpCardpediaTwoValue(BuffConfigurable bindConfigurable, string title, Vector2 defaultPos, float width, IScrollBoxHandler scrollBoxHandler = null) : base(title, defaultPos, new Vector2(width, CardpediaStatics.dropBox_dropButtonHeight + CardpediaStatics.chainBox_cosmeticRectHeight + CardpediaStatics.tinyGap * 3), false, scrollBoxHandler)
        {
            animBehaviour = new AnimBehaviour(CardpediaStatics.pediaUIDarkGrey * 0.5f + CardpediaStatics.pediaUILightGrey * 0.5f, CardpediaStatics.pediaUILightGrey, Color.white);
            this.bindConfigurable = bindConfigurable;
            twoValueAcceptable = bindConfigurable.acceptable as BuffConfigurableAcceptableTwoValue;

            eventInstance = mouseEvent.AddEvent(() => new Vector2(rectSize.x - CardpediaStatics.smallGap - CardpediaStatics.dropBox_dropButtonHeight, -CardpediaStatics.chainBox_cosmeticRectHeight - CardpediaStatics.tinyGap * 2),
                () => new Vector2(CardpediaStatics.dropBox_dropButtonHeight, CardpediaStatics.dropBox_dropButtonHeight),
                () => { animBehaviour.Flash(); animBehaviour.Anim = true; },
                () => animBehaviour.Anim = false,
                () => { ToggleValue(); animBehaviour.Flash(); },
                null,
                null
            );
            InitSprites();
            label.text = bindConfigurable.StringValue;
        }

        public override void Update()
        {
            base.Update();
            animBehaviour.Update();
        }

        public void ToggleValue()
        {
            var newValue = twoValueAcceptable.GetAnother(bindConfigurable.BoxedValue);
            bindConfigurable.BoxedValue = newValue;
            label.text = bindConfigurable.StringValue;
        }

        public override void InitSprites()
        {
            toggleButton = new FSprite("pixel")
            {
                scaleX = CardpediaStatics.dropBox_dropButtonHeight,
                scaleY = CardpediaStatics.dropBox_dropButtonHeight,
                anchorX = 0f,
                anchorY = 1f,
                color = CardpediaStatics.pediaUIDarkGrey
            };
            myContainer.AddChild(toggleButton);
            label = new FLabel(Custom.GetDisplayFont(), bindConfigurable.StringValue)
            {
                anchorX = 0f,
                anchorY = 1f,
                alignment = FLabelAlignment.Left
            };
            myContainer.AddChild(label);
            base.InitSprites();
        }

        public override void RecaculateSpriteScaleAndPos(float timeStacker)
        {
            base.RecaculateSpriteScaleAndPos(timeStacker);
            toggleButton.SetPosition(new Vector2(rectSize.x - CardpediaStatics.dropBox_dropButtonHeight - CardpediaStatics.smallGap, -CardpediaStatics.tinyGap * 2f - CardpediaStatics.chainBox_cosmeticRectHeight));
            label.SetPosition(new Vector2(CardpediaStatics.smallGap * 2f, -CardpediaStatics.tinyGap * 2f - CardpediaStatics.chainBox_cosmeticRectHeight));
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            toggleButton.color = animBehaviour.GetColor(timeStacker);
        }
    }
}
