using RandomBuff.Render.UI.Component;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Credit.CreditObject
{
    internal class FlagHolder : BuffCreditStageObject
    {
        RandomBuffFlag flag;
        RandomBuffFlagRenderer flagRenderer;

        Vector2 flagHangPos;
        Vector2 flagHidePos;

        public FlagHolder(Menu.Menu menu, BuffCreditStage owner, float inStageEnterTime, float lifeTime) : base(menu, owner, Vector2.zero, inStageEnterTime, lifeTime)
        {
            flag = new RandomBuffFlag(new IntVector2(60, 30), new Vector2(1200f, 500f));
            flagRenderer = new RandomBuffFlagRenderer(flag, RandomBuffFlagRenderer.FlagType.OuterTriangle, RandomBuffFlagRenderer.FlagColorType.Golden);
            flagHangPos = new Vector2(Custom.rainWorld.screenSize.x / 2f - flag.rect.x / 2f, 850f);
            flagHidePos = flagHangPos + Vector2.up * 800f;
            flagRenderer.pos = flagHangPos;
            flagRenderer.customAlpha = true;
            Container.AddChild(flagRenderer.container);
        }

        public override void Update()
        {
            base.Update();


            if(LifeParam >= 0f && LifeParam < 1f && !ableToRemove)
            {
                float detlaTime = CreditStage.StageTime - inStageEnterTime;

                float t1 = 0f;
                float t2 = 0f;

                if (detlaTime < 4f)
                {
                    t1 = detlaTime / 4f;
                    if (!flagRenderer.Show)
                        flagRenderer.Show = true;
                }
                else
                    t1 = 1f;


                if (detlaTime > lifeTime - 2f)
                {
                    t2 = (lifeTime - detlaTime) / 2f;
                    if (flagRenderer.Show)
                        flagRenderer.Show = false;
                }
                else
                    t2 = 1f;

                flag.Update();
                flagRenderer.Update();

                flagRenderer.pos = Vector2.Lerp(flagHidePos, flagHangPos, Helper.EaseInOutCubic(t1));
                flagRenderer.alpha = t2;
            }
            else if(LifeParam > 1f && !ableToRemove)
            {
                flagRenderer.alpha = 0f;
                ableToRemove = true;
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if(!ableToRemove)
                flagRenderer.GrafUpdate(timeStacker);
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            flagRenderer.container.RemoveFromContainer();
            flag = null;
            flagRenderer = null;
        }
    }
}
