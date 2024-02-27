#define BUFFDEBUG 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.SaveData;
using UnityEngine;


namespace RandomBuff.Core.Entry
{
    /// <summary>
    /// BuffData 中
    /// 若属性设置该attribute则会设置get函数获取静态配置
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CustomStaticConfigAttribute : Attribute
    {

    }

    public enum HookLifeTimeLevel
    {
        InGame,
        UntilQuit
    }



    /// <summary>
    /// 保存Buff的注册信息，负责保存BuffData/Buff的Type信息
    /// 不保存实际的data类型
    /// 不随存档变化刷新
    ///
    /// 外部接口
    /// </summary>
    public static partial class BuffRegister
    {

        /// <summary>
        /// 注册新的Buff，并且包含hook
        /// </summary>
        /// <typeparam name="TBuffType"></typeparam>
        /// <typeparam name="TDataType"></typeparam>
        /// <typeparam name="THookType"></typeparam>
        /// <param name="id"></param>
        public static void RegisterBuff<TBuffType, TDataType, THookType>(BuffID id) where TBuffType : IBuff, new()
            where TDataType : BuffData, new()
        {
            BuffHookWarpper.RegisterHook(id, typeof(THookType));
            RegisterBuff<TBuffType, TDataType>(id);
        }



        /// <summary>
        /// 注册新的buff，不包含hook
        /// </summary>
        /// <typeparam name="TBuffType"></typeparam>
        /// <typeparam name="TDataType"></typeparam>
        /// <param name="id"></param>
        public static void RegisterBuff<TBuffType, TDataType>(BuffID id) where TBuffType : IBuff, new()
            where TDataType : BuffData, new()
        {
            RegisterBuff(id,typeof(TBuffType),typeof(TDataType));
        }
        public static void RegisterBuff(BuffID id,Type buffType,Type dataType)
        {
            try
            {
                if (id != ((IBuff)Activator.CreateInstance(buffType)).ID ||
                    id != ((BuffData)(Activator.CreateInstance(dataType))).ID)
                {
                    BuffPlugin.LogError($"{id}'s Buff or BuffData has unexpected BuffID!");
                    return;
                }
                BuffTypes.Add(id, buffType);
                DataTypes.Add(id, dataType);
            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e,$"Exception when register buff {id}");
            }
      
        }

        /// <summary>
        /// 注册新的抽卡模式
        /// </summary>
        /// <typeparam name="TTemplateType"></typeparam>
        /// <param name="id"></param>
        public static void RegisterGachaTemplate<TTemplateType>(GachaTemplateID id)
            where TTemplateType : GachaTemplate, new()
        {
            try
            {
                if (id != Activator.CreateInstance<TTemplateType>().ID)
                {
                    BuffPlugin.LogError($"{id}'s GachaTemplate has unexpected GachaTemplateID!");
                    return;
                }

                TemplateTypes.Add(id,typeof(TTemplateType));
            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e, $"Exception when register GachaTemplate {id}");
            }
        }

        /// <summary>
        /// 注册新的通关条件
        /// </summary>
        /// <typeparam name="TConditionType"></typeparam>
        /// <param name="id"></param>
        /// <param name="displayName"></param>
        public static void RegisterCondition<TConditionType>(ConditionID id, string displayName)
            where TConditionType : Condition, new()
        {
            try
            {
                if (id != Activator.CreateInstance<TConditionType>().ID)
                {
                    BuffPlugin.LogError($"{id}'s GachaTemplate has unexpected ConditionID!");
                    return;
                }
                ConditionTypes.Add(id, typeof(TConditionType));
                ConditionNames.Add(id, displayName);
            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e, $"Exception when register condition {id}");
            }
        }
    }

    /// <summary>
    /// 保存Buff的注册信息，负责保存BuffData/Buff的Type信息
    /// 不保存实际的data类型
    /// 不随存档变化刷新
    /// </summary>
    public static partial class BuffRegister
    {
        internal static (BuffID id, Type type) GetDataType(string id) => (new BuffID(id),GetAnyType(new BuffID(id), DataTypes));

        internal static (BuffID id, Type type) GetBuffType(string id) => (new BuffID(id), GetAnyType(new BuffID(id), BuffTypes));

        internal static Type GetDataType(BuffID id) => GetAnyType(id, DataTypes);

        internal static Type GetBuffType(BuffID id) => GetAnyType(id, BuffTypes);

        internal static Type GetCondition(ConditionID id) => GetAnyType(id, ConditionTypes);

        internal static Type GetTemplate(GachaTemplateID id) => GetAnyType(id, TemplateTypes);


        private static Type GetAnyType<T>(T id, Dictionary<T, Type> dic)
        {
            if (dic.ContainsKey(id))
                return dic[id];
            return null;
        }


        internal static void InitAllBuffPlugin()
        {
            foreach (var mod in ModManager.ActiveMods)
            {
                string path = mod.path + Path.DirectorySeparatorChar + "buffplugins";
                if (!Directory.Exists(path))
                    continue;
                BuffPlugin.Log($"Find correct path in {mod.id} to load plugins");
                DirectoryInfo info = new DirectoryInfo(path);
                foreach (var file in info.GetFiles("*.dll"))
                {
                    var assembly = Assembly.LoadFile(file.FullName);
                    foreach (var type in assembly.GetTypes())
                    {

                        if (type.GetInterfaces().Contains(typeof(IBuffEntry)))
                        {
                            var obj = type.GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>());
                            try
                            {
                                type.GetMethod("OnEnable").Invoke(obj, Array.Empty<object>());
                                BuffPlugin.Log($"Invoke {type.Name}.OnEnable");
                            }
                            catch (Exception e)
                            {
                                BuffPlugin.LogException(e);
                                BuffPlugin.LogError($"Invoke {type.Name}.OnEnable Failed!");

                            }

                        }
                    }
                }
            }
        }


        /// <summary>
        /// 给BuffData设置config的warpper
        /// </summary>
        internal static void BuildAllDataStaticWarpper()
        {
            foreach (var dataType in DataTypes)
            {
                if (!BuffConfigManager.ContainsId(dataType.Key))
                {
                    BuffPlugin.LogError($"Can't find json data for ID :{dataType.Key}!");
                    continue;
                }
                foreach (var property in dataType.Value.GetProperties().
                             Where(i => i.GetCustomAttribute<CustomStaticConfigAttribute>() != null))
                {
                    //不存在get方法
                    if (property.GetGetMethod() == null)
                    {
                        BuffPlugin.LogError($"Property {property.Name} at Type {dataType.Value.Name} has no get method!");
                        continue;
                    }

                    //在json不存在属性
                    if (!BuffConfigManager.ContainsProperty(dataType.Key, property.Name))
                    {
                        BuffPlugin.LogWarning($"can't find custom property Named: {property.Name} At {dataType.Key} static data");
                        continue;
                    }

                    //有set属性
                    if (property.CanWrite)
                        BuffPlugin.LogWarning($"Property {property.Name} can write!");

                    
            
                    var hook = new ILHook(property.GetGetMethod(), (il) =>
                    {
                        il.Instrs.Clear();
                        il.Body.MaxStackSize += 1;
                        ILCursor c = new ILCursor(il);
                        c.Emit(OpCodes.Ldarg_0);
                        c.EmitDelegate<Func<BuffData, object>>((self) =>
                         self.GetConfig(property.Name, property.PropertyType));
                        if (property.PropertyType.IsValueType)
                            c.Emit(OpCodes.Unbox_Any, property.PropertyType);
                        else
                            c.Emit(OpCodes.Castclass, property.PropertyType);
                        c.Emit(OpCodes.Ret);
                    });

                    if (BuffPlugin.DevEnabled)
                        BuffPlugin.Log($"Warp config property for {dataType.Key} : {property.Name} : {property.PropertyType}");
                    
                }
            }
        }
        
        private static readonly Dictionary<BuffID, Type> DataTypes = new();
        private static readonly Dictionary<BuffID, Type> BuffTypes = new();
        private static readonly Dictionary<GachaTemplateID, Type> TemplateTypes = new();
        private static readonly Dictionary<ConditionID, Type> ConditionTypes = new();
        private static readonly Dictionary<ConditionID, string> ConditionNames = new();

    }
}
