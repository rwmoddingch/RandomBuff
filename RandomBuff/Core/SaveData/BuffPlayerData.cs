using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;

namespace RandomBuff.Core.SaveData
{
    internal class BuffPlayerData
    {

        protected BuffPlayerData()
        {
        }

        public static void LoadBuffPlayerData(string rawData)
        {
            var newData = JsonConvert.DeserializeObject<BuffPlayerData>(rawData) ?? new BuffPlayerData();
            Instance = newData;
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

        private List<string> collectData = new();
        public float playerTotExp = 0;


    }
}
