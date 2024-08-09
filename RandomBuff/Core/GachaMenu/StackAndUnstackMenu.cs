using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.UI;
using RWCustom;
using UnityEngine;

namespace RandomBuff.Core.GachaMenu
{
    internal class StackAndUnstackMenu : Menu.Menu
    {
        public StackAndUnstackMenu(ProcessManager manager,SlugcatStats.Name name, ProcessManager.ProcessID ID) : base(manager, ID)
        {
            this.title = new BuffSlotTitle();
            container.AddChild(title.Container);
            pages.Add(new Menu.Page(this, null, "StackAndUnstackMenu", 0));
            pages[0].subObjects.Add(exitButton = new SimpleButton(this, pages[0], "Exit", "ExitButton",
                new Vector2(ContinueAndExitButtonsXPos - 320f - manager.rainWorld.options.SafeScreenOffset.x, 50f), new Vector2(100f, 30f)));
            BuffPlugin.Log($"Enter StackAndUnstackMenu, ID:{ID}");
            pages[0].selectables.Remove(exitButton);
            inGameSlot = new BasicInGameBuffCardSlot();
            InputAgency.Current.TakeFocus(inGameSlot.BaseInteractionManager);
            var allIds = BuffDataManager.Instance.GetAllBuffIds(name);
            foreach (var id in allIds)
                inGameSlot.AppendCard(id);

            if (ID == BuffEnums.ProcessID.StackMenu)
            {
                var stackableArray = allIds.Where(i => i.GetStaticData().Stackable).ToArray();
                pickerSlot = new CardPickerSlot(inGameSlot, id =>
                    {
                        exitCounter = 0;
                        BuffPlugin.Log($"StackAndUnstackMenu, Select Card:{id}");
                        pages[0].selectables.Add(exitButton);
                        BuffDataManager.Instance.GetOrCreateBuffData(name,id, true);
                        BuffFile.Instance.SaveFile();
                    },
                    stackableArray, new BuffID[stackableArray.Length], 1, title, false,
                    BuffResourceString.Get("SleepMenu_Stack_Title"));
                container.AddChild(pickerSlot.Container);

            }
            else if(ID == BuffEnums.ProcessID.UnstackMenu) 
            {
                var stackableArray = allIds.ToArray();
                pickerSlot = new CardPickerSlot(inGameSlot, id =>
                    {
                        exitCounter = 0;
                        BuffPlugin.Log($"StackAndUnstackMenu, Select Card:{id}");

                        pages[0].selectables.Add(exitButton);
                        BuffDataManager.Instance.RemoveBuffData(name, id);
                        BuffFile.Instance.SaveFile();
                    },
                    stackableArray, new BuffID[stackableArray.Length], 1, title, false,
                    BuffResourceString.Get("SleepMenu_UnStack_Title"));
                container.AddChild(pickerSlot.Container);
            }
            else
            {
                exitCounter = 0;
                pages[0].selectables.Add(exitButton);
            }
            container.AddChild(inGameSlot.Container);

        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "ExitButton" && exitCounter >= 0)
            {
                manager.menuSetup.startGameCondition = BuffEnums.StoryGameInitCondition.BuffStackLoad;
                manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
                ShutDownProcess();
            }
        }
        public override void ShutDownProcess()
        {
            base.ShutDownProcess();
            pickerSlot?.Destory();
            inGameSlot?.Destory();
            title.Destroy();
            InputAgency.AllRelease();
        }


        public override void Update()
        {
            base.Update();
            pickerSlot?.Update();
            inGameSlot?.Update();
            title.Update();
            exitButton.inactive = exitCounter <= 0;
            if (exitCounter != -1)
                exitCounter++;
            InputAgency.StaticUpdate();
        }


        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
           
            pickerSlot?.GrafUpdate(timeStacker);
            inGameSlot?.GrafUpdate(timeStacker);
            title.GrafUpdate(timeStacker);
            if (exitCounter < 40)
                exitButton.black = Custom.LerpMap(exitCounter + timeStacker, 0, 40, 1, 0f, 0.75f);
        }


        public float ContinueAndExitButtonsXPos =>
            manager.rainWorld.options.ScreenSize.x + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f;

        private readonly BasicInGameBuffCardSlot inGameSlot;
        private readonly CardPickerSlot pickerSlot;
        private readonly BuffSlotTitle title;
        private readonly SimpleButton exitButton;

        private int exitCounter = -1;

    }
}
