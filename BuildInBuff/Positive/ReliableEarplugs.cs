using System;
using MonoMod;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TemplateGains
{
    //บรถ๚ศ๛
    class ReliableEarplugsBuff : Buff<ReliableEarplugsBuff, ReliableEarplugsBuffData> { public override BuffID ID => ReliableEarplugsBuffEntry.ReliableEarplugsID; }
    class ReliableEarplugsBuffData : BuffData { public override BuffID ID => ReliableEarplugsBuffEntry.ReliableEarplugsID; }
    class ReliableEarplugsBuffEntry : IBuffEntry
    {
        public static BuffID ReliableEarplugsID = new BuffID("ReliableEarplugsID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ReliableEarplugsBuff, ReliableEarplugsBuffData, ReliableEarplugsBuffEntry>(ReliableEarplugsID);
        }
        public static void HookOn()
        {
            On.Player.Deafen += Player_Deafen;
        }

        private static void Player_Deafen(On.Player.orig_Deafen orig, Player self, int df)
        {

        }
    }

}
