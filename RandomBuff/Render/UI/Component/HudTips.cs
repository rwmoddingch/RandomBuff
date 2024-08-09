using Menu.Remix.MixedUI;
using RandomBuff.Core.Option;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI.Component
{
    internal class HudTips
    {
        FContainer ownerContainer;

        FSprite dark;
        FLabel tipLabel;

        TickAnimCmpnt anim;
        bool show = false;

        float initAlpha;
        public bool slateForDeletion;

        public HudTips(FContainer ownerContainer)
        {
            this.ownerContainer = ownerContainer;

            dark = new FSprite("Futile_White") { shader = Custom.rainWorld.Shaders["FlatLight"],color = Color.black, alpha = 0f };
            dark.SetPosition(new Vector2(Custom.rainWorld.options.ScreenSize.x, 0f));
            ownerContainer.AddChild(dark);

            string text = string.Format(BuffResourceString.Get("HudTips"), BuffOptionInterface.Instance.CardSlotKey.Value);
            float width = LabelTest.GetWidth(text) * 1.1f;
            Vector2 pos = new Vector2(Custom.rainWorld.options.ScreenSize.x - 20f - width / 2f, 20f);

            tipLabel = new FLabel(Custom.GetFont(), text) { scale = 1.1f};
            tipLabel.SetPosition(pos);
            ownerContainer.AddChild(tipLabel);

            dark.scaleX = width;
            dark.scaleY = 40f;

            show = true;
            FireUpAnim();
        }

        public void ClearSprites()
        {
            dark.RemoveFromContainer();
            tipLabel.RemoveFromContainer();
        }

        public void Hide()
        {
            show = false;
            FireUpAnim();
        }

        public void FireUpAnim()
        {
            if (anim != null)
                anim.Destroy();
            initAlpha = tipLabel.alpha;

            anim = AnimMachine.GetTickAnimCmpnt(0, 80, autoDestroy: true).BindActions(OnAnimGrafUpdate: (t, f) =>
            {
                float alpha = Mathf.Lerp(initAlpha, show ? 1f : 0f, t.Get());
                dark.alpha = alpha;
                tipLabel.alpha = alpha;
            }, OnAnimFinished:(t) =>
            {
                anim = null;
                float alpha = show ? 1f : 0f;
                dark.alpha = alpha;
                tipLabel.alpha = alpha;
                if (!show)
                {
                    ClearSprites();
                }
            });
        }

        public static bool TryGetHudTips(FContainer ownerContainer, out HudTips hudTips)
        {
            if (!BuffOptionInterface.Instance.DisableCardSlotText.Value)
            {
                hudTips = new HudTips(ownerContainer);
                BuffOptionInterface.Instance.DisableCardSlotText.Value = true;
                BuffOptionInterface.SaveConfig();
                return true;
            }
            else
                hudTips = null;
            return false;
        }
    }
}
