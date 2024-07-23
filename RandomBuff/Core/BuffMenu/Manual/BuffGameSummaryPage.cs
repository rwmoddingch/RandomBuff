using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.BuffMenu.Manual
{
    internal class BuffGameSummaryPage : BuffManualPage
    {
        public BuffGameSummaryPage(BuffGameManual manual, int pageNumber, MenuObject owner) : base(manual, pageNumber, owner)
        {
            float anchorY = rectHeight;
            AddIllusitration("buffassets/illustrations", "manual_summary", ref anchorY);
            anchorY -= 25f;
            AddText(BuffResourceString.Get("BuffManual_Summary_Content_0"), true, ref anchorY);
        }
    }
}
