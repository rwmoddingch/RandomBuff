using RandomBuff.Render.CardRender;
using RandomBuff.Render.UI.Component;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Credit.CreditObject
{
    internal class ScrollNameLabel : BuffCreditStageObject
    {
        protected string string1;
        bool isTitle;

        protected TMProFLabel label_1;

        bool removeRequested;
        bool labelInited;

        int alphaCounter, lastAlphaCounter;
        Vector2 screenSize;

        public ScrollNameLabel(Menu.Menu menu, BuffCreditStage owner, float inStageEnterTime, float lifeTime, string string1, bool isTitle) : base(menu, owner, Vector2.zero, inStageEnterTime, lifeTime)
        {
            screenSize = Custom.rainWorld.options.ScreenSize;
            this.string1 = string1;
            this.isTitle = isTitle;
        }

        public void InitLabel()
        {
            labelInited = true;
            if (isTitle)
                Container.AddChild(label_1 = new TMProFLabel(CardBasicAssets.TitleFont, string1, new Vector2(screenSize.x / 2f, 0f), 0.8f) { color = Color.white, alpha = 0f, Pivot = new Vector2(0.5f, 0.5f), });
            else
                Container.AddChild(label_1 = new TMProFLabel(CardBasicAssets.TitleFont, string1, new Vector2(screenSize.x / 2f, 0f), 0.8f) { color = Color.white * 0.3f + Color.black * 0.7f, alpha = 0f, Pivot = new Vector2(0.5f, 0.5f), scale = 0.8f });
            lastPos = pos = new Vector2(screenSize.x / 2f, 0f);
        }

        public override void Update()
        {
            base.Update();
            if (removed)
                return;
            lastAlphaCounter = alphaCounter;

            if (LifeParam > 0f && LifeParam < 1f && !removeRequested)
            {
                float deltaTime = CreditStage.StageTime - inStageEnterTime;
                if (!labelInited)
                    InitLabel();
                if (alphaCounter < 40 && deltaTime < lifeTime - 1)
                    alphaCounter++;
                else if (alphaCounter > 0 && deltaTime >= lifeTime - 1)
                    alphaCounter--;

                pos.y = Mathf.Lerp(0f, screenSize.y, LifeParam);
            }
            else
            {
                if (alphaCounter > 0)
                    alphaCounter--;
                if (lastAlphaCounter == alphaCounter && alphaCounter == 0 && removeRequested)
                {
                    RemoveLabel();
                    ableToRemove = true;
                }                    
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (!labelInited)
                return;

            label_1.SetPosition(Vector2.Lerp(lastPos, pos, timeStacker));
            label_1.alpha = Mathf.Lerp(lastAlphaCounter / 40f, alphaCounter / 40f, timeStacker);
        }

        public override void RequestRemove()
        {
            removeRequested = true;
            base.RequestRemove();
        }

        public void RemoveLabel()
        {
            labelInited = false;
            label_1?.RemoveFromContainer();
            label_1 = null;
        }

        public override void RemoveSprites()
        {
            if (removed)
                return;
            RemoveLabel();
            base.RemoveSprites();
        }
    }
}
