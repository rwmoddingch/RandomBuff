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
    internal class GrindingTeethBuff : Buff<GrindingTeethBuff, GrindingTeethBuffData>
    {
        public override BuffID ID => GrindingTeethBuffEntry.GrindingTeeth;
        
        public GrindingTeethBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    GrindingTeeth grindingTeeth = new GrindingTeeth(player);
                    GrindingTeethBuffEntry.GrindingTeethFeatures.Add(player, grindingTeeth);
                }
            }
        }
    }

    internal class GrindingTeethBuffData : BuffData
    {
        public override BuffID ID => GrindingTeethBuffEntry.GrindingTeeth;
    }

    internal class GrindingTeethBuffEntry : IBuffEntry
    {
        public static BuffID GrindingTeeth = new BuffID("GrindingTeeth", true);
        public static ConditionalWeakTable<Player, GrindingTeeth> GrindingTeethFeatures = new ConditionalWeakTable<Player, GrindingTeeth>();

        public static int StackLayer
        {
            get
            {
                return GrindingTeeth.GetBuffData().StackLayer;
            }
        }

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<GrindingTeethBuff, GrindingTeethBuffData, GrindingTeethBuffEntry>(GrindingTeeth);
        }
        
        public static void HookOn()
        {
            //IL.Player.CanMaulCreature += Player_CanMaulCreatureIL;
            //On.Player.IsCreatureLegalToHoldWithoutStun += Player_IsCreatureLegalToHoldWithoutStun;
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;

            On.SlugcatStats.SlugcatCanMaul += SlugcatStats_SlugcatCanMaul;
            On.Creature.Violence += Creature_Violence;
        }
        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!GrindingTeethFeatures.TryGetValue(self, out _))
            {
                GrindingTeeth grindingTeeth = new GrindingTeeth(self);
                GrindingTeethFeatures.Add(self, grindingTeeth);
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);

            if (GrindingTeethFeatures.TryGetValue(self, out var grindingTeeth))
            {
                if(grindingTeeth.abilities.Count != StackLayer)
                {
                    for(int i = grindingTeeth.abilities.Count; i < StackLayer; i++)
                    {
                        grindingTeeth.AddAbility();
                    }
                }
            }
        }

        public static bool SlugcatStats_SlugcatCanMaul(On.SlugcatStats.orig_SlugcatCanMaul orig, SlugcatStats.Name slugcatNum)
        {
            bool result = orig(slugcatNum);
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                                 .Where(i => i != null && i.graphicsModule != null))
                {
                    if (GrindingTeethFeatures.TryGetValue(player, out var grindingTeeth) &&
                        player.SlugCatClass == slugcatNum &&
                        grindingTeeth.abilities.Contains(Positive.GrindingTeeth.Ability.CanMaul))
                    {
                        result = true;
                    }
                }
            }
            return result;
        }

        public static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (source != null && source.owner is Player && type == Creature.DamageType.Bite &&
                GrindingTeethFeatures.TryGetValue(source.owner as Player, out var grindingTeeth) &&
                grindingTeeth.abilities.Contains(Positive.GrindingTeeth.Ability.IncreaseDamage))
            {
                int num = 0;
                foreach (GrindingTeeth.Ability a in grindingTeeth.abilities)
                {
                    if(a == Positive.GrindingTeeth.Ability.IncreaseDamage)
                        num++;
                }
                damage *= 1f + 0.5f * num;
            }
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        public static bool Player_IsCreatureLegalToHoldWithoutStun(On.Player.orig_IsCreatureLegalToHoldWithoutStun orig, Player self, Creature grabCheck)
        {
            bool result = orig(self, grabCheck);
            if (GrindingTeethFeatures.TryGetValue(self, out var grindingTeeth) &&
                grindingTeeth.abilities.Contains(Positive.GrindingTeeth.Ability.StrengthenMaul))
            {
                result = true;
            }
            return result;
        }

        public static void Player_CanMaulCreatureIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(MoveType.After,
                    (i) => i.MatchCallvirt<Creature>("get_Stunned")))
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate<Func<bool, Player, bool>>((Stunned, self) =>
                    {
                        if (GrindingTeethFeatures.TryGetValue(self, out var grindingTeeth) &&
                            grindingTeeth.abilities.Contains(Positive.GrindingTeeth.Ability.StrengthenMaul))
                        {
                            return true;
                        }
                        return Stunned;
                    });
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }
    }
    internal class GrindingTeeth
    {
        WeakReference<Player> ownerRef;
        public List<Ability> abilities;

        public GrindingTeeth(Player owner)
        {
            ownerRef = new WeakReference<Player>(owner);
            abilities = new List<Ability>();
            for(int i = 0; i < GrindingTeethBuffEntry.StackLayer; i++)
            {
                this.AddAbility();
            }
        }

        public void AddAbility()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (!SlugcatStats.SlugcatCanMaul(player.SlugCatClass))
            {
                abilities.Add(Ability.CanMaul);
            }
            else if (!abilities.Contains(Ability.IncreaseDamage))
            {
                abilities.Add(Ability.IncreaseDamage);
            }
            else if (!abilities.Contains(Ability.StrengthenMaul))
            {
                abilities.Add(Ability.StrengthenMaul);
            }
            else
            {
                abilities.Add(Ability.IncreaseDamage);
            }
        }

        internal class Ability : ExtEnum<Ability>
        {
            public Ability(string value, bool register = false) : base(value, register)
            {

            }

            public static readonly Ability CanMaul = new Ability("CanMaul", false);
            public static readonly Ability IncreaseDamage = new Ability("IncreaseDamage", false);
            public static readonly Ability StrengthenMaul = new Ability("StrengthenMaul", false);
        }
    }
}
