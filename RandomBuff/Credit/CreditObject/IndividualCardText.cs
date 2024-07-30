using Menu;
using RandomBuff.Render.UI.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Credit.CreditObject
{
    internal class IndividualCardText : BuffCreditStageObject
    {
        bool slateForDeletion;
        CardTitle cardTitle;

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

        public IndividualCardText(Menu.Menu menu, BuffCreditStage owner, Vector2 pos, string text, float inStageEnterTime, float lifeTime) : base(menu, owner, pos, inStageEnterTime, lifeTime)
        {
            myContainer = new FContainer();
            menu.container.AddChild(myContainer);

            cardTitle = new CardTitle(myContainer, CardTitle.GetNormScale(1f), ScreenPos, 0.1f, 0f, CardTitle.GetNormFlipCounter(false), CardTitle.GetNormFlipDelay(false), CardTitle.GetNormSpanAdjust(1f));
            cardTitle.RequestSwitchTitle(text);
        }

        public override void Update()
        {
            base.Update();
            cardTitle.pos = ScreenPos;
            cardTitle.Update();

            if (!ableToRemove && slateForDeletion && cardTitle != null && cardTitle.readyForSwitch)
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
        }
    }
}
