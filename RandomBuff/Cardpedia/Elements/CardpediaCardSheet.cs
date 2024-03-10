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

    public class CardpediaCardSheet
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
                        string life;
                        string stack;
                        string trigger;
                        string description;
                        string title;
                        var language = Custom.rainWorld.inGameTranslator.currentLanguage;

                        if (cards[i].ID.GetBuffData() != null)
                        {
                            var data = cards[i].ID.GetBuffData();
                            if (data is CountableBuffData)
                            {
                                life = (data as CountableBuffData).MaxCycleCount.ToString() + (language == InGameTranslator.LanguageID.Chinese? "循环" : " Cycles");
                            }
                            else life = language == InGameTranslator.LanguageID.Chinese? "永久" : "Everlasting";
                        }
                        else
                        {
                            life = language == InGameTranslator.LanguageID.Chinese ? "永久" : "Everlasting";
                        }

                        var staticData = cards[i].ID.GetStaticData();
                        if(language == InGameTranslator.LanguageID.Chinese)
                        {
                            stack = staticData.Stackable ? "可叠加" : "不可叠加";
                            trigger = staticData.Triggerable ? "主动点击触发" : "自动触发";
                        }
                        else
                        {
                            stack = staticData.Stackable ? "Stackable" : "Unstackable";
                            trigger = staticData.Triggerable ? "Manually" : "Automatically";
                        }
                        description = staticData.GetCardInfo(Custom.rainWorld.inGameTranslator.currentLanguage).info.Description;
                        title = staticData.GetCardInfo(Custom.rainWorld.inGameTranslator.currentLanguage).info.BuffName;

                        sheetManager.owner.textBoxManager.RefreshInformation(life, stack, trigger, description,title, cards[i].ID);                   
                    }
                }
                else
                {
                    cards[i].Highlight = false;
                }
            }
        }
    }

}
