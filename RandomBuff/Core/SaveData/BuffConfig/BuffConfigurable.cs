using Newtonsoft.Json.Linq;
using RandomBuff.Core.Buff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.SaveData.BuffConfig
{
    internal class BuffConfigurable
    {
        public BuffID id;
        public Type valueType;
        public TypeSerializer serializer;
        public string key;

        public string name;
        public string description;
        public BuffConfigurableAcceptableBase acceptable;

        object lastSavedValue;
        object _boxedValue;
        public object BoxedValue
        {
            get => _boxedValue;
            set
            {
                if(_boxedValue != value)
                {
                    _boxedValue = value;
                    valueDirty = value != lastSavedValue;
                }
            }
        }
        public bool valueDirty;

        public BuffConfigurable(BuffID id,string key, Type type, object defaultValue)
        {
            this.id = id;
            this.key = key;
            valueType = type;
            serializer = TypeSerializer.GetSerializer(valueType);

            _boxedValue = defaultValue;
            lastSavedValue = defaultValue;
        }

        public void LoadConfig(string value)
        {
            valueDirty = false;
            object val = serializer.Deserialize(value);
            _boxedValue = val;
            lastSavedValue = val;
        }

        public void Set(string value)
        {
            BoxedValue = serializer.Deserialize(value);
        }

        public void SaveConfig(StringBuilder builder)
        {
            valueDirty = false;
            lastSavedValue = BoxedValue;

            builder.Append(key);
            builder.Append(BuffConfigManager.ParameterIdSplit);
            builder.Append(serializer.Serialize(BoxedValue));
            builder.Append(BuffConfigManager.ParameterSplit);
        }

        public string PushString(bool changeSavedValue = true)
        {
            if (changeSavedValue)
                lastSavedValue = BoxedValue;
            valueDirty = false;
            return serializer.Serialize(BoxedValue);
        }

        public void ResetConfig()
        {
            BoxedValue = lastSavedValue;
        }
    }

}
