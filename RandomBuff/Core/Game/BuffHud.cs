using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.UI;

namespace RandomBuff.Core.Game
{
    internal class BuffHud : HudPart
    {
        public BuffHud(HUD.HUD hud) : base(hud)
        {
            inGameSlot = new InGameBuffCardSlot();
            hud.fContainers[0].AddChild(inGameSlot.Container);

            foreach (var id in BuffDataManager.Instance.GetAllBuffIds())
                inGameSlot.AppendCard(id);
            Instance = this;
        }

        public void AppendNewCard(BuffID id)
        {
            inGameSlot.AppendCard(id);
        }

        public override void Update()
        {
            inGameSlot.Update();
        }


        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            inGameSlot.GrafUpdate(timeStacker);
        }

        public void RemoveCard(BuffID id)
        {
            //TODO : Debug
            inGameSlot.ForceRemoveCard_DEBUG(id);
        }


        public static BuffHud Instance { get; private set; }

        private InGameBuffCardSlot inGameSlot;
    }
}
