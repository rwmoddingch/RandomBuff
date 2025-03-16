using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI.BuffPack
{
    internal class PackMenuDialog : Dialog
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

        SimpleButton hideButton;

        TickAnimCmpnt showAnim;

        bool _showPack;
        public bool ShowPack => _showPack;

        public Action<List<BuffPluginInfo>> OnTogglePackCallBack;
        public Action ShutDownCallBack;

        float animF;
        float lastAnimF;
        bool show = true;
        bool hideAllOnClose;

        public PackMenuDialog(string description, ProcessManager manager, Vector2 showPos, bool hideAllOnClose, List<BuffPluginInfo> enabledPlugins = null) : base(manager)
        {
            this.showPos = showPos;
            this.hideAllOnClose = hideAllOnClose;
            buttonPackShowPos = showPos + new Vector2(0f, ScrollBoxShowSize.y + 10f);

            menuTabWrapper = new MenuTabWrapper(this, pages[0]);
            pages[0].subObjects.Add(menuTabWrapper);
            float sizeY = 0f;

            if (enabledPlugins == null)
                enabledPlugins = new List<BuffPluginInfo>();

            packButtons = new List<PackButton>();
            foreach (var pluginInfo in BuffConfigManager.PluginInfos.Values.Where(i => i.Enabled))
            {
                var packButton = new PackButton(Vector2.zero, packButtonSize, pluginInfo, false, true)
                {
                    Enabled = enabledPlugins.Contains(pluginInfo),
                    ToggleCallBack = OnPackButtonClick
                };
                packButtons.Add(packButton);
                new UIelementWrapper(menuTabWrapper, packButton);
                sizeY += packButtonSize.y + 10f;
            }

            sizeY = Mathf.Max(sizeY, 400f);

            new UIelementWrapper(menuTabWrapper, packButtonScrollBox = new OpScrollBox(hidePos, ScrollBoxHideSize, sizeY, hasSlideBar: false)
            {
                fillAlpha = 0.8f
            });

            float anchorY = sizeY;
            foreach (var button in packButtons)
            {
                anchorY -= button.size.y;
                anchorY -= 5f;
                button.SetPos(new Vector2(10f, anchorY));
                packButtonScrollBox.AddItems(button);
                anchorY -= 5f;
            }

            hideButton = new SimpleButton(this, pages[0], BuffResourceString.Get("PackMenu_Hide"), "Hide_Pack", showPos, ScrollBoxHideSize);
            pages[0].subObjects.Add(hideButton);
            //packButtonScrollBox.Hide();

            //foreach (var button in packButtons)
            //    button.Hide();

            //for (int i = 0; i < 4; i++)
            //{
            //    hideButton.roundedRect.sprites[packButtonScrollBox._rectBack.FillSideSprite(i)].shader = manager.rainWorld.Shaders["UIBlur"];
            //    hideButton.roundedRect.sprites[packButtonScrollBox._rectBack.FillCornerSprite(i)].shader = manager.rainWorld.Shaders["UIBlur"];
            //    hideButton.roundedRect.sprites[8].shader = manager.rainWorld.Shaders["UIBlur"];
            //}

            packButtonScrollBox.pos = showPos;
            packButtonScrollBox.size = Vector2.Lerp(ScrollBoxHideSize, ScrollBoxShowSize, 0f);
            hideButton.lastPos = hideButton.pos = Vector2.Lerp(showPos, buttonPackShowPos, 0f);

            packButtonScrollBox.Update();
            hideButton.Update();
        }

        public override void Update()
        {
            base.Update();

            lastAnimF = animF;
            if (show && animF != 1)
            {
                animF = Mathf.Clamp01(animF += 1 / 20f);
            }
            else if (!show)
            {
                animF = Mathf.Clamp01(animF -= 1 / 20f);
                if (animF == 0f)
                {
                    manager.StopSideProcess(this);
                    ShutDownCallBack?.Invoke();
                }
            }
            if (lastAnimF != 1f)
            {
                float f = Helper.EaseInOutCubic(animF);
                packButtonScrollBox.pos = showPos;
                hideButton.pos = Vector2.Lerp(showPos, buttonPackShowPos, f);
                hideButton.inactive = true;

                if (hideAllOnClose)
                {
                    container.alpha = animF;
                }
            }
            else
                hideButton.inactive = false;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            float f = Helper.EaseInOutCubic(Mathf.Lerp(lastAnimF, animF, timeStacker));
            packButtonScrollBox.size = Vector2.Lerp(ScrollBoxHideSize, ScrollBoxShowSize, f);
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if(message == "Hide_Pack")
            {
                show = false;
            }
        }

        public void OnPackButtonClick(PackButton self)
        {
            List<BuffPluginInfo> enabledPlugins = new List<BuffPluginInfo>();

            foreach (PackButton button in packButtons)
            {
                if (button.Enabled)
                    enabledPlugins.Add(button.pluginInfo);
            }

            OnTogglePackCallBack?.Invoke(enabledPlugins);
        }
    
        void Show()
        {
            _showPack = true;

            int lowBound = 0;
            if (showAnim != null)
            {
                lowBound = showAnim.current;
                showAnim.Destroy();
            }

            showAnim = AnimMachine.GetTickAnimCmpnt(lowBound, 20, autoDestroy: true).BindActions(
            OnAnimStart: (t) =>
            {
                packButtonScrollBox.pos = showPos;
                packButtonScrollBox.lastScreenPos = showPos + packButtonScrollBox.Owner.pos;
                packButtonScrollBox.size = ScrollBoxHideSize;
                packButtonScrollBox.Update();
                packButtonScrollBox.GrafUpdate(1f);
                packButtonScrollBox.Show();
                foreach (var button in packButtons)
                    button.Show();
            },
            OnAnimGrafUpdate: (t, f) =>
            {
                packButtonScrollBox.size = Vector2.Lerp(ScrollBoxHideSize, ScrollBoxShowSize, t.Get());
                hideButton.pos = Vector2.Lerp(showPos, buttonPackShowPos, t.Get());
            },
            OnAnimFinished: (t) =>
            {
                showAnim = null;
                packButtonScrollBox.size = ScrollBoxShowSize;
                hideButton.pos = buttonPackShowPos;

                packButtonScrollBox.Update();
                packButtonScrollBox.GrafUpdate(1f);
            }).BindModifier(Helper.EaseInOutCubic);
        }

        public override void ShutDownProcess()
        {
            menuTabWrapper.RemoveSprites();
            base.ShutDownProcess();
        }
    }
}
