using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.BuffMenu.Manual
{
    internal class BuffGameGamePlayManual : BuffManualPage
    {
        public BuffGameGamePlayManual(BuffGameManual manual, int pageNumber, MenuObject owner) : base(manual, pageNumber, owner)
        {
            float anchorY = rectHeight;
            if (pageNumber == 0) 
            {
                AddIllusitration("buffassets/illustrations", "buffmanual_gameplay_0", ref anchorY);
                anchorY -= 25f;
                AddText(BuffResourceString.Get("BuffManual_Gameplay_Content_0"), false, ref anchorY);
            }
            else if (pageNumber == 1)
            {
                AddIllusitration("buffassets/illustrations", "manual_gamemode_instruction", ref anchorY);
                anchorY -= 25f;
                AddText(BuffResourceString.Get("BuffManual_Gameplay_Content_1"), false, ref anchorY);
            }
            else if (pageNumber == 2)
            {
                AddIllusitration("buffassets/illustrations", "buffmanual_gameplay_2", ref anchorY);
                anchorY -= 25f;
                AddText(BuffResourceString.Get("BuffManual_Gameplay_Content_2"), false, ref anchorY);
            }
        }
    }
}