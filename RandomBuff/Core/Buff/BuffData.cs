 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 using Newtonsoft.Json;
 using RandomBuff.Core.SaveData;

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
        public abstract void DataLoaded(bool newData);


        /// <summary>
        /// 获取静态配置数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public T GetConfig<T>(string name)
        {
            if (BuffConfigManager.Instance.TryGet<T>(ID, name, out var data))
                return data;
            return default;
        }


        internal object GetConfig(string name,Type type)
        {
            if (BuffConfigManager.Instance.TryGet(ID, name,type, out var data))
                return data;
            return default;
        }
    }
}
