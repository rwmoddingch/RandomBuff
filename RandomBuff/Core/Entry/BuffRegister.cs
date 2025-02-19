#define BUFFDEBUG 

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Mono.Cecil.Rocks;
using MonoMod.Cil;
using MonoMod.Utils;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuff.Core.Progression;
using RandomBuff.Core.Progression.Quest.Condition;
using RandomBuff.Core.SaveData;
using RandomBuff.Core.SaveData.BuffConfig;
using RandomBuffUtils;
using UnityEngine;
using static Rewired.Utils.Classes.Data.TypeWrapper;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using PropertyAttributes = Mono.Cecil.PropertyAttributes;


namespace RandomBuff.Core.Entry
{


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
            BuffHookWarpper.RegisterBuffHook(id, typeof(THookType));
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
            InternalRegisterBuff(id,typeof(TBuffType),typeof(TDataType));
        }




        /// <summary>
        /// 无Buff类型注册Buff
        /// </summary>
        /// <typeparam name="THookType"></typeparam>
        /// <param name="id"></param>
        public static void RegisterBuff<THookType>(BuffID id)
        {
            BuffHookWarpper.RegisterBuffHook(id, typeof(THookType));

            //这里不得不提前注册，否则就需要BuffHookWarpper公开更多信息，我暂时没有好想法
            BuffConfigManager.AddBuffAssemblyBind(id,typeof(THookType).Assembly.GetName());

            if (CurrentPluginId == string.Empty)
            {
                BuffPlugin.LogError("Missing Mod ID!, can't use this out of IBuffEntry.OnEnable");
                return;
            }
            if (BuffTypes.ContainsKey(id) || CurrentRuntimeBuffName.Contains(id.value))
            {
                BuffPlugin.LogError($"{id} has already registered!");
                return;
            }
            try
            {
                BuffBuilder.GenerateBuffTypeWithCache(CurrentPluginId, id.value);
                CurrentRuntimeBuffName.Add(id.value);
            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e, $"Exception in GenerateBuffType:{CurrentPluginId}:{id}");
            }
        
        }


        /// <summary>
        /// 唯一一个指定的BuffTypes添加方法
        /// </summary>
        /// <param name="id"></param>
        /// <param name="buffType"></param>
        /// <param name="dataType"></param>
        /// <param name="hookType">几乎不在这里注册</param>
        /// <param name="originAssemblyName">原始的dll名称</param>
        internal static void InternalRegisterBuff(BuffID id,Type buffType,Type dataType,Type hookType = null, AssemblyName originAssemblyName = null)
        {
            try
            {
                if (BuffTypes.ContainsKey(id))
                {
                    BuffPlugin.LogError($"{id} has already registered!");
                    return;
                }


                if (id != Helper.GetUninit<IBuff>(buffType).ID || id != Helper.GetUninit<BuffData>(dataType).ID)
                {
                    BuffPlugin.LogError($"{id}'s Buff or BuffData has unexpected BuffID!");
                    return;
                }
                if(hookType != null)
                    BuffHookWarpper.RegisterBuffHook(id, hookType);

                originAssemblyName ??= buffType.Assembly.GetName();
                BuffTypes.Add(id, buffType);
                DataTypes.Add(id, dataType);


                //注意仅注册HookType的buff提前进行了绑定
                BuffConfigManager.AddBuffAssemblyBind(id, originAssemblyName);

            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e,$"Exception when register buff {id}");
            }
      
        }
        /// <summary>
        /// 注册新的任务种类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void RegisterQuestType<T>() where T : QuestCondition, new()
        {
            QuestCondition.Register<T>();
        }

        /// <summary>
        /// 注册新的装饰解锁要素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void RegisterCosmeticUnlock<T>() where T : CosmeticUnlock, new()
        {
            CosmeticUnlock.Register<T>();
        }

        /// <summary>
        /// 注册新的抽卡模式
        /// </summary>
        /// <typeparam name="TTemplateType"></typeparam>
        /// <param name="id">ID</param>
        /// <param name="banList">ban掉的条件</param>
        public static void RegisterGachaTemplate<TTemplateType>(GachaTemplateID id, params ConditionID[] banList)
            where TTemplateType : GachaTemplate, new()
        {
            try
            {
                if (id != Helper.GetUninit<TTemplateType>().ID)
                {
                    BuffPlugin.LogError($"{id}'s GachaTemplate has unexpected GachaTemplateID!");
                    return;
                }

                TemplateTypes.Add(id,new GachaTemplateType(
                    typeof(TTemplateType), new HashSet<ConditionID>(banList)));
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
        /// <param name="id">ID</param>
        /// <param name="displayName">显示类别名称</param>
        /// <param name="parentId">继承某个条件的Ban情况</param>
        /// <param name="banList">Ban掉特定的游玩模式，请保证对应的GachaTemplate已经注册</param>
        public static void RegisterCondition<TConditionType>(ConditionID id, string displayName,
            ConditionID parentId = null,params GachaTemplateID[] banList)
            where TConditionType : Condition, new()
        {
           
            try
            {
                if (id != Helper.GetUninit<TConditionType>().ID)
                {
                    BuffPlugin.LogError($"{id}'s Condition has unexpected ConditionID!");
                    return;
                }

                var parent = GetConditionType(parentId);
                if (parent == null && parentId != null)
                {
                    BuffPlugin.LogError($"can't find Condition:{parentId}, When register Condition:{id} parent");
                    return;
                }
                ConditionTypes.Add(id, new ConditionType(id,typeof(TConditionType), GetConditionType(parentId), displayName));
                foreach (var banId in banList)
                {
                    var type = GetTemplateType(banId);
                    if (type == null)
                    {
                        BuffPlugin.LogError($"can't find GachaTemplate:{banId}, When register Condition:{id} ban list");
                        continue;
                    }
                    if (!type.BanConditionIds.Contains(id))
                        type.BanConditionIds.Add(id);
                }
                BuffHookWarpper.RegisterConditionHook<TConditionType>();

            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e, $"Exception when register condition {id}");
            }
        }

        /// <summary>
        /// 注册新的通关条件
        /// </summary>
        /// <typeparam name="TConditionType"></typeparam>
        /// <param name="id">ID</param>
        /// <param name="displayName">显示类别名称</param>
        /// <param name="isHidden">是否为隐藏条件（不可随机抽取）</param>
        /// <param name="parentId">继承某个条件的Ban情况</param>
        public static void RegisterCondition<TConditionType>(ConditionID id, string displayName,bool isHidden,
            ConditionID parentId = null)
            where TConditionType : Condition, new()
        {
            try
            {
                if (id != Helper.GetUninit<TConditionType>().ID)
                {
                    BuffPlugin.LogError($"{id}'s Condition has unexpected ConditionID!");
                    return;
                }

                var parent = GetConditionType(parentId);
                if (parent == null && parentId != null)
                {
                    BuffPlugin.LogError($"can't find Condition:{parentId}, When register Condition:{id} parent");
                    return;
                }
                ConditionTypes.Add(id, new ConditionType(id, typeof(TConditionType), GetConditionType(parentId), displayName,isHidden));
                BuffHookWarpper.RegisterConditionHook<TConditionType>();
            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e, $"Exception when register condition {id}");
            }
        }

        /// <summary>
        /// 注册新的使命
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="mission"></param>
        public static void RegisterMission(MissionID ID, Mission mission)
        {
            MissionRegister.RegisterMission(ID,mission);
        }

        /// <summary>
        /// 一般不会使用，手动添加staticData
        /// </summary>
        /// <param name="data"></param>
        public static void RegisterStaticData([NotNull] BuffStaticData data)
        {
            var staticDatas =
                (Dictionary<BuffID, BuffStaticData>)typeof(BuffConfigManager).GetField("StaticDatas", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            if (!staticDatas.ContainsKey(data.BuffID))
            {
                BuffUtils.Log("BuffExtend", $"Register Buff:{data.BuffID} static data by Code ");
                staticDatas.Add(data.BuffID, data);
                BuffConfigManager.BuffTypeTable[data.BuffType].Add(data.BuffID);
            }
            else
                BuffUtils.Log("BuffExtend", $"already contains BuffID {data.BuffID}");
        }
    }



    /// <summary>
    /// 保存Buff的注册信息，负责保存BuffData/Buff的Type信息
    /// 不保存实际的data类型
    /// 不随存档变化刷新
    /// </summary>
    public static partial class BuffRegister
    {

        /// <summary>
        /// 读取时使用的临时资源
        /// </summary>
        #region LoadTempValue

        internal static string CurrentPluginId { get; private set; } = string.Empty;

        private static readonly List<string> CurrentRuntimeBuffName = new();

        private static readonly List<Type> NeedLoadAssetEntries = new();

        internal static readonly List<Assembly> AllBuffAssemblies = new();


        #endregion

        internal static (BuffID id, Type type) GetDataType(string id) => (new BuffID(id),GetAnyType(new BuffID(id), DataTypes));
        internal static (BuffID id, Type type) GetBuffType(string id) => (new BuffID(id),GetAnyType(new BuffID(id), BuffTypes));

        internal static Type GetDataType(BuffID id) => GetAnyType(id, DataTypes);
        internal static Type GetBuffType(BuffID id) => GetAnyType(id, BuffTypes);

        internal static ConditionType GetConditionType(ConditionID id)
        {
            if(id == null)
                return null;
            return GetAnyType(id, ConditionTypes);
        }
        internal static string GetConditionTypeName(ConditionID id) => GetAnyType(id, ConditionTypes).DisplayName;
        internal static List<ConditionID> GetAllConditionList() => ConditionTypes.Keys.ToList();

        internal static GachaTemplateType GetTemplateType(GachaTemplateID id) => GetAnyType(id, TemplateTypes);


        private static Y GetAnyType<T,Y>(T id, Dictionary<T, Y> dic)
        {
            if (dic.ContainsKey(id))
                return dic[id];
            return default;
        }



        internal static void CleanAll()
        {
            DataTypes.Clear();
            BuffTypes.Clear();
            ConditionTypes.Clear();
            TemplateTypes.Clear();
        }

        internal static void CheckAndRemoveInvalidBuff()
        {

            foreach (var type in BuffID.values.entries.ToArray().Select(i => new BuffID(i)))
            {
                if(type == BuffID.None)
                    continue;
                if (!BuffTypes.ContainsKey(type) || !DataTypes.ContainsKey(type))
                {
                    BuffPlugin.LogError($"UnRegister buff: {type}");
                    BuffTypes.Remove(type);
                    DataTypes.Remove(type);
                    type.Unregister();
                    return;
                }

                if (!BuffConfigManager.ContainsId(type))
                {
                    BuffPlugin.LogError($"Can't find json data for ID：{type}!, UnRegister Buff");
                    BuffTypes.Remove(type);
                    DataTypes.Remove(type);
                    type.Unregister();
                }

            }
        }

        /// <summary>
        /// 读取全部Buff信息
        /// </summary>
        internal static void InitAllBuffPlugin()
        {
            NeedLoadAssetEntries.Clear();
            AllBuffAssemblies.Clear();
            HashSet<string> refLocations = null;
            var entryType = typeof(IBuffEntry);


            foreach (var mod in ModManager.ActiveMods)
            {
                string path = mod.path + Path.DirectorySeparatorChar + "buffplugins";
                if (!Directory.Exists(path))
                    continue;
                BuffPlugin.Log($"Find correct path in {mod.id} to load plugins");
                DirectoryInfo info = new DirectoryInfo(path);
                
                foreach (var file in info.GetFiles("*.dll"))
                {
                    (Assembly assembly, bool needLoadAsset) = CheckAndUpdateBuffPlugin(mod, file);

                    //代表没启用
                    if (assembly == null)
                    {
                        BuffPlugin.LogWarning($"Skip load {file} because Disabled");
                        continue;
                    }
                    AllBuffAssemblies.Add(assembly);
                    CurrentPluginId = assembly.GetName().Name;
                    BuffPlugin.Log(
                        $"load buff plugin, ID:{CurrentPluginId}, Name:{BuffConfigManager.GetPluginInfo(CurrentPluginId).GetInfo(InGameTranslator.LanguageID.English).Name}");
                    foreach (var type in assembly.GetTypes())
                    {
                        
                        if (entryType.IsAssignableFrom(type))
                        {
                            var obj = Helper.GetUninit<IBuffEntry>(type);
                            try
                            {
                                obj.OnEnable();
                            }
                            catch (Exception e)
                            {
                                BuffPlugin.LogException(e);
                                BuffPlugin.LogError($"Invoke {type.Name}.OnEnable Failed!");
                            }
                            if(needLoadAsset)
                                NeedLoadAssetEntries.Add(type);
                        }
                        
                    }


                    try
                    {
                        var runtimeAss = BuffBuilder.FinishGenerate(CurrentPluginId);
                        foreach (var ass in runtimeAss)
                        {
                            for (int i = CurrentRuntimeBuffName.Count - 1; i >= 0; i--)
                            {
                                var name = CurrentRuntimeBuffName[i];
                                if (ass.GetType($"{CurrentPluginId}.{name}Buff") is { } type)
                                {
                                    InternalRegisterBuff(new BuffID(name),
                                        type, ass.GetType($"{CurrentPluginId}.{name}BuffData", true));
                                }
                                CurrentRuntimeBuffName.Remove(name);
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        BuffPlugin.LogException(e, $"Exception when load {mod.id}:{assembly.GetName().Name}'s RuntimeBuff");
                    }
                    CurrentRuntimeBuffName.Clear();
                }

            }

            CurrentPluginId = string.Empty;
            BuffBuilder.CleanAllDatas();

            #region Wawa

            var somePath = Path.Combine(ModManager.ActiveMods.First(i => i.id == BuffPlugin.ModId).basePath, "buffassets",
                "assetbundles", "extend");
            if (Directory.Exists(somePath))
            {
                DirectoryInfo info = new DirectoryInfo(somePath);
                foreach (var file in info.GetFiles())
                {
                    try
                    {
                        Assembly.LoadFrom(file.FullName);
                    }
                    catch (Exception e)
                    {
                        BuffPlugin.LogException(e);
                    }
                }
            }

            #endregion

            (Assembly Assembly,bool isNewLoad) CheckAndUpdateBuffPlugin(ModManager.Mod mod, FileInfo file)
            {
                var assemblyDef = AssemblyDefinition.ReadAssembly(file.FullName);
                var assemblyName = assemblyDef.Name.Name;
                assemblyDef.Dispose();
                if (!BuffPlugin.IsPluginsEnabled(assemblyName))
                {
                    BuffConfigManager.GetPluginInfo(assemblyName);
                    return (null, true);
                }

                if (BuffConfigManager.GetPluginInfo(assemblyName).codeAssembly is { } assembly)
                {
                    BuffPlugin.LogDebug($"Has load assembly for {assembly}");
                    return (assembly, false);
                }

                if (!File.Exists(Path.Combine(BuffPlugin.CacheFolder,
                        $"{mod.id}_{Path.GetFileNameWithoutExtension(file.Name)}_codeCache.dll")) ||
                    GetTrulyWriteTime(file.FullName) > new FileInfo(Path.Combine(BuffPlugin.CacheFolder,
                        $"{mod.id}_{Path.GetFileNameWithoutExtension(file.Name)}_codeCache.dll")).LastWriteTime)
                {
                    File.Delete($"{mod.id}_{Path.GetFileNameWithoutExtension(file.Name)}_dynamicCache.dll");
                    File.Delete($"{mod.id}_{Path.GetFileNameWithoutExtension(file.Name)}_dataCache.dll");

                    if (refLocations == null)
                    {
                        refLocations = new HashSet<string>();
                        foreach (var refAssembly in typeof(BuffPlugin).Assembly.GetReferencedAssemblies())
                            refLocations.Add(
                                Path.GetDirectoryName(AppDomain.CurrentDomain.GetAssemblies()
                                    .First(i => i.GetName().Name == refAssembly.Name).Location));

                    }

                    var def = BuildCachePlugin(mod, file.FullName, refLocations, out var hasPdb);
                    def.Write(
                        Path.Combine(BuffPlugin.CacheFolder,
                            $"{mod.id}_{Path.GetFileNameWithoutExtension(file.Name)}_codeCache.dll"),
                        new WriterParameters() { WriteSymbols = hasPdb });

                }

                return (BuffConfigManager.GetPluginInfo(assemblyName).codeAssembly = Assembly.LoadFile(
                    Path.Combine(BuffPlugin.CacheFolder,
                        $"{mod.id}_{Path.GetFileNameWithoutExtension(file.Name)}_codeCache.dll")), true);
            }
        }


        /// <summary>
        /// 读取全部buff资源
        /// 注意重复加载忽略
        /// </summary>
        internal static void LoadBuffPluginAsset()
        {
            foreach (var type in NeedLoadAssetEntries)
            {
                if (type.GetMethod("LoadAssets", BindingFlags.Static | BindingFlags.Public) is { } method &&
                    method.GetParameters().Length == 0)
                {
                    try
                    {
                        method.Invoke(null, null);

                    }
                    catch (Exception e)
                    {
                        BuffPlugin.LogException(e, $"{type.Name}.LoadAssets execute Failed!");
                    }

                    BuffPlugin.LogDebug($"Load Assets for {type.Name}");
                }

            }
            NeedLoadAssetEntries.Clear();
        }


        /// <summary>
        /// 给BuffData设置config的warpper
        /// </summary>
        internal static void BuildAllBuffConfigWarpper()
        {
            foreach (var dataType in DataTypes)
            {
                foreach (var property in dataType.Value.GetProperties().
                             Where(i => i.GetCustomAttribute<CustomBuffConfigAttribute>(true) != null))
                {
            
                    //读取特性
                    var configAttribute = property.GetCustomAttribute<CustomBuffConfigAttribute>();
                    var infoAttribute = property.GetCustomAttribute<CustomBuffConfigInfoAttribute>();//可为null
                    var result = BuffConfigurableManager.TryGetConfigurable(dataType.Key, property.Name, true, property.PropertyType, configAttribute.defaultValue);

                    if (result.createNew)
                    {
                        var bindConfigurable = result.configurable;

                        bindConfigurable.acceptable = BuffConfigurableManager.GetProperAcceptable(configAttribute);

                        if (infoAttribute != null)
                        {
                            bindConfigurable.name = infoAttribute.name;
                            bindConfigurable.description = infoAttribute.description;
                        }
                        else
                        {
                            bindConfigurable.name = property.Name;
                            bindConfigurable.description = "";
                        }
                        BuffPlugin.LogDebug($"New configurable name : {bindConfigurable.name}, description : {bindConfigurable.description}");
                    }
                }
            }
        }

        public class ConditionType
        {
            public Type Type { get; }
            public ConditionType Parent { get; }
            public string DisplayName { get; }

            public bool IsHidden { get; }

            public ConditionID Id { get; }

            public ConditionType(ConditionID id, Type type, ConditionType parent, string displayName,bool isHidden = false)
            {
                Id = id;
                Type = type;
                Parent = parent;
                DisplayName = displayName;
                IsHidden = isHidden;
            }

            public bool CanUseInCurrentTemplate(GachaTemplateID id)
            {
                if (IsHidden)
                    return false;
                var type = GetTemplateType(id);
                var con = this;
                while (con != null)
                {
                    if (type.BanConditionIds.Contains(con.Id))
                        return false;
                    con = con.Parent;
                }

                return true;
            }
        }

        public class GachaTemplateType
        {
            public Type Type { get; }
            public HashSet<ConditionID> BanConditionIds { get; }

            public GachaTemplateType(Type type, HashSet<ConditionID> banConditionIds)
            {
                Type = type;
                BanConditionIds = banConditionIds;
            }

            public GachaTemplateType(Type type)
            {
                Type = type;
                BanConditionIds = new HashSet<ConditionID>();
            }
        }

        private static readonly Dictionary<BuffID, Type> DataTypes = new();
        private static readonly Dictionary<BuffID, Type> BuffTypes = new();

        private static readonly Dictionary<GachaTemplateID, GachaTemplateType> TemplateTypes = new();
        private static readonly Dictionary<ConditionID, ConditionType> ConditionTypes = new();

    }

    public partial class BuffRegister
    {
        private static AssemblyDefinition BuildCachePlugin(ModManager.Mod mod,string filePath, HashSet<string> refLocations, out bool hasPdb)
        {
            hasPdb = File.Exists(filePath.Replace(".dll", ".pdb"));
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(ModManager.ActiveMods.First(i => i.id == BuffPlugin.ModId).path + "/plugins");
            foreach (var modPath in ModManager.ActiveMods.Where(i => mod.requirements.Contains(i.id)
                                                                     && i.requirements.Contains(BuffPlugin.ModId)))
            {
                resolver.AddSearchDirectory(modPath.path + "/plugins");
                resolver.AddSearchDirectory(modPath.path + "/buffplugins");
            }
            foreach (var location in refLocations)
                resolver.AddSearchDirectory(location);

            AssemblyDefinition assemblyDef = AssemblyDefinition.ReadAssembly(filePath,
                new ReaderParameters
                    { ReadSymbols = File.Exists(filePath.Replace(".dll", ".pdb")), AssemblyResolver = resolver });
            var attrCtor =
                assemblyDef.MainModule.ImportReference(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes));

            foreach (var data in assemblyDef.MainModule.GetAllTypes().Where(i => i.IsSubtypeOf(typeof(BuffData))))
            {
        
                foreach (var property in GetAllCustomPropertyDefinitions(data))
                {
                    var trulyProperty = property;
                    bool isVirtual = false;
                    if (property.DeclaringType != data)
                    {
                        isVirtual = true;
                        data.Properties.Add(trulyProperty = new PropertyDefinition(trulyProperty.Name, trulyProperty.Attributes, data.Module.ImportReference(trulyProperty.PropertyType)));
                        trulyProperty.CustomAttributes.Add(new CustomAttribute(attrCtor));
                        if (property.SetMethod != null)
                        {
                            var method = new MethodDefinition($"set_{trulyProperty.Name}",
                                MethodAttributes.SpecialName | MethodAttributes.HideBySig  |
                                (property.SetMethod.IsPublic  ? MethodAttributes.Public : 0) | MethodAttributes.Virtual, data.Module.TypeSystem.Void);
                            method.Parameters.Add(new ParameterDefinition(trulyProperty.PropertyType));
                            data.Methods.Add(method);
                            trulyProperty.SetMethod = method;
                            method.CustomAttributes.Add(new CustomAttribute(attrCtor));

                        }
                    }

                    BuffPlugin.Log($"Find Property {data.Name}:{trulyProperty.Name}");
                    if (trulyProperty.SetMethod != null)
                    {
                        trulyProperty.SetMethod.Body.Instructions.Clear();
                        var il = trulyProperty.SetMethod.Body.GetILProcessor();
                        il.Emit(OpCodes.Ldstr, "Custom Buff Config");
                        il.Emit(OpCodes.Ldstr, $"Try Access {data.Name}.{trulyProperty.Name}");
                        il.Emit(OpCodes.Call, typeof(BuffUtils).GetMethod(nameof(BuffUtils.LogError)));
                        il.Emit(OpCodes.Ret);
                    }

                    if (trulyProperty.GetMethod == null)
                    {
                       
                        var method = new MethodDefinition($"get_{trulyProperty.Name}",
                            MethodAttributes.SpecialName | 
                            (property.GetMethod != null ? MethodAttributes.HideBySig : MethodAttributes.NewSlot) | 
                            ((property.GetMethod?.IsPublic ?? false) ? MethodAttributes.Public : 0) |
                            (isVirtual ? MethodAttributes.Virtual : 0), trulyProperty.PropertyType);
                        data.Methods.Add(method);

                        trulyProperty.GetMethod = method;
                        method.CustomAttributes.Add(new CustomAttribute(attrCtor));

                    }
                    trulyProperty.GetMethod!.Body.Instructions.Clear();
                    var getIl = trulyProperty.GetMethod.Body.GetILProcessor();
                    getIl.Emit(OpCodes.Ldarg_0);
                    getIl.Emit(OpCodes.Ldstr, trulyProperty.Name);
                    getIl.Emit(OpCodes.Call, typeof(BuffData).GetMethod(nameof(BuffData.GetConfigurableValue),
                        BindingFlags.NonPublic | BindingFlags.Instance));
                    getIl.Emit(trulyProperty.PropertyType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, trulyProperty.PropertyType);
                    getIl.Emit(OpCodes.Ret);
                    BuffPlugin.LogDebug($"Warp config property for {data.Name} : {trulyProperty.Name} : {trulyProperty.PropertyType}");
                }
            }

            IEnumerable<PropertyDefinition> GetAllCustomPropertyDefinitions(TypeDefinition type ,bool mustVirtual = false)
            {
                if (type == null || type.Name == "BuffData")
                    return new List<PropertyDefinition>();
                return GetAllCustomPropertyDefinitions(type.BaseType.SafeResolve(), true).Concat(type.Properties.Where(
                    i =>
                        i.CustomAttributes.Any(attr =>
                            attr.AttributeType.Resolve().IsSubtypeOf(typeof(CustomBuffConfigAttribute)))
                        && (!mustVirtual || (i.GetMethod?.IsVirtual ?? false))));


            }

            HashSet<TypeDefinition> hookTypeDef = new HashSet<TypeDefinition>();
            foreach (var enable in assemblyDef.MainModule.GetAllTypes().Where(i => i.HasInterface<IBuffEntry>()).Select(i => i.FindMethod("OnEnable")))
            {
                WarpperMethod(enable);
            }

            foreach(var hookType in hookTypeDef)
                BuffHookWarpper.BuildStaticHook(hookType);
            return assemblyDef;

            void WarpperMethod(MethodDefinition enable)
            {
                var instructions = enable.Body.Instructions;
                foreach (var instr in instructions)
                {
                    if (instr.MatchCallOrCallvirt(out var method))
                    {

                        if (method.FullName.Contains("RegisterBuff") &&
                            method.DeclaringType.Name == "BuffRegister" &&
                            method is GenericInstanceMethod genericInstance &&
                            genericInstance.GenericArguments.Count is not 0 or 2)
                        {
                            if (genericInstance.GenericArguments.Last().SafeResolve() is { } def)
                                hookTypeDef.Add(def);
                        }
                        else if (method.DeclaringType.Module == enable.Module &&
                                 method.SafeResolve() is { IsStatic: true ,HasBody:true} checkModule)
                        {
                            WarpperMethod(checkModule);
                        }
                    }
                }
            }
        }


        internal static DateTime GetTrulyWriteTime(string path)
        {
            var attr = File.GetAttributes(path);
            if ((attr & FileAttributes.ReparsePoint) != 0)
                return new FileInfo(NativeMethods.GetFinalPathName(path)).LastWriteTime;
            return new FileInfo(path).LastWriteTime;
        }


        private static class NativeMethods
        {
            private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            private const uint FILE_READ_EA = 0x0008;
            private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;

            [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            static extern uint GetFinalPathNameByHandle(IntPtr hFile, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr CreateFile(
                [MarshalAs(UnmanagedType.LPTStr)] string filename,
                [MarshalAs(UnmanagedType.U4)] uint access,
                [MarshalAs(UnmanagedType.U4)] FileShare share,
                IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
                [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
                [MarshalAs(UnmanagedType.U4)] uint flagsAndAttributes,
                IntPtr templateFile);

            public static string GetFinalPathName(string path)
            {
                var h = CreateFile(path,
                    FILE_READ_EA,
                    FileShare.ReadWrite | FileShare.Delete,
                    IntPtr.Zero,
                    FileMode.Open,
                    FILE_FLAG_BACKUP_SEMANTICS,
                    IntPtr.Zero);
                if (h == INVALID_HANDLE_VALUE)
                    throw new Win32Exception();

                try
                {
                    var sb = new StringBuilder(1024);
                    var res = GetFinalPathNameByHandle(h, sb, 1024, 0);
                    if (res == 0)
                        throw new Win32Exception();

                    return sb.ToString();
                }
                finally
                {
                    CloseHandle(h);
                }
            }
        }

    } 


}
