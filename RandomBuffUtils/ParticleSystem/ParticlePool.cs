using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuffUtils.ParticleSystem
{
    internal static class ParticlePool
    {
        public static readonly Queue<Particle> pool = new Queue<Particle>();

        public static Particle GetParticle(ParticleEmitter emitter)
        {
            Particle result;
            if (pool.Count > 0)
                result = pool.Dequeue();
            else
                result = new Particle();
            return result;
        }

        public static void RecycleParticle(Particle particle)
        {
            pool.Enqueue(particle);
        }
    }
}
