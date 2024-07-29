using Menu;
using RandomBuff.Render.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RWCustom;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.SaveData;
using UnityEngine;

namespace RandomBuff.Core.GachaMenu
{
    internal class GachaMenu : Menu.Menu
    {
        public static ProcessManager.ProcessID GachaMenuID = new ("GachaMenu", true);

        private List<BuffID> picked = new ();
        BuffSlotTitle slotTitle;

        public GachaMenu(ProcessManager.ProcessID lastID, RainWorldGame game, ProcessManager manager) : base(manager, GachaMenuID)
        {
            pages.Add(new Menu.Page(this, null, "GachaMenu", 0));
            pages[0].subObjects.Add(exitButton = new SimpleButton(this, pages[0], "Exit", "ExitButton", 
                new Vector2(ContinueAndExitButtonsXPos - 320f - manager.rainWorld.options.SafeScreenOffset.x, 50f), new Vector2(100f, 30f)));
            this.lastID = lastID;
            this.game = game;
            inGameSlot = new BasicInGameBuffCardSlot();
            InputAgency.Current.TakeFocus(inGameSlot.BaseInteractionManager);

            foreach(var id in BuffDataManager.Instance.GetAllBuffIds(game.StoryCharacter))
                inGameSlot.AppendCard(id);
            container.AddChild(inGameSlot.Container);
            slotTitle = new BuffSlotTitle();
            container.AddChild(slotTitle.Container);

            currentPacket = BuffDataManager.Instance.GetGameSetting(game.StoryCharacter).gachaTemplate.CurrentPacket;
            if(currentPacket.positive.pickTimes == 0)
                positive = false;
            NewPicker();
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if(message == "ExitButton" && exitCounter >= 0)
            {
                manager.RequestMainProcessSwitch(lastID);
                ShutDownProcess();
            }
        }

        public void Select(BuffID id)
        {
            BuffPlugin.Log($"Pick buff : {id}");
            BuffDataManager.Instance.GetOrCreateBuffData(id, true);
            picked.Add(id);
            RequestNewPicker();
        }

        public void RequestNewPicker()
        {
            selectCount++;
            bool needNew = false;
            if (positive)
            {
                if (selectCount == currentPacket.positive.selectCount)
                {
                    currentPacket.positive.pickTimes--;
                    if (currentPacket.positive.pickTimes == 0)
                        positive = false;
                    if (currentPacket.negative.pickTimes != 0)
                    {
                        selectCount = 0;
                        needNew = true;
                    }
                    else
                    {
                        exitCounter = 0;
                        return;
                    }
                }
            }
            else
            {
                if (selectCount == currentPacket.negative.selectCount)
                {
                    currentPacket.negative.pickTimes--;
                    if (currentPacket.negative.pickTimes == 0)
                    {
                        exitCounter = 0;
                        return;
                    }
                    needNew = true;
                    selectCount = 0;
                }
            }

            if(needNew)
                NewPicker();
        }

        private void NewPicker()
        {
            CardPickerSlot pickerSlot;
            if (positive)
            {
                var positiveCards = BuffPicker.GetNewBuffsOfType(game.StoryCharacter, currentPacket.positive.showCount,
                    BuffType.Positive);

                var negativeCardsList = BuffPicker.GetNewBuffsOfType(game.StoryCharacter, currentPacket.positive.showCount,
                    BuffType.Negative, BuffType.Duality);

                //卡牌库存不足
                if (positiveCards == null || negativeCardsList == null)
                {
                    positive = false;
                    selectCount = 0;
                    RequestNewPicker();
                    return;
                }

                var negativeCards = negativeCardsList.Select(i => i.BuffID).ToArray();

                for (int i=0;i< positiveCards.Count;i++)
                    negativeCards[i] = positiveCards[i].BuffProperty == BuffProperty.Special ? negativeCards[i] : null;

                pickerSlot = new CardPickerSlot(inGameSlot, Select,
                    positiveCards.Select(i => i.BuffID).ToArray(),
                    negativeCards,
                    currentPacket.positive.selectCount, slotTitle);
            }
            else
            {
                var pickList = BuffPicker.GetNewBuffsOfType(game.StoryCharacter, currentPacket.negative.showCount,
                    BuffType.Negative, BuffType.Duality);

                //卡牌库存不足
                if (pickList == null)
                {
                    exitCounter = 0;
                    return;
                }

                pickerSlot = new CardPickerSlot(inGameSlot, Select,
                    pickList.Select(i => i.BuffID).ToArray(),
                    new BuffID[currentPacket.negative.showCount], currentPacket.negative.selectCount, slotTitle, true);
            }
            pickerSlots.Add(pickerSlot);
            container.AddChild(pickerSlot.Container);
            inGameSlot.Container.MoveToFront();
        }


        //把oldProcess放回去
        public override void ShutDownProcess()
        {
            base.ShutDownProcess();
            InputAgency.AllRelease();
            if (manager.oldProcess != game)
            {
                var all = BuffDataManager.Instance.GetAllBuffIds(game.StoryCharacter);
                foreach (var con in BuffDataManager.Instance.GetGameSetting(game.StoryCharacter).conditions)
                    con.GachaEnd(picked,all);
                BuffDataManager.Instance.GetGameSetting(game.StoryCharacter).fallbackPick = null;

                BuffFile.Instance.SaveFile();
                manager.oldProcess = game;
            }
        }

        public override void Update()
        {
            base.Update();
            for (int i = 0; i < pickerSlots.Count; i++)
                pickerSlots[i]?.Update();
            inGameSlot?.Update();
            slotTitle.Update();

            if (exitCounter != -1)
                exitCounter++;

        }


        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            for(int i=0;i<pickerSlots.Count;i++)
                pickerSlots[i]?.GrafUpdate(timeStacker);
            inGameSlot?.GrafUpdate(timeStacker);
            slotTitle.GrafUpdate(timeStacker);

            //一个很笨的淡入
            if (exitCounter < 40)
                exitButton.roundedRect.borderColor = exitButton.labelColor =
                    new HSLColor(0, 0, Custom.LerpMap(exitCounter + timeStacker, 0, 40, 0, 1f, 0.75f));
        }



        public float ContinueAndExitButtonsXPos =>
            manager.rainWorld.options.ScreenSize.x + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f;


        private List<CardPickerSlot> pickerSlots = new ();
        private BasicInGameBuffCardSlot inGameSlot;
        private ProcessManager.ProcessID lastID;
        private RainWorldGame game;

        private int exitCounter = -1;
        private SimpleButton exitButton;

        private int selectCount;
        private bool positive = true;
        private GachaTemplate.CachaPacket currentPacket;

    }
}
