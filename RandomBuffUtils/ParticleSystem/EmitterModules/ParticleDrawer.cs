using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuffUtils.ParticleSystem.EmitterModules
{
    public class DefaultDrawer : EmitterModule, IParticleInitSpritesAndAddToContainerModules, IParticleDrawModules, IParticleClearSpritesModule
    {
        int[] renderElementIndexs;

        public DefaultDrawer(ParticleEmitter emitter, int[] renderElementIndexs) : base(emitter)
        {
            this.renderElementIndexs = renderElementIndexs;
        }
        public void ApplyInitSpritesAndAddToContainer(Particle particle)
        {
            for(int i = 0;i < renderElementIndexs.Length;i++)
            {
                int index = renderElementIndexs[i];
                var param = particle.spriteInitParams[index];

                particle.fNodes[index] = new FSprite(param.element);
                if(!string.IsNullOrEmpty(param.shader))
                    (particle.fNodes[index] as FSprite).shader = Custom.rainWorld.Shaders[param.shader];
                particle.emitter.system.Containers[param.layer].AddChild(particle.fNodes[index]);
            }
        }

        public void ApplyDrawSprites(Particle particle, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 smoothPos = Vector2.Lerp(particle.lastPos, particle.pos, timeStacker);
            Vector2 smoothScaleXY = Vector2.Lerp(particle.lastScaleXY, particle.scaleXY, timeStacker);
            Color smoothColor = Color.Lerp(particle.lastColor, particle.color, timeStacker);
            float smoothRotation = Mathf.Lerp(particle.lastRotation, particle.rotation, timeStacker);

            for (int i = 0; i < renderElementIndexs.Length; i++)
            {
                int index = renderElementIndexs[i];
                particle.fNodes[index].SetPosition(smoothPos - camPos);
                particle.fNodes[index].scaleX = smoothScaleXY.x * particle.spriteInitParams[index].scale;
                particle.fNodes[index].scaleY = smoothScaleXY.y * particle.spriteInitParams[index].scale;
                (particle.fNodes[index] as FSprite).color = smoothColor;
                particle.fNodes[index].alpha = particle.spriteInitParams[index].alpha;
                particle.fNodes[index].rotation = smoothRotation;
            }
        }

        public void ApplyClearSprites(Particle particle)
        {
            for (int i = 0; i < renderElementIndexs.Length; i++)
            {
                int index = renderElementIndexs[i];
                particle.fNodes[index].RemoveFromContainer();
            }
        }
    }

    public class TrailDrawer : EmitterModule, IParticleUpdateModule, IParticleInitSpritesAndAddToContainerModules, IParticleDrawModules, IParticleClearSpritesModule, IOwnParticleUniqueData
    {
        public Func<Particle, int, int, Color> gradient;
        public Func<Particle, float, Color> colorOverLife;
        public Func<Particle, float, float> alphaModifyOverLife;
        public Func<Particle, int, int, float> alpha;
        public Func<Particle, int, int, float> width;
        public Func<Particle, float, float> widthModifyOverLife;

        int trailCount;
        int index;

        public TrailDrawer(ParticleEmitter emitter, int index, int trailCount = 10) : base(emitter)
        {
            this.index = index;
            this.trailCount = trailCount;
        }

        public void ApplyUpdate(Particle particle)
        {
            var data = particle.GetUniqueData<TrailData>(this);
            
            if (data == null)
                return;

            data.positionsList.Insert(0, particle.pos);
            if (data.positionsList.Count > trailCount)
            {
                data.positionsList.RemoveAt(trailCount);
            }
            data.colorsList.Insert(0, GetFirstColor(particle));
            if (data.colorsList.Count > trailCount)
            {
                data.colorsList.RemoveAt(trailCount);
            }
        }

        public void ApplyInitSpritesAndAddToContainer(Particle particle)
        {
            particle.fNodes[index] = TriangleMesh.MakeLongMesh(trailCount - 1, false, true, particle.spriteInitParams[index].element);
            if (!string.IsNullOrEmpty(particle.spriteInitParams[index].shader))
                (particle.fNodes[index] as TriangleMesh).shader = Custom.rainWorld.Shaders[particle.spriteInitParams[index].shader];

            particle.emitter.system.Containers[particle.spriteInitParams[index].layer].AddChild(particle.fNodes[index]);
        }

        public void ApplyDrawSprites(Particle particle, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            var data = particle.GetUniqueData<TrailData>(this);
            var triangleMesh = particle.fNodes[index] as TriangleMesh;
            if (data == null)
                return;

            Vector2 vector = Vector2.Lerp(particle.lastPos, particle.pos, timeStacker);
            //float size = 2f * this.owner.eyeSize;
            for (int i = 0; i < this.trailCount - 1; i++)
            {
                float width = this.width.Invoke(particle, i, trailCount);
                if (widthModifyOverLife != null)
                    width *= widthModifyOverLife.Invoke(particle, particle.LifeParam);
                Vector2 smoothPos = this.GetSmoothPos(data, i, timeStacker);
                Vector2 smoothPos2 = this.GetSmoothPos(data, i + 1, timeStacker);
                Vector2 vector2 = (vector - smoothPos).normalized;
                Vector2 vector3 = Custom.PerpendicularVector(vector2);
                vector2 *= Vector2.Distance(vector, smoothPos2) / 5f;
                triangleMesh.MoveVertice(i * 4, vector - vector3 * width - vector2 - camPos);
                triangleMesh.MoveVertice(i * 4 + 1, vector + vector3 * width - vector2 - camPos);
                triangleMesh.MoveVertice(i * 4 + 2, smoothPos - vector3 * width + vector2 - camPos);
                triangleMesh.MoveVertice(i * 4 + 3, smoothPos + vector3 * width + vector2 - camPos);

                //for (int j = 0; j < 4; j++)
                //    triangleMesh.verticeColors[i * 4 + j] = col; 

                vector = smoothPos;
            }

            for (int i = 0; i < triangleMesh.verticeColors.Length; i++)
            {
                Color col = gradient.Invoke(particle, i, triangleMesh.verticeColors.Length);
                if (alpha != null)
                    col.a *= alpha.Invoke(particle, i, triangleMesh.verticeColors.Length);
                if (alphaModifyOverLife != null)
                    col.a *= alphaModifyOverLife.Invoke(particle, particle.LifeParam);
                triangleMesh.verticeColors[i] = col;
            }
        }

        public void ApplyClearSprites(Particle particle)
        {
            particle.fNodes[index].RemoveFromContainer();
        }

        Vector2 GetSmoothPos(TrailData data, int i, float timeStacker)
        {
            return Vector2.Lerp(GetPos(data, i + 1), GetPos(data, i), timeStacker);
        }

        Vector2 GetPos(TrailData data, int i)
        {
            return data.positionsList[Custom.IntClamp(i, 0, data.positionsList.Count - 1)];
        }

        private Color GetCol(TrailData data, int i)
        {
            return data.colorsList[Custom.IntClamp(i, 0, data.colorsList.Count - 1)];
        }

        Color GetFirstColor(Particle particle)
        {
            Color result;

            if (gradient != null)
                result = gradient.Invoke(particle, 0, trailCount);
            else
                result = colorOverLife.Invoke(particle, particle.LifeParam);
            return result;
        }

        public Particle.ParticleUniqueData GetUniqueData(Particle particle)
        {
            TrailData result = new TrailData();
            result.positionsList = new List<Vector2> { particle.pos };
            result.colorsList = new List<Color>() { GetFirstColor(particle) };

            return result;
        }

        internal class TrailData : Particle.ParticleUniqueData
        {
            public List<Vector2> positionsList;
            public List<Color> colorsList;
        }
    }
}
