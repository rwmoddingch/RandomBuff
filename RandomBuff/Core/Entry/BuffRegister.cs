#define BUFFDEBUG 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MonoMod.RuntimeDetour;
using RandomBuff.Core.Buff;


namespace RandomBuff.Core.Entry
{

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


        private static Dictionary<BuffID, Type> DataTypes = new();
        private static Dictionary<BuffID, Type> BuffTypes = new();
    }
}
