using Menu.Remix.MixedUI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI.ExceptionTracker
{
    internal class TrackerInfoTextPanel : IDockObject
    {
        static Color infoColor = new Color(0.7f, 0.7f, 0.7f);
        static float topLeftRightEdge = 15f;
        static float bottomEdge = 50f;//为了放按钮

        public BuffExceptionSideDock Dock { get; set; }

        Vector2 topLeftPos;

        FLabel simpleInfo;
        FLabel fullInfo;
        FSprite black1;
        FSprite black2;

        bool scrollInfoDirty;
        //简略信息相关
        float simpleInfoHeight;

        //滚动相关；
        float scrollVel;
        float lastScrollY;
        float scrollY;
        float maxScrollY;
        float scrollStartY;

        Vector2 size;
        Vector2 pos;
        Vector2 lastPos;

        string _realSimpleInfoText;
        string _simpleInfoText;
        public string SimpleInfoText
        {
            get => _simpleInfoText;
            set
            {
                if (value == _simpleInfoText)
                    return;
                _simpleInfoText = value;
                _realSimpleInfoText = value.WrapText(true, size.x - topLeftRightEdge * 3f - 60f);//60为了让出右侧按钮
                scrollInfoDirty = true;
            }
        }

        string _realFullInfoText;
        string _fullInfoText;
        public string FullInfoText
        {
            get => _fullInfoText;
            set
            {
                if (value == _fullInfoText)
                    return;
                _fullInfoText = value;
                _realFullInfoText = value.WrapText(false, size.x - topLeftRightEdge * 3f);
                scrollInfoDirty = true;
            }
        }

        public bool MouseInside { get; private set; }

        public TrackerInfoTextPanel(Vector2 topLeft, Vector2 size, string text1, string text2)
        {
            topLeftPos = topLeft;
            this.size = size;

            SimpleInfoText = text1;
            FullInfoText = text2;
        }

        public void InitSprites()
        {
            fullInfo = new FLabel(Custom.GetFont(), "") { anchorX = 0f, anchorY = 1f, scale = 1.01f };
            Dock.Container.AddChild(fullInfo);

            var blur = Custom.rainWorld.Shaders["UIBlur"];
            black1 = new FSprite("pixel", true) { color = Color.black, anchorX = 0f, anchorY = 1f, scale = 1.01f,shader = blur };
            black2 = new FSprite("pixel", true) { color = Color.black, anchorX = 0f, anchorY = 1f, scaleX = size.x, scaleY = bottomEdge, shader = blur };
            Dock.Container.AddChild(black1);
            Dock.Container.AddChild(black2);

            simpleInfo = new FLabel(Custom.GetDisplayFont(), "") { anchorX = 0f, anchorY = 1f };
            Dock.Container.AddChild(simpleInfo);

            RecaculateScorllInfoAndSprite();
        }

        public void Update()
        {
            lastPos = pos;
            pos = Dock.BottomLeftPos + topLeftPos;

            if (scrollInfoDirty)
                RecaculateScorllInfoAndSprite();

            Vector2 delta = Dock.ScreenMousePosition - (pos - new Vector2(0f, size.y));
            if (delta.x > 0f && delta.x < size.x && delta.y > 0f && delta.y < size.y)
                MouseInside = true;
            else
                MouseInside = false;


            bool scrollVelToZero = true;
            if (MouseInside)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll < 0)
                {
                    scrollVel = Mathf.Lerp(scrollVel, 30f, 0.25f);
                    scrollVelToZero = false;
                }
                else if (scroll > 0)
                {
                    scrollVel = Mathf.Lerp(scrollVel, -30f, 0.25f);
                    scrollVelToZero = false;
                }
            }
            if (scrollVel != 0f && scrollVelToZero)
            {
                scrollVel = Mathf.Lerp(scrollVel, 0f, 0.25f);
                if (Mathf.Approximately(scrollVel, 0f))
                    scrollVel = 0f;
            }

            lastScrollY = scrollY;
            scrollY = Mathf.Clamp(scrollY + scrollVel, 0f, maxScrollY);
        }

        public void DrawSprites(float timeStacker)
        {
            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);
            float smoothScroll = Mathf.Lerp(lastScrollY, scrollY, timeStacker);

            simpleInfo.SetPosition(smoothPos + new Vector2(topLeftRightEdge, -topLeftRightEdge));
            fullInfo.SetPosition(smoothPos + new Vector2(topLeftRightEdge, -scrollStartY + smoothScroll));
            black1.SetPosition(smoothPos + new Vector2(1f, 0f));
            black2.SetPosition(smoothPos + new Vector2(1f, -size.y + bottomEdge - 1));
        }

        public void RemoveSprites()
        {
            simpleInfo.RemoveFromContainer();
            fullInfo.RemoveFromContainer();
        }

        public void Signal(string message)
        {
            if(message == "RefreshPanelText")
            {
                var tracker = BuffExceptionTracker.allTrackers[Dock.CurrentBrowseExceptionIndex];
                SimpleInfoText = tracker.streamlineInfo;
                FullInfoText = tracker.origMessage + tracker.origMessage + tracker.origMessage + tracker.origMessage;
            }
        }



        public void RecaculateScorllInfoAndSprite()
        {
            if (!scrollInfoDirty)
                return;

            scrollInfoDirty = false;

            lastScrollY = scrollY = 0f;
            scrollVel = 0f;

            simpleInfoHeight = Helper.GetLabelHeight(_realSimpleInfoText, true);
            float fullInfoHeight = Helper.GetLabelHeight(_realFullInfoText, false);
            float windowsHeight = size.y - topLeftRightEdge - bottomEdge - simpleInfoHeight - topLeftRightEdge;

            scrollStartY = topLeftRightEdge + simpleInfoHeight + topLeftRightEdge;

            maxScrollY = Mathf.Max(0f, fullInfoHeight - windowsHeight);

            black1.scaleX = size.x;
            black1.scaleY = simpleInfoHeight + topLeftRightEdge * 2f;

            simpleInfo.text = _realSimpleInfoText;
            fullInfo.text = _realFullInfoText;

            BuffPlugin.Log($"window : {windowsHeight}, fullInfo : {fullInfoHeight}, simple : {simpleInfoHeight}, scrollStartY : {scrollStartY}, maxScrollY : {maxScrollY}");
        }
    }
}
