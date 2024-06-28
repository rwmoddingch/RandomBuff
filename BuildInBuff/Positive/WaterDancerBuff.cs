using RandomBuff;
using RandomBuff.Core.Game;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RWCustom;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using BuiltinBuffs.Duality;
using BuiltinBuffs.Negative;

namespace BuiltinBuffs.Positive
{
    internal class WaterDancerBuff : Buff<WaterDancerBuff, WaterDancerBuffData>
    {
        public override BuffID ID => WaterDancerBuffEntry.WaterDancer;
        
        public WaterDancerBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    var waterdancer = new WaterDancer(player);
                    WaterDancerBuffEntry.WaterDancerFeatures.Add(player, waterdancer);
                }
            }
        }
    }

    internal class WaterDancerBuffData : BuffData
    {
        public override BuffID ID => WaterDancerBuffEntry.WaterDancer;
    }

    internal class WaterDancerBuffEntry : IBuffEntry
    {
        public static BuffID WaterDancer = new BuffID("WaterDancer", true);
        public static ConditionalWeakTable<Player, WaterDancer> WaterDancerFeatures = new ConditionalWeakTable<Player, WaterDancer>();

        public static int StackLayer
        {
            get
            {
                return WaterDancer.GetBuffData().StackLayer;
            }
        }

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<WaterDancerBuff, WaterDancerBuffData, WaterDancerBuffEntry>(WaterDancer);
        }
        
        public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!WaterDancerFeatures.TryGetValue(self, out _))
                WaterDancerFeatures.Add(self, new WaterDancer(self));
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            if (WaterDancerFeatures.TryGetValue(self, out var waterdancer))
            {
                waterdancer.lastAirInLungs = self.airInLungs;
            }

            orig(self, eu);

            if (WaterDancerFeatures.TryGetValue(self, out waterdancer))
            {
                waterdancer.airInLungs = self.airInLungs;
                waterdancer.subAirInLungs = waterdancer.lastAirInLungs - waterdancer.airInLungs;
                self.airInLungs = waterdancer.lastAirInLungs - 1f / (1f + 0.3f * StackLayer) * waterdancer.subAirInLungs;
                if (self.animation == Player.AnimationIndex.SurfaceSwim || self.animation == Player.AnimationIndex.DeepSwim)
                {
                    Vector2 addPos = Vector2.Lerp(self.bodyChunks[0].pos - self.bodyChunks[0].lastPos, self.bodyChunks[1].pos - self.bodyChunks[1].lastPos, 0.5f) * (0.3f * StackLayer);
                    self.bodyChunks[0].pos += addPos;
                    self.bodyChunks[1].pos += addPos;
                }
            }
        }
    }
    internal class WaterDancer
    {
        WeakReference<Player> ownerRef;

        public float airInLungs;
        public float lastAirInLungs;
        public float subAirInLungs;

        public WaterDancer(Player player)
        {
            ownerRef = new WeakReference<Player>(player);
        }
    }
}
