using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RandomBuffUtils.ParticleSystem.EmitterModules
{
    public class ParticleInitModule : EmitterModule
    {
        public ParticleInitModule(ParticleEmitter emitter) : base(emitter)
        {
        }

        public virtual void Apply(Particle particle)
        {
        }
    }

    public class SetRandomVelocity : ParticleInitModule
    {
        Vector2 a;
        Vector2 b;

        public SetRandomVelocity(ParticleEmitter emitter, Vector2 a, Vector2 b) : base(emitter)
        {
            this.a = a;
            this.b = b;
        }

        public override void Apply(Particle particle)
        {
            Vector2 vel = Vector2.Lerp(a, b, Random.value);
            particle.SetVel(vel);
        }
    }

    public class SetRandomPos : ParticleInitModule
    {
        float rad;
        public SetRandomPos(ParticleEmitter emitter, float rad) : base(emitter)
        {
            this.rad = rad;
        }

        public override void Apply(Particle particle)
        {
            Vector2 pos = Custom.RNV() * rad * Random.value + emitter.pos;
            particle.HardSetPos(pos);
        }
    }

    public class SetMoveType : ParticleInitModule
    {
        Particle.MoveType moveType;
        public SetMoveType(ParticleEmitter emitter, Particle.MoveType moveType) : base(emitter) 
        {
            this.moveType = moveType;
        }

        public override void Apply(Particle particle)
        {
            particle.moveType = moveType;
        }
    }

    public class SetRandomLife : ParticleInitModule
    {
        int a;
        int b;

        public SetRandomLife(ParticleEmitter emitter, int a, int b) : base(emitter)
        {
            this.a = a;
            this.b = b;
        }

        public override void Apply(Particle particle)
        {
            int life = Random.Range(a, b);
            particle.SetLife(life);
        }
    }

    public class SetRandomColor : ParticleInitModule
    {
        float hueA;
        float hueB;

        float saturation;
        float lightness;

        public SetRandomColor(ParticleEmitter emitter, float hueA, float hueB, float saturation, float lightness) : base(emitter)
        {
            this.hueA = hueA;
            this.hueB = hueB;

            this.lightness = lightness;
            this.saturation = saturation;
        }

        public override void Apply(Particle particle)
        {
            Color color = Custom.HSL2RGB(Random.Range(hueA, hueB), saturation, lightness);
            particle.HardSetColor(color);
        }
    }

    public class SetRandomScale : ParticleInitModule
    {
        float a;
        float b;

        public SetRandomScale(ParticleEmitter emitter, float a, float b) : base(emitter)
        {
            this.a = a;
            this.b = b;
        }

        public override void Apply(Particle particle)
        {
            float scale = Random.Range(a, b);
            particle.HardSetScale(scale);
        }
    }

    public class SetElement : ParticleInitModule
    {
        string element;
        public SetElement(ParticleEmitter emitter, string element) : base(emitter)
        {
            this.element = element;
        }

        public override void Apply(Particle particle)
        {
            particle.element = element;
        }
    }

    public class SetShader : ParticleInitModule
    {
        string shader;
        public SetShader(ParticleEmitter emitter, string shader) : base(emitter)
        {
            this.shader = shader;
        }

        public override void Apply(Particle particle)
        {
            particle.shader = shader;
        }
    }
}
