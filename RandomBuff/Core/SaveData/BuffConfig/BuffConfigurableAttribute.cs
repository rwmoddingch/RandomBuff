using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.SaveData.BuffConfig
{
    /// <summary>
    /// BuffData 中
    /// 若属性设置该attribute则会设置get函数获取自定义配置
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class CustomBuffConfigAttribute: Attribute
    {
        public Type valueType;
        public object defaultValue;
    }

    /// <summary>
    /// 普通enum或extEnum类型，将会在ui中生成下拉选框，针对KeyCode类型将会生成特殊的案件绑定器
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CustomBuffConfigEnumAttribute : CustomBuffConfigAttribute
    {
        public CustomBuffConfigEnumAttribute(Type enumType, string defaultValue)
        {
            BuffPlugin.Log($"CustomBuffConfigEnumAttribute : {enumType}, {defaultValue}");

            if(!(enumType.IsEnum) && !(enumType.IsSubclassOf(typeof(ExtEnumBase))))
                throw new ArgumentException("CustomBuffConfigEnumAttribute param type mismatch!");
            this.defaultValue = Activator.CreateInstance(enumType, defaultValue, false);
            valueType = enumType;
        }
    }

    /// <summary>
    /// 两值类型，将会在ui中生成单选按钮
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CustomBuffConfigTwoValueAttribute : CustomBuffConfigAttribute
    {
        public object valueA;
        public object valueB;

        public CustomBuffConfigTwoValueAttribute(object defaultValue, object valueB)
        {
            if(defaultValue.GetType() != valueB.GetType())
                throw new ArgumentException("CustomBuffConfigTwoValueAttribute param type mismatch!");

            this.valueA = defaultValue;
            this.valueB = valueB;
            this.defaultValue = valueA;
            valueType = defaultValue.GetType();
        }
    }

    /// <summary>
    /// 范围类型，将会在ui中生成滑条
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CustomBuffConfigRangeAttribute : CustomBuffConfigAttribute
    {
        public object minValue;
        public object maxValue;

        public CustomBuffConfigRangeAttribute(object defaultValue, object minValue, object maxValue)
        {
            var typeDefault = valueType = defaultValue.GetType();
            var typeMin = minValue.GetType();
            var typeMax = maxValue.GetType();   

            if(typeMax != typeDefault || typeMin != typeDefault || typeMin != typeMax)
                throw new ArgumentException("CustomBuffConfigRangeAttribute param type mismatch!");

            if (!Helper.DynamicImitator.SupportAndCreate(typeDefault))
                throw new ArgumentException($"CustomBuffConfigRangeAttribute {typeDefault} not supported!");


            this.defaultValue = defaultValue;
            this.minValue = minValue;
            this.maxValue = maxValue;
            

            if(Helper.DynamicImitator.GreaterOrEqual(minValue, maxValue))
                throw new ArgumentException("CustomBuffConfigRangeAttribute minValue has to be lower than maxValue");

            if (Helper.DynamicImitator.Smaller(defaultValue, minValue) || Helper.DynamicImitator.Greater(defaultValue,maxValue))
                throw new ArgumentException("CustomBuffConfigRangeAttribute default value must be inside range");
        }
    }

    /// <summary>
    /// 列表类型，将会在ui中生成下拉选框
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CustomBuffConfigListAttribute : CustomBuffConfigAttribute
    {
        public object[] values;

        public CustomBuffConfigListAttribute(object defaultValue, object[] values)
        {
            if (!values.Contains(defaultValue))
                throw new ArgumentException("CustomBuffConfigListAttribute default value must be contained in values!");
            this.defaultValue = defaultValue;
            this.values = values;

            valueType = defaultValue.GetType();
        }
    }

    public sealed class CustomBuffConfigInfoAttribute : Attribute
    {
        public string name;
        public string description;

        public CustomBuffConfigInfoAttribute(string name, string description)
        {
            this.name = name;
            this.description = description;
        }
    }
}
