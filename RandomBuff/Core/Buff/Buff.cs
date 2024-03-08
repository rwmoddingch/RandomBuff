using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;

namespace RandomBuff.Core.Buff
{

    /// <summary>
    /// Buff接口
    /// 不应被除了Buff以外的类型继承
    /// </summary>
    public interface IBuff
    {

        public BuffID ID { get; }

        public bool Active { get; }

        public bool Triggerable { get; }

        public BuffTimer MyTimer { get; set; }

        public bool Trigger(RainWorldGame game);

        public void Update(RainWorldGame game);

        public void Destroy();

    }
    /// <summary>
    /// Buff基类
    /// </summary>
    public abstract class Buff<TBuff, TData> : IBuff where TBuff : Buff<TBuff, TData> where TData : BuffData , new()
    {
        /// <summary>
        /// 数据类属性，只读
        /// </summary>
        public TData Data => (TData)ID.GetBuffData();

        /// <summary>
        /// 单例获取
        /// </summary>
        public static TBuff Instance { get; private set; }

        /// <summary>
        /// 增益ID
        /// </summary>
        public abstract BuffID ID { get; }

        /// <summary>
        /// 是否处于激活状态
        /// </summary>
        public virtual bool Active => true;

        /// <summary>
        /// 是否可以触发
        /// </summary>
        public virtual bool Triggerable => true;

        /// <summary>
        /// 点击触发方法，仅对可触发的增益有效。
        /// </summary>
        /// <param name="game"></param>
        /// <returns>返回true时，代表该增益已经完全触发，增益将会被减少堆叠层数（或移除）</returns>
        public virtual bool Trigger(RainWorldGame game) => false;

        /// <summary>
        /// 卡牌的计时器，在构造方法中赋值后，不可为null
        /// 赋值后可以在任意时机修改值
        /// </summary>
        public BuffTimer MyTimer { get; set; }


        /// <summary>
        /// 增益的更新方法，与RainWorldGame.Update同步
        /// </summary>
        public virtual void Update(RainWorldGame game)
        {
            MyTimer?.Update(game);
        }


        /// <summary>
        /// 增益的销毁方法，当该增益实例被移除的时候会调用
        /// 注意：当前轮回结束时会清除全部的Buff物体
        /// </summary>
        public virtual void Destroy()
        {
            Instance = null;
        }

        /// <summary>
        /// 强制触发增益效果，一般用于显示HUD反馈
        /// </summary>
        public void TriggerSelf(bool ignoreCheck = false)
        {
            if (BuffPoolManager.Instance.TriggerBuff(ID, ignoreCheck))
            {
                BuffHud.Instance.RemoveCard(ID);
            }
        }


        protected Buff()
        {
            Instance = (TBuff)this;
        }
    }

    /// <summary>
    /// 动态创建Buff的基类
    /// </summary>
    public abstract class RuntimeBuff : IBuff
    {

        public abstract BuffID ID { get; }

        public virtual bool Active => true;

        public virtual bool Triggerable => false;

        public virtual bool Trigger(RainWorldGame game) => false;

        public BuffTimer MyTimer { get; set; }

        public virtual void Update(RainWorldGame game)
        {
            MyTimer?.Update(game);
        }

        public virtual void Destroy() { }


        protected RuntimeBuff() { }
    }
}
