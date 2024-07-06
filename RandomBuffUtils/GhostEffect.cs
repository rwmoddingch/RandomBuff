using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace RandomBuffUtils
{
    public class GhostEffect : CosmeticSprite
    {

       string overrideLowLayerShader = null;

        Vector2 center;
        Vector2 lastCenter;
        Vector2[][] deltas;
        float[][] origAlphas;
        GraphicsModule graphicsModule;
        //float sizeMulti = 1f;

        int life;
        int currentLife;

        float alpha;
        float currentAlpha;
        float lastAlpha;

        Vector2 startVel;
        Vector2 currentVel;

        Vector2? posDelta;




        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="graphicsModule">目标对象，会拷贝其中所有的贴图</param>
        /// <param name="life">生命周期</param>
        /// <param name="alpha">初始透明度</param>
        /// <param name="velMulti">速度乘数，如果你不希望残影移动，那么请给0</param>
        public GhostEffect(GraphicsModule graphicsModule, int life, float alpha, float velMulti, Vector2? posDelta = null,string overrideLowLayerShader = null)
        {
            if (graphicsModule?.owner?.firstChunk == null)
            {
                Destroy();
                return;
            }
            this.graphicsModule = graphicsModule;

            startVel = graphicsModule.owner.firstChunk.vel * velMulti;
            this.alpha = alpha;
            this.life = life;
            this.posDelta = posDelta;
            this.overrideLowLayerShader = overrideLowLayerShader;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);

            foreach (var sleaser in rCam.spriteLeasers)
            {
                if (sleaser.drawableObject != graphicsModule)
                    continue;
                sLeaser.sprites = ProcessSprites(sleaser.sprites, rCam);
                break;
            }
         
            AddToContainer(sLeaser,rCam,null);
        }

        FSprite[] ProcessSprites(FSprite[] origs, RoomCamera roomCamera)
        {
            Vector2 screenCenterPos = Vector2.zero;

            int skipped = 0;
            foreach (var sprite in origs)
            {
                if (sprite.alpha == 0 || !sprite.isVisible)
                {
                    skipped++;
                    continue;
                }
                screenCenterPos += GetSpriteCenter(sprite);
            }
            screenCenterPos /= origs.Length - skipped;

            var sprites = new FSprite[origs.Length];
            deltas = new Vector2[origs.Length][];
            origAlphas = new float[origs.Length][];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = CopySprite(origs[i], overrideLowLayerShader, roomCamera);
                
                deltas[i] = GetDeltas(sprites[i], screenCenterPos);
                origAlphas[i] = GetAlphas(sprites[i]);
            }
            center = screenCenterPos;
            BuffUtils.Log("GhostEffect", $"center : {center}, pos : {graphicsModule.owner.firstChunk.pos}");

            return sprites;
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            base.AddToContainer(sLeaser, rCam, newContatiner);
            if (newContatiner == null)
                newContatiner = rCam.ReturnFContainer("Water");
            foreach (var sprite in sLeaser.sprites)
            {
                newContatiner.AddChild(sprite);
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (currentLife < life)
                currentLife++;
            else
            {
                Destroy();
                return;
            }
            if (slatedForDeletetion)
                return;

            //for (int i = 0; i < deltas.Length; i++)
            //{
            //    for (int j = 0; j < deltas[i].Length; j++)
            //    {
            //        deltas[i][j] *= sizeMulti;
            //    }
            //}

            float t = currentLife / (float)life;

            lastAlpha = currentAlpha;
            currentAlpha = (1f - t) * alpha;

            currentVel = startVel * (1f - t);

            //sizeMulti = 1f - t;

            lastCenter = center;
            center += currentVel;
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

            Vector2 smoothCenterPos = Vector2.Lerp(lastCenter, center, timeStacker);
            float smoothAlpha = Mathf.Lerp(lastAlpha, currentAlpha, timeStacker);

            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                ApplyDeltas(sLeaser.sprites[i], deltas[i], smoothCenterPos, camPos + posDelta ?? Vector2.zero);
                ApplyFade(sLeaser.sprites[i], origAlphas[i], smoothAlpha);
            }
        }

        public static Vector2 GetSpriteCenter(FSprite sprite)
        {
            Vector2 pos = Vector2.zero;
            if (sprite is CustomFSprite customFSprite)
            {
                for (int i = 0; i < 4; i++)
                {
                    pos += customFSprite.vertices[i];
                }
                pos /= 4;
            }
            else if (sprite is TriangleMesh triangleMesh)
            {
                for (int i = 0; i < triangleMesh.vertices.Length; i++)
                {
                    pos += triangleMesh.vertices[i];
                }
                pos /= triangleMesh.vertices.Length;
            }
            else
                pos = sprite.GetPosition();
            return pos;
        }

        public static FSprite CopySprite(FSprite orig, string lowLayerShader, RoomCamera rCam)
        {
            FSprite result;
            if (orig is CustomFSprite customFSprite)
            {
                CustomFSprite temp = new CustomFSprite(customFSprite.element.name);
                for (int i = 0; i < 4; i++)
                {
                    temp.vertices[i] = customFSprite.vertices[i];
                    temp.verticeColors[i] = customFSprite.verticeColors[i];
                }
                result = temp;
            }
            else if (orig is TriangleMesh triangleMesh)
            {
                TriangleMesh temp = new TriangleMesh(triangleMesh.element.name, triangleMesh.triangles, triangleMesh.customColor, true);
                for (int i = 0; i < triangleMesh.vertices.Length; i++)
                {
                    temp.vertices[i] = triangleMesh.vertices[i];
                    if (triangleMesh.customColor)
                        temp.verticeColors[i] = triangleMesh.verticeColors[i];
                    else
                        temp.color = triangleMesh.color;
                    temp.UVvertices[i] = triangleMesh.UVvertices[i];
                }
                result = temp;
            }
            else
            {
                result = new FSprite(orig.element.name, orig._facetTypeQuad);
                result.SetAnchor(orig.GetAnchor());
                result.SetPosition(orig.GetPosition());
                result.color = orig.color;
            }
            result.shader = orig.shader;
            result.rotation = orig.rotation;
            result.alpha = orig.alpha;
            result.scale = orig.scale;
            result.scaleX = orig.scaleX;
            result.scaleY = orig.scaleY;
            result.isVisible = orig.isVisible;
            if (lowLayerShader != null && rCam.SpriteLayers.IndexOf(orig.container) < rCam.SpriteLayerIndex["Foreground"])
                result.shader = rCam.game.rainWorld.Shaders[lowLayerShader];
            return result;
        }

        public static Vector2[] GetDeltas(FSprite sprite, Vector2 center)
        {
            Vector2[] result;
            if (sprite is CustomFSprite customFSprite)
            {
                result = new Vector2[4];
                for (int i = 0; i < 4; i++)
                {
                    result[i] = customFSprite.vertices[i] - center;
                }
            }
            else if (sprite is TriangleMesh triangleMesh)
            {
                result = new Vector2[triangleMesh.vertices.Length];
                for (int i = 0; i < triangleMesh.vertices.Length; i++)
                {
                    result[i] = triangleMesh.vertices[i] - center;
                }
            }
            else
            {
                result = new Vector2[1];
                result[0] = sprite.GetPosition() - center;
            }
            return result;
        }
        public static float[] GetAlphas(FSprite sprite)
        {
            float[] result;
            if (sprite is CustomFSprite customFSprite)
            {
                result = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    result[i] = customFSprite.verticeColors[i].a;
                }
            }
            else if (sprite is TriangleMesh triangleMesh && triangleMesh.customColor)
            {
                result = new float[triangleMesh.verticeColors.Length];
                for (int i = 0; i < triangleMesh.verticeColors.Length; i++)
                {
                    result[i] = triangleMesh.verticeColors[i].a;
                }
            }
            else
            {
                result = new float[1];
                result[0] = sprite.alpha;
            }
            return result;
        }

        public static void ApplyDeltas(FSprite sprite, Vector2[] deltas, Vector2 centerPos, Vector2 camPos)
        {
            if (sprite is CustomFSprite customFSprite)
            {
                for (int i = 0; i < 4; i++)
                {
                    customFSprite.vertices[i] = centerPos + deltas[i] - camPos;
                }
            }
            else if (sprite is TriangleMesh triangleMesh)
            {
                for (int i = 0; i < triangleMesh.vertices.Length; i++)
                {
                    triangleMesh.vertices[i] = centerPos + deltas[i] - camPos;
                }
            }
            else
            {
                sprite.SetPosition(centerPos + deltas[0] - camPos);
            }
        }

        public static void ApplyFade(FSprite sprite, float[] origAlpha, float alpha)
        {
            if (sprite is CustomFSprite customFSprite)
            {
                for (int i = 0; i < 4; i++)
                {
                    customFSprite.verticeColors[i].a = alpha * origAlpha[i];
                }
            }
            else if (sprite is TriangleMesh triangleMesh && triangleMesh.customColor)
            {
                for (int i = 0; i < triangleMesh.vertices.Length; i++)
                {
                    triangleMesh.verticeColors[i].a = alpha * origAlpha[i];
                }
            }
            else
            {
                sprite.alpha = alpha * origAlpha[0];
            }
        }
    }

    public abstract class GhostEmitter : UpdatableAndDeletable
    {
        protected WeakReference<GraphicsModule> graphicModuleRef;

        int ghostLife;
        float ghostAlpha;
        float ghostVelMulti;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graphicsModule">目标对象，会拷贝其中所有的贴图</param>
        /// <param name="room"></param>
        /// <param name="ghostLife">残影的生命周期</param>
        /// <param name="ghostAlpha">初始透明度</param>
        /// <param name="ghostVelMulti">速度乘数，如果你不希望残影移动，那么请给0</param>
        public GhostEmitter(GraphicsModule graphicsModule, Room room, int ghostLife = 40, float ghostAlpha = 0.3f, float ghostVelMulti = 0f)
        {
            graphicModuleRef = new WeakReference<GraphicsModule>(graphicsModule);
            this.room = room;

            this.ghostLife = ghostLife;
            this.ghostAlpha = ghostAlpha;
            this.ghostVelMulti = ghostVelMulti;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (!graphicModuleRef.TryGetTarget(out var graphicsModule))
            {
                Destroy();
            }
            else
            {
                if (graphicsModule.owner == null)
                    Destroy();
                if (graphicsModule.owner.room == null || graphicsModule.owner.room != room)
                    Destroy();
            }
        }

        public virtual void Emit(GraphicsModule graphicsModule, Vector2? posDelta = null)
        {
            room.AddObject(new GhostEffect(graphicsModule, ghostLife, ghostAlpha, ghostVelMulti, posDelta));
        }
    }

    public class GhostPeriodicEmitter : GhostEmitter
    {
        int currentLife;
        int maxLife;

        int emitCounter;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graphicsModule"></param>
        /// <param name="room"></param>
        /// <param name="emitterLife">发射器的声明周期，当赋值小于0时表示无限周期，仅当目标房间不同的时候才会销毁</param>
        /// <param name="emitCounter">每个残影间隔多少帧产生</param>
        public GhostPeriodicEmitter(GraphicsModule graphicsModule, Room room, int emitterLife = -1, int emitCounter = 10, int ghostLife = 40, float ghostAlpha = 0.3f, float ghostVelMulti = 0f) : base(graphicsModule, room, ghostLife, ghostAlpha, ghostVelMulti)
        {
            maxLife = emitterLife;
            this.emitCounter = emitCounter;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (slatedForDeletetion)
                return;

            currentLife++;
            if (currentLife >= maxLife && maxLife > 0)
                Destroy();

            if (graphicModuleRef.TryGetTarget(out var graphicModule))
            {
                if (currentLife % emitCounter == 0)
                    Emit(graphicModule);
            }
        }
    }

    public class GhostDisplacementEmitter : GhostEmitter
    {
        float displacementStacker;

        public GhostDisplacementEmitter(GraphicsModule graphicsModule, Room room, Vector2 startPos, Vector2 endPos, float displacement = 20f, int ghostLife = 40, float ghostAlpha = 0.3f, float ghostVelMulti = 0f) : base(graphicsModule, room, ghostLife, ghostAlpha, ghostVelMulti)
        {
            Vector2 delta = endPos - startPos;
            Vector2 deltaDir = delta.normalized;
            displacementStacker = delta.magnitude;

            while (displacementStacker > displacement)
            {
                displacementStacker -= displacement;
                Emit(graphicsModule, -deltaDir * displacementStacker);
            }
            Destroy();
        }
    }
}
