using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;

namespace RandomBuff
{
    public static class BuffCore
    {
        /// <summary>
        /// 获取ID对应的Buff
        /// </summary>
        /// <param name="id"></param>
        /// <param name="buff"></param>
        /// <returns></returns>
        public static bool TryGetBuff(BuffID id, out IBuff buff)
        {
            buff = null;
            if (BuffPoolManager.Instance != null)
                return BuffPoolManager.Instance.TryGetBuff(id, out buff);
            return false;
        }

        /// <summary>
        /// 获取ID对应的Buff
        /// </summary>
        /// <typeparam name="TBuff"></typeparam>
        /// <param name="id"></param>
        /// <param name="buff"></param>
        /// <returns></returns>
        public static bool TryGetBuff<TBuff>(BuffID id, out TBuff buff) where TBuff : IBuff
        {
            buff = default;
            if (TryGetBuff(id, out var iBuff))
            {
                buff = (TBuff)iBuff;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取ID对应的Buff,可能为空
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static IBuff GetBuff(this BuffID id)
        {
            if (BuffPoolManager.Instance != null)
                return BuffPoolManager.Instance.GetBuff(id);
            return null;
        }

        /// <summary>
        /// 获取ID对应的Buff,可能为空
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TBuff GetBuff<TBuff>(this BuffID id) where TBuff : IBuff
        {
            if (BuffPoolManager.Instance != null)
                return (TBuff)BuffPoolManager.Instance.GetBuff(id);
            return default;
        }

        /// <summary>
        /// 获取ID对应的BuffData,可能为空
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static BuffData GetBuffData(this BuffID id)
        {
            if (BuffPoolManager.Instance != null)
                return BuffPoolManager.Instance.GetBuffData(id);
            return BuffDataManager.Instance.GetBuffData(id);
        }



        /// <summary>
        /// 获取ID对应的BuffData,可能为空
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TData GetBuffData<TData>(this BuffID id) where TData : BuffData
        {
            if (BuffPoolManager.Instance != null)
                return (TData)BuffPoolManager.Instance.GetBuffData(id);
            return (TData)BuffDataManager.Instance.GetBuffData(id);
        }

        /// <summary>
        /// 获取当前猫的BuffList
        /// 可能为空
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static List<BuffID> GetAllBuffIds()
        {
            if (BuffPoolManager.Instance != null)
                return BuffPoolManager.Instance.GetAllBuffIds();
            return null;
        }

        internal static List<BuffID> GetAllBuffIds(SlugcatStats.Name name)
        {
            if (BuffPoolManager.Instance != null && BuffPoolManager.Instance.Game.StoryCharacter == name)
                return BuffPoolManager.Instance.GetAllBuffIds();
            return BuffDataManager.Instance.GetAllBuffIds(name);
        }

        public static bool BuffMode(this RainWorld rainWorld)
        {
            return rainWorld.options.saveSlot >= 100;
        }

        internal static BuffStaticData GetStaticData(this BuffID id)
        {
            return BuffConfigManager.GetStaticData(id);
        }

    }
}
