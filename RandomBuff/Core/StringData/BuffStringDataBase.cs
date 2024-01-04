using RandomBuff.Core.SaveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.StringData
{
    /// <summary>
    /// BuffStringData基类，不要直接使用这个类
    /// </summary>
    public abstract class BuffStringDataBase
    {
        internal string key;
        internal Type valueType;

        internal StringDataConverter.Converter matchedConverter;

        SaveFormater.DataChannel _bindedChannel;
        /// <summary>
        /// 链接的DataChannel，当设置channel的时候会自动将channel内的数据转换过来
        /// </summary>
        internal SaveFormater.DataChannel BindedChannel
        {
            get => _bindedChannel;
            set
            {
                _bindedChannel = value;
                ConvertToString();
            }
        }

        /// <summary>
        /// 数据的字符串形式
        /// </summary>
        internal string StringValue
        {
            get
            {
                if (_bindedChannel == null)
                    return "";
                return _bindedChannel.subChannels[2].Data;
            }
            set
            {
                if (_bindedChannel != null)
                    _bindedChannel.subChannels[2].Data = value;
            }
        }

        /// <summary>
        /// 装箱后数据，用途是可以统一放在DataManager里管理
        /// </summary>
        internal virtual object BoxedValue { get; set; }

        public BuffStringDataBase(string key, Type valueType)
        {
            this.key = key;
            this.valueType = valueType;

            matchedConverter = StringDataConverter.GetMatchedConverter(valueType);

            if (matchedConverter == null)
                throw new ArgumentException($"{valueType.FullName} is not supported");
        }

        internal virtual void ConvertToString()
        {
            StringValue = matchedConverter.ConvertToString(BoxedValue);
        }

        internal virtual void ConvertToValue()
        {
            if (String.IsNullOrEmpty(StringValue))
                return;
            BoxedValue = matchedConverter.ConvertToValue(StringValue);
        }
    }

    /// <summary>
    /// 与Buff存档链接的数据入口，可以用来简化保存数据的过程
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public sealed class BuffStringData<TData> : BuffStringDataBase
    {
        public TData Value { get; set; }

        internal override object BoxedValue
        {
            get => Value;
            set
            {
                Value = (TData)value;
            }
        }

        public BuffStringData(string key, TData defaultValue) : base(key, typeof(TData))
        {
            Value = defaultValue;
        }
    }
}
