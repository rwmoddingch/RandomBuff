using System.Linq;
using HUD;
using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.UI;
using RWCustom;
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

            if(Custom.rainWorld.processManager.menuSetup.startGameCondition ==
               ProcessManager.MenuSetup.StoryGameInitCondition.New)
                NewGame(Custom.rainWorld.progression.miscProgressionData
                    .currentlySelectedSinglePlayerSlugcat);
        }

        public void NewGame(SlugcatStats.Name saveName)
        {
            var setting = BuffDataManager.Instance.GetSafeSetting(saveName);
            if (setting.instance.CurrentPacket.NeedMenu)
            {
                for (int i = 0; i < setting.instance.CurrentPacket.positive.pickTimes; i++)
                {

                    var positiveCards = BuffPicker.GetNewBuffsOfType(saveName, setting.instance.CurrentPacket.positive.showCount,
                        BuffType.Positive);

                    var negativeCards = BuffPicker.GetNewBuffsOfType(saveName, setting.instance.CurrentPacket.positive.showCount,
                        BuffType.Negative, BuffType.Duality).Select(i => i.BuffID).ToArray();

                    for (int j = 0; j < positiveCards.Count; j++)
                        negativeCards[j] = positiveCards[j].BuffProperty == BuffProperty.Special ? negativeCards[j] : null;

                    inGameSlot.RequestPickCards((id) =>
                        {
                            BuffPoolManager.Instance.CreateBuff(id);
                            AppendNewCard(id);

                        }, positiveCards.Select(i => i.BuffID).ToArray(),
                        negativeCards, setting.instance.CurrentPacket.positive.selectCount);
                }
                for (int i = 0; i < setting.instance.CurrentPacket.negative.pickTimes; i++)
                {

                    inGameSlot.RequestPickCards((id) =>
                        {
                            BuffPoolManager.Instance.CreateBuff(id);
                            AppendNewCard(id);

                        }, BuffPicker.GetNewBuffsOfType(saveName, setting.instance.CurrentPacket.negative.showCount,
                            BuffType.Negative, BuffType.Duality).Select(i => i.BuffID).ToArray(),
                        new BuffID[setting.instance.CurrentPacket.negative.showCount], setting.instance.CurrentPacket.negative.selectCount);
                }
            }
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
