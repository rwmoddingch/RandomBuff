using Menu;
using Menu.Remix.MixedUI;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuff.Core.Progression;
using RandomBuff.Render.Quest;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RandomBuff.Render.Quest.QuestRendererManager;

namespace RandomBuff.Render.UI.Notification
{
    internal class NotificationBanner
    {
        static Vector2 buttonSize = new Vector2(60f, 30f);

        protected NotificationManager notificationManager;
        protected List<NotificationBannerButton> buttons = new();

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
            ActuallyAddButtons();
        }

        public virtual void InitSprites()
        {
            blackBackground = new FSprite("pixel") { anchorX = 0.5f, anchorY = 0.5f, color = Color.black };
            blurSprite = new FSprite("pixel") { shader = Custom.rainWorld.Shaders["UIBlur"], anchorX = 0.5f, anchorY = 0.5f };
            titleLabel = new FLabel(Custom.GetDisplayFont(), "") { shader = Custom.rainWorld.Shaders["MenuTextCustom"], anchorX = 0.5f, anchorY = 1f, scale = 1.5f};
            lineUp = new FSprite("pixel") { shader = Custom.rainWorld.Shaders["MenuTextCustom"], anchorX = 0.5f, anchorY = 0.5f };
            lineDown = new FSprite("pixel") { shader = Custom.rainWorld.Shaders["MenuTextCustom"], anchorX = 0.5f, anchorY = 0.5f };

            notificationManager.ownerContainer.AddChild(blackBackground);
            notificationManager.ownerContainer.AddChild(blurSprite);
            notificationManager.ownerContainer.AddChild(titleLabel);
            notificationManager.ownerContainer.AddChild(lineUp);
            notificationManager.ownerContainer.AddChild(lineDown);

            AddButton(BuffResourceString.Get("Notificaion_ConfirmButton"), Menu.MenuColorEffect.rgbMediumGrey, OnOkButtonClick);
            //AddButton("Wawa", Color.green, OnOkButtonClick);
            //AddButton("Wawa2", Color.red, OnOkButtonClick);
        }

        List<Action<int, float>> buttonCreators = new(); 
        public void AddButton(string text, Color color, Action callBack)
        {
            buttonCreators.Add((i, mid) =>
            {
                var button = new NotificationBannerButton(notificationManager.menu, notificationManager, text, screenCenter + new Vector2(0f, -bannerScale.y / 2f + 30f) - buttonSize / 2f + new Vector2((i - mid) * (buttonSize.x + 10), 0), buttonSize, color, callBack);
                notificationManager.subObjects.Add(button);
                buttons.Add(button);
                BuffPlugin.Log($"Create notification button : {text}-{color}");
            });
        }

        public void ActuallyAddButtons()
        {
            float mid = (buttonCreators.Count - 1) / 2f;
            for(int i = 0; i < buttonCreators.Count; i++)
            {
                buttonCreators[i].Invoke(i, mid);
            }
            buttonCreators.Clear();
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
            foreach(var button in buttons)
                button.alpha = contentExpand;
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

            titleLabel.scaleY = smoothContentExpand * 1.5f;
            titleLabel.SetPosition(screenCenter + new Vector2(0, bannerScale.y / 2f - 10));

            lineUp.scaleX = lineDown.scaleX = bannerScale.x * smoothExpand;
            lineUp.scaleY = lineDown.scaleY = 5f * smoothExpand;

            lineUp.SetPosition(screenCenter + new Vector2(0, bannerScale.y / 2f));
            lineDown.SetPosition(screenCenter + new Vector2(0, -bannerScale.y / 2f));
        }

        public virtual void Destroy()
        {
            blackBackground.RemoveFromContainer();
            blurSprite.RemoveFromContainer();
            titleLabel.RemoveFromContainer();
            lineUp.RemoveFromContainer();
            lineDown.RemoveFromContainer();
            notificationManager.banners.Remove(this);
            foreach(var button in buttons)
            {
                notificationManager.RemoveSubObject(button);
                button.RemoveSprites();
            }
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

        Color mediumColor;

        public float alpha;
        float lastAlpha;

        public NotificationBannerButton(Menu.Menu menu, NotificationManager notificationManager, string displayText, Vector2 pos, Vector2 size, Color color, Action callBack)
            : base(menu, notificationManager, pos, size)
        {
            this.callBack = callBack;
            labelColor = new HSLColor(Custom.RGB2HSL(color).x, Custom.RGB2HSL(color).y, Custom.RGB2HSL(color).z);
            roundedRect = new RoundedRect(menu, this, new Vector2(0f, 0f), size, true);
            subObjects.Add(this.roundedRect);
            selectRect = new RoundedRect(menu, this, new Vector2(0f, 0f), size, false);
            subObjects.Add(this.selectRect);
            menuLabel = new MenuLabel(menu, this, displayText, new Vector2(0f, 0f), size, false, null);
            subObjects.Add(this.menuLabel);

            rectColor = labelColor;
            mediumColor = color;
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

    internal class RewardBanner : NotificationBanner
    {
        public float rewardInstanceHeight;
        List<RewardInstance> rewardInstances = new();
        List<FSprite> splitLines = new();
        List<Vector2> linePoses = new();

        QuestRendererManager questRendererManager;

        public RewardBanner(NotificationManager notificationManager) : base(notificationManager)
        {
            rewardInstanceHeight = bannerScale.y / 2f;
            questRendererManager = new QuestRendererManager(notificationManager.Container, QuestRendererManager.Mode.NotificationBanner);
        }

        public void AppendReward(QuestUnlockedType questUnlockedType, string itemName)
        {
            if(questUnlockedType == QuestUnlockedType.Mission)
            {
                MissionID id = new MissionID(itemName);
                var newInstance = new MissionReward(id, this);
                newInstance.InitSprites();
                rewardInstances.Add(newInstance);

                if(rewardInstances.Count > 1)
                {
                    splitLines.Add(new FSprite("pixel") { scaleX = 2f, scaleY = rewardInstanceHeight });
                    notificationManager.Container.AddChild(splitLines.Last());
                    linePoses.Add(Vector2.zero);
                }
            }
            RecaculateInstancePos();

            void RecaculateInstancePos()
            {
                float totalWidth = rewardInstances.Sum((instance) => instance.width);
                float mid = totalWidth / 2f;
                for(int i = 0;i < rewardInstances.Count;i++)
                {
                    float deltaX = -mid;
                    deltaX += rewardInstances[i].width / 2f;
                    for(int k = 0;k < i; k++)
                        deltaX += rewardInstances[k].width;

                    rewardInstances[i].pos = new Vector2(deltaX + screenCenter.x, screenCenter.y);

                    if(i < rewardInstances.Count - 1 && splitLines.Count > 0)
                    {
                        linePoses[i] = new Vector2(deltaX + rewardInstances[i].width / 2f +screenCenter.x, screenCenter.y);
                    }
                }
            }
        }

        public override void InitSprites()
        {
            base.InitSprites();
            titleLabel.text = BuffResourceString.Get("Notification_RewardTitle");
        }

        public override void Update()
        {
            base.Update();
            questRendererManager.Update();
            foreach(var instance in rewardInstances) 
                instance.Update();
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            questRendererManager.GrafUpdate(timeStacker);

            foreach (var instance in rewardInstances)
                instance.GrafUpdate(timeStacker);

            float smoothExpand = Mathf.Lerp(lastExpand, expand, timeStacker);
            for (int i = 0;i < splitLines.Count; i++)
            {
                splitLines[i].scaleY = Mathf.Lerp(0f, rewardInstanceHeight, smoothExpand);
                splitLines[i].SetPosition(linePoses[i]);
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            questRendererManager.Destroy();
            foreach (var instance in rewardInstances)
                instance.Destroy();
            rewardInstances.Clear();

            foreach(var sprite in splitLines)
                sprite.RemoveFromContainer();
            splitLines.Clear();
        }


        public abstract class RewardInstance
        {
            public RewardBanner banner;
            
            public float width = 250;
            public Vector2 pos;

            public FLabel title;

            public RewardInstance(RewardBanner banner)
            {
                this.banner = banner;
            }

            public virtual void InitSprites()
            {
                title = new FLabel(Custom.GetDisplayFont(), "") { anchorX = 0.5f, anchorY = 1f};
                banner.notificationManager.Container.AddChild(title);
                
            }

            public virtual void Update()
            {
            }

            public virtual void GrafUpdate(float timeStacker)
            {
                float smoothContentExpand = Mathf.Lerp(banner.lastContentExpand, banner.contentExpand, timeStacker);
                title.alpha = smoothContentExpand;

                title.SetPosition(new Vector2(pos.x, pos.y + banner.rewardInstanceHeight / 2f));
            }

            public virtual void Destroy()
            {
                title.RemoveFromContainer();
            }
        }

        public class MissionReward : RewardInstance
        {
            bool unique;
            string missionName;

            FLabel missionLabel;
            QuestLeaser quest;

            public MissionReward(MissionID missionID, RewardBanner banner) : base(banner)
            {
                MissionRegister.TryGetMission(missionID, out var mission);
                missionName = mission.MissionName;
                unique = mission.BindSlug != null;
                width = LabelTest.GetWidth(missionName, true) + 20f;

                quest = banner.questRendererManager.AddQuestToRender(mission);
                width = quest.rect.x;
            }

            public override void InitSprites()
            {
                base.InitSprites();
                title.text = unique ? BuffResourceString.Get("Notification_MissionReward_Unique")  : BuffResourceString.Get("Notification_MissionReward");

                //missionLabel = new FLabel(Custom.GetDisplayFont(), missionName) { anchorX = 0.5f, anchorY = 0.5f };
                //banner.notificationManager.Container.AddChild(missionLabel);
            }

            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                float smoothContentExpand = Mathf.Lerp(banner.lastContentExpand, banner.contentExpand, timeStacker);

                quest.smoothAlpha = smoothContentExpand;
                quest.smoothCenterPos = pos;
            }

            public override void Destroy()
            {
                base.Destroy();
                //missionLabel.RemoveFromContainer();
            }
        }
    }
}
