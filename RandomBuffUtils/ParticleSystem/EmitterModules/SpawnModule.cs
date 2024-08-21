using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Random = UnityEngine.Random;
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

    public class RateSpawnerModule : SpawnModule
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
    public class RandomRateSpawnerModule : SpawnModule
    {
        static float secPerFrame = 1 / 40f;
        private float secNeed;
        private float counter;
        private float minRtePerSec;
        private float maxRtePerSec;
        public RandomRateSpawnerModule(ParticleEmitter emitter, int maxParticleCount, float minRtePerSec, float maxRtePerSec) : base(emitter, maxParticleCount)
        {
            secNeed = 1f / Random.Range(minRtePerSec, maxRtePerSec);
            this.maxRtePerSec = maxRtePerSec;
            this.minRtePerSec = minRtePerSec;
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
            while (counter > secNeed)
            {
                counter -= secNeed;
                secNeed = 1f / Random.Range(minRtePerSec, maxRtePerSec);
                if (emitter.Particles.Count < maxParitcleCount)
                {
                    emitter.SpawnParticle();
                }
            }
        }
    }
    public sealed class BurstSpawnerModule : SpawnModule
    {
        public bool emitted;
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
