using Menu;
using Menu.Remix.MixedUI;
using RandomBuff.Core.Buff;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI.BuffPack
{
    internal class PackButton : OpSimpleButton
    {
        static Color fillGreen = Color.green * 0.3f + Color.black * 0.7f;
        static Color edgeGreen = Color.green;

        public BuffPluginInfo pluginInfo;

        FSprite sprite;
        FLabel nameLabel;
        FLabel descriptionLabel;

        bool showDescription;
        bool canBeDisable;

        Vector2 spriteRect;

        bool _enabled;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                colorFill = value ? fillGreen : MenuColorEffect.rgbBlack;
                colorEdge = value ? edgeGreen : MenuColorEffect.rgbMediumGrey;
            }
        }

        public Action ToggleCallBack;

        public PackButton(Vector2 pos, Vector2 size, BuffPluginInfo pluginInfo, bool showDescription = true, bool canBeDisable = true) : base(pos, size, "")
        {
            this.pluginInfo = pluginInfo;
            this.showDescription = showDescription;

            var info = pluginInfo.GetInfo(Custom.rainWorld.inGameTranslator.currentLanguage);

            sprite = new FSprite(pluginInfo.Thumbnail, true);

            nameLabel = new FLabel(showDescription ? Custom.GetDisplayFont() : Custom.GetFont(), info.Name);

            myContainer.AddChild(sprite);
            myContainer.AddChild(nameLabel);

            sprite.SetAnchor(0.5f, 0.5f);
            nameLabel.SetAnchor(0f, showDescription ? 0f : 0.5f);
            

            float scale = (size.y - 10f) / sprite.element.sourcePixelSize.y;
            
            sprite.scale = (size.y - 10f) / sprite.element.sourcePixelSize.y;
            spriteRect = new Vector2(scale * sprite.element.sourcePixelSize.x, scale * sprite.element.sourcePixelSize.y);
            string wrapedDescription = LabelTest.WrapText(info.Description, false, size.x - 30f - spriteRect.x);

            description = info.Description;

            if(showDescription)
            {
                descriptionLabel = new FLabel(Custom.GetFont(), info.Description);
                myContainer.AddChild(descriptionLabel);
                descriptionLabel.SetAnchor(0f, 1f);
                descriptionLabel.text = wrapedDescription;
                descriptionLabel.color = MenuColorEffect.rgbMediumGrey;
            }

            OnClick += PackButton_OnClick;
        }

        private void PackButton_OnClick(UIfocusable trigger)
        {
            Enabled = !Enabled;
            ToggleCallBack?.Invoke();
        }

        public override void Change()
        {
            base.Change();
            sprite.SetPosition(spriteRect.x/2f + 10f, size.y / 2f);
            nameLabel.SetPosition(spriteRect.x + 20f, size.y / 2f + spriteRect.y / 2f - 30f);

            descriptionLabel?.SetPosition(spriteRect.x + 20f, size.y / 2f + spriteRect.y / 2f - 30f - 10f);
        }
    }
}
