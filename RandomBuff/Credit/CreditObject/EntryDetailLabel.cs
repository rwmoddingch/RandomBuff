using RandomBuff.Render.CardRender;
using RandomBuff.Render.UI.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Credit.CreditObject
{
    internal class EntryDetailLabel : NameDetailLabel
    {
        public EntryDetailLabel(Menu.Menu menu, BuffCreditStage owner, Vector2 endPos, float inStageEnterTime, float lifeTime, string string1, string string2) : base(menu, owner, endPos, inStageEnterTime, lifeTime, string1, string2)
        {
        }

        public override void InitLabels()
        {
            Container.AddChild(label_1 = new TMProFLabel(CardBasicAssets.TitleFont, string1, new Vector2(1000f, 100f), 0.8f) { color = Color.white * 0.3f + Color.black * 0.7f, alpha = 0f, Pivot = new Vector2(0f, 0.5f), Alignment = TMPro.TextAlignmentOptions.Left, scale = 0.8f });
            Container.AddChild(label_2 = new TMProFLabel(CardBasicAssets.TitleFont, string2, new Vector2(1000f, 100f), 0.8f) { color = Color.white, alpha = 0f, Pivot = new Vector2(0f, 0.5f), Alignment = TMPro.TextAlignmentOptions.Left });
        }

        public override void RecaculateTextRectParam()
        {
            base.RecaculateTextRectParam();

        }
    }
}
