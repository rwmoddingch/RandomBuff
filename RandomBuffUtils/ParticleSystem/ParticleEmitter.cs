using RandomBuffUtils.ParticleSystem.EmitterModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuffUtils.ParticleSystem
{
    public class ParticleEmitter
    {
        public ParticleSystem system;

        public Action<Particle> OnParticleDieEvent;
        public Action<Particle> OnParticleInitEvent;
        

        public List<Particle> Particles { get; } = new List<Particle>();

        public SpawnModule SpawnModule { get; private set; }
        public List<EmitterModule> EmitterModules { get; private set; } = new List<EmitterModule>();
        public List<ParticleInitModule> PInitModules { get; private set; } = new List<ParticleInitModule>();
        public List<ParticleUpdateModule> PUpdateModules { get; private set; } = new List<ParticleUpdateModule>();


        public Room room;
        public Vector2 pos;
        public Vector2 lastPos;


        public ParticleEmitter(Room room)
        {
            this.room = room;
            ParticleSystem.ApplyEmitterAndInit(this);
        }

        public virtual void Init()
        {
            foreach(var module in EmitterModules)
                module.Init();
        }

        public virtual void Die()
        {
            ClearSprites();
            system.managedEmitter.Remove(this);
        }

        #region Draw&Update
        public void Update(bool eu)
        {
            lastPos = pos;

            foreach (var module in EmitterModules)
                module.Update();
            foreach (var module in PInitModules)
                module.Update();
            foreach(var module in PUpdateModules)   
                module.Update();

            for(int i = Particles.Count - 1; i >= 0; i--)
            {
                foreach (var pModule in PUpdateModules)
                    pModule.UpdateApply(Particles[i]);
                Particles[i].Update();
            }
        }

        public void InitSpritesAndAddToContainer()
        {
            for (int i = Particles.Count - 1; i >= 0; i--)
            {
                Particles[i].InitSpritesAndAddToContainer();
            }
        }

        public void DrawSprites(RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            for (int i = Particles.Count - 1; i >= 0; i--)
            {
                Particles[i].DrawSprites(rCam, timeStacker, camPos);
            }
        }

        public void ClearSprites()
        {
            for (int i = Particles.Count - 1; i >= 0; i--)
            {
                Particles[i].ClearSprites();
            }

        }
        #endregion

        public void ApplyParticleSpawn(SpawnModule spawnModule)
        {
            SpawnModule = spawnModule;
            ApplyEmitterModule(spawnModule);
        }

        public void ApplyEmitterModule(EmitterModule emitterModule)
        {
            EmitterModules.Add(emitterModule);
        }

        public void ApplyParticleInit(ParticleInitModule particleInitModule)
        {
            PInitModules.Add(particleInitModule);
        }

        public void ApplyParticleUpdate(ParticleUpdateModule particleUpdateModule)
        {
            PUpdateModules.Add(particleUpdateModule);
        }


        public void SpawnParticle()
        {
            var result = ParticlePool.GetParticle(this);
            Particles.Add(result);

            foreach(var module in PInitModules)
            {
                module.Apply(result);
            }
            
            OnParticleInitEvent?.Invoke(result);
            //BuffUtils.Log("ParticleEmitter", $"spawn particle");
            if (system.IsOnStage)
            {
                //BuffUtils.Log("ParticleEmitter", $"Successful init particle");
                result.InitSpritesAndAddToContainer();
            }
        }
    }
}
