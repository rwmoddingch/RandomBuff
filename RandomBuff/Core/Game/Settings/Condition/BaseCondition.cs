using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using RandomBuff.Core.Entry;

namespace RandomBuff.Core.Game.Settings.Condition
{
    /// <summary>
    /// 结算条件ID
    /// </summary>
    public class ConditionID : ExtEnum<ConditionID>
    {
        public static ConditionID Cycle;
        static ConditionID()
        {
            Cycle = new ConditionID("Cycle", true);
            
        }

        public ConditionID(string value, bool register = false) : base(value, register)
        {
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class BaseCondition
    {
        public abstract ConditionID ID {get;}

        [JsonProperty]
        public bool Finished { get; protected set; }

        //轮回结束结算
        public abstract void SessionEnd(SaveState save);

        //设置随机条件
        public abstract void SetRandomParameter(float difficulty);

        public virtual void InGameUpdate(RainWorldGame game) {}


        internal static void Init()
        {
            BuffRegister.RegisterCondition<CycleCondition>(ConditionID.Cycle);
        }
    }
}
