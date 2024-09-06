using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuffUtils;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static BuiltinBuffs.Negative.SpearRainModule;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Duality
{
    internal class SpearBursterBuffEntry : IBuffEntry
    {
        public static BuffID spearBursterBuff = new BuffID("SpearBurster", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SpearBursterBuffEntry>(spearBursterBuff);
        }
        public static void HookOn()
        {
            On.ScavengerBomb.Explode += ScavengerBomb_Explode;
        }

        private static void ScavengerBomb_Explode(On.ScavengerBomb.orig_Explode orig, ScavengerBomb self, BodyChunk hitChunk)
        {
            if(self.thrownBy is Player)
            {
                self.explosionIsForShow = true;
                self.room.AddObject(new SpearBurster(self.room, self, self.firstChunk.pos, self.thrownBy.abstractCreature));
            }
            orig.Invoke(self, hitChunk);
        }
    }

    internal class SpearBurster : CosmeticSprite
    {
        static int totalCircle = 3;
        static float angleStep = 3;
        static int timePerCircle = 20;
        static int breakPerCircle = 10;
        static int playSoundCounter = 2;
        public static Color scanlineCol = Helper.GetRGBColor(255, 49, 101);
        public static Color scanLineCol2 = Helper.GetRGBColor(139, 0, 255);

        ScavengerBomb bindBomb;
        AbstractCreature killTag;
        float angleStepedPerFrame;

        ParticleEmitter emitter;
        SpearBursterSpawnModule emitterSpawner;

        float angle;
        float lastAngle;
        float angleStacker;
        float totalAngleStacker;
        float rad;

        int circle;
        int breakCounter;

        bool burstFinish;

        int soundCounter = playSoundCounter;

        Color blackColor = Color.black;


        public SpearBurster(Room room, ScavengerBomb bindBomb, Vector2 pos, AbstractCreature killTag)
        {
            this.room = room;
            lastPos = this.pos = pos;
            this.bindBomb = bindBomb;
            angleStepedPerFrame = 360f / timePerCircle;

            InitEmitter();
            this.killTag = killTag;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = new TriangleMesh("Futile_White", new TriangleMesh.Triangle[1] { new TriangleMesh.Triangle(0, 1, 2) }, true) { shader = rCam.game.rainWorld.Shaders["Hologram"] };
            sLeaser.sprites[1] = new FSprite("Futile_White") { shader = rCam.game.rainWorld.Shaders["LightSource"] };

            AddToContainer(sLeaser, rCam, null);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[0]);
            rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[1]);
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            Color fadeCol = scanlineCol;
            fadeCol.a = 0f;

            TriangleMesh mesh1 = sLeaser.sprites[0] as TriangleMesh;
            mesh1.verticeColors[0] = scanlineCol;
            mesh1.verticeColors[1] = fadeCol;
            mesh1.verticeColors[2] = fadeCol;
            sLeaser.sprites[1].color = scanlineCol;

            blackColor = palette.blackColor;
        }

        public void InitEmitter()
        {
            emitter = new ParticleEmitter(room);
            emitter.pos = emitter.lastPos = pos;

            emitter.ApplyParticleSpawn(emitterSpawner = new SpearBursterSpawnModule(this, emitter, 400));

            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("SmallSpear", "", 4)));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 80, 80));
            emitter.ApplyParticleModule(new SetRandomScale(emitter, 1f, 1f));
            emitter.ApplyParticleModule(new SetCustomColor(emitter, (p) => blackColor));
            emitter.ApplyParticleModule(new SpearBursterPhysics(this, emitter));
            emitter.ApplyParticleModule(new ColorOverLife(emitter, (p, l) =>
            {
                return Color.Lerp(scanlineCol, blackColor, l * 10f);
            }));

            ParticleSystem.ApplyEmitterAndInit(emitter);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (slatedForDeletetion)
                return;

            if (burstFinish && emitter.Particles.Count == 0)
                Destroy();

            if (burstFinish)
                return;

            if (breakCounter > 0)
            {
                breakCounter--;
                return;
            }

            if (!bindBomb.slatedForDeletetion)
            {
                bindBomb.firstChunk.HardSetPosition(pos);
                bindBomb.firstChunk.vel = Vector2.zero;
            }

            lastAngle = angle;
            float delta;
            while (angleStacker < angleStepedPerFrame)
            {
                angleStacker += angleStep;
                angle += angleStep;
                if (Vector2.Distance(pos, Helper.GetContactPos(pos, Custom.DegToVec(angle), room)) < 20f)
                    continue;
                delta = -angleStacker / angleStepedPerFrame ;
                emitterSpawner.Spawn(angle, delta);    
            }
            angleStacker -= angleStepedPerFrame;
            
            if(soundCounter > 0)
            {
                soundCounter--;
                if(soundCounter == 0)
                {
                    room.PlaySound(SoundID.King_Vulture_Tusk_Shoot, pos, 0.5f, 10f);
                    soundCounter = playSoundCounter;
                }
            }

            if (angle >= 360f)
            {
                circle++;
                angle = circle * (angleStep / totalCircle);
                lastAngle = angle;
                angleStacker = 0f;
                breakCounter = breakPerCircle;
            }

            if (circle >= totalCircle)
                burstFinish = true;
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            if (slatedForDeletetion)
                return;

            float smoothParam1 = (Random.value > 0.5f ? 1f : 0f) * (burstFinish ? 0f : 1f);
            float smoothParam2 = (Random.value > 0.5f ? 1f : 0f) * (burstFinish ? 0f : 1f);
            float smoothAngle = Mathf.Lerp(lastAngle, angle, timeStacker);
            Vector2 dir1 = Custom.DegToVec(smoothAngle - 10f);
            Vector2 end1 = pos + dir1 * 1000f;
            Vector2 dir2 = Custom.DegToVec(smoothAngle + 10f);
            Vector2 end2 = pos + dir2 * 1000f;

            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker) - camPos;

            TriangleMesh mesh1 = sLeaser.sprites[0] as TriangleMesh;

            mesh1.MoveVertice(0, smoothPos);
            mesh1.MoveVertice(1, end1 - camPos);
            mesh1.MoveVertice(2, end2 - camPos);

            mesh1.verticeColors[0].a = smoothParam1;
            if (burstFinish)
                mesh1.isVisible = false;

            sLeaser.sprites[1].SetPosition(smoothPos);
            sLeaser.sprites[1].scale = smoothParam2 * 32f;
            sLeaser.sprites[1].alpha = smoothParam2;
        }

        public override void Destroy()
        {
            base.Destroy();
            emitter.Die();
            emitterSpawner = null;
            emitter = null;
            if (!bindBomb.slatedForDeletetion)
                bindBomb.explosionIsForShow = false;
            bindBomb = null;
        }


        public class SpearBursterSpawnModule : SpawnModule
        {
            SpearBurster spearBurster;
            public SpearBursterSpawnModule(SpearBurster spearBurster, ParticleEmitter emitter, int maxParitcleCount) : base(emitter, maxParitcleCount)
            {
                this.spearBurster = spearBurster;
            }

            public void Spawn(float rotation, float delta)
            {
                emitter.SpawnParticle();
                var particle = emitter.Particles.Last();

                particle.HardSetPos(emitter.pos + Custom.DegToVec(rotation) * 30f * delta);
                particle.HardSetRotation(rotation);
                particle.SetVel(Custom.DegToVec(rotation) * 30f);
            }
        }

        internal class SpearBursterPhysics : SpearHitAndStuck
        {
            SpearBurster spearBurster;
            public SpearBursterPhysics(SpearBurster spearBurster, ParticleEmitter emitter) : base(emitter, 0f)
            {
                this.spearBurster = spearBurster;
            }

            public override void ApplyUpdate(Particle particle)
            {
                var data = particle.GetUniqueData<SpearBursterHitData>(this);

                if (data == null)
                {
                    if(particle.emitter.room.GetTile(particle.pos).Solid)
                    {
                        particle.vel = Vector2.zero;
                    }
                    else
                        base.ApplyUpdate(particle);
                }
                else
                {
                    if(!data.onWall)
                    {
                        if (particle.emitter.room.GetTile(particle.pos).Solid && !data.StuckInCreature)
                        {
                            data.wallPos = Helper.GetContactPos(particle.lastPos, particle.vel.normalized, particle.emitter.room);

                            for (int i = 0; i < 5; i++)
                            {
                                particle.emitter.room.AddObject(new Spark(data.wallPos, (-particle.vel.normalized + Custom.RNV() * 0.1f) * 5f, Color.Lerp(scanLineCol2,scanlineCol,Random.value * 0.5f + 0.5f) , null, 10 + Random.Range(0,10), 20));
                            }
                            particle.pos = data.wallPos;
                            particle.vel = Vector2.zero;
                            data.onWall = true;

                        }
                        base.ApplyUpdate(particle);
                    }
                }          
            }

            public override Particle.ParticleUniqueData GetUniqueData(Particle particle)
            {
                return new SpearBursterHitData(this, particle);
            }

            public override bool HitThisObject(PhysicalObject obj)
            {
                return base.HitThisObject(obj) || obj is Weapon;
            }

            internal class SpearBursterHitData : HitData
            {
                SpearBursterPhysics spearBursterPhysics;
                public Vector2 wallPos;
                public bool onWall;

                public SpearBursterHitData(SpearBursterPhysics physics, Particle particle) : base(particle)
                {
                    spearBursterPhysics = physics;
                    wallPos = Helper.GetContactPos(particle.pos, particle.vel.normalized, particle.emitter.room);
                }

                public override void ViolenceBehaviour(Creature creature, Particle particle, BodyChunk bodyChunk, PhysicalObject.Appendage.Pos appendagePos)
                {
                    creature.SetKillTag(spearBursterPhysics.spearBurster.killTag);
                    creature.Violence(spearBursterPhysics.spearBurster.bindBomb.firstChunk, creature is Player ? Vector2.zero : particle.vel * 0.1f, bodyChunk, null, Creature.DamageType.Stab, 0.42f, creature is Player ? 0f : 0.1f);
                    creature.Violence(spearBursterPhysics.spearBurster.bindBomb.firstChunk, null, bodyChunk, null, Creature.DamageType.Explosion, 0.08f, 0f);
                    if(appendagePos != null)
                    {
                        creature.Violence(spearBursterPhysics.spearBurster.bindBomb.firstChunk, null, bodyChunk, appendagePos, Creature.DamageType.Stab, 1f, 0f);
                    }
                    //if(ExterminationCondition.TryGetRecord(creature, out var record))
                    //{
                    //    record.sourceObjType = AbstractPhysicalObject.AbstractObjectType.ScavengerBomb;
                    //}
                }

                public override void HitObjectBehaviour(SharedPhysics.CollisionResult result, Particle p)
                {
                    var weapon = result.obj as Weapon;
                    if (weapon.mode == Weapon.Mode.Thrown)
                    {
                        weapon.WeaponDeflect((p.pos + weapon.firstChunk.pos) / 2f, (weapon.firstChunk.pos - p.pos).normalized, 15f);
                        p.Die();
                    }
                }
            }
        }
    }
}
