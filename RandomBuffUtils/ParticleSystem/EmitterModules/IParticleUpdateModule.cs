using RWCustom;
using System;
using UnityEngine;
using static SharedPhysics;

namespace RandomBuffUtils.ParticleSystem.EmitterModules
{

    public class ConstantAcc : EmitterModule, IParticleUpdateModule
    {
        Vector2 force;
        public ConstantAcc(ParticleEmitter emitter, Vector2 acc) : base(emitter)
        {
            this.force = acc;
        }

        public void ApplyUpdate(Particle particle)
        {
            particle.vel += force / 40f;
        }
    }

    public class Gravity : ConstantAcc
    {
        public Gravity(ParticleEmitter emitter, float g) : base(emitter, new Vector2(0f, -g))
        {
        }
    }

    public class ScaleOverLife : EmitterModule, IParticleUpdateModule
    {
        Func<Particle, float, Vector2> scaleFunc;
        public ScaleOverLife(ParticleEmitter emitter, Func<Particle, float, float> scaleFunc) : base(emitter)
        {
            this.scaleFunc = new Func<Particle, float, Vector2>((p, life) =>
            {
                float a = scaleFunc.Invoke(p, life);
                return new Vector2(a, a);
            });
        }

        public ScaleOverLife(ParticleEmitter emitter, Func<Particle, float , Vector2> scaleFunc) : base(emitter)
        {
            this.scaleFunc = scaleFunc;
        }

        public void ApplyUpdate(Particle particle)
        {
            particle.scaleXY = scaleFunc.Invoke(particle, particle.LifeParam);
        }
    }

    public class ColorOverLife : EmitterModule, IParticleUpdateModule
    {
        Func<Particle, float, Color> colorFunc;
        public ColorOverLife(ParticleEmitter emitter, Func<Particle, float, Color> colorFunc) : base(emitter)
        {
            this.colorFunc = colorFunc;
        }

        public void ApplyUpdate(Particle particle)
        {
            particle.color = colorFunc.Invoke(particle, particle.LifeParam);
        }
    }

    public class VelocityOverLife : EmitterModule, IParticleUpdateModule
    {
        Func<Particle, float, Vector2> velFunc;
        public VelocityOverLife(ParticleEmitter emitter, Func<Particle, float, Vector2> velFunc) : base(emitter)
        {
            this.velFunc = velFunc;
        }

        public void ApplyUpdate(Particle particle)
        {
            particle.vel = velFunc.Invoke(particle, particle.LifeParam);
        }
    }

    public class PositionOverLife : EmitterModule, IParticleUpdateModule
    {
        Func<Particle, float, Vector2> posFunc;
        public PositionOverLife(ParticleEmitter emitter, Func<Particle, float, Vector2> posFunc) : base(emitter)
        {
            this.posFunc = posFunc;
        }

        public void ApplyUpdate(Particle particle)
        {
            particle.pos = posFunc.Invoke(particle, particle.LifeParam);
        }
    }

    public class RotationOverLife : EmitterModule, IParticleUpdateModule
    {
        Func<Particle, float, float> rotationFunc;

        public RotationOverLife(ParticleEmitter emitter, Func<Particle, float, float> rotationFunc) : base(emitter)
        {
            this.rotationFunc = rotationFunc;
        }

        public void ApplyUpdate(Particle particle)
        {
            particle.rotation = rotationFunc.Invoke(particle, particle.LifeParam);
        }
    }

    public class AlphaOverLife : EmitterModule, IParticleUpdateModule
    {
        Func<Particle, float, float> aFunc;
        public AlphaOverLife(ParticleEmitter emitter, Func<Particle, float, float> aFunc) : base(emitter)
        {
            this.aFunc = aFunc;
        }

        public void ApplyUpdate(Particle particle)
        {
            particle.alpha = aFunc.Invoke(particle, particle.LifeParam);
        }
    }

    public class SimpleParticlePhysic : EmitterModule, IParticleUpdateModule, IOwnParticleUniqueData
    {
        bool bounce;
        bool die;
        float velDamping;

        public Action<Particle> OnTerrainCollide;

        public SimpleParticlePhysic(ParticleEmitter emitter, bool bounce, bool die, float velDamping = 0.8f) : base(emitter)
        {
            this.bounce = bounce;
            this.die = die;
            this.velDamping = velDamping;
        }

        public void ApplyUpdate(Particle particle)
        {
            var data = particle.GetUniqueData<PhysicData>(this);
            if (data == null)
                return;
            var currentTile = particle.emitter.room.GetTile(particle.pos);
            //var lastTile = particle.emitter.room.GetTile(particle.lastPos);
            //var predictTile = particle.emitter.room.GetTile(particle.pos + particle.vel);

            if (!bounce || die)
            {
                if (currentTile.Solid)
                {
                    particle.vel = Vector2.zero;
                    OnTerrainCollide?.Invoke(particle);
                    if (die)
                        particle.Die();
                }
            }
            else
            {
                bool flag = false;
                IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(particle.emitter.room, particle.emitter.room.GetTilePosition(particle.emitter.lastPos), particle.emitter.room.GetTilePosition(particle.emitter.pos));
                if (intVector != null)
                {
                    FloatRect floatRect = Custom.RectCollision(particle.pos, particle.lastPos, particle.emitter.room.TileRect(intVector.Value).Grow(4f));
                    particle.pos = floatRect.GetCorner(FloatRect.CornerLabel.D);
                    if (floatRect.GetCorner(FloatRect.CornerLabel.B).x < 0f)
                    {
                        particle.vel.x = Mathf.Abs(particle.vel.x) * velDamping;
                        particle.vel.y = particle.vel.y * velDamping;
                        flag = true;
                    }
                    else if (floatRect.GetCorner(FloatRect.CornerLabel.B).x > 0f)
                    {
                        particle.vel.x = -Mathf.Abs(particle.vel.x) * velDamping;
                        particle.vel.y = particle.vel.y * velDamping;
                        flag = true;
                    }
                    else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y < 0f)
                    {
                        particle.vel.y = Mathf.Abs(particle.vel.y) * velDamping;
                        particle.vel.x = particle.vel.x * velDamping;
                        flag = true;
                    }
                    else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y > 0f)
                    {
                        particle.vel.y = -Mathf.Abs(particle.vel.y) * velDamping;
                        particle.vel.x = particle.vel.x * velDamping;
                        flag = true;
                    }
                }
                if (!flag)
                {
                    Vector2 vector5 = particle.vel;
                    SharedPhysics.TerrainCollisionData terrainCollisionData = data.scratchTerrainCollisionData.Set(particle.pos, particle.lastPos, particle.vel, 4f, new IntVector2(0, 0), true);
                    terrainCollisionData = SharedPhysics.VerticalCollision(particle.emitter.room, terrainCollisionData);
                    terrainCollisionData = SharedPhysics.HorizontalCollision(particle.emitter.room, terrainCollisionData);
                    terrainCollisionData = SharedPhysics.SlopesVertically(particle.emitter.room, terrainCollisionData);
                    particle.pos = terrainCollisionData.pos;
                    particle.vel = terrainCollisionData.vel;
                    if (terrainCollisionData.contactPoint.x != 0)
                    {
                        particle.vel.x = Mathf.Abs(vector5.x) * velDamping * -(float)terrainCollisionData.contactPoint.x;
                        particle.vel.y = particle.vel.y * velDamping;
                        flag = true;
                    }
                    if (terrainCollisionData.contactPoint.y != 0)
                    {
                        particle.vel.y = Mathf.Abs(vector5.y) * velDamping * -(float)terrainCollisionData.contactPoint.y;
                        particle.vel.x = particle.vel.x * velDamping;
                        flag = true;
                    }
                }
                if(flag)
                    OnTerrainCollide?.Invoke(particle);
            }
        }

        public Particle.ParticleUniqueData GetUniqueData(Particle particle)
        {
            return new PhysicData();
        }

        public class PhysicData : Particle.ParticleUniqueData
        {
            public SharedPhysics.TerrainCollisionData scratchTerrainCollisionData;
        }
    }
}
