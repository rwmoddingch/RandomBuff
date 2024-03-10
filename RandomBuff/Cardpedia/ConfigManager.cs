using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;

namespace RandomBuff.Cardpedia.Elements
{
    public class ConfigManager
    {
        public CardpediaMenu cardpediaMenu;
        public FSprite blurSprite;
        public FLabel noConfigLabel;
        public FSprite sleepySlug;
        public ConfigManager(CardpediaMenu menu)
        {
            cardpediaMenu = menu;
            
            blurSprite = new FSprite("pixel");
            blurSprite.scaleX = 0.1f;
            blurSprite.SetPosition(new Vector2(1150f, 483f));
            blurSprite.shader = cardpediaMenu.manager.rainWorld.Shaders["UIBlur"];
            cardpediaMenu.container.AddChild(blurSprite);

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
            float lastAlpha = noConfigLabel.alpha;
            float alpha = Mathf.Lerp(lastAlpha, cardpediaMenu.SetAlpha, timeStacker);
            noConfigLabel.alpha = alpha;
            sleepySlug.alpha = alpha;
            blurSprite.alpha = alpha;
            blurSprite.scaleX = cardpediaMenu.blurSprite.scaleX;
            blurSprite.scaleY = cardpediaMenu.blurSprite.scaleY;
        }
    }
}
