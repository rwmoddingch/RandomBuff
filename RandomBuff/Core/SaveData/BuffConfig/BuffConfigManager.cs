using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Progression;
using RandomBuff.Core.SaveData.BuffConfig;
using UnityEngine;

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
            BuffPlugin.Log($"Load config : {config}");
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
        private static Dictionary<BuffID, BuffStaticData> staticDatas = new();
        private static Dictionary<string, TemplateStaticData> templateDatas = new();
        private static Dictionary<string, BuffQuest> questDatas = new();

        private static Dictionary<QuestUnlockedType,Dictionary<string, string>> lockedMap = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StaticDataLoaded(BuffID buffID) { return staticDatas.ContainsKey(buffID); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ContainsId(BuffID id)
            => staticDatas.ContainsKey(id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ContainsProperty(BuffID id, string key)
            => staticDatas.ContainsKey(id) && staticDatas[id].customParameterDefaultValues.ContainsKey(key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static BuffStaticData GetStaticData(BuffID id) => staticDatas[id];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ContainsTemplateName(string name) => templateDatas.ContainsKey(name);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static TemplateStaticData GetTemplateData(string name) => templateDatas[name];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static List<string> GetTemplateNameList() => templateDatas.Keys.ToList();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ContainsQuestName(string name) => questDatas.ContainsKey(name);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static BuffQuest GetQuestData(string name) => questDatas[name];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static List<string> GetQuestNameList() => questDatas.Keys.ToList();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsItemLocked(QuestUnlockedType unlockedType,string itemName) => lockedMap[unlockedType].ContainsKey(itemName) && !BuffPlayerData.Instance.IsQuestUnlocked(lockedMap[unlockedType][itemName]);

        internal static Dictionary<BuffType,List<BuffID>> buffTypeTable = new ();

        static BuffConfigManager()
        {
            foreach(var value in Enum.GetValues(typeof(BuffType)))
                buffTypeTable.Add((BuffType)value, new List<BuffID>());
            foreach (var value in Enum.GetValues(typeof(QuestUnlockedType)))
                lockedMap.Add((QuestUnlockedType)value,new ());
        }

        /// <summary>
        /// 读取static data
        /// post init时调用
        /// 文件格式 mod根目录/buffassets/卡牌名/卡牌资源
        /// </summary>
        internal static void InitBuffStaticData()
        {
            BuffPlugin.Log("Loading All Buff Static Data!");
            var dt = DateTime.Now;
            foreach (var mod in ModManager.ActiveMods)
            {
                string path = mod.path + Path.DirectorySeparatorChar + "buffassets";
                BuffPlugin.Log($"{path} | {Directory.Exists(path)}");
                if (!Directory.Exists(path))
                    continue;

                LoadInDirectory(new DirectoryInfo(path), new DirectoryInfo(mod.path).FullName);
            }
            BuffPlugin.LogDebug($"Cost time {dt-DateTime.Now}");
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
                        if (!templateDatas.ContainsKey(data.Name))
                            templateDatas.Add(data.Name, data);
                        else
                            BuffPlugin.LogError($"Same Key at {data.Name}");
                        
                    }
                }
            }

            if (templateDatas.Count == 0)
            {
                BuffPlugin.LogWarning("Missing Template, Load fallback template");
                if (TemplateStaticData.TryLoadTemplateStaticData("FallBackNormalTemplate",BuffResource.NormalTemplate, out var data))
                {
                    templateDatas.Add(data.Name, data);
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
                BuffPlugin.LogDebug($"Load file:{path}");
                foreach (var file in info.GetFiles("*.json"))
                {
                    try
                    {
                        var questType = JsonConvert.DeserializeObject<BuffQuestJsonGetType>(File.ReadAllText(file.FullName));
                        if(questType.TypeName == null)
                            BuffPlugin.LogError($" BuffQuest TypeName ,Mod:{mod.name} ,Path:{file.FullName}");
                        else if (BuffQuest.TryGetType(questType.TypeName, out var type))
                        {
                            var quest = (BuffQuest)JsonConvert.DeserializeObject(File.ReadAllText(file.FullName), type);
                            if(quest.QuestId == null || quest.QuestName == null)
                                BuffPlugin.LogError($" BuffQuest Name or ID missing ,Mod:{mod.name} ,Path:{file.FullName}");
                            else if ((quest.UnlockItem?.Sum(i => i.Value.Length) ?? 0) == 0)
                                BuffPlugin.LogError($"Null BuffQuest Unlocked Item at:{quest.QuestId} ,Mod:{mod.name}");
                            else if (!quest.VerifyData())
                                BuffPlugin.LogError($"BuffQuest VerifyData Error at:{quest.QuestId} ,Mod:{mod.name}");
                            else if (questDatas.ContainsKey(quest.QuestId))
                             BuffPlugin.LogError($"Conflict BuffQuest ID at:{quest.QuestId} ,Mod:{mod.name}");
                            else
                            {
                           
                                foreach (var dic in quest.UnlockItem)
                                {
                                    foreach (var item in dic.Value)
                                    {
                                        if (lockedMap[dic.Key].ContainsKey(item))
                                            BuffPlugin.LogWarning(
                                                $"Conflict BuffQuest unlocked item at ID:{quest.QuestId} ,Item:{item} ,Mod:{mod.name}");
                                        else
                                        {
                                            lockedMap[dic.Key].Add(item, quest.QuestId);
                                            BuffPlugin.LogDebug($"Lock Type:{dic.Key.ToString()} ItemValue:{item}");
                                        }
                                    }
                                }
                                questDatas.Add(quest.QuestId, quest);
                            }
                        }
                        else
                        {
                            BuffPlugin.LogError($"Unknown BuffQuest unlockedType! Mod:{mod.name} ,Path:{file.FullName}, Type:{questType.TypeName}");
                        }
                    }
                    catch (Exception e)
                    {
                       BuffPlugin.LogException(e, $"Invalid BuffQuest file! Mod:{mod.name} ,Path:{file.FullName}");
                    }
             
                }
            }
        }


        private static void LoadInDirectory(DirectoryInfo info, string rootPath)
        {
            foreach (var dir in info.GetDirectories())
            {
                LoadInDirectory(dir, rootPath);
            }

            foreach (var file in info.GetFiles("*.json"))
            {
                if (BuffStaticData.TryLoadStaticData(file,
                        info.FullName.Replace(rootPath, ""), out var data))
                {
                    if (!staticDatas.ContainsKey(data.BuffID))
                    {
                        staticDatas.Add(data.BuffID, data);
                        buffTypeTable[data.BuffType].Add(data.BuffID);
                    }
                    else
                    {
                        BuffPlugin.LogError($"Same Key at {staticDatas[data.BuffID].ToDebugString()}");
                    }
                }
            }
        }
    }
}
