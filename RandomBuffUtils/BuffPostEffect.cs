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


        /// <summary>
        /// 移除后期处理效果
        /// </summary>
        /// <param name="effect"></param>
        public static void RemoveEffect(BuffPostEffect effect)
        {
            if (allPostLayer.TryGetValue(effect.Layer, out var items))
                items.Remove(effect);

        }

        /// <summary>
        /// 添加新的后期处理效果
        /// </summary>
        /// <param name="effect">要添加的效果</param>
        /// <param name="pos">添加顺序位置</param>
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
    
    /// <summary>
    /// 后期处理效果的基类
    /// </summary>
    public abstract class BuffPostEffect
    {
        protected Material material;

        public bool needDeletion;

        protected BuffPostEffect(int layer)
        {
            Layer = layer;
        }

        /// <summary>
        /// 后期处理效果的层数，层数越大，处理越靠后
        /// </summary>
        public int Layer { get; protected set; } = 0;

        /// <summary>
        /// 进行后期处理的函数，
        /// 注意务必至少进行一次source到dest的Blit否则可能会出现未定义行为。
        /// </summary>
        /// <param name="source">原始贴图</param>
        /// <param name="destination">目标贴图</param>
        public abstract void OnRenderImage(RenderTexture source, RenderTexture destination);


        /// <summary>
        /// Unity的update，注意时间间隔为Time.deltatime
        /// </summary>
        public virtual void Update(){}

        /// <summary>
        /// 删除函数，
        /// 务必要在不使用的时候删除
        /// </summary>
        public virtual void Destroy()
        {
            needDeletion = true;
            BuffUtils.Log("BuffPostEffect",$"Destroy {GetType().Name}");
        }
    }

    /// <summary>
    /// 限定时间的后期处理基类，会在持续时间结束后自动删除。
    /// 注意若持续时间为负数则会永久存在。
    /// </summary>
    public abstract class BuffPostEffectLimitTime : BuffPostEffect
    {
        protected readonly float duringTime;
        protected float lifeTime = 1;
        protected readonly float enterTime;
        protected readonly float fadeTime;

        public bool IgnorePaused { get; set; }
        public bool IgnoreGameSpeed { get; set; }


        protected BuffPostEffectLimitTime(int layer,float duringTime, float enterTime, float fadeTime) : base(layer) 
        {
            this.duringTime = duringTime;
            this.enterTime = enterTime / duringTime;
            this.fadeTime = fadeTime / duringTime;
        }

        /// <summary>
        /// 过渡的alpha值
        /// </summary>
        protected virtual float LerpAlpha => Mathf.InverseLerp(0, enterTime, 1 - lifeTime) * Mathf.InverseLerp(0, fadeTime, lifeTime);

        public override void Update()
        {
            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game &&
                game.GamePaused && !IgnorePaused)
            {
            }
            else
                lifeTime -= Time.deltaTime * (IgnoreGameSpeed ? 1 : BuffCustom.TimeSpeed) / duringTime;

            if (lifeTime <= 0)
                Destroy();
        }

        public override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
        }
    }
}
