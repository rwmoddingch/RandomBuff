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
using RandomBuff.Core.SaveData;
using RandomBuff.Core.SaveData.BuffConfig;

namespace RandomBuff.Cardpedia.Elements
{
    internal class ConfigManager : OpCardpediaChainBox.IScrollBoxHandler
    {
        public CardpediaMenu cardpediaMenu;
        public FSprite blurSprite;
        public FLabel noConfigLabel;
        public FSprite sleepySlug;

        public MenuTabWrapper wrapper;

        public OpScrollBox scrollBox;
        float alpha;

        List<OpCardpediaChainBox> activeConfigs = new List<OpCardpediaChainBox>();

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
            //Vector2 fullRectScale = new Vector2(CardpediaStatics.narrowBlurSpriteScale.x - CardpediaStatics.tinyGap * 2, 200f);
            //var chainBosA = new OpCardpediaChainBox("ChainBoxTestA", pos, new Vector2(fullRectScale.x, 100f));
            //var chainBoxB = new OpCardpediaChainBox("ChainBoxTestB", pos, new Vector2(fullRectScale.x, 100f));
            //var chainDropBox = new OpCardpediaDropBox("DropBoxTest", pos, fullRectScale.x, SlugcatStats.Name.values.entries.ToArray());
            //var chainSlider = new OpCardpediaConfigSlider("SliderTest", pos, fullRectScale.x, 0f, 1f);
            //chainDropBox.chainTarget = chainBosA;
            //chainSlider.chainTarget = chainDropBox;
            //chainBoxB.chainTarget = chainSlider;
            //scrollBox.AddItems(chainBosA, chainBoxB, chainDropBox, chainSlider);

            scrollBox.ScrollToTop();

            return;
            //string str = Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese ?
            //    "当前卡牌无\n自定义设置。" : "Current card doesn't\nhave custom settings.";
            //noConfigLabel = new FLabel(Custom.GetFont(), str);
            //noConfigLabel.alignment = FLabelAlignment.Center;
            //noConfigLabel.scale = 1.2f;
            //noConfigLabel.alpha = 0f;
            //noConfigLabel.SetPosition(blurSprite.GetPosition());
            //cardpediaMenu.cursorContainer.AddChild(noConfigLabel);

            //Futile.atlasManager.LoadAtlas("Atlases/sleep");
            //sleepySlug = new FSprite("sleep_020");
            //sleepySlug.element = Futile.atlasManager.GetElementWithName("sleep_" + ((int)13).ToString("000"));
            //sleepySlug.alpha = 0f;
            //sleepySlug.SetPosition(blurSprite.GetPosition() - new Vector2(0f, 80f));
            //cardpediaMenu.cursorContainer.AddChild(sleepySlug);
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
            BuffConfigurableManager.PushAllConfigs();

            wrapper._tab.RemoveItems(scrollBox.items.ToArray());
            scrollBox.items.Clear();

            foreach (var item in activeConfigs)
                item.Unload();
            activeConfigs.Clear();

            Vector2 pos = new Vector2(CardpediaStatics.tinyGap, scrollBox.contentSize - CardpediaStatics.tinyGap);
            Vector2 fullRectScale = new Vector2(CardpediaStatics.narrowBlurSpriteScale.x - CardpediaStatics.tinyGap * 2, 200f);
            foreach(BuffConfigurable configurable in BuffConfigurableManager.GetAllConfigurableForID(card.ID))
            {
                if(configurable.acceptable is BuffConfigurableAcceptableRange range)
                {
                    activeConfigs.Add(new OpCardpediaConfigSlider(configurable, configurable.name, pos, fullRectScale.x, (float)range.minValue, (float)range.maxValue, this));
                }
                else if(configurable.acceptable is BuffConfigurableAcceptableList lst)
                {
                    activeConfigs.Add(new OpCardpediaDropBox(configurable, configurable.name, pos, fullRectScale.x, lst.values.Select((x) => x.ToString()).ToArray(), this));
                }
                else if(configurable.acceptable is BuffConfigurableAcceptableKeyCode key)
                {
                    activeConfigs.Add(new OpCardpediaKeyBinder(configurable, configurable.name, pos, fullRectScale.x, this));
                }
            }

            for(int i = 1; i < activeConfigs.Count; i++)
            {
                activeConfigs[i].chainTarget = activeConfigs[i - 1];
            }
            if(activeConfigs.Count > 0)
            {
                scrollBox.AddItems(activeConfigs.ToArray());
            }
            ResetScrollBoxSize();
        }

        public void ResetScrollBoxSize()
        {
            float newContentSize = Mathf.Max(-activeConfigs.Last().TargetChainedOffset.y + activeConfigs.Last().setRectSize.y + CardpediaStatics.tinyGap * 2, CardpediaStatics.narrowBlurSpriteScale.y);

            float currentScroll = scrollBox.scrollOffset;
            newContentSize = Mathf.Max(newContentSize, scrollBox.size.y);
            scrollBox.contentSize = newContentSize;
            scrollBox.targetScrollOffset = currentScroll;
            scrollBox.scrollOffset = scrollBox.targetScrollOffset;

            //BuffPlugin.Log($"config box new content size : {newContentSize}");

            activeConfigs.First().SetPos(new Vector2(CardpediaStatics.tinyGap, newContentSize - CardpediaStatics.tinyGap));
            foreach(var config in activeConfigs)
            {
                config.defaultPos = new Vector2(CardpediaStatics.tinyGap, newContentSize - CardpediaStatics.tinyGap);
            }
        }
    }
}
