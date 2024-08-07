using Menu.Remix.MixedUI;
using RandomBuff.Render.CardRender;
using RandomBuff.Render.UI.Component;
using RWCustom;
using System.Xml;
using UnityEngine;

namespace RandomBuff.Credit.CreditObject
{
    internal class NameDetailLabel : BuffCreditStageObject
    {
        protected static float gap = 15f;

        protected string string1;
        protected string string2;

        protected TMProFLabel label_1;
        protected TMProFLabel label_2;

        float width1;

        Vector2 denPos;
        Vector2 hangPos;

        float alpha;
        float lastAlpha;

        bool requestRemove;
        int removeCounter;

        float t;
        int updatePosCounter = 4;

        public NameDetailLabel(Menu.Menu menu, BuffCreditStage owner, Vector2 endPos, float inStageEnterTime, float lifeTime, string string1, string string2) : base(menu, owner, Vector2.zero, inStageEnterTime, lifeTime)
        {
            denPos = endPos;
            hangPos = endPos + Vector2.right * 100f;
            lastPos = pos = hangPos;
            this.string1 = string1;
            this.string2 = string2;

            InitLabels();
        }

        public virtual void InitLabels()
        {
            Container.AddChild(label_1 = new TMProFLabel(CardBasicAssets.TitleFont, string1, new Vector2(1000f, 100f), 0.8f) { color = Color.white, alpha = 0f, Pivot = new Vector2(0f, 0.5f), Alignment = TMPro.TextAlignmentOptions.Left });
            Container.AddChild(label_2 = new TMProFLabel(CardBasicAssets.TitleFont, string2, new Vector2(1000f, 100f), 0.8f) { color = Color.white * 0.3f + Color.black * 0.7f, alpha = 0f, Pivot = new Vector2(0f, 0.5f), Alignment = TMPro.TextAlignmentOptions.Left });
            width1 = label_1.TextRect.x + gap;
        }

        public virtual void RecaculateTextRectParam()
        {
            width1 = label_1.TextRect.x + gap;
        }

        public override void Update()
        {
            base.Update();
            if (removed)
                return;
            if(updatePosCounter > 0)
            {
                updatePosCounter--;
                if(updatePosCounter == 0)
                {
                    RecaculateTextRectParam();
                }
            }

            t = 0f;
            if (LifeParam > 0f && LifeParam < 1f)
            {
                float deltaTime = CreditStage.StageTime - inStageEnterTime;
                if (deltaTime < 2f)
                {
                    t = deltaTime / 2f;
                }
                else if (deltaTime > lifeTime - 2f)
                {
                    t = (lifeTime - deltaTime) / 2f;
                }
                else
                    t = 1f;
            }
            if (requestRemove)
            {
                if(removeCounter > 0)
                {
                    removeCounter--;
                    if (removeCounter == 0)
                        ableToRemove = true;
                }
                t = removeCounter / 40f;
            }
            if (LifeParam > 1f)
                ableToRemove = true;


            pos = Vector2.Lerp(hangPos, denPos, Helper.LerpEase(t));

            lastAlpha = alpha;
            alpha = Mathf.Lerp(0f, 1f, Helper.LerpEase(t));
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (removed)
                return;
            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);
            label_1.SetPosition(smoothPos);
            label_2.SetPosition(smoothPos + new Vector2(width1, 0f));
            
            float smoothAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
            label_1.alpha = smoothAlpha;
            label_2.alpha = smoothAlpha;
        }

        public override void RequestRemove()
        {
            requestRemove = true;
            removeCounter = Mathf.FloorToInt(40 * t);
        }

        public override void RemoveSprites()
        {
            if (removed)
                return;
            label_1.RemoveFromContainer();
            label_2.RemoveFromContainer();
            base.RemoveSprites();
        }
    }
}
