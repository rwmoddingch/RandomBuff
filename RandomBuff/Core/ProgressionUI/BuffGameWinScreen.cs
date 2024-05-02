using Menu;
using RandomBuff.Core.BuffMenu;
using RandomBuff.Core.Game;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.SaveData;
using UnityEngine;
using RandomBuff.Render.UI.Component;
using RandomBuff.Render.UI;

namespace RandomBuff.Core.StaticsScreen
{
    internal class BuffGameWinScreen : Menu.Menu
    {
        public SimpleButton continueButton;


        public FSprite gradient;

        public RandomBuffFlag flag;
        public RandomBuffFlagRenderer flagRenderer;
        public BuffGameScoreCaculator scoreCaculator;
        public CardTitle title;

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

        int titleShowDelay = 20;

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

            float middleX = 400f;
            float width = 460f;

            title = new CardTitle(container, BuffCard.normalScale * 0.6f, new Vector2(middleX, 668f), 0.5f);

            continueButton = new SimpleButton(this, pages[0], Translate("CONTINUE"), "CONTINUE", new Vector2(rightAnchor - 150f, 40f), new Vector2(100f, 30f));
            pages[0].subObjects.Add(continueButton);

            flag = new RandomBuffFlag(new IntVector2(40, 25), new Vector2(width + 200f, 500f));
            flagRenderer = new RandomBuffFlagRenderer(flag, RandomBuffFlagRenderer.FlagType.InnerTriangle, RandomBuffFlagRenderer.FlagColorType.Golden);
            flagRenderer.pos = new Vector2(middleX - (width + 200f) / 2f, 850f);
            flagRenderer.Show = true;
            pages[0].Container.AddChild(flagRenderer.container);
            flagRenderer.container.MoveBehindOtherNode(gradient);

            //winPackage
            var winPackage = BuffPoolManager.Instance.winGamePackage;
            BuffPlugin.Log($"Win with kills : {winPackage.sessionRecord.kills.Count}, Mission Id: {winPackage.missionId ??"null"}");
            var str = "[RECORD]: ";
            foreach (var item in winPackage.buffRecord.GetValueDictionary())
                str += $"{{{item.Key},{item.Value}}},";

            BuffPlugin.Log(str);

           //TODO:在这里完成结算数据上传到BuffPlayerData，并在之后调用以下函数
            var newFinishQuests = BuffPlayerData.Instance.UpdateQuestState(winPackage);
            foreach(var quest in newFinishQuests)
                BuffPlugin.Log($"accomplish quest: {quest.QuestName}");
            //TODO:新任务完成的提示

            pages[0].subObjects.Add(scoreCaculator = new BuffGameScoreCaculator(this, pages[0], new Vector2(middleX - width / 2f, 200f), winPackage, width));
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
            title.Update();

            if (titleShowDelay > 0)
                titleShowDelay--;
            else if(titleShowDelay == 0)
            {
                titleShowDelay--;
                title.RequestSwitchTitle("Random Buff");
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            flagRenderer.GrafUpdate(timeStacker);
            title.GrafUpdate(timeStacker);
        }
    }
}
