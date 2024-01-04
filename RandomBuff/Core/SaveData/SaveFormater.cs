using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RandomBuff.Core.SaveData
{
    ///雨世界存档基本结构: <keyA>A层数据1
    ///                     <keyB>B层数据1-1
    ///                     <keyB>B层数据1-2
    ///                         <keyC>C层数据1-2-1
    ///                 <keyA>A层数据2
    ///                 <keyA>A层数据3
    ///                     <keyB>B层数据3-1
    ///                         <keyC>C层数据3-3-1
    /// 实际情况下不会出现换行符号                      
    /// 
    /// <summary>
    /// 存档数据构造器，与雨世界的存档文件结构相同
    /// 层级嵌套用<keyA><keyB><keyC>...来区分，一般来说深度不会超过C
    /// </summary>
    public sealed class SaveFormater
    {
        //static data
        internal static string depthString = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        internal readonly string key;

        public List<DataChannel> channels = new List<DataChannel>();

        /// <summary>
        /// 创建一个空的构造器，由用户自行指定数据
        /// </summary>
        /// <param name="key"></param>
        public SaveFormater(string key)
        {
            this.key = key;
        }

        /// <summary>
        /// 从现有的存档数据构造一个构造器
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public SaveFormater(string key, string dataString, bool ignoreEmpty = false) : this(key)
        {
            if (string.IsNullOrEmpty(dataString))
                return;

            var topChannelDatas = Regex.Split(dataString, $"<{key}A>");
            for (int i = 0; i < topChannelDatas.Length; i++)
            {
                if (!String.IsNullOrEmpty(topChannelDatas[i]) || ignoreEmpty)
                {
                    Console.WriteLine($"Build channel for:{topChannelDatas[i]}");
                    channels.Add(new DataChannel(this, null, topChannelDatas[i]));
                }
            }
        }

        /// <summary>
        /// 构造存档字符串
        /// </summary>
        /// <param name="stringBuilder"></param>
        public void BuildSave(StringBuilder stringBuilder)
        {
            foreach (var channel in channels)
            {
                channel.BuildSave(stringBuilder);
            }
        }

        #region Channel Management
        /// <summary>
        /// 添加新通道
        /// </summary>
        /// <param name="dataChannel"></param>
        public void AppendChannel(DataChannel dataChannel)
        {
            channels.Add(dataChannel);
        }

        public DataChannel AppendNewChannel()
        {
            var channel = new DataChannel(this, null);
            AppendChannel(channel);
            return channel;
        }

        /// <summary>
        /// 插入新通道
        /// </summary>
        /// <param name="dataChannel"></param>
        /// <param name="index"></param>
        public void InsertChannel(DataChannel dataChannel, int index)
        {
            channels.Insert(index, dataChannel);
        }

        public DataChannel InsertNewChannel(int index)
        {
            var channel = new DataChannel(this, null);
            InsertChannel(channel, index);
            return channel;
        }

        /// <summary>
        /// 移除通道
        /// </summary>
        /// <param name="index"></param>
        public void RemoveChannel(int index)
        {
            var channelToRemove = channels[index];
            channels.RemoveAt(index);
        }

        /// <summary>
        /// 获取指定data的通道
        /// </summary>
        /// <param name="data"></param>
        /// <returns>返回值可能为null</returns>
        public DataChannel GetChannelOfData(string data)
        {
            foreach (var channel in channels)
            {
                if (channel.Data == data)
                    return channel;
            }
            return null;
        }

        public DataChannel GetChannelAt(int index)
        {
            return channels[index];
        }
        #endregion

        /// <summary>
        /// 获取指定深度的所有通道
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        public IEnumerable<DataChannel> GetAllChannelsAtDepth(int depth)
        {
            foreach (var channel in channels)
            {
                foreach (var subChannel in channel.GetAllSubChannels())
                {
                    if (subChannel.Depth == depth)
                        yield return subChannel;
                }
            }
        }

        public override string ToString()
        {
            string result = "";
            foreach (var channel in channels)
            {
                result += channel.ToString();
            }
            return result;
        }

        /// <summary>
        /// 单个数据层
        /// </summary>
        public class DataChannel
        {
            internal SaveFormater formatBuilder;
            internal DataChannel parentChannel;

            string data = "";
            public string Data
            {
                get => data;
                set => data = value;
            }
            public List<DataChannel> subChannels = new List<DataChannel>();
            public int Depth => parentChannel == null ? 0 : parentChannel.Depth + 1;

            internal char DepthChar => depthString[Depth];
            internal char NextDepthChar => depthString[Depth + 1];

            /// <summary>
            /// 创建一个空的数据层
            /// </summary>
            /// <param name="formatBuilder"></param>
            /// <param name="parent"></param>
            /// <param name="insertAt">位于父通道中的位置</param>
            public DataChannel(SaveFormater formatBuilder, DataChannel parent, int insertAt = -1)
            {
                this.formatBuilder = formatBuilder;
                this.parentChannel = parent;

                if (insertAt == -1)
                    parentChannel?.subChannels.Add(this);
                else
                    parentChannel?.subChannels.Insert(insertAt, this);
            }

            /// <summary>
            /// 从已有数据创造数据层
            /// </summary>
            /// <param name="formatBuilder"></param>
            /// <param name="parentChannel"></param>
            /// <param name="dataString"></param>
            public DataChannel(SaveFormater formatBuilder, DataChannel parentChannel, string dataString) : this(formatBuilder, parentChannel)
            {
                var nextChannelDatas = Regex.Split(dataString, $"<{formatBuilder.key}{NextDepthChar}>");
                if (nextChannelDatas.Length > 0 && !string.IsNullOrEmpty(nextChannelDatas[0]))
                {
                    Data = nextChannelDatas[0];
                }
                for (int i = 1; i < nextChannelDatas.Length; i++)
                {
                    new DataChannel(formatBuilder, this, nextChannelDatas[i]);
                }
            }

            /// <summary>
            /// 构造存档字符串
            /// </summary>
            /// <param name="stringBuilder"></param>
            internal void BuildSave(StringBuilder stringBuilder)
            {
                stringBuilder.Append($"<{formatBuilder.key}{DepthChar}>");
                stringBuilder.Append(Data);

                foreach (var channel in subChannels)
                {
                    channel.BuildSave(stringBuilder);
                }
            }

            /// <summary>
            /// 在末尾追加新的数据
            /// </summary>
            /// <param name="data"></param>
            public DataChannel AppendNewChannel(string data)
            {
                var newChannel = new DataChannel(formatBuilder, this);
                newChannel.Data = data;
                return newChannel;
            }

            /// <summary>
            /// 在指定位置插入新的数据
            /// </summary>
            /// <param name="data"></param>
            /// <param name="index"></param>
            public DataChannel InsertNewChannel(string data, int index)
            {
                var newChannel = new DataChannel(formatBuilder, this, index);
                newChannel.Data = data;
                return newChannel;
            }

            /// <summary>
            /// 移除位于指定索引位置的数据（无越界检查）
            /// </summary>
            /// <param name="index"></param>
            public void RemoveAt(int index)
            {
                subChannels.RemoveAt(index);
            }

            /// <summary>
            /// 获取拥有指定data的子通道（仅包含下一深度）
            /// </summary>
            /// <param name="data"></param>
            /// <returns>返回值可能为null</returns>
            public DataChannel GetSubChannelOfData(string data)
            {
                foreach (var channel in subChannels)
                {
                    if (channel.Data == data)
                    {
                        return channel;
                    }
                }
                return null;
            }

            /// <summary>
            /// 获取所有的子通道（包括所有的深度）
            /// </summary>
            /// <returns></returns>
            public IEnumerable<DataChannel> GetAllSubChannels()
            {
                foreach (var subChannel in subChannels)
                {
                    foreach (var subsubChannel in subChannel.GetAllSubChannels())
                    {
                        yield return subsubChannel;
                    }
                    yield return subChannel;
                }
            }

            //debug用方法
            public override string ToString()
            {
                string result = $"{new string(' ', Depth * 4)}<{formatBuilder.key}{DepthChar}>\n";
                if (!String.IsNullOrEmpty(data))
                    result += $"{new string(' ', Depth * 4 + 4)}{data}\n";
                foreach (var channel in subChannels)
                {
                    result += channel.ToString();
                }
                return result;
            }
        }

        public interface IChannelDataConverter
        {
            string ConvertToString();
            void ConvertToData(string dataString);
        }
    }
}
