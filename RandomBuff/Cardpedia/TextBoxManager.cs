using RandomBuff.Cardpedia.Elements;
using RandomBuff.Cardpedia.InfoPageRender;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using RandomBuff.Core.Buff;

namespace RandomBuff.Cardpedia
{
    public class TextBoxManager
    {
        public bool textSetted;
        public bool inited;
        public BuffType currentType;

        public CardpediaMenu owner;
        public List<CardpediaTextBox> textBoxes;
        public CardpediaTextBox titleBox;
        public List<float> yOffSet;
        public List<float> origScaleX;
        public List<float> origScaleY;
        public List<float> origTextScaleY;
        public List<float> textYOffset;
        
        public List<FTexture> textTextures;
        public List<FSprite> subtitleSprites;
        public List<FSprite> shadowSprites;
        public FSprite titleBack;
        public FContainer Container;
        public float Y_upLimit = 723f;
        public float Y_downLimit = 243f;
        public float gap = 15f;
        public float scroll = 0f;
        public float scrollSpeed = 0.05f;
        public float scrollLength;

        public TextBoxManager(CardpediaMenu menu) 
        {
            currentType = BuffType.Negative;
            owner = menu;
            textBoxes = new List<CardpediaTextBox>();
            yOffSet = new List<float>();
            origScaleX = new List<float>();
            origScaleY = new List<float>();
            Container = new FContainer();
            textTextures = new List<FTexture>();
            subtitleSprites = new List<FSprite>();
            shadowSprites = new List<FSprite>();
            origTextScaleY = new List<float>();
            textYOffset = new List<float>();

            for (int i = 0; i < 5; i++)
            {
                textBoxes.Add(SetupTextBox(i));
                textBoxes[i].textTexture.scale = 0.8f;               
            }

            titleBack = new FSprite("pixel");
            titleBack.color = currentType == BuffType.Negative ? new Color(0.6f, 0f, 0.05f) :
                (currentType == BuffType.Positive ? new Color(0f, 0.6f, 0.4f) : new Color(0.5f, 0.5f, 0.5f));
            titleBack.scaleX = 260f;
            titleBack.scaleY = 80f;
            titleBack.SetPosition(new Vector2(203f, Y_downLimit + 40f));
            titleBack.alpha = 0f;
            owner.cursorContainer.AddChild(titleBack);

            var renderer = ScruffyPool.GetRenderer(5);
            titleBox = new CardpediaTextBox(renderer, true, Color.white, 1, 5);
            titleBox.textTexture.SetPosition(new Vector2(203f,298f));           
            owner.cursorContainer.AddChild(titleBox.textTexture);

            scrollLength -= Y_upLimit - Y_downLimit;

            inited = true;

        }

        public CardpediaTextBox SetupTextBox(int id)
        {            
            var textRenderer = ScruffyPool.GetRenderer(id);
            var textBox = new CardpediaTextBox(textRenderer, false, Color.white, 1, id);
            Container.AddChild(textBox.Container);
            FSprite blurSprite = new FSprite("buffassets/illustrations/UIBlock");
            if (id < 3)
            {
                blurSprite.scaleX = 2.1f;
                blurSprite.scaleY = 1.6f;
                blurSprite.SetPosition(owner.blurSprite.GetPosition() + new Vector2(gap + 50f * blurSprite.scaleX + 130f + id * (100f * blurSprite.scaleX + 5f), 0.5f * (Y_upLimit - Y_downLimit - 100f * blurSprite.scaleY)));
            }
            else
            {
                blurSprite.scaleX = 6.4f;
                if (id == 3)
                {                    
                    blurSprite.scaleY = 3.1f;
                }
                else
                {
                    blurSprite.scaleY = 2f;
                }
                blurSprite.SetPosition(new Vector2(owner.blurSprite.GetPosition().x + gap + 50f * blurSprite.scaleX + 130f, yOffSet[2] - 10f - 0.5f * (100f * blurSprite.scaleY + 160f) - (id < 4? 0f : 10f + origScaleY[id - 1]))) ;
            }
            blurSprite.shader = owner.manager.rainWorld.Shaders["UIBlurFoldable"];
            blurSprite.alpha = 0f;
            textBox.blurSprite = blurSprite;
            textBox.Container.AddChild(textBox.blurSprite);
            textBox.Container.AddChild(textBox.uselessSprite);
            owner.cursorContainer.AddChild(textBox.textTexture);
            yOffSet.Add(blurSprite.GetPosition().y);
            textYOffset.Add(yOffSet[id]);
            origScaleX.Add(100f * blurSprite.scaleX);
            origScaleY.Add(100f * blurSprite.scaleY);
            if(id >= 2)
            {
                scrollLength += origScaleY[id] + (id == 4 ? 0 : 10f);
            }
            string path = "buffassets/illustrations/Titles/";

            if (id < 3)
            {
                
                var subtitleSprite = new FSprite(path + (id == 0? "Type" : (id == 1? "Stack" : "Trigger")) + "_" + (Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.English? "Eng" : "Chi"));
                var shadowSprite = new FSprite(path + "SmallShadow");
                subtitleSprite.shader = Custom.rainWorld.Shaders["FoldableTextVertical"];
                shadowSprite.shader = Custom.rainWorld.Shaders["FoldablePicVertical"];
                subtitleSprite.SetPosition(textBox.blurSprite.GetPosition());
                shadowSprite.SetPosition(textBox.blurSprite.GetPosition() - new Vector2(1f,1f));
                subtitleSprites.Add(subtitleSprite);
                shadowSprites.Add(shadowSprite);
                subtitleSprite.alpha = 0f;
                shadowSprite.alpha = 0f;
                owner.cursorContainer.AddChild(subtitleSprite);
                owner.cursorContainer.AddChild(shadowSprite);
            }
            else
            {
                var subtitleSprite = new FSprite(path + (id == 3 ? "Description" : "Confliction") + "_" + (Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.English ? "Eng" : "Chi"));
                var shadowSprite = new FSprite(path + "LongShadow" + (id == 3? string.Empty : "_200"));
                subtitleSprite.shader = Custom.rainWorld.Shaders["FoldableTextVertical"];
                shadowSprite.shader = Custom.rainWorld.Shaders["FoldablePicVertical"];
                subtitleSprite.SetPosition(textBox.blurSprite.GetPosition() + new Vector2(0, id == 3? 80f : 0f));
                shadowSprite.SetPosition(textBox.blurSprite.GetPosition() - new Vector2(1f, 1f));
                subtitleSprites.Add(subtitleSprite);
                shadowSprites.Add(shadowSprite);
                subtitleSprite.alpha = 0f;
                shadowSprite.alpha = 0f;
                owner.cursorContainer.AddChild(subtitleSprite);
                owner.cursorContainer.AddChild(shadowSprite);
            }
            //subtitleSprites[id].scale = 0.8f;
            //shadowSprites[id].scale = 0.8f;
            
            return textBox;
        }
             
        public void RefreshInformation(string cardLife, string stackability, string trigger, string description, string title, BuffID id)
        {
            owner.cardSheetManager.displayingCard.element = Futile.atlasManager.GetElementWithName(id.GetStaticData().FaceName);
            owner.cardSheetManager.displayingCard.scale = 0.35f * (600f / owner.cardSheetManager.displayingCard.element.sourcePixelSize.x);

            textBoxes[0].RefreshTextTexture(owner.cursorContainer, cardLife, textBoxes[0].blurSprite.GetPosition());
            textBoxes[1].RefreshTextTexture(owner.cursorContainer, stackability, textBoxes[1].blurSprite.GetPosition());
            textBoxes[2].RefreshTextTexture(owner.cursorContainer, trigger, textBoxes[2].blurSprite.GetPosition());            
            textBoxes[3].RefreshTextTexture(owner.cursorContainer, description, textBoxes[3].blurSprite.GetPosition() + new Vector2(0, 80f));
            titleBox.RefreshTextTexture(owner.cursorContainer, title, new Vector2(203f, 298f));
            
        }

        public void Update()
        {            

            if (Input.GetKey(KeyCode.DownArrow))
            {
                if (scroll < 1f)
                {
                    scroll += scrollSpeed;
                }
                
            }
            else if (Input.GetKey(KeyCode.UpArrow))
            {
                if (scroll > 0f)
                {
                    scroll -= scrollSpeed;
                }
            }

            if(!textSetted)
            {
                InitEmptyInfo();
                textSetted = true;
            }
        }

        public void InitEmptyInfo()
        {
            textBoxes[0].RefreshTextTexture(owner.cursorContainer, "? ? ?", textBoxes[0].blurSprite.GetPosition());
            textBoxes[1].RefreshTextTexture(owner.cursorContainer, "? ? ?", textBoxes[1].blurSprite.GetPosition());
            textBoxes[2].RefreshTextTexture(owner.cursorContainer, "? ? ?", textBoxes[2].blurSprite.GetPosition());
            string str = Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese ?
                "（点击下方卡牌以查看详细信息）" : "(Click the cards below to view the detail information)";
            textBoxes[3].RefreshTextTexture(owner.cursorContainer, str, textBoxes[3].blurSprite.GetPosition() + new Vector2(0, 80f));
            string str2 = Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese ?
                "（无）" : "(None)";
            textBoxes[4].RefreshTextTexture(owner.cursorContainer, str2, textBoxes[4].blurSprite.GetPosition());
            titleBox.RefreshTextTexture(owner.cursorContainer, "? ? ?", new Vector2(203f, 298f));           
        }

        public void Draw(float timeStacker)
        {
            if (!inited) return;
            for (int i = 0; i < textBoxes.Count; i++)
            {
                //白框框更新
                textBoxes[i].lastYOffSet = textBoxes[i].blurSprite.y;
                textBoxes[i].blurSprite.y = Mathf.Lerp(textBoxes[i].lastYOffSet, yOffSet[i] + scrollLength * scroll, timeStacker);
                //if(i < 3)
                {
                    subtitleSprites[i].y = textBoxes[i].blurSprite.y;
                    shadowSprites[i].y = textBoxes[i].blurSprite.y - 1f;                   
                }

                textBoxes[i].blurSprite.alpha = owner.blurSprite.alpha;
                textBoxes[i].textTexture.alpha = owner.blurSprite.alpha;
                subtitleSprites[i].alpha = owner.blurSprite.alpha;
                shadowSprites[i].alpha = 0.3f * owner.blurSprite.alpha;
                
                float lastUp = textBoxes[i].blurSprite._renderLayer._meshRenderer.material.GetFloat("_UpAlpha");
                float lastDown = textBoxes[i].blurSprite._renderLayer._meshRenderer.material.GetFloat("_DownAlpha");

                float upAlpha = 1f;
                float downAlpha = 1f;

                if (textBoxes[i].blurSprite.y > Y_upLimit - 0.5f * origScaleY[i])
                {
                    upAlpha -= (textBoxes[i].blurSprite.y - Y_upLimit + 0.5f * origScaleY[i]) / origScaleY[i];
                }

                if (textBoxes[i].blurSprite.y < Y_downLimit + 0.5f * origScaleY[i])
                {
                    downAlpha -= (Y_downLimit + 0.5f * origScaleY[i] - textBoxes[i].blurSprite.y) / origScaleY[i];
                }

                if (upAlpha < 0f) { upAlpha = 0f; }
                if (downAlpha < 0f) { downAlpha = 0f; }

                textBoxes[i].blurSprite._renderLayer._meshRenderer.material.SetFloat("_UpAlpha", Mathf.Lerp(lastUp,upAlpha,timeStacker));
                textBoxes[i].blurSprite._renderLayer._meshRenderer.material.SetFloat("_DownAlpha", Mathf.Lerp(lastDown, downAlpha, timeStacker));

                //if(i < 3)
                {
                    subtitleSprites[i]._renderLayer._meshRenderer.material.SetFloat("_UpAlpha", Mathf.Lerp(lastUp, upAlpha, timeStacker));
                    subtitleSprites[i]._renderLayer._meshRenderer.material.SetFloat("_DownAlpha", Mathf.Lerp(lastDown, downAlpha, timeStacker));
                    shadowSprites[i]._renderLayer._meshRenderer.material.SetFloat("_UpAlpha", Mathf.Lerp(lastUp, upAlpha, timeStacker));
                    shadowSprites[i]._renderLayer._meshRenderer.material.SetFloat("_DownAlpha", Mathf.Lerp(lastDown, downAlpha, timeStacker));
                }

                //文字更新
                DrawText(timeStacker);               
            }
        }

        public void DrawText(float timeStacker)
        {
            for (int i = 0; i < textBoxes.Count; i++)
            {
                textBoxes[i].textTexture.y = textBoxes[i].blurSprite.y + (i == 3? 80f : 0f);
                float lastUp = textBoxes[i].textTexture._renderLayer._meshRenderer.material.GetFloat("_UpAlpha");
                float lastDown = textBoxes[i].textTexture._renderLayer._meshRenderer.material.GetFloat("_DownAlpha");

                float upAlpha = 1f;
                float downAlpha = 1f;

                if (textBoxes[i].textTexture.y > Y_upLimit - 250f)
                {
                    upAlpha -= (textBoxes[i].textTexture.y - Y_upLimit + 250) / 500;
                }

                if (textBoxes[i].textTexture.y < Y_downLimit + 250f)
                {
                    downAlpha -= (Y_downLimit + 250f - textBoxes[i].textTexture.y) / 500;
                }

                if (upAlpha < 0f) { upAlpha = 0f; }
                if (downAlpha < 0f) { downAlpha = 0f; }

                textBoxes[i].textTexture._renderLayer._meshRenderer.material.SetFloat("_UpAlpha", Mathf.Lerp(lastUp, upAlpha, timeStacker));
                textBoxes[i].textTexture._renderLayer._meshRenderer.material.SetFloat("_DownAlpha", Mathf.Lerp(lastDown, downAlpha, timeStacker));
                
            }
            titleBack.alpha = 0.25f * owner.blurSprite.alpha;
            titleBox.textTexture.alpha = owner.blurSprite.alpha;
        }
    }
}
