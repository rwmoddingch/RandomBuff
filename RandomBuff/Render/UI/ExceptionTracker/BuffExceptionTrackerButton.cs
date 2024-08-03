using RandomBuff.Render.UI.Component;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI.ExceptionTracker
{
    internal class BuffExceptionTrackerButton : IDockObject
    {
        public static Color flashCol = Color.white;
        public static Color normCol = new Color(0.6f, 0.6f, 0.6f);
        public static Color deactiveCol = new Color(0.3f, 0.3f, 0.3f);
        public static Vector2 bumpSize = new Vector2(10f, 10f);

        public BuffExceptionSideDock Dock { get; set; }

        RoundRectSprites roundRect;

        readonly string signal;
        public Action<string, BuffExceptionTrackerButton> siganlCallBack;

        bool alphaContainer;

        public bool fixedPosition;
        protected Vector2 lastPos;
        protected Vector2 pos;
        protected Vector2 bottomLeftPos;
        protected Vector2 size;

        protected float sizeBump;
        protected float lastSizeBump;

        protected float flash;
        protected float lastFlash;

        protected float activeParam;

        public bool active = true;

        public Color color;
        public Color lastCol;

        public bool MouseInside { get; private set; }
        protected FContainer Container => alphaContainer ? Dock.alphaContainer : Dock.Container;


        public BuffExceptionTrackerButton(string signal, Vector2 size, Vector2 bottomLeftDockPos, bool alpha = false)
        {
            this.size = size;
            this.signal = signal;
            this.alphaContainer = alpha;
            bottomLeftPos = bottomLeftDockPos;
        }

        public virtual void InitSprites()
        {
            roundRect = new RoundRectSprites(Container, bottomLeftPos + Dock.BottomLeftPos, size, true);
            lastPos = pos = bottomLeftPos + Dock.BottomLeftPos;
        }


        public virtual void Update()
        {
            lastPos = pos;
            pos = fixedPosition ? bottomLeftPos : bottomLeftPos + Dock.BottomLeftPos;

            Vector2 delta = Dock.ScreenMousePosition - pos;
            if (delta.x > 0f && delta.x < size.x && delta.y > 0f && delta.y < size.y && active)
            {
                if (!MouseInside)
                    flash = 1f;
                MouseInside = true;
            }
            else
                MouseInside = false;

            if (MouseInside && Dock.MouseClick)
                Dock.Signal(signal);

            lastSizeBump = sizeBump;
            sizeBump = Mathf.Lerp(sizeBump, MouseInside ? 1f : 0f, 0.25f);

            lastFlash = flash;
            flash = Mathf.Lerp(flash, 0f, 0.25f);

            activeParam = Mathf.Lerp(activeParam, active ? 1f : 0f, 0.25f);

            lastCol = color;
            color = Color.Lerp(deactiveCol, normCol, activeParam);
            color = Color.Lerp(color, flashCol, flash);

            roundRect.Update();
            roundRect.pos = pos - sizeBump * bumpSize * 0.5f;
            roundRect.size = size + sizeBump * bumpSize;
        }

        public virtual void DrawSprites(float timeStacker)
        {
            roundRect.GrafUpdate(timeStacker);
            roundRect.borderColor = Color.Lerp(lastCol, color, timeStacker);
        }


        public virtual void RemoveSprites()
        {
            roundRect.RemoveSprites();
        }

        public virtual void Signal(string message)
        {
            siganlCallBack?.Invoke(message, this);
        }
    }

    internal class TrackerTextButton : BuffExceptionTrackerButton
    {
        string _text;
        public string Text
        {
            get => _text;
            set
            {
                if (value == _text) return;
                _text = value;

                if (label != null)
                    label.text = _text;
            }
        }
        FLabel label;
        public TrackerTextButton(string text, string signal, Vector2 size, Vector2 bottomLeftDockPos, bool alpha = false) : base(signal, size, bottomLeftDockPos, alpha)
        {
            Text = text;
        }

        public override void InitSprites()
        {
            base.InitSprites();
            label = new FLabel(Custom.GetDisplayFont(), Text);
            Container.AddChild(label);
        }

        public override void DrawSprites(float timeStacker)
        {
            base.DrawSprites(timeStacker);
            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker) + size / 2f;
            label.SetPosition(smoothPos);
            label.color = Color.Lerp(lastCol, color, timeStacker);
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            label.RemoveFromContainer();
        }

        public override void Signal(string message)
        {
            base.Signal(message);
            if (message == "NewTracker")
                flash = 1f;
        }
    }
}
