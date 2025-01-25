using MonoMod.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Mono.Cecil.Cil;
using BuiltinBuffs.Duality;
using BuiltinBuffs.Positive;
using RandomBuff;
using RWCustom;

namespace BuiltinBuffs.Negative
{
    internal class HydrophobiaBuff : Buff<HydrophobiaBuff, HydrophobiaBuffData>
    {
        public override BuffID ID => HydrophobiaBuffEntry.Hydrophobia;
        public HydrophobiaBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    if (HydrophobiaBuffEntry.HydrophobiaFeatures.TryGetValue(player, out _))
                        HydrophobiaBuffEntry.HydrophobiaFeatures.Remove(player);
                    var hydrophobia = new Hydrophobia(player);
                    HydrophobiaBuffEntry.HydrophobiaFeatures.Add(player, hydrophobia);
                }
            }
        }
    }

    internal class HydrophobiaBuffData : BuffData
    {
        public override BuffID ID => HydrophobiaBuffEntry.Hydrophobia;
    }

    internal class HydrophobiaBuffEntry : IBuffEntry
    {
        public static BuffID Hydrophobia = new BuffID("Hydrophobia", true);

        public static ConditionalWeakTable<Player, Hydrophobia> HydrophobiaFeatures = new ConditionalWeakTable<Player, Hydrophobia>();

        public static int StackLayer
        {
            get
            {
                return Hydrophobia.GetBuffData()?.StackLayer ?? 0;
            }
        }

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<HydrophobiaBuff, HydrophobiaBuffData, HydrophobiaBuffEntry>(Hydrophobia);
        }

        public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!HydrophobiaFeatures.TryGetValue(self, out _))
                HydrophobiaFeatures.Add(self, new Hydrophobia(self));
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            if (HydrophobiaFeatures.TryGetValue(self, out var hydrophobia))
            {
                hydrophobia.lastAirInLungs = self.airInLungs;
            }

            orig(self, eu);

            if (HydrophobiaFeatures.TryGetValue(self, out hydrophobia))
            {
                hydrophobia.Update();
            }
        }
    }

    internal class Hydrophobia
    {
        WeakReference<Player> ownerRef;

        public float airInLungs;
        public float lastAirInLungs;
        public float subAirInLungs;
        private int fearCount;

        public Hydrophobia(Player player)
        {
            ownerRef = new WeakReference<Player>(player);
        }

        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;

            this.airInLungs = player.airInLungs;
            this.subAirInLungs = this.lastAirInLungs - this.airInLungs;
            //player.airInLungs -= this.subAirInLungs;
            player.airInLungs = this.lastAirInLungs - this.subAirInLungs * (1f + (HydrophobiaBuffEntry.StackLayer - 1f) * 0.5f);

            if (player.animation == Player.AnimationIndex.SurfaceSwim || player.animation == Player.AnimationIndex.DeepSwim)
            {
                fearCount = 1200;
            }
            if (fearCount > 0)
            {
                fearCount--;
                Vector2 addPos = Vector2.Lerp(player.bodyChunks[0].pos - player.bodyChunks[0].lastPos, player.bodyChunks[1].pos - player.bodyChunks[1].lastPos, 0.5f) *
                             Mathf.Max(1f - Mathf.Pow(0.9f, HydrophobiaBuffEntry.StackLayer - 1), 0.5f);
                //效果即将结束时给一个缓冲
                if (fearCount < 120)
                    addPos *= (float)fearCount / 120f;
                player.bodyChunks[0].pos -= addPos;
                player.bodyChunks[1].pos -= addPos;
            }
        }
    }
}
