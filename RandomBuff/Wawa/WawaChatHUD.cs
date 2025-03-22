using Menu.Remix.MixedUI;
using RandomBuff.Render.UI;
using RandomBuff.Render.UI.Component;
using RandomBuff.Wawa.ChatConv;
using RandomBuffUtils;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Wawa
{
    internal class WawaChatHUD : CosmeticSprite
    {
        static Vector2 Gap = new Vector2(200f, 80f);
        static float height = 150f;
        static Vector2 targetExpressionSize = new Vector2(130f, 130f);

        WawaChatConv convToRead;

        FContainer container;
        RoundRectSprites roundRectSprites;
        FSprite expressionSprite;
        FSprite arrow;
        FLabel conv, devLabel;

        //expression
        float baseScale;

        //state anim
        bool finish;
        float show, lastShow;

        //interact 
        bool keyDown;
        float push, lastPush;

        //conv
        string currentConv;
        bool firstConvInited;
        int nextCharCD = 0;
        int currentCharLength = 0;
        int currentConvIndex = 0;
        float convMaxWidth;
        int charStep;


        Vector2 targetSize;

        public WawaChatHUD(Room room, WawaChatConv wawaChatConv)
        {
            this.convToRead = wawaChatConv;
            this.room = room;
            targetSize = new Vector2(Custom.rainWorld.options.ScreenSize.x - Gap.x * 2f, height);
            convMaxWidth = targetSize.x - 20f * 2f - targetExpressionSize.x - 20f;

            room.game.cameras[0].ReturnFContainer("HUD").AddChild(container = new FContainer());
            roundRectSprites = new RoundRectSprites(container, new Vector2(BuffResolution.HalfScreenSize.x, Gap.y), new Vector2(0f, 0f));
            expressionSprite = new FSprite("pixel", true);
            arrow = new FSprite("Big_Menu_Arrow")
            {
                rotation = 180f,
                scale = 0f
            };
            conv = new FLabel(Custom.GetDisplayFont(), "")
            {
                alignment = FLabelAlignment.Left,
                anchorX = 0f,
                anchorY = 1f,
            };
            devLabel = new FLabel(Custom.GetDisplayFont(), "")
            {
                alignment = FLabelAlignment.Right,
                anchorX = 1f,
                anchorY = 0f
            };
            baseScale = targetExpressionSize.x / expressionSprite.element.sourcePixelSize.x;
            container.AddChild(expressionSprite);
            container.AddChild(conv);
            container.AddChild(arrow);
            container.AddChild(devLabel);
            conv.SetPosition(Gap.x + height - targetExpressionSize.x, Gap.y + height * 2 - targetExpressionSize.y - 30f);
           

            currentConvIndex = -1;//为了加1后是读的第一段对话
            charStep = Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese ? 2 : 4;
            TryNextConv();
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (slatedForDeletetion)
                return;

            if(roundRectSprites != null)
            {
                roundRectSprites.Update();
                roundRectSprites.size = Vector2.Lerp(Vector2.zero, targetSize, Helper.EaseInOutCubic(show));
                roundRectSprites.pos = new Vector2(Mathf.Lerp(BuffResolution.HalfScreenSize.x, Gap.x, Helper.EaseInOutCubic(show)), Gap.y);
                
            }

            lastShow = show;
            if (!finish && show < 1f)
            {
                show += 1f / 40f;
            }
            else if(finish && show > 0)
            {
                show -= 1f / 40f;
                if (show <= 0f)
                    Destroy();
            }

            lastPush = push;
            push = Mathf.Lerp(push, 0f, 0.15f);

            if(show >= 1 && !finish)
            {
                ConvLogic();
            }
        }

        void ConvLogic()
        {
            if(nextCharCD == 0)
            {
                if (currentCharLength <= currentConv.Length)
                {
                    room.PlaySound(SoundID.Shelter_Bolt_Open, 0f, 0.3f, 45f);
                    nextCharCD = 2;
                    currentCharLength += charStep;
                    conv.text = currentConv.Substring(0, Mathf.Min(currentConv.Length, currentCharLength));
                    arrow.color = Color.gray * 0.5f + Color.black * 0.5f;
                }
                else
                {
                    if (Input.GetKey(KeyCode.Space) && !keyDown)
                    {
                        TryNextConv();
                        push = 1f;
                    }
                    arrow.color = Color.white;
                }
            }
            else
            {
                nextCharCD--;
            }
            keyDown = Input.GetKey(KeyCode.Space);
        }

        void TryNextConv()
        {
            currentConvIndex++;
            if(currentConvIndex == convToRead.GetConvCount(Custom.rainWorld.inGameTranslator.currentLanguage))
            {
                finish = true;
                conv.text = string.Empty;
                return;
            }
            currentCharLength = 0;
            nextCharCD = 1;
            UpdateExpression();
            devLabel.text = convToRead.GetDev(Custom.rainWorld.inGameTranslator.currentLanguage, currentConvIndex);
            currentConv = LabelTest.WrapText(convToRead.GetConv(Custom.rainWorld.inGameTranslator.currentLanguage, currentConvIndex), true, convMaxWidth);
            BuffUtils.Log("WawaChatHUD", $"New Conv : {currentConv}");
            room.PlaySound(SoundID.MENU_Button_Select_Gamepad_Or_Keyboard, 0f, 1f, 1f);
        }

        void UpdateExpression()
        {
            expressionSprite.SetElementByName(WawaChatLoader.GetElement(convToRead.GetOrigDev(Custom.rainWorld.inGameTranslator.currentLanguage, currentConvIndex), convToRead.GetExpression(Custom.rainWorld.inGameTranslator.currentLanguage, currentConvIndex)));
            baseScale = targetExpressionSize.x / expressionSprite.element.sourcePixelSize.x;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[0];
            AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("HUD"));
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (!sLeaser.deleteMeNextFrame && (base.slatedForDeletetion || room != rCam.room))
            {
                sLeaser.CleanSpritesAndRemove();
                roundRectSprites?.RemoveSprites();
                roundRectSprites = null;
                return;
            }
            roundRectSprites?.GrafUpdate(timeStacker);

            float f = Helper.EaseInOutCubic(Mathf.Lerp(lastShow, show, timeStacker));
            expressionSprite.SetPosition(Vector2.Lerp(new Vector2(BuffResolution.HalfScreenSize.x, Gap.y), new Vector2(BuffResolution.ScreenSize.x - Gap.x - height/2f, Gap.y + height / 2f), f));

            expressionSprite.scale = baseScale * f;
            expressionSprite.alpha = f;
            arrow.scale = 0.5f * f;

            arrow.SetPosition(BuffResolution.HalfScreenSize.x, Gap.y + 20f * f * (1f - 0.4f * Mathf.Lerp(lastPush, push, timeStacker)));
            devLabel.SetPosition(expressionSprite.x - (targetExpressionSize.x / 2f + 20f) * f, Gap.y + 10f * f);
            devLabel.scale = f;
        }

        public override void Destroy()
        {
            if (slatedForDeletetion)
                return;

            if(roundRectSprites != null)
            {
                roundRectSprites.RemoveSprites();
                roundRectSprites = null;
            }
            expressionSprite.RemoveFromContainer();
            conv.RemoveFromContainer();
            arrow.RemoveFromContainer();
            devLabel.RemoveFromContainer();

            container.RemoveAllChildren();
            container.RemoveFromContainer();

            base.Destroy();
        }
    }
}
