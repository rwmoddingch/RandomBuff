using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CoralBrain;
using HarmonyLib;
using JetBrains.Annotations;
using MonoMod.Utils;
using RWCustom;
using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
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
    public sealed partial class BuffDataManager
    {
        public static BuffDataManager Instance { get; private set; }


        /// <summary>
        /// 获取BuffData
        /// 外部方法
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public BuffData GetBuffData(BuffID id)
        {
            return GetOrCreateBuffData(id);
        }

        /// <summary>
        /// 获取全部启用的BuffID
        /// 外部方法
        /// </summary>
        /// <returns></returns>
        public List<BuffID> GetAllBuffIds(SlugcatStats.Name name)
        {
            if (!allDatas.ContainsKey(name))
                return new List<BuffID>();

            if (BuffPoolManager.Instance != null)
                return tempDatas.Keys.ToList();

            return allDatas[name].Keys.ToList();
        }

        /// <summary>
        /// 获取全部启用的BuffID
        /// 外部方法
        /// </summary>
        /// <returns></returns>
        public List<BuffID> GetAllBuffIds()
        {
            SlugcatStats.Name name;
            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
                name = game.StoryCharacter;
            else
                name = RainWorld.lastActiveSaveSlot;
            return GetAllBuffIds(name);
        }
    }



    /// <summary>
    /// 管理本存档下已加载过的全部BuffData（所有猫）
    /// 更换存档会重新创建
    ///
    /// 内部接口
    /// </summary>
    public sealed partial class BuffDataManager
    {


        internal static bool LoadData(string file,string formatVersion)
        {
            Instance = new BuffDataManager();
            return Instance.InitStringData(file,formatVersion);
        }


        /// <summary>
        /// 获取或创建BuffData
        /// 内部方法 
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="createIfMissing">当true时如果BuffData不存在则创建</param>
        /// <returns>返回值可能为空</returns>
        internal BuffData GetOrCreateBuffData(BuffID id, bool createIfMissing = false)
        {
            if (tempDatas.ContainsKey(id))
                return tempDatas[id];

            SlugcatStats.Name name;
            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
                name = game.StoryCharacter;
            else
                name = RainWorld.lastActiveSaveSlot;
            

            if (!allDatas.ContainsKey(name))
            {
                if (createIfMissing)
                    allDatas.Add(name, new());
                else
                    return null;
            }

            if (!allDatas[name].ContainsKey(id))
            {
                if (createIfMissing)
                {
                    allDatas[name].Add(id, (BuffData)Activator.CreateInstance(BuffRegister.GetDataType(id)));
                    allDatas[name][id].DataLoaded(true);
                    BuffFile.Instance.AddCollect(id.value);
                    BuffPlugin.Log($"Add new buff data. ID: {id}, Character :{name}");

                }
                else
                    return null;
            }

            return allDatas[name][id];
        }

        /// <summary>
        /// 创建BuffData在游戏局内
        /// 内部方法 
        /// </summary>
        /// <param name="id">ID</param>
        internal void CreateTempBuffData(SlugcatStats.Name name, BuffID id)
        {
            if (!allDatas.ContainsKey(name))
            {
                allDatas.Add(name, new());
      
            }


            if (!allDatas[name].ContainsKey(id) && !tempDatas.ContainsKey(id))
            {
                tempDatas.Add(id, (BuffData)Activator.CreateInstance(BuffRegister.GetDataType(id)));
                tempDatas[id].DataLoaded(true);
                BuffPlugin.Log($"Add new buff data. ID: {id}, Character :{name}");
            }
        }


        /// <summary>
        /// 从Menu进入游戏调用
        /// 进行初始化
        /// </summary>
        /// <param name="name"></param>

        internal void EnterGameFromMenu(SlugcatStats.Name name)
        {
            var setting = CreateOrGetSettingInstance(name);
            if (Custom.rainWorld.processManager.menuSetup.startGameCondition ==
                ProcessManager.MenuSetup.StoryGameInitCondition.New)
                setting.NewGame();
            
        }

        /// <summary>
        /// 每轮回进入游戏时调用
        /// </summary>
        /// <param name="name"></param>
        internal void EnterGame(SlugcatStats.Name name)
        {
            tempDatas = new();

            if (allDatas.ContainsKey(name))
            {
                BuffPlugin.Log("Clone all data to tempData");
                foreach (var data in allDatas[name].Values)
                    tempDatas.Add(data.ID, data.Clone());
            }
        }

      

        /// <summary>
        /// 轮回结束时更新 通过BuffPoolManager调用
        /// 负责把临时数据转移到永久
        /// </summary>
        /// <param name="name"></param>
        internal void WinGame(BuffPoolManager manager)
        {
            var name = manager.Game.StoryCharacter;
            if(!allDatas.ContainsKey(name)) allDatas.Add(name, new());


            allDatas[name] = tempDatas;
            foreach(var id in tempDatas.Keys)
                BuffFile.Instance.AddCollect(id.value);

            foreach (var data in allDatas[name])
                data.Value.CycleEnd();
        }

        /// <summary>
        /// 减少存档内BuffData的堆叠层数
        /// 层数为0或非堆叠自动删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal void UnstackBuff(BuffID id)
        {
            SlugcatStats.Name name;
            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
                name = game.StoryCharacter;
            else
                name = RainWorld.lastActiveSaveSlot;

            if (!allDatas.ContainsKey(name))
            {
                BuffPlugin.LogError($"Try unstack buff at null slot: {name}");
                return;
            }

            if (!allDatas[name].ContainsKey(id))
            {
                BuffPlugin.LogError($"unstack buff not found: {id}");
                return;
            }

            if (BuffConfigManager.GetStaticData(id).Stackable)
            {
                BuffPlugin.LogError($"UnStack buff : {name}");
                allDatas[name][id].UnStack();
                if (allDatas[name][id].StackLayer == 0)
                    RemoveBuffData(name, id);
                return;
            }
            else
            {
                RemoveBuffData(name, id);
            }    
        }


        /// <summary>
        /// 移除存档内BuffData
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private void RemoveBuffData(SlugcatStats.Name name, BuffID id)
        {
            BuffPlugin.Log($"Remove buff : {name}-{id}");
            if (tempDatas.ContainsKey(id))
                tempDatas.Remove(id);
            else
;               allDatas[name].Remove(id);
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

            if (BuffPoolManager.Instance != null)
                return tempDatas;

            return allDatas[name];
        }

        /// <summary>
        /// 安全的获取目前的setting信息
        /// 不会为空
        /// 但是instance可能为空
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal SafeGameSetting GetSafeSetting(SlugcatStats.Name name)
        {
            if(!allSettings.ContainsKey(name))
                allSettings.Add(name, new SafeGameSetting());
            return allSettings[name];
        }




        /// <summary>
        /// 安全的GameSetting
        /// 包含了UI界面的偏好信息
        /// </summary>
        internal class SafeGameSetting
        {
            public BuffSettingID ID = BuffSettingID.Normal;
            public BaseGameSetting instance;
        }


    }

    /// <summary>
    /// 私有部分
    /// </summary>
    public sealed partial class BuffDataManager
    {
        private BuffDataManager()
        {
        }

        /// <summary>
        /// 删除单一猫存档
        /// </summary>
        /// <param name="name"></param>
        internal void DeleteSaveData(SlugcatStats.Name name,bool deleteSetting = true)
        {
            if (name != null && allDatas.ContainsKey(name))
            {
                BuffPlugin.Log($"DELETE SAVE DATA: {name}");
                allDatas.Remove(name);
            }
            if(deleteSetting)
                GetSafeSetting(name).instance = null;
        }


        /// <summary>
        /// 删除存档
        /// </summary>
        internal void DeleteAll()
        {
            BuffPlugin.Log($"DELETE ALL SAVE DATA");
            allDatas.Clear();
            allSettings.Clear();
        }

        /// <summary>
        /// 创建新的Setting实例
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private BaseGameSetting CreateOrGetSettingInstance(SlugcatStats.Name name)
        {
            if (!allSettings.ContainsKey(name))
                allSettings.Add(name, new SafeGameSetting());
            return allSettings[name].instance =
                (BaseGameSetting)Activator.CreateInstance(BaseGameSetting.settingDict[allSettings[name].ID]);
        }

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
            foreach (var catSetting in allSettings)
            {
                builder.Append(catSetting.Key);
                builder.Append(CatIdSplit);

                builder.Append(catSetting.Value.ID);
                if (catSetting.Value.instance != null)
                {
            
                    builder.Append(BuffSplit);
                    builder.Append(JsonConvert.SerializeObject(catSetting.Value.instance));
                }

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
            if (formatVersion != "a-0.0.1")
            {
                var split = Regex.Split(file, SettingSplit)
                    .Where(i => !string.IsNullOrEmpty(i)).ToArray();
                if (split.Length == 0)
                {
                    BuffPlugin.LogWarning($"Empty Data !");
                    return false;

                }
                file = split[0];
                if (split.Length <= 1)
                    BuffPlugin.LogError($"Corrupted Data : Missing Setting data");
                else
                    InitStringSetting(split[1]);
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
                    if (dataType.type == null)
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
        private void InitStringSetting(string file)
        {
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
                if (allSettings.ContainsKey(slugName))
                    BuffPlugin.LogWarning($"Redefine Slugcat Name: {catSplit[0]}");
                else
                    allSettings.Add(slugName, new SafeGameSetting());


                var split = Regex.Split(catSplit[1], BuffSplit);


                //未知game setting类型
                if (!BaseGameSetting.settingDict.TryGetValue((allSettings[slugName].ID = new BuffSettingID(split[0])), out var type))
                {
                    allSettings[slugName].ID = BuffSettingID.Normal;
                    BuffPlugin.LogError($"Unknown game setting, IGNORED");
                    continue;
                }
                
                //如果存在实例则获取
                if (split.Length == 2)
                {
                    BuffPlugin.LogDebug($"Setting Data : {split[0]} - {split[1]}");

                    try
                    {
                        allSettings[slugName].instance = (BaseGameSetting)JsonConvert.DeserializeObject(split[1], type);
                    }
                    catch (Exception e)
                    {
                        BuffPlugin.LogException(e);
                        BuffPlugin.LogError($"Corrupted Buff Data At : {split[1]}");
                    }
                }
                else
                {
                    BuffPlugin.LogDebug($"Setting Data : {split[0]} - NULL");
                }
            }
        }

        /// <summary>
        /// 该存档槽下全部猫的存档数据
        /// </summary>
        private readonly Dictionary<SlugcatStats.Name, Dictionary<BuffID, BuffData>> allDatas = new();

        private readonly Dictionary<SlugcatStats.Name, SafeGameSetting> allSettings = new();

        /// 轮回内的临时数据
        private Dictionary<BuffID, BuffData> tempDatas = new ();

        /// 如果被卸载导致slugcat name或data缺失，则暂时储存在此处
        private readonly List<string> ukSlugcatDatas = new();
        private readonly Dictionary<SlugcatStats.Name, List<string>> ukBuffDatas = new();

        private const string CatSplit = " <BuA>";
        private const string CatIdSplit = "<BuAI>";

        private const string BuffSplit = "<BuB>";
        private const string BuffIdSplit = "<BuBI>";

        private const string SettingSplit = "<BuS>";
    }

}
