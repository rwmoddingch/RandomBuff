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
using MoreSlugcats;
using Mono.Cecil;

namespace BuiltinBuffs.Positive
{
    internal class DrainLifeBuff : Buff<DrainLifeBuff, DrainLifeBuffData>
    {
        public override BuffID ID => DrainLifeBuffEntry.DrainLife;

        public DrainLifeBuff()
        {

        }
    }

    internal class DrainLifeBuffData : BuffData
    {
        public override BuffID ID => DrainLifeBuffEntry.DrainLife;
    }

    internal class DrainLifeBuffEntry : IBuffEntry
    {
        public static BuffID DrainLife = new BuffID("DrainLife", true);
        public static ConditionalWeakTable<Creature, DrainLife> DrainLifeFeatures = new ConditionalWeakTable<Creature, DrainLife>();

        public static int StackLayer => DrainLife.GetBuffData()?.StackLayer ?? 0;

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<DrainLifeBuff, DrainLifeBuffData, DrainLifeBuffEntry>(DrainLife);
        }

        public static void HookOn()
        {
            On.Creature.ctor += Creature_ctor;
            On.Creature.Update += Creature_Update;
        }

        private static void Creature_ctor(On.Creature.orig_ctor orig, Creature self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!DrainLifeFeatures.TryGetValue(self, out _))
            {
                DrainLife drainLife = new DrainLife(self);
                DrainLifeFeatures.Add(self, drainLife);
            }
        }

        private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
        {
            orig(self, eu);

            if (DrainLifeFeatures.TryGetValue(self, out var drainLife))
            {
                if (drainLife.LastHealth != -1)
                {
                    if (self.killTag != null &&
                        self.killTag.realizedCreature != null &&
                        self.killTag.realizedCreature is Player)
                    {
                        Player player = self.killTag.realizedCreature as Player;
                        float damage = Mathf.Min(drainLife.LastHealth - (self.State as HealthState).health, drainLife.LastHealth);
                        for (int i = 0; i < Mathf.FloorToInt(StackLayer * damage / 0.25f); i++)
                        {
                            player.AddQuarterFood();
                        }
                    }

                    drainLife.LastHealth = Mathf.Max((self.State as HealthState).health, 0f);
                }
                if (!self.abstractCreature.state.alive)
                {
                    DrainLifeFeatures.Remove(self);
                    return;
                }

            }
            else if (!DrainLifeFeatures.TryGetValue(self, out _) &&
                     self.abstractCreature.state is HealthState && 
                     self.abstractCreature.state.alive)
            {
                DrainLifeFeatures.Add(self, new DrainLife(self));
            }
        }
    }


    internal class DrainLife
    {
        WeakReference<Creature> ownerRef;

        public float LastHealth { get; set; }

        public DrainLife(Creature c)
        {
            ownerRef = new WeakReference<Creature>(c);
            if (c.State is HealthState state)
                LastHealth = state.health;
            else
                LastHealth = -1;
        }
    }
}
