using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace RandomBuff.Core.SaveData.BuffConfig
{
    public abstract partial class TypeSerializer
    {
        public abstract string Serialize(object value);

        public abstract object Deserialize(string value);

        public T Deserialize<T>(string value) => (T)Deserialize(value);

    }


    public abstract partial class TypeSerializer
    {
        public static void Register<TType>(TypeSerializer typeSerializer)
        {
            Register(typeof(TType),typeSerializer);
        }

        public static void Init()
        {
            Register<Vector2>(new UnityTypeSerializer<Vector2>());
            Register<Vector3>(new UnityTypeSerializer<Vector3>());
            Register<Color>(new UnityTypeSerializer<Color>());
        }

        public static void Register(Type type, TypeSerializer typeSerializer)
        {
            if(!SerializerDic.ContainsKey(type))
                SerializerDic.Add(type, typeSerializer);
            else
                BuffPlugin.LogError("Same Key already contains in TypeSerializer Dictionary");
        }

        public static TypeSerializer GetSerializer<TType>()
        {
            return GetSerializer(typeof(TType));
        }

        public static TypeSerializer GetSerializer(Type type)
        {
            if(SerializerDic.ContainsKey(type))
                return SerializerDic[type];
            else
            {
                TypeSerializer re;
                if (type.IsSubclassOf(typeof(ExtEnumBase)))
                {
                    re = new ExtEnumSerializer(type);
                }
                else
                {
                    re = new DefaultTypeSerializer(type);
                }
                BuffPlugin.Log($"Missing serializer for {type},now registing {re}");
                Register(type,re);
                return re;
            }
        }

        private static readonly Dictionary<Type,TypeSerializer> SerializerDic = new ();
    }

    public class UnityTypeSerializer<TUnityType> : TypeSerializer
    {
        public override string Serialize(object value)
        {
            return JsonUtility.ToJson(value);
        }

        public override object Deserialize(string value)
        {
            return JsonUtility.FromJson<TUnityType>(value);
        }
    }

    public class DefaultTypeSerializer : TypeSerializer
    {
        public DefaultTypeSerializer(Type type)
        {
            this.type = type;
        }
        public readonly Type type;

        public override string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value);
        }

        public override object Deserialize(string value)
        {
            return JsonConvert.DeserializeObject(value, type);
        }
    }

    public class ExtEnumSerializer : TypeSerializer
    {
        Type type;
        ExtEnumType enumType;
        public ExtEnumSerializer(Type type)
        {
            this.type = type;
            enumType = ExtEnumBase.GetExtEnumType(type);
        }

        public override string Serialize(object value)
        {
            return (value as ExtEnumBase).value;
        }

        public override object Deserialize(string value)
        {
            return Activator.CreateInstance(type, value, false);
        }
    }
}
