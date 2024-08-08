using Menu;
using RandomBuff.Core.Progression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Progression.Quest;
using UnityEngine;

namespace RandomBuff.Render.UI.Notification
{
    internal class NotificationManager : Page
    {
        public FContainer ownerContainer;

        public List<NotificationBanner> banners = new();
        int menulastFocusPage;

        public NotificationManager(Menu.Menu menu, FContainer ownerContainer, int index) : base(menu, null, "Notification", index)
        {
            this.ownerContainer = ownerContainer;
        }

        public override void Update()
        {
            base.Update();
            for(int i = banners.Count - 1;i >= 0; i--)
                banners[i].Update();

            //if(Input.GetKey(KeyCode.K) && banners.Count == 0)
            //{
            //    //NewRewardNotification(QuestUnlockedType.Mission, "DodgeTheRock");
               
            //    NewRewardNotification(QuestUnlockedType.Mission, "Druid");
            //    NewRewardNotification(QuestUnlockedType.Cosmetic, "HoloScore");
            //    NewRewardNotification(QuestUnlockedType.Cosmetic, "FireWork");
            //    NewRewardNotification(QuestUnlockedType.Mission, "MidnightSnack");
            //    NewRewardNotification(QuestUnlockedType.Cosmetic, "Crown");
            //}
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            for (int i = banners.Count - 1; i >= 0; i--)
                banners[i].GrafUpdate(timeStacker);
        }

        public override void RemoveSprites()
        {
            for (int i = banners.Count - 1; i >= 0; i--)
                banners[i].Destroy();
        }

        public void RecoverFocus()
        {
            menu.currentPage = menulastFocusPage;
        }
        
        public void TakeFocus()
        {
            menulastFocusPage = menu.currentPage;
            menu.currentPage = index;
        }

        public void NewNotification(NotificationType notificationType)
        {
            if (banners.Count > 0)
                return;

            if(notificationType == NotificationType.Reward)
            {
                banners.Add(new RewardBanner(this));
            }
            else if(notificationType == NotificationType.Info)
            {
                banners.Add(new InfoBanner(this));
            }
            TakeFocus();
        }

        public void NewRewardNotification(QuestUnlockedType questUnlockedType, string itemName)
        {
            NewNotification(NotificationType.Reward);
            (banners.First() as RewardBanner).AppendReward(questUnlockedType,itemName);
        }

        public void NewInfoNotification(string title, string info)
        {
            NewNotification(NotificationType.Info);
            (banners.First() as InfoBanner).SetInfo(title, info);
        }

        public enum NotificationType
        {
            Reward,
            Info
        }
    }
}
