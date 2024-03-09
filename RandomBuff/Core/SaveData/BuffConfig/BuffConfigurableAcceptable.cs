using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.SaveData.BuffConfig
{
    internal abstract class BuffConfigurableAcceptableBase
    {
        public readonly Type valueType;
        public readonly object defaultValue;

        protected BuffConfigurableAcceptableBase(object defaultValue)
        {
            this.defaultValue = defaultValue;
            valueType = defaultValue.GetType();
        }

        public abstract object Clamp(object value);

        public abstract bool IsValid(object value);
    }

    internal class BuffConfigurableAcceptableRange : BuffConfigurableAcceptableBase
    {
        public object minValue;
        public object maxValue;

        public BuffConfigurableAcceptableRange(object defaultValue, object minValue, object maxValue) : base(defaultValue)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
        }

        public override object Clamp(object value)
        {
            if (Helper.DynamicImitator.Greater(value, maxValue))
                return maxValue;
            if(Helper.DynamicImitator.Smaller(value, minValue))
                return minValue;
            return value;
        }

        public object GetLerpedValue(float t)
        {
            return Helper.DynamicImitator.Addition(Helper.DynamicImitator.Multiply(minValue,1f - t), Helper.DynamicImitator.Multiply(maxValue, t));
        }

        public override bool IsValid(object value)
        {
            return Helper.DynamicImitator.GreaterOrEqual(value, minValue) && Helper.DynamicImitator.SmallerOrEqual(value, maxValue);
        }
    }

    internal class BuffConfigurableAcceptableList : BuffConfigurableAcceptableBase
    {
        public object defaultValueString;
        public object[] values;

        public BuffConfigurableAcceptableList(object defaultValues, object[] values) : base(defaultValues)
        {
            this.defaultValueString = defaultValues;
            this.values = values;

            BuffPlugin.Log($"Create BuffConfigurableAcceptableList of {defaultValue.GetType()}, default : {defaultValue}");
        }

        public override object Clamp(object value)
        {
            if(values.Contains(value))
                return value;
            return values.First();
        }

        public override bool IsValid(object value)
        {
            return values.Contains(value);
        }
    }

    internal class BuffConfigurableAcceptableKeyCode : BuffConfigurableAcceptableBase
    {
        public KeyCode defaultkey;

        public BuffConfigurableAcceptableKeyCode(KeyCode defaultkey) : base(defaultkey)
        {
            this.defaultkey = defaultkey;
        }

        public override object Clamp(object value)
        {
            return null;
        }

        public override bool IsValid(object value)
        {
            return true;
        }
    }

    internal class BuffConfigurableAcceptableTwoValue : BuffConfigurableAcceptableBase
    {
        public object valueA;
        public object valueB;

        public BuffConfigurableAcceptableTwoValue(object valueA, object valueB) : base(valueA) 
        {
            this.valueA = valueA;
            this.valueB = valueB;
        }

        public override object Clamp(object value)
        {
            if (value != valueA && value != valueB)
                return valueA;
            return value;
        }

        public object GetAnother(object value)
        {
            if (value == valueA)
                return valueB;
            else
                return valueA;
        }

        public override bool IsValid(object value)
        {
            return value == valueA || value == valueB;
        }
    }
}
