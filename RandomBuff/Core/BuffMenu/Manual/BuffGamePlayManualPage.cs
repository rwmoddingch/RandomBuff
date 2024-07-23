using Menu;
using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.BuffMenu.Manual
{
    internal class BuffGamePlayManualPage : BuffManualPage
    {
        public BuffGamePlayManualPage(BuffGameManual manual, int pageNumber, MenuObject owner) : base(manual, pageNumber, owner)
        {
            float anchorY = rectHeight;
            AddIllusitration("", "gameplaychanges", ref anchorY);

            //MenuIllustration menuIllustration = new MenuIllustration(menu, owner, "", "gameplaychanges", new Vector2(-2f + manual.contentOffX, 349f), true, true);
            //menuIllustration.sprite.SetAnchor(0f, 0.5f);
            //this.subObjects.Add(menuIllustration);

            anchorY -= 25f;
            AddText(menu.Translate("Whilst undertaking an expedition, certain gameplay elements are changed:"), true, ref anchorY);
            //string[] array = Regex.Split(menu.Translate("Whilst undertaking an expedition, certain gameplay elements are changed:").WrapText(true, 570f + manual.wrapTextMargin, false), "\n");
            //for (int i = 0; i < array.Length; i++)
            //{
            //    MenuLabel menuLabel = new MenuLabel(menu, owner, array[i], new Vector2(295f + manual.contentOffX, anchorY), Vector2.zero, true, null);
            //    menuLabel.label.SetAnchor(0.5f, 1f);
            //    menuLabel.label.color = new Color(0.7f, 0.7f, 0.7f);
            //    this.subObjects.Add(menuLabel);
            //    anchorY -= 25f;
            //}
            anchorY -= 30f;
            float num = 0f;
            if (menu.CurrLang == InGameTranslator.LanguageID.Russian)
            {
                num = 30f;
            }
            AddText(menu.Translate("Players start the Expedition in a randomly selected shelter.<LINE>All characters start with two karma and have a maximum karma limit of five.<LINE>The Survivor achievement is completed by default, and other achievements are not gated by it.<LINE>Passages are disabled by default, instead being unlocked by a Perk.<LINE>Echoes will not spawn on the first cycle.<LINE>There is no cycle limit for The Hunter in Expedition.<LINE>Collectable tokens will not appear and discovered lore is not saved in the Collection menu."), false, ref anchorY);
        }
    }
}
