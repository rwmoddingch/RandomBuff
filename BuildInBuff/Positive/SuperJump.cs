using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotDogGains.Positive
{
    class SuperJumpBuff : Buff<SuperJumpBuff, SuperJumpBuffData>{public override BuffID  ID => SuperJumpBuffEntry.SuperJumpID;}
    class SuperJumpBuffData :BuffData{public override BuffID ID => SuperJumpBuffEntry.SuperJumpID;}
    class SuperJumpBuffEntry : IBuffEntry
    {
        public static BuffID SuperJumpID = new BuffID("SuperJumpID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SuperJumpBuff,SuperJumpBuffData,SuperJumpBuffEntry>(SuperJumpID);
        }
            public static void HookOn()
        {
            On.Player.Jump += Player_Jump;
        }

        private static void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            orig.Invoke(self);
            self.bodyChunks[0].vel.y = 8 + (0.3f * BuffCore.GetBuffData(SuperJumpID).StackLayer);//¿¨ÅÆµÄ¶ÑµþÊýÁ¿);
        }
    }
}