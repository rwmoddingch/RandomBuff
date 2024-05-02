using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;
using RWCustom;
using UnityEngine;

namespace RandomBuff.Core.Progression
{

    enum QuestUnlockedType
    {
        Card,
        Mission,
        Special
    }

    /// <summary>
    /// 不要理会这个 偷懒拿名称用的
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    struct BuffQuestJsonGetType
    {
        [JsonProperty("Type")]
        public string TypeName;
    }

    /// <summary>
    /// 储存Quest的信息，并负责实际判断，
    /// 但是不存存档变化，不储存数据（数据请到BuffPlayerData）
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    abstract partial class BuffQuest
    {
        /// <summary>
        /// 类型名称
        /// </summary>
        [JsonProperty("Type")]
        public abstract string TypeName { get; }

        /// <summary>
        /// 任务提示文本
        /// </summary>
        /// <returns></returns>
        public abstract string QuestMessage();


        /// <summary>
        /// 在结算时调用，返回更新后的任务解锁状态
        /// </summary>
        /// <returns></returns>
        public abstract bool UpdateUnlockedState(BuffPoolManager.WinGamePackage package);

        /// <summary>
        /// 在初始化时调用，验证数据是否有效
        /// </summary>
        /// <returns></returns>
        public abstract bool VerifyData();

        /// <summary>
        /// 任务名称
        /// </summary>
        public string QuestName => questName;

        /// <summary>
        /// 任务Id
        /// </summary>
        public string QuestId => id;

        /// <summary>
        /// 任务颜色
        /// </summary>
        public Color QuestColor => color;


        /// <summary>
        /// 已解锁物品
        /// </summary>
        public Dictionary<QuestUnlockedType, string[]> UnlockItem => unlockItem;

  


        [JsonProperty("Name")]
        protected string questName;

        [JsonProperty("Color")]
        protected string ColorString
        {
            set
            {
                try
                {
                    color = Custom.hexToColor(value);
                }
                catch (Exception _)
                {
                    BuffPlugin.LogError($"BuffQuest: Invalid color value: {value}, At BuffQuest named: {questName}");
                    color = Color.white;
                }
            }
        }
        [JsonProperty("ID")]
        protected string id;

        [JsonProperty("UnlockItem")]
        protected Dictionary<QuestUnlockedType, string[]> unlockItem;


        protected Color color;
    }

    abstract partial class BuffQuest
    {
        private static Dictionary<string, Type> buffQuestTypes = new();

        public static void Init()
        {
            buffQuestTypes.Add(nameof(LevelQuest),typeof(LevelQuest));
            buffQuestTypes.Add(nameof(MissionQuest), typeof(MissionQuest));
        }

        public static bool TryGetType(string typeName, out Type type)
        {
            return buffQuestTypes.TryGetValue(typeName, out type);
        }
    }
}
