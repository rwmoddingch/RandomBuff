using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Positive;
using MonoMod.RuntimeDetour;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;
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
                if (template.relationships[CreatureTemplate.Type.Fly.Index].type !=
                    CreatureTemplate.Relationship.Type.Eats)
                {
                    origRelationships.Add(template.type, template.relationships[CreatureTemplate.Type.Fly.Index]);
                    template.relationships[CreatureTemplate.Type.Fly.Index] =
                        new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.2f);
                }
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
            return template.IsLizard || template.TopAncestor().type == CreatureTemplate.Type.DaddyLongLegs || template.TopAncestor().type == CreatureTemplate.Type.BigSpider || template.type == CreatureTemplate.Type.DropBug;
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


        private static readonly ConditionalWeakTable<FlyAI, FlyModule> Modules = new ConditionalWeakTable<FlyAI, FlyModule>();

        private static readonly ConditionalWeakTable<AbstractCreature, FlyExplodeModule> ExplodeModules = new ConditionalWeakTable<AbstractCreature, FlyExplodeModule>();

        private static readonly Color FlyRed = Custom.hexToColor("FF334E");
        private static readonly Color FlyDarkRed = Custom.hexToColor("4D0304");


        public void OnEnable()
        {
            BuffRegister.RegisterBuff<HeartDevouringWormBuff, HeartDevouringWormBuffData, HeartDevouringWormBuffEntry>(
                HeartDevouringWorm);
        }
        private static FlyExplodeModule CreateExplodeModule(Creature crit)
        {
            return ExplodeModules.GetValue(crit.abstractCreature, (i) => new FlyExplodeModule(i.realizedCreature));
        }

        private static bool IsInfected(Creature crit)
        {
            return ExplodeModules.TryGetValue(crit.abstractCreature, out _);
        }

        public static void HookOn()
        {
            On.FlyAI.ConsiderOtherCreature += FlyAI_ConsiderOtherCreature;
            On.FlyAI.Update += FlyAI_Update;
            On.FlyAI.ctor += FlyAI_ctor;
            On.Fly.Update += Fly_Update;
            On.Creature.Grab += Creature_Grab;

            On.Creature.Update += Creature_Update;
            On.AbstractCreature.Update += AbstractCreature_Update;

            On.SlugcatStats.AutoGrabBatflys += SlugcatStats_AutoGrabBatflys;

            On.FlyGraphics.DrawSprites += FlyGraphics_DrawSprites;

            On.DaddyAI.IUseARelationshipTracker_UpdateDynamicRelationship += DaddyAI_IUseARelationshipTracker_UpdateDynamicRelationship;
        }

        private static CreatureTemplate.Relationship DaddyAI_IUseARelationshipTracker_UpdateDynamicRelationship(On.DaddyAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, DaddyAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            if (dRelation.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplate.Type.Fly)
                return self.StaticRelationship(dRelation.trackerRep.representedCreature);
            return orig(self,dRelation);
        }

        private static void FlyGraphics_DrawSprites(On.FlyGraphics.orig_DrawSprites orig, FlyGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self,sLeaser,rCam,timeStacker,camPos);
            if (self.fly.AI != null && Modules.TryGetValue(self.fly.AI, out _))
            {
                sLeaser.sprites[3].color = FlyRed;
                for (int i = 0; i < 3; i++)
                    sLeaser.sprites[i].color = FlyDarkRed;
            }
        }

        private static void Fly_Update(On.Fly.orig_Update orig, Fly self, bool eu)
        {
            orig(self, eu);
            if (self.AI != null && Modules.TryGetValue(self.AI, out var module))
                module.BodyUpdate(self);
        }

        private static bool SlugcatStats_AutoGrabBatflys(On.SlugcatStats.orig_AutoGrabBatflys orig, SlugcatStats.Name slugcatNum)
        {
            orig(slugcatNum);
            return false;
        }

        private static void AbstractCreature_Update(On.AbstractCreature.orig_Update orig, AbstractCreature self, int time)
        {
            orig(self, time);
            if(ExplodeModules.TryGetValue(self,out var module))
                module.AbstractUpdate(time);
        }

        private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
        {
            orig(self,eu);
            if(ExplodeModules.TryGetValue(self.abstractCreature,out var module))
                module.Update();
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
            private Creature Crit
            {
                get => crit;
                set
                {
                    if (crit != value)
                    {
                        crit = value;
                        if (flyRef.TryGetTarget(out var fly) && fly.fly.bites != 0)
                        {
                            fly.fly.Die();
                        }
                        playerEatCounter = 0;
                    }
                }
            }

            private Creature crit;
            private readonly WeakReference<FlyAI> flyRef;

            private ParticleEmitter emitter;

            private int playerEatCounter;

            private int MaxPlayerEatCounter = 150;

            private LightSource light;

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
                    !IsInfected(newCrit.realizedCreature))
                {
                    if ((Crit == null || Custom.Dist(ai.fly.DangerPos, Crit.DangerPos) >
                        Custom.Dist(ai.fly.DangerPos, newCrit.realizedCreature.DangerPos)) &&
                        Custom.DistLess(ai.fly.DangerPos, newCrit.realizedCreature.DangerPos, (newCrit.realizedCreature is Player ? 200 : 150)) &&
                        newCrit.state.alive)
                    {
                        Crit = newCrit.realizedCreature;
                    }

                    return true;
                }

                return false;
            }

            public void BodyUpdate(Fly self)
            {
                if (self.dead)
                {
                    if (emitter != null)
                    {
                        emitter.Die();
                        emitter = null;
                    }

                    if (light != null)
                    {
                        light.Destroy();
                        light = null;
                    }

                }
            }


            public void Update()
            {
                if (!flyRef.TryGetTarget(out var ai) || !ai.fly.Consious) return;
                if (emitter == null && ai.fly.room != null && !ai.fly.inShortcut && !ai.fly.dead)
                {
                    emitter = new ParticleEmitter(ai.fly.room);
                    emitter.ApplyEmitterModule(new BindEmitterToPhysicalObject(emitter, ai.fly));

                    emitter.ApplyParticleSpawn(new RandomRateSpawnerModule(emitter, 50, 1.2f,3f));

                    emitter.ApplyParticleModule(new SetRandomLife(emitter, 25, 60));
                    emitter.ApplyParticleModule(new SetConstColor(emitter, FlyRed));
                    emitter.ApplyParticleModule(new SetRandomScale(emitter, 1, 4));
                    emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
                    emitter.ApplyParticleModule(new SetRandomPos(emitter, 20));
                    emitter.ApplyParticleModule(new AddElement(emitter,
                        new Particle.SpriteInitParam("Circle20", "", 8, 1, 0.05f)));
                    emitter.ApplyParticleModule(new AddElement(emitter,
                        new Particle.SpriteInitParam("Futile_White", "LightSource", 8, 0.2f, 3)));
                    emitter.ApplyParticleModule(new AddElement(emitter,
                        new Particle.SpriteInitParam("Futile_White", "FlatLight", 8, 0.2f, 1f)));
                    emitter.ApplyParticleModule(new SetOriginalAlpha(emitter, 0));
                    emitter.ApplyParticleModule(new AlphaOverLife(emitter, (particle, f) =>
                    {
                        particle.vel *= 0.89f;
                        return Mathf.InverseLerp(0, 0.05f, f) * Mathf.Pow(Mathf.InverseLerp(1, 0.35f, f), 1.5f);
                    }));
                    emitter.ApplyParticleModule(new SetVelociyFromEmitter(emitter,0.5f));
                    emitter.ApplyParticleModule(new Gravity(emitter,0.2f));
                    ParticleSystem.ApplyEmitterAndInit(emitter);

                }

                if (emitter?.slateForDeletion ?? false)
                    emitter = null;

                if (light == null && ai.room != null && !ai.fly.dead)
                {
                    ai.room.AddObject(light = new LightSource(ai.FlyPos, false, FlyRed, ai.fly)
                    {
                        requireUpKeep = true,
                        affectedByPaletteDarkness = 0,
                        alpha = 0.5f,
                        rad = 100
                    });
                }
                else if (light != null)
                {
                    if (light.room == ai.room && !light.slatedForDeletetion)
                    {
                        light.stayAlive = true;
                        light.setPos = ai.FlyPos;
                    }
                    else
                    {
                        light = null;
                    }
                }

     

                if (Crit != null && ai.room == Crit.room && ai.behavior != FollowCrit && !Custom.DistLess(Crit.mainBodyChunk.pos, ai.fly.DangerPos, (crit is Player ? 50 : 100)))
                    ai.behavior = FollowCrit;
                else if (Crit != null && 
                         (Crit.room != ai.room || IsInfected(Crit) || !Custom.DistLess(Crit.mainBodyChunk.pos,ai.fly.DangerPos, (crit is Player? 300 : 200))))
                {
                    Crit = null;
                    ai.behavior = FlyAI.Behavior.Idle;
                }

                if (ai.behavior == FollowCrit && ai.room != null)
                {
                    ai.localGoal = Crit is Vulture vulture ? vulture.Head().pos : Crit.DangerPos;
                    ai.followingDijkstraMap = -1;
                    if (Random.value < 0.5f && Custom.DistLess(Crit is Vulture vulture1 ? vulture1.Head().lastPos : Crit.mainBodyChunk.lastPos, 
                            ai.fly.firstChunk.pos, 9f) && playerEatCounter == 0)
                    {
                        ai.fly.firstChunk.vel += Custom.RNV() * 6f;
                    }
                }

                PlayerEatUpdate(ai.fly);
            }

            private void PlayerEatUpdate(Fly self)
            {
                if (playerEatCounter == 0 &&
                    Crit is Player &&
                    Crit.bodyChunks.Any(i => Custom.DistLess(self.DangerPos, i.pos, 20)))
                {
                    playerEatCounter++;
                }

                if (playerEatCounter > 0 && crit is Player player)
                {
                    self.mainBodyChunk.MoveFromOutsideMyUpdate(false, (crit.graphicsModule as PlayerGraphics).head.pos);
                    playerEatCounter++;

                    if (playerEatCounter % 50 == 0)
                    {
                        self.room.PlaySound(playerEatCounter == 150 ? SoundID.Slugcat_Final_Bite_Fly : SoundID.Slugcat_Bite_Fly, self.mainBodyChunk.pos);
                        self.bites--;
                        if (playerEatCounter == 150)
                        {
                            self.killTag = crit.abstractCreature;
                            player.ObjectEaten(self);
                            self.Die();
                            self.Destroy();
                            CreateExplodeModule(player);
                        }
                    }

                }
            }
        }




        public class FlyExplodeModule
        {
            private readonly WeakReference<AbstractCreature> critRef;


            private const int MaxPreCounter = 200;


            private int counter;
            private int preCounter = MaxPreCounter;
            private readonly int maxCounter;

            private readonly float massFac;


            private readonly float[] rads; 
            private readonly Vector2[] pos;

            private LightSource light;

            public FlyExplodeModule(Creature crit)
            {
                critRef = new WeakReference<AbstractCreature>(crit.abstractCreature);
                massFac = Custom.LerpMap(crit.TotalMass, 1, 10, 0.5f, 2f);
                maxCounter = Mathf.RoundToInt(300 * massFac);

                counter = maxCounter;

                rads = new float[crit.bodyChunks.Length];
                pos = new Vector2[crit.bodyChunks.Length];
                for (int i = 0; i < crit.bodyChunks.Length; i++)
                {
                    rads[i] = crit.bodyChunks[i].rad;
                    pos[i] = crit.bodyChunks[i].pos;

                }
            }

            public void Update()
            {
                if (!critRef.TryGetTarget(out var self))
                    return;
                if (preCounter > 0)
                {
                    preCounter--;
                    for (int i = 0; i < self.realizedCreature.bodyChunks.Length; i++)
                    {
                        
                        self.realizedCreature.bodyChunks[i].rad =
                            rads[i] * Custom.LerpMap(preCounter, 0, MaxPreCounter, 1.25F, 1f);
                    }

                    return;
                }

                if (light?.slatedForDeletetion ?? false)
                {
                    light = null;
                }
                if (light == null && self.realizedCreature.room != null)
                {
                    self.realizedCreature.room.AddObject(new LightSource(self.realizedCreature.mainBodyChunk.pos,false,FlyRed,self.realizedCreature)
                    {
                        requireUpKeep = true,
                        rad = self.realizedCreature.mainBodyChunk.rad * 2,
                        alpha = Custom.LerpMap(preCounter, 0, MaxPreCounter, 0.7F, 0f)
                    });
                }

                if (light != null)
                {
                    if (light.room != self.realizedCreature.room)
                    {
                        light.Destroy();
                        light = null;
                    }
                    else
                    {
                        light.requireUpKeep = true;
                        light.setPos = self.realizedCreature.mainBodyChunk.pos;
                        light.alpha = Custom.LerpMap(preCounter, 0, MaxPreCounter, 0.7F, 0f);
                    }
                }

                if (counter > 0)
                {
                    if (self.state.dead)
                    {
                        counter = -1;
                        BuffUtils.Log(HeartDevouringWorm, "Cancel explode because creature die " + self.ID);
                        Destroy(self);
                        return;
                    }

                    if(counter > 1 || (self.realizedCreature.room != null && !self.realizedCreature.inShortcut))
                        counter--;

                    if (counter == 0)
                    {
                        Explode(self.realizedCreature);
                        return;
                    }
                    for (int i = 0; i < self.realizedCreature.bodyChunks.Length; i++)
                    {
                        var rnv = new Vector2((Random.value - 0.5f), (Random.value - 0.5f) / 2).normalized;
                       
                        self.realizedCreature.bodyChunks[i].rad = rads[i] * Custom.LerpMap(counter, 0, maxCounter, 2, 1.25f);
                        self.realizedCreature.bodyChunks[i].vel += rnv * Custom.LerpMap(counter, 0, maxCounter, 6, 1f) 
                                                                       * Mathf.Clamp(self.realizedCreature.bodyChunks[i].mass, 0.4f, 1.5f);
                        if (Random.value > 2 / 40f)
                        {
                            self.realizedCreature.Stun(Mathf.CeilToInt(Random.Range(10, 20) *
                                                                       Custom.LerpMap(counter, 0, maxCounter, 2.5f,
                                                                           1f)));
                            if(self.realizedCreature.room != null)
                                self.realizedCreature.room.PlaySound(SoundID.Bat_Attatch_To_Chain,
                                    self.realizedCreature.RandomChunk);
                        }
                    }

                }
                else
                {
                    for (int i = 0; i < self.realizedCreature.bodyChunks.Length; i++)
                        self.realizedCreature.bodyChunks[i].rad = Mathf.Lerp(self.realizedCreature.bodyChunks[i].rad, rads[i], 0.02f);
                    if (self.realizedCreature.bodyChunks[0].rad / rads[0] < 1.03f)
                        Destroy(self);
                }

            }

            public void Destroy(AbstractCreature crit)
            {
                ExplodeModules.Remove(crit);
            }

            public void AbstractUpdate(int count)
            {
                if (!critRef.TryGetTarget(out var self) || self.realizedCreature != null)
                    return;
                if (preCounter > 0)
                {
                    preCounter -= count;
                    if (preCounter < 0)
                        counter += preCounter;
                    return;
                }

                if (counter > 0)
                {
                    if (self.state.dead)
                    {
                        counter = -1;
                        BuffUtils.Log(HeartDevouringWorm, "Cancel explode because creature die " + self.ID);
                        Destroy(self);
                        return;
                    }
                    counter -=count;
                    if (counter < 0)
                    {
                        AbstractExplode(self);
                        Destroy(self);
                    }
                }
                else
                {
                    Destroy(self);
                }
            }

            public void AbstractExplode(AbstractCreature crit)
            {
                BuffUtils.Log(HeartDevouringWorm, "Fly abstract explode from " + crit.ID);
                int count = Mathf.Clamp(Mathf.RoundToInt(Random.Range(4f, 6f) * massFac), 1, 12);
                for (int i = 0; i < count; i++)
                {
                    AbstractCreature abstractCreature = new AbstractCreature(crit.world,
                        StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly), null,
                        crit.pos, crit.world.game.GetNewID());
                    crit.Room.AddEntity(abstractCreature);
                }

                crit.state.meatLeft = 0;
                crit.Die();
            }
            public void Explode(Creature crit)
            {
                BuffUtils.Log(HeartDevouringWorm, "Fly explode from " + crit.abstractCreature.ID);
                crit.room.PlaySound(SoundID.Rock_Hit_Creature, crit.mainBodyChunk);
                crit.Die();
                crit.abstractCreature.state.meatLeft = 0;

                int count = Mathf.Clamp(Mathf.RoundToInt(Random.Range(4f, 6f) * massFac),1,12);
                for (int i = 0; i < count; i++)
                {
                    AbstractCreature abstractCreature = new AbstractCreature(crit.abstractCreature.world,
                        StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly), null,
                        crit.abstractCreature.pos, crit.abstractCreature.world.game.GetNewID());
                    crit.abstractCreature.Room.AddEntity(abstractCreature);
                    abstractCreature.RealizeInRoom();

                    var targetPos = crit.firstChunk.pos;
                    var targetRad = crit.firstChunk.rad;
                    if ((crit.bodyChunkConnections?.Length ?? 0) != 0)
                    {
                        var connect = RXRandom.AnyItem(crit.bodyChunkConnections);
                        var r = Random.value;
                        targetPos = Vector2.Lerp(connect.chunk1.pos, connect.chunk2.pos, r);
                        targetRad = Mathf.Lerp(connect.chunk1.rad, connect.chunk2.rad, r);
                    }

                    if (abstractCreature.realizedCreature != null)
                    {
                        foreach (var bodyChunk in abstractCreature.realizedCreature.bodyChunks)
                        {
                            bodyChunk.vel += Custom.RNV() * Random.Range(5f, 20f);
                            bodyChunk.pos = targetPos + targetRad/6 * Random.value * bodyChunk.vel.normalized;
                        }
                        abstractCreature.realizedCreature.Stun(5);
                    }
                }

            }
        }
    }
}

