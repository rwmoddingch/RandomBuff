using System.Linq;
using HUD;
using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.UI;
using UnityEngine;

namespace RandomBuff.Core.Game
{
    internal class BuffHud : HudPart
    {
        public BuffHud(HUD.HUD hud) : base(hud)
        {
            inGameSlot = new CommmmmmmmmmmmmmpleteInGameSlot();
            hud.fContainers[1].AddChild(inGameSlot.Container);

            foreach (var id in BuffDataManager.Instance.GetAllBuffIds())
                inGameSlot.AppendCardDirectly(id);

            Instance = this;
        }

        public void AppendNewCard(BuffID id)
        {
            inGameSlot.AppendCard(id);
        }

        public void AppendCardDirectly(BuffID id)
        {
            inGameSlot.AppendCardDirectly(id);
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
            inGameSlot.BasicSlot.RemoveCard(id, true);
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
        }


        public static BuffHud Instance { get; private set; }

        private CommmmmmmmmmmmmmpleteInGameSlot inGameSlot;
    }
}
