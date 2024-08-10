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
using Menu.Remix;
using Menu.Remix.MixedUI;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.ProgressionUI;
using RandomBuff.Render.UI.Notification;
using RandomBuff.Core.Progression;
using RandomBuff.Core.Progression.Quest;
using RandomBuff.Core.Progression.Record;

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

        public MenuTabWrapper wrapper;
        public OpScrollBox recordBox;

        public NotificationManager notificationManager;

        List<BuffQuest> newFinishedQuests;

        bool recordShowed;
        bool questShow;

        Vector2 recordBoxHidePos;
        Vector2 recordBoxShowPos;

        public bool isShowingDialog;
        public bool evaluateExpedition;
        public int questsDisplayed;
        public int startingLevel;
        public bool showLevelUp;
        public float leftAnchor;
        public float rightAnchor;

        int titleShowDelay = 20;

        private bool showCredit = false;

        public BuffGameWinScreen(ProcessManager manager) : base(manager, BuffEnums.ProcessID.BuffGameWinScreen)
        {
            pages = new List<Page>();
            pages.Add(new Page(this, null, "Main", 0));
            pages.Add(notificationManager = new NotificationManager(this, container, 1));
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

            title = new CardTitle(container, BuffCard.normalScale * 0.6f, new Vector2(middleX, 668f), 0.1f, spanAdjust: -50f);

            continueButton = new SimpleButton(this, pages[0], Translate("CONTINUE"), "CONTINUE", new Vector2(rightAnchor - 150f, 40f), new Vector2(100f, 30f));
            pages[0].subObjects.Add(continueButton);

            flag = new RandomBuffFlag(new IntVector2(40, 25), new Vector2(width + 200f, 500f));
            flagRenderer = new RandomBuffFlagRenderer(flag, RandomBuffFlagRenderer.FlagType.InnerTriangle, RandomBuffFlagRenderer.FlagColorType.Golden);
            flagRenderer.pos = new Vector2(middleX - (width + 200f) / 2f, 850f);
            flagRenderer.Show = true;
            pages[0].Container.AddChild(flagRenderer.container);
            flagRenderer.container.MoveBehindOtherNode(gradient);

            wrapper = new MenuTabWrapper(this, pages[0]);
            pages[0].subObjects.Add(wrapper);

            
            //winPackage
            var winPackage = BuffPoolManager.Instance.winGamePackage;
            var record = winPackage.buffRecord as InGameTimerRecord;
            BuffPlugin.Log($"Win with kills : {winPackage.sessionRecord.kills.Count}, Mission Id: {winPackage.missionId ??"null"}");
            var str = "[RECORD]: ";
            foreach (var item in record.GetValueDictionary())
                str += $"{{{item.Key},{item.Value}}},";

            BuffPlugin.Log(str);

            Vector2 screenSize = Custom.rainWorld.options.ScreenSize;
            Vector2 recordSize = new Vector2(400f, 300f);
            recordBoxHidePos = new Vector2(screenSize.x + 10, screenSize.y - recordSize.y - 100f);
            recordBoxShowPos = recordBoxHidePos + new Vector2(-40 - recordSize.x, 0);
            recordBox = new OpScrollBox(new Vector2(screenSize.x + 10, screenSize.y - recordSize.y - 100f), recordSize, 100f, false, false, false);
            new UIelementWrapper(wrapper, recordBox);
            BuffProgressionPage.CreateElementsForRecordPage(recordBox, recordSize, record);


            pages[0].subObjects.Add(scoreCaculator = new BuffGameScoreCaculator(this, pages[0], new Vector2(middleX - width / 2f, 200f), winPackage, width));
            scoreCaculator.Container.MoveToFront();

            if (BuffPoolManager.Instance.GameSetting.gachaTemplate is not SandboxGachaTemplate)
            {
                BuffPlayerData.Instance.SlotRecord.RunCount++;

                if (winPackage.missionId != null)
                    BuffPlayerData.Instance.finishedMission.Add(winPackage.missionId);


                newFinishedQuests = BuffPlayerData.Instance.UpdateQuestState(winPackage);
                showCredit = newFinishedQuests.Any(i => i.QuestId == "builtin.quest.Crown");
                foreach (var quest in newFinishedQuests)
                    BuffPlugin.Log($"accomplish quest: {quest.QuestName}:{quest.QuestId}");

            }
            else
            {
                newFinishedQuests = new List<BuffQuest>();
            }

            manager.rainWorld.progression.WipeSaveState(winPackage.saveState.saveStateNumber);

        }

        public override void Singal(MenuObject sender, string message)
        {
            if (message == "CONTINUE")
            {
                if(newFinishedQuests.Count == 0 && scoreCaculator != null && scoreCaculator.state == BuffGameScoreCaculator.ScoreCaculatorState.Finish)
                {
                    if(questShow)
                    {
                        this.manager.RequestMainProcessSwitch(showCredit ? BuffEnums.ProcessID.CreditID : ProcessManager.ProcessID.MainMenu);
                        if (manager.musicPlayer != null)
                            manager.musicPlayer.FadeOutAllSongs(50f);
                    }
                }
                else
                {
                    scoreCaculator.fastCaculate = true;
                }
                
            }
        }

        TickAnimCmpnt animCmpnt; 
               
        public void OnScoreCaculateFinish()
        {
            if (recordShowed)
                return;

            recordShowed = true;
            animCmpnt = new TickAnimCmpnt(0, 60, autoStart: false, autoDestroy: true)
            .BindModifier(Helper.EaseInOutCubic)
            .BindActions(OnAnimGrafUpdate: UpdateRecordBox, OnAnimFinished: (_) =>
            {
                animCmpnt = null;
                questShow = true;
            });
            animCmpnt.SetEnable(true);
        }

        void UpdateRecordBox(TickAnimCmpnt tickAnimCmpnt, float a)
        {
            float p = tickAnimCmpnt.Get();
            recordBox.pos = Vector2.Lerp(recordBoxHidePos, recordBoxShowPos, p);
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

            if(questShow && newFinishedQuests.Count > 0 && notificationManager.banners.Count == 0)
            {
                var quest = newFinishedQuests.Pop();
                foreach (var pair in quest.UnlockItem)
                {
                    foreach (var item in pair.Value)
                    {
                        notificationManager.NewRewardNotification(pair.Key, item);
                    }
                }
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
