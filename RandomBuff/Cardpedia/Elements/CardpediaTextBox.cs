using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using RandomBuff.Cardpedia.InfoPageRender;
using UnityEngine;
using RWCustom;

namespace RandomBuff.Cardpedia.Elements
{
    public class CardpediaTextBox
    {
        public PediaTextRenderer pediaTextRenderer;
       
        public bool isTitle;
        public bool needScroll;
        public int lineLength;
        public int lineCounts;
        public float lastAlpha;
        public float SetAlpha;
        public float lastYOffSet;
        public float lastScaleY;
        public float lineHeight = 26.28f;
        public int id;
        public Color textCol;

        public FTexture textTexture;
        public FSprite uselessSprite;
        public FSprite blurSprite;

        public FContainer Container;
        public Vector2 horizontalLimit;
        public Vector2 verticalLimit;
        public Vector2 textHeight;
        public Vector2 textWidth;
        public FShader shader;

        public bool inited;

        public CardpediaTextBox(PediaTextRenderer textRenderer, bool isTitle, Color textColor, float textSize, int id)
        {
            pediaTextRenderer = textRenderer;
            this.isTitle = isTitle;
            this.id = id;            
            Container = new FContainer();
            //lineLength = isChinese? 28 : 36;
            textCol = textColor;
            if (isTitle)
            {
                shader = Custom.rainWorld.Shaders["FoldableTextHorizontal"];
            }
            else shader = Custom.rainWorld.Shaders["FoldableTextVertical"];
            Init(isTitle);
            inited = true;
        }

        public void Init(bool isTitle)
        {
            if (pediaTextRenderer.pediaText == null || pediaTextRenderer.pediaCamera == null)
            {
                pediaTextRenderer.Init("Content", id, isTitle, 0.8f);
            }
            uselessSprite = new FSprite("pixel");
            uselessSprite.color = Color.black;
            pediaTextRenderer.pediaText.RefreshText(" ",10);
            RenderTexture renderTexture = pediaTextRenderer.pediaCamera.targetTexture;
            textTexture = new FTexture(renderTexture);
            textTexture.shader = shader;
            textTexture.SetPosition(new Vector2(511.5f, 393 + lineHeight * (lineCounts - 1)));
        }

        public void RefreshTextTexture(FContainer mouseContainer, string newContent, Vector2 newPositon)
        {
            RefreshLineLength();
            lineCounts = (int)Mathf.Ceil((float)PediaText.GenerateLongText(newContent, lineLength).Length / lineLength);
            pediaTextRenderer.pediaText.RefreshText(newContent,lineLength);
            textTexture.shader = shader;
            if (isTitle)
            {
                textTexture._renderLayer._meshRenderer.material.SetFloat("_LeftAlpha", 1f);
                textTexture._renderLayer._meshRenderer.material.SetFloat("_RightAlpha", 1f);
                textTexture._renderLayer._meshRenderer.material.SetColor("_SetColor", textCol);
            }
            else
            {
                //textTexture._renderLayer._meshRenderer.material.SetFloat("_UpAlpha", 1f);
                //textTexture._renderLayer._meshRenderer.material.SetFloat("_DownAlpha", 1f);
                textTexture._renderLayer._meshRenderer.material.SetColor("_SetColor", textCol);
            }
            //textTexture.SetPosition(newPositon + new Vector2(0, lineHeight * (lineCounts - 1)));
            textTexture.SetPosition(newPositon);
            mouseContainer.RemoveChild(textTexture);
            mouseContainer.AddChild(textTexture);
        }

        public void RefreshLineLength()
        {
            if (!isTitle)
            {
                if (Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese) lineLength = 25;
                else lineLength = 45;
            }
            else
            {
                if (Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese) lineLength = 12;
                else lineLength = 20;
            }
        }

        public void Draw(float timeStacker)
        {

        }
    }
}
