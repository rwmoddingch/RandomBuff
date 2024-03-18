using Menu.Remix;
using Menu.Remix.MixedUI;
using RandomBuff.Cardpedia.Elements.Config;
using RandomBuff.Cardpedia.PediaPage;
using RandomBuff.Core.BuffMenu;
using RandomBuff.Render.CardRender;
using RandomBuff.Render.UI;
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

        //状态变量
        public float alpha;

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
            Vector2 titleRectScale = new Vector2((CardpediaStatics.infoDisplayWindowScale.x - CardpediaStatics.tinyGap * (totalTitleCount+1)) / totalTitleCount , y: 100f);

            typeTitleLabel = CreateLabel("Type:", titleRectScale);
            Vector2 pos = new Vector2(CardpediaStatics.tinyGap, contentSize - CardpediaStatics.tinyGap);
            CreateAndWrapCosmeticRect(pos, titleRectScale);
            opNodeWrapper.WrapNode(typeTitleLabel, pos);

            triggleableTitleLabel = CreateLabel("Trigger:", titleRectScale);
            pos = new Vector2(CardpediaStatics.tinyGap * 2 + titleRectScale.x, contentSize - CardpediaStatics.tinyGap);
            CreateAndWrapCosmeticRect(pos, titleRectScale);
            opNodeWrapper.WrapNode(triggleableTitleLabel, pos);

            stackableTitleLabel = CreateLabel("Stack:", titleRectScale);
            pos = new Vector2(CardpediaStatics.tinyGap * 3 + titleRectScale.x * 2, contentSize - CardpediaStatics.tinyGap);
            CreateAndWrapCosmeticRect(pos, titleRectScale);
            opNodeWrapper.WrapNode(stackableTitleLabel, pos);

            Vector2 fullRectScale = new Vector2(CardpediaStatics.infoDisplayWindowScale.x - CardpediaStatics.tinyGap * 2, 200f);
            pos = new Vector2(CardpediaStatics.tinyGap, contentSize - (CardpediaStatics.tinyGap + titleRectScale.y + CardpediaStatics.tinyGap));
            descriptionTitleLabel = CreateLabel("Description:", fullRectScale);
            CreateAndWrapCosmeticRect(pos, fullRectScale);
            opNodeWrapper.WrapNode(descriptionTitleLabel, pos);

            conflictTitleLabel = CreateLabel("Conflict:", fullRectScale);
            pos -= new Vector2(0f, fullRectScale.y + CardpediaStatics.tinyGap);
            CreateAndWrapCosmeticRect(pos, fullRectScale);
            opNodeWrapper.WrapNode(conflictTitleLabel, pos);

            scrollBox.ScrollToTop();

            TMProFLabel CreateLabel(string text, Vector2 size)
            {
                //TopLeftLabel
                var result = new TMProFLabel(CardBasicAssets.TitleFont, text, size)
                {
                    Alignment = TMPro.TextAlignmentOptions.TopLeft,
                    Pivot = new Vector2(0f, 1f)
                };
                return result;
            }

            void CreateAndWrapCosmeticRect(Vector2 topLeftPos, Vector2 rect)
            {
                var backGround = new FSprite("pixel") { scaleX = rect.x - CardpediaStatics.tinyGap * 2, scaleY = 40f , anchorX = 0f, anchorY = 1f, color = CardpediaStatics.pediaUIDarkGrey, shader = Custom.rainWorld.Shaders["MenuTextCustom"]
                };
                opNodeWrapper.WrapNode(backGround, new Vector2(topLeftPos.x + CardpediaStatics.tinyGap, topLeftPos.y - CardpediaStatics.tinyGap));

                var lineA = new FSprite("pixel") { scaleX = rect.x, scaleY = 1f, anchorX = 0f, anchorY = 1f, color = CardpediaStatics.pediaUILightGrey };
                opNodeWrapper.WrapNode(lineA, new Vector2(topLeftPos.x, topLeftPos.y - rect.y));

                var lineB = new FSprite("pixel") { scaleX = 1f, scaleY = rect.y - 40f - CardpediaStatics.tinyGap, anchorX = 0f, anchorY = 1f, color = CardpediaStatics.pediaUILightGrey };
                opNodeWrapper.WrapNode(lineB, new Vector2(topLeftPos.x + rect.x, topLeftPos.y - 40f - CardpediaStatics.tinyGap));
            }
        }

        public void Update()
        {
            scrollBox._insideTexture.alpha = alpha;
            blurSprite.alpha = alpha;
        }

        public void GrafUpdate(float timeStacker)
        {

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
