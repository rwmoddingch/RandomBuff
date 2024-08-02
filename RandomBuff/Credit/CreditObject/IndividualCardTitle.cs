using Menu;
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
    internal class IndividualCardTitle : BuffCreditStageObject
    {
        static float endScale = 0.5f;
        bool slateForDeletion;
        CardTitle cardTitle;
        int dontRemoveCounter;

        public float DynamicScale
        {
            get => myContainer.scale;
            set => myContainer.scale = value;
        }

        string _text;
        public string Text
        {
            get => _text;
            set
            {
                if (value != _text && !slateForDeletion)
                {
                    _text = value;
                    cardTitle.RequestSwitchTitle(value);
                }
            }
        }

        public float Alpha
        {
            get => myContainer.alpha;
            set => myContainer.alpha = value;
        }
        public Vector2 ScreenSize => Custom.rainWorld.options.ScreenSize;

        Vector2 hangPos;
        Vector2 denPos;

        public IndividualCardTitle(Menu.Menu menu, BuffCreditStage owner, string text, float inStageEnterTime, float lifeTime, bool noAnim = false) : base(menu, owner, Vector2.zero, inStageEnterTime, lifeTime)
        {
            myContainer = new FContainer();
            menu.container.AddChild(myContainer);

            cardTitle = new CardTitle(myContainer, CardTitle.GetNormScale(0.5f), Vector2.zero, 0.1f, 0.5f, CardTitle.GetNormFlipCounter(false), CardTitle.GetNormFlipDelay(false), CardTitle.GetNormSpanAdjust(0.5f));
            myContainer.SetPosition(ScreenSize / 2f);
            cardTitle.RequestSwitchTitle(text);

            if (!noAnim)
            {
                AnimMachine.GetDelayCmpnt(40 * 3, autoDestroy: true).BindActions(OnAnimFinished: (t) =>
                {
                    hangPos = ScreenSize / 2f;
                    denPos = new Vector2(0f + (cardTitle.rect.x / 2f) * endScale, ScreenSize.y - cardTitle.rect.y * endScale / 2f);

                    AnimMachine.GetTickAnimCmpnt(0, 80, autoDestroy: true).BindActions(OnAnimGrafUpdate: (ta, f) =>
                    {
                        float scale = Mathf.Lerp(1f, endScale, ta.Get());
                        float x = Mathf.Lerp(hangPos.x, denPos.x, ta.Get());
                        float y = Mathf.Lerp(hangPos.y, denPos.y, Mathf.Pow(ta.Get(), 3f));
                        myContainer.SetPosition(x, y);
                        myContainer.scale = scale;
                    }).BindModifier(Helper.EaseInOutCubic);
                });
            }
        }

        public override void Update()
        {
            base.Update();
            cardTitle.Update();

            if (LifeParam >= 1f && !slateForDeletion)
                RequestRemove();

            if (dontRemoveCounter > 0)
                dontRemoveCounter--;

            if (!ableToRemove && slateForDeletion && cardTitle != null && cardTitle.readyForSwitch && dontRemoveCounter == 0)
            {
                ableToRemove = true;
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            cardTitle.GrafUpdate(timeStacker);
        }

        public override void RemoveSprites()
        {
            cardTitle.Destroy();
            cardTitle = null;
            base.RemoveSprites();
        }

        public override void RequestRemove()
        {
            if (slateForDeletion)
                return;

            slateForDeletion = true;
            cardTitle.RequestSwitchTitle("");
            dontRemoveCounter = 3;
        }
    }
}
