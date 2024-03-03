using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public static void LoadBuffPlayerData(string rawData)
        {
            try
            {
                var newData = JsonConvert.DeserializeObject<BuffPlayerData>(rawData) ?? new BuffPlayerData();
                newData.keyBindData ??= new Dictionary<string, string>();
                newData.collectData ??= new List<string>();
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
            if(keyBindData.ContainsKey(buffId.value))
                keyBindData[buffId.value] = keyBind;
            else
                keyBindData.Add(buffId.value, keyBind);
        }


        private List<string> collectData = new();

        public float playerTotExp = 0;

        private Dictionary<string, string> keyBindData = new();
    }
}
