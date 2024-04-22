﻿using Menu;
using Menu.Remix;
using Newtonsoft.Json.Linq;
using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.CardRender;
using RandomBuff.Render.UI;
using RWCustom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Cardpedia.PediaPage
{
    //由小风的代码改写
    internal class CardSheetPage : PositionedMenuObject
    {
        public CardpediaMenu cardpediaMenu;
        public FContainer sheetPageContainer;
        public MenuTabWrapper tabWrapper;

        //容器元素
        //TextBoxManager textBoxManager;
        CardpediaSlot cardpediaSlot;
        CardInfoDisplay infoDisplay;

        List<List<BuffID>> sheetIDPages = new();

        //页面元素
        FTexture displayingCard;

        //状态变量
        public bool Show
        {
            get => setAlpha == 1f;
            set => setAlpha = value ? 1f : 0f;
        }
        public float setAlpha;
        public float alpha;
        public float lastAlpha;

        public int pageIndex;

        public BuffType currentType = BuffType.Positive;

        public CardSheetPage(CardpediaMenu cardpediaMenu, MenuObject owner, Vector2 pos) : base(cardpediaMenu, owner, pos)
        {
            this.cardpediaMenu = cardpediaMenu;

            sheetPageContainer = new FContainer();
            owner.Container.AddChild(sheetPageContainer);

            InitSheet();
        }

        void InitSheet()
        {
            //textBoxManager = new TextBoxManager(sheetPageContainer);
            cardpediaSlot = new CardpediaSlot(cardpediaMenu);
            cardpediaSlot.AddListener(OnCardPick);
            sheetPageContainer.AddChild(cardpediaSlot.Container);

            tabWrapper = new MenuTabWrapper(cardpediaMenu, this);
            subObjects.Add(tabWrapper);

            infoDisplay = new CardInfoDisplay(this);

            tabWrapper.Container.MoveToFront();
            ResetCardTexture();
        }

        void ResetCardTexture(Texture texture = null)
        {
            displayingCard?.RemoveFromContainer();

            var tex = currentType == BuffType.Negative ? CardBasicAssets.FPBack : (currentType == BuffType.Positive ? CardBasicAssets.MoonBack : CardBasicAssets.SlugBack);
            if (texture != null)
                tex = texture;

            displayingCard = new FTexture(tex);
            sheetPageContainer.AddChild(displayingCard);
            displayingCard.SetPosition(CardpediaStatics.displayCardTexturePos);
            //BuffPlugin.Log($"tex name : {tex.name}, {tex == null}, {tex.texelSize}");
            displayingCard.scale = 0.35f * (600f / displayingCard.element.sourcePixelSize.x);
            displayingCard.alpha = alpha;
            displayingCard.MoveToFront();
        }

        public override void Update()
        {
            base.Update();
            //textBoxManager.Update();
            cardpediaSlot.Update();
            infoDisplay.Update();

            lastAlpha = alpha;
            if(setAlpha != alpha)
            {
                alpha = Custom.LerpAndTick(alpha, setAlpha, 0.25f, 1 / 40f);
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            //textBoxManager.Draw(timeStacker);
            cardpediaSlot.GrafUpdate(timeStacker);
            infoDisplay.GrafUpdate(timeStacker);

            cardpediaSlot.alpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
            displayingCard.alpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
            infoDisplay.alpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
        }

        public void RefreshSheet(BuffType buffType)
        {
            currentType = buffType;
            ResetCardTexture();

            //构建ID页面
            sheetIDPages.Clear();
            var allIDs = (BuffConfigManager.buffTypeTable[buffType]);
            sheetIDPages.Add(new List<BuffID>());
            for (int i = 0; i < allIDs.Count; i++)
            {
                var lst = sheetIDPages.Last();
                lst.Add(allIDs[i]);
                if (lst.Count >= CardpediaStatics.sheetNumPerPage)
                    sheetIDPages.Add(new List<BuffID>());
            }
            pageIndex = 0;
            SwitchPage(0);
            infoDisplay.SetText(null, null, null, null, null);
            //textBoxManager.currentType = buffType;
            //textBoxManager.titleBack.color = textBoxManager.currentType == BuffType.Negative ? CardpediaStatics.negativeColor : (textBoxManager.currentType == BuffType.Positive ? CardpediaStatics.positiveColor : CardpediaStatics.dualityColor);
            //textBoxManager.InitEmptyInfo();
        }

        public void SwitchPage(int addition)
        {
            pageIndex = Mathf.Clamp(pageIndex + addition, 0, sheetIDPages.Count - 1);

            cardpediaSlot.SwitchPage(sheetIDPages[pageIndex].ToArray());
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            //textBoxManager.Destroy();
            cardpediaSlot.Destory();
            sheetPageContainer.RemoveFromContainer();
        }

        public void OnCardPick(BuffCard card)
        {
            string life;
            string stack;
            string trigger;
            string description;
            string title;
            var language = Custom.rainWorld.inGameTranslator.currentLanguage;

            if (card.ID.GetBuffData() != null)
            {
                var data = card.ID.GetBuffData();
                if (data is CountableBuffData)
                {
                    life = (data as CountableBuffData).MaxCycleCount.ToString() + BuffResourceString.Get("CardSheetPage_Life_Countable");
                }
                else life = BuffResourceString.Get("CardSheetPage_Life_Uncountable");
            }
            else
            {
                life = BuffResourceString.Get("CardSheetPage_Life_Uncountable");
            }

            var staticData = card.StaticData;
            stack = staticData.Stackable ? BuffResourceString.Get("CardSheetPage_Stack_Stackable") : BuffResourceString.Get("CardSheetPage_Stack_Unstackable");
            trigger = staticData.Triggerable ? BuffResourceString.Get("CardSheetPage_Stack_Triggerable") : BuffResourceString.Get("CardSheetPage_Stack_Untriggerable");
            description = staticData.GetCardInfo(Custom.rainWorld.inGameTranslator.currentLanguage).info.Description;
            title = staticData.GetCardInfo(Custom.rainWorld.inGameTranslator.currentLanguage).info.BuffName;
            infoDisplay.SetText(life, trigger, stack, description, "None");
            ResetCardTexture(staticData.GetFaceTexture());
            //sheetManager.owner.configManager.OnCardPick(card);
            //textBoxManager.RefreshInformation(life, stack, trigger, description, title, card.ID);
            cardpediaMenu.configManager.OnCardPick(card);
        }
    }
}