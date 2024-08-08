
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Progression;
using RandomBuff.Core.Progression.Quest;
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
#if TESTVERSION
            if (BuffPlugin.DevEnabled)
            {
                var str = "";
                foreach(var type in types)
                    str +=type.ToString() + ", ";
                BuffPlugin.LogDebug($"Request pick: {str}");
            }
            else
                BuffPlugin.LogDebug($"Request pick");
#endif
            if (types.Any(i => i == BuffType.Duality))
            {
                types = types.Where(i => i != BuffType.Duality).ToArray();
                BuffPlugin.LogError("Now buff picker don't use Duality");
            }


            IEnumerable<BuffID> alreadyHas;

            var conflict = new List<string>();
            if (BuffPoolManager.Instance == null)
            {
                alreadyHas = BuffDataManager.Instance.GetDataDictionary(name).Keys.Where(i => 
                    types.Contains(i.GetStaticData().BuffType) || i.GetStaticData().BuffType == BuffType.Duality);
            }
            else
            {
                alreadyHas = BuffPoolManager.Instance.GetDataDictionary().Keys.Where(i =>
                    types.Contains(i.GetStaticData().BuffType) || i.GetStaticData().BuffType == BuffType.Duality);
            }

            foreach (var id in alreadyHas)
                conflict.AddRange(id.GetStaticData().Conflict);

#if TESTVERSION
            foreach (var id in conflict)
                BuffPlugin.LogDebug($"Conflict: {id}");

#endif
            var alreadyHasNoStackable = alreadyHas.Where(i => !i.GetStaticData().Stackable);

            var list = new List<BuffStaticData>();
            var copyUnique = new List<BuffID>();
            foreach (var type in types)
            {
                copyUnique.AddRange(BuffConfigManager.buffTypeTable[type].ToArray());
                copyUnique.AddRange(BuffConfigManager.buffTypeTable[BuffType.Duality].Where(i => i.GetStaticData().AsPositive == (type == BuffType.Positive)));
            }

            copyUnique.RemoveAll(alreadyHasNoStackable.Contains);
            copyUnique.RemoveAll(i =>i.GetStaticData().Hidden && !BuffPlayerData.Instance.IsCollected(i));


            copyUnique.RemoveAll(i => BuffConfigManager.IsItemLocked(QuestUnlockedType.Card,i.value));// 去除未解锁

            copyUnique.RemoveAll(i => !alreadyHas.Contains(i) && (conflict.Contains(i.value) || conflict.Any(j => i.GetStaticData().Tag.Contains(j))));//去除冲突（类别或名称）
            copyUnique.RemoveAll(i => i.GetStaticData().Conflict.Contains(name.value));//对应猫

            copyUnique.RemoveAll(i => i.GetStaticData().Conflict.Any(c => ModManager.ActiveMods.Any(mod => mod.id == c))); //去除mod
            if (copyUnique.Count < pickCount)
            {
                BuffPlugin.LogError($"No enough card!!!");
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
