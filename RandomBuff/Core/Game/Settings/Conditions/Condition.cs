using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using MonoMod.RuntimeDetour;
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
        public static ConditionID HuntAll;
        public static ConditionID Achievement;
        public static ConditionID Like;
        public static ConditionID Gourmand;
        public static ConditionID CycleScore;
        public static ConditionID Score;

        public static ConditionID MeetSS_SL;
        public static ConditionID Extermination;
        public static ConditionID FixedCycle;
        public static ConditionID WithInCycle;

        public static ConditionID Death;
        public static ConditionID ContinuousSurvival;



        static ConditionID()
        {
            Cycle = new ConditionID("Cycle", true);
            Card = new ConditionID("Card", true);
            Hunt = new ConditionID("Hunt", true);
            Achievement = new ConditionID("Achievement", true);
            Like = new ConditionID("Like", true);
            MeetSS_SL = new ConditionID("MeetSS_SL", true);
            Gourmand = new ConditionID("Gourmand", true);
            CycleScore = new ConditionID(nameof(CycleScore), true);
            Extermination = new ConditionID("Extermination", true);
            Score = new ConditionID(nameof(Score), true);
            //SaveSL = new ConditionID("SaveSL", true);
            FixedCycle = new(nameof(FixedCycle), true);
            Death = new(nameof(Death), true);
            WithInCycle = new(nameof(WithInCycle), true);
            ContinuousSurvival = new(nameof(ContinuousSurvival),true);
            HuntAll = new(nameof(HuntAll), true);
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
        public abstract int Exp { get; }

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
        public virtual void SessionEnd(SaveState save) {}


        //在任何退出or结算时触发
        public virtual void OnDestroy() => DisableHook();

        //轮回间抽卡结算
        public virtual void GachaEnd(List<BuffID> picked, List<BuffID> allCards){}

        //设置随机条件
        //如果可以重复第二项则为已有同类型的列表
        //返回true代表可以继续选择本类型
        public abstract ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty,
            List<Condition> conditions);

        //获取进度
        public abstract string DisplayProgress(InGameTranslator translator);

        //获取显示名称
        public abstract string DisplayName(InGameTranslator translator);

        public virtual void InGameUpdate(RainWorldGame game) {}


        //游戏开始
        public virtual void EnterGame(RainWorldGame game)
        {
            BuffPlugin.LogDebug($"Enable Condition Hook for Name: {TypeName}");
            HookOn();
        }

        //添加hook，会在游戏开始后调用
        public virtual void HookOn() { }



        //禁用所有hook
        internal void DisableHook()
        {
            runtimeHooks.ForEach(i => i.Dispose());
            runtimeHooks.Clear();
            BuffPlugin.LogDebug($"Disable Condition Hook for Name: {TypeName}");
            BuffHookWarpper.DisableCondition(this);
        }


        private List<Hook> runtimeHooks = new();

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
            BuffRegister.RegisterCondition<LikeCondition>(ConditionID.Like, "Like Condition");
            BuffRegister.RegisterCondition<MeetSS_SLCondition>(ConditionID.MeetSS_SL, "Meet SS and SL");
            BuffRegister.RegisterCondition<GourmandCondition>(ConditionID.Gourmand, "Gourmand Feast");
            BuffRegister.RegisterCondition<CycleScoreCondition>(ConditionID.CycleScore, "Score Condition");

            BuffRegister.RegisterCondition<HuntCondition>(ConditionID.Hunt, "Hunt Condition");
            BuffRegister.RegisterCondition<HuntAllCondition>(ConditionID.HuntAll, "Hunt All Condition");

            BuffRegister.RegisterCondition<ExterminationCondition>(ConditionID.Extermination, "Extermination Condition");
            BuffRegister.RegisterCondition<ScoreCondition>(ConditionID.Score, "Score Condition");
            BuffRegister.RegisterCondition<DeathCondition>(ConditionID.Death, "Death");
            BuffRegister.RegisterCondition<ContinuousSurvivalCondition>(ConditionID.ContinuousSurvival, "Continuous Survival");

            BuffRegister.RegisterCondition<FixedCycleCondition>(ConditionID.FixedCycle, "Fix Cycles", true);
            BuffRegister.RegisterCondition<WithInCycleCondition>(ConditionID.WithInCycle, "WithIn Cycle", true);
        }

        public enum ConditionState
        {
            Ok_More,
            Ok_NoMore,
            Fail,
            Fail_Tmp,
        }
    }
}
