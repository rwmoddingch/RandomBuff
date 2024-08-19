using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using BuiltinBuffs.Negative;
using MonoMod;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using TemplateGains;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HotDogGains.Negative
{
    class HeadAcheBuff : Buff<HeadAcheBuff, HeadAcheBuffData> { public override BuffID ID => HeadAcheBuffEntry.HeadAcheID; }
    class HeadAcheBuffData : CountableBuffData
    {
        public override BuffID ID => HeadAcheBuffEntry.HeadAcheID;

        public override int MaxCycleCount => 3;
    }
    class HeadAcheBuffEntry : IBuffEntry
    {
        public static BuffID HeadAcheID = new BuffID("HeadAcheID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<HeadAcheBuff, HeadAcheBuffData, HeadAcheBuffEntry>(HeadAcheID);
        }
        public static void HookOn()
        {
            On.Player.Update += Player_Update;
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);

            self.Ache().AcheUpdate();
            //if (Random.Range(0,3000)>2996)
            //{
            //    HeadAcheBuff.Instance.TriggerSelf(true);
            //    self.Stun(80);
            //}
        }
    }
    public class AcheData
    {
        public int stunCount = Random.Range(40 * 15, 40 * 60);
        public Player player;

        public AcheData(Player player)
        {
            this.player = player;
        }

        public void RollCount()
        {
            stunCount = Random.Range(40 * 30, 40 * 300);
        }
        public void AcheStun()
        {
            HeadAcheBuff.Instance.TriggerSelf();
            player.Stun(80);
            RollCount();
        }
        public void AcheUpdate()
        {
            stunCount--;
            if (stunCount == 5f*40f) BuffPostEffectManager.AddEffect(new DisplacementEffect(0, 5, 1, 5f, 3, 0.04f)); ;

            //if (stunCount < 10 * 40) player.AerobicIncrease(0.2f);
            if (stunCount < 5 * 40) player.Blink(20);
            if (stunCount < 3 * 40) player.slowMovementStun=1;
            if (stunCount <= 0) AcheStun();
        }

    }
    public static class GetData
    {
        public static readonly ConditionalWeakTable<Player, AcheData> modules = new ConditionalWeakTable<Player, AcheData>();

        public static AcheData Ache(this Player player) => modules.GetValue(player, (Player p) => new AcheData(p));
    }
}
