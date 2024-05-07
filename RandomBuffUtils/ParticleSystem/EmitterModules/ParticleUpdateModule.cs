using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuffUtils.ParticleSystem.EmitterModules
{
    public class ParticleUpdateModule : EmitterModule
    {
        public ParticleUpdateModule(ParticleEmitter emitter) : base(emitter)
        {
        }

        public virtual void UpdateApply(Particle particle)
        {
        }
    }

    public class ConstantAcc : ParticleUpdateModule
    {
        Vector2 force;
        public ConstantAcc(ParticleEmitter emitter, Vector2 acc) : base(emitter)
        {
            this.force = acc;
        }

        public override void UpdateApply(Particle particle)
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

    public class ScaleOverLife : ParticleUpdateModule
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

        public override void UpdateApply(Particle particle)
        {
            particle.scaleXY = scaleFunc.Invoke(particle, particle.LifeParam);
        }
    }

    public class ColorOverLife : ParticleUpdateModule
    {
        Func<Particle, float, Color> colorFunc;
        public ColorOverLife(ParticleEmitter emitter, Func<Particle, float, Color> colorFunc) : base(emitter)
        {
            this.colorFunc = colorFunc;
        }

        public override void UpdateApply(Particle particle)
        {
            particle.color = colorFunc.Invoke(particle, particle.LifeParam);
        }
    }

    public class VelocityOverLife : ParticleUpdateModule
    {
        Func<Particle, float, Vector2> velFunc;
        public VelocityOverLife(ParticleEmitter emitter, Func<Particle, float, Vector2> velFunc) : base(emitter)
        {
            this.velFunc = velFunc;
        }

        public override void UpdateApply(Particle particle)
        {
            particle.vel = velFunc.Invoke(particle, particle.LifeParam);
        }
    }

    public class RotationOverLife : ParticleUpdateModule
    {
        Func<Particle, float, float> rotationFunc;

        public RotationOverLife(ParticleEmitter emitter, Func<Particle, float, float> rotationFunc) : base(emitter)
        {
            this.rotationFunc = rotationFunc;
        }

        public override void UpdateApply(Particle particle)
        {
            particle.rotation = rotationFunc.Invoke(particle, particle.LifeParam);
        }
    }
}
