using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using UnityEngine;
using System.Runtime.CompilerServices;
using RandomBuff.Core.Game;
using RandomBuff.Core.Progression;
using RandomBuff.Core.Progression.Quest;
using RandomBuff.Core.Progression.Record;
using static RandomBuff.Render.UI.Component.RandomBuffFlag;
using System.Reflection;

namespace RandomBuff.Core.SaveData
{
    internal class BuffPlayerData
    {
        protected BuffPlayerData()
        {
            Instance = this;
        }

        protected BuffPlayerData(string file, BuffFormatVersion formatVersion) 
        {
            var split = Regex.Split(file, PlayerDataSplit)
                .Where(i => !string.IsNullOrEmpty(i)).ToArray();
            foreach (var item in split)
            {
                var dataSplit = Regex.Split(item, PlayerDataSubSplit);
                if (dataSplit.Length <= 1)
                {
                    BuffPlugin.LogError($"Corrupted PlayerData at :{item}");
                    continue;
                }

                switch (dataSplit[0])
                {
                    case "COLLECT":
                        collectData = JsonConvert.DeserializeObject<List<string>>(dataSplit[1]);
                        break;
                    case "KEYBIND":
                        keyBindData = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataSplit[1]);
                        break;
                    case "EXP":
                        playerTotExp = int.Parse(dataSplit[1]);
                        break;
                    case "QUEST":
                        finishedQuest = JsonConvert.DeserializeObject<HashSet<string>>(dataSplit[1]);
                        break;
                    case "TOTCARDS":
                        try
                        {
                            SlotRecord = JsonConvert.DeserializeObject<SlotRecord>(dataSplit[1]);
                        }
                        catch (Exception e)
                        {
                            SlotRecord = new SlotRecord();
                        }
                     
                        break;
                    case "COSMETIC":
                        enableCosmetics = JsonConvert.DeserializeObject<HashSet<string>>(dataSplit[1]);
                        break;
                    case "QUESTSTATES":
                        questStates = JsonConvert.DeserializeObject<Dictionary<string, HashSet<int>>>(dataSplit[1]);
                        break;
                    case "MISSION":
                        finishedMission = JsonConvert.DeserializeObject<HashSet<string>>(dataSplit[1]);
                        break;
                    default:
                        unrecognizedSaveStrings.Add(item);
                        break;
                }
            }
            Instance = this;

            BuffPlugin.Log($"Completed loaded player data");
            var str = "[RECORD]: ";
            foreach (var item in SlotRecord.GetValueDictionary())
                str += $"{{{item.Key},{item.Value}}},";
            BuffPlugin.Log(str);

            foreach(var quest in GetAllCompleteQuests())
                BuffPlugin.LogDebug($"BuffQuest: finished quest {quest}");
        }

        internal string ToStringData()
        {
            StringBuilder builder = new();
            builder.Append($"COLLECT{PlayerDataSubSplit}{JsonConvert.SerializeObject(collectData)}{PlayerDataSplit}");
            builder.Append($"KEYBIND{PlayerDataSubSplit}{JsonConvert.SerializeObject(keyBindData)}{PlayerDataSplit}");
            builder.Append($"EXP{PlayerDataSubSplit}{playerTotExp}{PlayerDataSplit}");
            builder.Append($"QUEST{PlayerDataSubSplit}{JsonConvert.SerializeObject(finishedQuest)}{PlayerDataSplit}");
            builder.Append($"QUESTSTATES{PlayerDataSubSplit}{JsonConvert.SerializeObject(questStates)}{PlayerDataSplit}");

            builder.Append($"TOTCARDS{PlayerDataSubSplit}{JsonConvert.SerializeObject(SlotRecord)}{PlayerDataSplit}");
            builder.Append($"COSMETIC{PlayerDataSubSplit}{JsonConvert.SerializeObject(enableCosmetics)}{PlayerDataSplit}");
            builder.Append($"MISSION{PlayerDataSubSplit}{JsonConvert.SerializeObject(finishedMission)}{PlayerDataSplit}");

            foreach (var item in unrecognizedSaveStrings)
                builder.Append($"{item}{PlayerDataSplit}");
            return builder.ToString();
        }

        public static void LoadBuffPlayerData(string rawData, BuffFormatVersion formatVersion)
        {
            try
            {
                _ = new BuffPlayerData(rawData, formatVersion);

            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e, "Exception In BuffPlayerData:LoadBuffPlayerData");
                _ = new BuffPlayerData();
            }
      
        }

        public static BuffPlayerData Instance { get; private set; }

        /// <summary>
        /// 添加新的BuffID
        /// </summary>
        /// <param name="buffId"></param>
        public void AddCollect(BuffID buffId)
        {
            if (!collectData.Contains(buffId.value))
            {
                BuffPlugin.Log($"Add buff:{buffId} collect to Save Slot");
                collectData.Add(buffId.value);
            }
        }

        /// <summary>
        /// 判断对应ID是否在收藏
        /// </summary>
        /// <param name="buffId"></param>
        /// <returns></returns>
        public bool ContainsCollect(BuffID buffId)
        {
            return collectData.Contains(buffId.value);
        }
        
        /// <summary>
        /// 获取所有可用的收集过的BuffID
        /// </summary>
        /// <returns></returns>
        public List<BuffID> GetAllCollect()
        {
            return collectData.Select(i => new BuffID(i)).Where(BuffConfigManager.ContainsId).ToList();
        }

        /// <summary>
        /// 是否已经收集过
        /// </summary>
        /// <param name="buffId"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsCollected(BuffID buffId) => collectData.Contains(buffId.value) || BuffPlugin.AllCardDisplay;

        /// <summary>
        /// 获取按键绑定，若不存在则返回KeyCode.None.ToString()
        /// </summary>
        /// <param name="buffId"></param>
        /// <returns></returns>
        public string GetKeyBind(BuffID buffId)
        {
            if(keyBindData.ContainsKey(buffId.value)) return keyBindData[buffId.value];
            return KeyCode.None.ToString();
        }

        /// <summary>
        /// 设置按键绑定
        /// </summary>
        /// <param name="buffId"></param>
        /// <param name="keyBind"></param>
        public void SetKeyBind(BuffID buffId, string keyBind)
        {
            InternalSetKeyBind(buffId.value, keyBind);
        }

        void InternalSetKeyBind(string id, string keyBind)
        {
            if (keyBind != KeyCode.None.ToString())//清除重复的绑定
            {
                foreach (var bind in keyBindData.ToList())
                {
                    if (bind.Value == keyBind)
                        InternalSetKeyBind(bind.Key, KeyCode.None.ToString());
                }
            }

            if (keyBindData.ContainsKey(id))
                keyBindData[id] = keyBind;
            else
                keyBindData.Add(id, keyBind);
        }

        /// <summary>
        /// 判断任务是否已经完成
        /// </summary>
        /// <param name="questId"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsQuestUnlocked(string questId) => finishedQuest.Contains(questId);


        /// <summary>
        /// 更新同步任务条件状态
        /// </summary>
        /// <param name="questId"></param>
        /// <param name="index"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateQuestConditionState(string questId, int index)
        {
            if(!questStates.ContainsKey(questId))
                questStates.Add(questId,new HashSet<int>());
            if (!questStates[questId].Contains(index))
                questStates[questId].Add(index);
        }


        /// <summary>
        /// 获取对应id任务的条件完成数量
        /// </summary>
        /// <param name="questId"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetQuestConditionStateCount(string questId)
        {
            if (!questStates.ContainsKey(questId))
                questStates.Add(questId, new HashSet<int>());
            return questStates[questId].Count;
        }


        /// <summary>
        /// 获取所有完成的任务，注意可能会包含卸载的mod内的任务
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllCompleteQuests() => finishedQuest.ToList();

        /// <summary>
        /// 更新任务状态，并返回新完成的任务list
        /// </summary>
        public List<BuffQuest> UpdateQuestState(WinGamePackage package)
        {
            List<BuffQuest> list = new ();
            foreach (var questName in BuffConfigManager.GetQuestIDList())
            {
                if(IsQuestUnlocked(questName)) continue;
                if (BuffConfigManager.GetQuestData(questName).UpdateUnlockedState(package))
                {
                    finishedQuest.Add(questName);
                    list.Add(BuffConfigManager.GetQuestData(questName));
                }

            }

            return list;
        }

        /// <summary>
        /// 获取是否启用了解锁的装饰
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsCosmeticEnable(string id) => enableCosmetics.Contains(id);


        /// <summary>
        /// 设置解锁装置的启用状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="newEnable"></param>
        public void SetCosmeticEnable(string id, bool newEnable)
        {
            if (enableCosmetics.Contains(id))
            {
                if(!newEnable) enableCosmetics.Remove(id);
            }
            else
            {
                if(newEnable) enableCosmetics.Add(id);
            }
        }

        public static int Exp2Level(int exp)
        {
            if (exp > ExpBeforeConstDelta)
            {
                return (exp - ExpBeforeConstDelta) / 400 + 10;
            }
            return Mathf.FloorToInt((-295 + Mathf.Sqrt(295f * 295f + 4f * 5f * exp)) / (2f * 5f));
        }

        public static int Level2Exp(int level)
        {
            if (level <= 10)//等差
                return /*(300 + 300 + 10 * (level - 1)) * level / 2;*/ 295 * level + 5 * (level * level);
            else
                return ExpBeforeConstDelta + (level - 10) * 400;
        }




        public int playerTotExp = 0;

        public SlotRecord SlotRecord { get; set; } = new();

        //TODO : 改进等级算法
        public int PlayerLevel => Exp2Level(playerTotExp);


        public readonly HashSet<string> finishedMission = new();



        private readonly List<string> collectData = new();

        private readonly HashSet<string> enableCosmetics = new();

        private readonly Dictionary<string, string> keyBindData = new();

        private readonly List<string> unrecognizedSaveStrings = new();

        private readonly Dictionary<string,HashSet<int>> questStates = new();

        private readonly HashSet<string> finishedQuest = new();


        private const string PlayerDataSplit = "<Bpd>";
        private const string PlayerDataSubSplit = "<BpdI>";
        private const int ExpBeforeConstDelta = 295 * 10 + 5 * (10 * 10);
    }
}
