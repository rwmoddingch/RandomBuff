using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;

namespace RandomBuff.Core.Buff
{
    /// <summary>
    /// Buff的ID，和内置的ExtEnum用法相同。
    /// </summary>
    public class BuffID : ExtEnum<BuffID>
    {
        public static BuffID None;
        static BuffID()
        {
            None = new BuffID("None", true);
        }

        public BuffID(string value, bool register = false) : base(value, register)
        {
        }
    }

    public enum BuffType
    {
        Positive,
        Negative,
        Duality
    }

    public enum BuffProperty
    {
        Normal,
        Special
    }
    public static class BuffExt
    {
        public static BuffData GetData(this BuffID id)
        {
            if (BuffPoolManager.Instance != null)
                return BuffPoolManager.Instance.GetBuffData(id);
            return BuffDataManager.Instance.GetBuffData(id);
        }

        public static TBuff GetBuff<TBuff>(this BuffID id) where TBuff : IBuff
        {
            return (TBuff)BuffPoolManager.Instance.GetBuff(id);
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


