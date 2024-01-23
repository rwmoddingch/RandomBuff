 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 using Newtonsoft.Json;

 namespace RandomBuff.Core.Buff
{
    /// <summary>
    /// Buff的数据类，包括动态数据（单一猫存档内数据）
    /// 会序列化全部包含JsonProperty的属性
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class BuffData 
    {
        [JsonProperty]
        public abstract BuffID ID { get; }


        protected BuffData()
        {
            
        }

        /// <summary>
        /// 轮回结束时候触发
        /// </summary>
        public virtual void CycleEnd() { }



        /// <summary>
        /// 重复选取
        /// 增加堆叠次数
        /// </summary>
        public virtual void Stack() { }


        /// <summary>
        /// 当存档数据读取后调用
        /// </summary>
        public abstract void OnDataLoaded(bool newData);
    }
}
