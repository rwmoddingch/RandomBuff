using RandomBuff.Render.UI.Component;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI
{
    internal class BuffSlotTitle
    {
        public FContainer Container { get; private set; } = new FContainer();

        CardTitle cardTitle;
        Vector2 titlePos;

        string lastTitle = "";

        public BuffSlotTitle()
        {
            Vector2 screenSize = Custom.rainWorld.screenSize;
            titlePos = new Vector2(screenSize.x * 0.5f, screenSize.y * 0.8f);

            cardTitle = new CardTitle(Container, BuffCard.normalScale * 0.2f, titlePos, 0.2f, 0.5f, 10, 3);
        }

        public void ChangeTitle(string newTitle, bool recordAsLast)
        {
            cardTitle.RequestSwitchTitle(newTitle);
            if(recordAsLast)
                lastTitle = newTitle;
        }

        public void ResumeLastTitle()
        {
            cardTitle.RequestSwitchTitle(lastTitle);
        }

        public void Update()
        {
            cardTitle.Update();
        }

        public void GrafUpdate(float timeStacker)
        {
            cardTitle.GrafUpdate(timeStacker);
        }

        public void Destroy()
        {
            cardTitle.Destroy();
        }
    }
}
