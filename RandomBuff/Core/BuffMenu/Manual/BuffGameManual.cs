using Menu;
using Menu.Remix.MixedUI;
using RandomBuff.Render.UI;
using RandomBuff.Render.UI.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.BuffMenu.Manual
{
    internal sealed class BuffGameManual : ManualDialog
    {
        public static Dictionary<string, int> topicKeys = new Dictionary<string, int>
        {
            { "BuffManual_Introduction", 1 },
            { "BuffManual_Gameplay", 1 },
            { "BuffManual_Buff", 1 },
            { "BuffManual_Condition", 1 },
            { "BuffManual_Gamemode", 6 }
        };

        public CardTitle manualPageTitle;

        public BuffGameManual(ProcessManager manager, Dictionary<string, int> topics)
            : base(manager, topics)
        {
            currentTopic = this.topics.Keys.ElementAt(0);
            pageNumber = 0;

            manualPageTitle = new CardTitle(container, CardTitle.GetNormScale(0.15f), new Vector2(520.01f, 155f + 2155f) + new Vector2(15f + contentOffX, 472f), anchorX: 0f, spanShrink: 0.1f, flipCounter:CardTitle.GetNormFlipCounter(true), flipDelay: CardTitle.GetNormFlipDelay(true), spanAdjust: CardTitle.GetNormSpanAdjust(0.15f));

            GetManualPage(currentTopic, pageNumber);
        }

        public override void Update()
        {
            base.Update();
            manualPageTitle.Update();
            manualPageTitle.pos = pages[1].ScreenPos + new Vector2(15f + contentOffX, 472f);
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            manualPageTitle.GrafUpdate(timeStacker);
            
        }

        public override string TopicName(string topic)
        {
            return BuffResourceString.Get(topic, true);
        }

        public override void GetManualPage(string topic, int pageNumber)
        {
            if (currentTopicPage != null)
            {
                currentTopicPage.RemoveSprites();
                pages[1].RemoveSubObject(currentTopicPage);
            }

            if (topic == "BuffManual_Gameplay")
                currentTopicPage = new BuffGamePlayManualPage(this, pageNumber, pages[1]);
            else if(topic == "BuffManual_Introduction")
                currentTopicPage = new BuffGameSummaryPage(this, pageNumber, pages[1]);
            else if(topic == "BuffManual_Gamemode")
                currentTopicPage = new BuffGameModeManualPage(this, pageNumber, pages[1]);
            else
                currentTopicPage = new BuffManualPage(this, pageNumber, pages[1]);
            pages[1].subObjects.Add(this.currentTopicPage);
        }
    }

    internal class BuffManualPage : ManualPage
    {
        public static readonly int rectHeight = 450;

        public BuffManualPage(BuffGameManual manual, int pageNumber, MenuObject owner) : base(manual, owner)
        {
            topicName = BuffResourceString.Get($"{manual.currentTopic}_{pageNumber}");

            manual.manualPageTitle.RequestSwitchTitle(topicName);
            headingSeparator = new FSprite("pixel", true);
            headingSeparator.scaleX = 594f;
            headingSeparator.scaleY = 2f;
            headingSeparator.color = new Color(0.7f, 0.7f, 0.7f);
            Container.AddChild(headingSeparator);
        }

        public void AddIllusitration(string folder, string file, ref float anchorY)
        {
            MenuIllustration menuIllustration = new MenuIllustration(menu, owner, folder, file, new Vector2(-2f + (menu as BuffGameManual).contentOffX, anchorY), true, true);
            menuIllustration.sprite.SetAnchor(0f, 1f);
            //menuIllustration.lastPos = menuIllustration.pos = new Vector2(-2f + (menu as BuffGameManual).contentOffX, anchorY - menuIllustration.size.y / 2f);
            this.subObjects.Add(menuIllustration);
            anchorY -= menuIllustration.size.y;
        }

        public void AddText(string text, bool bigText, ref float anchorY)
        {
            text = Regex.Replace(text, "<LINE>", "\n");
            string[] array = Regex.Split(text.WrapText(bigText, 570f + (menu as BuffGameManual).wrapTextMargin, false), "\n");
            for (int i = 0; i < array.Length; i++)
            {
                MenuLabel menuLabel = new MenuLabel(menu, owner, array[i], new Vector2(295f + (menu as BuffGameManual).contentOffX, anchorY), Vector2.zero, bigText, null);
                menuLabel.label.SetAnchor(0.5f, 1f);
                menuLabel.label.color = new Color(0.7f, 0.7f, 0.7f);
                this.subObjects.Add(menuLabel);
                anchorY -= bigText ? 30f : 25f;
            }
        }


        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            headingSeparator.x = base.page.pos.x + 295f + (menu as BuffGameManual).contentOffX;
            headingSeparator.y = base.page.pos.y + 450f;
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            headingSeparator.RemoveFromContainer();
        }

        public FSprite headingSeparator;
    }
}
