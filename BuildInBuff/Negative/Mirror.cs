using System;
using MonoMod;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TemplateGains
{

    class MirrorBuff : Buff<MirrorBuff, MirrorBuffData> { public override BuffID ID => MirrorBuffEntry.MirrorID; }
    class MirrorBuffData : BuffData { public override BuffID ID => MirrorBuffEntry.MirrorID; }
    class MirrorBuffEntry : IBuffEntry
    {
        public static BuffID MirrorID = new BuffID("MirrorID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<MirrorBuff, MirrorBuffData, MirrorBuffEntry>(MirrorID);
        }
        public static void HookOn()
        {
            //��ȡ��������ʱ��תx������
            On.RWInput.PlayerInput_int += RWInput_PlayerInput_int; ;
        }

        private static Player.InputPackage RWInput_PlayerInput_int(On.RWInput.orig_PlayerInput_int orig, int playerNumber)
        {
            var self = orig.Invoke(playerNumber);
            self.x *= -1;
            self.analogueDir.x *= -1;
            self.downDiagonal *= -1;

            return self;
        }

        //private static Player.InputPackage RWInput_PlayerInput(On.RWInput.orig orig, int playerNumber, RainWorld rainWorld)
        //{
            
        //}

    }
}
