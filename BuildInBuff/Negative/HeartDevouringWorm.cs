using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;



namespace BuiltinBuffs.Negative
{
    internal class HeartDevouringWormBuff : Buff<HeartDevouringWormBuff, HeartDevouringWormBuffData>
    {
        public override BuffID ID => HeartDevouringWormBuffEntry.HeartDevouringWorm;

        public HeartDevouringWormBuff()
        {
            origRelationships.Clear();
            foreach (var template in CreatureTemplate.Type.values.entries.Select(i =>
                         StaticWorld.GetCreatureTemplate(new CreatureTemplate.Type(i)))
                         .Where(NeedReplace))
            {
                origRelationships.Add(template.type, template.relationships[CreatureTemplate.Type.Fly.Index]);
                template.relationships[CreatureTemplate.Type.Fly.Index] =
                    new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.9f);
            }
        }



        public override void Destroy()
        {
            base.Destroy();
            foreach (var template in CreatureTemplate.Type.values.entries.Select(i =>
                             StaticWorld.GetCreatureTemplate(new CreatureTemplate.Type(i)))
                         .Where(NeedReplace))
            {
                if (origRelationships.TryGetValue(template.type, out var relationship))
                    template.relationships[CreatureTemplate.Type.Fly.index] = relationship;
            }
        }

        private readonly Dictionary<CreatureTemplate.Type, CreatureTemplate.Relationship> origRelationships
            = new Dictionary<CreatureTemplate.Type, CreatureTemplate.Relationship>();

        private static bool NeedReplace(CreatureTemplate template)
        {
            return template.IsLizard || template.IsVulture;
        }
    }

    internal class HeartDevouringWormBuffData : BuffData
    {
        public override BuffID ID => HeartDevouringWormBuffEntry.HeartDevouringWorm;
    }

    internal class HeartDevouringWormBuffEntry : IBuffEntry
    {
        public static readonly BuffID HeartDevouringWorm = new BuffID(nameof(HeartDevouringWorm), true);

        public static readonly FlyAI.Behavior FollowCrit = new FlyAI.Behavior($"HeartDevouringWorm.{nameof(FollowCrit)}", true);


        public static readonly ConditionalWeakTable<FlyAI, FlyModule> Modules = new ConditionalWeakTable<FlyAI, FlyModule>();

        public static readonly ConditionalWeakTable<Creature, FlyExplodeModule> ExplodeModules = new ConditionalWeakTable<Creature, FlyExplodeModule>();


        public void OnEnable()
        {
            BuffRegister.RegisterBuff<HeartDevouringWormBuff, HeartDevouringWormBuffData, HeartDevouringWormBuffEntry>(
                HeartDevouringWorm);
        }

        public static void HookOn()
        {
            On.FlyAI.ConsiderOtherCreature += FlyAI_ConsiderOtherCreature;
            On.FlyAI.Update += FlyAI_Update;
            On.FlyAI.ctor += FlyAI_ctor;

            On.Creature.Grab += Creature_Grab;
        }

        private static FlyExplodeModule CreateExplodeModule(Creature crit)
        {
            return ExplodeModules.GetValue(crit, (i) => new FlyExplodeModule(i));
        }

        private static bool IsInfected(Creature crit)
        {
            return ExplodeModules.TryGetValue(crit, out _);
        }

        private static bool Creature_Grab(On.Creature.orig_Grab orig, Creature self, PhysicalObject obj, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool overrideEquallyDominant, bool pacifying)
        {
            var re = orig(self,obj,graspUsed,chunkGrabbed,shareability, dominance, overrideEquallyDominant, pacifying);
            if (self is Player)
                return re;
            else if (obj is Fly fly)
            {
                fly.Die();
                fly.Destroy();
                self.ReleaseGrasp(graspUsed);
                CreateExplodeModule(self);
                return false;
            }

            return re;
        }

        private static void FlyAI_ctor(On.FlyAI.orig_ctor orig, FlyAI self, Fly fly, World world)
        {
            if (!Modules.TryGetValue(self, out _))
                Modules.Add(self, new FlyModule(self));
            orig(self, fly, world);
        }

        private static void FlyAI_Update(On.FlyAI.orig_Update orig, FlyAI self)
        {

            if (Modules.TryGetValue(self, out var module))
                module.Update();
            orig(self);
        }

        private static void FlyAI_ConsiderOtherCreature(On.FlyAI.orig_ConsiderOtherCreature orig, FlyAI self, AbstractCreature crit)
        {
            if (Modules.TryGetValue(self, out var module))
                if (module.ConsiderOtherCreature(crit))
                    return;

            orig(self, crit);

        }

        public class FlyModule
        {
            private Creature crit;
            private readonly WeakReference<FlyAI> flyRef;
            public FlyModule(FlyAI ai)
            {
                flyRef = new WeakReference<FlyAI>(ai);
            }

            public bool ConsiderOtherCreature(AbstractCreature newCrit)
            {
                if (!flyRef.TryGetTarget(out var ai))
                    return false;
                var template = StaticWorld.GetCreatureTemplate(newCrit.creatureTemplate.type);
                if (template.relationships[CreatureTemplate.Type.Fly.Index].type ==
                    CreatureTemplate.Relationship.Type.Eats &&
                    newCrit.realizedCreature != null &&
                    IsInfected(newCrit.realizedCreature))
                {
                    if ((crit == null || Custom.Dist(ai.fly.DangerPos, crit.DangerPos) >
                        Custom.Dist(ai.fly.DangerPos, newCrit.realizedCreature.DangerPos)) &&
                        Custom.DistLess(ai.fly.DangerPos, newCrit.realizedCreature.DangerPos,200))
                    {
                        crit = newCrit.realizedCreature;
                    }

                    return true;
                }

                return false;
            }

            public void Update()
            {
                if (!flyRef.TryGetTarget(out var ai)) return;

                if (crit != null && ai.room == crit.room && ai.behavior != FollowCrit)
                    ai.behavior = FollowCrit;
                else if (crit != null && (crit.room != ai.room || IsInfected(crit)))
                {
                    crit = null;
                    ai.behavior = FlyAI.Behavior.Idle;
                }

                if (ai.behavior == FollowCrit)
                {
                    if (Random.value * 7f > Vector2.Distance(crit.mainBodyChunk.lastPos, crit.mainBodyChunk.pos))
                    {
                        Vector2 vector = crit.mainBodyChunk.pos + Custom.RNV() * Mathf.Pow(Random.value, 3f) * 120f;
                        if (!ai.room.GetTile(vector).Solid && ai.room.VisualContact(vector, (crit.graphicsModule as PlayerGraphics).tail[3].pos))
                            ai.localGoal = vector;
                        
                    }
                    ai.followingDijkstraMap = -1;
                    if (Random.value < 0.5f && Custom.DistLess(crit.mainBodyChunk.lastPos, ai.fly.firstChunk.pos, 9f))
                    {
                        ai.fly.firstChunk.vel += Custom.RNV() * 6f;
                        crit.room.PlaySound(SoundID.Fly_Lure_Ruffled_By_Fly, crit.mainBodyChunk);
                    }
                }
            }
        }




        public class FlyExplodeModule
        {
            private readonly WeakReference<AbstractCreature> critRef;
            private int counter = 200;

            public FlyExplodeModule(Creature crit)
            {
                critRef = new WeakReference<AbstractCreature>(crit.abstractCreature);
            }

            public void Update()
            {
                if (!critRef.TryGetTarget(out var self))
                    return;

                counter--;
                if (counter == 0)
                {

                }

            }

            public void AbstractUpdate()
            {

            }

            public void Explode()
            {

            }
        }
    }
}

