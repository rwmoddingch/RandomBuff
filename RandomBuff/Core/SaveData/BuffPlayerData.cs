using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using UnityEngine;

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
                        playerTotExp = float.Parse(dataSplit[1]);
                        break;
                    default:
                        unrecognizedSaveStrings.Add(item);
                        break;
                }
            }
            BuffPlugin.Log("Completed loaded player data");

        }

        internal string ToStringData()
        {
            StringBuilder builder = new();
            builder.Append($"COLLECT{PlayerDataSubSplit}{JsonConvert.SerializeObject(collectData)}{PlayerDataSplit}");
            builder.Append($"KEYBIND{PlayerDataSubSplit}{JsonConvert.SerializeObject(keyBindData)}{PlayerDataSplit}");
            builder.Append($"EXP{PlayerDataSubSplit}{playerTotExp}{PlayerDataSplit}");

            foreach(var item in unrecognizedSaveStrings)
                builder.Append($"{item}{PlayerDataSplit}");
            return builder.ToString();
        }

        public static void LoadBuffPlayerData(string rawData, BuffFormatVersion formatVersion)
        {
            try
            {   // TODO: 正式版删除
                if (formatVersion < new BuffFormatVersion("a-0.0.5"))
                {
                    var newData = JsonConvert.DeserializeObject<BuffPlayerData>(rawData) ?? new BuffPlayerData();
                    newData.keyBindData ??= new Dictionary<string, string>();
                    newData.collectData ??= new List<string>();
                    return;
                }

                new BuffPlayerData(rawData, formatVersion);

            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e, "Exception In BuffPlayerData:LoadBuffPlayerData");
                new BuffPlayerData();
            }
      
        }

        public static BuffPlayerData Instance { get; private set; }

        public void LoadOldCollectData(string rawData)
        {
            collectData = JsonConvert.DeserializeObject<List<string>>(rawData);
        }

        /// <summary>
        /// 添加新的BuffID
        /// </summary>
        /// <param name="buffId"></param>
        public void AddCollect(BuffID buffId)
        {
            if (collectData == null)
                collectData = new List<string>();
            if (!collectData.Contains(buffId.value))
            {
                BuffPlugin.Log($"Add buff:{buffId} collect to Save Slot");
                collectData.Add(buffId.value);
            }
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
        public bool IsCollected(BuffID buffId)
        {
            return collectData.Contains(buffId.value);
        }

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
                foreach (var bind in keyBindData)
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


        private List<string> collectData = new();

        public float playerTotExp = 0;

        private Dictionary<string, string> keyBindData = new();

        private readonly List<string> unrecognizedSaveStrings = new();

        private const string PlayerDataSplit = "<Bpd>";
        private const string PlayerDataSubSplit = "<BpdI>";

    }
}
