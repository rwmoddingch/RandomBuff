using Menu;
using Menu.Remix.MixedUI;
using RandomBuff.Render.CardRender;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.BuffMenu
{
    internal class BuffExtraInfoPage : Page
    {
        static float gap = 10f;
        static float bigGap = 20f;
        static float infoEntryHeight = 40f;
        static float lineScale = 4f;

        float maxMainInfoWidth;

        //静态元素
        FSprite black;
        FSprite line;
        FSprite gradient;
        FSprite split;

        FLabel mainInfo;

        //动态元素
        FSprite[] icons = new FSprite[0];
        FLabel[] infos = new FLabel[0];
  

        bool show;
        float initY;
        
        float height;
        float infoEntryWidth;
        int menulastFocusPage;

        List<string> requestedIcons = new List<string>();
        List<string> requestedInfos = new List<string>();
        List<Color> requestedIconColors = new List<Color>();
        string requestedMainInfo = string.Empty;


        SimpleButton closeButton;
        FNodeWrapper nodeWrapper;

        public bool Show => show;

        public BuffExtraInfoPage(Menu.Menu menu, MenuObject owner, string name, int index) : base(menu, owner, name, index)
        {
            Vector2 screenSize = Custom.rainWorld.options.ScreenSize;
            maxMainInfoWidth = screenSize.x * 0.618f - bigGap * 2f;

            lastPos = pos = Vector2.down * 1000f;
  
            subObjects.Add(nodeWrapper = new FNodeWrapper(menu, this));
            //显示元素
            nodeWrapper.WrapNode(black = new FSprite("pixel") { scaleX = screenSize.x, anchorX = 0f, anchorY = 0f, color = Color.black }, Vector2.zero);
            nodeWrapper.WrapNode(line = new FSprite("pixel") { scaleX = screenSize.x, scaleY = lineScale, anchorX = 0f, anchorY = 0f, color = Color.white }, Vector2.zero);
            nodeWrapper.WrapNode(split = new FSprite("pixel") { scaleX = 1f, anchorX = 0.5f, anchorY = 0f, color = Color.white }, new Vector2(screenSize.x * (1f - 0.618f), bigGap));
            nodeWrapper.WrapNode(gradient = new FSprite("LinearGradient200") { scaleX = screenSize.x, scaleY = 2f, anchorX = 0f, anchorY = 0f, color = Color.black }, Vector2.zero);
            nodeWrapper.WrapNode(mainInfo = new FLabel(Custom.GetDisplayFont(), "") { anchorX = 0f, anchorY = 1f }, new Vector2(screenSize.x * (1f - 0.618f) + bigGap, 0f));

            //menuitem
            subObjects.Add(closeButton = new SimpleButton(menu, this, "X", "BuffExtraInfoPage_CloseButton", new Vector2(screenSize.x * 0.9f, 0f), new Vector2(40f, 40f)));
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "BuffExtraInfoPage_CloseButton")
                SetShow(false);
        }

        TickAnimCmpnt showAnim;
        public void SetShow(bool show)
        {
            if (show == this.show)
                return;

            this.show = show;
            if (show)
            {
                TakeFocus();
                RecaculateElements();
            }
            else
                RecoverFocus();

            initY = pos.y;

            if (showAnim != null)
                showAnim.Destroy();
            showAnim = AnimMachine.GetTickAnimCmpnt(0, 40, autoDestroy: true).BindActions(
                OnAnimGrafUpdate: (t,f) =>
                {
                    if (this.show)
                        pos.y = Mathf.Lerp(initY, 0f, t.Get());
                    else
                        pos.y = Mathf.Lerp(initY, -1000f, t.Get());
                },
                OnAnimFinished: (t) =>
                {
                    showAnim = null;
                }).BindModifier(Helper.EaseInOutCubic);
        }

        public override void Update()
        {
            base.Update();
            closeButton.roundedRect.fillAlpha = 1f;
        }

        void RecaculateElements()
        {
            for(int i = 0;i < infos.Length;i++)
            {
                nodeWrapper.UnwarpNode(infos[i]);
                if (icons[i] != null)
                    nodeWrapper.UnwarpNode(icons[i]);
            }

            Vector2 screenSize = Custom.rainWorld.options.ScreenSize;
            float splitX = screenSize.x * (1f - 0.618f);

            float infoHeight = requestedInfos.Count * (infoEntryHeight + gap);
            float infoWidth = 0f;
            float maxHeight = 0f;

            foreach(var entry in requestedInfos)
            {
                float width = LabelTest.GetWidth(entry, true);
                if(width > infoWidth)
                    infoWidth = width;
            }
            if(infoWidth > 0f)
            {
                //           图标的尺寸
                infoWidth += infoEntryHeight + gap + bigGap;
            }
            infoEntryWidth = infoWidth;

            if (!string.IsNullOrEmpty(requestedMainInfo))
            {
                requestedMainInfo = LabelTest.WrapText(requestedMainInfo, true, maxMainInfoWidth);
            }
            string[] splited = requestedMainInfo.Split('\n');
            maxHeight = Mathf.Max(infoHeight, splited.Length * infoEntryHeight);
            height = maxHeight + bigGap * 2;

            black.scaleY = height;
            nodeWrapper.SetPosition(line, new Vector2(0, height));
            nodeWrapper.SetPosition(split, new Vector2(splitX, bigGap));
            split.scaleY = maxHeight;

            icons = new FSprite[requestedInfos.Count];
            infos = new FLabel[requestedInfos.Count];

            float y;
            for (int i = 0; i < requestedInfos.Count; i++)
            {
                y = height - bigGap - infoEntryHeight * (i + 0.5f);
                infos[i] = new FLabel(Custom.GetDisplayFont(), requestedInfos[i]) { anchorX = 0f, anchorY = 0.5f};
                nodeWrapper.WrapNode(infos[i], new Vector2(splitX - infoWidth + infoEntryHeight + gap, y));
                if (!string.IsNullOrEmpty(requestedIcons[i]))
                {
                    icons[i] = new FSprite(requestedIcons[i]) { anchorX = 0f, anchorY = 0.5f, color = requestedIconColors[i] };
                    nodeWrapper.WrapNode(icons[i], new Vector2(splitX - infoWidth, y));
                }
            }

            mainInfo.text = requestedMainInfo;
            nodeWrapper.SetPosition(mainInfo, new Vector2(splitX + bigGap, height - bigGap));
            nodeWrapper.SetPosition(gradient, new Vector2(0f, height + lineScale));
            closeButton.pos = new Vector2(screenSize.x * 0.9f, height - 20f + lineScale / 2f);

            requestedMainInfo = string.Empty;
            requestedIcons.Clear();
            requestedInfos.Clear();

            for(int i = 0;i < 2; i++)
            {
                nodeWrapper.Update();
                nodeWrapper.GrafUpdate(0f);
            }
        }

        void RecoverFocus()
        {
            menu.currentPage = menulastFocusPage;
        }

        void TakeFocus()
        {
            menulastFocusPage = menu.currentPage;
            menu.currentPage = index;
        }

        public BuffExtraInfoPage AppendInfoEntry(string info, string element = "", Color? color = null)
        {
            if (string.IsNullOrEmpty(element))
                element = "buffassets/illustrations/uilozenge";
            requestedIcons.Add(element);
            requestedInfos.Add(info);
            requestedIconColors.Add(color ?? Color.white);
            return this;
        }

        public BuffExtraInfoPage SetMainInfo(string info)
        {
            requestedMainInfo = info;
            return this;
        }

        public BuffExtraInfoPage AppendGachaInfo(string info, bool positive)
        {
            return AppendInfoEntry(info, "buffassets/illustrations/cardpickicon", positive ? CardBasicAssets.PositiveColor : CardBasicAssets.NegativeColor);
        }


        public void EscLogic()
        {
            SetShow(false);
        }
    }
}
