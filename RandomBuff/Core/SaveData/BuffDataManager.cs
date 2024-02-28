using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using RWCustom;
using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuff.Core.Game.Settings;
using UnityEngine;

namespace RandomBuff.Core.SaveData
{
    /// <summary>
    /// 管理本存档下已加载过的全部BuffData（所有猫）
    /// 更换存档会重新创建
    ///
    /// 外部接口
    /// 没有提供直接获取字典的方式防止意外的外部修改
    /// </summary>
    internal sealed partial class BuffDataManager
    {
        internal static BuffDataManager Instance { get; private set; }


        /// <summary>
        /// 获取BuffData
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal BuffData GetBuffData(BuffID id)
        {
            if (BuffPoolManager.Instance != null)
            {
                BuffPlugin.LogError("Access BuffDataManager:GetBuffData in Game");
                return BuffPoolManager.Instance.GetBuffData(id);
            }
            return GetOrCreateBuffData(id);
        }

        /// <summary>
        /// 获取全部启用的BuffID
        /// </summary>
        /// <returns></returns>
        internal List<BuffID> GetAllBuffIds(SlugcatStats.Name name)
        {
            if (BuffPoolManager.Instance != null)
            {
                BuffPlugin.LogError("Access BuffDataManager:GetAllBuffIds in Game");
                if (BuffPoolManager.Instance.Game.StoryCharacter == name)
                    return BuffPoolManager.Instance.GetAllBuffIds();
            }
            if (!allDatas.ContainsKey(name))
                return new List<BuffID>();

            return allDatas[name].Keys.ToList();
        }

    }


    /// <summary>
    /// 管理本存档下已加载过的全部BuffData（所有猫）
    /// 更换存档会重新创建
    ///
    /// 内部接口
    /// </summary>
    internal sealed partial class BuffDataManager
    {

        internal static bool LoadData(string file,string formatVersion)
        {
            Instance = new BuffDataManager();
            return Instance.InitStringData(file,formatVersion);
        }

        /// <summary>
        /// 获取或创建BuffData
        /// 仅限非游戏局内使用
        /// 内部方法 
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="createOrStack">当true时如果BuffData不存在则创建，存在则叠加</param>
        /// <returns>返回值可能为空</returns>
        internal BuffData GetOrCreateBuffData(BuffID id, bool createOrStack = false)
        {
            if (BuffPoolManager.Instance != null)
            {
                BuffPlugin.LogError("Access BuffDataManager:GetOrCreateBuffData in Game");
                return BuffPoolManager.Instance.GetBuffData(id) ?? BuffPoolManager.Instance.CreateNewBuffData(id);
            }
            SlugcatStats.Name name;
            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
                name = game.StoryCharacter;
            else
                name = RainWorld.lastActiveSaveSlot;
            
            if (!allDatas.ContainsKey(name))
            {
                if (createOrStack)
                    allDatas.Add(name, new());
                else
                    return null;
            }

            if (!allDatas[name].ContainsKey(id))
            {
                if (createOrStack)
                {
                    allDatas[name].Add(id, (BuffData)Activator.CreateInstance(BuffRegister.GetDataType(id)));
                    allDatas[name][id].DataLoaded(true);
                    BuffHookWarpper.EnableBuff(id, HookLifeTimeLevel.UntilQuit);
                    BuffPlugin.Log($"Add new buff data. ID: {id}, Character :{name}");
                }
                else
                    return null;
            }
            if(createOrStack)
                allDatas[name][id].Stack();

            return allDatas[name][id];
        }



        /// <summary>
        /// 从Menu进入游戏调用
        /// 进行初始化
        /// </summary>
        /// <param name="name"></param>

        internal void EnterGameFromMenu(SlugcatStats.Name name)
        {
            var setting = GetGameSetting(name);
            BuffHookWarpper.CheckAndDisableAllHook();
            foreach (var id in GetAllBuffIds(name))
                BuffHookWarpper.EnableBuff(id,HookLifeTimeLevel.UntilQuit);

            if (Custom.rainWorld.processManager.menuSetup.startGameCondition ==
                ProcessManager.MenuSetup.StoryGameInitCondition.New)
                setting.NewGame();
        }

    

        /// <summary>
        /// 轮回结束时更新 通过BuffPoolManager调用
        /// 负责把临时数据转移到永久
        /// </summary>
        /// <param name="name"></param>
        internal void WinGame(BuffPoolManager manager, Dictionary<BuffID, BuffData> tempDatas, GameSetting setting)
        {
            var name = manager.Game.StoryCharacter;

            if(!allDatas.ContainsKey(name)) allDatas.Add(name, new());
            allDatas[name] = tempDatas;
            gameSettings[name] = setting;

            foreach(var id in allDatas[name].Keys)
                BuffPlayerData.Instance.AddCollect(id);

            foreach (var data in allDatas[name])
                data.Value.CycleEnd();
        }


        /// <summary>
        /// 获取或创建单猫存档下的BuffData字典
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal Dictionary<BuffID, BuffData> GetDataDictionary(SlugcatStats.Name name)
        {
            if (!allDatas.ContainsKey(name))
                allDatas.Add(name, new());

            return allDatas[name];
        }

        /// <summary>
        /// 获取目前的setting信息
        /// 不会为空
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal GameSetting GetGameSetting(SlugcatStats.Name name)
        {
            if(!gameSettings.ContainsKey(name))
                gameSettings.Add(name, new GameSetting());
            return gameSettings[name];
        }

    }

    /// <summary>
    /// 私有部分
    /// </summary>
    internal sealed partial class BuffDataManager
    {
        private BuffDataManager()
        {
        }

        /// <summary>
        /// 删除单一猫存档
        /// </summary>
        /// <param name="name"></param>
        internal void DeleteSaveData(SlugcatStats.Name name)
        {
            if (name != null && allDatas.ContainsKey(name))
            {
                BuffPlugin.Log($"DELETE SAVE DATA: {name}");
                allDatas.Remove(name);
            }
        }


        /// <summary>
        /// 删除存档
        /// </summary>
        internal void DeleteAll()
        {
            BuffPlugin.Log($"DELETE ALL SAVE DATA");
            allDatas.Clear();
            gameSettings.Clear();
        }

        /// <summary>
        /// 创建新的Setting实例
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
   

        /// <summary>
        /// 序列化存档信息
        /// </summary>
        /// <returns></returns>
        internal string ToStringData()
        {
            StringBuilder builder = new();
            foreach (var catData in allDatas)
            {
                builder.Append(catData.Key);
                builder.Append(CatIdSplit);
                foreach (var buffData in catData.Value)
                {
                    string valueData = "";
                    try
                    {
                        valueData = JsonConvert.SerializeObject(buffData.Value);

                    }
                    catch (Exception e)
                    {
                        BuffPlugin.LogException(e);
                        BuffPlugin.LogError($"Serialize Failed at {catData.Key}:{buffData.Key}, Ignored");
                        continue;
                    }
                    builder.Append(buffData.Key);
                    builder.Append(BuffIdSplit);
                    builder.Append(valueData);
                    builder.Append(BuffSplit);
                }

                if (ukBuffDatas.ContainsKey(catData.Key))
                {
                    foreach (var ukBuffData in ukBuffDatas[catData.Key])
                    {
                        builder.Append(ukBuffData);
                        builder.Append(BuffSplit);
                    }
                }

                builder.Append(CatSplit);
            }

            foreach (var ukCatData in ukSlugcatDatas)
            {
                builder.Append(ukCatData);
                builder.Append(CatSplit);
            }

            //---------------------设置-------------------//

            builder.Append(SettingSplit);
            foreach (var catSetting in gameSettings)
            {
                builder.Append(catSetting.Key);
                builder.Append(CatIdSplit);
                builder.Append(catSetting.Value.SaveToString());
                builder.Append(CatSplit);
            }

            foreach (var ukCatSetting in ukSlugcatSettings)
            {
                builder.Append(ukCatSetting);
                builder.Append(CatSplit);
            }

            return builder.ToString();
        }


        /// <summary>
        /// 初始化存档信息
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private bool InitStringData(string file, string formatVersion)
        {

            var split = Regex.Split(file, SettingSplit)
                .Where(i => !string.IsNullOrEmpty(i)).ToArray();
            if (split.Length == 0)
            {
                BuffPlugin.LogWarning($"Empty data !");
                return false;

            }
            file = split[0];
            if (split.Length <= 1)
                BuffPlugin.LogWarning($"Missing Setting data !");
            else
                InitStringSetting(split[1], formatVersion);


            foreach (var catSingle in Regex.Split(file, CatSplit)
                         .Where(i => !string.IsNullOrEmpty(i)))
            {
                var catSplit = Regex.Split(catSingle, CatIdSplit);

                //数据损坏
                if (catSplit.Length != 2)
                {
                    BuffPlugin.LogError($"Corrupted Buff Data At: {catSingle}");
                    continue;
                }

                //不存在的猫名字
                if (!ExtEnumBase.TryParse(typeof(SlugcatStats.Name), catSplit[0], true, out var re) ||
                    re is not SlugcatStats.Name slugName)
                {
                    BuffPlugin.LogWarning($"Unknown Slugcat Name: {catSplit[0]}");
                    //暂存栏
                    ukSlugcatDatas.Add(catSingle);
                    continue;
                }


                //重定义只做提示
                if (allDatas.ContainsKey(slugName))
                    BuffPlugin.LogWarning($"Redefine Slugcat Name: {catSplit[0]}");
                else
                    allDatas.Add(slugName, new());

                var slugDatas = allDatas[slugName];

                //读取对应猫内的数据
                foreach (var dataSingle in Regex.Split(catSplit[1], BuffSplit)
                             .Where(i => !string.IsNullOrEmpty(i)))
                {

                    var dataSplit = Regex.Split(dataSingle, BuffIdSplit);

                    if (dataSplit.Length != 2)
                    {
                        BuffPlugin.LogError($"Corrupted Buff Data At: {catSplit[0]}:{dataSingle}");
                        continue;
                    }

                    var dataType = BuffRegister.GetDataType(dataSplit[0]);
                    //不存在Type
                    if (dataType.type == null || !BuffConfigManager.ContainsId(dataType.id))
                    {
                        BuffPlugin.LogWarning($"Unknown BuffData ID : {dataSplit[0]}");
                        if (!ukBuffDatas.ContainsKey(slugName))
                            ukBuffDatas.Add(slugName, new());
                        //暂存数据
                        ukBuffDatas[slugName].Add(dataSingle);
                        continue;
                    }

                    //重定义提示并返回
                    if (slugDatas.ContainsKey(dataType.id))
                    {
                        BuffPlugin.LogWarning($"Redefine BuffData Id: {catSplit[0]}:{dataSplit[0]}, Ignore: {dataSplit[1]}");
                        continue;
                    }

                    BuffData newData;
                    try
                    {
                        newData = (BuffData)JsonConvert.DeserializeObject(dataSplit[1], dataType.type);
                        newData.DataLoaded(false);
                    }
                    catch (Exception e)
                    {
                        BuffPlugin.LogException(e);
                        BuffPlugin.LogError($"Corrupted Buff Data At : {dataSplit[1]}");
                        newData = GetOrCreateBuffData(dataType.id, true);
                        newData.DataLoaded(true);
                    }
                    slugDatas.Add(dataType.id, newData);
                }

            }

            return true;
        }

        /// <summary>
        /// 初始化setting信息
        /// </summary>
        /// <param name="data"></param>
        ///  <param name="version"></param>
        private void InitStringSetting(string file, string version)
        {
            if (version == "a-0.0.2")
            {
                BuffPlugin.LogWarning("Remove OutDate settings.");
                return;
            }

            foreach (var catSingle in Regex.Split(file, CatSplit)
                         .Where(i => !string.IsNullOrEmpty(i)))
            {
                var catSplit = Regex.Split(catSingle, CatIdSplit);

                //数据损坏
                if (catSplit.Length != 2)
                {
                    BuffPlugin.LogError($"Corrupted Buff Data At: {catSingle}");
                    continue;
                }

                //不存在的猫名字
                if (!ExtEnumBase.TryParse(typeof(SlugcatStats.Name), catSplit[0], true, out var re) ||
                    re is not SlugcatStats.Name slugName)
                {
                    BuffPlugin.LogWarning($"Unknown Slugcat Name: {catSplit[0]}");
                    //暂存栏
                    ukSlugcatSettings.Add(catSingle);
                    continue;
                }

                if (!GameSetting.TryLoadGameSetting(catSplit[1], out var setting))
                    setting = new GameSetting();
                BuffPlugin.LogDebug($"{catSplit[0]}, {catSplit[1]}");
                gameSettings.Add(slugName,setting);

            }


        }

        /// <summary>
        /// 该存档槽下全部猫的存档数据
        /// </summary>
        private readonly Dictionary<SlugcatStats.Name, Dictionary<BuffID, BuffData>> allDatas = new();

        


        /// 如果被卸载导致slugcat name或data缺失，则暂时储存在此处
        private readonly List<string> ukSlugcatDatas = new();
        private readonly Dictionary<SlugcatStats.Name, List<string>> ukBuffDatas = new();

        private readonly Dictionary<SlugcatStats.Name, GameSetting> gameSettings = new();
        private readonly List<string> ukSlugcatSettings = new();


        private const string CatSplit = " <BuA>";
        private const string CatIdSplit = "<BuAI>";

        private const string BuffSplit = "<BuB>";
        private const string BuffIdSplit = "<BuBI>";

        private const string SettingSplit = "<BuS>";
    }

}
