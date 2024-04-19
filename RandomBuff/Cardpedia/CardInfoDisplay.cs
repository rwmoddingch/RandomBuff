using Menu.Remix;
using Menu.Remix.MixedUI;
using RandomBuff.Cardpedia.Elements.Config;
using RandomBuff.Cardpedia.PediaPage;
using RandomBuff.Core.BuffMenu;
using RandomBuff.Render.CardRender;
using RandomBuff.Render.UI;
using RandomBuff.Render.UI.Component;
using RWCustom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Cardpedia
{
    internal class CardInfoDisplay
    {
        readonly CardSheetPage sheetPage;

        //容器变量
        OpScrollBox scrollBox;
        FNodeWrapper nodeWrapper;
        OpFNodeWrapper opNodeWrapper;

        //元素
        FSprite blurSprite;

        TMProFLabel typeTitleLabel;
        TMProFLabel triggleableTitleLabel;
        TMProFLabel stackableTitleLabel;
        TMProFLabel descriptionTitleLabel;
        TMProFLabel conflictTitleLabel;

        TMProFLabel typeInfoLabel;
        TMProFLabel triggleableInfoLabel;
        TMProFLabel stackableInfoLabel;
        TMProFLabel descriptionInfoLabel;
        TMProFLabel conflictInfoLabel;

        CosmeticRectInstance typeRect;
        CosmeticRectInstance triggleableRect;
        CosmeticRectInstance stackableRect;
        CosmeticRectInstance descriptionRect;
        CosmeticRectInstance conflictRect;

        //状态变量
        public float alpha;
        int contentSizeDirty;

        public CardInfoDisplay(CardSheetPage sheetPage)
        {
            this.sheetPage = sheetPage;

            InitInfoDisplay();
        }

        public void InitInfoDisplay()
        {
            nodeWrapper = new FNodeWrapper(sheetPage.menu, sheetPage);
            sheetPage.subObjects.Add(nodeWrapper);

            blurSprite = new FSprite("pixel") { shader = Custom.rainWorld.Shaders["UIBlur"] };
            blurSprite.scaleX = CardpediaStatics.infoDisplayWindowScale.x;
            blurSprite.scaleY = CardpediaStatics.infoDisplayWindowScale.y;
            blurSprite.anchorX = 0f;
            blurSprite.anchorY = 0f;

            nodeWrapper.WrapNode(blurSprite, CardpediaStatics.infoDisplayWindowPos);

            float contentSize = 2000f;
            scrollBox = new OpScrollBox(CardpediaStatics.infoDisplayWindowPos /*- CardpediaStatics.infoDisplayWindowScale / 2f*/, CardpediaStatics.infoDisplayWindowScale, contentSize, false, false, false);
            new UIelementWrapper(sheetPage.tabWrapper, scrollBox);

            opNodeWrapper = new OpFNodeWrapper(Vector2.zero, Vector2.one);
            scrollBox.AddItems(opNodeWrapper);

            int totalTitleCount = 3;
            Vector2 titleRectScale = new Vector2((CardpediaStatics.infoDisplayWindowScale.x - CardpediaStatics.tinyGap * (totalTitleCount + 1)) / totalTitleCount, CardpediaStatics.infoDisplay_BigRectHeight);

            typeTitleLabel = CreateLabel(BuffResourceString.Get("CardInfoDisplay_TypeTitle") + "懑", titleRectScale);
            Vector2 pos = new Vector2(CardpediaStatics.tinyGap, contentSize - CardpediaStatics.tinyGap);
            typeRect = CreateAndWrapCosmeticRect(pos, titleRectScale);
            typeInfoLabel = CreateInfoLabel("???", titleRectScale, 0.8f);
            opNodeWrapper.WrapNode(typeTitleLabel, pos);
            opNodeWrapper.WrapNode(typeInfoLabel, pos + new Vector2(titleRectScale.x / 2f, -CardpediaStatics.tinyGap * 2 - CardpediaStatics.cosmeticRectHeight));


            triggleableTitleLabel = CreateLabel(BuffResourceString.Get("CardInfoDisplay_TriggerTitle"), titleRectScale);
            pos = new Vector2(CardpediaStatics.tinyGap * 2 + titleRectScale.x, contentSize - CardpediaStatics.tinyGap);
            triggleableRect = CreateAndWrapCosmeticRect(pos, titleRectScale);
            triggleableInfoLabel = CreateInfoLabel(BuffResourceString.Get("CardInfoDisplay_Missing"), titleRectScale, 0.8f);
            opNodeWrapper.WrapNode(triggleableTitleLabel, pos);
            opNodeWrapper.WrapNode(triggleableInfoLabel, pos + new Vector2(titleRectScale.x / 2f, -CardpediaStatics.tinyGap * 2 - CardpediaStatics.cosmeticRectHeight));

            stackableTitleLabel = CreateLabel(BuffResourceString.Get("CardInfoDisplay_StackTitle"), titleRectScale);
            pos = new Vector2(CardpediaStatics.tinyGap * 3 + titleRectScale.x * 2, contentSize - CardpediaStatics.tinyGap);
            stackableRect = CreateAndWrapCosmeticRect(pos, titleRectScale);
            stackableInfoLabel = CreateInfoLabel(BuffResourceString.Get("CardInfoDisplay_Missing"), titleRectScale, 0.8f);
            opNodeWrapper.WrapNode(stackableTitleLabel, pos);
            opNodeWrapper.WrapNode(stackableInfoLabel, pos + new Vector2(titleRectScale.x / 2f, -CardpediaStatics.tinyGap * 2 - CardpediaStatics.cosmeticRectHeight));

            Vector2 fullRectScale = new Vector2(CardpediaStatics.infoDisplayWindowScale.x - CardpediaStatics.tinyGap * 2, 200f);
            pos = new Vector2(CardpediaStatics.tinyGap, contentSize - (CardpediaStatics.tinyGap + titleRectScale.y + CardpediaStatics.tinyGap));
            descriptionTitleLabel = CreateLabel(BuffResourceString.Get("CardInfoDisplay_DescriptionTitle"), fullRectScale);
            descriptionRect = CreateAndWrapCosmeticRect(pos, fullRectScale);
            descriptionInfoLabel = CreateLabel(BuffResourceString.Get("CardInfoDisplay_MissingDescription"), fullRectScale, 0.8f);
            opNodeWrapper.WrapNode(descriptionTitleLabel, pos);
            opNodeWrapper.WrapNode(descriptionInfoLabel, pos + new Vector2(CardpediaStatics.tinyGap, -CardpediaStatics.tinyGap * 3 - CardpediaStatics.cosmeticRectHeight));

            conflictTitleLabel = CreateLabel(BuffResourceString.Get("CardInfoDisplay_ConflictTitle"), fullRectScale);
            pos -= new Vector2(0f, fullRectScale.y + CardpediaStatics.tinyGap);
            conflictRect = CreateAndWrapCosmeticRect(pos, fullRectScale);
            conflictInfoLabel = CreateLabel(BuffResourceString.Get("CardInfoDisplay_MissingConflict"), fullRectScale, 0.8f);
            opNodeWrapper.WrapNode(conflictTitleLabel, pos);
            opNodeWrapper.WrapNode(conflictInfoLabel, pos + new Vector2(CardpediaStatics.tinyGap, -CardpediaStatics.tinyGap * 3 - CardpediaStatics.cosmeticRectHeight));

            scrollBox.ScrollToTop();
            contentSizeDirty = 2;

            TMProFLabel CreateLabel(string text, Vector2 size, float fontSize = 1f)
            {
                //TopLeftLabel
                var result = new TMProFLabel(CardBasicAssets.TitleFont, text, size, fontSize)
                {
                    Alignment = TMPro.TextAlignmentOptions.TopLeft,
                    Pivot = new Vector2(0f, 1f)
                };
                return result;
            }

            TMProFLabel CreateInfoLabel(string text, Vector2 size, float fontSize = 0.8f)
            {
                var result = new TMProFLabel(CardBasicAssets.TitleFont, text, size, fontSize)
                {
                    Alignment = TMPro.TextAlignmentOptions.Top,
                    Pivot = new Vector2(0.5f, 1f)
                };
                return result;
            }

            CosmeticRectInstance CreateAndWrapCosmeticRect(Vector2 topLeftPos, Vector2 rect)
            {
                return new CosmeticRectInstance(opNodeWrapper, topLeftPos, rect);
            }
        }

        public void RecaculateContextSizeAndPos()
        {
            if (descriptionInfoLabel.TextRect.x < 0 || descriptionInfoLabel.TextRect.y < 0 || conflictInfoLabel.TextRect.x < 0 || conflictInfoLabel.TextRect.y < 0)
                return;

            contentSizeDirty = -1;
            float newContentSize = CardpediaStatics.infoDisplay_BigRectHeight;

            newContentSize += CardpediaStatics.tinyGap * 2f + CardpediaStatics.cosmeticRectHeight;//background;
            float descriptionLabelRectHeight = descriptionInfoLabel.TextRect.y + CardpediaStatics.tinyGap * 2f;
            newContentSize += descriptionLabelRectHeight;
            newContentSize += CardpediaStatics.tinyGap;

            newContentSize += CardpediaStatics.tinyGap * 2f + CardpediaStatics.cosmeticRectHeight;//background;
            float conflictLabelRectHeigth = conflictInfoLabel.TextRect.y + CardpediaStatics.tinyGap * 2f;
            newContentSize += conflictLabelRectHeigth;

            newContentSize = Mathf.Max(CardpediaStatics.infoDisplayWindowScale.y, newContentSize);

            scrollBox.contentSize = newContentSize;

            BuffPlugin.Log($"New scrollBox contentSize : {newContentSize}");

            int totalTitleCount = 3;
            Vector2 titleRectScale = new Vector2((CardpediaStatics.infoDisplayWindowScale.x - CardpediaStatics.tinyGap * (totalTitleCount + 1)) / totalTitleCount, CardpediaStatics.infoDisplay_BigRectHeight);

            opNodeWrapper.SetPos(Vector2.zero);

            Vector2 pos = new Vector2(CardpediaStatics.tinyGap, newContentSize - CardpediaStatics.tinyGap);
            typeRect.Pos = pos;//typeRect = CreateAndWrapCosmeticRect(pos, titleRectScale);
            opNodeWrapper.WrapNode(typeTitleLabel, pos);
            opNodeWrapper.WrapNode(typeInfoLabel, pos + new Vector2(titleRectScale.x / 2f, -CardpediaStatics.tinyGap * 2 - CardpediaStatics.cosmeticRectHeight));


            pos = new Vector2(CardpediaStatics.tinyGap * 2 + titleRectScale.x, newContentSize - CardpediaStatics.tinyGap);
            triggleableRect.Pos = pos;//triggleableRect = CreateAndWrapCosmeticRect(pos, titleRectScale);
            opNodeWrapper.WrapNode(triggleableTitleLabel, pos);
            opNodeWrapper.WrapNode(triggleableInfoLabel, pos + new Vector2(titleRectScale.x / 2f, -CardpediaStatics.tinyGap * 2 - CardpediaStatics.cosmeticRectHeight));


            pos = new Vector2(CardpediaStatics.tinyGap * 3 + titleRectScale.x * 2, newContentSize - CardpediaStatics.tinyGap);
            stackableRect.Pos = pos;//stackableRect = CreateAndWrapCosmeticRect(pos, titleRectScale);
            opNodeWrapper.WrapNode(stackableTitleLabel, pos);
            opNodeWrapper.WrapNode(stackableInfoLabel, pos + new Vector2(titleRectScale.x / 2f, -CardpediaStatics.tinyGap * 2 - CardpediaStatics.cosmeticRectHeight));


            Vector2 descriptionRectSize = new Vector2(CardpediaStatics.infoDisplayWindowScale.x - CardpediaStatics.tinyGap * 2, descriptionLabelRectHeight + CardpediaStatics.tinyGap * 3 + CardpediaStatics.cosmeticRectHeight);
            pos = new Vector2(CardpediaStatics.tinyGap, newContentSize - (CardpediaStatics.tinyGap + titleRectScale.y + CardpediaStatics.tinyGap));
            descriptionRect.Rect = descriptionRectSize; //descriptionRect = CreateAndWrapCosmeticRect(pos, fullRectScale);
            descriptionRect.Pos = pos;
            opNodeWrapper.WrapNode(descriptionTitleLabel, pos);
            opNodeWrapper.WrapNode(descriptionInfoLabel, pos + new Vector2(CardpediaStatics.tinyGap, -CardpediaStatics.tinyGap * 3 - CardpediaStatics.cosmeticRectHeight));


            Vector2 conflictRectSize = new Vector2(CardpediaStatics.infoDisplayWindowScale.x - CardpediaStatics.tinyGap * 2, conflictLabelRectHeigth + CardpediaStatics.tinyGap * 2 + CardpediaStatics.cosmeticRectHeight);
            pos -= new Vector2(0f, descriptionRectSize.y + CardpediaStatics.tinyGap);
            conflictRect.Rect = conflictRectSize;
            conflictRect.Pos = pos;
            opNodeWrapper.WrapNode(conflictTitleLabel, pos);
            opNodeWrapper.WrapNode(conflictInfoLabel, pos + new Vector2(CardpediaStatics.tinyGap, -CardpediaStatics.tinyGap * 3 - CardpediaStatics.cosmeticRectHeight));

            scrollBox.ScrollToTop();
        }

        public void SetText(string type ,string triggleable, string stackable, string description, string conflict)
        {
            typeInfoLabel.Text = type ?? BuffResourceString.Get("CardInfoDisplay_Missing");
            triggleableInfoLabel.Text = triggleable ?? BuffResourceString.Get("CardInfoDisplay_Missing");
            stackableInfoLabel.Text = stackable ?? BuffResourceString.Get("CardInfoDisplay_Missing");
            descriptionInfoLabel.Text = description ?? BuffResourceString.Get("CardInfoDisplay_MissingDescription");
            conflictInfoLabel.Text = conflict ?? BuffResourceString.Get("CardInfoDisplay_MissingConflict");
            contentSizeDirty = 2;
        }

        public void Update()
        {
            scrollBox._insideTexture.alpha = alpha;
            blurSprite.alpha = alpha;
            if (contentSizeDirty > 0)
                contentSizeDirty--;
            else if(contentSizeDirty == 0)
                RecaculateContextSizeAndPos();
        }

        public void GrafUpdate(float timeStacker)
        {
        }

        class CosmeticRectInstance
        {
            OpFNodeWrapper nodeWrapper;

            public FSprite lineA;
            public FSprite lineB;
            public FSprite background;

            bool forcepos;
            Vector2 pos;
            public Vector2 Pos
            {
                get => pos;
                set
                {
                    if (pos == value && !forcepos)
                        return;
                    pos = value;
                    forcepos = false;
                    nodeWrapper.WrapNode(background, new Vector2(pos.x + CardpediaStatics.tinyGap, pos.y - CardpediaStatics.tinyGap));
                    nodeWrapper.WrapNode(lineA, new Vector2(pos.x, pos.y - rect.y));
                    nodeWrapper.WrapNode(lineB, new Vector2(pos.x + rect.x, pos.y - 40f - CardpediaStatics.tinyGap));
                }
            }

            Vector2 rect;
            public Vector2 Rect
            {
                get => rect;
                set
                {
                    if (rect == value)
                        return;
                    rect = value;
                    background.scaleX = rect.x - CardpediaStatics.tinyGap * 2;
                    background.scaleY = CardpediaStatics.cosmeticRectHeight;

                    lineA.scaleX = rect.x;
                    lineA.scaleY = 1f;

                    lineB.scaleX = 1f;
                    lineB.scaleY = rect.y - CardpediaStatics.cosmeticRectHeight - CardpediaStatics.tinyGap;
                    forcepos = true;
                }
            }
        
            public CosmeticRectInstance(OpFNodeWrapper nodeWrapper, Vector2 pos, Vector2 rect)
            {
                this.nodeWrapper = nodeWrapper;
                background = new FSprite("pixel")
                {
                    scaleX = rect.x - CardpediaStatics.tinyGap * 2,
                    scaleY = CardpediaStatics.cosmeticRectHeight,
                    anchorX = 0f,
                    anchorY = 1f,
                    color = CardpediaStatics.pediaUIDarkGrey,
                    shader = Custom.rainWorld.Shaders["MenuTextCustom"]
                };
                lineA = new FSprite("pixel") 
                { 
                    scaleX = rect.x, 
                    scaleY = 1f, 
                    anchorX = 0f, 
                    anchorY = 1f, 
                    color = CardpediaStatics.pediaUILightGrey 
                };
                lineB = new FSprite("pixel") 
                { 
                    scaleX = 1f, 
                    scaleY = rect.y - CardpediaStatics.cosmeticRectHeight - CardpediaStatics.tinyGap, 
                    anchorX = 0f, 
                    anchorY = 1f, 
                    color = CardpediaStatics.pediaUILightGrey 
                };

                Rect = rect;
                Pos = pos;
            }
        }
    }

    public class OpFNodeWrapper : UIelement
    {
        List<FNode> nodes = new();
        public OpFNodeWrapper(Vector2 pos, Vector2 size) : base(pos, size)
        {
        }

        public void WrapNode(FNode node, Vector2 position)
        {
            if (!nodes.Contains(node))
            {
                nodes.Add(node);
                myContainer.AddChild(node);
            }
            node.SetPosition(position);
            Change();
        }

        public void UnwarpNode(FNode node)
        {
            if (!nodes.Contains(node))
                return;

            nodes.Remove(node);
            myContainer.RemoveChild(node);
        }
    }
}
