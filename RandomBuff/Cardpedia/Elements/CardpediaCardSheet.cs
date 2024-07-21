using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;

namespace RandomBuff.Cardpedia.Elements
{
    internal class CardpediaCardSheet
    {
        public BuffType sheetBuffType;
        public int lastDisplayCardIndex;
        public bool hasCardDisplaying;
        public float switchingProgress;

        public float sheetYOffset;
        public int sheetPage;
        public int numPerPage = 8;
        public int maxPage;
        public float flipCounter;
        public float switchCount;

        List<List<BuffID>> sheetIDs = new List<List<BuffID>>();
        internal List<BuffCard> cards;
        public List<float> origXOffsets;

        public CardSheetManager sheetManager;

        public CardpediaCardSheet(CardSheetManager sheetManager, BuffType buffType, float origY)
        {
            this.sheetManager = sheetManager;
            cards = new List<BuffCard>();
            origXOffsets = new List<float>();
            sheetYOffset = origY;
            sheetBuffType = buffType;
            sheetPage = 0;

            var allIDs = (BuffConfigManager.buffTypeTable[buffType]);

            sheetIDs.Add(new List<BuffID>());
            for(int i = 0;i < allIDs.Count; i++)
            {
                var lst = sheetIDs.Last();
                lst.Add(allIDs[i]);
                if (lst.Count >= numPerPage)
                    sheetIDs.Add(new List<BuffID>());
            }
        }

        public void Update()
        {
            if (!sheetManager.inited) return;
            if (sheetManager.currentType != sheetBuffType) return;
            if (maxPage == 0) maxPage = (int)Mathf.Ceil((float)cards.Count / numPerPage);

            if (flipCounter > 0) flipCounter -= 0.05f;
            if (Input.GetKey(KeyCode.RightArrow))
            {
                if (sheetPage + 1 < maxPage && flipCounter < 0.05f)
                {
                    sheetPage++;
                    flipCounter = 1f;
                    //FlipPage();
                }
            }
            else if (Input.GetKey(KeyCode.LeftArrow) && flipCounter < 0.05f)
            {
                if (sheetPage > 0)
                {
                    sheetPage--;
                    flipCounter = 1f;
                    //FlipPage();
                }
            }

            if (switchCount < 1) switchCount += 0.0625f;
            switchingProgress = Mathf.Sin(0.5f * Mathf.PI * switchCount);

            Vector2 vec;
            float num;
            for (int i = 0; i < cards.Count; i++)
            {
                vec = new Vector2(350f + (300f * cards[i].Scale + 20f) * (i % numPerPage), sheetYOffset);
                num = 1700f * (i / numPerPage - sheetPage);
                vec.x += num;

                if (vec.x > 1376f) vec.x = 1450f;
                if (vec.x < 0f) vec.x = -100f;

                cards[i].Position = vec;                
            }

        }

        public void FlipPage()
        {
            Vector2 vec;
            float num;
            for (int i = 0; i < cards.Count; i++)
            {
                vec = new Vector2(350f + (300f * cards[i].Scale + 20f) * (i % numPerPage), sheetYOffset);
                num = 1700f * (i / numPerPage - sheetPage);
                vec.x += num;

                if (vec.x > 1376f) vec.x = 1450f;
                if (vec.x < 0f) vec.x = -100f;                

                cards[i].Position = vec;
                (cards[i].currentAnimator as CardpediaSlotScrollingAnimator).offDisplayPos = vec;
            }

        }

        public void Draw(float timeStacker)
        {
            if (!sheetManager.inited) return;
            if (sheetManager.currentType != sheetBuffType) return;
           
            for (int i = numPerPage * sheetPage; i < numPerPage * (sheetPage + 1); i++)
            {
                if (i >= cards.Count) continue;
                if (cards[i].currentAnimator == null || !(cards[i].currentAnimator is CardpediaSlotScrollingAnimator)) continue;
                if ((cards[i].currentAnimator as CardpediaSlotScrollingAnimator).MouseOver)
                {
                    cards[i].Highlight = true;

                    if (sheetManager.owner.mouseDown)
                    {
                        OnCardPick(cards[i]);
                    }
                }
                else
                {
                    cards[i].Highlight = false;
                }
            }
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
                    life = (data as CountableBuffData).MaxCycleCount.ToString() + BuffResourceString.Get("Cycles",true);
                }
                else life = BuffResourceString.Get("Everlasting", true);
            }
            else
            {
                life = BuffResourceString.Get("Everlasting", true);
            }

            var staticData = card.StaticData;
            stack = BuffResourceString.Get(staticData.Stackable ? "Stackable" : "Unstackable",true);
            trigger = BuffResourceString.Get(staticData.Triggerable ? "CardpediaCardSheet_Manually" : "CardpediaCardSheet_Automatically");
            
            description = staticData.GetCardInfo(Custom.rainWorld.inGameTranslator.currentLanguage).info.Description;
            title = staticData.GetCardInfo(Custom.rainWorld.inGameTranslator.currentLanguage).info.BuffName;

            sheetManager.owner.configManager.OnCardPick(card);
            //sheetManager.owner.textBoxManager.RefreshInformation(life, stack, trigger, description, title, card.ID);

        }
    }
}
