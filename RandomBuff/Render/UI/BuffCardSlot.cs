using RandomBuff.Cardpedia;
using RandomBuff.Core.Buff;
using RandomBuff.Core.BuffMenu;
using RandomBuff.Core.Game;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.UI.BuffCondition;
using RandomBuff.Render.UI.Component;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RandomBuff.Render.UI
{
    internal class BuffCardSlot
    {
        public CardInteractionManager BaseInteractionManager { get; protected set; }
        public FContainer Container { get; protected set; }

        public List<BuffCard> BuffCards { get; protected set; }

        public HelpInfoProvider HelpInfoProvider { get; protected set; }

        public BuffSlotTitle Title { get; protected set; }

        public BuffCardSlot()
        {
            Container = new FContainer();
            BuffCards = new List<BuffCard>();
        }

        public virtual void Update()
        {
            BaseInteractionManager.Update();
            HelpInfoProvider?.Update();
        }

        public virtual void GrafUpdate(float timeStacker)
        {
            BaseInteractionManager.GrafUpdate(timeStacker);
            HelpInfoProvider?.GrafUpdate(timeStacker);
        }

        public virtual void AppendCard(BuffCard buffCard)
        {
            BaseInteractionManager?.ManageCard(buffCard);
            if(!BuffCards.Contains(buffCard))
                BuffCards.Add(buffCard);
            Container.AddChild(buffCard.Container);
        }

        public virtual BuffCard AppendCard(BuffID buffID)
        {
            var result = new BuffCard(buffID);
            AppendCard(result);
            return result;
        }

        /// <summary>
        /// 从卡槽中移除卡牌
        /// </summary>
        /// <param name="buffCard"></param>
        /// <param name="destroyAfterRemove">移除后是否直接销毁卡牌</param>
        public virtual void RemoveCard(BuffCard buffCard, bool destroyAfterRemove = false)
        {
            BaseInteractionManager.DismanageCard(buffCard);
            BuffCards.Remove(buffCard);
            Container.RemoveChild(buffCard.Container);
            if(destroyAfterRemove)
                buffCard.Destroy();
        }

        /// <summary>
        /// 从卡槽中移除卡牌
        /// </summary>
        /// <param name="buffID"></param>
        /// <param name="destroyAfterRemove">移除后是否直接销毁卡牌</param>
        public virtual void RemoveCard(BuffID buffID, bool destroyAfterRemove = false)
        {
            var cardToRemove = GetCard(buffID);

            if(cardToRemove != null)
                RemoveCard(cardToRemove, destroyAfterRemove);
            else
                BuffPlugin.LogWarning($"Buff {buffID} not exist in buffcardslot");
        }

        public virtual void RemoveCard(BuffID buffID)
        {
            var cardToRemove = GetCard(buffID);
            if(cardToRemove != null)
            {
                RemoveCard(cardToRemove);
            }
            else
            {
                BuffPlugin.LogWarning($"Buff {buffID} not exist in buffcardslot");
            }
        }

        public BuffCard GetCard(BuffID buffID)
        {
            foreach(var card in BuffCards)
            {
                if(card.ID == buffID) return card;
            }
            return null;
        }
    
        public virtual void BringToTop(BuffCard buffCard)
        {
            Container.RemoveChild(buffCard.Container);
            Container.AddChild(buffCard.Container);
        }

        public virtual void RecoverCardSort(BuffCard buffCard)
        {
            Container.RemoveChild(buffCard.Container);
            Container.AddChildAtIndex(buffCard.Container, BuffCards.IndexOf(buffCard));
        }

        public virtual void Destory()
        {
            BaseInteractionManager?.Destroy();
            Container.RemoveFromContainer();
        }
    }

    /// <summary>
    /// 基础的游戏内卡槽
    /// </summary>
    internal class BasicInGameBuffCardSlot : BuffCardSlot
    {
        public CommmmmmmmmmmmmmpleteInGameSlot completeSlot;

        FSprite darkMask_back;
        FSprite darkMaks_front;

        float targetBackDark;
        public bool BackDark
        {
            get => targetBackDark == 0.4f;
            set
            {
                targetBackDark = value ? 0.4f : 0f;
            }
        }

        float targetFrontDark;
        public bool FrontDark
        {
            get => targetFrontDark == 0.4f;
            set
            {
                targetFrontDark = value ? 0.4f : 0f;
            }
        }

        static int CardStartIndex = 1;//卡牌帮助信息+黑幕

        public BasicInGameBuffCardSlot(bool canTriggerBuff = false, CommmmmmmmmmmmmmpleteInGameSlot completeSlot = null)
        {
            BaseInteractionManager = new InGameSlotInteractionManager(this, canTriggerBuff);
            Container.AddChild(darkMask_back = new FSprite("pixel") { scaleX = Custom.rainWorld.screenSize.x, scaleY = Custom.rainWorld.screenSize.y, color = Color.black, alpha = 0f });
            Container.AddChild(darkMaks_front = new FSprite("pixel") { scaleX = Custom.rainWorld.screenSize.x, scaleY = Custom.rainWorld.screenSize.y, color = Color.black, alpha = 0f });
            darkMask_back.SetPosition(Custom.rainWorld.screenSize / 2f);
            darkMaks_front.SetPosition(Custom.rainWorld.screenSize / 2f);

            HelpInfoProvider = new HelpInfoProvider(this);
            foreach (var label in HelpInfoProvider.labels)
                label.MoveToFront();
            this.completeSlot = completeSlot;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (Mathf.Abs(targetBackDark - darkMask_back.alpha) > 0.01f)
                darkMask_back.alpha = Mathf.Lerp(darkMask_back.alpha, targetBackDark, 0.1f);

            if (Mathf.Abs(targetFrontDark - darkMaks_front.alpha) > 0.01f)
                darkMaks_front.alpha = Mathf.Lerp(darkMaks_front.alpha, targetFrontDark, 0.1f);
        }

        public override void Update()
        {
            base.Update();
        }

        //重写加卡方法，用来管理Container中的元素顺序
        public override void AppendCard(BuffCard buffCard)
        {
            BaseInteractionManager.ManageCard(buffCard);
            if(!BuffCards.Contains(buffCard))
                BuffCards.Add(buffCard);
            Container.AddChildAtIndex(buffCard.Container, CardStartIndex);
        }

        public override void BringToTop(BuffCard buffCard)
        {
            darkMaks_front.MoveToFront();
            foreach(var label in HelpInfoProvider.labels)
                label.MoveToFront();

            Container.RemoveChild(buffCard.Container);
            Container.AddChild(buffCard.Container);
        }

        public override void RecoverCardSort(BuffCard buffCard)
        {
            Container.RemoveChild(buffCard.Container);
            Container.AddChildAtIndex(buffCard.Container, BuffCards.IndexOf(buffCard) + CardStartIndex);
        }

        static BasicInGameBuffCardSlot()
        {
            HelpInfoProvider.CustomProviders += HelpInfoProvider_CustomProviders;
        }

        private static bool HelpInfoProvider_CustomProviders(HelpInfoProvider.HelpInfoID ID, out string helpInfo, params object[] Params)
        {
            helpInfo = "";
            if(ID == InGame_OnMouseFocus)
            {
                BuffID id = Params[0] as BuffID;
                helpInfo = BuffResourceString.Get("BasicInGameBuffCardSlot_OnMouseFocus");
                helpInfo = Regex.Replace(helpInfo, "<BuffID>", id.ToString());
                
                helpInfo += BuffResourceString.Get("BasicInGameBuffCardSlot_ExitHUD");
                helpInfo = Regex.Replace(helpInfo, "<HUD_KEY>", KeyCode.Tab.ToString());


                return true;
            }
            else if (ID == InGame_OnCardExclusiveShow)
            {
                BuffID id = Params[0] as BuffID;
                helpInfo = BuffResourceString.Get("BasicInGameBuffCardSlot_OnCardExclusiveShow");
                helpInfo = Regex.Replace(helpInfo, "<HUD_KEY>", KeyCode.Tab.ToString());

                if(BuffConfigManager.GetStaticData(id).Triggerable)
                {
                    helpInfo += BuffResourceString.Get("BasicInGameBuffCardSlot_BindKey");
                    helpInfo = Regex.Replace(helpInfo, "<KEYBINDER_KEY>", KeyCode.CapsLock.ToString());
                    helpInfo = Regex.Replace(helpInfo, "<BuffID>", id.ToString());
                }

                helpInfo += BuffResourceString.Get("BasicInGameBuffCardSlot_ExitHUD");
                helpInfo = Regex.Replace(helpInfo, "<HUD_KEY>", KeyCode.Tab.ToString());
                return true;
            }
            else if (ID == InGame_NoCardFocus)
            {
                helpInfo = BuffResourceString.Get("BasicInGameBuffCardSlot_NoCardFocus");
                helpInfo = Regex.Replace(helpInfo, "<HUD_KEY>", KeyCode.Tab.ToString());

                helpInfo += BuffResourceString.Get("BasicInGameBuffCardSlot_ExitHUD");
                helpInfo = Regex.Replace(helpInfo, "<HUD_KEY>", KeyCode.Tab.ToString());
                return true;
            }
            return false;
        }

        internal static HelpInfoProvider.HelpInfoID InGame_NoCardFocus = new HelpInfoProvider.HelpInfoID("InGame_NoCardFocus", true);
        internal static HelpInfoProvider.HelpInfoID InGame_OnMouseFocus = new HelpInfoProvider.HelpInfoID("InGame_OnMouseFocus", true);
        internal static HelpInfoProvider.HelpInfoID InGame_OnCardExclusiveShow = new HelpInfoProvider.HelpInfoID("InGame_OnCardExclusiveShow", true);
    }

    /// <summary>
    /// 选卡卡槽
    /// </summary>
    internal class CardPickerSlot : BuffCardSlot
    {
        public BasicInGameBuffCardSlot InGameBuffCardSlot { get; private set; }//可为null

        public BuffID[] majorSelections;
        public BuffID[] additionalSelections;

        Action<BuffID> selectCardCallBack;
        int numOfChoices;

        //状态变量
        int pickedCount;
        /// <summary>
        /// 是否完成所有抽卡并且卡牌动画均完成
        /// </summary>
        public bool AllFinished { get; private set; } = false;
        bool resumeTitle;

        /// <summary>
        /// 创建一次抽卡卡槽
        /// </summary>
        /// <param name="inGameBuffCardSlot">当前界面的基础游戏内卡槽，可以置空</param>
        /// <param name="selectCardCallBack">选择一张卡牌的回调，可能会触发多次</param>
        /// <param name="majorSelections">主抽卡选项</param>
        /// <param name="additionalSelections">附加抽卡选项，需要和主抽卡选项的长度一致</param>
        /// <param name="numOfChoices">完成本次抽卡需要抽取的卡牌数量</param>
        public CardPickerSlot(BasicInGameBuffCardSlot inGameBuffCardSlot, Action<BuffID> selectCardCallBack ,BuffID[] majorSelections, BuffID[] additionalSelections, int numOfChoices = 1, BuffSlotTitle slotTitle = null, bool resumeTitle = false)
        {
            this.majorSelections = majorSelections;
            this.additionalSelections = additionalSelections;
            this.Title = slotTitle;
            this.resumeTitle = resumeTitle;

            this.selectCardCallBack = selectCardCallBack;
            this.numOfChoices = numOfChoices;
            InGameBuffCardSlot = inGameBuffCardSlot;

            BaseInteractionManager = new CardPickerInteractionManager(this);
            if(InGameBuffCardSlot != null)
            {
                InGameBuffCardSlot.BaseInteractionManager.SubManager = BaseInteractionManager;
            }

            for(int i = 0;i < majorSelections.Length;i++)
            {
                var major = new BuffCard(majorSelections[i]);
                major.Position = new Vector2(2000, -100);
                (BaseInteractionManager as CardPickerInteractionManager).ManageMajorCard(major);

                if (additionalSelections[i] != null)
                {
                    var additional = new BuffCard(additionalSelections[i]);
                    additional.Position = new Vector2(2000, -100);

                    Container.AddChild(additional.Container);
                    (BaseInteractionManager as CardPickerInteractionManager).ManageAddtionalCard(additional, major);
                }

                Container.AddChild(major.Container);
            }
            (BaseInteractionManager as CardPickerInteractionManager).FinishManage();
            if(Title != null)
            {
                string title = BuffResourceString.Get("CardPickSlot_SlotTitle");
                title = Regex.Replace(title, "<Cards>", numOfChoices.ToString());
                title = Regex.Replace(title, "<Type>", BuffConfigManager.GetStaticData(majorSelections[0]).BuffType.ToString());
                Title.ChangeTitle(title, false);
            }  
        }

        public override void Update()
        {
            base.Update();
            if (!AllFinished && pickedCount == numOfChoices)
            {
                if(BuffCards.Count == 0)
                    AllFinished = true;
            }
        }

        public void CardPicked(BuffCard card)
        {
            List<BuffCard> cards = new List<BuffCard>();
            for (int i = 0; i < majorSelections.Length; i++)
            {
                if (additionalSelections[i] == card.ID)
                {
                    cards.Add(card);
                    cards.Add(GetCardOfID(majorSelections[i]));
                }

                if (majorSelections[i] == card.ID)
                {
                    cards.Add(card);
                    if (additionalSelections[i] != null)
                        cards.Add(GetCardOfID(additionalSelections[i]));
                }
            }

            foreach(var buffCard in cards)
            {
                selectCardCallBack.Invoke(buffCard.ID);
   
                if (InGameBuffCardSlot != null)
                {
                    BaseInteractionManager.DismanageCard(buffCard);
                    InGameBuffCardSlot.AppendCard(buffCard);
                }
                else
                {
                    BuffPlugin.Log($"Card picker remove card, {BuffCards.Count} remains");
                    buffCard.SetAnimatorState(BuffCard.AnimatorState.CardPicker_Disappear);
                }
            }

            pickedCount++;
            if(pickedCount == numOfChoices)
            {
                (BaseInteractionManager as CardPickerInteractionManager).FinishSelection();
                if(resumeTitle)
                    Title.ResumeLastTitle();
            }
        }

        BuffCard GetCardOfID(BuffID buffID)
        {
            foreach(var card in BuffCards)
            {
                if(card.ID == buffID)
                    return card;
            }
            return null;
        }
    }

    /// <summary>
    /// buff目录界面的静态展示卡槽
    /// </summary>
    internal class BuffGameMenuSlot : BuffCardSlot
    {
        public BuffGameMenu Menu { get; private set; }

        public List<List<BuffID>> buffIDPages = new List<List<BuffID>>();
        List<BuffCard> positiveCards = new List<BuffCard>();
        List<BuffCard> negativeCards = new List<BuffCard>();

        public Vector2 basePos;

        public BuffGameMenuSlot(BuffGameMenu buffGameMenu)
        {
            Menu = buffGameMenu;
            BaseInteractionManager = new DoNotingInteractionManager<BuffGameMenuSlot>(this);
        }

        /// <summary>
        /// 初始化存档中不同角色对应的卡牌
        /// </summary>
        /// <param name="nameOrders"></param>
        public void SetupBuffs(List<SlugcatStats.Name> nameOrders)
        {
            buffIDPages.Clear();
            foreach (var name in nameOrders)
            {
                buffIDPages.Add(BuffDataManager.Instance.GetAllBuffIds(name));
                BuffPlugin.Log($"{buffIDPages.Last().Count} buffs for {name}");
            }
        }

        public override void Update()
        {
            base.Update();
        }

        /// <summary>
        /// 更新当前页面
        /// </summary>
        /// <param name="page"></param>
        public void UpdatePage(int page)
        {
            foreach(var card in positiveCards)
            {
                card.SetAnimatorState(BuffCard.AnimatorState.BuffGameMenu_Disappear);
            }
            positiveCards.Clear();
            foreach(var card in negativeCards)
            {
                card.SetAnimatorState(BuffCard.AnimatorState.BuffGameMenu_Disappear);
            }
            negativeCards.Clear();
            foreach(var ids in buffIDPages[page])
            {
                CreateCard(ids);
                BuffPlugin.LogDebug($"Display buff card {ids} in page {page}");
            }

            foreach(var card in BuffCards)
            {
                if(card.currentAniamtorState != BuffCard.AnimatorState.BuffGameMenu_Disappear)
                    card.SetAnimatorState(BuffCard.AnimatorState.BuffGameMenu_Show);
            }
        }

        public float Scroll(float timeStacker)
        {
            return Mathf.Lerp(Mathf.Abs(Menu.lastScroll), Mathf.Abs(Menu.scroll), timeStacker);
        }

        /// <summary>
        /// 创建卡牌，并且根据其类型加入不同的列表管理
        /// </summary>
        /// <param name="id"></param>
        public void CreateCard(BuffID id)
        {
            var card = new BuffCard(id);

            if(card.StaticData.BuffType == BuffType.Positive)
                positiveCards.Add(card);
            else
                negativeCards.Add(card);

            AppendCard(card);
        }

        public int GetCurrentIndex(BuffCard card, out int positveOrNegative, out int totalLength)
        {
            if(card.StaticData.BuffType == BuffType.Positive)
            {
                positveOrNegative = 1;
                totalLength = positiveCards.Count;
                return positiveCards.IndexOf(card);
            }
            else
            {
                positveOrNegative = -1;
                totalLength = negativeCards.Count;
                return negativeCards.IndexOf(card);
            }
        }

        public void DestroyCard(BuffCard card)
        {
            if(card.StaticData.BuffType == BuffType.Positive)
                positiveCards.Remove(card);
            else
                negativeCards.Remove(card);

            RemoveCard(card, true);
        }
    }

    /// <summary>
    /// 完整的游戏内卡槽，包含入卡动画和抽卡界面
    /// </summary>
    internal class CommmmmmmmmmmmmmpleteInGameSlot : BuffCardSlot//名字是个意外.jpg
    {
        public BasicInGameBuffCardSlot BasicSlot { get; private set; }
        public ActivateCardAnimSlot ActiveAnimSlot { get; private set; }//加卡动画
        public TriggerBuffAnimSlot TriggerAnimSlot { get; private set; }
        public BuffTimerAnimSlot TimerAnimSlot { get; private set; }
        public CardPickerSlot ActivePicker { get; private set; }

        public BuffConditionHUD ConditionHUD { get; private set; }

        Queue<Action> pickerRequests = new();

        public CommmmmmmmmmmmmmpleteInGameSlot(BuffSlotTitle slotTitle = null)
        {
            Title = slotTitle;
            BuffCards = null;//不直接管理卡牌，所以设置为null来提前触发异常
            BaseInteractionManager = new DoNotingInteractionManager<CommmmmmmmmmmmmmpleteInGameSlot>(this);

            BasicSlot = new BasicInGameBuffCardSlot(true, this);
            ActiveAnimSlot = new ActivateCardAnimSlot(this);
            TriggerAnimSlot = new TriggerBuffAnimSlot(this);
            TimerAnimSlot = new BuffTimerAnimSlot(this);

            ConditionHUD = new BuffConditionHUD();
            Container.AddChild(BasicSlot.Container);
            Container.AddChild(ActiveAnimSlot.Container);
            Container.AddChild(TriggerAnimSlot.Container);
            Container.AddChild(TimerAnimSlot.Container);
            
            Container.AddChild(ConditionHUD.Container);
        }

        public override void Update()
        {
            base.Update();
            BasicSlot.Update();
            ActiveAnimSlot.Update();
            TriggerAnimSlot.Update();
            TimerAnimSlot.Update();
            ConditionHUD.Update();
            if(ActivePicker != null)
            {
                ActivePicker.Update();
                if (ActivePicker.AllFinished)
                {
                    BuffPlugin.Log("CompleteInGameSlot destroy active picker");
                    ActivePicker.Destory();
                    ActivePicker = null;

                    if(pickerRequests.Count == 0)
                        Title.ResumeLastTitle();
                }
            }
            else
            {
                if(pickerRequests.Count > 0)
                {
                    pickerRequests.Dequeue().Invoke();
                    BuffPlugin.Log("Dequeue new pick");
                }
            }
        }

        public override void AppendCard(BuffCard buffCard)
        {
            ActiveAnimSlot.AppendCard(buffCard);
            TimerAnimSlot.TryAddTimer(buffCard.ID);
        }

        /// <summary>
        /// 不经过加卡动画直接加入卡槽
        /// </summary>
        /// <param name="buffID"></param>
        /// <returns></returns>
        public BuffCard AppendCardDirectly(BuffID buffID)
        {
            var card = new BuffCard(buffID);
            AppendCardDirectly(card);
            return card;
        }

        /// <summary>
        /// 不经过加卡动画直接加入卡槽
        /// </summary>
        /// <param name="buffCard"></param>
        public void AppendCardDirectly(BuffCard buffCard)
        {
            BasicSlot.AppendCard(buffCard);
            TimerAnimSlot.TryAddTimer(buffCard.ID);
        }

        static string path = Application.streamingAssetsPath + Path.DirectorySeparatorChar + "DebugBuffID.txt";
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            BasicSlot.GrafUpdate(timeStacker);
            ActiveAnimSlot.GrafUpdate(timeStacker);
            TriggerAnimSlot.GrafUpdate(timeStacker);
            ActivePicker?.GrafUpdate(timeStacker);
            TimerAnimSlot.GrafUpdate(timeStacker);
            ConditionHUD.DrawSprites(timeStacker);

            if (Input.GetKeyDown(KeyCode.C) && File.Exists(path))
            {
                //BuffPoolManager.Instance.CreateBuff(new BuffID("DeathFreeMedallion"));
                //AppendCard(new BuffID("DeathFreeMedallion"));

                string[] lines = File.ReadAllLines(path);
                foreach(var line in lines)
                {
                    var id = new BuffID(line);
                    BuffPoolManager.Instance.CreateBuff(id);
                    AppendCard(id);
                    BuffHud.Instance.HandleAddBuffHUD(id);
                }
            }
        }

        /// <summary>
        /// 申请抽卡
        /// 完成一个抽卡之后才会进行下一个
        /// </summary>
        /// <param name="selectCardCallBack">完成抽卡后的回调</param>
        /// <param name="majorSelections">主抽卡选项</param>
        /// <param name="additionalSelections">附加抽卡选项</param>
        /// <param name="numOfChoices">完成抽卡所需的选择数</param>
        public void RequestPickCards(Action<BuffID> selectCardCallBack, BuffID[] majorSelections, BuffID[] additionalSelections, int numOfChoices = 1)
        {
            pickerRequests.Enqueue(() => 
            { 
                ActivePicker = new CardPickerSlot(BasicSlot, selectCardCallBack, majorSelections, additionalSelections, numOfChoices, Title);
                Container.AddChild(ActivePicker.Container);
                ActivePicker.Container.MoveBehindOtherNode(BasicSlot.Container);
            });
            BuffPlugin.Log("Request new pick");
        }

        /// <summary>
        /// 申请抽卡，利用委托延迟创建抽取的卡牌
        /// 完成一个抽卡之后才会进行下一个
        /// </summary>
        /// <param name="selectCardCallBack">完成抽卡后的回调</param>
        /// <param name="createMajorSelections">创建主抽卡选项的委托</param>
        /// <param name="createAdditionalSelections">创建附加抽卡选项的委托，返回数组长度需要和主抽卡选项一致</param>
        /// <param name="numOfChoices">完成抽卡所需的选择数</param>
        /// <exception cref="ArgumentException">附加抽卡选项与主抽卡选项不一致时会抛出此异常</exception>
        public void RequestPickCards(Action<BuffID> selectCardCallBack, Func<BuffID[]> createMajorSelections, Func<BuffID[]> createAdditionalSelections, int numOfChoices = 1)
        {
            pickerRequests.Enqueue(() =>
            {
                BuffID[] majorSelections = createMajorSelections.Invoke();
                BuffID[] additionalSelections = createAdditionalSelections.Invoke();

                if(majorSelections.Length != additionalSelections.Length)
                {
                    throw new ArgumentException("Major selections length and additional selections length not match");
                }

                ActivePicker = new CardPickerSlot(BasicSlot, selectCardCallBack, majorSelections, additionalSelections, numOfChoices);
                Container.AddChild(ActivePicker.Container);
                ActivePicker.Container.MoveBehindOtherNode(BasicSlot.Container);
            });
            BuffPlugin.Log("Request new pick with custom selection creator");
        }

        /// <summary>
        /// 触发卡牌，播放动画
        /// </summary>
        /// <param name="buffID"></param>
        public void TriggerBuff(BuffID buffID)
        {
            TriggerAnimSlot.TriggerBuff(buffID);
        }

        public override void Destory()
        {
            BasicSlot.Destory();
            ActivePicker?.Destory();
            ActiveAnimSlot.Destory();
            TriggerAnimSlot.Destory();
            TimerAnimSlot.Destory();
            ConditionHUD.Destroy();
            base.Destory();
        }

        /// <summary>
        /// 用来实现加卡动画的卡槽
        /// </summary>
        internal class ActivateCardAnimSlot : BuffCardSlot
        {
            CommmmmmmmmmmmmmpleteInGameSlot completeSlot;
            public ActivateCardAnimSlot(CommmmmmmmmmmmmmpleteInGameSlot completeSlot)
            {
                this.completeSlot = completeSlot;
                BaseInteractionManager = new DoNotingInteractionManager<ActivateCardAnimSlot>(this);
            }

            public override void AppendCard(BuffCard buffCard)
            {
                base.AppendCard(buffCard);
                buffCard.SetAnimatorState(BuffCard.AnimatorState.ActivateCardAnimSlot_Append);
            }

            public void FinishAnimation(BuffCard card)
            {
                RemoveCard(card);
                completeSlot.BasicSlot.AppendCard(card);
            }
        }
    
        internal class TriggerBuffAnimSlot : BuffCardSlot
        {
            public static Vector2 hoverPos = new Vector2(40f, Custom.rainWorld.screenSize.y - 60f);
            static int CardStartIndex = 1;
            static float flashScale = 40f;
            CommmmmmmmmmmmmmpleteInGameSlot completeSlot;

            float flash;
            FSprite flatLight;

            public TriggerBuffAnimSlot(CommmmmmmmmmmmmmpleteInGameSlot completeSlot)
            {
                this.completeSlot = completeSlot;
                BaseInteractionManager = new DoNotingInteractionManager<TriggerBuffAnimSlot>(this);
                Container.AddChild(flatLight = new FSprite("Futile_White")
                {
                    x = hoverPos.x,
                    y = hoverPos.y,
                    scale = 0f,
                    alpha = 0f,
                    shader = Custom.rainWorld.Shaders["FlatLight"],
                    isVisible = false
                });
            }

            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if(flash > 0f)
                {
                    flash = Mathf.Lerp(flash, 0f, timeStacker);
                    if (flash < 0.01f)
                    {
                        flash = 0f;
                        flatLight.isVisible = false;
                    }

                    flatLight.alpha = flash;
                    flatLight.scale = flash * flashScale;
                    flatLight.isVisible = true;
                }
            }

            public void TriggerBuff(BuffID buffID)
            {
                var card = AppendCard(buffID);
                card.SetAnimatorState(BuffCard.AnimatorState.TriggerBuffAnimSlot_Trigger);
                flatLight.color = Color.Lerp(card.StaticData.Color, Color.white, 0.4f);
                flash = 1f;
                flatLight.alpha = flash;
                flatLight.scale = flash * flashScale;
                flatLight.isVisible = true;
            }

            public override void AppendCard(BuffCard buffCard)
            {
                if (!BuffCards.Contains(buffCard))
                    BuffCards.Insert(0, buffCard);
                BaseInteractionManager?.ManageCard(buffCard);
                flatLight.MoveToFront();
                Container.AddChild(buffCard.Container);
            }

            public override void Destory()
            {
                flatLight.RemoveFromContainer();
                flatLight = null;
                base.Destory();
            }
        }
        
        internal class BuffTimerAnimSlot : BuffCardSlot
        {
            CommmmmmmmmmmmmmpleteInGameSlot completeSlot;

            FContainer timerContainer;

            List<TimerInstance> activeTimerInstances = new();
            public Dictionary<BuffCard, TimerInstance> buffCard2TimerInstanceMapper = new();
            Dictionary<TimerInstance, BuffCard> timerInstance2BuffCardMapper = new();

            public BuffTimerAnimSlot(CommmmmmmmmmmmmmpleteInGameSlot completeSlot)
            {
                this.completeSlot = completeSlot;
                BaseInteractionManager = new DoNotingInteractionManager<BuffTimerAnimSlot>(this);

                timerContainer = new FContainer();
                Container.AddChild(timerContainer);
            }

            public void TryAddTimer(BuffID id)
            {
                if(BuffPoolManager.Instance.TryGetBuff(id, out var ibuff))
                {
                    foreach(var timerInstance in activeTimerInstances)
                    {
                        if (timerInstance.id == id)
                        {
                            BuffPlugin.LogWarning($"timer of {id} already added");
                            return;
                        }
                    }

                    if(ibuff.MyTimer != null)
                    {
                        activeTimerInstances.Add(new TimerInstance(this, id, ibuff));
                        BuffPlugin.Log($"Create timer instance for {id}");
                    }
                }
            }

            public override void AppendCard(BuffCard buffCard)
            {
                base.AppendCard(buffCard);
                buffCard.SetAnimatorState(BuffCard.AnimatorState.BuffTimerAnimSlot_Show);
                BuffPlugin.Log($"BuffTimerAnimSlot append card of {buffCard.ID}");
            }

            public override void Update()
            {
                base.Update();
                for(int i = activeTimerInstances.Count - 1; i >= 0; i--)
                {
                    activeTimerInstances[i].Update();
                }
            }

            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                for (int i = activeTimerInstances.Count - 1; i >= 0; i--)
                {
                    activeTimerInstances[i].GrafUpdate(timeStacker);
                }
            }

            public override void Destory()
            {
                base.Destory();
                foreach(var timerInstance in activeTimerInstances)
                    timerInstance.Destroy();

                activeTimerInstances.Clear();
                buffCard2TimerInstanceMapper.Clear();
                timerInstance2BuffCardMapper.Clear();
            }

            internal class TimerInstance : BuffCardTimer.IOwnBuffTimer
            {
                //public BuffTimer timer;
                public BuffID id;
                WeakReference<IBuff> buffRef;

                BuffTimerAnimSlot slot;

                BuffCardTimer cardTimer;
                //BuffCountDisplay countDisplay;
                

                public int Second { get; private set; }
                internal bool Show { get; private set; }
                internal bool ActuallyShow => counter > 0 || Show;

                bool destroyAfterHide;

                public float ShowTimerFactor => counter / 40f;
                public float LastShowTimerFactor => lastCounter / 40f;

                int lastCounter;
                int counter;

                int index;

                public Vector2 lastPos;
                public Vector2 pos;
                Vector2 TargetPos => new Vector2(Custom.rainWorld.screenSize.x - 80, Custom.rainWorld.screenSize.y - 80 - index * 80);

                public TimerInstance(BuffTimerAnimSlot slot, BuffID id, IBuff buff)
                {
                    this.id = id;
                    buffRef = new WeakReference<IBuff>(buff);
                    this.slot = slot;

                    if (buff.MyTimer.DisplayStrategy == null)
                        throw new ArgumentException($"displayStrategy for {id} cant be null");
                }

                public void Update()
                {
                    if(buffRef.TryGetTarget(out var buff))
                    {
                        if (buff.MyTimer.DisplayStrategy == null)
                            throw new ArgumentException($"displayStrategy for {id} cant be null");

                        Second = buff.MyTimer.Second;
                        Show = buff.MyTimer.DisplayStrategy.DisplayThisFrame;
                    }
                    else
                    {
                        Show = false;
                        destroyAfterHide = true;
                    }

                    cardTimer?.Update();
                    lastCounter = counter;

                    lastPos = pos;
                    if (Mathf.Abs(pos.x - TargetPos.x) > 0.01f || Mathf.Abs(pos.y - TargetPos.y) > 0.01f)
                        pos = Vector2.Lerp(pos, TargetPos, 0.2f);

                    if (Show)
                    {
                        if (counter < 40)
                            counter++;

                        if(cardTimer == null)
                        {
                            cardTimer = new BuffCardTimer(slot.timerContainer, this);
                            cardTimer.HardSetNumber();
                            slot.buffCard2TimerInstanceMapper.Add(slot.AppendCard(id), this);
                        }

                        UpdateIndexAndTimer();
                    }
                    else
                    {
                        if (counter > 0)
                        {
                            counter--;
                            UpdateIndexAndTimer();
                        }
                        else if (counter == 0)
                        {
                            if (cardTimer != null)
                            {
                                cardTimer.ClearSprites();
                                cardTimer = null;
                                slot.RemoveCard(id, true);
                            }
                            counter--;
                            if(destroyAfterHide)
                            {
                                Destroy();
                                slot.activeTimerInstances.Remove(this);
                            }    
                        }
                    }
                }

                void UpdateIndexAndTimer()
                {
                    int newIndex = 0;
                    foreach (var timerInstance in slot.activeTimerInstances)
                    {
                        if (timerInstance == this)
                            break;
                        if (timerInstance.ActuallyShow)
                            newIndex++;
                    }
                    index = newIndex;

                    cardTimer.pos = pos + Vector2.right * 60f + Vector2.left * Helper.LerpEase(ShowTimerFactor) * 100f;
                    cardTimer.alpha = ShowTimerFactor;
                }

                public void GrafUpdate(float timeStacker)
                {
                    cardTimer?.DrawSprites(timeStacker);
                }

                public void Destroy()
                {
                    cardTimer?.ClearSprites();
                    slot.RemoveCard(id, true);
                }
            }
        }
    }

    /// <summary>
    /// 卡牌收藏的卡槽
    /// </summary>
    internal class CardpediaSlot : BuffCardSlot
    {
        public CardpediaMenu cardpediaMenu;
        public float alpha;

        public CardpediaSlot(CardpediaMenu cardpediaMenu)
        {
            this.cardpediaMenu = cardpediaMenu;
            BaseInteractionManager = new ClickSignalInteractionManager<CardpediaSlot>(this);
        }

        public void SwitchPage(params BuffID[] newPageIDs)
        {
            for(int i = BuffCards.Count - 1; i >= 0; i--)
            {
                RemoveCard(BuffCards[i], true);
            }
            foreach(var id in newPageIDs)
            {
                var card = AppendCard(id);
                card.SetAnimatorState(BuffCard.AnimatorState.CardpediaSlot_StaticShow);
            }
        }

        public void AddListener(Action<BuffCard> mouseEvent)
        {
            (BaseInteractionManager as ClickSignalInteractionManager<CardpediaSlot>).OnBuffCardSingleClick += mouseEvent; 
        }
    }
}
