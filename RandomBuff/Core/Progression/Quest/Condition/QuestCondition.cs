using Newtonsoft.Json;
using RandomBuff.Core.Game;
using System.Collections.Generic;
using System;
namespace RandomBuff.Core.Progression.Quest.Condition
{


    /// <summary>
    /// 不要理会这个 偷懒拿名称用的
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public struct QuestConditionJsonGetType
    {
        [JsonProperty("Type")]
        public string TypeName;
    }

    /// <summary>
    /// Quest条件信息
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract partial class QuestCondition
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
        public abstract string ConditionMessage();


        /// <summary>
        /// 在结算时调用，返回更新后的任务解锁状态
        /// </summary>
        /// <returns></returns>
        public abstract bool UpdateUnlockedState(WinGamePackage package);

        /// <summary>
        /// 在初始化时调用，验证数据是否有效
        /// </summary>
        /// <returns></returns>
        public abstract bool VerifyData();

    }

    public abstract partial class QuestCondition
    {
        private static readonly Dictionary<string, Type> QuestConditionTypes = new();

        public static void Register<T>() where T : QuestCondition, new()
        {
            Register(typeof(T));
        }
        internal static void Register(Type type)
        {
            var inst = Helper.GetUninit<QuestCondition>(type);
            if (QuestConditionTypes.ContainsKey(inst.TypeName))
                BuffPlugin.LogError($"BuffQuests: same buff quest {inst.TypeName}.");
            else
                QuestConditionTypes.Add(inst.TypeName, type);

        }

        internal static void Init()
        {
            Register<LevelQuestCondition>();
            Register<MissionQuestCondition>();
            Register<CardQuestCondition>();
            Register<RunCountQuestCondition>();
            Register<CosmeticQuestCondition>();
            QuestUnlockedType.Init();
        }

        internal static bool TryGetType(string typeName, out Type type)
        {
            return QuestConditionTypes.TryGetValue(typeName, out type);
        }
    }
}
