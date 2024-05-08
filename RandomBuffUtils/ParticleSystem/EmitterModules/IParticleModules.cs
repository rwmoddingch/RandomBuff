using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuffUtils.ParticleSystem.EmitterModules
{
    public interface IParticleInitModule
    {
        void ApplyInit(Particle particle);
    }

    public interface IParticleInitSpritesAndAddToContainerModules
    {
        void ApplyInitSpritesAndAddToContainer(Particle particle);
    }
    public interface IParticleUpdateModule
    {
        void ApplyUpdate(Particle particle);
    }

    public interface IParticleDrawModules
    {
        void ApplyDrawSprites(Particle particle, RoomCamera rCam, float timeStacker, Vector2 camPos);
    }

    public interface IParticleClearSpritesModule
    {
        void ApplyClearSprites(Particle particle);
    }

    public interface IParticleDieModules
    {
        void ApplyDie(Particle particle);
    }

    public interface IOwnParticleUniqueData
    {
        Particle.ParticleUniqueData GetUniqueData(Particle particle);
    }
}
