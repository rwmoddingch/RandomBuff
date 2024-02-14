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
        }

        public override void Update()
        {
            inGameSlot.Update();
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            inGameSlot.GrafUpdate(timeStacker);

            if (Input.GetKeyDown(KeyCode.N))
            {
                var positiveCards = BuffPicker.GetNewBuffsOfType(SlugcatStats.Name.Yellow, 1,
                   BuffType.Positive, BuffType.Negative, BuffType.Duality).Select(i => i.BuffID).First();

                inGameSlot.TriggerBuff(positiveCards);
                //var negativeCards = BuffPicker.GetNewBuffsOfType(SlugcatStats.Name.Yellow, 3,
                //    BuffType.Negative, BuffType.Duality).Select(i => i.BuffID).ToArray();

                //inGameSlot.RequestPickCards((id) => BuffPlugin.Log($"Pick {id}"), positiveCards, new BuffID[3]);
                //inGameSlot.RequestPickCards((id) => BuffPlugin.Log($"Pick {id}"), negativeCards, new BuffID[3]);
            }
        }

        public void RemoveCard(BuffID id)
        {
            //TODO : Debug
            inGameSlot.RemoveCard(id, true);
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
        }


        public static BuffHud Instance { get; private set; }

        private CommmmmmmmmmmmmmpleteInGameSlot inGameSlot;
    }
}
