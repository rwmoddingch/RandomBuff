using Menu;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI.Notification
{
    internal class NotificationBanner
    {
        static Vector2 buttonSize = new Vector2(60f, 30f);

        protected NotificationManager notificationManager;
        protected NotificationBannerButton notificationBannerButton;

        protected FSprite blackBackground;
        protected FSprite blurSprite;
        protected FSprite lineUp;
        protected FSprite lineDown;
        protected FLabel titleLabel;

        protected Vector2 bannerScale;
        protected Vector2 screenCenter;

        public float alpha;
        protected float lastAlpha;

        public float expand;
        protected float lastExpand;

        public float contentExpand;
        protected float lastContentExpand;

        public int stateCounter;
        public int maxStateCounter;

        public State currentState;


        public NotificationBanner(NotificationManager notificationManager)
        {
            this.notificationManager = notificationManager;
            bannerScale = new Vector2(Custom.rainWorld.options.ScreenSize.x, 400f);
            screenCenter = Custom.rainWorld.options.ScreenSize / 2f;

            InitSprites();
            SwitchState(State.ExpandBanner);
        }

        public virtual void InitSprites()
        {
            blackBackground = new FSprite("pixel") { anchorX = 0.5f, anchorY = 0.5f, color = Color.black };
            blurSprite = new FSprite("pixel") { shader = Custom.rainWorld.Shaders["UIBlur"], anchorX = 0.5f, anchorY = 0.5f };
            titleLabel = new FLabel(Custom.GetDisplayFont(), "已获得奖励:") { shader = Custom.rainWorld.Shaders["MenuTextCustom"], anchorX = 0.5f, anchorY = 1f};
            lineUp = new FSprite("pixel") { shader = Custom.rainWorld.Shaders["MenuTextCustom"], anchorX = 0.5f, anchorY = 0.5f };
            lineDown = new FSprite("pixel") { shader = Custom.rainWorld.Shaders["MenuTextCustom"], anchorX = 0.5f, anchorY = 0.5f };

            notificationManager.ownerContainer.AddChild(blackBackground);
            notificationManager.ownerContainer.AddChild(blurSprite);
            notificationManager.ownerContainer.AddChild(titleLabel);
            notificationManager.ownerContainer.AddChild(lineUp);
            notificationManager.ownerContainer.AddChild(lineDown);

            notificationManager.subObjects.Add(notificationBannerButton = new NotificationBannerButton(notificationManager.menu, notificationManager, "确认", screenCenter + new Vector2(0f, -bannerScale.y / 2f + 30f) - buttonSize / 2f, buttonSize, Menu.MenuColorEffect.rgbMediumGrey, OnOkButtonClick));
        }

        public virtual void Update()
        {
            lastAlpha = alpha;
            lastExpand = expand;
            lastContentExpand = contentExpand;

            InStateUpdate();
        }

        protected virtual void OnOkButtonClick()
        {
            SwitchState(State.CollapseContent);
        }

        public virtual void InStateUpdate()
        {
            if (stateCounter < maxStateCounter)
                stateCounter++;

            if(currentState == State.ExpandBanner)
            {
                alpha = Helper.LerpEase(Mathf.InverseLerp(0, maxStateCounter, stateCounter));
                expand = Helper.LerpEase(Mathf.InverseLerp(0, maxStateCounter, stateCounter));
                if (stateCounter == maxStateCounter)
                    SwitchState(State.ExpandContent);
            }
            else if(currentState == State.ExpandContent)
            {
                alpha = 1f;
                expand = 1f;
                contentExpand = Helper.LerpEase(Mathf.InverseLerp(0, maxStateCounter, stateCounter));
                if (stateCounter == maxStateCounter)
                    SwitchState(State.Wait);
            }
            else if(currentState == State.Wait)
            {
                //pass; 
            }
            else if(currentState == State.CollapseContent)
            {
                alpha = 1f;
                expand = 1f;
                contentExpand = Helper.LerpEase(1f - Mathf.InverseLerp(0, maxStateCounter, stateCounter));
                if (stateCounter == maxStateCounter)
                    SwitchState(State.CollapseBanner);
            }
            else if(currentState == State.CollapseBanner)
            {
                alpha = Helper.LerpEase(1f - Mathf.InverseLerp(0, maxStateCounter, stateCounter));
                expand = Helper.LerpEase(1f - Mathf.InverseLerp(0, maxStateCounter, stateCounter));
                if (stateCounter == maxStateCounter)
                    Destroy();
            }
            notificationBannerButton.alpha = contentExpand;
        }

        public virtual void SwitchState(State newState)
        {
            currentState = newState;
            stateCounter = 0;

            BuffPlugin.Log($"Banner switch state to {newState}");

            if (newState == State.ExpandBanner)
            {
                maxStateCounter = 20;
            }
            else if (newState == State.ExpandContent)
            {
                maxStateCounter = 20;
            }
            else if (newState == State.Wait)
            {
                maxStateCounter = 0;
            }
            else if (newState == State.CollapseContent)
            {
                maxStateCounter = 20;
            }
            else if (newState == State.CollapseBanner)
            {
                maxStateCounter = 20;
            }
        }

        public virtual void GrafUpdate(float timeStacker)
        {
            float smootAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
            float smoothExpand = Mathf.Lerp(lastExpand ,expand, timeStacker);
            float smoothContentExpand = Mathf.Lerp(lastContentExpand ,contentExpand, timeStacker);

            blurSprite.alpha = smoothContentExpand;
            blurSprite.scaleX = bannerScale.x;
            blurSprite.scaleY = bannerScale.y;
            blurSprite.SetPosition(screenCenter);

            blackBackground.alpha = smoothContentExpand * 0.5f;
            blackBackground.scaleX = bannerScale.x;
            blackBackground.scaleY = bannerScale.y;
            blackBackground.SetPosition(screenCenter);

            titleLabel.scaleY = smoothContentExpand;
            titleLabel.SetPosition(screenCenter + new Vector2(0, bannerScale.y / 2f - 10));

            lineUp.scaleX = lineDown.scaleX = bannerScale.x * smoothExpand;
            lineUp.scaleY = lineDown.scaleY = 5f * smoothExpand;

            lineUp.SetPosition(screenCenter + new Vector2(0, bannerScale.y / 2f));
            lineDown.SetPosition(screenCenter + new Vector2(0, -bannerScale.y / 2f));
        }

        public virtual void Destroy()
        {
            blurSprite.RemoveFromContainer();
            titleLabel.RemoveFromContainer();
            lineUp.RemoveFromContainer();
            lineDown.RemoveFromContainer();
            notificationManager.banners.Remove(this);
            notificationManager.RemoveSubObject(notificationBannerButton);
            notificationBannerButton.RemoveSprites();
            notificationManager.RecoverFocus();
        }

        public enum State
        {
            ExpandBanner,
            ExpandContent,
            Wait,
            CollapseContent,
            CollapseBanner,
        }
    }

    internal class NotificationBannerButton : ButtonTemplate
    {
        Action callBack;
        public MenuLabel menuLabel;
        public RoundedRect roundedRect;
        public RoundedRect selectRect;
        public HSLColor labelColor;

        public float alpha;
        float lastAlpha;

        public NotificationBannerButton(Menu.Menu menu, NotificationManager notificationManager, string displayText, Vector2 pos, Vector2 size, Color color, Action callBack)
            : base(menu, notificationManager, pos, size)
        {
            this.callBack = callBack;
            labelColor = Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey);
            roundedRect = new RoundedRect(menu, this, new Vector2(0f, 0f), size, true);
            subObjects.Add(this.roundedRect);
            selectRect = new RoundedRect(menu, this, new Vector2(0f, 0f), size, false);
            subObjects.Add(this.selectRect);
            menuLabel = new MenuLabel(menu, this, displayText, new Vector2(0f, 0f), size, false, null);
            subObjects.Add(this.menuLabel);
        }

        public void SetSize(Vector2 newSize)
        {
            size = newSize;
            roundedRect.size = size;
            selectRect.size = size;
            menuLabel.size = size;
        }

        public override void Update()
        {
            base.Update();
            lastAlpha = alpha;
            roundedRect.fillAlpha = Mathf.Lerp(0.3f, 0.6f, buttonBehav.col) * alpha;
            roundedRect.addSize = new Vector2(10f, 6f) * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * 3.1415927f)) * (buttonBehav.clicked ? 0f : 1f);
            selectRect.addSize = new Vector2(2f, -2f) * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * 3.1415927f)) * (buttonBehav.clicked ? 0f : 1f);
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            float smoothAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
            menuLabel.label.color = InterpColor(timeStacker, labelColor);
            menuLabel.label.alpha = smoothAlpha;

            Color color = Color.Lerp(Menu.Menu.MenuRGB(Menu.Menu.MenuColors.Black), Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White), Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));
            for (int i = 0; i < 9; i++)
            {
                roundedRect.sprites[i].color = color;
            }
            float num = 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(buttonBehav.lastSin, buttonBehav.sin, timeStacker) / 30f * 3.1415927f * 2f);
            num *= buttonBehav.sizeBump;
            for (int j = 0; j < 8; j++)
            {
                selectRect.sprites[j].color = MyColor(timeStacker);
                selectRect.sprites[j].alpha = num * smoothAlpha;
            }

            for(int i = 0;i < 4; i++)
            {
                roundedRect.sprites[roundedRect.SideSprite(i)].alpha = smoothAlpha;
                roundedRect.sprites[roundedRect.CornerSprite(i)].alpha = smoothAlpha;
            }
        }

        public override void Clicked()
        {
            callBack?.Invoke();
        }
    }
}
