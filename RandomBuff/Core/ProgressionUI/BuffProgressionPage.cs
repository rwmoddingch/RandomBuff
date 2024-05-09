using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RandomBuff.Core.BuffMenu;
using RandomBuff.Core.BuffMenu.Test;
using RandomBuff.Core.Progression;
using RandomBuff.Core.Progression.Record;
using RandomBuff.Core.SaveData;
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
        public static int pageCount = 3;
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

        float smoothPage = 0.001f;
        int setPage;
        float lastSmoothPage;

        bool Show
        {
            get => _targetShowCounter == BuffGameMenuStatics.MaxShowSwitchCounter;
            set => _targetShowCounter = (value ? BuffGameMenuStatics.MaxShowSwitchCounter : 0);
        }
        float ShowFactor => (float)_showCounter / BuffGameMenuStatics.MaxShowSwitchCounter;

        //FLabel testLabel;
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
            levelBar = new BuffLevelBarDynamic(myContainer, levelBarPos, levelBarWidth, BuffPlayerData.Instance.playerTotExp, BuffPlayerData.Exp2Level, BuffPlayerData.Level2Exp);
            levelBar.setAlpha = 1f;
            levelBar.HardSet();

            recordTitlePos = new Vector2(screenSize.x / 2f, levelBarPos.y - 100f);
            recordTitle = new CardTitle(Container, BuffCard.normalScale * 0.3f, recordTitlePos , flipCounter: 10);

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
            var testPage3 = CreatePage("COSMETIC", 2);

            CreateElementsForRecordPage(testPage, recordScrollBoxSize, BuffPlayerData.Instance.SlotRecord);
            CreateElementsForCosmeticPage(testPage3, recordScrollBoxSize);
            //testLabel = new FLabel(Custom.GetDisplayFont(), "WA");
            //Container.AddChild(testLabel);
            //testLabel.SetPosition(200, 200);
        }

        public static void CreateElementsForRecordPage(OpScrollBox opScrollBox, Vector2 size, InGameRecord records)
        {
            float labelHeight = 30f;
            float span = 30f;

            int totalEntryCount = records.GetValueDictionary().Count;
            int current = 0;
            float currentHue = 0f;

            float contentSize = opScrollBox.contentSize = Mathf.Max((totalEntryCount + 1) * labelHeight, size.y);
            foreach(var pair in records.GetValueDictionary())
            {
                Color col = (new HSLColor(currentHue, 1f, 0.8f)).rgb;
                
                OpLabel entryLabel = new OpLabel(span, contentSize - (current + 1) * labelHeight,      BuffResourceString.Get(pair.Key), true) { color = col };
                entryLabel.label.shader = Custom.rainWorld.Shaders["MenuTextCustom"];
                OpLabel valueLabel = new OpLabel(size.x - span, contentSize - (current + 1) * labelHeight, pair.Value, true) { color = col };
                valueLabel.label.shader = Custom.rainWorld.Shaders["MenuTextCustom"];

                opScrollBox.AddItems(entryLabel, valueLabel);

                current++;
                currentHue += 0.05f;
            }
            opScrollBox.ScrollToTop(true);
        }

        public static void CreateElementsForCosmeticPage(OpScrollBox opScrollBox, Vector2 size)
        {
            //参数
            Vector2 buttonSize = new Vector2(60, 60);
            Vector2 smallGap = new Vector2(5, 5);
            float bigGap = 20f;
            int buttonsOneLine = 5;

            //初始化cosmetic
            List<CosmeticUnlock> noBindCosmetics = new List<CosmeticUnlock>();
            Dictionary<SlugcatStats.Name, List<CosmeticUnlock>> nameForCosmetics = new Dictionary<SlugcatStats.Name, List<CosmeticUnlock>>();

            var buffData = BuffPlayerData.Instance;
            var configManager = BuffConfigManager.Instance;

            foreach(var idValue in CosmeticUnlockID.values.entries)
            {
                var id = new CosmeticUnlockID(idValue);
                if((BuffConfigManager.IsCosmeticCanUse(idValue) || true/*目前先跳过*/) && CosmeticUnlock.cosmeticUnlocks.ContainsKey(id))
                {
                    var cosmetic = Activator.CreateInstance(CosmeticUnlock.cosmeticUnlocks[id]) as CosmeticUnlock;
                    if(cosmetic.BindCat == null)
                        noBindCosmetics.Add(cosmetic);
                    else
                    {
                        if (nameForCosmetics.ContainsKey(cosmetic.BindCat))
                            nameForCosmetics[cosmetic.BindCat].Add(cosmetic);
                        else
                            nameForCosmetics.Add(cosmetic.BindCat, new List<CosmeticUnlock>() { cosmetic });
                    }
                }
            }

            //计算contentSize
            float contentSize = 0f;
            contentSize += CaculateBlockHeight(noBindCosmetics.Count);
            foreach(var pair in nameForCosmetics)
                contentSize += CaculateBlockHeight(pair.Value.Count);
            contentSize += bigGap;
            contentSize = Mathf.Max(contentSize, size.y);

            opScrollBox.contentSize = contentSize;

            //创建元素
            float yPointer = contentSize;
            yPointer -= smallGap.y;

            yPointer -= CreateSingleBlock(new Color(0.5f, 0.5f, 0.5f), noBindCosmetics, yPointer, true);
            foreach(var pair in nameForCosmetics)
            {
                Color col = PlayerGraphics.SlugcatColor(pair.Key);
                yPointer -= CreateSingleBlock(col, pair.Value, yPointer);
            }


            float CaculateBlockHeight(int totalCount)
            {
                int line = Mathf.CeilToInt(totalCount / (float)buttonsOneLine);
                return line * buttonSize.y + Mathf.Max(line - 1, 0) * smallGap.y + bigGap;
            }

            float CreateSingleBlock(Color color, List<CosmeticUnlock> unlocks, float startY, bool first = false)
            {
                //Color disableColor = color * 0.5f;
                //disableColor.a = 1f;

                float yDecrease = buttonSize.y;
                if (!first)
                    yDecrease += bigGap;

                float x = smallGap.x;
                int lineButtonCount = 0;

                OpImage icon = new OpImage(new Vector2(x, startY - yDecrease), "Kill_Slugcat") { color = color };
                opScrollBox.AddItems(icon);
                x += 40f;

                foreach(var unlock in unlocks)
                {
                    CosmeticButton cosmeticButton = new CosmeticButton(new Vector2(x, startY - yDecrease), buttonSize, unlock.IconElement, unlock.UnlockID.value, Color.green, color, OnCosmeticButtonClick);
                    cosmeticButton.SetEnable(buffData.IsCosmeticEnable(unlock.UnlockID.value));

                    opScrollBox.AddItems(cosmeticButton);
                    
                    lineButtonCount++;
                    if(lineButtonCount >= buttonsOneLine)
                    {
                        lineButtonCount = 0;
                        yDecrease += smallGap.y + buttonSize.y;
                        x = smallGap.x + 40f;
                    }
                    else
                    {
                        x += smallGap.x + buttonSize.x;
                    }
                }
                return yDecrease;
            }
        }

        static void OnCosmeticButtonClick(CosmeticButton button)
        {
            BuffPlugin.Log($"{button.id}, {BuffPlayerData.Instance.IsCosmeticEnable(button.id)}");
            bool newEnable = !BuffPlayerData.Instance.IsCosmeticEnable(button.id);
            BuffPlayerData.Instance.SetCosmeticEnable(button.id, newEnable);
            button.SetEnable(newEnable);
        }

        OpScrollBox CreatePage(string pageName, int index)
        {
            pageNames[index] = pageName;
            pagePosesDelta[index] = new Vector2(0, -recordScrollBoxSize.y - 140f - 40f);
            var opScrollBox = new OpScrollBox(levelBarPos + pagePosesDelta[index], recordScrollBoxSize, 400f, false, false ,false);
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

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            subObjects.Remove(tabWrapper);
            tabWrapper.RemoveSprites();
        }
    }

    public class CosmeticButton : OpSimpleImageButton
    {
        

        public string id;
        string element;

        Action<CosmeticButton> clickCallBack;

        Color enabledCol;
        Color disabledCol;

        public static FieldInfo OnClickField = typeof(OpSimpleButton).GetField("OnClick", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        public CosmeticButton(Vector2 pos, Vector2 size, string element, string id, Color enableCol, Color disableCol, Action<CosmeticButton> clickCallBack) : base(pos, size, element)
        {
            this.id = id;
            this.clickCallBack = clickCallBack;
            this.sprite = new FSprite(element, true);
            this.myContainer.AddChild(this.sprite);
            this.sprite.SetAnchor(0.5f, 0.5f);
            this.sprite.SetPosition(base.size.x / 2f, base.size.y / 2f);

            this.enabledCol = enableCol;
            this.disabledCol = disableCol;
            isTexture = true;

            var OnClick = OnClickField.GetValue(this) as OnSignalHandler;
            OnClick += OnClickCallBack;
            OnClickField.SetValue(this, OnClick);
        }

        void OnClickCallBack(UIfocusable uIfocusable)
        {
            if(uIfocusable == this)
            {
                clickCallBack?.Invoke(this);
            }
        }

        public void SetEnable(bool enable)
        {
            if (enable)
                colorEdge = enabledCol;
            else
                colorEdge = disabledCol;
        }

        public override void Unload()
        {
            isTexture = false;
            base.Unload();
        }
    }
}
