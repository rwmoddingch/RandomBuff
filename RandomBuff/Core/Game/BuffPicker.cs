
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;
using UnityEngine;

namespace RandomBuff.Core.Game
{

    /// <summary>
    /// 获取下次可以抽的卡
    /// 如果buff抽到了特殊那下次抽debuff把count改多就好了
    /// </summary>
    static class BuffPicker
    {
        public static List<BuffStaticData> GetNewBuffsOfType(BuffType type,SlugcatStats.Name name,int pickCount = 3)
        {
            var alreadyHas = BuffDataManager.Instance.GetDataDictionary(name).Keys.
                Where(i =>BuffConfigManager.GetStaticData(i).BuffType == type && 
                          !BuffConfigManager.GetStaticData(i).Stackable);
            var list = new List<BuffStaticData>();

          
            var copyUnique = BuffConfigManager.buffTypeTable[type].ToList();
            copyUnique.RemoveAll(alreadyHas.Contains);

            //TODO: DEBUG
            if (copyUnique.Count < pickCount)
            {
                BuffPlugin.LogWarning($"No Enough Unique Buff! Count: {pickCount}");
                copyUnique = BuffConfigManager.buffTypeTable[type];
            }

            //TODO: DEBUG
            bool canDeleteDEBUG = copyUnique.Count >= pickCount;
            if (!canDeleteDEBUG)
            {
                BuffPlugin.LogWarning($"No Enough Buff! Count: {pickCount}");
            }

            while (list.Count < pickCount)
            {
                list.Add(BuffConfigManager.GetStaticData(copyUnique[Random.Range(0, copyUnique.Count)]));
                if(canDeleteDEBUG)
                    copyUnique.Remove(list[list.Count-1].BuffID);
            }
            return list;
        }
    }
}
