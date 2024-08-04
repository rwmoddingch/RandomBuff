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
        static float gap = 15f;

        TMProFLabel nameLabel;
        TMProFLabel detailLabel;

        float width1;

        Vector2 denPos;
        Vector2 hangPos;

        float alpha;
        float lastAlpha;

        bool requestRemove;
        int removeCounter;

        float t;
        int updatePosCounter = 4;

        public NameDetailLabel(Menu.Menu menu, BuffCreditStage owner, Vector2 endPos, float inStageEnterTime, float lifeTime, string name, string detail) : base(menu, owner, Vector2.zero, inStageEnterTime, lifeTime)
        {
            denPos = endPos;
            hangPos = endPos + Vector2.right * 100f;
            lastPos = pos = hangPos;

            Container.AddChild(nameLabel = new TMProFLabel(CardBasicAssets.TitleFont, name, new Vector2(1000f, 100f), 0.8f) { color = Color.white, alpha = 0f, Pivot = new Vector2(0f, 0.5f), Alignment = TMPro.TextAlignmentOptions.Left});
            Container.AddChild(detailLabel = new TMProFLabel(CardBasicAssets.TitleFont, detail, new Vector2(1000f, 100f), 0.8f) { color = Color.white * 0.3f + Color.black * 0.7f, alpha = 0f, Pivot = new Vector2(0f, 0.5f), Alignment = TMPro.TextAlignmentOptions.Left });
            width1 = nameLabel.TextRect.x + gap;
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
                    width1 = nameLabel.TextRect.x + gap;
                    //nameLabel.tmpText.color = Color.white;
                    //nameLabel.alpha = 0f;
                    //detailLabel.tmpText.color = Color.white * 0.3f + Color.black * 0.7f;
                    //detailLabel.alpha = 0f;
                    BuffPlugin.Log($"NameDetailLabel  {detailLabel.Text}-{width1}");
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
            nameLabel.SetPosition(smoothPos);
            detailLabel.SetPosition(smoothPos + new Vector2(width1, 0f));
            
            float smoothAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
            nameLabel.alpha = smoothAlpha;
            detailLabel.alpha = smoothAlpha;
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
            nameLabel.RemoveFromContainer();
            detailLabel.RemoveFromContainer();
            base.RemoveSprites();
        }
    }
}
