
using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine;
using System.Runtime.Remoting.Contexts;
using MoreSlugcats;
using RWCustom;
using Random = UnityEngine.Random;
using System.Runtime.CompilerServices;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;

namespace BuiltinBuffs.Negative
{
 
    internal class SpearRainIBuffEntry : IBuffEntry
    {
        public static BuffID SpearRainBuffID = new BuffID("SpearRain", true);
        public static RoomRain.DangerType SpearRain = new RoomRain.DangerType("SpearRain", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SpearRainIBuffEntry>(SpearRainBuffID);
        }

        public static void HookOn()
        {
            //IL.RoomRain.Update += RoomRain_Update_IL;
            On.RoomRain.Update += RoomRain_Update;
            On.RoomRain.ctor += RoomRain_ctor;
 
            //IL.RoomRain.DrawSprites += RoomRain_DrawSprites;
        }

        private static void RoomRain_ctor(On.RoomRain.orig_ctor orig, RoomRain self, GlobalRain globalRain, Room rm)
        {
            orig.Invoke(self, globalRain, rm);
            if (self.dangerType == RoomRain.DangerType.FloodAndRain || self.dangerType == RoomRain.DangerType.Rain)
                self.splashes = 0;
        }

        

        private static void RoomRain_DrawSprites(ILContext il)
        {
            ILCursor c1 = new ILCursor(il);
            c1.Index = 0;
            c1.Emit(OpCodes.Ret);
        }

        private static void RoomRain_Update(On.RoomRain.orig_Update orig, RoomRain self, bool eu)
        {
            orig.Invoke(self, eu);
            if(self.room.BeingViewed && (self.dangerType == RoomRain.DangerType.FloodAndRain || self.dangerType == RoomRain.DangerType.Rain) && !SpearRainModule.rainModules.TryGetValue(self, out _))
            {
                var module = new SpearRainModule(self.room, self);
                self.room.AddObject(module);
                SpearRainModule.rainModules.Add(self, module);
            }
        }
    }

    public class SpearRainModule : UpdatableAndDeletable, IDrawable
    {
        static float spearsPerTilePerSec = 20;
        public static ConditionalWeakTable<RoomRain, SpearRainModule> rainModules = new ConditionalWeakTable<RoomRain, SpearRainModule>();

        RoomRain roomRain;

        string spearElement = "SmallSpear";
        Color spearColor = Color.black;

        bool initColor;
        Spear getSpriteSpear;

        List<IntVector2> spawnSpearTiles = new List<IntVector2>();
        int spearDensity;

        ParticleEmitter spearEmitter;

        public float TotalSpearRate => spearDensity * spearsPerTilePerSec * roomRain.intensity; //roomRain.intensity;

        public SpearRainModule(Room room, RoomRain roomRain)
        {
            this.room = room;
            this.roomRain = roomRain;

            for(int x = 0;x < room.Width; x++)
            {
                if(!room.GetTile(x, room.Height - 1).Solid)
                    spawnSpearTiles.Add(new IntVector2(x, room.Height));
            }
            spearDensity = spawnSpearTiles.Count;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (slatedForDeletetion)
                return;

            if (getSpriteSpear != null)
            {
                var linq = room.game.cameras[0].spriteLeasers.Where((s) => s.drawableObject == getSpriteSpear);
                if (linq.Count() > 0)
                {
                    var sprite = linq.First().sprites[0];
                    spearElement = sprite.element.name;
                    spearColor = sprite.color;
                    getSpriteSpear.Destroy();
                    getSpriteSpear = null;
                }
            }

            if(roomRain.intensity > 0f)
            {
                if(spearEmitter == null)
                    spearEmitter = CreateSpearEmitter(false);
            }
            else
            {
                if(spearEmitter != null)
                {
                    spearEmitter.Die();
                    spearEmitter = null;
                }
            }

            if (!room.BeingViewed)
                Destroy();
        }

        public override void Destroy()
        {
            base.Destroy();
            if(spearEmitter != null)
            {
                spearEmitter.Die();
                spearEmitter = null;
            }
            rainModules.Remove(roomRain);
        }

        public ParticleEmitter CreateSpearEmitter(bool life)
        {
            BuffUtils.Log("SpearRain", "Create Emitter");
            ParticleEmitter emitter = new ParticleEmitter(room);

            
            if (life)
            {
                emitter.ApplyParticleSpawn(new RateSpawnerModule(emitter, 400, 40));
                emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 40 * 20, false, true));
            }
            else
                emitter.ApplyParticleSpawn(new SpearSpawnModule(emitter, 400, this));

            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam(spearElement, "", 9)));

            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 40 * 5, 40 * 6));

            emitter.ApplyParticleModule(new FollowColor(emitter, this));
            emitter.ApplyParticleModule(new DispatchSpearPos(emitter, this));
            emitter.ApplyParticleModule(new SetRandomScale(emitter, 1f, 1f));
            emitter.ApplyParticleModule(new AlphaOverLife(emitter, (p, l) =>
            {
                if (l < 0.8f)
                    return 1f;
                return (1f - l) / 0.2f;
            }));
            emitter.ApplyParticleModule(new SetRandomRotation(emitter, Custom.VecToDeg(Vector2.down), Custom.VecToDeg(Vector2.down)));
            emitter.ApplyParticleModule(new SetConstVelociy(emitter, Vector2.down * 40));
            emitter.ApplyParticleModule(new SimpleParticlePhysic(emitter, false, true)
            {
                OnTerrainCollide = (p) =>
                {
                    p.emitter.room.PlaySound(SoundID.Spear_Bounce_Off_Wall, p.pos, 1f, Random.value * 0.2f + 1f);
                }
            }
            );
            emitter.ApplyParticleModule(new SpearHitAndStuck(emitter));
            ParticleSystem.ApplyEmitterAndInit(emitter);
            return emitter;
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[0]; 
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if(!initColor)
            {
                AbstractSpear abSpear = new AbstractSpear(room.world, null, room.GetWorldCoordinate(new Vector2(0, 0)), room.game.GetNewID(), false);
                abSpear.RealizeInRoom();
                getSpriteSpear = abSpear.realizedObject as Spear;
                initColor = true;
            }
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
        }

        internal class SpearSpawnModule : SpawnModule
        {
            static float secPerFrame = 1 / 40f;
            protected float secNeed;
            float counter;
            SpearRainModule spearRainModule;

            public SpearSpawnModule(ParticleEmitter emitter, int maxParticleCount, SpearRainModule spearRainModule) : base(emitter, maxParticleCount)
            {
                this.spearRainModule = spearRainModule;
            }

            public override void Update()
            {
                if (emitter.slateForDeletion)
                    return;

                if (spearRainModule.TotalSpearRate == 0f)
                    secNeed = float.MaxValue;
                else
                    secNeed = 1f / spearRainModule.TotalSpearRate;

                counter += secPerFrame;
                while (counter > secNeed)
                {
                    counter -= secNeed;
                    if (emitter.Particles.Count < maxParitcleCount)
                    {
                        emitter.SpawnParticle();
                    }
                }
            }
        }

        internal class FollowColor : EmitterModule, IParticleInitModule
        {
            SpearRainModule spearRainModule;
            public FollowColor(ParticleEmitter emitter, SpearRainModule spearRainModule) : base(emitter)
            {
                this.spearRainModule = spearRainModule;
            }

            public void ApplyInit(Particle particle)
            {
                particle.HardSetColor(spearRainModule.spearColor);
            }
        }

        internal class DispatchSpearPos : EmitterModule, IParticleInitModule
        {
            SpearRainModule spearRainModule;
            public DispatchSpearPos(ParticleEmitter emitter, SpearRainModule spearRainModule) : base(emitter)
            {
                this.spearRainModule = spearRainModule;
            }

            public void ApplyInit(Particle particle)
            {
                IntVector2 tile = spearRainModule.spawnSpearTiles[Random.Range(0, spearRainModule.spawnSpearTiles.Count)];
                particle.HardSetPos(particle.emitter.room.MiddleOfTile(tile) + Custom.DegToVec(Random.value * 360f) * Random.value * 20f);
            }
        }

        internal class SpearHitAndStuck : EmitterModule, IParticleUpdateModule, IOwnParticleUniqueData, SharedPhysics.IProjectileTracer, IParticleDieModules
        {
            float noDetectRate;
            public SpearHitAndStuck(ParticleEmitter emitter, float noDetectRate = 0.5f) : base(emitter)
            {
                this.noDetectRate = noDetectRate;
            }

            public void ApplyDie(Particle particle)
            {
                var data = particle.GetUniqueData<HitData>(this);
                if (data == null)
                    return;
                data.ClearData();
            }

            public virtual void ApplyUpdate(Particle particle)
            {
                if (particle.randomParam3 < noDetectRate)
                    return; 

                var data = particle.GetUniqueData<HitData>(this);
                if (data == null)
                    return;
                Vector2 pos = particle.pos + particle.vel;
                if (data.StuckInCreature)
                    data.MoveWithStuckObj(particle);
                else
                {
                    SharedPhysics.CollisionResult result = SharedPhysics.TraceProjectileAgainstBodyChunks(this, particle.emitter.room, particle.pos, ref pos, 10f, 1, null, true);

                    if (result.obj != null && (result.chunk != null || result.onAppendagePos != null))
                    {
                        if (result.obj is Creature)
                        {
                            data.LogIntoCreature(result, particle);
                            data.MoveWithStuckObj(particle);
                            particle.emitter.room.PlaySound(SoundID.Spear_Stick_In_Creature, particle.pos, 1f, Random.value * 0.2f + 1f);
                        }
                        else
                            data.HitObjectBehaviour(result, particle);
                    }
                }
            }


            public virtual Particle.ParticleUniqueData GetUniqueData(Particle particle)
            {
                return new HitData(particle);
            }

            public virtual bool HitThisChunk(BodyChunk chunk)
            {
                return true;
            }

            public virtual bool HitThisObject(PhysicalObject obj)
            {
                return obj is Creature;
            }

            internal class HitData : Particle.ParticleUniqueData
            {
                PhysicalObject stuckInObject;
                PhysicalObject.Appendage.Pos stuckInAppendage;

                int stuckInChunkIndex;
                float stuckRotation;

                float deltaRad;
                float deltaRotation;

                public bool StuckInCreature => stuckInObject != null;
                public BodyChunk StuckInChunk => stuckInObject.bodyChunks[stuckInChunkIndex];

                public HitData(Particle particle)
                {
                }

                public void LogIntoCreature(SharedPhysics.CollisionResult result, Particle p)
                {
                    if (result.obj == null || !(result.obj is Creature creature))
                        return;

                    if (result.chunk == null && result.onAppendagePos == null)
                        return;
                    if(result.onAppendagePos == null)
                    {
                        stuckInObject = result.obj;
                        stuckInChunkIndex = result.chunk.index;
                        stuckRotation = Custom.Angle(p.vel, result.chunk.Rotation);
                        deltaRotation = Custom.VecToDeg(p.pos - result.chunk.pos) - Custom.VecToDeg(result.chunk.Rotation);
                        deltaRad = Mathf.Clamp(Vector2.Distance(p.pos, result.chunk.pos), 0f, result.chunk.rad * 0.9f);
                    }
                    else
                    {
                        stuckInObject = result.obj;
                        stuckInChunkIndex = 0;
                        this.stuckInAppendage = result.onAppendagePos;
                        this.stuckRotation = Custom.VecToDeg(p.vel) - Custom.VecToDeg(stuckInAppendage.appendage.OnAppendageDirection(this.stuckInAppendage));
                    }

                    ViolenceBehaviour(creature, p, StuckInChunk, stuckInAppendage);
                }

                public virtual void HitObjectBehaviour(SharedPhysics.CollisionResult result, Particle p)
                {

                }

                public virtual void ViolenceBehaviour(Creature creature, Particle particle, BodyChunk bodyChunk, PhysicalObject.Appendage.Pos appendagePos)
                {
                    if (creature is Player player)
                    {
                        if (particle.randomParam1 < 0.1f)
                        {
                            if (particle.randomParam2 < 0.01f)
                                player.Violence(null, null, bodyChunk, appendagePos, Creature.DamageType.Stab, 1f, 20f);
                            if (ModManager.MSC)
                            {
                                player.playerState.permanentDamageTracking += (double)(0.01f / player.Template.baseDamageResistance);
                                if (player.playerState.permanentDamageTracking >= 1.0)
                                {
                                    player.Die();
                                }
                            }
                        }
                    }
                    else
                    {
                        creature.Violence(null, particle.vel * 0.1f, bodyChunk, appendagePos, Creature.DamageType.Stab, 0.1f, 0f);
                    }
                }


                public void MoveWithStuckObj(Particle p)
                {
                    if (stuckInObject.slatedForDeletetion || stuckInObject.room == null)
                    {
                        p.Die();
                        return;
                    }

                    p.vel = Vector2.zero;
                    if (stuckInAppendage == null)
                    {
                        p.rotation = Custom.VecToDeg(StuckInChunk.Rotation) + stuckRotation;
                        p.pos = StuckInChunk.pos + Custom.DegToVec(Custom.VecToDeg(StuckInChunk.Rotation) + deltaRotation) * deltaRad;                
                    }
                    else
                    {
                        p.rotation = Custom.VecToDeg(Custom.DegToVec(this.stuckRotation + Custom.VecToDeg(this.stuckInAppendage.appendage.OnAppendageDirection(this.stuckInAppendage))));
                        p.pos = stuckInAppendage.appendage.OnAppendagePosition(this.stuckInAppendage);
                    }
                }

                public void ClearData()
                {
                    stuckInObject = null;
                }
            }
        }
    }
}
