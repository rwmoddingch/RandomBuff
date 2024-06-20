using System;
using MonoMod;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Runtime.CompilerServices;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;

namespace TemplateGains
{
    class DequantizeBuff : Buff<DequantizeBuff, DequantizeBuffData>{public override BuffID  ID => DequantizeBuffEntry.DequantizeID;}
    class DequantizeBuffData :BuffData{public override BuffID ID => DequantizeBuffEntry.DequantizeID;}
    class DequantizeBuffEntry : IBuffEntry
    {
        public static BuffID DequantizeID = new BuffID("DequantizeID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<DequantizeBuff,DequantizeBuffData,DequantizeBuffEntry>(DequantizeID);
        }
            public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
        }
        public static ConditionalWeakTable<Player, MinFood> module = new ConditionalWeakTable<Player, MinFood>();
        
        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (!module.TryGetValue(self, out var min))
            {
                module.Add(self, new MinFood(self));
                return;
            }

            if (min.food < self.playerState.foodInStomach || min.littleFood < self.playerState.quarterFoodPoints)
            {
                min.resetMin();
                if (Random.Range(0, 5) > 3)
                {
                    DequantizeBuff.Instance.TriggerSelf(true);
                    self.SubtractFood(1);
                }
            }
            else
            {
                min.resetMin();
            }
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig.Invoke(self, abstractCreature, world);
            module.Add(self, new MinFood(self));
        }

    }
    public class MinFood
    
    {
        public int food = 0;
        public int littleFood = 0;
        public Player player;

        public void resetMin()
        {
            food = player.playerState.foodInStomach;
            littleFood = player.playerState.quarterFoodPoints;
        }
        public MinFood(Player self)
        {
            player = self;
            resetMin();
        }
    }
    
}
