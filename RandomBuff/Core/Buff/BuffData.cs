 using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
 using Newtonsoft.Json;
 using RandomBuff.Core.Entry;
 using RandomBuff.Core.SaveData;
using RandomBuff.Core.SaveData.BuffConfig;
using UnityEngine;

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

        private Dictionary<string,BuffConfigurable> bindConfigurables = new();

        internal object GetConfigurableValue(string propertyName)
        {
            if(bindConfigurables.TryGetValue(propertyName, out var value))
            {
                BuffPlugin.Log($"Get property {propertyName} configurable : {value.valueType}-{value.BoxedValue}");
                return value.BoxedValue;
            }
            else
            {
                var result = BuffConfigurableManager.TryGetConfigurable(ID, propertyName);

                if(result.configurable == null)
                {
                    foreach (var property in GetType().GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (property.Name != propertyName)
                            continue;

                        var configAttribute = property.GetCustomAttribute<CustomBuffConfigAttribute>(true);
                        var infoAttribute = property.GetCustomAttribute<CustomBuffConfigInfoAttribute>();
                        result = BuffConfigurableManager.TryGetConfigurable(ID, propertyName, true, property.PropertyType, configAttribute.defaultValue);

                        if (infoAttribute != null)
                        {
                            result.configurable.name = infoAttribute.name;
                            result.configurable.description = infoAttribute.description;
                        }
                        else
                        {
                            result.configurable.name = propertyName;
                            result.configurable.description = "";
                        }
                        BuffPlugin.Log($"New configurable name : {result.configurable.name}, description : {result.configurable.description}");

                        var acceptable = BuffConfigurableManager.GetProperAcceptable(configAttribute);
                        result.configurable.acceptable = acceptable;
                        bindConfigurables.Add(propertyName, result.configurable);
                        break;
                    }
                }
                else
                {
                    bindConfigurables.Add(propertyName, result.configurable);
                }
                
                if (result.configurable == null)
                    throw new NotSupportedException($"{propertyName} not supported!");
                else
                    return result.configurable.BoxedValue;
            }
        }

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
        public virtual bool CanStackMore()
        {
            return StackLayer < ID.GetStaticData().MaxStackLayers;
        }


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
        //public T GetConfig<T>(string name)
        //{
        //    if (BuffConfigManager.Instance.TryGet<T>(ID, name, out var data))
        //        return data;
        //    return default;
        //}


        //internal object GetConfig(string name,Type type)
        //{
        //    if (BuffConfigManager.Instance.TryGet(ID, name,type, out var data))
        //        return data;
        //    return default;
        //}
    }




    /// <summary>
    /// 带轮回倒数的BuffData
    /// </summary>
    public abstract class CountableBuffData : BuffData
    {
        public abstract override BuffID ID { get; }

        /// <summary>
        /// 最大轮回数量
        /// 超过数量会删除
        /// </summary>
        public abstract int MaxCycleCount { get; }


        public override void Stack()
        {
            CycleUse = 0;
            base.Stack();
        }

        [JsonProperty]
        public int CycleUse { get; protected set; }

        public override bool NeedDeletion => CycleUse >= MaxCycleCount;

        public override void CycleEnd()
        {
            base.CycleEnd();
            CycleUse++;
        }
    }

    /// <summary>
    /// 自带按键绑定的BuffData
    /// </summary>
    public abstract class KeyBindBuffData : BuffData
    {
        [CustomBuffConfigInfo("Player1", "bind key for player 1")]
        [CustomBuffConfigEnum(typeof(KeyCode), "None")]
        public KeyCode Player1 { get; }

        [CustomBuffConfigInfo("Player2", "bind key for player 2")]
        [CustomBuffConfigEnum(typeof(KeyCode), "None")]
        public KeyCode Player2 { get; }

        [CustomBuffConfigInfo("Player3", "bind key for player 3")]
        [CustomBuffConfigEnum(typeof(KeyCode), "None")]
        public KeyCode Player3 { get; }

        [CustomBuffConfigInfo("Player4", "bind key for player 4")]
        [CustomBuffConfigEnum(typeof(KeyCode), "None")]
        public KeyCode Player4 { get; }

        public KeyCode this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Player1;
                    case 1: return Player2;
                    case 2: return Player3;
                    case 3: return Player4;
                    default:
                        return KeyCode.None;
                }
            }
        }
    }

}
