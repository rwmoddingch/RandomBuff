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
                    if (WaterDancerBuffEntry.WaterDancerFeatures.TryGetValue(player, out _))
                        WaterDancerBuffEntry.WaterDancerFeatures.Remove(player);
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

        public static float Level
        {
            get
            {
                return 1f + (StackLayer - 1f) * 0.5f;
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
            On.Player.MovementUpdate += Player_MovementUpdate;
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
            else
            {
                WaterDancerFeatures.Add(self, new WaterDancer(self));
            }

            orig(self, eu);

            if (WaterDancerFeatures.TryGetValue(self, out waterdancer))
            {
                waterdancer.airInLungs = self.airInLungs;
                waterdancer.subAirInLungs = waterdancer.lastAirInLungs - waterdancer.airInLungs;
                self.airInLungs = waterdancer.lastAirInLungs - 1f / (1f + 1f * Level) * waterdancer.subAirInLungs;
                if (self.animation == Player.AnimationIndex.SurfaceSwim || 
                    self.animation == Player.AnimationIndex.DeepSwim || 
                    waterdancer.waterFlip)
                {
                    Vector2 addPos = Vector2.Lerp(self.bodyChunks[0].pos - self.bodyChunks[0].lastPos, self.bodyChunks[1].pos - self.bodyChunks[1].lastPos, 0.5f) * (1f * Level);
                    self.bodyChunks[0].pos += addPos;
                    self.bodyChunks[1].pos += addPos;
                    //辅助转向
                    if (self.input[0].x == 0)
                    {
                        self.bodyChunks[0].vel.x *= 0.75f;
                        self.bodyChunks[1].vel.x *= 0.75f;
                    }
                    if (self.input[0].y == 0)
                    {
                        self.bodyChunks[0].vel.y *= 0.75f;
                        self.bodyChunks[1].vel.y *= 0.75f;
                    }
                    //水中后空翻（？）
                    if (self.input[0].x * self.bodyChunks[0].vel.x < 0)
                    {
                        if (self.bodyChunks[0].vel.x > 2f)
                            self.bodyChunks[0].vel.x *= 0.5f;
                        if (self.wantToJump > 0 || self.input[0].y != 0)
                        {
                            self.bodyChunks[0].vel.x += 8f * self.input[0].x;
                            self.bodyChunks[0].vel.y += 4f * (self.input[0].y == 0 ? 1f : self.input[0].y);
                            if (self.wantToJump > 0)
                            {
                                waterdancer.waterFlip = true;
                            }
                        }
                        else
                            self.bodyChunks[0].vel.x += 2f * self.input[0].x;
                    }
                    if (self.input[0].y * self.bodyChunks[0].vel.y < 0)
                    {
                        if (self.bodyChunks[0].vel.y > 2f)
                            self.bodyChunks[0].vel.y *= 0.5f;
                        if (self.wantToJump > 0 || self.input[0].x != 0)
                        {
                            self.bodyChunks[0].vel.x += 4f * (self.input[0].x == 0 ? 1f : self.input[0].x);
                            self.bodyChunks[0].vel.y += 8f * self.input[0].y;
                            if (self.wantToJump > 0)
                            {
                                waterdancer.waterFlip = true;
                            }
                        }
                        else
                            self.bodyChunks[0].vel.y += 2f * self.input[0].y;
                    }
                }
                if (!self.submerged || waterdancer.waterFlipCount > 30)
                {
                    waterdancer.waterFlip = false;
                    waterdancer.waterFlipCount = 0; 
                    //self.waterFriction = waterdancer.origWaterFriction;
                }
            }
        }

        private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            if (WaterDancerFeatures.TryGetValue(self, out var waterdancer))
            {
                if (waterdancer.waterFlip && self.submerged)
                {
                    self.animation = Player.AnimationIndex.Flip;
                    //self.waterFriction = 0f;
                    waterdancer.waterFlipCount++;
                    //BuffPlugin.Log("waterdancer.waterFlipCount: " + waterdancer.waterFlipCount);
                }
            }
            orig(self, eu);
        }
    }
    internal class WaterDancer
    {
        WeakReference<Player> ownerRef;

        public float airInLungs;
        public float lastAirInLungs;
        public float subAirInLungs;

        public bool waterFlip;
        public int waterFlipCount;
        public float origWaterFriction;

        public WaterDancer(Player player)
        {
            ownerRef = new WeakReference<Player>(player);
            lastAirInLungs = player.airInLungs;
            waterFlip = false;
            waterFlipCount = 0;
            origWaterFriction = player.waterFriction;
        }
    }
}
