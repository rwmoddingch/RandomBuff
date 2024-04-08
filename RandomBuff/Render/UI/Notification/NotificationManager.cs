using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public void NewNotification()
        {

        }
    }
}
