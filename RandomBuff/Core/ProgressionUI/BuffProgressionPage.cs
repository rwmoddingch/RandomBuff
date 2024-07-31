using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using MoreSlugcats;
using RandomBuff.Core.BuffMenu;
using RandomBuff.Core.BuffMenu.Test;
using RandomBuff.Core.Progression;
using RandomBuff.Core.Progression.Quest;
using RandomBuff.Core.Progression.Quest.Condition;
using RandomBuff.Core.Progression.Record;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.Quest;
using RandomBuff.Render.UI;
using RandomBuff.Render.UI.Component;
using RWCustom;
using UnityEngine;
using static RandomBuff.Core.ProgressionUI.QuestButton;
using static RandomBuff.Render.Quest.QuestRendererManager;

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

        QuestInfoHoverBox questInfoHoverBox;

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
            myContainer.AddChild(blurSprite = new FSprite("pixel") { scaleX = screenSize.x, scaleY = screenSize.y, x = screenSize.x / 2f, y = screenSize.y / 2f, shader = menu.manager.rainWorld.Shaders["UIBlur"], color = Color.black });
            myContainer.AddChild(blackSprite = new FSprite("pixel") { scaleX = screenSize.x, scaleY = screenSize.y, x = screenSize.x / 2f, y = screenSize.y / 2f , color = Color.black});

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

            var testPage = CreatePage(BuffResourceString.Get("ProgressionUI_Record"), 0);
            var testPage2 = CreatePage(BuffResourceString.Get("ProgressionUI_Quest"), 1);
            var testPage3 = CreatePage(BuffResourceString.Get("ProgressionUI_Cosmetic"), 2);

            questInfoHoverBox = new QuestInfoHoverBox(menu, this);
            
            //questInfoHoverBox.DisplayInfo(new QuestButton.QuestInfo());

            CreateElementsForRecordPage(testPage, recordScrollBoxSize, BuffPlayerData.Instance.SlotRecord);
            CreateElementsForCosmeticPage(testPage3, recordScrollBoxSize);
            CreateElementsForQuestPage(testPage2, recordScrollBoxSize, this);
            subObjects.Add(questInfoHoverBox);
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
                
                OpLabel entryLabel = new OpLabel(span, contentSize - (current + 1) * labelHeight, BuffResourceString.Get(pair.Key), true) { color = col };
                entryLabel.label.shader = Custom.rainWorld.Shaders["MenuTextCustom"];
                float width = LabelTest.GetWidth(pair.Value, true);
                OpLabel valueLabel = new OpLabel(size.x - span - width, contentSize - (current + 1) * labelHeight, pair.Value, true) { color = col };
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
            Vector2 buttonSize = new Vector2(70, 70);
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
                if(!BuffConfigManager.IsItemLocked(QuestUnlockedType.Cosmetic, idValue) && CosmeticUnlock.cosmeticUnlocks.ContainsKey(id))
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
                Color col = PlayerGraphics.DefaultSlugcatColor(pair.Key);
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
                    CosmeticButton cosmeticButton = new CosmeticButton(new Vector2(x, startY - yDecrease), buttonSize, unlock.IconElement, unlock.UnlockID.value, Color.green, 
                        (color * (unlock.BindCat == MoreSlugcatsEnums.SlugcatStatsName.Saint ? 0.5F : 1F)).CloneWithNewAlpha(1), OnCosmeticButtonClick);
                    cosmeticButton.SetEnable(buffData.IsCosmeticEnable(unlock.UnlockID.value));
                    cosmeticButton.wrapper = opScrollBox.wrapper;
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

        public static void CreateElementsForQuestPage(OpScrollBox opScrollBox, Vector2 size, BuffProgressionPage page)
        {
            //构建任务信息
            List<QuestButton.QuestInfo> levelQuests = new List<QuestButton.QuestInfo>();
            List<QuestButton.QuestInfo> otherQuests = new List<QuestButton.QuestInfo>();

            foreach(var questID in BuffConfigManager.GetQuestIDList())
            {
                bool isLevelQuest = false;
                var questInfo = new QuestButton.QuestInfo();
                questInfo.conditions = new List<string>();
                questInfo.rewards = new Dictionary<QuestUnlockedType, List<string>>();

                var questData = BuffConfigManager.GetQuestData(questID);
                if(!BuffResourceString.TryGet($"QuestName_{questData.QuestName}", out questInfo.name))
                    questInfo.name = Custom.rainWorld.inGameTranslator.Translate(questData.QuestName);
                questInfo.color = questData.QuestColor;
                
                foreach(var condition in questData.QuestConditions)
                {
                    if(condition is LevelQuestCondition levelQuestCondition)
                    {
                        isLevelQuest = true;
                        questInfo.level = levelQuestCondition.Level;
                    }

                    questInfo.conditions.Add(condition.ConditionMessage());
                }

                foreach(var rewards in questData.UnlockItem)
                {
                    questInfo.rewards.Add(rewards.Key, new List<string>());
                    foreach (var entry in rewards.Value)
                        questInfo.rewards[rewards.Key].Add(entry);
                }

                questInfo.finished = BuffPlayerData.Instance.IsQuestUnlocked(questID);
                if (isLevelQuest)
                {
                    levelQuests.Add(questInfo);
                    BuffPlugin.Log($"Add level quest : {questInfo.name}");
                }
                else
                {
                    otherQuests.Add(questInfo);
                    BuffPlugin.Log($"Add other quest : {questInfo.name}");
                }
            }

            levelQuests.Sort((x, y) =>
            {
                int res = x.level.CompareTo(y.level);
                if (res == 0)
                    res = x.conditions.Count.CompareTo(y.conditions.Count);
                return res;
            });


            //构建按钮元素
            Vector2 buttonSize = new Vector2(50, 50);
            Vector2 smallGap = new Vector2(5, 5);
            float bigGap = 20f;

            int buttonsInALine = Mathf.FloorToInt((size.x - smallGap.x) / (smallGap.x + buttonSize.x));
            int lineCount = Mathf.CeilToInt(levelQuests.Count / (float)buttonsInALine) + Mathf.CeilToInt(otherQuests.Count / (float)buttonsInALine);

            //                  顶部间隙   两种按钮总行间距                            两类按钮之间间距    底部间隙
            float contentSize = bigGap + lineCount * (smallGap.y + buttonSize.y) + bigGap     +     bigGap;
            contentSize = Mathf.Max(contentSize, size.y);
            opScrollBox.contentSize = contentSize;

            int x = 0;
            float xPtr = smallGap.x;
            float yPtr = contentSize - (bigGap + buttonSize.y);
            
            foreach(var info in levelQuests)
            {
                CreateButtonForInfo(info);
            }

            x = 0;
            xPtr = smallGap.x;
            yPtr -= bigGap + buttonSize.y;

            foreach(var info in otherQuests)
            {
                CreateButtonForInfo(info);
            }

            void CreateButtonForInfo(QuestButton.QuestInfo info)
            {
                var button = new QuestButton(info, new Vector2(xPtr, yPtr), buttonSize, info.finished);
                button.OnMouseOver += page.ShowQuestInfo;
                button.OnMouseLeave += page.HideQuestInfo;

                opScrollBox.AddItems(button);
                button.wrapper = opScrollBox.wrapper;
                x++;
                if (x == buttonsInALine)
                {
                    x = 0;
                    xPtr = smallGap.x;
                    yPtr -= (buttonSize.y + smallGap.y);
                }
                else
                {
                    xPtr += buttonSize.x + smallGap.x;
                }
            }
        }

        void ShowQuestInfo(QuestButton.QuestInfo questInfo)
        {
            questInfoHoverBox.DisplayInfo(questInfo);
        }

        void HideQuestInfo()
        {
            questInfoHoverBox.Hide();
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

        public void EscLogic()
        {
            HideProgressionPage();
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

            if(clickCallBack != null)
            {
                var OnClick = OnClickField.GetValue(this) as OnSignalHandler;
                OnClick += OnClickCallBack;
                OnClickField.SetValue(this, OnClick);
            }
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

    public class QuestButton : OpSimpleImageButton
    {
        QuestInfo myInfo;

        bool lastMouseOver;

        public Action<QuestInfo> OnMouseOver;
        public Action OnMouseLeave;
        
        public QuestButton(QuestInfo info, Vector2 pos, Vector2 size, bool finished) : base(pos, size, finished ? "buffassets/illustrations/correctSymbol" : "Sandbox_QuestionMark")
        {
            myInfo = info;
        }

        public override void Update()
        {
            base.Update();
            if(MouseOver && !lastMouseOver)
            {
                OnMouseOver?.Invoke(myInfo);
            }
            else if(!MouseOver && lastMouseOver)
            {
                OnMouseLeave?.Invoke();
            }
            lastMouseOver = MouseOver;
        }


        public struct QuestInfo
        {
            public string name;
            public int level;//只在有level condition时使用
            public bool finished;
            public Color color;
            public List<string> conditions;
            public Dictionary<QuestUnlockedType, List<string>> rewards;

            public override bool Equals(object obj)
            {
                if(obj is QuestInfo questInfo)
                    return Equals(questInfo.name, name);
                return false;
            }

            public override int GetHashCode()
            {
                return name.GetHashCode();
            }
        }
    }

    public class QuestInfoHoverBox : MenuObject
    {
        static float boundWidth = 2f;
        static float titleBorder = 20f;
        static float entryHeight = 20f;
        static float smallGap = 4f;
        static float bigGap = 10f;
        static float veryBigGap = 40f;

        static int maxCardInARow = 5;

        FSprite background;

        FSprite leftBound;
        FSprite upBound;
        FSprite rightBound;
        FSprite buttomBound;
        FSprite midBound;

        FLabel title;

        float maxConditionX, maxRewardX;
        List<FLabel> conditionLabels = new List<FLabel>();
        List<FLabel> rewardTypeLabels = new List<FLabel>();
        List<KeyValuePair<QuestUnlockedType, List<QuestLeaser>>> rewardLeasers = new List<KeyValuePair<QuestUnlockedType, List<QuestLeaser>>>();

        QuestRendererManager questRendererManager;

        float setAlpha;
        float alpha;
        float lastAlpha;

        Vector2 mouseScreenPos;
        Vector2 lastMouseScreenPos;

        Vector2 setAnchor = new Vector2(0f, 1f);
        Vector2 anchor = new Vector2(0f, 0f);//左上
        Vector2 lastAnchor = new Vector2(0f, 0f);

        Vector2 size = new Vector2(400f, 300f);

        bool shouldHideHover;
        QuestInfo? lastRequestedQuestInfo;

        QuestInfo currentInfo;

        public QuestInfoHoverBox(Menu.Menu menu, MenuObject owner) : base(menu, owner)
        {
            background = new FSprite("pixel")
            {
                color = Color.black,
                scaleX = size.x,
                scaleY = size.y,
            };
            Container.AddChild(background);

            leftBound = new FSprite("pixel") {scaleX = boundWidth , anchorX = 0f, anchorY = 1f};
            upBound = new FSprite("pixel") { scaleY = boundWidth, anchorX = 0f, anchorY = 1f};
            rightBound = new FSprite("pixel") { scaleX = boundWidth, anchorX = 1f, anchorY = 1f};
            buttomBound = new FSprite("pixel") { scaleY = boundWidth, anchorX = 0f, anchorY = 0f};
            midBound = new FSprite("pixel") { scaleX = boundWidth, anchorX = 0.5f, anchorY = 1f};

            Container.AddChild(leftBound);
            Container.AddChild(upBound);
            Container.AddChild(rightBound);
            Container.AddChild(buttomBound);
            Container.AddChild(midBound);

            title = new FLabel(Custom.GetDisplayFont(), "");

            Container.AddChild(title);
            questRendererManager = new QuestRendererManager(Container, QuestRendererManager.Mode.QuestDisplay);


            //foreach (KeyValuePair<string, FAtlasElement> item in Futile.atlasManager._allElementsByName)
            //{
            //    BuffPlugin.Log(item.Key);
            //}
        }

        public override void Update()
        {
            base.Update();
            lastAlpha = alpha;
            if(alpha != setAlpha)
            {
                alpha = Mathf.Lerp(alpha, setAlpha, 0.25f);
                if(Mathf.Approximately(alpha, setAlpha))
                    alpha = setAlpha;
            }

            lastAnchor = anchor;
            if (anchor != setAnchor)
            {
                anchor = Vector2.Lerp(anchor, setAnchor, 0.25f);
                if(Mathf.Approximately(anchor.x, setAnchor.x) && Mathf.Approximately(anchor.y , setAnchor.y))
                {
                    anchor = lastAnchor = setAnchor;
                }
            }
            questRendererManager.Update();

            if(shouldHideHover)
            {
                shouldHideHover = false;
                if (lastRequestedQuestInfo == null)
                    _InternalHide();
            }

            if(lastRequestedQuestInfo != null)
            {
                _InternalDisplayInfo(lastRequestedQuestInfo.Value);
                lastRequestedQuestInfo = null;
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            Vector2 ownerPos = Vector2.zero;
            if (owner is PositionedMenuObject p)
                ownerPos = Vector2.Lerp(p.lastPos, p.pos, timeStacker);

            Vector2 smoothPos = mouseScreenPos + ownerPos;
            Vector2 smoothAnchor = Vector2.Lerp(lastAnchor, anchor, timeStacker);

            background.SetPosition(smoothPos);
            background.alpha = alpha;


            leftBound.SetPosition(smoothPos + new Vector2((-smoothAnchor.x) * size.x , (1f - smoothAnchor.y) * size.y));
            leftBound.scaleY = size.y;
            leftBound.alpha = alpha;

            upBound.SetPosition(smoothPos + new Vector2((-smoothAnchor.x) * size.x, (1f - smoothAnchor.y) * size.y));
            upBound.scaleX = size.x;
            upBound.alpha = alpha;

            rightBound.SetPosition(smoothPos + new Vector2((1f - smoothAnchor.x) * size.x, (1f - smoothAnchor.y) * size.y));
            rightBound.scaleY = size.y;
            rightBound.alpha = alpha;

            buttomBound.SetPosition(smoothPos + new Vector2((-smoothAnchor.x) * size.x, (-smoothAnchor.y) * size.y));
            buttomBound.scaleX = size.x;
            buttomBound.alpha = alpha;

            Vector2 topAnchor = smoothPos + new Vector2((1f -smoothAnchor.x - smoothAnchor.x) * size.x / 2f, (1f - smoothAnchor.y) * size.y - titleBorder);
            title.SetPosition(topAnchor);
            title.alpha = alpha;

            midBound.SetPosition(topAnchor + new Vector2(0f, - veryBigGap));
            midBound.alpha = alpha;

            for (int i = 0;i < conditionLabels.Count;i++)//绘制条件
            {
                conditionLabels[i].SetPosition(topAnchor + new Vector2(-bigGap - maxConditionX, -bigGap - entryHeight - (entryHeight + bigGap) * i));
                conditionLabels[i].alpha = alpha;
            }

            float ybias = - entryHeight;

            for(int typeIndex = 0; typeIndex < rewardTypeLabels.Count;typeIndex++)
            {
                ybias -= bigGap;
                rewardTypeLabels[typeIndex].SetPosition(topAnchor + new Vector2(bigGap, ybias));
                rewardTypeLabels[typeIndex].alpha = alpha;
                var currentRewardLeasers = rewardLeasers[typeIndex];

                ybias -= entryHeight + bigGap;

                if(currentRewardLeasers.Key == QuestUnlockedType.Card)
                {
                    DrawBuffCardQuestRenderers(currentRewardLeasers.Value, topAnchor, alpha, ref ybias);
                }
                else
                {
                    for (int i = 0; i < currentRewardLeasers.Value.Count; i++)
                    {
                        var leaser = currentRewardLeasers.Value[i];

                        leaser.smoothCenterPos = topAnchor + new Vector2(bigGap + veryBigGap + leaser.rect.x / 2f, ybias - leaser.rect.y / 2f);
                        leaser.smoothAlpha = alpha;
                        ybias -= leaser.rect.y + smallGap;
                    }
                }
            }

            if (lastAnchor != anchor)
            {
                background.SetAnchor(smoothAnchor);
            }
            questRendererManager.GrafUpdate(timeStacker);
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            background.RemoveFromContainer();
            leftBound.RemoveFromContainer();
            upBound.RemoveFromContainer();
            rightBound.RemoveFromContainer();
            buttomBound.RemoveFromContainer();
            midBound.RemoveFromContainer();
            questRendererManager.Destroy();
        }


        void _InternalDisplayInfo(QuestInfo questInfo)
        {
            mouseScreenPos = Futile.mousePosition;
            setAlpha = 1.0f;
            if (currentInfo.Equals(questInfo))
                return;
            currentInfo = questInfo;

            title.text = questInfo.name;

            foreach (var label in conditionLabels)
                label.RemoveFromContainer();
            conditionLabels.Clear();

            foreach (var label in rewardTypeLabels)
                label.RemoveFromContainer();
            rewardTypeLabels.Clear();

            questRendererManager.Destroy();
            rewardLeasers.Clear();

            background.MoveToFront();
            leftBound.MoveToFront();
            upBound.MoveToFront();
            rightBound.MoveToFront();
            buttomBound.MoveToFront();
            midBound.MoveToFront();
            title.MoveToFront();

            int longest = questInfo.conditions.Count;
            float y = titleBorder + bigGap + entryHeight + longest * (entryHeight + bigGap) + bigGap;

            maxConditionX = float.MinValue;
            maxRewardX = float.MinValue;

            foreach (var condition in questInfo.conditions)//条件条目
            {
                float testWidth = LabelTest.GetWidth(condition, false);
                if (testWidth > maxConditionX)
                    maxConditionX = testWidth;
                conditionLabels.Add(new FLabel(Custom.GetFont(), condition) { anchorX = 0f, anchorY = 1f});
                Container.AddChild(conditionLabels.Last());
            }

            float y2 = titleBorder + bigGap + entryHeight + bigGap + entryHeight;
            foreach(var rewards in questInfo.rewards)//奖励渲染
            {
                y2 += entryHeight + bigGap * 2f;
                rewardLeasers.Add(new KeyValuePair<QuestUnlockedType, List<QuestLeaser>>(rewards.Key, new List<QuestLeaser>()));

                string rewardEntryTitle = rewards.Key.value;//类别标签
                maxRewardX = Mathf.Max(smallGap + LabelTest.GetWidth(rewardEntryTitle), maxRewardX);
                rewardTypeLabels.Add(new FLabel(Custom.GetFont(), rewards.Key.value) { anchorX = 0f, anchorY = 1f});
                Container.AddChild(rewardTypeLabels.Last());

                if (rewards.Key == QuestUnlockedType.Card)
                {
                    foreach (var reward in rewards.Value)
                    {
                        var leaser = questRendererManager.AddQuestToRender(rewards.Key, reward);
                        rewardLeasers.Last().Value.Add(leaser);
                    }

                    Vector2 rect = GetBuffCardQuestRendererSize(rewardLeasers.Last().Value);

                    BuffPlugin.Log($"GetBuffCardQuestRendererSize {rect.x} {rect.y}");
                    maxRewardX = Mathf.Max(rect.x + bigGap + veryBigGap, maxRewardX);
                    y2 += rect.y;
                }
                else
                {
                    foreach (var reward in rewards.Value)
                    {
                        var leaser = questRendererManager.AddQuestToRender(rewards.Key, reward);
                        rewardLeasers.Last().Value.Add(leaser);
                        y2 += leaser.rect.y + smallGap;

                        maxRewardX = Mathf.Max(leaser.rect.x + bigGap + veryBigGap, maxRewardX);
                    }
                }
            }
            y = Mathf.Max(y, y2);

            float max = Mathf.Max(maxConditionX, maxRewardX);
            maxConditionX = max;
            float x = bigGap + max + bigGap + bigGap + max + bigGap;
            size = new Vector2(x, y);
            background.scaleX = size.x;
            background.scaleY = size.y;

            midBound.scaleY = size.y - titleBorder - veryBigGap - bigGap;

            leftBound.color = questInfo.color;
            upBound.color = questInfo.color;
            rightBound.color = questInfo.color;
            buttomBound.color = questInfo.color;
            midBound.color = questInfo.color;

            setAnchor = new Vector2(0f, 1f);
            if(mouseScreenPos.y - size.y < 0f)
            {
                float delta = Mathf.Abs(mouseScreenPos.y - size.y) + bigGap;
                setAnchor.y = 1f - delta / size.y;
            }

            if (mouseScreenPos.x + size.x > Custom.rainWorld.options.ScreenSize.x)
            {
                float delta = mouseScreenPos.x + size.x - Custom.rainWorld.options.ScreenSize.x + bigGap;
                setAnchor.x = delta / size.x;
            }
        }

        Vector2 GetBuffCardQuestRendererSize(List<QuestLeaser> buffCardLeasers)
        {
            if (buffCardLeasers.Count < maxCardInARow)
                return new Vector2(buffCardLeasers.Count * (buffCardLeasers.First().rect.x + smallGap), buffCardLeasers.First().rect.y + smallGap);

            int rows = Mathf.Max(1, Mathf.CeilToInt(buffCardLeasers.Count / (float)maxCardInARow));
            return new Vector2(maxCardInARow * (buffCardLeasers.First().rect.x + smallGap), rows * (buffCardLeasers.First().rect.y + smallGap));
        }

        void DrawBuffCardQuestRenderers(List<QuestLeaser> buffCardLeasers, Vector2 topAnchor, float alpha, ref float yBias)
        {
            int x = 0;
            int y = 0;

            foreach(var leaser in buffCardLeasers)
            {
                leaser.smoothCenterPos = topAnchor + new Vector2(bigGap + veryBigGap + x * (leaser.rect.x + smallGap) + leaser.rect.x / 2f, yBias/* - y * (leaser.rect.y + smallGap)*/ - leaser.rect.y / 2f);
                leaser.smoothAlpha = alpha;

                x++;
                if(x == maxCardInARow)
                {
                    x = 0;
                    y++;
                    yBias -= leaser.rect.y + smallGap;
                }
            }
            if(x > 0)
                yBias -= buffCardLeasers.First().rect.y + smallGap;
        }

        public void DisplayInfo(QuestInfo questInfo)
        {
            lastRequestedQuestInfo = questInfo;
        }

        public void Hide()
        {
            shouldHideHover = true;
        }

        void _InternalHide()
        {
            setAlpha = 0f;
        }
    }
}
