using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            if (instance == null)
                FindObjectOfType<Camera>().gameObject.AddComponent<BuffPostEffectManager>();
            
        }


        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            var tmp = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
            var a = source;
            var b = tmp;
            foreach (var item in allPostLayer.Values)
            {
                foreach (var effect in item)
                {
                    try
                    {
                        effect.OnRenderImage(a, b);
                        Swap();

                    }
                    catch (Exception e)
                    {
                        BuffUtils.LogException("BuffPostEffect",e);
                        BuffUtils.LogError("BuffPostEffect",$"Exception in calling {effect.GetType().Name}.OnRenderImage");
                    }
                }
                item.RemoveAll(i => i.needDeletion);

            }

            Graphics.Blit(a, destination);
            RenderTexture.ReleaseTemporary(tmp);
            void Swap()
            {
                RenderTexture c;
                c = a; a = b; b = c;
            }
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


        public virtual void Destroy()
        {
            needDeletion = true;
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

        public override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            lifeTime -= Time.deltaTime / duringTime;
            if(lifeTime <= 0) 
                Destroy();
        }
    }
}
