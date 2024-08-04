using RWCustom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RandomBuff.Render.UI.ExceptionTracker
{
    internal class BuffExceptionTracker
    {
        public static BuffExceptionTracker Singleton { get; private set; }

        public static Dictionary<string, ExceptionTracker.TrackedException> exceptions = new();
        public static List<ExceptionTracker.TrackedException> allTrackers = new();

        public static bool EnableTracker = true;

        public FStage stage;
        public FContainer container;

        BuffExceptionSideDock dock;

        public BuffExceptionTracker()
        {
            Singleton = this;
            stage = new FStage("RandomBuff_ExceptionTracker2");
            Futile.AddStage(stage);
            stage.MoveToFront();

            container = new FContainer();
            stage.AddChild(container);

            dock = new BuffExceptionSideDock();
        }

        public void RawUpdate()
        {
            dock.RawUpdate(Time.deltaTime);
        }

        public static void TrackExceptionNew(string stackTrace, string streamlineInfo)
        {
            if (!EnableTracker)
                return;

            string key = stackTrace + streamlineInfo;
            if (exceptions.ContainsKey(key))
            {
                exceptions[key].count++;
            }
            else
            {
                var tracker = new ExceptionTracker.TrackedException(streamlineInfo, stackTrace);
                exceptions.Add(key, tracker);
                allTrackers.Add(tracker);
            }

            if (Singleton != null)
                Singleton.dock.Signal("NewTracker");
        }
    }

    internal class BuffExceptionSideDock
    {
        static int framePerSecond = 40;
        static float panelWidth = 500f;

        float timeStacker;

        Helper.InputButtonTracker mouseLeftButtonTracker;
        List<IDockObject> dockObjects = new List<IDockObject>();

        TickAnimCmpnt showAnim;
        int showAnimCounter;

        public FContainer Container => BuffExceptionTracker.Singleton.container;
        public readonly FContainer alphaContainer;

        public Vector2 ScreenMousePosition { get; private set; }
        public Vector2 ScreenSize { get; private set; }

        public Vector2 TopLeftPos { get; private set; }
        public Vector2 BottomLeftPos { get; private set; }

        public bool MouseClick { get; private set; }
        public bool MouseDown { get; private set; }

        public bool Show { get; private set; }

        public float ShowFactor { get; private set; }
        public float LastShowFactor { get; private set; }

        public int CurrentBrowseExceptionIndex { get; private set; }

        bool _enable;
        public bool Enable 
        {
            get => _enable; 
            set
            {
                if(_enable != value)
                {
                    _enable = value;
                    Container.isVisible = value;
                }
            }
        }

        public BuffExceptionSideDock()
        {
            alphaContainer = new FContainer() { alpha = 0.3f};
            Container.AddChild(alphaContainer);

            mouseLeftButtonTracker = new Helper.InputButtonTracker(() => Input.GetMouseButton(0), false);
            ScreenSize = Custom.rainWorld.options.ScreenSize;

            BottomLeftPos = new Vector2(ScreenSize.x + 2, 0f);
            TopLeftPos = new Vector2(ScreenSize.x + 2, ScreenSize.y);

            Show = false;
            Container.isVisible = false;

            InitElements();

            for(int i= 0;i < 2; i++)
            {
                Update(true);
                GrafUpdate(1f, true);
            }
        }

        public void InitElements()
        {
            AddObject(new TrackerBlackPanel(new Vector2(panelWidth + 2f, ScreenSize.y), BottomLeftPos));
            AddObject(new TrackerInfoTextPanel(new Vector2(0f, ScreenSize.y), new Vector2(panelWidth, ScreenSize.y), "", ""));
            AddObject(new TrackerTextButton("!", "OpenCloseDock", new Vector2(30f, 30f), TopLeftPos - new Vector2(40f, 40f), true) { fixedPosition = true });
            AddObject(new TrackerTextButton("Prev", "SelectLastException", new Vector2(60f, 30f), new Vector2(10f, 10f))
            {
                siganlCallBack = (s, b) =>
                {
                    if (b.Dock.CurrentBrowseExceptionIndex == 0)
                        b.active = false;
                    else
                        b.active = true;
                }
            });
            AddObject(new TrackerTextButton("Next", "SelectNextException", new Vector2(60f, 30f), new Vector2(panelWidth - 60f - 10f, 10f))
            {
                siganlCallBack = (s, b) =>
                {
                    if (b.Dock.CurrentBrowseExceptionIndex == BuffExceptionTracker.allTrackers.Count - 1)
                        b.active = false;
                    else
                        b.active = true;
                }
            });
            alphaContainer.MoveToFront();
        }

        public void Update(bool forceUpdate = false)
        {
            if (!Enable && !forceUpdate)
                return;

            mouseLeftButtonTracker.Update(out bool mouseClick, out _);
            MouseClick = mouseClick;
            MouseDown = Input.GetMouseButton(0);

            ScreenMousePosition = Futile.mousePosition;

            LastShowFactor = ShowFactor;
            if (showAnimCounter > 0)
            {
                showAnimCounter--;

                float t = showAnimCounter / 20f;
                if (Show)
                    t = 1f - t;
                t = Helper.EaseInOutCubic(t);

                TopLeftPos = Vector2.Lerp(ScreenSize + new Vector2(2f, 0f), ScreenSize - new Vector2(panelWidth, 0f), t);
                BottomLeftPos = Vector2.Lerp(new Vector2(ScreenSize.x + 2, 0f), new Vector2(ScreenSize.x - panelWidth, 0f), t);

                ShowFactor = t;
                alphaContainer.alpha = Mathf.Lerp(0.3f, 1f, ShowFactor);
            }


            for (int i = dockObjects.Count - 1; i >= 0; i--)
                dockObjects[i].Update();

            //BuffPlugin.Log($"Dock update {dockObjects.Count}, {Container._isOnStage}, {BuffExceptionTracker.Singleton.stage._isOnStage}");

        }

        public void Signal(string message)
        {
            if (!BuffExceptionTracker.EnableTracker)
                return;

            if (message == "OpenCloseDock")
            {
                SetShow(!Show);
                if (Show)
                {
                    Signal("RefreshPanelText");
                    Signal("RefreshPrevNextButton");
                }
            }
            else if(message == "SelectLastException")
            {
                UpdateIndex(-1);
            }
            else if(message == "SelectNextException")
            {
                UpdateIndex(1);
            }
            else if(message == "NewTracker")
            {
                Enable = true;
                Signal("RefreshPrevNextButton");
            }

            foreach (var obj in dockObjects)
                obj.Signal(message);
        }

        void UpdateIndex(int add)
        {
            int nextIndex = Mathf.Clamp(CurrentBrowseExceptionIndex + add, 0, BuffExceptionTracker.allTrackers.Count - 1);
            if (nextIndex != CurrentBrowseExceptionIndex)
            {
                CurrentBrowseExceptionIndex = nextIndex;
                Signal("RefreshPanelText");
            }
            Signal("RefreshPrevNextButton");
        }

        public void GrafUpdate(float timeStacker, bool forceUpdate = true)
        {
            if (!Enable && !forceUpdate)
                return;

            for (int i = dockObjects.Count - 1; i >= 0; i--)
                dockObjects[i].DrawSprites(timeStacker);
        }

        public void RawUpdate(float dt)
        {
           

            if (!Enable)
                return;

            timeStacker += dt * framePerSecond;
            while (timeStacker > 1f)
            {
                Update();
                timeStacker--;
            }

            GrafUpdate(timeStacker);
            //BuffPlugin.Log($"Dock rawupdate {timeStacker},{dt}");
        }

        public void AddObject(IDockObject dockObject)
        {
            dockObjects.Add(dockObject);
            dockObject.Dock = this;
            dockObject.InitSprites();
        }

        public void SetShow(bool show)
        {
            if (show == Show || showAnimCounter > 0)
                return;

            Show = show;
            showAnimCounter = 20;
        }
    }

    interface IDockObject
    {
        public BuffExceptionSideDock Dock { get; set; }

        void InitSprites();
        void Update();
        void DrawSprites(float timeStacker);
        void RemoveSprites();

        void Signal(string message);
    }
}
