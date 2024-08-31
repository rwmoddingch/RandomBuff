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

        MenuTabWrapper menuTabWrapper;

        OpScrollBox packButtonScrollBox;
        List<PackButton> packButtons;

        SimpleButton showHideButton;


        TickAnimCmpnt showAnim;

        bool _showPack;
        public bool ShowPack => _showPack;

        public Action<List<BuffPluginInfo>> OnTogglePackCallBack;

        public PackMenu(ProcessManager manager, Vector2 showPos, List<BuffPluginInfo> enabledPlugins = null) : base(manager, BuffEnums.ProcessID.BuffPackMenu)
        {
            this.showPos = showPos;
            this.hidePos = new Vector2(-1000f, showPos.y);
            buttonPackShowPos = showPos + new Vector2(0f, ScrollBoxShowSize.y + 10f);

            pages.Add(new Page(this, null, "PackPage", 0));

            menuTabWrapper = new MenuTabWrapper(this, pages[0]);
            pages[0].subObjects.Add(menuTabWrapper);
            float sizeY = 0f;

            if(enabledPlugins == null)
                enabledPlugins = new List<BuffPluginInfo>();

            packButtons = new List<PackButton>();
            foreach (var pluginInfo in BuffConfigManager.PluginInfos.Values.Where(i => i.Enabled))
            {
                var packButton = new PackButton(Vector2.zero, packButtonSize, pluginInfo, false, false)
                {
                    Enabled = enabledPlugins.Contains(pluginInfo),
                    ToggleCallBack = OnPackButtonClick
                };
                packButtons.Add(packButton);
                new UIelementWrapper(menuTabWrapper, packButton);
                sizeY += packButtonSize.y + 10f;
            }

            sizeY = Mathf.Max(sizeY, 400f);

            new UIelementWrapper(menuTabWrapper, packButtonScrollBox = new OpScrollBox(hidePos, ScrollBoxHideSize, sizeY, hasSlideBar: false));

            float anchorY = sizeY;
            foreach (var button in packButtons)
            {
                anchorY -= button.size.y;
                anchorY -= 5f;
                button.SetPos(new Vector2(10f, anchorY));
                packButtonScrollBox.AddItems(button);
                anchorY -= 5f;
            }

            showHideButton = new SimpleButton(this, pages[0], "Pack", "Show_Pack", showPos, ScrollBoxHideSize);
            pages[0].subObjects.Add(showHideButton);
        }

        public void OnPackButtonClick()
        {
            List<BuffPluginInfo> enabledPlugins = new List<BuffPluginInfo>();

            foreach(PackButton button in packButtons)
            {
                if (button.Enabled)
                    enabledPlugins.Add(button.pluginInfo);
            }

            OnTogglePackCallBack?.Invoke(enabledPlugins);
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if(message == "Show_Pack")
            {
                _showPack = true;

                int lowBound = 0;
                if (showAnim != null)
                {
                    lowBound = showAnim.current;
                    showAnim.Destroy();
                }
                
                showAnim = AnimMachine.GetTickAnimCmpnt(lowBound, 20, autoDestroy: true).BindActions(
                OnAnimStart:(t) =>
                {
                    packButtonScrollBox.pos = showPos;
                    packButtonScrollBox.lastScreenPos = showPos + packButtonScrollBox.Owner.pos;
                    packButtonScrollBox.size = ScrollBoxHideSize;
                    packButtonScrollBox.Update();
                    packButtonScrollBox.GrafUpdate(1f);
                },
                OnAnimGrafUpdate: (t, f) =>
                {
                    packButtonScrollBox.size = Vector2.Lerp(ScrollBoxHideSize, ScrollBoxShowSize, t.Get());
                    showHideButton.pos = Vector2.Lerp(showPos, buttonPackShowPos, t.Get());
                }, 
                OnAnimFinished:(t) =>
                {
                    showAnim = null;
                    packButtonScrollBox.size = ScrollBoxShowSize;
                    showHideButton.signalText = "Hide_Pack";
                    showHideButton.menuLabel.text = "Hide";
                    showHideButton.pos = buttonPackShowPos;

                    packButtonScrollBox.Update();
                    packButtonScrollBox.GrafUpdate(1f);
                }).BindModifier(Helper.EaseInOutCubic);
            }
            else if(message == "Hide_Pack")
            {
                int lowBound = 0;
                if (showAnim != null)
                {
                    lowBound = showAnim.current;
                    showAnim.Destroy();
                }
                showAnim = AnimMachine.GetTickAnimCmpnt(lowBound, 20, autoDestroy: true).BindActions(OnAnimGrafUpdate: (t, f) =>
                {
                    packButtonScrollBox.size = Vector2.Lerp(ScrollBoxShowSize, ScrollBoxHideSize, t.Get());
                    showHideButton.pos = Vector2.Lerp(buttonPackShowPos, showPos, t.Get());
                }, OnAnimFinished: (t) =>
                {
                    packButtonScrollBox.pos = hidePos;
                    packButtonScrollBox.lastScreenPos = hidePos + packButtonScrollBox.Owner.pos;
                    showAnim = null;
                    packButtonScrollBox.size = ScrollBoxHideSize;
                    showHideButton.signalText = "Show_Pack";
                    showHideButton.menuLabel.text = "Show";
                    showHideButton.pos = showPos;
                    _showPack = false;

                    packButtonScrollBox.Update();
                    packButtonScrollBox.GrafUpdate(1f);

                }).BindModifier(Helper.EaseInOutCubic);
            }
        }


        public override void ShutDownProcess()
        {
            menuTabWrapper.RemoveSprites();
            base.ShutDownProcess();
        }
    }
}
