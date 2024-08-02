using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuffUtils.ParticleSystem
{
    public class ParticleSystem : UpdatableAndDeletable, IDrawable
    {
        internal List<ParticleEmitter> managedEmitter = new List<ParticleEmitter>();


        public FContainer[] Containers { get; private set; }

        bool lastInContainer;
        public bool IsOnStage => Containers != null && Containers[0]._isOnStage;

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if(Containers == null)
            {
                Containers = new FContainer[rCam.SpriteLayers.Length];
                sLeaser.containers = new FContainer[rCam.SpriteLayers.Length];
                for (int i = 0; i < sLeaser.containers.Length; i++)
                    Containers[i] = new FContainer();
            }

            sLeaser.sprites = Array.Empty<FSprite>();
            sLeaser.containers = new FContainer[rCam.SpriteLayers.Length];
            for (int i = 0; i < sLeaser.containers.Length; i++)
                sLeaser.containers[i] = Containers[i];

            AddToContainer(sLeaser, rCam, null);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            for (int i = 0; i < sLeaser.containers.Length; i++)
            {
                rCam.SpriteLayers[i].AddChild(sLeaser.containers[i]);
            }

            for (int i = managedEmitter.Count - 1; i >= 0; i--)
                managedEmitter[i].InitSpritesAndAddToContainer();
        }
        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            for (int i = managedEmitter.Count - 1; i >= 0; i--)
                managedEmitter[i].DrawSprites(rCam, timeStacker, camPos);

            if (slatedForDeletetion || room != rCam.room)
                sLeaser.CleanSpritesAndRemove();
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            
            if(lastInContainer != IsOnStage)
            {
                if (IsOnStage)
                {
                    for (int i = managedEmitter.Count - 1; i >= 0; i--)
                        managedEmitter[i].InitSpritesAndAddToContainer();
                }
                else
                {
                    for (int i = managedEmitter.Count - 1; i >= 0; i--)
                        managedEmitter[i].ClearSprites();
                }
                lastInContainer = IsOnStage;
            }

            for (int i = managedEmitter.Count - 1; i >= 0; i--)
                managedEmitter[i].Update(eu);
        }


        public static void ApplyEmitterAndInit(ParticleEmitter emitter)
        {
            var room = emitter.room;
            var system = room.updateList.Find((obj) => obj is ParticleSystem) as ParticleSystem;
            if (system == null)
            {
                system = new ParticleSystem();
                room.AddObject(system);
            }

            emitter.system = system;
            system.managedEmitter.Add(emitter);
            emitter.Init();
            if (system.IsOnStage)
            {
                emitter.InitSpritesAndAddToContainer();
            }
        }
    }
}
