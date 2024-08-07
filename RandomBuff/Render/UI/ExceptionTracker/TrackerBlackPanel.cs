using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI.ExceptionTracker
{
    internal class TrackerBlackPanel : IDockObject
    {
        public BuffExceptionSideDock Dock { get; set; }
        FSprite black;
        FSprite gradient;

        Vector2 size;
        Vector2 bottomLeftPos;
        Vector2 lastBottomLeftPos;

        public TrackerBlackPanel(Vector2 size, Vector2 bottomLeftPos)
        {
            this.size = size;
            lastBottomLeftPos = this.bottomLeftPos = bottomLeftPos;
        }

        public void InitSprites()
        {
            var blur = Custom.rainWorld.Shaders["UIBlur"];
            black = new FSprite("pixel", true) { scaleX = size.x, scaleY = size.y, anchorX = 0f, anchorY = 0f, color = Color.black, shader = blur };
            gradient = new FSprite("LinearGradient200", true) { scaleX = size.y, scaleY = 0f, anchorX = 0f, anchorY = 0f, rotation = -90f, color = Color.black };
            Dock.Container.AddChild(black);
            Dock.Container.AddChild(gradient);
        }

        public void DrawSprites(float timeStacker)
        {
            Vector2 smoothPos = Vector2.Lerp(lastBottomLeftPos, bottomLeftPos, timeStacker);
            black.SetPosition(smoothPos);
            gradient.SetPosition(smoothPos + new Vector2(1f, 0f));
            float smoothShowFactor = Mathf.Lerp(Dock.LastShowFactor, Dock.ShowFactor, timeStacker);
            gradient.alpha = smoothShowFactor;
            gradient.scaleY = 0.1f * smoothShowFactor;
        }


        public void RemoveSprites()
        {
            black.RemoveFromContainer();
        }

        public void Signal(string message)
        {
        }

        public void Update()
        {
            lastBottomLeftPos = bottomLeftPos;
            bottomLeftPos = Dock.BottomLeftPos;
        }
    }
}
