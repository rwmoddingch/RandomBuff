using System.Collections.Generic;
using System.Linq;
using HUD;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
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

            foreach (var id in BuffPoolManager.Instance.GetAllBuffIds())
                inGameSlot.AppendCardDirectly(id);

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

        public void TriggerCard(BuffID id)
        {
            inGameSlot.TriggerBuff(id);
        }

        public void RemoveCard(BuffID id)
        {
            inGameSlot.BasicSlot.RemoveCard(id);
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
        }


        public static BuffHud Instance { get; private set; }

        private CommmmmmmmmmmmmmpleteInGameSlot inGameSlot;
    }

    //internal class TConditionHud : HudPart
    //{
    //    public TConditionHud(HUD.HUD hud) : base(hud)
    //    {
    //        gameSetting = BuffPoolManager.Instance.GameSetting;
    //        foreach (var condition in gameSetting.conditions)
    //        {
    //            condition.BindHudFunction(OnCompleted, OnUncompleted,OnLabelRefresh);
    //            //加入已经完成的判断
    //            FLabel label = new FLabel(Custom.GetDisplayFont(),
    //                condition.DisplayName(Custom.rainWorld.inGameTranslator) + " " +
    //                condition.DisplayProgress(Custom.rainWorld.inGameTranslator))
    //            {
    //                alignment = FLabelAlignment.Left,
    //                anchorX = 0,
    //                anchorY = 1
    //            };
    //            hud.fContainers[1].AddChild(label);
    //            label.y = Custom.rainWorld.screenSize.y - dicts.Count * 30;
    //            label.x = 20;
    //            dicts.Add(condition,label);
    //        }
    //    }

    //    public void OnLabelRefresh(Condition condition)
    //    {
    //        if (dicts.TryGetValue(condition, out var label))
    //            label.text = condition.DisplayName(Custom.rainWorld.inGameTranslator) + " " +
    //                         condition.DisplayProgress(Custom.rainWorld.inGameTranslator);
    //    }

    //    public void OnCompleted(Condition condition)
    //    {
    //        //TODO: 完成时效果
    //    }
    //    public void OnUncompleted(Condition condition)
    //    {
    //        //TODO: 撤销完成时效果
    //    }

    //    public override void ClearSprites()
    //    {
    //        base.ClearSprites();
    //        foreach(var condition in gameSetting.conditions)
    //            condition.UnbindHudFunction(OnCompleted,OnUncompleted,OnLabelRefresh);
    //        foreach(var label in dicts.Values)
    //            label.RemoveFromContainer();
    //    }

    //    private Dictionary<Condition, FLabel> dicts = new();
    //    private GameSetting gameSetting;
    //}
}
