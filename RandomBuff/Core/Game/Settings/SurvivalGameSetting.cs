using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.Game.Settings
{
    internal class SurvivalGameSetting : BaseGameSetting
    {
        public override BuffSettingID ID => BuffSettingID.Survival;
        public override void NewGame()
        {
            CurrentPacket = new CachaPacket(){negative = (5,8,1),positive = (1,3,1)};
        }

        public override void SessionEnd()
        {
            CurrentPacket = new CachaPacket(){negative = (1,2,1)};
        }
    }
}
