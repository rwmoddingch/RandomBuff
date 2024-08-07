using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using JollyCoop.JollyMenu;
using Menu;
using Menu.Remix;
using Music;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;
using RWCustom;
using UnityEngine;
using static RandomBuff.Core.SaveData.BuffFile;

namespace RandomBuff.Core.BuffMenu.Test
{

    ////T开头均为测试用
    //internal class TStartGameMenu : Menu.Menu, CheckBox.IOwnCheckBox
    //{
    //    public static ProcessManager.ProcessID BuffGameMenu = new("BuffGameMenu", true);

    //    public TStartGameMenu(ProcessManager manager, ProcessManager.ProcessID ID) : base(manager, BuffGameMenu)
    //    {
    //        callBack = new (OnDataLoaded);
    //        if (manager.rainWorld.options.saveSlot < 100)
    //        {
    //            var lastSlot = manager.rainWorld.options.saveSlot;
    //            BuffPlugin.Log($"Enter from slot {lastSlot}, To {manager.rainWorld.options.saveSlot += 100}");
    //            manager.rainWorld.progression.Destroy(lastSlot);
    //            manager.rainWorld.progression = new PlayerProgression(manager.rainWorld, true, false);
    //        }

    //    }



    //    void OnDataLoaded()
    //    {
    //        isLoaded = true;
    //        pages = new List<Page>()
    //        {
    //            new (this, null, "OHHHH", 0)
    //        };
    //        pages[0].subObjects.Add(selectButton = new SimpleButton(this, pages[0], Translate(SlugcatStats.getSlugcatName(currentName)), "SELECT", new Vector2(200, 350),
    //            new Vector2(120, 40)));
    //        pages[0].subObjects.Add(startButton = new SimpleButton(this, pages[0], Translate("NEW GAME"), "START", new Vector2(200, 300),
    //            new Vector2(120, 40)));
    //        pages[0].subObjects.Add(settingButton = new SimpleButton(this, pages[0], Translate(BuffDataManager.Instance.GetGameSetting(currentName).ID.value), "SELECT_MODE", new Vector2(200, 250),
    //            new Vector2(120, 40)));
    //        pages[0].subObjects.Add(new SimpleButton(this, pages[0], Translate("EXIT"), "EXIT", new Vector2(200, 200),
    //            new Vector2(120, 40)));
    //        pages[0].subObjects.Add(new CheckBox(this, pages[0], this, new Vector2(350, 360), 65, Translate("Restart"), "RESET", true));
            
    //        settingButton.inactive = BuffDataManager.Instance.GetGameSetting(currentName).instance != null && !needReset;
    //        if (!manager.rainWorld.progression.IsThereASavedGame(currentName))
    //            startButton.menuLabel.text = Translate("NEW GAME");
    //        else
    //            startButton.menuLabel.text = Translate("CONTINUE");
    //        if (ModManager.JollyCoop)
    //        {
    //            pages[0].subObjects.Add(coopButton = new SymbolButton(this, pages[0],
    //                "coop", "JOLLY_TOGGLE_CONFIG", new Vector2(440f, 550f)));
    //            coopButton.roundedRect.size = new Vector2(50f, 50f);
    //            coopButton.size = coopButton.roundedRect.size;

    //        }
    //    }

    //    private SlugcatStats.Name currentName = SlugcatStats.Name.White;
    //    private SimpleButton startButton;
    //    private SimpleButton settingButton;
    //    private SimpleButton selectButton;
    //    private SymbolButton coopButton;
    //    private bool needReset;

    //    private BuffFileCompletedCallBack callBack;

    //    public override void Singal(MenuObject sender, string message)
    //    {
    //        base.Singal(sender, message);
    //        if (message == "SELECT")
    //        {
    //            currentName = new SlugcatStats.Name(SlugcatStats.Name.values.entries[
    //                currentName.Index == SlugcatStats.Name.values.entries.Count - 1 ? 0 : currentName.Index + 1]);

    //            selectButton.menuLabel.text = Translate(SlugcatStats.getSlugcatName(currentName));
    //            if (!manager.rainWorld.progression.IsThereASavedGame(currentName))
    //                startButton.menuLabel.text = Translate("NEW GAME");
    //            else
    //                startButton.menuLabel.text = Translate("CONTINUE");

    //            var safeData = BuffDataManager.Instance.GetGameSetting(currentName);
    //            settingButton.menuLabel.text = Translate(safeData.ID.ToString());
    //            settingButton.inactive = safeData.instance != null && !needReset;

    //        }
    //        else if (message == "START")
    //        {
            
    //            if (!manager.rainWorld.progression.IsThereASavedGame(currentName) || needReset)
    //            {
    //                manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat =
    //                    currentName;
    //                manager.rainWorld.progression.WipeSaveState(currentName);
    //                manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
                 
    //            }
    //            else
    //            {
    //                manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat =
    //                    currentName;
    //                manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
    //            }

    //            BuffDataManager.Instance.EnterGameFromMenu(currentName);
    //            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
    //            PlaySound(SoundID.MENU_Start_New_Game);
    //        }
    //        else if (message == "EXIT")
    //        {
    //            PlaySound(SoundID.MENU_Switch_Page_Out);
    //            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
    //        }
    //        else if (message == "JOLLY_TOGGLE_CONFIG")
    //        {
    //            Vector2 closeButtonPos = new Vector2(1000f - (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, manager.rainWorld.screenSize.y - 100f);
    //            JollySetupDialog dialog = new JollySetupDialog(currentName, manager, closeButtonPos);
    //            manager.ShowDialog(dialog);
    //        }
    //        else if (message == "SELECT_MODE")
    //        {
    //            var safeData = BuffDataManager.Instance.GetGameSetting(currentName);
    //            safeData.ID = new (GachaTemplateID.values.entries[
    //                safeData.ID.Index == GachaTemplateID.values.entries.Count - 1 ? 0 : safeData.ID.Index + 1]);

    //            settingButton.menuLabel.text = Translate(safeData.ID.ToString());
    //            settingButton.inactive = safeData.instance != null && !needReset;

    //        }
    //    }

    //    public bool GetChecked(CheckBox box)
    //    {
    //        return needReset;
    //    }

    //    public override void Update()
    //    {
    //        if (!isLoaded)
    //            return;

    //        base.Update();
    //    }

    //    public override void RawUpdate(float dt)
    //    {
    //        if (!isLoaded)
    //        {
    //            manager.blackDelay = 0.1f;
    //            return;
    //        }
    //        base.RawUpdate(dt);
    //    }


    //    private bool isLoaded;


    //    public void SetChecked(CheckBox box, bool c)
    //    {
    //        needReset = c;
    //    }
    //}
}
