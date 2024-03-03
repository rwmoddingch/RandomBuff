using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using RandomBuff.Core.Buff;
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
        public static ConditionID Achievement;
        static ConditionID()
        {
            Cycle = new ConditionID("Cycle", true);
            Card = new ConditionID("Card", true);
            //Hunt = new ConditionID("Hunt", true);
            Achievement = new ConditionID("Achievement", true);

        }

        public ConditionID(string value, bool register = false) : base(value, register)
        {
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class Condition
    {
        public abstract ConditionID ID { get; }

        /// <summary>
        /// 完成获取的经验值
        /// </summary>
        public abstract float Exp { get; }

        /// <summary>
        /// 条件种类名
        /// </summary>
        public string TypeName => BuffRegister.GetConditionTypeName(ID);

        /// <summary>
        /// 条件是否完成
        /// </summary>
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
        //记得调用————
        protected Action<Condition> onLabelRefresh;

        //轮回结束结算
        public virtual void SessionEnd(SaveState save){}

        //轮回间抽卡结算
        public virtual void GachaEnd(List<BuffID> picked, List<BuffID> allCards){}

        //设置随机条件
        //如果可以重复第二项则为已有同类型的列表
        //返回true代表可以继续选择本类型
        public abstract bool SetRandomParameter(SlugcatStats.Name name, float difficulty,
            List<Condition> sameConditions);

        //获取进度
        public abstract string DisplayProgress(InGameTranslator translator);

        //获取显示名称
        public abstract string DisplayName(InGameTranslator translator);

        public virtual void InGameUpdate(RainWorldGame game) {}


        //游戏开始
        public virtual void EnterGame(RainWorldGame game){}


        //绑定状态更新
        internal void BindHudFunction(Action<Condition> hudCompleted, Action<Condition> hudUncompleted, Action<Condition> hudLabelRefreshed)
        {
            onCompleted += hudCompleted;
            onUncompleted += hudUncompleted;
            onLabelRefresh += hudLabelRefreshed;
        }

        //解绑状态更新
        internal void UnbindHudFunction(Action<Condition> hudCompleted, Action<Condition> hudUncompleted, Action<Condition> hudLabelRefreshed)
        {
            onCompleted -= hudCompleted;
            onUncompleted -= hudUncompleted;
            onLabelRefresh -= hudLabelRefreshed;
        }


        internal static void Init()
        {
            BuffRegister.RegisterCondition<CycleCondition>(ConditionID.Cycle, "Cycle Condition");
            BuffRegister.RegisterCondition<CardCondition>(ConditionID.Card, "Card Condition");
            BuffRegister.RegisterCondition<AchievementCondition>(ConditionID.Achievement, "Achievement Condition");
            //BuffRegister.RegisterCondition<HuntCondition>(ConditionID.Hunt, "Hunt Condition");

        }
    }
}
