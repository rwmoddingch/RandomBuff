
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Progression;
using RandomBuff.Core.SaveData;
using UnityEngine;

namespace RandomBuff.Core.Game
{

    /// <summary>
    /// 获取下次可以抽的卡
    /// 如果buff抽到了特殊那下次抽debuff把count改多就好了
    /// 返回值为空证明库存不足
    /// </summary>
    static class BuffPicker
    {
        public static List<BuffStaticData> GetNewBuffsOfType(SlugcatStats.Name name,int pickCount ,params BuffType[] types)
        {
            IEnumerable<BuffID> alreadyHas;
            if (BuffPoolManager.Instance == null)
            {
                alreadyHas = BuffDataManager.Instance.GetDataDictionary(name).Keys.Where(i =>
                    types.Contains(BuffConfigManager.GetStaticData(i).BuffType) &&
                    !BuffConfigManager.GetStaticData(i).Stackable);
            }
            else
            {
                alreadyHas = BuffPoolManager.Instance.GetDataDictionary().Keys.Where(i =>
                    types.Contains(BuffConfigManager.GetStaticData(i).BuffType) &&
                    !BuffConfigManager.GetStaticData(i).Stackable);
            }

            var list = new List<BuffStaticData>();

            var copyUnique = new List<BuffID>();
            foreach(var type in types)
                copyUnique.AddRange(BuffConfigManager.buffTypeTable[type].ToList());
            copyUnique.RemoveAll(alreadyHas.Contains);
            copyUnique.RemoveAll(i => BuffConfigManager.GetStaticData(i).NeedUnlocked && !BuffPlayerData.Instance.IsCollected(i));
            copyUnique.RemoveAll(i => BuffConfigManager.IsItemLocked(QuestUnlockedType.Card,i.value));// 去除未解锁
            if (copyUnique.Count < pickCount)
            {
                return null;
            }

        

            while (list.Count < pickCount)
            {
                list.Add(BuffConfigManager.GetStaticData(copyUnique[Random.Range(0, copyUnique.Count)]));
                copyUnique.Remove(list[list.Count-1].BuffID);
            }
            return list;
        }
    }
}
