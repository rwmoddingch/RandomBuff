using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff
{
    internal class AnimMachine
    {
        public static int nextID = 0;
        static List<WeakReference<AnimateComponentBase>> componentRefs = new List<WeakReference<AnimateComponentBase>>();
        public static float timeStacker { get; private set; }

        public static void Init()
        {
            On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;
            On.ProcessManager.Update += ProcessManager_Update;
        }

        private static void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            orig.Invoke(self, ID);
            timeStacker = 0f;
        }

        private static void ProcessManager_Update(On.ProcessManager.orig_Update orig, ProcessManager self, float deltaTime)
        {
            orig.Invoke(self, deltaTime);
            if (self.currentMainLoop != null)
                RawUpdate(self.currentMainLoop, deltaTime);
        }

        static void RawUpdate(MainLoopProcess mainLoopProcess,float deltaTime)
        {
            timeStacker += deltaTime * mainLoopProcess.framesPerSecond;
            int num = 0;
            while (timeStacker > 1f)
            {
                Update();
                timeStacker -= 1f;
                num++;
                if (num > 2)
                {
                    timeStacker = 0f;
                }
            }
            GrafUpdate(timeStacker);
        }

        public static void Update()
        {
            for(int i = componentRefs.Count - 1; i >= 0; i--)
            {
                if (componentRefs[i].TryGetTarget(out var component))
                {
                    component.Update();
                }
                else
                {
                    componentRefs.RemoveAt(i);
                    //BuffPlugin.LogDebug($"Remove componenetRef due to no value");
                }
            }
        }

        public static void GrafUpdate(float timeStacker)
        {
            for (int i = componentRefs.Count - 1; i >= 0; i--)
            {
                if (componentRefs[i].TryGetTarget(out var component))
                {
                    component.GrafUpdate(timeStacker);
                }
                else
                {
                    componentRefs.RemoveAt(i);
                    //BuffPlugin.LogDebug($"Remove componenetRef due to no value");
                }
            }
        }

        public static int GetID()
        {
            return nextID++;
        }

        //查找动画组件弱引用
        internal static WeakReference<AnimateComponentBase> GetRef(AnimateComponentBase animateComponentBase)
        {
            foreach(var reference in componentRefs)
            {
                if(reference.TryGetTarget(out var component) && animateComponentBase == component)
                    return reference;
            }
            return null;
        }

        //添加动画组件弱引用
        public static WeakReference<AnimateComponentBase> AddRef(AnimateComponentBase animateComponentBase)
        {
            if (GetRef(animateComponentBase) == null)
            {
                WeakReference<AnimateComponentBase> result;
                componentRefs.Add(result = new WeakReference<AnimateComponentBase>(animateComponentBase));
                //BuffPlugin.Log($"{animateComponentBase} add new weakreference");
                return result;
            }
            else
                BuffPlugin.LogWarning($"WeakReference of {animateComponentBase} already exist in componentRefs");
            return GetRef(animateComponentBase);
        }

        //移除动画组件弱引用
        public static void RemoveRef(AnimateComponentBase animateComponentBase)
        {
            var reference = GetRef(animateComponentBase);
            if(reference != null)
            {
                componentRefs.Remove(reference);
                //BuffPlugin.Log($"AnimMachine remove weakref of {animateComponentBase} successfully");
            }
            else
                BuffPlugin.LogWarning($"WeakReference for {animateComponentBase} not exist in componentRefs");
        }

        public static TickAnimCmpnt GetTickAnimCmpnt(int lowBound, int highBound, int? initial = null, int tick = 1, bool autoStart = true, bool autoDestroy = false)
        {
            return new TickAnimCmpnt(lowBound, highBound, initial, tick, autoStart, autoDestroy);
        }
        public static LerpAnimCmpnt GetLerpAnimCmpnt(float lowBound, float highBound, float? initial = null, float lerpFactor = 0.15f, bool autoStart = true, bool autoDestroy = false)
        {
            return new LerpAnimCmpnt(lowBound, highBound, initial, lerpFactor, autoStart, autoDestroy);
        }

        public static DelayCmpnt GetDelayCmpnt(int ticks, bool autoStart = true, bool autoDestroy = false)
        {
            return new DelayCmpnt(ticks, autoStart, autoDestroy);
        }

        public static T BindActions<T>(T instance, Action<T> OnAnimStart = null, Action<T> OnAnimUpdate = null, Action<T, float> OnAnimGrafUpdate = null, Action<T> OnAnimFinished = null) where T : AnimateComponent<T>
        {
            instance.OnAnimStart += OnAnimStart;
            instance.OnAnimUpdate += OnAnimUpdate;
            instance.OnAnimGrafUpdate += OnAnimGrafUpdate;
            instance.OnAnimFinished += OnAnimFinished;

            return instance;
        }
        public static T BindModifier<T>(T instance, Func<float, float> modifier) where T : AnimateComponent<T>
        {
            instance.modifier = modifier;
            return instance;
        }
    }

    public abstract class AnimateComponentBase
    {
        protected int id;
        public bool inMachine;
        public bool finished;
        public AnimateComponentBase lastCmpnt;
        public AnimateComponentBase nextCmpnt;

        public bool enable { get; protected set; }
        protected bool autoDestroy;

        bool firstHandleInMachine = true;

        public AnimateComponentBase(bool autoStart, bool autoDestroy)
        {
            enable = autoStart;
            id = AnimMachine.GetID();
        }

        public void SetEnable(bool enable)
        {
            this.enable = enable;
            //BuffPlugin.Log($"{this} set enable {enable}");
            if (enable && firstHandleInMachine)
                HandleInMachine();
        }

        //更新调用方法
        public virtual void Update()
        {

        }

        //绘制调用方法
        public virtual void GrafUpdate(float timeStacker)
        {
        }

        //获取0-1之间的动画参数
        public virtual float Get()
        {
            return 0f;
        }

        //重置动画组件
        public virtual void Reset()
        {
            finished = false;
        }

        //启用下一个动画组件,自动模式时自动调用
        public virtual void HandleNext()
        {
            if (!inMachine)
                return;

            var weakRef = AnimMachine.GetRef(this);
            if (weakRef == null)
                return;

            nextCmpnt.HandleInMachine(weakRef);
            nextCmpnt.SetEnable(true);
        }

        //将动画组件添加到动画机器中管理
        public virtual void HandleInMachine(WeakReference<AnimateComponentBase> weakReference = null)
        {
            if(weakReference == null)
            {
                AnimMachine.AddRef(this);
                //BuffPlugin.Log($"{this} handleInMachine with new weakref : enable-{enable}");

            }
            else
            {
                weakReference.SetTarget(this);
                //BuffPlugin.Log($"{this} handleInMachine with exist weakref : enable-{enable}");
            }
            firstHandleInMachine = false;
            inMachine = true;
           
        }

        //手动删除方法，弱使用自动动画请在离开动画对象作用域时调用该方法
        public virtual void Destroy()
        {
            //BuffPlugin.Log($"{this} destroy");
            AnimateComponentBase cmpntInMachine = this;
            while (!cmpntInMachine.inMachine)
                cmpntInMachine = cmpntInMachine.lastCmpnt;
            if (cmpntInMachine != null)
            {
                cmpntInMachine.HandleDestroy();
                return;
            }

            cmpntInMachine = this;
            while (!cmpntInMachine.inMachine)
                cmpntInMachine = cmpntInMachine.nextCmpnt;
            if (cmpntInMachine != null)
                cmpntInMachine.HandleDestroy();
        }

        //处理删除
        protected virtual void HandleDestroy()
        {
            //BuffPlugin.Log($"{this} HandleDestroy");
            inMachine = false;
            AnimMachine.RemoveRef(this);
        }
    }


    public abstract class AnimateComponent<T> : AnimateComponentBase where T : AnimateComponent<T>
    {
        public Action<T> OnAnimStart;
        public Action<T> OnAnimUpdate;
        public Action<T, float> OnAnimGrafUpdate;
        public Action<T> OnAnimFinished;
        public Func<float, float> modifier;

        public AnimateComponent(bool autoStart, bool autoDestroy) : base(autoStart, autoDestroy)
        {
            SetAutoDestroy(autoDestroy);
        }

        protected void SetAutoDestroy(bool autoDestroy)
        {
            if(this.autoDestroy == autoDestroy)
            {
                return;
            }
            if (autoDestroy)
            {
                OnAnimFinished += AutoDestroy;
            }
            else
                OnAnimFinished -= AutoDestroy;
            this.autoDestroy = autoDestroy;
        }

        void AutoDestroy(T self)
        {
            if (nextCmpnt == null)
                Destroy();
            else
                HandleNext();
        }

        public T BindActions(Action<T> OnAnimStart = null, Action<T> OnAnimUpdate = null, Action<T, float> OnAnimGrafUpdate = null, Action<T> OnAnimFinished = null)
        {
            return AnimMachine.BindActions(this as T, OnAnimStart, OnAnimUpdate, OnAnimGrafUpdate, OnAnimFinished);
        }

        public T AutoPause()
        {
            return BindActions(OnAnimFinished: (self) => self.SetEnable(false));
        }

        public TickAnimCmpnt GetTickAnimCmpntAfterFinish(int lowBound, int highBound, int? initial = null, int tick = 1, bool autoDestroy = false)
        {
            var result = AnimMachine.GetTickAnimCmpnt(lowBound, highBound, initial, tick, false, autoDestroy);
            nextCmpnt = result;
            SetAutoDestroy(true);

            return result;
        }
        public LerpAnimCmpnt GetLerpAnimCmpntAfterFinish(float lowBound, float highBound, float? initial = null, float lerpFactor = 0.15f, bool autoDestroy = false)
        {
            var result = AnimMachine.GetLerpAnimCmpnt(lowBound, highBound, initial, lerpFactor, false, autoDestroy);
            result.lastCmpnt = this;
            nextCmpnt = result;
            SetAutoDestroy(true);

            return result;
        }

        public DelayCmpnt GetDelayCmpntAfterFinish(int ticks, bool autoStart = true, bool autoDestroy = false)
        {
            var result = AnimMachine.GetDelayCmpnt(ticks, false, autoDestroy);
            result.lastCmpnt = this;
            nextCmpnt = result;
            SetAutoDestroy(true);

            return result;
        }

        public T BindModifier(Func<float, float> modifier)
        {
            return AnimMachine.BindModifier(this as T, modifier);
        }

        protected float ApplyModifier(float t)
        {
            if (modifier != null)
                return modifier.Invoke(t);
            return t;
        }
    }

    //基于计时器的动画，本身不具有缓动效果，持续时间完全可控
    public class TickAnimCmpnt : AnimateComponent<TickAnimCmpnt>
    {
        public int lowBound;
        public int highBound;
        public int current;
        public int last;
        public int tick;
        int initial;


        public TickAnimCmpnt(int lowBound, int highBound, int? initial = null, int tick = 1, bool autoStart = true, bool autoDestroy = false) : base(autoStart, autoDestroy)
        {
            if (lowBound >= highBound)
                throw new ArgumentException("highBound must be greater than lowBound");
            this.lowBound = lowBound;
            this.highBound = highBound;
            if (initial == null)
                initial = lowBound;
            this.initial = current = last = initial.Value;
            this.tick = tick;

            if (autoStart && lastCmpnt == null)
                HandleInMachine();
        }

        public override void Update()
        {
            base.Update();
            if (!enable)
                return;
            if (current == initial)
                OnAnimStart?.Invoke(this);

            last = current;
            if((tick > 0 && current <= highBound) || (tick < 0 && current >= lowBound))
            {
                current += tick;
                current = Mathf.Clamp(current, lowBound, highBound);
                OnAnimUpdate?.Invoke(this);
                if((current == highBound || current == lowBound) && !finished)
                {
                    OnAnimFinished?.Invoke(this);
                    finished = true;
                }
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            OnAnimGrafUpdate?.Invoke(this, timeStacker);
        }

        public override float Get()
        {
            return ApplyModifier(Mathf.InverseLerp((float)lowBound, (float)highBound, Mathf.Lerp((float)last, (float)current, AnimMachine.timeStacker)));
        }

        public override void Reset()
        {
            base.Reset();
            Reset(initial);
        }

        public void Reset(int value)
        {
            current = last = value;
        }

        public void SetTickAndStart(int tick)
        {
            Reset(current);
            this.tick = tick;
            SetEnable(true);
        }

        public override string ToString()
        {
            return $"{base.ToString()}_{id}";
        }
    }

    //使用Lerp来实现的动画，本身具有缓动效果，但持续时间很难预测
    public class LerpAnimCmpnt : AnimateComponent<LerpAnimCmpnt>
    {
        public float lowBound;
        public float highBound;
        public float current;
        public float last;
        public float lerpFactor;
        float initial;

        public LerpAnimCmpnt(float lowBound, float highBound, float? initial = null, float lerpFactor = 0.15f, bool autoStart = true, bool autoDestroy = false) : base(autoStart, autoDestroy)
        {
            if (lowBound >= highBound)
                throw new ArgumentException("highBound must be greater than lowBound");
            this.lowBound = lowBound;
            this.highBound = highBound;
            if (initial == null)
                initial = lowBound;
            this.initial = current = last = initial.Value;
            this.lerpFactor = lerpFactor;

            if (autoStart && lastCmpnt == null)
                HandleInMachine();
        }

        public override void Update()
        {
            base.Update();
            if (!enable)
                return;

            if (current == initial)
                OnAnimStart?.Invoke(this);

            OnAnimUpdate?.Invoke(this);
            last = current;
            if(current != highBound)
            {
                current = Mathf.Lerp(current, highBound, lerpFactor);
                if(Mathf.Approximately(current, highBound))
                {
                    current = highBound;
                    finished = true;
                    OnAnimFinished?.Invoke(this);
                }
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            OnAnimGrafUpdate?.Invoke(this, timeStacker);
        }

        public override float Get()
        {
            return ApplyModifier(Mathf.InverseLerp(lowBound, highBound, Mathf.Lerp(last, current, AnimMachine.timeStacker)));
        }

        public override void Reset()
        {
            base.Reset();
            current = last = initial;
        }

        public override string ToString()
        {
            return $"{base.ToString()}_{id}";
        }
    }

    //仅有启动事件和结束事件生效
    public class DelayCmpnt : AnimateComponent<DelayCmpnt>
    {
        int ticks;
        int current;

        public DelayCmpnt(int ticks, bool autoStart = true, bool autoDestroy = false) : base(autoStart, autoDestroy)
        {
            this.ticks = ticks;
            if (autoStart && lastCmpnt == null)
                HandleInMachine();
        }

        public override void Update()
        {
            base.Update();
            if (!enable)
                return;
            if (current == 0)
                OnAnimStart?.Invoke(this);

            if(current <= ticks)
            {
                current++;
                if (current == ticks && !finished)
                {
                    OnAnimFinished?.Invoke(this);
                    finished = true;
                }
            }
        }
    }
}
