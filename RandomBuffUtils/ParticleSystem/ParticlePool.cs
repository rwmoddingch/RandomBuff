using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuffUtils.ParticleSystem
{
    internal static class ParticlePool
    {
        public static readonly Queue<Particle> NoHostPool = new ();

        public static readonly Dictionary<ParticleEmitter, Queue<Particle>> PoolDictionary = new();

        public static Particle GetParticle(this ParticleEmitter emitter)
        {
            Particle result;
            if (PoolDictionary[emitter].Count > 0)
                return PoolDictionary[emitter].Dequeue();
            else if(NoHostPool.Count > 0)
                return NoHostPool.Dequeue();
            else
                result = new Particle();
            return result;
        }

        public static void RecycleParticle(this ParticleEmitter emitter, Particle particle)
        {
            PoolDictionary[emitter].Enqueue(particle);
        }

        public static void NewPool(this ParticleEmitter emitter)
        {
            PoolDictionary.Add(emitter,new Queue<Particle>());
        }

        public static void RecyclePool(this ParticleEmitter emitter)
        {
            var pool = PoolDictionary[emitter];
            while (pool.Count > 0)
            {
                pool.Peek().SetDirty();
                NoHostPool.Enqueue(pool.Dequeue());
            }

            PoolDictionary.Remove(emitter);
        }
    }
}
