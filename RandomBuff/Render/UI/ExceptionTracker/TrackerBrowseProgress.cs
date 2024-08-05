using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI.ExceptionTracker
{
    internal class TrackerBrowseProgress : IDockObject
    {
        public BuffExceptionSideDock Dock { get; set; }

        FLabel label;

        int current;
        int total;

        Vector2 bottomLeftPos;
        Vector2 pos;
        Vector2 lastPos;

        public TrackerBrowseProgress(Vector2 pos)
        {
            this.bottomLeftPos = pos;
        }

        public void InitSprites()
        {
            label = new FLabel(Custom.GetDisplayFont(), "");
            label.color = BuffExceptionTrackerButton.normCol;

           Dock.Container.AddChild(label);

            pos = lastPos = bottomLeftPos + Dock.BottomLeftPos;
        }

        public void Update()
        {
            lastPos = pos;
            pos = bottomLeftPos + Dock.BottomLeftPos;
        }
        public void DrawSprites(float timeStacker)
        {
            label.SetPosition(Vector2.Lerp(lastPos, pos, timeStacker));
        }

        public void RemoveSprites()
        {
            label.RemoveFromContainer();
        }

        public void Signal(string message)
        {
            if (message == "NewTracker")
            {
                UpdateText();
            }
            else if(message == "RefreshPanelText")
            {
                UpdateText();
            }
        }

        public void UpdateText()
        {
            current = Dock.CurrentBrowseExceptionIndex + 1;
            total = BuffExceptionTracker.allTrackers.Count;
            label.text = $"{current}/{total}";
        }

    }
}
