using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public bool NeedDeletion { get; set; }

        public bool Trigger(RainWorldGame game);

        public void Update(RainWorldGame game);

        public void Destroy();

    }
    /// <summary>
    /// Buff基类
    /// </summary>
    public abstract class Buff<TData> : IBuff where TData : BuffData , new()
    {
        /// <summary>
        /// 数据类属性，只读
        /// </summary>
        public TData Data => (TData)BuffDataManager.Instance.GetBuffData(ID);

        public abstract BuffID ID { get; }


        /// <summary>
        /// 如果为true，则在周期结束时自动移除Buff
        /// </summary>
        public bool NeedDeletion { get; set; }


        /// <summary>
        /// 点击触发方法，仅对可触发的增益有效。
        /// </summary>
        /// <param name="game"></param>
        /// <returns>返回true时，代表该增益已经完全触发，增益将会被移除</returns>
        public virtual bool Trigger(RainWorldGame game) => false;



        /// <summary>
        /// 增益的更新方法，与RainWorldGame.Update同步
        /// </summary>
        public virtual void Update(RainWorldGame game){}


        /// <summary>
        /// 增益的销毁方法，当该增益实例被移除的时候会调用
        /// 注意：当前轮回结束时会清除全部的Buff物体
        /// </summary>
        public virtual void Destroy(){}


        protected Buff() { }
    }
}
