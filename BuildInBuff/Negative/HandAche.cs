using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Runtime.CompilerServices;
using System.Net.Configuration;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;

namespace HotDogGains.Negative
{
    class HandAcheBuff : Buff<HandAcheBuff, HandAcheBuffData>{public override BuffID  ID => HandAcheBuffEntry.HandAcheID;}
    class HandAcheBuffData :CountableBuffData
    {
        public override BuffID ID => HandAcheBuffEntry.HandAcheID;

        public override int MaxCycleCount => 3;
    }
    class HandAcheBuffEntry : IBuffEntry
    {
        public static BuffID HandAcheID = new BuffID("HandAcheID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<HandAcheBuff,HandAcheBuffData,HandAcheBuffEntry>(HandAcheID);
        }
        public static void HookOn()
        {
            On.Player.Update += Player_Update;
        }

        public static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if(modules.TryGetValue(self, out HandAcheModule module))
            {
                module.NeedRelax();
            }
            else modules.Add(self, new HandAcheModule(self));
            
            

        }
        public static ConditionalWeakTable<Player, HandAcheModule> modules = new ConditionalWeakTable<Player, HandAcheModule>(); 
    }

    internal class HandAcheModule
    {
        public int[] hands = new int[2] { 0,0};//用于记录拿东西的疲惫值
        public int climb = 0;//用于记录攀爬的疲惫值
        public Player self;//作用的玩家

        //计算双手的疲劳
        public void twoHandsTired()
        {
            if (self.FreeHand() != -1)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (self.grasps[i] != null) hands[i]++;//给拿东西的手增加疲劳点数
                    else hands[i] = 0;//如果没拿东西清空手疲劳
                }
            }
            else hands[0] = hands[1] = 0;//如果没拿东西清空双手疲劳
        }

        //计算攀爬的疲劳
        public void climbTired()
        {
            if (self.animation == Player.AnimationIndex.HangFromBeam || self.animation == Player.AnimationIndex.AntlerClimb || self.animation == Player.AnimationIndex.HangUnderVerticalBeam|| self.animation == Player.AnimationIndex.VineGrab||self.animation == Player.AnimationIndex.ClimbOnBeam)
            {
                climb++;
            }
            else climb = 0;
        }


        public void NeedRelax()
        {
            twoHandsTired();
            climbTired();

            for (int i = 0; i < 2; i++)
            {
                if (hands[i]>=200)
                {
                    HandAcheBuff.Instance.TriggerSelf(true);
                    self.grasps[i].Release();
                    hands[i] = 0;
                }
            }

            if (climb>=200)
            {
                HandAcheBuff.Instance.TriggerSelf(true);
                self.animation = Player.AnimationIndex.None;
                climb= 0;
            }


        }
        public HandAcheModule(Player player) { this.self = player; }
    }
}
