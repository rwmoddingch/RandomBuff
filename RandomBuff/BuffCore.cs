using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
        public static bool TryGetBuff(this BuffID id, out IBuff buff)
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
        public static bool TryGetBuff<TBuff>(this BuffID id, out TBuff buff) where TBuff : IBuff
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
        /// 创建新的Buff
        /// </summary>
        /// <param name="id">待创建的BuffID</param>
        /// <param name="needStack">是否需要堆叠，非堆叠情况下尝试创建不会重复创建仅会获取到第一个</param>
        /// <returns></returns>
        public static IBuff CreateNewBuff(this BuffID id, bool needStack = true)
        {
            if (BuffPoolManager.Instance != null)
            {
                bool canShow = !id.TryGetBuff(out _);
                var re = BuffPoolManager.Instance.CreateBuff(id, needStack);
                if (canShow)
                    BuffHud.Instance.AppendNewCard(id);
                return re;
            }

            return null;
        }
        /// <summary>
        /// 创建新的Buff
        /// </summary>
        /// <param name="id">待创建的BuffID</param>
        /// <param name="needStack">是否需要堆叠，非堆叠情况下尝试创建不会重复创建仅会获取到第一个</param>
        /// <returns></returns>
        public static TBuff CreateNewBuff<TBuff>(this BuffID id, bool needStack = true) where TBuff : class, IBuff
        {
            if (BuffPoolManager.Instance != null)
            {
                bool canShow = !id.TryGetBuff(out _);
                var re = BuffPoolManager.Instance.CreateBuff(id, needStack);
                if (canShow)
                    BuffHud.Instance.AppendNewCard(id);
                return (TBuff)re;
            }
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
        /// 删除或减少卡牌叠层
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool UnstackBuff(this BuffID id)
        {
            if (BuffPoolManager.Instance != null && BuffPoolManager.Instance.TryGetBuff(id, out _))
            {
                BuffPoolManager.Instance.UnstackBuff(id);
                return true;
            }

            return false;
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


        /// <summary>
        /// 获取全部的Buff，特定猫
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static List<BuffID> GetAllBuffIds(SlugcatStats.Name name)
        {
            if (BuffPoolManager.Instance != null && BuffPoolManager.Instance.Game.StoryCharacter == name)
                return BuffPoolManager.Instance.GetAllBuffIds();
            return BuffDataManager.Instance.GetAllBuffIds(name);
        }


        /// <summary>
        /// 是否是卡牌模式
        /// </summary>
        /// <param name="rainWorld"></param>
        /// <returns></returns>
        public static bool BuffMode(this RainWorld rainWorld)
        {
            return rainWorld.options.saveSlot >= 100;
        }


        /// <summary>
        /// 获取静态信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static BuffStaticData GetStaticData(this BuffID id)
        {
            return BuffConfigManager.GetStaticData(id);
        }



        /// <summary>
        /// 在游戏内请求抽卡
        /// </summary>
        /// <param name="pickedCallBack"></param>
        /// <param name="buffs"></param>
        /// <param name="selectNumber"></param>
        /// <returns></returns>
        internal static bool RequestPickerInGame(Action<BuffID> pickedCallBack, List<(BuffID major, BuffID additive)> buffs, int selectNumber)
        {
            if (BuffHud.Instance == null || 
                buffs.Any(i => !BuffConfigManager.ContainsId(i.major) || (i.additive!=null && !BuffConfigManager.ContainsId(i.additive))))
                return false;
            BuffHud.Instance.RequestNewPick(pickedCallBack, buffs, selectNumber);

            return true;
        }

    }
}
