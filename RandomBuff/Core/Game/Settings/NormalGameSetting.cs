using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.Game.Settings
{
    internal class NormalGameSetting : BaseGameSetting
    {
        public override BuffSettingID ID => BuffSettingID.Normal;

        public override void NewGame()
        {
            CurrentPacket = new CachaPacket();
        }

        public override void SessionEnd()
        {
            CurrentPacket = new CachaPacket() { positive = (1, 3, 1), negative = (1, 3, 1) };
        }
    }
}
