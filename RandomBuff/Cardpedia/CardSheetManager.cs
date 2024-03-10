using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RandomBuff.Cardpedia.Elements;
using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.UI;

namespace RandomBuff.Cardpedia
{

    public class CardSheetManager
    {
        public bool inited;
        public FTexture displayingCard;

        public BuffType currentType;
        public CardpediaCardSheet[] pediaCardSheets;
        public CardpediaMenu owner;
        public Vector2 YRange;
        public Vector2 XRange;
        public Vector2 displayingPos = (new Vector2(203, 503));

        public TextBoxManager textBoxManager
        {
            get
            {
                return owner.textBoxManager;
            }
        }

        public CardSheetManager(CardpediaMenu menu, BuffType buffType)
        {
            owner = menu;
            pediaCardSheets = new CardpediaCardSheet[3];
            pediaCardSheets[0] = new CardpediaCardSheet(this, BuffType.Negative, -300f);
            pediaCardSheets[1] = new CardpediaCardSheet(this, BuffType.Positive, -300f);
            pediaCardSheets[2] = new CardpediaCardSheet(this, BuffType.Duality, -300f);
            this.currentType = buffType;
            YRange = new Vector2(120f, -300f);
            XRange = new Vector2(-300f, 300f);

            InitDisplayingCard();
            FillSheets();

            inited = true;
        }

        public void InitDisplayingCard()
        {
            string path = "buffassets/cardbacks/";
            string backname = currentType == BuffType.Negative ? "fpback" : (currentType == BuffType.Positive ? "moonback" : "slugback");
            displayingCard = new FTexture(Futile.atlasManager.LoadImage(path + backname).texture);
            displayingCard.scale = 0.35f * (600f/displayingCard.element.sourcePixelSize.x);
            displayingCard.alpha = 0f;
            displayingCard.SetPosition(displayingPos);
            displayingCard.RemoveFromContainer();
            owner.cursorContainer.AddChild(displayingCard);
        }

        public void FillSheets()
        {
            foreach (var id in ExtEnumBase.GetNames(typeof(Core.Buff.BuffID)))
            {
                BuffID buffID = new BuffID(id);
                try
                {
                    if (BuffConfigManager.Instance.StaticDataLoaded(buffID))
                    {
                        BuffStaticData buffData = BuffConfigManager.GetStaticData(buffID);
                        var card = new BuffCard(buffID, BuffCard.AnimatorState.CardpediaSlot_Scrolling);
                        card.Position = new Vector2(-500f, -500f);
                        card.Scale = 0.25f;
                        if (buffData.BuffType == BuffType.Negative)
                        {
                            pediaCardSheets[0].cards.Add(card);
                        }
                        else if (buffData.BuffType == BuffType.Positive)
                        {
                            pediaCardSheets[1].cards.Add(card);
                        }
                        else
                        {
                            pediaCardSheets[2].cards.Add(card);
                        }
                        owner.cursorContainer.AddChild(card.Container);
                    }
                    continue;
                }
                catch
                {
                    Debug.Log($"[Cardpedia] Failed to get static data, skipping the problematic buff:" + id);
                }
            }
        }

        public void Update()
        {
            if (!inited) return;
            for(int i = 0; i < 3; i++)
            {
                pediaCardSheets[i].Update();
            }
            
        }

        public void GrafUpdate(float timeStacker)
        {
            if (!inited) return;
            for (int i = 0; i < 3; i++)
            {
                bool flag = i == 0 && currentType == BuffType.Negative;
                bool flag2 = i == 1 && currentType == BuffType.Positive;
                bool flag3 = i == 2 && currentType == BuffType.Duality;
                if (flag || flag2 || flag3)
                {
                    float lastY = pediaCardSheets[i].sheetYOffset;
                    float targetY = Mathf.Lerp(YRange.y, YRange.x, owner.SetAlpha);
                    pediaCardSheets[i].sheetYOffset = Mathf.Lerp(lastY, targetY, timeStacker);
                }
                else
                {
                    pediaCardSheets[i].sheetYOffset = YRange.y;
                }

                pediaCardSheets[i].Draw(timeStacker);
            }

            
            displayingCard.alpha = owner.blurSprite.alpha;
        }
    }

}
