using MoreSlugcats;
using RandomBuffUtils;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RandomBuffUtils.ParticleSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Random = UnityEngine.Random;
using System.Reflection;
using RandomBuff.Core.Option;

namespace RandomBuff.Core.Progression.CosmeticUnlocks
{
    internal class FireworkCosmetic : CosmeticUnlock
    {
        public override CosmeticUnlockID UnlockID => CosmeticUnlockID.FireWork;

        public override string IconElement => "BuffCosmetic_Firework";

        public override SlugcatStats.Name BindCat => MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer;
        private static void Player_ClassMechanicsArtificer(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            BuffPlugin.Log("Player_ClassMechanicsArtificer");
            while(c.TryGotoNext(MoveType.After,
                (i) => i.MatchLdsfld<SoundID>("Fire_Spear_Explode")))
            {
                c.GotoNext(MoveType.After, (i) => i.MatchCallvirt<Room>("PlaySound"));
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<Player>>(PyroJumped);
            }
        }

        public override void StartGame(RainWorldGame game)
        {
            base.StartGame(game);
            IL.Player.ClassMechanicsArtificer += Player_ClassMechanicsArtificer;
            BuffPlugin.Log("FireworkCosmetic enabled");
        }

        public override void Destroy()
        {
            base.Destroy();
            IL.Player.ClassMechanicsArtificer -= Player_ClassMechanicsArtificer;
        }

        public static void PyroJumped(Player player)
        {
            if (player.room == null || (player.slugcatStats.name != MoreSlugcatsEnums.SlugcatStatsName.Artificer && !BuffOptionInterface.Instance.CosmeticForEverySlug.Value))
                return;
            CreateFireworkEmitter(player.room, player.DangerPos);
            var game = player.room.game;
            
        }

        public static void CreateFireworkEmitter(Room room, Vector2 pos)
        {
            room.AddObject(new FireworkGenerator(room, pos));   
            return;
        }

        static void CreateSubParticle(float angle, float range, float vel, float hue, ParticleEmitter owner)
        {
            Vector2 velA = Custom.DegToVec(angle) * vel;
            Vector2 velB = Custom.DegToVec(range + angle) * vel;

            owner.OnParticleDieEvent += CreateEmitter;

            void CreateEmitter(Particle particle)
            {
                var emitter = new ParticleEmitter(particle.emitter.room);
                emitter.pos = particle.pos;

                emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 10, false));
                emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, 5));

                emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight")));
                emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "")));

                emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
                emitter.ApplyParticleModule(new SetRandomPos(emitter, 0f));
                emitter.ApplyParticleModule(new SetRandomLife(emitter, 200, 400));
                emitter.ApplyParticleModule(new SetRandomColor(emitter, hue, hue + 0.2f, 1f, 0.5f));
                emitter.ApplyParticleModule(new SetRandomVelocity(emitter, velA, velB));
                emitter.ApplyParticleModule(new SetRandomScale(emitter, 1f, 1.2f));

                emitter.ApplyParticleModule(new ConstantAcc(emitter, new Vector2(0f, -2f)));
                emitter.ApplyParticleModule(new SimpleParticlePhysic(emitter, true, false));
                emitter.ApplyParticleModule(new ColorOverLife(emitter, (p, lifeParam) =>
                {
                    Color result = Color.Lerp(p.setColor, Color.white, (Mathf.Sin(lifeParam * 3f) * 0.5f + 0.5f));
                    result.a = 1f - lifeParam;
                    return result;
                }));
                emitter.ApplyParticleModule(new ScaleOverLife(emitter, (p, lifeParam) =>
                {
                    return p.setScaleXY * (Mathf.Sin(lifeParam * 10f) * 0.3f + 0.7f) * (1f - lifeParam);
                }));


                emitter.ApplyParticleModule(new DefaultDrawer(emitter, new int[1] { 0 }));
                emitter.ApplyParticleModule(new TrailDrawer(emitter, 1, 20)
                {
                    alpha = (p, i, a) => 1f - i / (float)a,
                    alphaModifyOverLife = (p, l) => 1f - l,
                    gradient = (p, i, a) => Color.Lerp(Color.white, p.setColor, i / (float)a),
                    width = (p, i, a) => (1f - i / (float)a) * p.setScaleXY.x
                });


                ParticleSystem.ApplyEmitterAndInit(emitter);
            }
        }
    }

    internal class FireworkGenerator : UpdatableAndDeletable
    {
        Vector2 pos;

        List<FireworkStageInfo> stages = new List<FireworkStageInfo>();
        
        public FireworkGenerator(Room room, Vector2 pos)
        {
            this.room = room;
            this.pos = pos;

            var initStage = new FireworkStageInfo(this, null)
            {
                waitLastStageEnd = false,
                count = 1,
                scale = 1f,
                colorType = ColorType.Constant,
                moveType = MoveType.Natural,
                fadeType = FadeType.Normal,
                trailType = TrailType.Simple,
                ignitCountType = IgnitCountType.Single,
                initWaitType = InitWaitType.Instant,

                hue1 = Random.value,
            };
            initStage.hue2 = (initStage.hue1 + 0.2f) % 1;
            stages.Add(initStage);

            var current = GenerateRandomStage(initStage, initStage);
            if(Random.value < 0.8f)
            {
                current = GenerateRandomStage(current, initStage);
                if(Random.value < 0.2f)
                    current = GenerateRandomStage(current, initStage);
            }
        }

        public FireworkStageInfo GenerateRandomStage(FireworkStageInfo last, FireworkStageInfo wait)
        {
            var next = new FireworkStageInfo(this, last)
            {
                waitLastStageEnd = wait != null,
                waitStage = wait,
                lifeFactor = 2,
                scale = Mathf.Lerp(0.3f, 0.7f, Random.value),
                moveType = RandomEnum<MoveType>(),
                fadeType = RandomEnum<FadeType>(),
                trailType = RandomEnum<TrailType>(),
                ignitCountType = RandomEnum<IgnitCountType>(),
                initWaitType = RandomEnum<InitWaitType>(),
                colorType = RandomEnum<ColorType>(),

                hue1 = (last.hue1 + Random.value * 0.1f) % 1,
            };
            if (Random.value < 0.5f)
                next.ignitCountType = IgnitCountType.Angle;
            else
                next.ignitCountType = IgnitCountType.RandomCircle;

            next.hue2 = (next.hue2 + 0.2f) % 1;
            switch (next.ignitCountType)
            {
                case IgnitCountType.Single:
                    next.count = 1;
                    break;
                case IgnitCountType.RandomCircle:
                    next.count = Random.Range(25, 30) * 4;
                    break;
                case IgnitCountType.Angle:
                    next.count = Random.Range(25, 30) * 2;
                    break;
            }
            stages.Add(next);
            return next;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (slatedForDeletetion)
                return;

            bool allStageEnd = true;
            foreach (var stage in stages)
            {
                stage.Update();
                if(!stage.stageEnd)
                    allStageEnd = false;
            }
            if (allStageEnd)
                Destroy();
        }

        public override void Destroy()
        {
            base.Destroy();
            foreach(var stage in stages)
            {
                if (stage.stageEnd)
                    continue;
                stage.Destroy();
            }
            stages.Clear();
        }


        public class FireworkStageInfo
        {
            public bool waitLastStageEnd;
            public int lifeFactor = 1;
            public int count;

            public TrailType trailType;
            public MoveType moveType;
            public FadeType fadeType;
            public IgnitCountType ignitCountType;
            public InitWaitType initWaitType;
            public ColorType colorType;

            public float scale;

            public float hue1;
            public float hue2;

            public float randomParam1;
            public float randomParam2;

            public FireworkStageInfo waitStage;
            public bool stageStart;
            public bool stageEnd;

            FireworkGenerator generator;

            ParticleEmitter emitter1;
            ParticleEmitter emitter2;

            public Vector2 stagePos;

            public FireworkStageInfo(FireworkGenerator generator, FireworkStageInfo waitStage)
            {
                this.generator = generator;
                this.waitStage = waitStage;

                randomParam1 = Random.value;
                randomParam2 = Random.value;
            }

            public void Update()
            {
                if(!stageStart)
                {
                    if(waitLastStageEnd && waitStage != null && waitStage.stageEnd)
                    {
                        GenerateEmitter();
                    }
                    else if(!waitLastStageEnd)
                    {
                        GenerateEmitter();
                    }
                }
                else if(!stageEnd)
                {
                    if(emitter1 == null && emitter2 == null)
                    {
                        stageEnd = true;
                    }
                }
            }

            public void Destroy()
            {
                emitter1?.Die(true);
                emitter2?.Die(true);
            }

            public void GenerateEmitter()
            {
                stageStart = true;
                emitter1 = new ParticleEmitter(generator.room);
                emitter1.pos = emitter1.lastPos = waitStage != null ? waitStage.stagePos : generator.pos;

                emitter1.ApplyEmitterModule(new SetEmitterLife(emitter1, 2, false));
                emitter1.ApplyParticleSpawn(new BurstSpawnerModule(emitter1, count));
                emitter1.ApplyParticleModule(new SetMoveType(emitter1, Particle.MoveType.Global));

                emitter1.ApplyParticleModule(new AddElement(emitter1, new Particle.SpriteInitParam("buffassets/illustrations/FlatLight200", "StormIsApproaching.AdditiveDefault", layer: 9, alpha: 0.5f, scale: 0.5f)));
                emitter1.ApplyParticleModule(new AddElement(emitter1, new Particle.SpriteInitParam("buffassets/illustrations/AlphaCircle20", "StormIsApproaching.AdditiveDefault",layer:9)));
                if (trailType == TrailType.Simple)
                    emitter1.ApplyParticleModule(new AddElement(emitter1, new Particle.SpriteInitParam("buffassets/illustrations/AlphaCircle20", "StormIsApproaching.AdditiveDefault")));

                emitter1.ApplyParticleModule(new SetRandomPos(emitter1, 0f));
                emitter1.ApplyParticleModule(new SetRandomColor(emitter1, hue1, hue1, 1f, 0.5f));

                emitter1.ApplyParticleModule(new SetRandomLife(emitter1, 60 * lifeFactor, 70 * lifeFactor));
                emitter1.ApplyParticleModule(new SetRandomScale(emitter1, 0.3f * scale, 0.4f * scale));
                emitter1.ApplyParticleModule(new AlphaOverLife(emitter1, (p, l) => 1f));

                switch (ignitCountType)
                {
                    case IgnitCountType.Single:
                        emitter1.ApplyParticleModule(new SetRandomVelocity(emitter1, Vector2.up * 8f / lifeFactor, Vector2.up * 10f / lifeFactor));
                        break;
                    case IgnitCountType.Angle:
                        emitter1.ApplyParticleModule(new SetCustomVelocity(emitter1, (p) =>
                        {
                            int split = Mathf.CeilToInt(Mathf.Lerp(3f, 6f, randomParam2));
                            float dispatch =Mathf.Pow((p.randomParam1* 2f - 1f), 5f) * 360f / split;

                            float angleP = Mathf.CeilToInt(p.randomParam2 * split) * (360f / split) + dispatch;

                            return (Mathf.Lerp(8f / lifeFactor, 9f / lifeFactor, randomParam1) + 3f * p.randomParam3) * Custom.DegToVec(angleP);
                        }));
                        break;
                    case IgnitCountType.RandomCircle:
                        float vel = Mathf.Lerp(8f / lifeFactor, 9f / lifeFactor, randomParam1);
                        emitter1.ApplyParticleModule(new SetSphericalVelocity(emitter1, vel, vel + 3f));
                        break;
                }

                switch (moveType)
                {
                    case MoveType.Natural:
                        if(ignitCountType == IgnitCountType.Single)
                        {
                            emitter1.ApplyParticleModule(new VelocityOverLife(emitter1, (p, l) =>
                            {
                                return p.setVel * Mathf.Pow(1f - l, 2f);
                            }));
                            break;
                        }
                        else
                        {
                            emitter1.ApplyParticleModule(new VelocityOverLife(emitter1, (p, l) =>
                            {
                                return p.setVel * Mathf.Pow(1f - l, 4f) + Vector2.down * l * (0.5f + 0.5f * p.randomParam2);
                            }));
                            break;
                        }
    
                    case MoveType.Spin:
                        emitter1.ApplyParticleModule(new VelocityOverLife(emitter1, (p, l) =>
                        {
                            p.setVel = p.setVel + Custom.PerpendicularVector(p.setVel.normalized) * (Random.value * 2f - 1f);
                            return p.setVel * (1f - l) * 0.3f /*+ Vector2.down * l * (0.5f + 0.5f * p.randomParam2)*/;
                        }));
                        break;
                    case MoveType.SmallBias:
                        emitter1.ApplyParticleModule(new VelocityOverLife(emitter1, (p, l) =>
                        {
                            float t = Mathf.Pow(1f - l, 4f);
                            return p.setVel * t + Vector2.down * (1f - l) * 2f + Custom.PerpendicularVector(p.setVel.normalized) * Mathf.Sin(l * Mathf.PI * Mathf.Lerp(20f, 30f, p.randomParam1)) * t;
                        }));
                        break;
                }

                if(colorType == ColorType.Change)
                {
                    emitter1.ApplyParticleModule(new ColorOverLife(emitter1, (p, l) =>
                    {
                        return Color.Lerp(Custom.HSL2RGB(hue1, 1f, 0.5f), Custom.HSL2RGB(hue2, 1f, 0.5f), l);
                    }));
                }

                switch (fadeType)
                {
                    case FadeType.Normal:
                        emitter1.ApplyParticleModule(new AlphaOverLife(emitter1, (p, l) =>
                        {
                            if (l < 0.1f)
                                return l * 10f;
                            else if (l > 0.9f)
                                return (l - 0.9f) * 10f;
                            else
                                return 1f;
                        }));
                        break;
                    case FadeType.Flash:
                        emitter1.ApplyEmitterModule(new AlphaOverLife(emitter1, (p, l) =>
                        {
                            if (l < 0.1f)
                                return l * 10f;
                            else if (l < 0.7f)
                                return 1f;
                            else
                                return (l - 0.7f) / 0.3f * Random.value;
                        }));
                        break;
                }

                switch (trailType)
                {
                    case TrailType.Simple:
                        emitter1.ApplyParticleModule(new DefaultDrawer(emitter1, new int[2] { 0, 1 }));
                        emitter1.ApplyParticleModule(new TrailDrawer(emitter1, 2, 20)
                        {
                            alpha = (p, i, a) => 1f - i / (float)a,
                            gradient = (p, i, a) => Color.Lerp(Custom.HSL2RGB(hue1, 1f, 0.5f),Custom.HSL2RGB(hue2, 1f, 0.5f), i / (float)a),
                            width = (p, i, a) => (1f - i / (float)a) * p.setScaleXY.x * 16f
                        });
                        break;
                    case TrailType.None:
                        break;
                    default:
                        CreateTrailParticleEmitter(emitter1);
                        break;
                }

                emitter1.OnParticleDieEvent += (p) =>
                {
                    stageEnd = true;
                    stagePos = p.pos;
                };
                emitter1.OnEmitterActuallyDie += (emitter) =>
                {
                    emitter1 = null;
                };
                ParticleSystem.ApplyEmitterAndInit(emitter1);
            }

            public void CreateTrailParticleEmitter(ParticleEmitter ownerEmitter)
            {
                emitter2 = new ParticleEmitter(generator.room);

                switch (trailType)
                {
                    case TrailType.Spread:
                        emitter2.ApplyParticleSpawn(new BindBurstSpawner(emitter2, ownerEmitter, 10, 20, 1200));
                        break;
                    case TrailType.SpreadAndRotate:
                        emitter2.ApplyParticleSpawn(new BindRateSpawner(emitter2, 5 * lifeFactor, ownerEmitter, 1200));
                        break;
                }

                emitter2.ApplyParticleModule(new BindParticleToParticle(emitter2, ownerEmitter));
                emitter2.ApplyParticleModule(new AddElement(emitter2, new Particle.SpriteInitParam("buffassets/illustrations/AlphaCircle20", "StormIsApproaching.AdditiveDefault", layer: 8))); emitter1.ApplyParticleModule(new SetRandomColor(emitter1, hue1, hue1, 1f, 0.5f));

                switch (trailType)
                {
                    case TrailType.Spread:
                        emitter2.ApplyParticleModule(new SetRandomLife(emitter2, 60 * lifeFactor, 70 * lifeFactor));
                        emitter2.ApplyParticleModule(new VelocityOverLife(emitter2, (p, l) =>
                        {
                            return p.setVel * Mathf.Pow(1f - l, 4f) * p.randomParam1 + Vector2.down * l * 2f * (0.5f + 0.5f * p.randomParam2);
                        }));
                        break;
                    case TrailType.SpreadAndRotate:
                        emitter2.ApplyParticleModule(new SetRandomLife(emitter2, 20 * lifeFactor, 40 * lifeFactor));
                        emitter2.ApplyParticleModule(new VelocityOverLife(emitter2, (p, l) =>
                        {
                            float t = Mathf.Pow(1f - l, 4f);
                            Vector2 rotVel = Custom.DegToVec(p.randomParam1 * 360f) * 3f * t / lifeFactor;
                            return p.setVel * t + Vector2.down * l * 2f * (0.5f + 0.5f*p.randomParam2) * 0.3f + rotVel;
                        }));
                        break;
                }
                emitter2.ApplyParticleModule(new ScaleOverLife(emitter2, (p, l) =>
                {
                    return p.setScaleXY * (1f - l) * 0.4f;
                }));
                emitter2.ApplyParticleModule(new ColorOverLife(emitter2, (p, l) =>
                {
                    return Color.Lerp(Custom.HSL2RGB(hue1, 1f, 0.5f), Custom.HSL2RGB(hue2, 1f, 0.5f), l);
                }));

                emitter2.ApplyParticleModule(new AlphaOverLife(emitter2, (p, l) => (1f - l) * p.setAlpha));

                emitter2.OnEmitterActuallyDie += (emitter) =>
                {
                    emitter2 = null;
                };
                ParticleSystem.ApplyEmitterAndInit(emitter2);
            }
        }

        public static T RandomEnum<T>()where T : System.Enum
        {
            var enumType = typeof(T);
            string[] names = Enum.GetNames(enumType);
            return (T)Enum.Parse(enumType, names[Random.Range(0, names.Length - 1)]);
        }

        public enum TrailType
        {
            None,
            Spread,
            SpreadAndRotate,
            Simple
        }

        public enum InitWaitType
        {
            Instant,
            WaitSecs,
        }

        public enum IgnitCountType
        {
            Single,
            Angle,
            RandomCircle
        }

        public enum MoveType
        {
            Natural,
            Spin,
            SmallBias
        }

        public enum FadeType
        {
            Normal,
            Flash
        }

        public enum ColorType
        {
            Constant,
            Change
        }


        public class BindParticleToParticle : EmitterModule, IOwnParticleUniqueData
        {
            ParticleEmitter bindEmitter;
            bool carryVel;
            bool carryScale;
            bool carryAlpha;

            public BindParticleToParticle(ParticleEmitter emitter, ParticleEmitter bindEmitter, bool carryVel = true, bool carryScale = true, bool carryAlpha = true) : base(emitter)
            {
                this.bindEmitter = bindEmitter;
                this.carryVel = carryVel;
                this.carryScale = carryScale;
                this.carryAlpha = carryAlpha;

                bindEmitter.OnEmitterActuallyDie += (emitter) =>
                {
                    this.emitter.Die();
                };
            }

            public override void Update()
            {
                base.Update();
            }


            public Particle.ParticleUniqueData GetUniqueData(Particle particle)
            {
                var binder = new ParticleBinder() { bindParticle = bindEmitter.Particles[Random.Range(0, bindEmitter.Particles.Count)] };
                particle.HardSetPos(binder.bindParticle.pos);
                if (carryScale)
                    particle.HardSetScale(binder.bindParticle.scale);

                if (carryVel)
                    particle.SetVel(binder.bindParticle.vel);

                if (carryAlpha)
                    particle.HardSetAlpha(binder.bindParticle.alpha);
                return binder;
            }

            public class ParticleBinder : Particle.ParticleUniqueData
            {
                public Particle bindParticle;
            }
        }

        public class BindRateSpawner : SpawnModule
        {
            static float secPerFrame = 1 / 40f;
            ParticleEmitter bindEmitter;
            float secNeed;
            float baseSecNeed;
            float counter;

            public BindRateSpawner(ParticleEmitter emitter, int baseRate, ParticleEmitter bindEmitter, int maxParitcleCount) : base(emitter, maxParitcleCount)
            {
                this.bindEmitter = bindEmitter;
                baseSecNeed = 1f / (float)baseRate;
            }

            public override void Update()
            {
                if (emitter.slateForDeletion)
                    return;

                if (bindEmitter.Particles.Count == 0)
                    return;
                secNeed = baseSecNeed / bindEmitter.Particles.Count;

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

        public class BindBurstSpawner : SpawnModule
        {
            int low;
            int high;

            public BindBurstSpawner(ParticleEmitter emitter, ParticleEmitter bindEmitter, int burstCountLow, int burstCountHight, int maxParitcleCount) : base(emitter, maxParitcleCount)
            {
                low = burstCountLow;
                high = burstCountHight;
                bindEmitter.OnParticleInitEvent += Burst;
            }

            public void Burst(Particle _)
            {
                int count = Random.Range(low, high);
                for(int i = 0; i < count && emitter.Particles.Count < maxParitcleCount; i++)
                {
                    emitter.SpawnParticle();
                }
            }
        }
    }
}
