using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RandomBuff.Core.BuffMenu;
using RandomBuff.Core.BuffMenu.Test;
using RandomBuff.Render.UI;
using RandomBuff.Render.UI.Component;
using RWCustom;
using UnityEngine;

namespace RandomBuff.Core.ProgressionUI
{
    internal class BuffProgressionPage : Page
    {
        public static float levelBarWidth = 800f;
        public static Vector2 recordScrollBoxSize = new Vector2(levelBarWidth, 400f);
        public static int pageCount = 2;
        public static float pageSpan = 1400f;

        //页面元素
        FSprite blackSprite;
        FSprite blurSprite;

        BuffLevelBarDynamic levelBar;
        CardTitle recordTitle;

        SimpleButton backButton;
        BigArrowButton prevRecordPageButton;
        BigArrowButton nextRecordPageButton;

        MenuTabWrapper tabWrapper;

        string[] pageNames;
        OpScrollBox[] pages;
        Vector2[] pagePosesDelta;

        //状态变量
        int lastMenuPageIndex;
        Vector2 screenSize;
        Vector2 levelBarPos;
        Vector2 recordTitlePos;

        int _showCounter = -1;
        int _targetShowCounter;

        float smoothPage;
        int setPage;
        float lastSmoothPage;

        bool Show
        {
            get => _targetShowCounter == BuffGameMenuStatics.MaxShowSwitchCounter;
            set => _targetShowCounter = (value ? BuffGameMenuStatics.MaxShowSwitchCounter : 0);
        }
        float ShowFactor => (float)_showCounter / BuffGameMenuStatics.MaxShowSwitchCounter;

        FLabel testLabel;
        public BuffProgressionPage(Menu.Menu menu, MenuObject owner, int index) : base(menu, owner, "ProgressionPage", index)
        {
            screenSize = menu.manager.rainWorld.options.ScreenSize;
            myContainer = new FContainer();
            menu.container.AddChild(myContainer);
            lastPos = pos = Vector2.Lerp(BuffGameMenuStatics.HidePos, Vector2.zero, Helper.LerpEase(ShowFactor));
            InitPage();
        }

        void InitPage()
        {
            myContainer.AddChild(blackSprite = new FSprite("pixel") { scaleX = screenSize.x, scaleY = screenSize.y, x = screenSize.x / 2f, y = screenSize.y / 2f , color = Color.black});
            myContainer.AddChild(blurSprite = new FSprite("pixel") { scaleX = screenSize.x, scaleY = screenSize.y, x = screenSize.x / 2f, y = screenSize.y / 2f , shader = menu.manager.rainWorld.Shaders["UIBlur"] ,color = Color.black});

            levelBarPos = new Vector2(screenSize.x / 2f - levelBarWidth / 2f, screenSize.y - 110f);
            levelBar = new BuffLevelBarDynamic(myContainer, levelBarPos, levelBarWidth, 1234, 100);
            levelBar.setAlpha = 1f;
            levelBar.HardSet();

            recordTitlePos = new Vector2(screenSize.x / 2f, levelBarPos.y - 100f);
            recordTitle = new CardTitle(Container, BuffCard.normalScale * 0.3f, recordTitlePos);

            //初始化按钮
            subObjects.Add(backButton = new SimpleButton(menu, this, menu.Translate("BACK"), "PROGRESSIONPAGE_BACK", new Vector2(200f, 698f), new Vector2(110f, 30f)));

            subObjects.Add(tabWrapper = new MenuTabWrapper(menu, this));
            subObjects.Add(prevRecordPageButton = new BigArrowButton(menu, this, "PROGRESSIONPAGE_PREV_RECORD", recordTitlePos + new Vector2(-recordScrollBoxSize.x / 2f, -20f),3));
            subObjects.Add(nextRecordPageButton = new BigArrowButton(menu, this, "PROGRESSIONPAGE_NEXT_RECORD", recordTitlePos + new Vector2(recordScrollBoxSize.x / 2f - 50f, -20f), 1));


            pages = new OpScrollBox[pageCount];
            pageNames = new string[pageCount];
            pagePosesDelta = new Vector2[pageCount];

            var testPage = CreatePage("RECORD", 0);
            var testPage2 = CreatePage("QUEST", 1);

            testLabel = new FLabel(Custom.GetDisplayFont(), "WA");
            Container.AddChild(testLabel);
            testLabel.SetPosition(200, 200);
        }

        OpScrollBox CreatePage(string pageName, int index)
        {
            pageNames[index] = pageName;
            pagePosesDelta[index] = new Vector2(0, -recordScrollBoxSize.y - 140f - 40f);
            var opScrollBox = new OpScrollBox(levelBarPos + pagePosesDelta[index], recordScrollBoxSize, 400f);
            new UIelementWrapper(tabWrapper, opScrollBox);
            pages[index] = opScrollBox;

            return opScrollBox;
        }

        public void ShowProgressionPage()
        {
            lastMenuPageIndex = menu.currentPage;
            menu.currentPage = index;
            Show = true; 
            recordTitle.RequestSwitchTitle(pageNames[setPage], true);
        }

        public void HideProgressionPage()
        {
            menu.currentPage = lastMenuPageIndex;
            Show = false;
            recordTitle.RequestSwitchTitle("");
        }

        public override void Update()
        {
            base.Update();
            levelBar.Update();
            recordTitle.Update();

            levelBar.pos = levelBarPos + pos;
            recordTitle.pos = recordTitlePos + pos;
            if (_showCounter != _targetShowCounter)
            {
                if (_showCounter < _targetShowCounter)
                    _showCounter++;
                else if (_showCounter > _targetShowCounter)
                    _showCounter--;

                pos = Vector2.Lerp(BuffGameMenuStatics.HidePos, Vector2.zero, Helper.LerpEase(ShowFactor));
                //gameMenu.menuSlot.basePos = pos;
                
                blackSprite.alpha = ShowFactor * 0.8f;
                blurSprite.alpha = ShowFactor;
            }

            lastSmoothPage = smoothPage;
            if(smoothPage != setPage)
            {
                smoothPage = Mathf.Lerp(smoothPage, setPage, 0.15f);
                if(Mathf.Approximately(smoothPage, setPage))
                    smoothPage = setPage;

                for(int i = 0;i < pageCount; i++)
                {
                    float delta = i - smoothPage;
                    pages[i].pos = pos + levelBarPos + pagePosesDelta[i] + new Vector2(pageSpan * delta, 0f);
                }
            }
        }

        //TickAnimCmpnt animCmpnt = AnimMachine.GetTickAnimCmpnt(0, 100, autoStart:false).BindModifier(Helper.EaseInOutSine);
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            levelBar.GrafUpdate(timeStacker);
            recordTitle.GrafUpdate(timeStacker);
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if(message == "PROGRESSIONPAGE_BACK")
            {
                HideProgressionPage();
            }
            else if(message == "PROGRESSIONPAGE_PREV_RECORD")
            {
                setPage = Mathf.Clamp(setPage - 1, 0, pageCount - 1);
                recordTitle.RequestSwitchTitle(pageNames[setPage]);
            }
            else if (message == "PROGRESSIONPAGE_NEXT_RECORD")
            {
                setPage = Mathf.Clamp(setPage + 1, 0, pageCount - 1);
                recordTitle.RequestSwitchTitle(pageNames[setPage]);
            }
        }
    }
}
