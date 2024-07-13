using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using HUD;
using RandomBuff.Core.Buff;
using RandomBuff.Core.BuffMenu.Test;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.UI;
using RandomBuff.Render.UI.Component;
using RWCustom;
using UnityEngine;

namespace RandomBuff.Core.Game
{
    internal class BuffHud : HudPart
    {
        public BuffHud(HUD.HUD hud) : base(hud)
        {
            slotTitle = new BuffSlotTitle();
            inGameSlot = new CommmmmmmmmmmmmmpleteInGameSlot(slotTitle);
            hud.fContainers[1].AddChild(inGameSlot.Container);
            hud.fContainers[1].AddChild(slotTitle.Container);

            foreach (var id in BuffPoolManager.Instance.GetAllBuffIds())
            {
                inGameSlot.AppendCardDirectly(id);
                HandleAddBuffHUD(id);
            }

            Instance = this;

            if(Custom.rainWorld.processManager.menuSetup.startGameCondition ==
               ProcessManager.MenuSetup.StoryGameInitCondition.New)
                NewGame(Custom.rainWorld.progression.miscProgressionData
                    .currentlySelectedSinglePlayerSlugcat);
        }

        public void NewGame(SlugcatStats.Name saveName)
        {
            var setting = BuffPoolManager.Instance.GameSetting.gachaTemplate;
            if (setting.CurrentPacket.NeedMenu)
            {
                for (int i = 0; i < setting.CurrentPacket.positive.pickTimes; i++)
                {

                    var positiveCards = BuffPicker.GetNewBuffsOfType(saveName, setting.CurrentPacket.positive.showCount,
                        BuffType.Positive);
                    var negativeCardsList = BuffPicker.GetNewBuffsOfType(saveName, setting.CurrentPacket.positive.showCount,
                        BuffType.Negative, BuffType.Duality);

                    if (positiveCards == null || negativeCardsList == null)
                        break;

                    var negativeCards = negativeCardsList.Select(i => i.BuffID).ToArray();

                    for (int j = 0; j < positiveCards.Count; j++)
                        negativeCards[j] = positiveCards[j].BuffProperty == BuffProperty.Special ? negativeCards[j] : null;

                    inGameSlot.RequestPickCards((id) =>
                        {
                            BuffPoolManager.Instance.CreateBuff(id,true);

                        }, positiveCards.Select(i => i.BuffID).ToArray(),
                        negativeCards, setting.CurrentPacket.positive.selectCount);
                }
                for (int i = 0; i < setting.CurrentPacket.negative.pickTimes; i++)
                {
                    var pickList = BuffPicker.GetNewBuffsOfType(saveName, setting.CurrentPacket.negative.showCount, 
                        BuffType.Negative, BuffType.Duality);
                    if (pickList == null)
                        break;
                    inGameSlot.RequestPickCards((id) =>
                        {
                            BuffPoolManager.Instance.CreateBuff(id, true);
                        }, pickList.Select(i => i.BuffID).ToArray(),
                        new BuffID[setting.CurrentPacket.negative.showCount], setting.CurrentPacket.negative.selectCount);
                }
            }
        }

        public void AppendNewCard(BuffID id)
        {
            inGameSlot.AppendCard(id);
            HandleAddBuffHUD(id);
        }

        public void AppendCardDirectly(BuffID id)
        {
            inGameSlot.AppendCardDirectly(id);
            HandleAddBuffHUD(id);
        }

        public override void Update()
        {
            inGameSlot.Update();
            slotTitle.Update();

            for (int i = hudParts.Count - 1; i >= 0; i--)
            {
                hudParts[i].Update(hud);
            }
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            inGameSlot.GrafUpdate(timeStacker);
            slotTitle.GrafUpdate(timeStacker);
            for (int i = hudParts.Count - 1; i >= 0; i--)
            {
                hudParts[i].Draw(hud, timeStacker);
            }
        }

        public void TriggerCard(BuffID id)
        {
            inGameSlot.TriggerBuff(id);
        }

        public void RemoveCard(BuffID id)
        {
            inGameSlot.BasicSlot.RemoveCard(id);
            HandleRemoveBuffHUD(id);
        }

        public override void ClearSprites()
        {
            inGameSlot.Destory();
            slotTitle.Destroy();
            base.ClearSprites();

            foreach(var part in hudParts)
            {
                part.ClearSprites();
            }
            hudParts.Clear();
            id2hudParts.Clear();
        }

        public void HandleAddBuffHUD(BuffID id)
        {
            if(BuffPoolManager.Instance.GetBuff(id) is BuffHudPart.IOwnBuffHudPart owner)
            {
                var part = owner.CreateHUDPart();
                part.owner = this;
                part.InitSprites(hud);

                id2hudParts.Add(id, part);
                hudParts.Add(part);
            }
        }

        public void HandleRemoveBuffHUD(BuffID id)
        {
            if (id2hudParts.TryGetValue(id, out var part))
            {
                part.ClearSprites();

                id2hudParts.Remove(id);
                hudParts.Remove(part);
            }
        }


        public static BuffHud Instance { get; private set; }

        BuffSlotTitle slotTitle;
        private CommmmmmmmmmmmmmpleteInGameSlot inGameSlot;

        Dictionary<BuffID, BuffHudPart> id2hudParts = new Dictionary<BuffID, BuffHudPart>();
        List<BuffHudPart> hudParts = new List<BuffHudPart>();
    }

    public abstract class BuffHudPart
    {
        internal BuffHud owner;
        public RoomCamera Camera => (owner.hud.owner as Player).abstractCreature.Room.world.game.cameras[0];

        public virtual void InitSprites(HUD.HUD hud)
        {
        }

        public virtual void Update(HUD.HUD hud)
        {
        }

        public virtual void Draw(HUD.HUD hud, float timeStacker)
        {
        }

        public virtual void ClearSprites()
        {
        }

        public interface IOwnBuffHudPart
        {
            BuffHudPart CreateHUDPart();
        }
    }
}
