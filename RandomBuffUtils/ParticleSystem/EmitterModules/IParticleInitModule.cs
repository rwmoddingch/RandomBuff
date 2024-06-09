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
 
    public class SetRandomVelocity : EmitterModule, IParticleInitModule
    {
        Vector2 a;
        Vector2 b;

        public SetRandomVelocity(ParticleEmitter emitter, Vector2 a, Vector2 b) : base(emitter)
        {
            this.a = a;
            this.b = b;
        }

        public void ApplyInit(Particle particle)
        {
            Vector2 vel = Vector2.Lerp(a, b, Random.value);
            particle.SetVel(vel);
        }
    }

    public class SetSphericalVelocity : EmitterModule, IParticleInitModule
    {
        float a;
        float b;

        public SetSphericalVelocity(ParticleEmitter emitter, float vA, float vB) : base(emitter)
        {
            a = vA;
            b = vB;
        }

        public void ApplyInit(Particle particle)
        {
            particle.SetVel(Custom.RNV() * Random.Range(a, b));
        }
    }

    public class SetRandomPos : EmitterModule, IParticleInitModule
    {
        float rad;
        public SetRandomPos(ParticleEmitter emitter, float rad) : base(emitter)
        {
            this.rad = rad;
        }

        public void ApplyInit(Particle particle)
        {
            Vector2 pos = Custom.RNV() * rad * Random.value + emitter.pos;
            particle.HardSetPos(pos);
        }
    }

    public class SetMoveType : EmitterModule, IParticleInitModule
    {
        Particle.MoveType moveType;
        public SetMoveType(ParticleEmitter emitter, Particle.MoveType moveType) : base(emitter) 
        {
            this.moveType = moveType;
        }

        public void ApplyInit(Particle particle)
        {
            particle.moveType = moveType;
        }
    }

    public class SetRandomLife : EmitterModule, IParticleInitModule
    {
        int a;
        int b;

        public SetRandomLife(ParticleEmitter emitter, int a, int b) : base(emitter)
        {
            this.a = a;
            this.b = b;
        }

        public void ApplyInit(Particle particle)
        {
            int life = Random.Range(a, b);
            particle.SetLife(life);
        }
    }

    public class SetRandomColor : EmitterModule, IParticleInitModule
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

        public void ApplyInit(Particle particle)
        {
            Color color = Custom.HSL2RGB(Random.Range(hueA, hueB), saturation, lightness);
            particle.HardSetColor(color);
        }
    }

    public class SetConstColor : EmitterModule, IParticleInitModule
    {
        Color color;
        public SetConstColor(ParticleEmitter emitter, Color color) : base(emitter)
        {
            this.color = color;
        }

        public void ApplyInit(Particle particle)
        {
            particle.HardSetColor(color);
        }
    }

    public class SetRandomScale : EmitterModule, IParticleInitModule
    {
        Vector2 a;
        Vector2 b;

        public SetRandomScale(ParticleEmitter emitter, float a, float b) : this(emitter, new Vector2(a, a), new Vector2(b, b))
        {
        }

        public SetRandomScale(ParticleEmitter emitter, Vector2 a, Vector2 b) : base(emitter)
        {
            this.a = a;
            this.b = b;
        }

        public void ApplyInit(Particle particle)
        {
            Vector2 scale = Vector2.Lerp(a, b, Random.value);
            particle.HardSetScale(scale);
        }
    }

    public class SetRandomRotation : EmitterModule, IParticleInitModule
    {
        float a;
        float b;
        public SetRandomRotation(ParticleEmitter emitter, float rotationA, float rotationB) : base(emitter)
        {
            this.a = rotationA;
            this.b = rotationB;
        }

        public void ApplyInit(Particle particle)
        {
            float r = Mathf.Lerp(a, b, Random.value);
            particle.rotation = r;
        }
    }

    public class AddElement : EmitterModule, IParticleInitModule
    {
        Particle.SpriteInitParam[] spriteInitParam;

        public AddElement(ParticleEmitter emitter, params Particle.SpriteInitParam[] spriteInitParam) : base(emitter)
        {
            this.spriteInitParam = spriteInitParam;
        }

        public void ApplyInit(Particle particle)
        {
            int index = (int)(particle.randomParam1 * spriteInitParam.Length - 1);
            particle.spriteInitParams.Add(spriteInitParam[index]);
        }
    }

    public class SetVelociyFromEmitter : EmitterModule, IParticleInitModule
    {
        float t;

        public SetVelociyFromEmitter(ParticleEmitter emitter, float t) : base(emitter)
        {
            this.t = t;
        }

        public void ApplyInit(Particle particle)
        {
            Vector2 vel = particle.emitter.vel * t;
            particle.SetVel(vel);
        }
    }

    //public class SetShader : ParticleInitModule
    //{
    //    string shader;
    //    public SetShader(ParticleEmitter emitter, string shader) : base(emitter)
    //    {
    //        this.shader = shader;
    //    }

    //    public override void Apply(Particle particle)
    //    {
    //        particle.shader = shader;
    //    }
    //}
}
