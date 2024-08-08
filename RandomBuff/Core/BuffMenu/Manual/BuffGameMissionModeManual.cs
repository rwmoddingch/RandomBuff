using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.BuffMenu.Manual
{
    internal class BuffGameMissionModeManual : BuffManualPage
    {
        public BuffGameMissionModeManual(BuffGameManual manual, int pageNumber, MenuObject owner) : base(manual, pageNumber, owner)
        {
            float anchorY = rectHeight;
            if (pageNumber == 0)
            {
                AddIllusitration("buffassets/illustrations", "buffmanual_missionmode_0", ref anchorY);
                anchorY -= 25f;
                AddText(BuffResourceString.Get("BuffManual_MissionMode_Content_0"), false, ref anchorY);
            }
            else if (pageNumber == 1)
            {
               
                anchorY -= 25f;
                AddText(BuffResourceString.Get("BuffManual_MissionMode_Content_1"), false, ref anchorY);
            }
            else if (pageNumber == 2)
            {
                AddIllusitration("buffassets/illustrations", "buffmanual_missionmode_1", ref anchorY);
                anchorY -= 25f;
                AddText(BuffResourceString.Get("BuffManual_MissionMode_Content_2"), false, ref anchorY);
            }
            else if (pageNumber == 3)
            {
                AddIllusitration("buffassets/illustrations", "buffmanual_missionmode_2", ref anchorY);
                //AddIllusitration("buffassets/illustrations", "manual_buffadncard_3", ref anchorY);
                anchorY -= 25f;
                AddText(BuffResourceString.Get("BuffManual_MissionMode_Content_3"), false, ref anchorY);
            }
        }
    }
}
