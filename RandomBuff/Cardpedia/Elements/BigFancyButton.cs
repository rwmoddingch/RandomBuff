using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using Menu;

namespace RandomBuff.Cardpedia.Elements
{
    public class BigFancyButton : ButtonTemplate
    {
        public bool inited;
        public static string[] Type = new string[3]
        {
            "Negative",
            "Positive",
            "Duality"
        };
        public string currentType;
        public FSprite fillSprite;
        public FSprite flatSprite;
        public FLabel chiLabel;
        public float SetAlpha;
        public float lastLightUp;
        public float lighUp;
        public Vector2 destinyPos;

        public BigFancyButton(string type, Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            SetAlpha = 1.0f;
            currentType = type;
            this.menu = menu;
            string fillName = currentType + "_Fill";
            string flatName = currentType + "_Flat";
            fillSprite = new FSprite("buffassets/illustrations/" + fillName);
            flatSprite = new FSprite("buffassets/illustrations/" + flatName);
            float num = currentType == "Negative" ? 180f : (currentType == "Positive" ? -180f : 0);
            fillSprite.SetPosition(683, 363 + num);
            fillSprite.scale = 1f;
            fillSprite.shader = menu.manager.rainWorld.Shaders["MenuTextCustom"];
            //fillSprite.alpha = 0;
            flatSprite.SetPosition(683, 363 + num);
            flatSprite.scale = 1f;
            flatSprite.color = new Color(0.4f, 0.4f, 0.4f);
            
            if (currentType == "Duality")
            {                
                fillSprite.color = new Color(0.85f,0.85f,0.85f);
            }
            else if (currentType == "Negative")
            {
                fillSprite.color = new Color(0.9f, 0f, 0.10f);
            }
            else
            {
                fillSprite.color = new Color(0f, 1f, 0.85f);
            }
                      
            Container.AddChild(flatSprite);
            Container.AddChild(fillSprite);

            if (menu.manager.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese)
            {
                string str = currentType == "Negative" ? "负面卡牌":(currentType == "Positive"? "正面卡牌" : "双性卡牌");
                chiLabel = new FLabel(Custom.GetFont(), str);
                chiLabel.scale = 2f;
                chiLabel.SetPosition(flatSprite.GetPosition() - new Vector2(310f, 20f));
                chiLabel.color = fillSprite.color;
                chiLabel.shader = fillSprite.shader;
                Container.AddChild(chiLabel);
            }

            inited = true;
        }

        public override void Update()
        {
            base.Update();
            lastLightUp = lighUp;
            if (!MouseOver)
            {
                if (lighUp > 0) lighUp -= 0.1f; 
            }
            else
            {
                if (lighUp < 1) lighUp += 0.1f;
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (inited)
            {
                float num = Mathf.Lerp(lastLightUp,lighUp,timeStacker);
                flatSprite.color = Color.Lerp(new Color(0.4f, 0.4f, 0.4f), Color.white, num);
            }
        }

        public override bool MouseOver
        {
            get
            {
                if(!inited) return false;
                if (menu is CardpediaMenu && (menu as CardpediaMenu).BrowsingCards) return false;

                Vector2 screenPos = fillSprite.GetPosition();
                return this.menu.mousePosition.x > screenPos.x - 381f && this.menu.mousePosition.y > screenPos.y - 85f && this.menu.mousePosition.x < screenPos.x + 381f && this.menu.mousePosition.y < screenPos.y + 85f;
            }
        }

        public Vector2[] MouseOverPos()
        {
            return new Vector2[3]
            {
                    new Vector2 (0,0),
                    new Vector2 (20f,100f),
                    new Vector2 (60f,100f),
            };
        }

        public override void Clicked()
        {
            if (!MouseOver) return;
            if (!(menu is CardpediaMenu)) return;

            if (!(menu as CardpediaMenu).BrowsingCards)
            {
                (menu as CardpediaMenu).BrowsingCards = true;
                (menu as CardpediaMenu).currentType = currentType == "Negative"? Core.Buff.BuffType.Negative : (currentType == "Positive"? Core.Buff.BuffType.Positive : Core.Buff.BuffType.Duality);
            }
        }        
    }
}
