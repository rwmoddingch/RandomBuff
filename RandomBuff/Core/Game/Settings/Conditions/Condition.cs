using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using RandomBuff.Core.Entry;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    /// <summary>
    /// 结算条件ID
    /// </summary>
    public class ConditionID : ExtEnum<ConditionID>
    {
        public static ConditionID Cycle;
        public static ConditionID Card;
        public static ConditionID Hunt;

        static ConditionID()
        {
            Cycle = new ConditionID("Cycle", true);
            Card = new ConditionID("Card", true);
            Hunt = new ConditionID("Hunt", true);

        }

        public ConditionID(string value, bool register = false) : base(value, register)
        {
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class Condition
    {
        public abstract ConditionID ID {get;}

        public bool Finished
        {
            get => finished;
            protected set
            {
                if (finished != value)
                {
                    finished = value;
                    if (finished)
                        onCompleted?.Invoke(this);
                    else
                        onUncompleted?.Invoke(this);
                }
            }
        }

        [JsonProperty] 
        private bool finished;

        //当成功完成时触发
        private Action<Condition> onCompleted;

        //当撤回完成时触发
        private Action<Condition> onUncompleted;

        //当文本更改时触发
        protected Action<Condition> onLabelRefresh;

        //轮回结束结算
        public abstract void SessionEnd(SaveState save);

        //设置随机条件
        public abstract void SetRandomParameter(float difficulty);

        //获取进度
        public abstract string DisplayProgress(InGameTranslator translator);

        //获取显示名称
        public abstract string DisplayName(InGameTranslator translator);

        public virtual void InGameUpdate(RainWorldGame game) {}


        //游戏开始
        public virtual void EnterGame(RainWorldGame game){}


        //绑定状态更新
        public void BindHudFunction(Action<Condition> hudCompleted, Action<Condition> hudUncompleted, Action<Condition> hudLabelRefreshed)
        {
            onCompleted += hudCompleted;
            onUncompleted += hudUncompleted;
            onLabelRefresh += hudLabelRefreshed;
        }

        //解绑状态更新
        public void UnbindHudFunction(Action<Condition> hudCompleted, Action<Condition> hudUncompleted, Action<Condition> hudLabelRefreshed)
        {
            onCompleted -= hudCompleted;
            onUncompleted -= hudUncompleted;
            onLabelRefresh -= hudLabelRefreshed;
        }


        internal static void Init()
        {
            BuffRegister.RegisterCondition<CycleCondition>(ConditionID.Cycle, "Cycle Condition");
            BuffRegister.RegisterCondition<CardCondition>(ConditionID.Card, "Card Condition");
            //BuffRegister.RegisterCondition<HuntCondition>(ConditionID.Hunt, "Hunt Condition");

        }
    }
}
