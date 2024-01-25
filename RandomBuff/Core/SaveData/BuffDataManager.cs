using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CoralBrain;
using JetBrains.Annotations;
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
        public List<BuffID> GetAllBuffIds()
        {
            SlugcatStats.Name name;
            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
                name = game.StoryCharacter;
            else
                name = RainWorld.lastActiveSaveSlot;
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
    public sealed partial class BuffDataManager
    {


        private BuffDataManager()
        {
        }

        internal static bool LoadData(string file,string formatVersion)
        {
            Instance = new BuffDataManager();
            return Instance.InitStringData(file,formatVersion);
        }

        internal void DeleteSaveData(SlugcatStats.Name name)
        {
            if (name != null && allDatas.ContainsKey(name))
            {
                BuffPlugin.Log($"DELETE SAVE DATA: {name}");
                allDatas.Remove(name);
            }
        }



        internal void DeleteAll()
        {
            BuffPlugin.Log($"DELETE ALL SAVE DATA");
            allDatas.Clear();
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
                    BuffPlugin.Log($"Add new buff data. ID: {id}, Character :{name}");

                }
                else
                    return null;
            }

            return allDatas[name][id];
        }

        /// <summary>
        /// 轮回结束时更新 通过BuffPoolManager调用
        /// </summary>
        /// <param name="name"></param>
        internal void WinGame(BuffPoolManager manager)
        {
            var name = manager.Game.StoryCharacter;
            foreach (var data in allDatas[name])
                data.Value.CycleEnd();
        }

        /// <summary>
        /// 移除存档内BuffData
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal void RemoveBuffData(BuffID id)
        {
            SlugcatStats.Name name;
            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
                name = game.StoryCharacter;
            else
                name = RainWorld.lastActiveSaveSlot;

            if (!allDatas.ContainsKey(name))
            {
                BuffPlugin.LogError($"Try remove buff at null slot: {name}");
                return;
            }

            if (!allDatas[name].ContainsKey(id))
            {
                BuffPlugin.LogError($"remove buff not found: {id}");
                return;
            }
            BuffPlugin.LogError($"Remove buff : {name}");

            allDatas[name].Remove(id);
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
        /// 初始化存档信息
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private bool InitStringData(string file, string formatVersion)
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
                var slugName = (SlugcatStats.Name)ExtEnumBase.Parse(typeof(SlugcatStats.Name), catSplit[0], true);

                //不存在的猫名字
                if (slugName == null)
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
                        Debug.LogException(e);
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
                        Debug.LogException(e);
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
            return builder.ToString();
        }

        /// <summary>
        /// 该存档槽下全部猫的存档数据
        /// </summary>
        private readonly Dictionary<SlugcatStats.Name, Dictionary<BuffID, BuffData>> allDatas = new();

        /// 如果被卸载导致slugcat name或data缺失，则暂时储存在此处
        private readonly List<string> ukSlugcatDatas = new();
        private readonly Dictionary<SlugcatStats.Name, List<string>> ukBuffDatas = new();

        private const string CatSplit = "<BuA>";
        private const string CatIdSplit = "<BuAI>";

        private const string BuffSplit = "<BuB>";
        private const string BuffIdSplit = "<BuBI>";


    }


}
