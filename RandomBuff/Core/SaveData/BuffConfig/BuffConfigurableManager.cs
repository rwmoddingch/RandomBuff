using RandomBuff.Core.Buff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.SaveData.BuffConfig
{
    //configurable部分
    internal static partial class BuffConfigurableManager
    {
        public static List<BuffConfigurable> buffConfigurables = new List<BuffConfigurable>();
        public static Dictionary<BuffID, Dictionary<string, BuffConfigurable>> configurablesMapper = new();

        public static (BuffConfigurable configurable, bool createNew) TryGetConfigurable(BuffID id, string key, bool addIfMissing = false, Type newType = null, object defaultValue = null)
        {
            BuffPlugin.Log($"Try get configurables: id-{id}, key-{key}, addIfMissing-{addIfMissing}");
            BuffConfigurable result = null;
            bool missingResult = false;
            bool creatingNew = false; ;
            if (configurablesMapper.ContainsKey(id))
            {
                if (configurablesMapper[id].ContainsKey(key))
                {
                    result = configurablesMapper[id][key];
                }
                else if(addIfMissing)
                    missingResult = true;
            }
            else if(addIfMissing)
            {
                missingResult = true;
                configurablesMapper.Add(id, new());
            }
            if (missingResult)
            {
                result = new BuffConfigurable(id, key, newType, defaultValue);
                configurablesMapper[id].Add(key, result);
                buffConfigurables.Add(result);
                creatingNew = true;
            }
            BuffPlugin.Log($"Created result : {result}, creatingNew : {creatingNew}");
            return (result, creatingNew);
        }

        /// <summary>
        /// 获取该buff下所有的configurable，如果没有会返回空数组
        /// </summary>
        /// <param name="buffID"></param>
        /// <returns></returns>
        public static BuffConfigurable[] GetAllConfigurableForID(BuffID buffID)
        {
            if (configurablesMapper.ContainsKey(buffID))
                return configurablesMapper[buffID].Values.ToArray();
            return new BuffConfigurable[0];
        }

        public static void FetchAllConfigs()
        {
            BuffPlugin.Log($"Fetching configurables: {buffConfigurables.Count}");
            foreach (var config in buffConfigurables)
            {
                bool missingConfig = false;
                if (BuffConfigManager.Instance.allConfigs.ContainsKey(config.id.value))
                {
                    if (BuffConfigManager.Instance.allConfigs[config.id.value].ContainsKey(config.key))
                    {
                        config.LoadConfig(BuffConfigManager.Instance.allConfigs[config.id.value][config.key]);
                    }
                    else
                        missingConfig = true;
                }
                else
                {
                    BuffConfigManager.Instance.allConfigs.Add(config.id.value, new Dictionary<string, string>());
                    missingConfig = true;
                }

                if (missingConfig)
                {
                    BuffConfigManager.Instance.allConfigs[config.id.value].Add(config.key, config.serializer.Serialize(config.BoxedValue));
                    BuffPlugin.Log($"Fetching {config.id}-{config.key} but missing field in config, now adding : type:{config.valueType},default:{config.BoxedValue}, acceptable : {config.acceptable}");
                }
            }
        }

        public static void PushAllConfigs()
        {
            BuffPlugin.Log($"Pushing configurables: {buffConfigurables.Count}");
            foreach (var config in buffConfigurables)
            {
                BuffConfigManager.Instance.allConfigs[config.id.value][config.key] = config.PushString();
            }
        }
    }

    //Acceptable部分
    internal static partial class BuffConfigurableManager
    {
        public static BuffConfigurableAcceptableBase GetProperAcceptable(CustomBuffConfigAttribute attribute)
        {
            BuffPlugin.Log($"BuffConfigurableManager GetProperAcceptable of {attribute}");
            if(attribute is CustomBuffConfigEnumAttribute enumConfig)
            {
                if (enumConfig.valueType == typeof(KeyCode))
                    return new BuffConfigurableAcceptableKeyCode((KeyCode)enumConfig.defaultValue);
                else if(enumConfig.valueType.IsSubclassOf(typeof(ExtEnumBase)))
                {
                    var extEnumType = ExtEnumBase.GetExtEnumType(enumConfig.valueType);
                    string[] entries = extEnumType.entries.ToArray();
                    object[] extEnums = new object[entries.Length];
                    for(int i = 0;i < entries.Length; i++)
                    {
                        extEnums[i] = Activator.CreateInstance(enumConfig.valueType, entries[i], false);
                    }

                    return new BuffConfigurableAcceptableList(enumConfig.defaultValue, extEnums);
                }
                else if(enumConfig.valueType.IsEnum)
                {
                    string[] nameList = Enum.GetNames(enumConfig.valueType);
                    object[] enumList = new object[nameList.Length];
                    for(int i = 0;i < enumList.Length; i++)
                    {
                        enumList[i] = Enum.Parse(enumConfig.valueType, nameList[i]);
                    }

                    return new BuffConfigurableAcceptableList(enumConfig.defaultValue, enumList);
                }
            }
            else if(attribute is CustomBuffConfigTwoValueAttribute twoValueConfig)
            {
                return new BuffConfigurableAcceptableTwoValue(twoValueConfig.valueA, twoValueConfig.valueB);
            }
            else if(attribute is CustomBuffConfigRangeAttribute rangeConfig)
            {
                return new BuffConfigurableAcceptableRange(rangeConfig.defaultValue, rangeConfig.minValue, rangeConfig.maxValue);
            }
            else if(attribute is CustomBuffConfigListAttribute listConfig)
            {
                return new BuffConfigurableAcceptableList(listConfig.defaultValue, listConfig.values);
            }
            return null;
        }
    }
}
