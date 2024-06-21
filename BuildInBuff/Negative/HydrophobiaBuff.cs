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
                hydrophobia.airInLungs = self.airInLungs;
                hydrophobia.subAirInLungs = hydrophobia.lastAirInLungs - hydrophobia.airInLungs;
                self.airInLungs -= hydrophobia.subAirInLungs;
            }
        }
    }

    internal class Hydrophobia
    {
        WeakReference<Player> ownerRef;

        public float airInLungs;
        public float lastAirInLungs;
        public float subAirInLungs;

        public Hydrophobia(Player player)
        {
            ownerRef = new WeakReference<Player>(player);
        }
    }
}
