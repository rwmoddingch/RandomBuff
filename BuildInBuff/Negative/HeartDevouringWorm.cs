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
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RandomBuff;
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
            return template.IsLizard || template.TopAncestor().type == CreatureTemplate.Type.DaddyLongLegs || 
                   template.TopAncestor().type == CreatureTemplate.Type.BigSpider || 
                   template.type == CreatureTemplate.Type.DropBug;
        }
    }

    internal class HeartDevouringWormBuffData : CountableBuffData
    {
        public override BuffID ID => HeartDevouringWormBuffEntry.HeartDevouringWorm;
        public override int MaxCycleCount => 10;
    }

    internal class HeartDevouringWormBuffEntry : IBuffEntry
    {
        public static readonly BuffID HeartDevouringWorm = new BuffID(nameof(HeartDevouringWorm), true);

        public static readonly FlyAI.Behavior FollowCrit = new FlyAI.Behavior($"HeartDevouringWorm.{nameof(FollowCrit)}", true);


        private static readonly ConditionalWeakTable<FlyAI, FlyModule> Modules = new ConditionalWeakTable<FlyAI, FlyModule>();

        private static readonly ConditionalWeakTable<AbstractCreature, FlyExplodeModule> ExplodeModules = new ConditionalWeakTable<AbstractCreature, FlyExplodeModule>();

        private static readonly Color FlyRed = Custom.hexToColor("FF334E");
        private static readonly Color FlyDarkRed = Custom.hexToColor("4D0304");

        private static readonly HashSet<CreatureTemplate.Type> Ignored = new HashSet<CreatureTemplate.Type>()
        {
            CreatureTemplate.Type.PoleMimic, CreatureTemplate.Type.Overseer, CreatureTemplate.Type.TempleGuard,
            CreatureTemplate.Type.TentaclePlant, CreatureTemplate.Type.TubeWorm , CreatureTemplate.Type.Centipede, CreatureTemplate.Type.Spider
        };

        private static readonly HashSet<CreatureTemplate.Type> DontNeedEat = new HashSet<CreatureTemplate.Type>()
        {
            CreatureTemplate.Type.EggBug, CreatureTemplate.Type.Deer, CreatureTemplate.Type.Scavenger,
            CreatureTemplate.Type.BigNeedleWorm, CreatureTemplate.Type.SmallNeedleWorm,
            CreatureTemplate.Type.Slugcat, MoreSlugcatsEnums.CreatureTemplateType.FireBug
        };

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<HeartDevouringWormBuff, HeartDevouringWormBuffData, HeartDevouringWormBuffEntry>(
                HeartDevouringWorm);
        }

        private static bool IsIgnored(CreatureTemplate template)
        {
            return Ignored.Contains(template.type) || template.TopAncestor().type == CreatureTemplate.Type.Centipede;
        }
        private static bool IsNeedChangeRad(Creature crit)
        {
            return crit is Lizard || crit is Scavenger || crit is Spider || crit is DropBug || crit is EggBug;
        }
        private static bool IsDontEat(CreatureTemplate template)
        {
            return DontNeedEat.Contains(template.type) || 
                   template.TopAncestor().type == CreatureTemplate.Type.Scavenger ||
                   template.TopAncestor().type == CreatureTemplate.Type.Slugcat ||
                   template.TopAncestor().type == CreatureTemplate.Type.Vulture;
        }

        private static bool IsInfected(Creature crit)
        {
            return ExplodeModules.TryGetValue(crit.abstractCreature, out _);
        }

        private static bool IsInfected(AbstractCreature crit)
        {
            return ExplodeModules.TryGetValue(crit, out _);
        }
        private static FlyExplodeModule CreateExplodeModule(Creature crit)
        {
            return ExplodeModules.GetValue(crit.abstractCreature, (i) => new FlyExplodeModule(i.realizedCreature));
        }



        public static void HookOn()
        {
            On.FlyAI.ConsiderOtherCreature += FlyAI_ConsiderOtherCreature;
            On.FlyAI.Update += FlyAI_Update;
            On.FlyAI.ctor += FlyAI_ctor;
            On.Fly.Update += Fly_Update;
            On.FlyGraphics.DrawSprites += FlyGraphics_DrawSprites;

            On.Creature.Grab += Creature_Grab;
            On.Creature.Update += Creature_Update;
            On.AbstractCreature.Update += AbstractCreature_Update;

            On.SlugcatStats.AutoGrabBatflys += SlugcatStats_AutoGrabBatflys;

            On.DaddyAI.IUseARelationshipTracker_UpdateDynamicRelationship += DaddyAI_IUseARelationshipTracker_UpdateDynamicRelationship;
            _ = new Hook(typeof(DaddyLongLegs).GetMethod("CheckDaddyConsumption"),
                typeof(HeartDevouringWormBuffEntry).GetMethod("Daddy_CheckDaddyConsumption",
                    BindingFlags.Static | BindingFlags.NonPublic));
            IL.DaddyTentacle.Update += DaddyTentacle_UpdateIL;
            On.DaddyLongLegs.Update += DaddyLongLegs_Update;

            On.ScavengerAI.IUseARelationshipTracker_UpdateDynamicRelationship += ScavengerAI_IUseARelationshipTracker_UpdateDynamicRelationship;


            On.Vulture.Update += Vulture_Update;

            On.ThreatDetermination.ThreatOfCreature += ThreatDetermination_ThreatOfCreature;


        }

        private static float ThreatDetermination_ThreatOfCreature(On.ThreatDetermination.orig_ThreatOfCreature orig, ThreatDetermination self, Creature creature, Player player)
        {
           var re= orig(self, creature, player);
           if (creature is Fly fly && fly.AI != null && Modules.TryGetValue(fly.AI, out var module))
           {
               if (module.Crit == player)
                   return Mathf.Max(re, Custom.LerpMap(Custom.Dist(player.DangerPos, creature.DangerPos), 300, 100, 0.1f, 0.3f));
           }

           return re;
        }

        private static void Vulture_Update(On.Vulture.orig_Update orig, Vulture self, bool eu)
        {
            orig(self, eu);
            if (!IsInfected(self) && self.room != null && self.Snapping)
            {
                foreach (var fly in self.room.updateList.OfType<Fly>())
                {
                    if (Custom.DistLess(fly.DangerPos, self.Head().pos, self.Head().rad))
                    {
                        fly.killTag = self.abstractCreature;
                        fly.Die();
                        fly.Destroy();
                        fly.room.PlaySound(SoundID.Fly_Caught, fly.mainBodyChunk);
                        CreateExplodeModule(self);
                        break;
                    }
                }
            }
        }

        private static void DaddyLongLegs_Update(On.DaddyLongLegs.orig_Update orig, DaddyLongLegs self, bool eu)
        {
            orig(self, eu);
            if (!IsInfected(self))
            {
                foreach (var ten in self.tentacles)
                {
                    if (ten.grabChunk?.owner is Fly fly && Custom.DistLess(ten.grabChunk.pos, self.mainBodyChunk.pos,
                            self.mainBodyChunk.rad * 1.25f))
                    {
                        fly.killTag = self.abstractCreature;
                        fly.Die();
                        fly.Destroy();
                        fly.room.PlaySound(SoundID.Fly_Caught, fly.mainBodyChunk);
                        CreateExplodeModule(self);
                        break;
                    }
                }
            }
        }

        private static void DaddyTentacle_UpdateIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            while (c.TryGotoNext(MoveType.After,
                       i => i.MatchLdfld<BodyChunk>("mass")))
            {
                c.EmitDelegate<Func<float, float>>((re) => Mathf.Max(0.6f, re));
            }
        }

        private static CreatureTemplate.Relationship ScavengerAI_IUseARelationshipTracker_UpdateDynamicRelationship(On.ScavengerAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, ScavengerAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            var re = orig(self, dRelation);
            if (dRelation.trackerRep.representedCreature.Room != self.creature.Room || dRelation.trackerRep.representedCreature.state.dead)
                return re;
            if (dRelation.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplate.Type.Fly)
                return new CreatureTemplate.Relationship(
                    self.creature.personality.aggression > 0.6F
                        ? CreatureTemplate.Relationship.Type.Attacks
                        : CreatureTemplate.Relationship.Type.Afraid,
                    Mathf.Clamp(
                        self.creature.Room.creatures.Count(i => i.creatureTemplate.type == CreatureTemplate.Type.Fly) *
                        Custom.LerpMap(self.creature.personality.nervous, 0, 1, 0.35f, 0.6f), 0, 1));
            if(IsInfected(dRelation.trackerRep.representedCreature))
                return new CreatureTemplate.Relationship(self.creature.personality.aggression > 0.6F &&
                                                         dRelation.trackerRep.representedCreature.creatureTemplate.type !=CreatureTemplate.Type.Scavenger &&
                                                         dRelation.trackerRep.representedCreature.creatureTemplate.TopAncestor().type != CreatureTemplate.Type.Scavenger ?
                    CreatureTemplate.Relationship.Type.Attacks : CreatureTemplate.Relationship.Type.Afraid,
                    (dRelation.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplate.Type.Scavenger ||
                     dRelation.trackerRep.representedCreature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Scavenger) ? 1 :
                    Custom.LerpMap(self.creature.personality.nervous, 0, 1, 0.45f, 1f));

            return re;
        }

  

        private static bool Daddy_CheckDaddyConsumption(Func<DaddyLongLegs, PhysicalObject, bool> orig,
            DaddyLongLegs self, PhysicalObject test)
        {
            if (test is Fly)
                return true;
            return orig(self, test);
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
            if (obj is Fly fly && !(self is Centipede) && !(self is Player))
            {
                fly.Die();
                fly.Destroy();
                self.ReleaseGrasp(graspUsed);
                CreateExplodeModule(self);
                self.room.PlaySound(SoundID.Fly_Caught, self.mainBodyChunk);
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
            public Creature Crit
            {
                get => crit;
                private set
                {
                    if (crit != value)
                    {
                        crit = value;
                        if (flyRef.TryGetTarget(out var fly) && fly.fly.bites != 3)
                        {
                            fly.fly.Die();
                        }
                        playerEatCounter = 0;
                        otherEatCounter = 0;
                        forgetCounter = 0;
                    }
                }
            }

            private const int MaxPlayerSingleEatCounter = 40;
            private const int MaxOtherSingleEatCounter = 30;


            private Creature crit;
            private readonly WeakReference<FlyAI> flyRef;

            private ParticleEmitter emitter;

            private int playerEatCounter;
            private int otherEatCounter;
            private int forgetCounter;

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
                if ((template.relationships[CreatureTemplate.Type.Fly.Index].type ==
                     CreatureTemplate.Relationship.Type.Eats || IsDontEat(template)) &&
                    newCrit.realizedCreature != null &&
                    !IsInfected(newCrit.realizedCreature) &&
                    ai.room.VisualContact(ai.fly.DangerPos,newCrit.realizedCreature.DangerPos) &&
                    !IsIgnored(template))
                {
                    if ((Crit == null || Custom.Dist(ai.fly.DangerPos, Crit.DangerPos) >
                        Custom.Dist(ai.fly.DangerPos, newCrit.realizedCreature.DangerPos)) &&
                        Custom.DistLess(ai.fly.DangerPos, newCrit.realizedCreature.DangerPos, (IsDontEat(newCrit.creatureTemplate) ? 300 : 200)) &&
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
                if (!flyRef.TryGetTarget(out var ai)) return;
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

                if (Crit != null && ai.room != null && !ai.room.VisualContact(ai.fly.DangerPos, Crit.DangerPos))
                {
                    forgetCounter++;
                    if (forgetCounter > 200)
                    {
                        BuffUtils.Log(HeartDevouringWorm, $"Forget creature:{Crit.abstractCreature.ID}");
                        Crit = null;
                    }
                }

                if (Crit != null && ai.room == Crit.room && ai.behavior != FollowCrit && 
                    !Custom.DistLess(Crit.mainBodyChunk.pos, ai.fly.DangerPos, (IsDontEat(crit.Template) ? 50 : 100)))
                    ai.behavior = FollowCrit;
                else if (Crit != null && 
                         (Crit.room != ai.room || IsInfected(Crit) || 
                          !Custom.DistLess(Crit.mainBodyChunk.pos,ai.fly.DangerPos, (IsDontEat(crit.Template) ? 500 : 370)) ||
                          ai.fly.grabbedBy.Count > 0))
                    Crit = null;
                
                if (Crit == null)
                    ai.behavior = FlyAI.Behavior.Idle;

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
                OtherCreatureEatUpdate(ai.fly);
            }

            private void OtherCreatureEatUpdate(Fly self)
            {
                if (Crit == null || Crit is Player || !IsDontEat(Crit.Template))
                    return;

                if (otherEatCounter == 0 && !(Crit is Player) &&
                    Crit.bodyChunks.Any(i => Custom.DistLess(self.DangerPos, i.pos, i.rad + 10)))
                {
                    Crit.room.PlaySound(SoundID.Fly_Caught, Crit.mainBodyChunk);
                    otherEatCounter++;
                }

                if (otherEatCounter > 0 && !(Crit is Player))
                {
                    otherEatCounter++;
                    if (Crit.graphicsModule is ScavengerGraphics scavenger)
                    {
                        self.mainBodyChunk.MoveFromOutsideMyUpdate(false,
                            scavenger.drawPositions[scavenger.headDrawPos, 0]);
                    }
                    else if (Crit is Deer deer)
                    {
                        self.mainBodyChunk.MoveFromOutsideMyUpdate(false,
                            Vector2.Lerp(deer.firstChunk.pos, deer.bodyChunks[1].pos,0.5f));
                    }
                    else
                    {
                        self.mainBodyChunk.MoveFromOutsideMyUpdate(false,
                            Crit.firstChunk.pos);
                    }
                    if (otherEatCounter % MaxOtherSingleEatCounter == 0)
                    {
                        self.bites--;
                        if (otherEatCounter == MaxOtherSingleEatCounter * 3)
                        {
                            self.killTag = crit.abstractCreature;
                            self.Die();
                            self.Destroy();
                            CreateExplodeModule(Crit);
                        }
                    }
                }
            }

            private void PlayerEatUpdate(Fly self)
            {
                if (playerEatCounter == 0 &&
                    Crit is Player &&
                    Crit.bodyChunks.Any(i => Custom.DistLess(self.DangerPos, i.pos, 20)))
                {
                    Crit.room.PlaySound(SoundID.Fly_Caught, Crit.mainBodyChunk);
                    playerEatCounter++;
                }

                if (playerEatCounter > 0 && crit is Player player)
                {
                    self.mainBodyChunk.MoveFromOutsideMyUpdate(false, (crit.graphicsModule as PlayerGraphics).head.pos);
                    playerEatCounter++;

                    if (playerEatCounter % MaxPlayerSingleEatCounter == 0)
                    {
                        self.room.PlaySound(playerEatCounter == MaxPlayerSingleEatCounter*3 ? SoundID.Slugcat_Final_Bite_Fly : SoundID.Slugcat_Bite_Fly, self.mainBodyChunk.pos);
                        self.bites--;
                        if (playerEatCounter == MaxPlayerSingleEatCounter*3)
                        {
                            self.killTag = crit.abstractCreature;
                            //player.ObjectEaten(self);
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

                        if (IsNeedChangeRad(self.realizedCreature))
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
                        if(IsNeedChangeRad(self.realizedCreature))
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
                    if (IsNeedChangeRad(self.realizedCreature))
                        for (int i = 0; i < self.realizedCreature.bodyChunks.Length; i++)
                            self.realizedCreature.bodyChunks[i].rad = Mathf.Lerp(self.realizedCreature.bodyChunks[i].rad, rads[i], 0.05f);
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
                crit.room.AddObject(new FirecrackerPlant.ScareObject(crit.firstChunk.pos)
                {
                    fearRange = 200f,
                    fearScavs = true,
                    lifeTime = 450
                });
                int count = Mathf.Clamp(Mathf.RoundToInt(Random.Range(4f, 6f) * massFac),1,12);
                float randomFac = Custom.LerpMap(count, 25, 50,1,0.5f);
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
                            bodyChunk.vel += Custom.RNV() * Random.Range(7,25f);
                            bodyChunk.pos = targetPos + targetRad/2 * Random.value * bodyChunk.vel.normalized;
                        }

                        for (int j = 0; j < Mathf.CeilToInt(Random.Range(3f,7f) * randomFac) ; j++)
                        {
                            crit.room.AddObject(new WormSpit(targetPos, Custom.RNV() * Random.Range(10f, 20f),crit,targetRad/2));
                        }
                        abstractCreature.realizedCreature.Stun(5);
                    }
                }

            }
        }

        public class WormSpit : UpdatableAndDeletable, IDrawable
        {
            public Vector2 pos;

            public Vector2 lastPos;

            public Vector2 vel;

            private float massLeft;

            private readonly float disapearSpeed;

            private readonly Vector2[,] slime;


            private float Rad => maxRad * massLeft;

            private readonly float maxRad = 0;

            public int JaggedSprite => 0;

            public int DotSprite => 1 + slime.GetLength(0);
                
            public int TotalSprites => slime.GetLength(0) + 2;

            private readonly Color darkColor;
            private readonly Color color;

            public int SlimeSprite(int s)
            {
                return 1 + s;
            }

            public WormSpit(Vector2 pos, Vector2 vel, Creature crit,float targetRad)
            {
                lastPos = pos;
                this.vel = vel;
                this.pos = pos + vel;
                massLeft = 1f;
                disapearSpeed = Random.value;
                maxRad =  Mathf.Clamp(Random.Range(0.7f, 4f) * targetRad, 6f,float.MaxValue);
                slime = new Vector2[(int)Mathf.Lerp(8f, 15f, Random.value), 4];
                for (int i = 0; i < slime.GetLength(0); i++)
                {
                    slime[i, 0] = pos + Custom.RNV() * 4f * Random.value;
                    slime[i, 1] = slime[i, 0];
                    slime[i, 2] = vel + Custom.RNV() * 4f * Random.value;
                    int num = ((i != 0 && !(Random.value < 0.3f)) ? ((!(Random.value < 0.7f)) ? Random.Range(0, slime.GetLength(0)) : (i - 1)) : (-1));
                    slime[i, 3] = new Vector2(num, Mathf.Lerp(3f, 8f, Random.value));
                }

                if (crit is Player)
                    darkColor = crit.ShortCutColor() * 0.5F;
                else if (crit.graphicsModule is LizardGraphics lizard)
                    darkColor = lizard.BodyColor(1);
                else
                    darkColor = crit.room.game.cameras[0].currentPalette.blackColor;

                color = crit.ShortCutColor();

                var initLerp = Random.value;
                darkColor = Color.Lerp(darkColor, FlyDarkRed, initLerp);
                color = Color.Lerp(color, FlyRed, initLerp);



            }

            public override void Update(bool eu)
            {
                lastPos = pos;
                pos += vel;
                vel.y -= 0.9f;
                for (int i = 0; i < slime.GetLength(0); i++)
                {
                    slime[i, 1] = slime[i, 0];
                    slime[i, 0] += slime[i, 2];
                    slime[i, 2] *= 0.99f;
                    slime[i, 2].y -= 0.9f * (ModManager.MMF ? room.gravity : 1f);
                    if ((int)slime[i, 3].x < 0 || (int)slime[i, 3].x >= slime.GetLength(0))
                    {
                        Vector2 vector = pos;
                        Vector2 vector2 = Custom.DirVec(slime[i, 0], vector);
                        float num = Vector2.Distance(slime[i, 0], vector);
                        slime[i, 0] -= vector2 * (slime[i, 3].y * massLeft - num) * 0.9f;
                        slime[i, 2] -= vector2 * (slime[i, 3].y * massLeft - num) * 0.9f;
                        pos += vector2 * (slime[i, 3].y - num) * 0.1f;
                        vel += vector2 * (slime[i, 3].y - num) * 0.1f;
                    }
                    else
                    {
                        Vector2 vector3 = Custom.DirVec(slime[i, 0], slime[(int)slime[i, 3].x, 0]);
                        float num2 = Vector2.Distance(slime[i, 0], slime[(int)slime[i, 3].x, 0]);
                        slime[i, 0] -= vector3 * (slime[i, 3].y * massLeft - num2) * 0.5f;
                        slime[i, 2] -= vector3 * (slime[i, 3].y * massLeft - num2) * 0.5f;
                        slime[(int)slime[i, 3].x, 0] += vector3 * (slime[i, 3].y * massLeft - num2) * 0.5f;
                        slime[(int)slime[i, 3].x, 2] += vector3 * (slime[i, 3].y * massLeft - num2) * 0.5f;
                    }
                }

                IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(room, room.GetTilePosition(lastPos), room.GetTilePosition(pos));
                if (intVector.HasValue)
                {
                    FloatRect floatRect = Custom.RectCollision(pos, lastPos, room.TileRect(intVector.Value).Grow(Rad));
                    pos = floatRect.GetCorner(FloatRect.CornerLabel.D);
                    if (floatRect.GetCorner(FloatRect.CornerLabel.B).x < 0f)
                    {
                        vel.x = Mathf.Abs(vel.x) * 0.2f;
                        vel.y *= 0.8f;
                    }
                    else if (floatRect.GetCorner(FloatRect.CornerLabel.B).x > 0f)
                    {
                        vel.x = (0f - Mathf.Abs(vel.x)) * 0.2f;
                        vel.y *= 0.8f;
                    }
                    else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y < 0f)
                    {
                        vel.y = Mathf.Abs(vel.y) * 0.2f;
                        vel.x *= 0.8f;
                    }
                    else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y > 0f)
                    {
                        vel.y = (0f - Mathf.Abs(vel.y)) * 0.2f;
                        vel.x *= 0.8f;
                    }
                }

                massLeft -= Mathf.Lerp(0.5f, 1.5f, disapearSpeed) / 80;

                if (massLeft <= 0f || pos.y < -300f)
                {
                    Destroy();
                }
                base.Update(eu);
            }

            private Vector2 StuckPosOfSlime(int s, float timeStacker)
            {
                if ((int)slime[s, 3].x < 0 || (int)slime[s, 3].x >= slime.GetLength(0))
                {
                    return Vector2.Lerp(lastPos, pos, timeStacker);
                }
                return Vector2.Lerp(slime[(int)slime[s, 3].x, 1], slime[(int)slime[s, 3].x, 0], timeStacker);
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[TotalSprites];
                sLeaser.sprites[DotSprite] = new FSprite("Futile_White");
                sLeaser.sprites[DotSprite].shader = rCam.game.rainWorld.Shaders["JaggedCircle"];
                sLeaser.sprites[DotSprite].alpha = Random.value * 0.5f;
                sLeaser.sprites[JaggedSprite] = new FSprite("Futile_White");
                sLeaser.sprites[JaggedSprite].shader = rCam.game.rainWorld.Shaders["JaggedCircle"];
                sLeaser.sprites[JaggedSprite].alpha = Random.value * 0.5f;
                for (int i = 0; i < slime.GetLength(0); i++)
                {
                    sLeaser.sprites[SlimeSprite(i)] = new FSprite("Futile_White");
                    sLeaser.sprites[SlimeSprite(i)].anchorY = 0.05f;
                    sLeaser.sprites[SlimeSprite(i)].shader = rCam.game.rainWorld.Shaders["JaggedCircle"];
                    sLeaser.sprites[SlimeSprite(i)].alpha = Random.value;
                }
                AddToContainer(sLeaser, rCam, null);
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                Vector2 vector = Vector2.Lerp(lastPos, pos, timeStacker);
                float t = Mathf.InverseLerp(30f, 6f, Vector2.Distance(lastPos, pos));
                float t2 = Mathf.InverseLerp(6f, 30f, Mathf.Lerp(Vector2.Distance(lastPos, pos), Vector2.Distance(vector, Vector2.Lerp(slime[0, 1], slime[0, 0], timeStacker)), t));
                Vector2 v = Vector3.Slerp(Custom.DirVec(lastPos, pos), Custom.DirVec(vector, Vector2.Lerp(slime[0, 1], slime[0, 0], timeStacker)), t);
                sLeaser.sprites[DotSprite].x = vector.x - camPos.x;
                sLeaser.sprites[DotSprite].y = vector.y - camPos.y;
                sLeaser.sprites[DotSprite].rotation = Custom.VecToDeg(v);
                sLeaser.sprites[DotSprite].scaleX = Mathf.Lerp(0.4f, 0.2f, t2) * massLeft;
                sLeaser.sprites[DotSprite].scaleY = Mathf.Lerp(0.3f, 0.7f, t2) * massLeft;
                sLeaser.sprites[JaggedSprite].x = vector.x - camPos.x;
                sLeaser.sprites[JaggedSprite].y = vector.y - camPos.y;
                sLeaser.sprites[JaggedSprite].rotation = Custom.VecToDeg(v);
                sLeaser.sprites[JaggedSprite].scaleX = Mathf.Lerp(0.6f, 0.4f, t2) * massLeft;
                sLeaser.sprites[JaggedSprite].scaleY = Mathf.Lerp(0.5f, 1f, t2) * massLeft;
                for (int i = 0; i < slime.GetLength(0); i++)
                {
                    Vector2 vector2 = Vector2.Lerp(slime[i, 1], slime[i, 0], timeStacker);
                    Vector2 vector3 = StuckPosOfSlime(i, timeStacker);
                    sLeaser.sprites[SlimeSprite(i)].x = vector2.x - camPos.x;
                    sLeaser.sprites[SlimeSprite(i)].y = vector2.y - camPos.y;
                    sLeaser.sprites[SlimeSprite(i)].scaleY = (Vector2.Distance(vector2, vector3) + 3f) / 16f;
                    sLeaser.sprites[SlimeSprite(i)].rotation = Custom.AimFromOneVectorToAnother(vector2, vector3);
                    sLeaser.sprites[SlimeSprite(i)].scaleX = Custom.LerpMap(Vector2.Distance(vector2, vector3), 0f, slime[i, 3].y * 3.5f, 6f, 2f, 2f) * massLeft / 16f;
                }
                sLeaser.sprites[JaggedSprite].color = Color.Lerp(darkColor, FlyDarkRed,(1 - massLeft) * 3);

                sLeaser.sprites[DotSprite].color = Color.Lerp(color, FlyRed,(1 - massLeft)*3);

                for (int i = 0; i < slime.GetLength(0); i++)
                    sLeaser.sprites[SlimeSprite(i)].color = Color.Lerp(darkColor, FlyDarkRed, (1 - massLeft) * 3);
                if (base.slatedForDeletetion || room != rCam.room)
                {
                    sLeaser.CleanSpritesAndRemove();
                }
            }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {

            }

            public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                if (newContatiner == null)
                {
                    newContatiner = rCam.ReturnFContainer("Items");
                }

                foreach (var sprite in sLeaser.sprites)
                {
                    sprite.RemoveFromContainer();
                    newContatiner.AddChild(sprite);
                }
            }
        }
    }


 
}

