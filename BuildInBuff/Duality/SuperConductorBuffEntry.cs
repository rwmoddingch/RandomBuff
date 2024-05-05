using Mono.Cecil;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
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
using static BuiltinBuffs.Duality.SuperConductorBuff;
using static RandomBuffUtils.PlayerUtils;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Duality
{
    internal class SuperConductorBuff : Buff<SuperConductorBuff, SuperConductorBuffData>
    {
        public override BuffID ID => SuperConductorBuffEntry.superConductorBuffID;

        public SuperConductorBuff()
        {
            BuffUtils.Log("SuperConductorBuff", "ctor");
        }

        bool keyDown;
        public override void Update(RainWorldGame game)
        {
            bool down = Input.GetKey(KeyCode.E);
            if(down && !keyDown)
            {
                var emitter = new ParticleEmitter(game.cameras[0].room);
                emitter.pos = game.Players[0].realizedCreature.DangerPos;

                emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 120, true));
                emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, 1));

                emitter.ApplyParticleInit(new SetElement(emitter, "Futile_White"));
                emitter.ApplyParticleInit(new SetShader(emitter, "FlatLight"));
                emitter.ApplyParticleInit(new SetMoveType(emitter, Particle.MoveType.Global));
                emitter.ApplyParticleInit(new SetRandomPos(emitter, 0f));
                emitter.ApplyParticleInit(new SetRandomLife(emitter, 70, 80));
                emitter.ApplyParticleInit(new SetRandomColor(emitter, 0f, 0.4f, 1f, 0.5f));
                emitter.ApplyParticleInit(new SetRandomVelocity(emitter, new Vector2(-1f, 10f), new Vector2(1f, 10f)));
                emitter.ApplyParticleInit(new SetRandomScale(emitter, 1f, 1.5f));

                emitter.ApplyParticleUpdate(new ScaleOverLife(emitter, (particle, lifeParam) =>
                {
                    return particle.setScale * (Mathf.Sin(lifeParam * 10f) * 0.3f + 0.7f);
                }));
                emitter.ApplyParticleUpdate(new ColorOverLife(emitter, (particle, lifeParam) =>
                {
                    return Color.Lerp(particle.setColor, Color.white, (Mathf.Sin(lifeParam * 10f) * 0.5f + 0.5f));
                }));
                emitter.ApplyParticleUpdate(new VelocityOverLife(emitter, (particle, lifeParam) =>
                {
                    float sin = Mathf.Sin(lifeParam * 10f);
                    return new Vector2(particle.setVel.x + sin * (1f - lifeParam), particle.setVel.y * (1f - lifeParam));
                }));
                ParticleSystem.ApplyEmitterAndInit(emitter);

                int range = 60;
                for(int angle = 0; angle < 360; angle += range)
                {
                    CreateSubParticle(angle, range, 1f, 0f, emitter);
                }
            }
            keyDown = down;
        }

        void CreateSubParticle(float angle, float range, float vel, float hue, ParticleEmitter owner)
        {
            Vector2 velA = Custom.DegToVec(angle) * vel;
            Vector2 velB = Custom.DegToVec(range + angle) * vel;

            owner.OnParticleDieEvent += CreateEmitter;

            void CreateEmitter(Particle particle)
            {
                var emitter = new ParticleEmitter(particle.emitter.room);
                emitter.pos = particle.pos;

                emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 100, false));
                emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, 5));

                emitter.ApplyParticleInit(new SetElement(emitter, "Futile_White"));
                emitter.ApplyParticleInit(new SetShader(emitter, "FlatLight"));
                emitter.ApplyParticleInit(new SetMoveType(emitter, Particle.MoveType.Global));
                emitter.ApplyParticleInit(new SetRandomPos(emitter, 0f));
                emitter.ApplyParticleInit(new SetRandomLife(emitter, 40, 80));
                emitter.ApplyParticleInit(new SetRandomColor(emitter, hue, hue + 0.4f, 1f, 0.5f));
                emitter.ApplyParticleInit(new SetRandomVelocity(emitter, velA, velB));
                emitter.ApplyParticleInit(new SetRandomScale(emitter, 0.5f, 0.8f));

                emitter.ApplyParticleUpdate(new ConstantAcc(emitter, new Vector2(0f, -0.5f)));

                emitter.ApplyParticleUpdate(new ColorOverLife(emitter, (p, lifeParam) =>
                {
                    return Color.Lerp(p.setColor, Color.white, (Mathf.Sin(lifeParam * 3f) * 0.5f + 0.5f));
                }));
                emitter.ApplyParticleUpdate(new ScaleOverLife(emitter, (p, lifeParam) =>
                {
                    return p.setScale * (Mathf.Sin(lifeParam * 10f) * 0.3f + 0.7f) * (1f - lifeParam);
                }));
                ParticleSystem.ApplyEmitterAndInit(emitter);
            }
        }

        public override void Destroy()
        {
        }
    }

    internal class SuperConductorBuffData : BuffData
    {
        public override BuffID ID => SuperConductorBuffEntry.superConductorBuffID;
    }

    internal class SuperConductorBuffEntry : IBuffEntry
    {
        public static BuffID superConductorBuffID = new BuffID("SuperConductor", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SuperConductorBuff, SuperConductorBuffData, SuperConductorBuffEntry >(superConductorBuffID);
        }

        public static void HookOn()
        {
            On.Creature.Violence += Creature_Violence;
            On.Centipede.Shock += Centipede_Shock;
            On.Lizard.Violence += Lizard_Violence;
        }

        private static void Lizard_Violence(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
        {
            orig.Invoke(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
            TryAddArcThrower(source, type, damage, self);
        }

        private static void Centipede_Shock(On.Centipede.orig_Shock orig, Centipede self, PhysicalObject shockObj)
        {
            orig.Invoke(self, shockObj);
            if (ArcThrower.source2throwerMapper.ContainsKey(self.mainBodyChunk))
                return;
            self.room.AddObject(new ArcThrower(self.room, self.mainBodyChunk, self, Mathf.Max(self.TotalMass, 1f)));
        }

        private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            orig.Invoke(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
            TryAddArcThrower(source, type, damage, self);
        }

        static void TryAddArcThrower(BodyChunk source, Creature.DamageType type, float damage, Creature self)
        {
            if (type == Creature.DamageType.Electric)
            {
                BuffUtils.Log("SuperConductor", $"Edmg {damage}");
                if (ArcThrower.source2throwerMapper.ContainsKey(source))
                    return;

                if(source.owner is Weapon weapon && weapon.thrownBy != null)
                    self.room.AddObject(new ArcThrower(self.room, weapon.thrownBy.firstChunk, self, Mathf.Max(damage, 1f)));
                else
                    self.room.AddObject(new ArcThrower(self.room, source, self, Mathf.Max(damage, 1f)));

            }
        }

        internal class ArcThrower : UpdatableAndDeletable
        {
            public static Dictionary<BodyChunk, ArcThrower> source2throwerMapper = new Dictionary<BodyChunk, ArcThrower>();
            public float dmgAttenuation = 0.3f;
            static float LightningColorHue = 182f / 360f;

            List<PhysicalObject> excludeObjs = new List<PhysicalObject>();
            BodyChunk source;

            PhysicalObject throwSource;
            float currentDmg;

            int throwArcDelay;

            public ArcThrower(Room room ,BodyChunk source, PhysicalObject throwSource, float startDmg)
            {
                this.room = room;
                this.source = source;
                excludeObjs.Add(source.owner);
                if (source.owner is Player)
                    dmgAttenuation = 0.1f;
                excludeObjs.Add(throwSource);
                this.throwSource = throwSource;
                this.currentDmg = startDmg;

                source2throwerMapper.Add(source, this);
                BuffUtils.Log("SuperConductor",$"Create ArcThrower for {source.owner}");
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                if (slatedForDeletetion)
                    return;

                if (throwArcDelay == 0)
                {
                    ThrowNextArc();
                    throwArcDelay = 0;
                }
                else
                    throwArcDelay--;
            }

            public override void Destroy()
            {
                base.Destroy();
                source2throwerMapper.Remove(source);
            }

            public void ThrowNextArc()
            {
                var targets = from updates in room.updateList
                              where updates is Creature
                              where !excludeObjs.Contains(updates)
                              select updates as Creature;
                BuffUtils.Log("SuperConductor", $"ArcThrower ThrowNextArc");
                if (targets.Count() == 0)
                {
                    Destroy();
                    return;
                }

                currentDmg -= dmgAttenuation;
                if (currentDmg <= 0f)
                {
                    Destroy();
                    return;
                }

                Creature closestTarget = null;
                float distance = float.MaxValue;
                foreach(var target in targets)
                {
                    float dist = (target.DangerPos - throwSource.firstChunk.pos).magnitude;
                    if (dist < distance)
                    {
                        distance = dist;
                        closestTarget = target;
                    }
                }
                excludeObjs.Add(closestTarget);
                var Lightning = new LightningBolt(throwSource.firstChunk.pos, closestTarget.firstChunk.pos, 0, Mathf.Clamp(currentDmg, 0.1f, 1f), 0.25f, 0.5f, LightningColorHue, true);
                Lightning.intensity = 1f;
                room.AddObject(Lightning);
                room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, closestTarget.firstChunk.pos, currentDmg, 1.4f - Random.value * 0.4f);
                //room.PlaySound(SoundID.Zapper_Zap, closestTarget.firstChunk.pos, currentDmg, 1.4f - Random.value * 0.4f);
                //room.PlaySound(SoundID.Bomb_Explode, closestTarget.firstChunk.pos, 1f, 1.4f - Random.value * 0.4f);

                closestTarget.SetKillTag((source.owner as Creature).abstractCreature);
                closestTarget.Violence(source, null, closestTarget.firstChunk, null, Creature.DamageType.Electric, currentDmg, currentDmg * 10);
                throwSource = closestTarget;
            }
        }
    }
}
