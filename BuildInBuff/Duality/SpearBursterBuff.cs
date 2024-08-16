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
            On.Creature.Die += Creature_Die;
        }

        private static void Creature_Die(On.Creature.orig_Die orig, Creature self)
        {
            orig.Invoke(self);
            BuffUtils.Log("SpearBurster", $"{self} killed by {self.killTag}");

        }

        private static void ScavengerBomb_Explode(On.ScavengerBomb.orig_Explode orig, ScavengerBomb self, BodyChunk hitChunk)
        {
            self.explosionIsForShow = true;
            orig.Invoke(self, hitChunk);
            if(self.thrownBy is Player)
                self.room.AddObject(new SpearBurster(self.room, self, self.firstChunk.pos, self.thrownBy.abstractCreature));
        }
    }

    internal class SpearBurster : CosmeticSprite
    {
        static int totalCircle = 3;
        static float angleStep = 3;
        static int timePerCircle = 20;
        static int breakPerCircle = 20;
        static int playSoundCounter = 2;
        public static Color scanlineCol = Helper.GetRGBColor(255, 49, 101);

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
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("SmallSpear", "", 8)));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 80, 80));
            emitter.ApplyParticleModule(new SetRandomScale(emitter, 1f, 1f));
            emitter.ApplyParticleModule(new SetCustomColor(emitter, (p) => blackColor));
            emitter.ApplyParticleModule(new SpearBursterPhysics(this, emitter));

            ParticleSystem.ApplyEmitterAndInit(emitter);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (slatedForDeletetion)
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

            while (angleStacker < angleStepedPerFrame)
            {
                emitterSpawner.Spawn(angle);
                angleStacker += angleStep;
                angle += angleStep;
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

            lastAngle = angle;
            if (angle >= 360f)
            {
                angle = circle * (angleStep / totalCircle);
                lastAngle = angle;
                angleStacker = 0f;
                breakCounter = breakPerCircle;
                circle++;
            }

            if (circle >= totalCircle)
                Destroy();
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            if (slatedForDeletetion)
                return;

            float smoothAngle = Mathf.Lerp(lastAngle, angle, timeStacker);
            Vector2 dir1 = Custom.DegToVec(smoothAngle - 10f);
            Vector2 end1 = pos + dir1 * 2000f;
            Vector2 dir2 = Custom.DegToVec(smoothAngle + 10f);
            Vector2 end2 = pos + dir2 * 2000f;

            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker) - camPos;

            TriangleMesh mesh1 = sLeaser.sprites[0] as TriangleMesh;
            mesh1.MoveVertice(0, smoothPos);
            mesh1.MoveVertice(1, end1 - camPos);
            mesh1.MoveVertice(2, end2 - camPos);

            float smoothParam = Random.value > 0.5f ? 1f : 0f;

            sLeaser.sprites[1].SetPosition(smoothPos);
            sLeaser.sprites[1].scale = smoothParam * 32f;
            sLeaser.sprites[1].alpha = smoothParam;
        }

        public override void Destroy()
        {
            base.Destroy();
            emitter.Die();
            emitterSpawner = null;
            emitter = null;
        }


        public class SpearBursterSpawnModule : SpawnModule
        {
            SpearBurster spearBurster;
            public SpearBursterSpawnModule(SpearBurster spearBurster, ParticleEmitter emitter, int maxParitcleCount) : base(emitter, maxParitcleCount)
            {
                this.spearBurster = spearBurster;
            }

            public void Spawn(float rotation)
            {
                emitter.SpawnParticle();
                var particle = emitter.Particles.Last();

                particle.HardSetPos(emitter.pos);
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
                if (particle.emitter.room.GetTile(particle.pos).Solid)
                {
                    particle.vel = Vector2.zero;
                }
                else
                    base.ApplyUpdate(particle);
            }

            public override Particle.ParticleUniqueData GetUniqueData(Particle particle)
            {
                return new SpearBursterHitData(this, particle);
            }

            internal class SpearBursterHitData : HitData
            {
                SpearBursterPhysics spearBursterPhysics;
                public SpearBursterHitData(SpearBursterPhysics physics, Particle particle) : base(particle)
                {
                    spearBursterPhysics = physics;
                }

                public override void ViolenceBehaviour(Creature creature, Particle particle, BodyChunk bodyChunk, PhysicalObject.Appendage.Pos appendagePos)
                {
                    creature.SetKillTag(spearBursterPhysics.spearBurster.killTag);
                    creature.Violence(spearBursterPhysics.spearBurster.bindBomb.firstChunk,creature is Player ? Vector2.zero : particle.vel * 0.1f, bodyChunk, null, Creature.DamageType.Stab, 0.42f, creature is Player ? 0f : 0.1f);
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
            }
        }
    }
}
