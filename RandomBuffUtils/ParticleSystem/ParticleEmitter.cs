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
        public int particleID;
        public ParticleSystem system;

        public Action<Particle> OnParticleDieEvent;
        public Action<Particle> OnParticleInitEvent;

        public Action<ParticleEmitter> OnEmitterDie; 

        public List<Particle> Particles { get; } = new List<Particle>();

        public SpawnModule SpawnModule { get; private set; }
        public List<EmitterModule> EmitterModules { get; private set; } = new List<EmitterModule>();

        public List<IParticleInitModule> PInitModules { get; private set; } = new List<IParticleInitModule>();
        public List<IParticleInitSpritesAndAddToContainerModules> PInitSpritesAndAddToContainerModules { get; private set; } = new List<IParticleInitSpritesAndAddToContainerModules>();
        public List<IParticleUpdateModule> PUpdateModules { get; private set; } = new List<IParticleUpdateModule>();
        public List<IParticleDrawModules> PDrawModules { get; private set; } = new List<IParticleDrawModules>();
        public List<IParticleClearSpritesModule> PClearSpritesModules { get; private set; } = new List<IParticleClearSpritesModule>();
        public List<IParticleDieModules> PDieModules { get; private set; } = new List<IParticleDieModules>();

        public List<IOwnParticleUniqueData> PUniqueDatas { get; private set; } = new List<IOwnParticleUniqueData>();

        public Room room;
        public Vector2 pos;
        public Vector2 lastPos;
        public Vector2 vel;
        public bool slateForDeletion;

        public ParticleEmitter(Room room)
        {
            this.room = room;
        }

        public virtual void Init()
        {
            if(PDrawModules.Count == 0)
            {
                int count = PInitModules.FindAll((m) => m is AddElement).Count;
                int[] lst = new int[count];
                for(int i = 0; i < count; i++)
                    lst[i] = i;

                ApplyParticleModule(new DefaultDrawer(this, lst));
            }
            foreach(var module in EmitterModules)
                module.Init();
            this.NewPool();
        }

        void ActualDie()
        {
            room = null;
            //BuffUtils.Log("ParticleEmitter", "ActualDie");
            Particles.Clear();
            this.RecyclePool();
            system.managedEmitter.Remove(this);
        }

        public virtual void Die()
        {
            //BuffUtils.Log("ParticleEmitter", "Die");
            slateForDeletion = true;
            OnEmitterDie?.Invoke(this);
            foreach (var module in EmitterModules)
                module.OnDie();
        }

        #region Draw&Update
        public void Update(bool eu)
        {
            lastPos = pos;
            for (int i = Particles.Count - 1; i >= 0; i--)
            {
                Particles[i].Update();
            }

            if (slateForDeletion)
            {
                if (Particles.Count == 0)
                    ActualDie();
                return;
            }

            foreach (var module in EmitterModules)
            {
                module.Update();
                if (slateForDeletion)
                    return;
            }
            //foreach (var module in PInitModules)
            //    module.Update();
            //foreach(var module in PUpdateModules)   
            //    module.Update();
        }

        public void InitSpritesAndAddToContainer()
        {
            if (slateForDeletion)
                return;

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

   

        public void ApplyParticleModule(EmitterModule particleModule)
        {
            EmitterModules.Add(particleModule);

            if(particleModule is IParticleInitModule pInit)
                PInitModules.Add(pInit);
            if (particleModule is IParticleInitSpritesAndAddToContainerModules pInit2)
                PInitSpritesAndAddToContainerModules.Add(pInit2);
            if(particleModule is IParticleUpdateModule pUpdateModule)
                PUpdateModules.Add(pUpdateModule);
            if (particleModule is IParticleDrawModules pDraw)
                PDrawModules.Add(pDraw);
            if( particleModule is IParticleClearSpritesModule pClear)
                PClearSpritesModules.Add(pClear);
            if(particleModule is IParticleDieModules pDie)
                PDieModules.Add(pDie);
            if(particleModule is IOwnParticleUniqueData pUnique)
                PUniqueDatas.Add(pUnique);
        }


        public void SpawnParticle()
        {
            var result = this.GetParticle();
            Particles.Add(result);
            result.Init(this, particleID);
            
            particleID++;
            
            OnParticleInitEvent?.Invoke(result);
            if (system.IsOnStage)
                result.InitSpritesAndAddToContainer();
            
        }
    }
}
