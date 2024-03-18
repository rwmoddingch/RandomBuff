using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Random = UnityEngine.Random;
using RandomBuff.Render.UI;
using RandomBuff.Cardpedia.Elements.Config;

namespace RandomBuff.Cardpedia.Elements
{
    internal class ConfigManager
    {
        public CardpediaMenu cardpediaMenu;
        public FSprite blurSprite;
        public FLabel noConfigLabel;
        public FSprite sleepySlug;

        public MenuTabWrapper wrapper;

        public OpScrollBox scrollBox;
        float alpha;

        public ConfigManager(CardpediaMenu menu)
        {
            cardpediaMenu = menu;
            
            blurSprite = new FSprite("pixel");
            blurSprite.scaleX = 0.1f;
            blurSprite.SetPosition(CardpediaStatics.rightBlurSpritePos);
            blurSprite.shader = cardpediaMenu.manager.rainWorld.Shaders["UIBlur"];
            cardpediaMenu.container.AddChild(blurSprite);

            wrapper = new MenuTabWrapper(cardpediaMenu, menu.pages[0]);
            menu.pages[0].subObjects.Add(wrapper);

            Vector2 scale = CardpediaStatics.narrowBlurSpriteScale;
            float contentSize = 1000f;
            scrollBox = new OpScrollBox(new Vector2(1150f, 483f) - scale / 2f, scale, contentSize, false, false, false);
            new UIelementWrapper(wrapper, scrollBox);

            Vector2 pos = new Vector2(CardpediaStatics.tinyGap, contentSize - CardpediaStatics.tinyGap);
            Vector2 fullRectScale = new Vector2(CardpediaStatics.narrowBlurSpriteScale.x - CardpediaStatics.tinyGap * 2, 200f);
            var chainBosA = new OpCardpediaChainBox("ChainBoxTestA", pos, new Vector2(fullRectScale.x, 100f));
            var chainBoxB = new OpCardpediaChainBox("ChainBoxTestB", pos, new Vector2(fullRectScale.x, 100f));
            var chainDropBox = new OpCardpediaDropBox("DropBoxTest", pos, fullRectScale.x, SlugcatStats.Name.values.entries.ToArray());
            var chainSlider = new OpCardpediaConfigSlider("SliderTest", pos, fullRectScale.x, 0f, 1f);
            chainDropBox.chainTarget = chainBosA;
            chainSlider.chainTarget = chainDropBox;
            chainBoxB.chainTarget = chainSlider;
            scrollBox.AddItems(chainBosA, chainBoxB, chainDropBox, chainSlider);

            scrollBox.ScrollToTop();

            return;
            string str = Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese ?
                "当前卡牌无\n自定义设置。" : "Current card doesn't\nhave custom settings.";
            noConfigLabel = new FLabel(Custom.GetFont(), str);
            noConfigLabel.alignment = FLabelAlignment.Center;
            noConfigLabel.scale = 1.2f;
            noConfigLabel.alpha = 0f;
            noConfigLabel.SetPosition(blurSprite.GetPosition());
            cardpediaMenu.cursorContainer.AddChild(noConfigLabel);

            Futile.atlasManager.LoadAtlas("Atlases/sleep");
            sleepySlug = new FSprite("sleep_020");
            sleepySlug.element = Futile.atlasManager.GetElementWithName("sleep_" + ((int)13).ToString("000"));
            sleepySlug.alpha = 0f;
            sleepySlug.SetPosition(blurSprite.GetPosition() - new Vector2(0f, 80f));
            cardpediaMenu.cursorContainer.AddChild(sleepySlug);
        }

        public void GrafUpdate(float timeStacker)
        {
            alpha = Mathf.Lerp(alpha, cardpediaMenu.SetAlpha, timeStacker);
            //noConfigLabel.alpha = alpha;
            //sleepySlug.alpha = alpha;
            blurSprite.alpha = alpha;
            blurSprite.scaleX = cardpediaMenu.blurSprite.scaleX;
            blurSprite.scaleY = cardpediaMenu.blurSprite.scaleY;

            scrollBox._insideTexture.alpha = alpha;
            scrollBox.Hidden = cardpediaMenu.SetAlpha == 0f;
        }

        public void OnCardPick(BuffCard card)
        {
            wrapper._tab.RemoveItems(scrollBox.items.ToArray());
            scrollBox.items.Clear();

            List<UIelement> lst = new List<UIelement>();
            for (int i = 0; i < Random.Range(5, 20); i++)
            {
                lst.Add(new OpSimpleButton(new Vector2(40f, scrollBox.contentSize - 40f * (i + 1)), new Vector2(100f, 30f), "Wawa Test " + i.ToString()));
            }
            scrollBox.AddItems(lst.ToArray());
        }
    }
}
