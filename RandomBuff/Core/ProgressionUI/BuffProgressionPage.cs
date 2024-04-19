using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using RandomBuff.Core.BuffMenu;
using RandomBuff.Core.BuffMenu.Test;
using RandomBuff.Render.UI;
using RandomBuff.Render.UI.Component;
using UnityEngine;

namespace RandomBuff.Core.ProgressionUI
{
    internal class BuffProgressionPage : Page
    {
        public static float levelBarWidth = 800f;

        //页面元素
        FSprite blackSprite;
        FSprite blurSprite;

        BuffLevelBarDynamic levelBar;
        CardTitle recordTitle;

        SimpleButton backButton;

        //状态变量
        int lastMenuPageIndex;
        Vector2 screenSize;
        Vector2 levelBarPos;
        Vector2 recordTitlePos;

        int _showCounter = -1;
        int _targetShowCounter;
        bool Show
        {
            get => _targetShowCounter == BuffGameMenuStatics.MaxShowSwitchCounter;
            set => _targetShowCounter = (value ? BuffGameMenuStatics.MaxShowSwitchCounter : 0);
        }
        float ShowFactor => (float)_showCounter / BuffGameMenuStatics.MaxShowSwitchCounter;

        public BuffProgressionPage(Menu.Menu menu, MenuObject owner, int index) : base(menu, owner, "ProgressionPage", index)
        {
            screenSize = menu.manager.rainWorld.options.ScreenSize;
            myContainer = new FContainer();
            menu.container.AddChild(myContainer);
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

            //
            subObjects.Add(backButton = new SimpleButton(menu, this, menu.Translate("BACK"), "PROGRESSIONPAGE_BACK", new Vector2(200f, 698f), new Vector2(110f, 30f)));

            recordTitlePos = new Vector2(screenSize.x / 2f, levelBarPos.y - 100f);
            recordTitle = new CardTitle(Container, BuffCard.normalScale * 0.3f, recordTitlePos);
        }

        public void ShowProgressionPage()
        {
            lastMenuPageIndex = menu.currentPage;
            menu.currentPage = index;
            Show = true;
            recordTitle.RequestSwitchTitle("Record", true);
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
        }

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
        }
    }
}
