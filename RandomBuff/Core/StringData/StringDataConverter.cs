using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.StringData
{
    /// <summary>
    /// 
    /// </summary>
    public static class StringDataConverter
    {
        internal static List<Converter> registedConverter = new List<Converter>();

        public static void SetUpConverters()
        {
            registedConverter.Add(new FloatConverter());
            registedConverter.Add(new IntConverter());
        }

        /// <summary>
        /// 获取匹配类型的格式转换器
        /// </summary>
        /// <param name="type"></param>
        /// <returns>当没有合适的转换器时，会返回null</returns>
        public static Converter GetMatchedConverter(Type type)
        {
            foreach(var converter in registedConverter)
            {
                if (converter.SupportThisType(type))
                    return converter;
            }

            //struct类型
            if (type.IsValueType && !type.IsEnum && !type.IsPrimitive)
            {
                var result = new StructConverter(type);
                registedConverter.Add(result);
                return result;
            }
            return null;
        }

        public abstract class Converter
        {
            public virtual bool SupportThisType(Type type)
            {
                throw new NotImplementedException();
            }

            public virtual object ConvertToValue(string value)
            {
                throw new NotImplementedException();
            }

            public virtual string ConvertToString(object value)
            {
                throw new NotImplementedException();
            }
        }
    }

    public sealed class FloatConverter : StringDataConverter.Converter
    {
        public override bool SupportThisType(Type type)
        {
            return type == typeof(float);
        }

        public override string ConvertToString(object value)
        {
            return value.ToString();
        }

        public override object ConvertToValue(string value)
        {
            return float.Parse(value);
        }
    }

    public sealed class IntConverter : StringDataConverter.Converter
    {
        public override bool SupportThisType(Type type)
        {
            return type == typeof(int);
        }

        public override string ConvertToString(object value)
        {
            return value.ToString();
        }

        public override object ConvertToValue(string value)
        {
            return int.Parse(value);
        }
    }

    public sealed class StructConverter : StringDataConverter.Converter
    {
        internal Type structType;
        internal FieldInfo[] fields;
        internal StringDataConverter.Converter[] fieldConverters;

        public StructConverter(Type structType)
        {
            this.structType = structType;
            fields = structType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            fieldConverters = new StringDataConverter.Converter[fields.Length];

            for(int i = 0; i < fields.Length; i++)
            {
                fieldConverters[i] = StringDataConverter.GetMatchedConverter(fields[i].FieldType);
                if (fieldConverters[i] == null)
                {
                    throw new ArgumentException($"\"{fields[i].Name}\" field of {structType.FullName} is not supported");
                }
            }
        }

        public override bool SupportThisType(Type type)
        {
            return type == structType;
        }

        public override string ConvertToString(object value)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for(int i = 0; i < fields.Length; i++)
            {
                stringBuilder.Append(fields[i].GetValue(value));
                if(i < fields.Length - 1)
                    stringBuilder.Append("|");
            }
            return stringBuilder.ToString();
        }

        public override object ConvertToValue(string value)
        {
            object[] parameters = new object[fields.Length];
            string[] stringParas = value.Split('|');

            for(int i = 0;i < fields.Length; i++)
            {
                parameters[i] = fieldConverters[i].ConvertToValue(stringParas[i]);
            }

            var result = Activator.CreateInstance(structType);
            for (int i = 0; i < parameters.Length; i++)
            {
                fields[i].SetValue(result, parameters[i]);
            }
            return result; ;
        }
    }
}
