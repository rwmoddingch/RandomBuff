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
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.SaveData;
using UnityEngine;

namespace RandomBuff.Core.GachaMenu
{
    internal class GachaMenu : Menu.Menu
    {
        private List<BuffID> picked = new ();

        private BuffSlotTitle slotTitle;
        private RainEffect rainEffect;

        public GachaMenu(ProcessManager.ProcessID lastID, RainWorldGame game, ProcessManager manager) : base(manager, BuffEnums.ProcessID.GachaMenuID)
        {
            pages.Add(new Menu.Page(this, null, "GachaMenu", 0));

            rainEffect = new RainEffect(this, pages[0]);
            pages[0].subObjects.Add(rainEffect);

            pages[0].subObjects.Add(exitButton = new SimpleButton(this, pages[0], "Exit", "ExitButton", 
                new Vector2(ContinueAndExitButtonsXPos - 320f - manager.rainWorld.options.SafeScreenOffset.x, 50f), new Vector2(100f, 30f)));
            this.lastID = lastID;
            this.game = game;
            pages[0].selectables.Remove(exitButton);
            inGameSlot = new BasicInGameBuffCardSlot();
            InputAgency.Current.TakeFocus(inGameSlot.BaseInteractionManager);

            foreach(var id in BuffDataManager.Instance.GetAllBuffIds(game.StoryCharacter))
                inGameSlot.AppendCard(id);
            container.AddChild(inGameSlot.Container);
            slotTitle = new BuffSlotTitle();
            container.AddChild(slotTitle.Container);

            pickSlots = BuffDataManager.Instance.GetGameSetting(game.StoryCharacter).fallbackPick;

            AnimMachine.GetTickAnimCmpnt(0, 80, autoDestroy: true).BindActions(OnAnimGrafUpdate: (t, f) =>
            {
                rainEffect.rainFade = Custom.SCurve(t.Get(), 0.8f) * 0.3f;
            });

            NewPicker();
        }



        public void Select(BuffID id)
        {
            BuffPlugin.Log($"Pick buff : {id}");
            BuffDataManager.Instance.GetOrCreateBuffData(id, true);
            picked.Add(id);
            if (pickerSlots.Last().majorSelections.Contains(id) ||
                pickerSlots.Last().additionalSelections.Contains(id))
                RequestNewPicker();
        }

        public void RequestNewPicker()
        {
            if ((++selectCount) < pickSlots.Count)
                NewPicker();
            else
            {
                exitCounter = 0;
                pages[0].selectables.Add(exitButton);
            }
        }

        private void NewPicker()
        {
            CardPickerSlot pickerSlot = new CardPickerSlot(inGameSlot, Select,
                pickSlots[selectCount].major,
                pickSlots[selectCount].additive, pickSlots[selectCount].selectCount, slotTitle, true);
            pickerSlots.Add(pickerSlot);
            container.AddChild(pickerSlot.Container);
            if (pickerSlots.Count >= 2)
                pickerSlot.Container.MoveBehindOtherNode(pickerSlots[pickerSlots.Count-1].Container); 
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
            foreach(var pick in pickerSlots.Where(i => i != null))
                pick.Destory();
            inGameSlot.Destory();
            slotTitle.Destroy();
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "ExitButton" && exitCounter >= 0)
            {
                manager.RequestMainProcessSwitch(lastID);
                ShutDownProcess();
            }
        }

        public override void Update()
        {
            base.Update();
            for (int i = 0; i < pickerSlots.Count; i++)
                pickerSlots[i]?.Update();
            inGameSlot?.Update();
            slotTitle.Update();
            exitButton.inactive = exitCounter <= 0;
            if (exitCounter != -1)
                exitCounter++;
            InputAgency.StaticUpdate();
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
                exitButton.black = Custom.LerpMap(exitCounter + timeStacker, 0, 40, 1, 0f, 0.75f);
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
        private List<GameSetting.FallbackPickSlot> pickSlots;

    }
}
