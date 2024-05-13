using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuffUtils.ParticleSystem.EmitterModules
{
    public abstract class SpawnModule : EmitterModule
    {
        public int maxParitcleCount;
        public SpawnModule(ParticleEmitter emitter, int maxParitcleCount) : base(emitter)
        {
            this.maxParitcleCount = maxParitcleCount;
        }
    }

    public sealed class RateSpawnerModule : SpawnModule
    {
        static float secPerFrame = 1 / 40f;
        float secNeed;
        float counter;

        public RateSpawnerModule(ParticleEmitter emitter, int maxParticleCount, int ratePerSec) : base(emitter, maxParticleCount)
        {
            secNeed = 1f / ratePerSec;
        }

        public override void Init()
        {
            counter = 0;
        }

        public override void Update()
        {
            base.Update();

            if (emitter.slateForDeletion)
                return;

            counter += secPerFrame;
            while(counter > secNeed)
            {
                counter -= secNeed;
                if(emitter.Particles.Count < maxParitcleCount)
                {
                    emitter.SpawnParticle();
                }
            }
        }
    }

    public sealed class BurstSpawnerModule : SpawnModule
    {
        bool emitted;
        public BurstSpawnerModule(ParticleEmitter emitter, int count) : base(emitter, count)
        {
        }

        public override void Init()
        {
            emitted = false;
        }

        public override void Update()
        {
            if (!emitted)
            {
                for (int i = 0; i < maxParitcleCount; i++)
                    emitter.SpawnParticle();
                emitted = true;
            }
        }
    }
}
