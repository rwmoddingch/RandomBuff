using RandomBuff.Core.Buff;
using RandomBuff.Render.CardRender;
using RandomBuff.Render.UI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Credit.CreditObject
{
    internal class BuffCardDisplayShelf : BuffCreditStageObject
    {
        static float cardScale = BuffCard.normalScale * 0.5f;
        static float edgeGap = 150f;

        bool slateForDeletion;
        Vector2 topLeftPos;

        BuffID[] buffIDs;
        Vector2[] denPosList;
        float[] startTime;
        bool[] added;

        BuffCard[] cards;
        Vector3[] rotations, lastRotations;
        float[] alphas, lastAlphas;

        public BuffCardDisplayShelf(Menu.Menu menu, BuffCreditStage owner, Vector2 topLeftPos, float inStageEnterTime, BuffID[] buffIDs) : base(menu, owner, Vector2.zero, inStageEnterTime, 0f)
        {
            this.topLeftPos = topLeftPos;
            lifeTime = buffIDs.Length * 0.5f + 5f;

            this.buffIDs = buffIDs;
            denPosList = new Vector2[buffIDs.Length];
            startTime = new float[buffIDs.Length];
            added = new bool[buffIDs.Length];
            cards = new BuffCard[buffIDs.Length];

            rotations = new Vector3[buffIDs.Length];
            lastRotations = new Vector3[buffIDs.Length];
            alphas = new float[buffIDs.Length];
            lastAlphas = new float[buffIDs.Length];


            Vector2 screenSize = Custom.rainWorld.options.ScreenSize;
            Vector2 cardRect = new Vector2(CardBasicAssets.RenderTextureSize.x, CardBasicAssets.RenderTextureSize.y) * cardScale * BuffCard.interactiveScaleBound;

            float x = topLeftPos.x + cardRect.x / 2f;
            float y = topLeftPos.y - cardRect.y / 2f;

            for(int i = 0; i < buffIDs.Length; i++)
            {
                startTime[i] = i * 0.5f;
                added[i] = false;
                denPosList[i] = new Vector2(x, y);
                x += cardRect.x;
                if(x >= screenSize.x - edgeGap)
                {
                    x = topLeftPos.x + cardRect.x / 2f;
                    y -= cardRect.y;
                }
            }
        }


        public override void Update()
        {
            base.Update();
            if(LifeParam > 0f && LifeParam < 1f && !slateForDeletion)
            {
                float localTime = CreditStage.StageTime - inStageEnterTime;

                for (int i = 0;i < buffIDs.Length;i++)
                {
                    if (localTime > startTime[i])//添加卡牌
                    {
                        if (!added[i])
                        {
                            added[i] = true;
                            cards[i] = new BuffCard(buffIDs[i]);
                            cards[i].Scale = cardScale;
                            Container.AddChild(cards[i].Container);
                            rotations[i] = lastRotations[i] = new Vector3(0f, 90f, 0f);
                        }
                        cards[i].Update();
                        float cardLocalTime = localTime - startTime[i];

                        float t = 0f;//绘制
                        if (cardLocalTime < 1f)
                        {
                            t = cardLocalTime;
                        }
                        else if (localTime > lifeTime - 1f)
                        {
                            t = lifeTime - localTime;
                        }
                        else
                            t = 1f;


                        lastRotations[i] = rotations[i];
                        rotations[i] = new Vector3(0f, 90f * Helper.EaseInOutCubic(1f - t), 0f);

                        lastAlphas[i] = alphas[i];
                        alphas[i] = 1f;
                    }
                }
            }
            else if((LifeParam > 1f || slateForDeletion) && !ableToRemove)
            {
                for(int i = 0;i < buffIDs.Length; i++)
                {
                    if (added[i])
                    {
                        added[i] = false;
                        cards[i].Container.RemoveFromContainer();
                        cards[i].Destroy();
                        cards[i] = null;
                    }
                }
                ableToRemove = true;
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            for(int i = 0;i < buffIDs.Length;i++)
            {
                if (!added[i])
                    continue;

                cards[i].GrafUpdate(timeStacker);
                cards[i].Position = denPosList[i];
                cards[i].Alpha = Mathf.Lerp(lastAlphas[i],alphas[i],timeStacker);
                cards[i].Rotation = Vector3.Lerp(lastRotations[i], rotations[i],timeStacker);
            }
        }

        public override void RequestRemove()
        {
            slateForDeletion = true;
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            if (!ableToRemove)
            {
                for (int i = 0; i < buffIDs.Length; i++)
                {
                    added[i] = false;
                    cards[i].Container.RemoveFromContainer();
                    cards[i].Destroy();
                    cards[i] = null;
                }
                ableToRemove = true;
            }
        }
    }
}
