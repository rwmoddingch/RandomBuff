using Menu;
using RandomBuff.Render.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.BuffMenu
{
    internal class BuffGameMenu : Menu.Menu, CheckBox.IOwnCheckBox
    {
        RainEffect rainEffect;

        List<SlugcatStats.Name> slugNameOrders = new List<SlugcatStats.Name>();
        Dictionary<SlugcatStats.Name, WawaSaveData> saveGameData = new Dictionary<SlugcatStats.Name, WawaSaveData>();

        bool restartCurrent;
        //菜单元素
        HoldButton startButton;
        SimpleButton backButton;
        BigArrowButton prevButton;
        BigArrowButton nextButton;
        CheckBox restartCheckbox;

        CardInteractionManager interactionManager;
        BuffCard testCard;

        MenuLabel testLabel;

        public BuffGameMenu(ProcessManager manager, ProcessManager.ProcessID ID) : base(manager, ID)
        {
            if (manager.rainWorld.options.saveSlot < 100)//诺普的存档加载
            {
                var lastSlot = manager.rainWorld.options.saveSlot;
                BuffPlugin.Log($"Enter from slot {lastSlot}, To {manager.rainWorld.options.saveSlot += 100}");
                manager.rainWorld.progression.Destroy(lastSlot);
                manager.rainWorld.progression = new PlayerProgression(manager.rainWorld, true, false);
            }
            SetupSlugNameOrders();
            foreach(var name in slugNameOrders)
            {
                saveGameData.Add(name, MineFromSave(manager, name));
            }

            pages = new List<Page>()
            {
                new (this, null, "WawaPage", 0)
            };

            //构建页面
            pages[0].subObjects.Add(rainEffect = new RainEffect(this, pages[0]));

            pages[0].subObjects.Add(startButton = new HoldButton(this, this.pages[0], "", "START", new Vector2(683f, 85f), 40f));
            pages[0].subObjects.Add(backButton = new SimpleButton(this, this.pages[0], base.Translate("BACK"), "BACK", new Vector2(200f, 668f), new Vector2(110f, 30f)));
            pages[0].subObjects.Add(prevButton = new BigArrowButton(this, this.pages[0], "PREV", new Vector2(500f, 50f), -1));
            pages[0].subObjects.Add(nextButton = new BigArrowButton(this, this.pages[0], "NEXT", new Vector2(816f, 50f), 1));

            pages[0].subObjects.Add(testLabel = new MenuLabel(this, this.pages[0], "", new Vector2(100f, 50f), new Vector2(100f, 30f), false));

            float restartTextWidth = SlugcatSelectMenu.GetRestartTextWidth(base.CurrLang);
            float restartTextOffset = SlugcatSelectMenu.GetRestartTextOffset(base.CurrLang);

            pages[0].subObjects.Add(restartCheckbox = new CheckBox(this, this.pages[0], this, new Vector2(this.startButton.pos.x + 200f + restartTextOffset, Mathf.Max(30f, manager.rainWorld.options.SafeScreenOffset.y)), restartTextWidth, base.Translate("Restart game"), "RESTART", false));
            restartCheckbox.label.pos.x = restartCheckbox.label.pos.x + (restartTextWidth - restartCheckbox.label.label.textRect.width - 5f);

            interactionManager = new BasicInteractionManager();
            testCard = new BuffCard(new Buff.BuffID("BounceSpear"));
            container.AddChild(testCard.Container);

            testCard.Position = new Vector2(400f, 300f);

            interactionManager.ManageCard(testCard);
            testCard.Container.MoveToBack();
        }

        void SetupSlugNameOrders()
        {
            foreach(var entry in SlugcatStats.Name.values.entries)
            {
                slugNameOrders.Add(new SlugcatStats.Name(entry));
            }
        }

        WawaSaveData MineFromSave(ProcessManager manager, SlugcatStats.Name slugcat)
        {
            if (!manager.rainWorld.progression.IsThereASavedGame(slugcat))
            {
                return null;
            }
            if(manager.rainWorld.progression.currentSaveState != null && manager.rainWorld.progression.currentSaveState.saveStateNumber == slugcat)
            {
                WawaSaveData result = new WawaSaveData();
                result.karmaCap =    manager.rainWorld.progression.currentSaveState.deathPersistentSaveData.karmaCap;
                result.karma =       manager.rainWorld.progression.currentSaveState.deathPersistentSaveData.karma;
                result.food =        manager.rainWorld.progression.currentSaveState.food;
                result.cycle =       manager.rainWorld.progression.currentSaveState.cycleNumber;
                result.hasGlow =     manager.rainWorld.progression.currentSaveState.theGlow;
                result.hasMark =     manager.rainWorld.progression.currentSaveState.deathPersistentSaveData.theMark;
                result.shelterName = manager.rainWorld.progression.currentSaveState.GetSaveStateDenToUse();
                return result;
            }
            return null;
        }

        public bool GetChecked(CheckBox box)
        {
            return restartCurrent;
        }

        public void SetChecked(CheckBox box, bool c)
        {
        }

        public override void Singal(MenuObject sender, string message)
        {
            if(message == "BACK")
            {
                manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                PlaySound(SoundID.MENU_Switch_Page_Out);
            }
        }

        public override void Update()
        {
            base.Update();
            interactionManager.Update();
            testLabel.text = $"CurrentFocused: {testCard.CurrentFocused}; localMousePos x:{testCard.LocalMousePos.x} y:{testCard.LocalMousePos.y}\nEdge : {testCard.Highlight}";
        }

        public override void RawUpdate(float dt)
        {
            base.RawUpdate(dt);
            interactionManager.GrafUpdate();
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
        }

        internal class WawaSaveData
        {
            public int karmaCap;
            public int karma;
            public int food;
            public int cycle;
            public bool hasGlow;
            public bool hasMark;
            public string shelterName;
        }
    }
}
