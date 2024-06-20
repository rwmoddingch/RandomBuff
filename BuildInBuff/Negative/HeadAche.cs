using System;
using MonoMod;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HotDogGains.Negative
{
    class HeadAcheBuff : Buff<HeadAcheBuff, HeadAcheBuffData>{public override BuffID  ID => HeadAcheBuffEntry.HeadAcheID;}
    class HeadAcheBuffData :CountableBuffData
    {
        public override BuffID ID => HeadAcheBuffEntry.HeadAcheID;

        public override int MaxCycleCount => 3;
    }
    class HeadAcheBuffEntry : IBuffEntry
    {
        public static BuffID HeadAcheID = new BuffID("HeadAcheID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<HeadAcheBuff,HeadAcheBuffData,HeadAcheBuffEntry>(HeadAcheID);
        }
            public static void HookOn()
        {
            On.Player.Update += Player_Update;
        }
        
        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (Random.Range(0,3000)>2996)
            {
                HeadAcheBuff.Instance.TriggerSelf(true);
                self.Stun(80);
            }
        }
    }
}
