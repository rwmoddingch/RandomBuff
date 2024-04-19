using Menu;
using RandomBuff.Core.BuffMenu;
using RandomBuff.Core.BuffMenu.Test;
using RandomBuff.Core.Game;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.SaveData;
using UnityEngine;

namespace RandomBuff.Core.StaticsScreen
{
    internal class BuffGameWinScreen : Menu.Menu
    {
        public SimpleButton continueButton;

        public FSprite shadow;
        public FSprite title;
        public FSprite gradient;

        public RandomBuffFlag flag;
        public RandomBuffFlagRenderer flagRenderer;
        public BuffGameScoreCaculator scoreCaculator;

        public SleepScreenKills kills;

        public MenuLabel missionTime;

        public MenuLabel bestMissionTime;

        public bool isShowingDialog;

        public bool evaluateExpedition;

        public int questsDisplayed;

        public int startingLevel;

        public bool showLevelUp;

        public float leftAnchor;

        public float rightAnchor;

        public BuffGameWinScreen(ProcessManager manager) : base(manager, BuffEnums.ProcessID.BuffGameWinScreen)
        {
            pages = new List<Page>();
            pages.Add(new Page(this, null, "Main", 0));
            scene = new InteractiveMenuScene(this, pages[0], MenuScene.SceneID.SleepScreen);
            scene.camPos.x = scene.camPos.x - 400f;
            pages[0].subObjects.Add(scene);
            leftAnchor = Custom.GetScreenOffsets()[0];
            rightAnchor = Custom.GetScreenOffsets()[1];

            if (manager.musicPlayer != null)
            {
                manager.musicPlayer.MenuRequestsSong("RW_65 - Garden", 100f, 50f);
            }
            gradient = new FSprite("LinearGradient200", true);
            gradient.x = 0f;
            gradient.y = 0f;
            gradient.rotation = 90f;
            gradient.SetAnchor(1f, 0f);
            gradient.scaleY = 3f;
            gradient.scaleX = 1500f;
            gradient.color = new Color(0f, 0f, 0f);
            pages[0].Container.AddChild(this.gradient);
            shadow = new FSprite("expeditionshadow", true);
            shadow.x = 10f;
            shadow.y = 638f;
            shadow.SetAnchor(0f, 0f);
            shadow.shader = manager.rainWorld.Shaders["MenuText"];
            pages[0].Container.AddChild(this.shadow);
            title = new FSprite("expeditiontitle", true);
            title.x = 10f;
            title.y = 638f;
            title.SetAnchor(0f, 0f);
            pages[0].Container.AddChild(this.title);
            FSprite fsprite = new FSprite("LinearGradient200", true);
            fsprite.rotation = 90f;
            fsprite.scaleY = 2.5f;
            fsprite.scaleX = 2.5f;
            fsprite.SetAnchor(new Vector2(0.5f, 0f));
            fsprite.x = 40f;
            fsprite.y = 646f;
            fsprite.shader = manager.rainWorld.Shaders["MenuText"];
            pages[0].Container.AddChild(fsprite);

            continueButton = new SimpleButton(this, pages[0], Translate("CONTINUE"), "CONTINUE", new Vector2(rightAnchor - 150f, 40f), new Vector2(100f, 30f));
            pages[0].subObjects.Add(continueButton);

            flag = new RandomBuffFlag(new IntVector2(40, 30), new Vector2(700f, 450f));
            flagRenderer = new RandomBuffFlagRenderer(flag, RandomBuffFlagRenderer.FlagType.InnerTriangle, RandomBuffFlagRenderer.FlagColorType.Golden);
            flagRenderer.pos = new Vector2(40f, 800f);
            flagRenderer.Show = true;
            pages[0].Container.AddChild(flagRenderer.container);
            flagRenderer.container.MoveBehindOtherNode(gradient);

            //winPackage
            var winPackage = BuffPoolManager.Instance.winGamePackage;
            BuffPlugin.Log($"Win with kills : {winPackage.sessionRecord.kills.Count}, Mission Id: {winPackage.missionId ??"null"}, Tot Card In Game : {BuffPoolManager.Instance.GameSetting.TotCardInGame}");

            //TODO:在这里完成结算数据上传到BuffPlayerData，并在之后调用以下函数
            var newFinishQuests = BuffPlayerData.Instance.UpdateQuestState(winPackage);
            foreach(var quest in newFinishQuests)
                BuffPlugin.Log($"accomplish quest: {quest.QuestName}");
            //TODO:新任务完成的提示

            pages[0].subObjects.Add(scoreCaculator = new BuffGameScoreCaculator(this, pages[0], new Vector2(200f, 200f), winPackage));
            scoreCaculator.Container.MoveToFront();
        }

        public override void Singal(MenuObject sender, string message)
        {
            if (message == "CONTINUE")
            {
                this.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
            }
        }

        public override void Update()
        {
            base.Update();
            flag.Update();
            flagRenderer.Update();
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            flagRenderer.GrafUpdate(timeStacker);
        }
    }
}
