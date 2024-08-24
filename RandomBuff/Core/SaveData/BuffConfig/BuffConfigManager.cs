using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MonoMod.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Option;
using RandomBuff.Core.Progression;
using RandomBuff.Core.Progression.Quest;
using RandomBuff.Core.SaveData.BuffConfig;
using UnityEngine;
using Color = UnityEngine.Color;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;
using CustomAttributeNamedArgument = Mono.Cecil.CustomAttributeNamedArgument;
using SecurityAttribute = Mono.Cecil.SecurityAttribute;

namespace RandomBuff.Core.SaveData
{

    /// <summary>
    /// Buff静态数据的保存 仅跟随saveSlot变化而更换
    /// 公开部分
    /// </summary>
    public partial class BuffConfigManager
    {
        public static BuffConfigManager Instance { get; private set; }

        internal static void LoadConfig(string config, BuffFormatVersion formatVersion)
        {
            //BuffPlugin.Log($"Load config : {config}");
            Instance = new BuffConfigManager(config, formatVersion);
            BuffConfigurableManager.FetchAllConfigs();
        }

        /// <summary>
        /// 序列化保存配置信息
        /// </summary>
        /// <returns></returns>
        internal string ToStringData()
        {
            BuffConfigurableManager.PushAllConfigs();
            StringBuilder builder = new();
            foreach (var buffConfig in allConfigs)
            {
                builder.Append(buffConfig.Key);
                builder.Append(BuffIdSplit);
                foreach (var paraConfig in buffConfig.Value)
                {

                    builder.Append(paraConfig.Key);
                    builder.Append(ParameterIdSplit);
                    builder.Append(paraConfig.Value);
                    builder.Append(ParameterSplit);
                }

                builder.Append(BuffSplit);
            }
            return builder.ToString();
        }


        
        public readonly Dictionary<string, Dictionary<string, string>> allConfigs = new();
    }

    /// <summary>
    /// 内部实例部分
    /// </summary>
    public partial class BuffConfigManager
    {
        private BuffConfigManager(string config, BuffFormatVersion formatVersion)
        {

            foreach (var buffSingle in Regex.Split(config, BuffSplit)
                         .Where(i => !string.IsNullOrEmpty(i)))
            {
                var buffSplit = Regex.Split(buffSingle, BuffIdSplit);
                if (buffSplit.Length != 2)
                {
                    BuffPlugin.LogError($"Corrupted Config Data At: {buffSingle}");
                    continue;
                }

                if (allConfigs.ContainsKey(buffSplit[0]))
                    BuffPlugin.LogWarning($" Redefine buff name in config:{buffSplit[0]}");
                else
                    allConfigs.Add(buffSplit[0], new());

                var buffConfigs = allConfigs[buffSplit[0]];

                foreach (var paraSingle in Regex.Split(buffSplit[1], ParameterSplit)
                             .Where(i => !string.IsNullOrEmpty(i)))
                {
                    var paraSplit = Regex.Split(paraSingle, ParameterIdSplit);
                    if (paraSplit.Length != 2)
                    {
                        BuffPlugin.LogError($"Corrupted Config Data At: {buffSplit[0]}:{paraSingle}");
                        continue;
                    }

                    if (buffConfigs.ContainsKey(paraSplit[0]))
                    {
                        BuffPlugin.LogWarning($"Redefine BuffConfig Name: {buffSplit[0]}:{paraSplit[0]}, Ignore {paraSplit[1]}");
                        continue;
                    }

                    buffConfigs.Add(paraSplit[0], paraSplit[1]);
                }
            }
            BuffPlugin.Log($"Completed loaded config data, Count: {allConfigs.Count}");
        }



        internal const string BuffSplit = "<BuB>";
        internal const string BuffIdSplit = "<BuBI>";

        internal const string ParameterSplit = "<BuC>";
        internal const string ParameterIdSplit = "<BuCI>";
    }

    /// <summary>
    /// 静态部分
    /// </summary>
    public partial class BuffConfigManager
    {
        private static readonly Dictionary<BuffID, BuffStaticData> StaticDatas = new();
        private static readonly Dictionary<string, TemplateStaticData> TemplateDatas = new();
        private static readonly Dictionary<string, BuffQuest> QuestDatas = new();

        private static readonly Dictionary<string, HashSet<BuffStaticData>> ModStaticDatas = new();

        private static readonly Dictionary<QuestUnlockedType,Dictionary<string, string>> LockedMap = new();

        private static readonly Dictionary<string, Assembly> CacheAssemblies = new();
        private static readonly Dictionary<string, AssemblyDefinition> CacheAssembliesDef = new();



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StaticDataLoaded(BuffID buffID) { return StaticDatas.ContainsKey(buffID); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ContainsId(BuffID id)
            => StaticDatas.ContainsKey(id);

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static BuffStaticData GetStaticData(BuffID id) => StaticDatas[id];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ContainsTemplateName(string name) => TemplateDatas.ContainsKey(name);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static TemplateStaticData GetTemplateData(string name) => TemplateDatas[name];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static List<string> GetTemplateNameList() => TemplateDatas.Keys.ToList();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ContainsQuestName(string name) => QuestDatas.ContainsKey(name);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static BuffQuest GetQuestData(string id) => QuestDatas[id];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static List<string> GetQuestIDList() => QuestDatas.Keys.ToList();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsItemLocked(QuestUnlockedType unlockedType,string itemName) => (LockedMap[unlockedType].ContainsKey(itemName) && 
                !BuffPlayerData.Instance.IsQuestUnlocked(LockedMap[unlockedType][itemName])) && 
            (!BuffOptionInterface.Instance.CheatAllCosmetics.Value || unlockedType != QuestUnlockedType.Cosmetic) &&
            (!BuffOptionInterface.Instance.CheatAllCards.Value || unlockedType != QuestUnlockedType.Card);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsSpecialItemLocked(string itemName) => IsItemLocked(QuestUnlockedType.Special,itemName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsCosmeticCanUse(string id) => !IsItemLocked(QuestUnlockedType.Cosmetic, id) && BuffPlayerData.Instance.IsCosmeticEnable(id) && CosmeticUnlock.cosmeticUnlocks.ContainsKey(new CosmeticUnlockID(id));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetFreePickCount(float multiply) =>
            (int)((0.5f + LockedMap[QuestUnlockedType.FreePick].Count(i => BuffPlayerData.Instance.IsQuestUnlocked(i.Value))) * multiply);

        internal static Dictionary<BuffType,List<BuffID>> buffTypeTable = new ();

        static BuffConfigManager()
        {
            foreach (var value in Enum.GetValues(typeof(BuffType)))
                buffTypeTable.Add((BuffType)value, new List<BuffID>());
            foreach (var value in QuestUnlockedType.values.entries)
                LockedMap.Add(new(value),new());
        }

        /// <summary>
        /// 读取static data
        /// post init时调用
        /// 文件格式 mod根目录/buffassets/卡牌名/卡牌资源
        /// </summary>
        internal static void InitBuffStaticData()
        {
            BuffPlugin.Log("Loading All Buff Static Data!");

            foreach (var mod in ModManager.ActiveMods)
            {
                string path = Path.Combine(mod.basePath, "buffassets");

                if (!Directory.Exists(path))
                    continue;
                if (File.Exists(Path.Combine(BuffPlugin.CacheFolder, $"{mod.id}_dataCache.dll")) && 
                    CheckFileNoUpdated(Path.Combine(BuffPlugin.CacheFolder, $"{mod.id}_dataCache.dll"), path)) 
                {
                    var ass = Assembly.LoadFile(Path.Combine(BuffPlugin.CacheFolder, $"{mod.id}_dataCache.dll"));
                    Type[] types = ass.GetTypes();
                    if (types.FirstOrDefault(i => i.Name.Contains("StaticCache_")) is { } type)
                    {
                        BuffPlugin.Log($"Load cache file for mod:{mod.id}");
                        var modStaticData = (HashSet<BuffStaticData>)type.GetField("StaticData", BindingFlags.Public | BindingFlags.Static)
                            .GetValue(null);
                        ModStaticDatas.Add(mod.id, modStaticData);
                        foreach (var data in modStaticData)
                        {
                            if (StaticDatas.ContainsKey(data.BuffID))
                            {
                                BuffPlugin.LogError($"Same Key at {StaticDatas[data.BuffID].ToDebugString()}");
                                continue;
                            }
                            StaticDatas.Add(data.BuffID, data);
                            buffTypeTable[data.BuffType].Add(data.BuffID);
                
                        }
                        continue;
                    }


                }

                AssemblyDefinition assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition($"{mod.id}_dataCache", new Version(BuffPlugin.ModVersion)),
                    "Main", ModuleKind.Dll);

                var decl = new SecurityDeclaration(Mono.Cecil.SecurityAction.RequestMinimum);
                assembly.SecurityDeclarations.Add(decl);
                var attr = new SecurityAttribute(assembly.MainModule.ImportReference(typeof(SecurityPermissionAttribute)));
                decl.SecurityAttributes.Add(attr);
                attr.Properties.Add(new CustomAttributeNamedArgument("SkipVerification",
                    new CustomAttributeArgument(assembly.MainModule.TypeSystem.Boolean, true)));
         
                ModStaticDatas.Add(mod.id, new());
                LoadInDirectory(new DirectoryInfo(path), new DirectoryInfo(mod.path).FullName, mod.id);

                CreateStaticDataCache(assembly.MainModule, mod.id);
                assembly.Write(Path.Combine(BuffPlugin.CacheFolder, $"{mod.id}_dataCache.dll"));


            }

            bool CheckFileNoUpdated(string assemblyPath, string assetPath)
            {
                var assemblyTime = new FileInfo(assemblyPath).LastWriteTime;
                List<DirectoryInfo> infos = new List<DirectoryInfo>(){new (assetPath)};
                for (int i = 0; i < infos.Count; i++)
                {
                    //if (infos[i].LastWriteTime > assemblyTime)
                    //    return false;
                    if (infos[i].GetFiles("*.json").Any(i => i.LastWriteTime > assemblyTime))
                        return false;
                    infos.AddRange(infos[i].GetDirectories());
                }
                return true;
            }
        }


        private static void CreateStaticDataCache(ModuleDefinition module,string modId)
        {
            var type = new TypeDefinition("RandomBuffCache", $"StaticCache_{modId}", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed |
                TypeAttributes.BeforeFieldInit,module.TypeSystem.Object);
            module.Types.Add(type);
            module.ImportReference(typeof(Color));
            var field = new FieldDefinition("StaticData", FieldAttributes.Static | FieldAttributes.Public, module.ImportReference(typeof(HashSet<BuffStaticData>)));
            type.Fields.Add(field);
    
            BuffPlugin.Log($"Create static data cache for {modId}");
            var staticDataType = typeof(BuffStaticData);
            var staticDataCtor = staticDataType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .First(i => i.GetParameters().Length == 0);

            BuffStaticData defaultData = (BuffStaticData)staticDataCtor.Invoke(Array.Empty<object>());

            type.DefineStaticConstructor((il) =>
            {
                //.Body.Method.Attributes |= MethodAttributes.UnmanagedExport
                var dataVal = new VariableDefinition(type.Module.ImportReference(typeof(BuffStaticData)));
                il.Body.Variables.Add(dataVal);

                il.Emit(OpCodes.Newobj, typeof(HashSet<BuffStaticData>).GetConstructor(Type.EmptyTypes));
                il.Emit(OpCodes.Stsfld, field);
                foreach (var data in ModStaticDatas[modId])
                {
                    il.Emit(OpCodes.Ldsfld, field);
                    il.Emit(OpCodes.Newobj, staticDataCtor);
                    il.Emit(OpCodes.Stloc, dataVal);
                    foreach (var property in staticDataType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public |
                                                                          BindingFlags.Instance))
                    {
                        var defaultValue = property.GetValue(defaultData);
                        var dataValue = property.GetValue(data);
                        if((property.PropertyType.GetProperty("Count") is {} count &&
                           (int)count.GetValue(dataValue) == 0) ||
                           (property.PropertyType.GetProperty("Length") is { } length &&
                            (int)length.GetValue(dataValue) == 0))
                            continue;
                        if(defaultValue == null && dataValue == null)
                            continue;
                        if(defaultValue != null && defaultValue.Equals(dataValue))
                            continue;

                        if ((property.GetSetMethod() ?? property.GetSetMethod(true)) is { } set)
                        {
                            il.Emit(OpCodes.Ldloc, dataVal);
                            EmitValue(il, dataValue);
                            il.Emit(OpCodes.Callvirt, set);
                        }

                    }
                    il.Emit(OpCodes.Ldloc, dataVal);
                    il.Emit(OpCodes.Callvirt, typeof(HashSet<BuffStaticData>).GetMethod("Add"));
                    il.Emit(OpCodes.Pop);
                }
                il.Emit(OpCodes.Ret);
                il.Body.Optimize();

            });
        }

        private static void LoadInDirectory(DirectoryInfo info, string rootPath, string modId)
        {
            foreach (var dir in info.GetDirectories())
            {
                LoadInDirectory(dir, rootPath, modId);
            }

            foreach (var file in info.GetFiles("*.json"))
            {
                if (BuffStaticData.TryLoadStaticData(file,
                        info.FullName.Replace(rootPath, ""), out var data))
                {
                    if (!StaticDatas.ContainsKey(data.BuffID))
                    {
                        StaticDatas.Add(data.BuffID, data);
                        ModStaticDatas[modId].Add(data);
                        buffTypeTable[data.BuffType].Add(data.BuffID);
                    }
                    else
                    {
                        BuffPlugin.LogError($"Same Key at {StaticDatas[data.BuffID].ToDebugString()}");
                    }
                }
            }
        }

        /// <summary>
        /// 读取templateData
        /// post init时调用
        /// 文件格式 mod根目录/bufftemplates/
        /// </summary>
        internal static void InitTemplateStaticData()
        {
            BuffPlugin.Log("Loading All Template Data!");
            foreach (var mod in ModManager.ActiveMods)
            {
                string path = mod.path + Path.DirectorySeparatorChar + "bufftemplates";
                if (!Directory.Exists(path))
                    continue;
                var info = new DirectoryInfo(path);
                foreach (var file in info.GetFiles("*.json"))
                {
                    if (TemplateStaticData.TryLoadTemplateStaticData(file, out var data))
                    {
                        if (!TemplateDatas.ContainsKey(data.Name))
                            TemplateDatas.Add(data.Name, data);
                        else
                            BuffPlugin.LogError($"Same Key at {data.Name}");
                        
                    }
                }
            }

            if (TemplateDatas.Count == 0)
            {
                BuffPlugin.LogWarning("Missing Template, Load fallback template");
                if (TemplateStaticData.TryLoadTemplateStaticData("FallBackNormalTemplate",BuffResource.NormalTemplate, out var data))
                {
                    TemplateDatas.Add(data.Name, data);
                }
            }
        }

        /// <summary>
        /// 读取templateData
        /// post init时调用
        /// 文件格式 mod根目录/buffquests/
        /// </summary>
        public static void InitQuestData()
        {
            BuffPlugin.Log("Loading All BuffQuest Data!");

            foreach (var mod in ModManager.ActiveMods)
            {
                string path = mod.path + Path.DirectorySeparatorChar + "buffquests";
                if (!Directory.Exists(path))
                    continue;
                var info = new DirectoryInfo(path);
                BuffPlugin.LogDebug($"Load quest file:{path}");
                foreach (var file in info.GetFiles("*.json"))
                {
                    try
                    {
                        var quest = JsonConvert.DeserializeObject<BuffQuest>(File.ReadAllText(file.FullName));
                        if (quest.QuestId == null || quest.QuestName == null)
                            BuffPlugin.LogError($" BuffQuest Name or ID missing ,Mod:{mod.name} ,Path:{file.FullName}");
                        else if ((quest.UnlockItem?.Sum(i => i.Value.Length) ?? 0) == 0)
                            BuffPlugin.LogError($"Null BuffQuest Unlocked Item at:{quest.QuestId} ,Mod:{mod.name}");
                        else if (!quest.VerifyData())
                            BuffPlugin.LogError($"BuffQuest VerifyData Error at:{quest.QuestId} ,Mod:{mod.name}");
                        else if (QuestDatas.ContainsKey(quest.QuestId))
                            BuffPlugin.LogError($"Conflict BuffQuest ID at:{quest.QuestId} ,Mod:{mod.name}");
                        else
                        {

                            foreach (var dic in quest.UnlockItem)
                            {
                                foreach (var item in dic.Value)
                                {
                                    if (LockedMap[dic.Key].ContainsKey(item))
                                        BuffPlugin.LogWarning(
                                            $"Conflict BuffQuest unlocked item at ID:{quest.QuestId} ,Item:{item} ,Mod:{mod.name}");
                                    else
                                    {
                                        LockedMap[dic.Key].Add(item, quest.QuestId);
                                        BuffPlugin.LogDebug($"Lock Type:{dic.Key} ItemValue:{item}");
                                    }
                                }
                            }
                            QuestDatas.Add(quest.QuestId, quest);
                        }


                    }
                    catch (Exception e)
                    {
                        BuffPlugin.LogException(e, $"Invalid BuffQuest file! Mod:{mod.name} ,Path:{file.FullName}");
                    }

                }

#if TESTVERSION
                foreach (var qId in GetQuestIDList())
                {
                    BuffPlugin.LogDebug($"Quest ID:{qId}");
                    foreach (var con in GetQuestData(qId).QuestConditions)
                        BuffPlugin.LogDebug($"---condition:{con.ConditionMessage()}");
                    foreach (var reward in GetQuestData(qId).UnlockItem)
                    foreach (var v in reward.Value)
                        BuffPlugin.LogDebug($"---reward:{reward.Key},{v}");



                }
#endif
            }

        }




        private static void EmitValue(ILProcessor processor, object value)
        {
            var type = value.GetType();
            if (type == typeof(int) || type.IsEnum)
                processor.Emit(OpCodes.Ldc_I4, (int)value);
            else if (value is bool bo)
                processor.Emit(OpCodes.Ldc_I4, bo ? 1 : 0);
            else if (type == typeof(float))
                processor.Emit(OpCodes.Ldc_R4, value);
            else if (type == typeof(string))
                processor.Emit(OpCodes.Ldstr, value);
            else if (value is IDictionary dictionary)
            {
                if (dictionary.Count == 0)
                    processor.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
                else
                {
                    var dicVal = GetOrCreate(type);
                    processor.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
                    processor.Emit(OpCodes.Stloc, dicVal);
                    foreach (var key in dictionary.Keys)
                    {
                        processor.Emit(OpCodes.Ldloc, dicVal);
                        EmitValue(processor, key);
                        EmitValue(processor, dictionary[key]);
                        processor.Emit(OpCodes.Callvirt, type.GetMethod("Add"));
                        if (type.GetMethod("Add").ReturnType != typeof(void))
                            processor.Emit(OpCodes.Pop);
                    }

                    processor.Emit(OpCodes.Ldloc, dicVal);
                }

            }
            else if (value is IEnumerable enumerable)
            {
                bool hasAny = false;
                var listVal = GetOrCreate(type);
                processor.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
                var instr = Instruction.Create(OpCodes.Stloc, listVal);
                processor.Append(instr);
                foreach (var item in enumerable)
                {
                    hasAny = true;
                    processor.Emit(OpCodes.Ldloc, listVal);
                    EmitValue(processor, item);
                    processor.Emit(OpCodes.Callvirt, type.GetMethod("Add"));
                    if (type.GetMethod("Add").ReturnType != typeof(void))
                        processor.Emit(OpCodes.Pop);
                }
                if (hasAny)
                    processor.Emit(OpCodes.Ldloc, listVal);
                else
                    processor.Remove(instr);

            }
            else if (value is InGameTranslator.LanguageID id &&
                     typeof(InGameTranslator.LanguageID).GetField(id.value) is { } field)
            {
                processor.Emit(OpCodes.Ldsfld, field);
            }
            else if (value is ExtEnumBase ex)
            {
                processor.Emit(OpCodes.Ldstr, ex.value);
                processor.Emit(OpCodes.Ldc_I4_0);
                processor.Emit(OpCodes.Newobj, type.GetConstructor(new[] { typeof(string), typeof(bool) }));
            }
            else if (value is BuffStaticData.CardInfo info)
            {
                processor.Emit(OpCodes.Ldstr, info.BuffName);
                processor.Emit(OpCodes.Ldstr, info.Description);
                processor.Emit(OpCodes.Newobj, type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance
                    , null, CallingConventions.Any,
                    new[] { typeof(string), typeof(string) },
                    Array.Empty<ParameterModifier>()));
            }
            else if (value is Color color)
            {
                processor.Emit(OpCodes.Ldc_R4, color.r);
                processor.Emit(OpCodes.Ldc_R4, color.g);
                processor.Emit(OpCodes.Ldc_R4, color.b);
                processor.Emit(OpCodes.Ldc_R4, color.a);
                processor.Emit(OpCodes.Newobj, type.GetConstructor(new[] { typeof(float), typeof(float), typeof(float), typeof(float) }));
            }
            else if (type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                         null, CallingConventions.Any, Type.EmptyTypes, Array.Empty<ParameterModifier>()) is { } ctor)
            {
                var val = GetOrCreate(type);
                processor.Emit(OpCodes.Newobj, ctor);
                foreach (var property in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public |
                                                            BindingFlags.Instance).Where(i => i.CanRead))
                {

                    if ((property.GetSetMethod() ?? property.GetSetMethod(true)) is { } set)
                    {
                        processor.Emit(OpCodes.Ldloc, val);
                        EmitValue(processor, property.GetValue(value));
                        processor.Emit(OpCodes.Callvirt, set);
                    }
                }

                foreach (var fieldInfo in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public |
                                                            BindingFlags.Instance).Where(i => i.CanRead))
                {
                    processor.Emit(OpCodes.Ldloc, val);
                    EmitValue(processor, fieldInfo.GetValue(value));
                    processor.Emit(OpCodes.Ldfld, fieldInfo);

                }
            }
            else
            {
                BuffPlugin.LogError($"unexpected type : {type}");
            }

            VariableDefinition GetOrCreate(Type valType)
            {
                var re = processor.Body.Variables.FirstOrDefault(i => i.VariableType.FullName.Contains(valType.Name));
                if (re == null)
                {
                    re = new VariableDefinition(processor.Import(type));
                    processor.Body.Variables.Add(re);
                }

                return re;
            }
        }
    }
}
