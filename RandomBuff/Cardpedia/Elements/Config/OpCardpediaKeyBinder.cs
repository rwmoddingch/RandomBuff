using RandomBuff.Core.Option;
using RandomBuff.Core.SaveData.BuffConfig;
using RandomBuffUtils;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using static RandomBuff.Cardpedia.Elements.Config.OpCardpediaChainBox;

namespace RandomBuff.Cardpedia.Elements.Config
{
    internal class OpCardpediaKeyBinder : OpCardpediaChainBox
    {
        BuffConfigurable bindConfigurable;
        AnimBehaviour animBehaviour;

        MouseEvent.MouseEventInstance eventInstance;

        FSprite background;
        FLabel label;

        string current;
        bool bindMode;

        int flashCounter;

        public OpCardpediaKeyBinder(BuffConfigurable bindConfigurable, string title, Vector2 defaultPos, float width, IScrollBoxHandler scrollBoxHandler = null) : base(title, defaultPos, new Vector2(width, CardpediaStatics.dropBox_dropButtonHeight + CardpediaStatics.chainBox_cosmeticRectHeight + CardpediaStatics.tinyGap * 3), false, scrollBoxHandler)
        {
            current = bindConfigurable.StringValue;
            animBehaviour = new AnimBehaviour(CardpediaStatics.pediaUIDarkGrey, CardpediaStatics.pediaUILightGrey, Color.white);
            this.bindConfigurable = bindConfigurable;

            eventInstance = mouseEvent.AddEvent(() =>
                {
                    if (bindMode)
                        return MousePos + new Vector2(-10f, 10f);//follow mouse
                    return new Vector2(CardpediaStatics.smallGap, -CardpediaStatics.chainBox_cosmeticRectHeight - CardpediaStatics.tinyGap * 2);
                },
                () => new Vector2(rectSize.x, CardpediaStatics.dropBox_dropButtonHeight),
                () => { animBehaviour.Flash(); animBehaviour.Anim = true; },
                () => animBehaviour.Anim = false,
                () =>
                {
                    if(!bindMode)
                    {
                        EnterBindMode();
                    }
                },
                null,
                null
            );
            InitSprites();
            BuffInput.OnAnyKeyDown += BuffInput_OnAnyKeyDown;
        }

        public override void Update()
        {
            base.Update();
            animBehaviour.Update();

            if(bindMode)
            {
                flashCounter++;
                if(flashCounter >= 40)
                {
                    flashCounter = 0;
                    animBehaviour.Flash();
                }    
            }
        }

        public void EnterBindMode()
        {
            bindMode = true;
            animBehaviour.Flash();
            label.text = "";
        }

        public void ExitBindMode()
        {
            bindMode = false;

        }

        public void SetBindKey(string keyCode)
        {
            current = keyCode;
            label.text = keyCode.ToString();
            bindConfigurable.Set(keyCode);
        }
        private void BuffInput_OnAnyKeyDown(string keyDown)
        {
            if (!bindMode || keyDown == "Mouse0" || !Enum.TryParse<KeyCode>(keyDown, out var key))
                return;
            if (keyDown == BuffOptionInterface.Instance.KeyBindKey.Value || keyDown == BuffOptionInterface.Instance.CardSlotKey.Value)
                return;
            SetBindKey(keyDown);
            ExitBindMode();
        }


        public override void InitSprites()
        {         
            background = new FSprite("pixel")
            {
                scaleX = rectSize.x - CardpediaStatics.smallGap * 2,
                scaleY = CardpediaStatics.dropBox_dropButtonHeight,
                anchorX = 0f,
                anchorY = 1f,
                color = CardpediaStatics.pediaUIDarkGrey
            };
            myContainer.AddChild(background);
            label = new FLabel(Custom.GetDisplayFont(), current.ToString())
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
            background.scaleX = rectSize.x - CardpediaStatics.smallGap * 2;
            background.SetPosition(new Vector2(CardpediaStatics.smallGap, -CardpediaStatics.tinyGap * 2f - CardpediaStatics.chainBox_cosmeticRectHeight));
            label.SetPosition(new Vector2(CardpediaStatics.smallGap * 2f, -CardpediaStatics.tinyGap * 2f - CardpediaStatics.chainBox_cosmeticRectHeight));
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            background.color = animBehaviour.GetColor(timeStacker);

        }

        public override void Unload()
        {
            base.Unload();
            BuffInput.OnAnyKeyDown -= BuffInput_OnAnyKeyDown;
        }
    }
}
