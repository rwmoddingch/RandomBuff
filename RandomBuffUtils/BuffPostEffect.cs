using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RWCustom;
using UnityEngine;

namespace RandomBuffUtils
{
    public class BuffPostEffectManager : MonoBehaviour
    {

        public enum InsertPosition
        {
            First,
            Last,
        }

        private static BuffPostEffectManager instance;
        private static Dictionary<int, List<BuffPostEffect>> allPostLayer = new();

        public static void RemoveEffect(BuffPostEffect effect)
        {
            if (allPostLayer.TryGetValue(effect.Layer, out var items))
                items.Remove(effect);

        }


        public static void AddEffect(BuffPostEffect effect, InsertPosition pos = InsertPosition.Last)
        {
            if (!allPostLayer.TryGetValue(effect.Layer, out var items))
                allPostLayer.Add(effect.Layer, items = new List<BuffPostEffect>());
            if(pos ==  InsertPosition.First)
                items.Insert(0,effect);
            else
                items.Add(effect);
            BuffUtils.Log("BuffPostEffect",$"Add new post effect: {effect.GetType().Name}");
            if (instance == null)
                instance = Futile.instance._cameraHolder.AddComponent<BuffPostEffectManager>();
            
        }

        private void Update()
        {
            foreach (var item in allPostLayer.Values)
            {
                foreach (var effect in item)
                {
                    try
                    {
                        effect.Update();

                    }
                    catch (Exception e)
                    {
                        BuffUtils.LogException("BuffPostEffect", e);
                        BuffUtils.LogError("BuffPostEffect", $"Exception in calling {effect.GetType().Name}.Update");
                    }

                }
                item.RemoveAll(i => i.needDeletion);

            }
        }


        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            var tmp = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
            var a = source;
            var b = tmp;
      
            foreach (var item in allPostLayer.OrderBy(i => i.Key).Select(i => i.Value))
            {
                foreach (var effect in item)
                {
                    try
                    {
                        effect.OnRenderImage(a, b);
                        (a, b) = (b, a);

                    }
                    catch (Exception e)
                    {
                        BuffUtils.LogException("BuffPostEffect",e);
                        BuffUtils.LogError("BuffPostEffect",$"Exception in calling {effect.GetType().Name}.OnRenderImage");
                    }
                }

            }

            Graphics.Blit(a, destination);
            RenderTexture.ReleaseTemporary(tmp);
        }


    }

    public abstract class BuffPostEffect
    {
        protected Material material;

        public bool needDeletion;

        protected BuffPostEffect(int layer)
        {
            Layer = layer;
        }

        public int Layer { get; protected set; } = 0;

        public abstract void OnRenderImage(RenderTexture source, RenderTexture destination);

        public virtual void Update(){}

        public virtual void Destroy()
        {
            needDeletion = true;
            BuffUtils.Log("BuffPostEffect",$"Destroy {GetType().Name}");
            Material.Destroy(material);
        }
    }

    public abstract class BuffPostEffectLimitTime : BuffPostEffect
    {
        protected readonly float duringTime;
        protected float lifeTime = 1;
        protected readonly float enterTime;
        protected readonly float fadeTime;

        protected BuffPostEffectLimitTime(int layer,float duringTime, float enterTime, float fadeTime) : base(layer) 
        {
            this.duringTime = duringTime;
            this.enterTime = enterTime / duringTime;
            this.fadeTime = fadeTime / duringTime;
        }

        protected virtual float LerpAlpha => Mathf.InverseLerp(0, enterTime, 1 - lifeTime) * Mathf.InverseLerp(0, fadeTime, lifeTime);

        public override void Update()
        {
            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game &&
                game.pauseMenu != null)
            {
            }
            else
                lifeTime -= Time.deltaTime * (Custom.rainWorld.processManager.currentMainLoop?.framesPerSecond ?? 40) / 40f / duringTime;
            
  
            if (lifeTime <= 0)
                Destroy();
        }

        public override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
        }
    }
}
