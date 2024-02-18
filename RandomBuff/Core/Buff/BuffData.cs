 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 using Newtonsoft.Json;
 using RandomBuff.Core.Entry;
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

        [JsonProperty]
        public int StackLayer { get; set; }

        protected BuffData() { }

        /// <summary>
        /// 轮回结束时候触发
        /// </summary>
        public virtual void CycleEnd() { }


        /// <summary>
        /// 如果为true，则在周期结束时自动移除Buff
        /// </summary>
        public virtual bool NeedDeletion { get; set; }

        /// <summary>
        /// 重复选取
        /// 增加堆叠次数
        /// </summary>
        public virtual void Stack()
        {
            StackLayer++;
        }

        internal BuffData Clone()
        {
           return (BuffData)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(this), BuffRegister.GetDataType(ID));
        }

        /// <summary>
        /// 增加减少堆叠次数，在删除的时候被调用
        /// </summary>
        public virtual void UnStack()
        {
            StackLayer--;
        }

        /// <summary>
        /// 是否可以继续增加
        /// </summary>
        public virtual bool CanStackMore() => true;


        /// <summary>
        /// 当存档数据读取后调用
        /// </summary>
        public virtual void DataLoaded(bool newData){}


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
