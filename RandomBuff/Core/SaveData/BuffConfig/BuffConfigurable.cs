using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.SaveData.BuffConfig
{
    internal class BuffConfigurable<T>
    {
        Type valueType;
        TypeSerializer serializer;

        T _value;
        public T Value
        {
            get => _value;
            set
            {
                if(!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    _value = value;
                    valueDirty = true;
                }
            }
        }

        public T lastSavedValue;
        public bool valueDirty;

        public BuffConfigurable(string key)
        {
            valueType = typeof(T);
            serializer = TypeSerializer.GetSerializer(valueType);
        }
    }
}
