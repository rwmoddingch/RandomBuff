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
using BuiltinBuffs.Duality;
using static BuiltinBuffs.Positive.SuperCoyoteJumpBuffEntry;

namespace BuiltinBuffs.Positive
{
    internal class SuperCoyoteJumpBuff : Buff<SuperCoyoteJumpBuff, SuperCoyoteJumpBuffData>
    {
        public override BuffID ID => SuperCoyoteJumpBuffEntry.SuperCoyoteJump;
        
        public SuperCoyoteJumpBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    if (SuperCoyoteJumpBuffEntry.SuperCoyoteJumpFeatures.TryGetValue(player, out _))
                        SuperCoyoteJumpBuffEntry.SuperCoyoteJumpFeatures.Remove(player);
                    var superCoyoteJump = new SuperCoyoteJump(player);
                    SuperCoyoteJumpBuffEntry.SuperCoyoteJumpFeatures.Add(player, superCoyoteJump);
                }
            }
        }
    }

    internal class SuperCoyoteJumpBuffData : BuffData
    {
        public override BuffID ID => SuperCoyoteJumpBuffEntry.SuperCoyoteJump;
    }

    internal class SuperCoyoteJumpBuffEntry : IBuffEntry
    {
        public static BuffID SuperCoyoteJump = new BuffID("SuperCoyoteJump", true);
        public static ConditionalWeakTable<Player, SuperCoyoteJump> SuperCoyoteJumpFeatures = new ConditionalWeakTable<Player, SuperCoyoteJump>();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SuperCoyoteJumpBuff, SuperCoyoteJumpBuffData, SuperCoyoteJumpBuffEntry>(SuperCoyoteJump);
        }
        
        public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!SuperCoyoteJumpFeatures.TryGetValue(self, out _))
                SuperCoyoteJumpFeatures.Add(self, new SuperCoyoteJump(self));
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);

            if (SuperCoyoteJumpFeatures.TryGetValue(self, out var superCoyoteJump) && self.canJump > 0)
            {
                if (superCoyoteJump.canJumpAddCount >= 1)
                {
                    superCoyoteJump.canJumpAddCount = 0;
                    self.canJump++;
                }
                else
                    superCoyoteJump.canJumpAddCount++;
            }
        }
    }

    internal class SuperCoyoteJump
    {
        WeakReference<Player> ownerRef;
        public int canJumpAddCount;

        public SuperCoyoteJump(Player player)
        {
            ownerRef = new WeakReference<Player>(player);
            canJumpAddCount = 0;
        }
    }
}
