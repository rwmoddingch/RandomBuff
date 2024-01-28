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
using RandomBuff.Core.SaveData;


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

    public interface IBuffHook
    {
        public void HookOn();
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
        public static void RegisterBuff<BuffType, DataType, HookType>(BuffID id) where BuffType : IBuff, new()
            where DataType : BuffData, new()
        {
            BuffTypes.Add(id, typeof(BuffType));
            DataTypes.Add(id, typeof(DataType));
            BuffHookWarpper.RegisterHook(id, typeof(HookType));
        }

        public static void RegisterBuff<BuffType, DataType>(BuffID id) where BuffType : IBuff, new()
            where DataType : BuffData, new()
        {
            BuffTypes.Add(id, typeof(BuffType));
            DataTypes.Add(id, typeof(DataType));
        }
    }

    /// <summary>
    /// 保存Buff的注册信息，负责保存BuffData/Buff的Type信息
    /// 不保存实际的data类型
    /// 不随存档变化刷新
    /// </summary>
    public static partial class BuffRegister
    {
        internal static (BuffID id, Type type) GetDataType(string id) => 
            (new BuffID(id),GetAnyType(new BuffID(id), DataTypes));

        internal static (BuffID id, Type type) GetBuffType(string id) => 
            (new BuffID(id), GetAnyType(new BuffID(id), BuffTypes));

        internal static Type GetDataType(BuffID id) => GetAnyType(id, DataTypes);

        internal static Type GetBuffType(BuffID id) => GetAnyType(id, BuffTypes);

        private static Type GetAnyType(BuffID id, Dictionary<BuffID, Type> dic)
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
                            type.GetMethod("OnEnable").Invoke(obj, Array.Empty<object>());
                            BuffPlugin.Log($"Invoke {type.Name}.OnEnable");
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
        
        private static Dictionary<BuffID, Type> DataTypes = new();
        private static Dictionary<BuffID, Type> BuffTypes = new();
    }
}
