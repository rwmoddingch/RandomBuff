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

        public static int StackLayer
        {
            get
            {
                return DrainLife.GetBuffData().StackLayer;
            }
        }

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

            if (DrainLifeFeatures.TryGetValue(self, out var drainLife) &&
                drainLife.LastHealth != -1)
            {
                if (self.killTag != null &&
                    self.killTag.realizedCreature != null &&
                    self.killTag.realizedCreature is Player)
                {
                    Player player = self.killTag.realizedCreature as Player;
                    float damage = drainLife.LastHealth - (self.State as HealthState).health;
                    for (int i = 0; i < Mathf.FloorToInt(StackLayer * damage / 0.25f); i++)
                    {
                        player.AddQuarterFood();
                    }
                }
                drainLife.LastHealth = (self.State as HealthState).health;
            }
        }
    }


    internal class DrainLife
    {
        WeakReference<Creature> ownerRef;
        float lastHealth;

        public float LastHealth
        {
            get
            {
                return this.lastHealth;
            }
            set
            {
                this.lastHealth = value;
            }
        }

        public DrainLife(Creature c)
        {
            ownerRef = new WeakReference<Creature>(c);
            if (c.State is HealthState)
                lastHealth = (c.State as HealthState).health;
            else
                lastHealth = -1;
        }
    }
}
