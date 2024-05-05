using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuffUtils.ParticleSystem.EmitterModules
{
    public abstract class EmitterModule
    {
        public ParticleEmitter emitter;

        public EmitterModule(ParticleEmitter emitter)
        {
            this.emitter = emitter;
        }

        public virtual void Init()
        {
        }

        public virtual void Update()
        {
        }
    }

    public sealed class SetEmitterLife : EmitterModule
    {
        int setLife;
        int life;
        bool loop;
        bool killOnFinish;
        public SetEmitterLife(ParticleEmitter emitter, int life, bool loop, bool killOnFinish = true) : base(emitter)
        {
            this.setLife = life;
            this.life = life;
            this.loop = loop;
            this.killOnFinish = killOnFinish;
        }

        public override void Init()
        {
            base.Init();
            life = setLife;
        }

        public override void Update()
        {
            if (life > 0)
            {
                life--;
                if(life == 0)
                {
                    if (loop)
                        emitter.Init();
                    else if (killOnFinish)
                        emitter.Die();
                }
            }
        }
    }
}
