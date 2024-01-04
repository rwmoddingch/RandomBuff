using RandomBuff.Core.StringData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.SaveData
{
    /// <summary>
    /// 管理DeathPersistenceData中所有的buffdata
    /// SaveFormater中的格式参见api文档
    /// </summary>
    internal sealed class BuffDataManager
    {
        public static BuffDataManager Singleton { get; private set; }

        internal SaveFormater managerFormater;
        internal SaveFormater buffDataFormater;

        internal Dictionary<string, BuffStringDataBase> bindedStringData = new Dictionary<string, BuffStringDataBase>();

        static BuffDataManager()
        {
            Singleton = new BuffDataManager();
        }

        internal void FromSave(string save)
        {
            managerFormater = new SaveFormater("buff", save, true);
            if (string.IsNullOrEmpty(save))
            {
                managerFormater.AppendNewChannel();//[buffEnabled]
                managerFormater.AppendNewChannel();//[buffToAdd]
                managerFormater.AppendNewChannel();//buff_data
            }

            buffDataFormater = new SaveFormater("buff_data", managerFormater.GetChannelAt(2).Data);

            foreach (var channel in buffDataFormater.channels)
            {
                if (bindedStringData.TryGetValue(channel.subChannels[0].Data, out var buffStringData))
                {
                    buffStringData.BindedChannel = channel;
                }
            }
        }
        internal string ToSave()
        {
            foreach (var stringData in bindedStringData.Values)
            {
                stringData.ConvertToString();
            }
            var builder = new StringBuilder();
            buffDataFormater.BuildSave(builder);
            return builder.ToString();
        }

        public BuffStringData<T> BindBuffData<T>(string key, T defaultValue)
        {
            if (bindedStringData.TryGetValue(key, out var result))
            {
                return result as BuffStringData<T>;
            }

            //创建新的数据条目，分配新的数据通道
            result = new BuffStringData<T>(key, defaultValue);

            bool findMatch = false;
            foreach (var channel in buffDataFormater.channels)
            {
                if (channel.subChannels[0].Data == key)
                {
                    if (channel.subChannels[1].Data != typeof(T).FullName)
                    {
                        BuffPlugin.Log($"string data {key} already exist, but not in same type, reset data");
                        channel.subChannels[2].Data = "";
                    }

                    result.BindedChannel = channel;
                    findMatch = true;
                }
            }

            if (!findMatch)
            {
                var newChannel = buffDataFormater.AppendNewChannel();
                newChannel.AppendNewChannel(key);
                newChannel.AppendNewChannel(typeof(T).FullName);
                newChannel.AppendNewChannel("");
                result.BindedChannel = newChannel;
            }


            bindedStringData.Add(key, result);
            return result as BuffStringData<T>;
        }
    }
}
