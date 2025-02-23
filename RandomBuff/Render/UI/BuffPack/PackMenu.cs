using Menu.Remix.MixedUI;
using Menu;
using Menu.Remix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RandomBuff.Core.SaveData;
using RandomBuff.Core.Buff;

namespace RandomBuff.Render.UI.BuffPack
{
    internal class PackMenu : Menu.Menu
    {
        static Vector2 ScrollBoxShowSize = new Vector2(240f, 400f);
        static Vector2 ScrollBoxHideSize = new Vector2(110f, 30f);
        static Vector2 packButtonSize = new Vector2(220f, 60f);

        Vector2 showPos;
        Vector2 hidePos;
        Vector2 buttonPackShowPos;

        SimpleButton showHideButton;

        public Action<List<BuffPluginInfo>> OnTogglePackCallBack;
        public Func<List<BuffPluginInfo>> GetCurrentPlugins;

        public PackMenu(ProcessManager manager, Vector2 showPos, Func<List<BuffPluginInfo>> GetCurrentPlugins, FContainer ownerContainer = null) : base(manager, BuffEnums.ProcessID.BuffPackMenu)
        {
            this.GetCurrentPlugins = GetCurrentPlugins;

            if(ownerContainer != null)
            {
                container.RemoveFromContainer();
                ownerContainer.AddChild(container);
            }

            this.showPos = showPos;
            this.hidePos = new Vector2(-1000f, showPos.y);
            buttonPackShowPos = showPos + new Vector2(0f, ScrollBoxShowSize.y + 10f);

            pages.Add(new Page(this, null, "PackPage", 0));

          
            float sizeY = 0f;

            sizeY = Mathf.Max(sizeY, 400f);


            showHideButton = new SimpleButton(this, pages[0], BuffResourceString.Get("PackMenu_Show"), "Show_Pack", showPos, ScrollBoxHideSize);
            pages[0].subObjects.Add(showHideButton);
        }


        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if(message == "Show_Pack")
            {
                manager.ShowDialog(new PackMenuDialog("", manager, showPos, false, GetCurrentPlugins.Invoke())
                {
                    ShutDownCallBack = () => container.isVisible = true,
                    OnTogglePackCallBack = this.OnTogglePackCallBack
                });
                container.isVisible = false;
            }
        }


        public override void ShutDownProcess()
        {
            base.ShutDownProcess();
        }
    }
}
