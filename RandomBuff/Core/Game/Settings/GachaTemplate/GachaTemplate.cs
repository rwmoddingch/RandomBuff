using JetBrains.Annotations;
using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game.Settings.Conditions;

namespace RandomBuff.Core.Game.Settings.GachaTemplate
{
    /// <summary>
    /// 抽卡模式ID
    /// </summary>
    public class GachaTemplateID : ExtEnum<GachaTemplateID>
    {
        public static GachaTemplateID Normal;
        public static GachaTemplateID Quick;
        public static GachaTemplateID Mission;
        public static GachaTemplateID SandBox;
        static GachaTemplateID()
        {
            Normal = new GachaTemplateID("Normal", true);
            Quick = new GachaTemplateID("Quick", true);
            Mission = new GachaTemplateID("Mission", true);
            SandBox = new GachaTemplateID("SandBox", true);
        }

        public GachaTemplateID(string value, bool register = false) : base(value, register)
        {
        }
    }

    /// <summary>
    /// 负责控制游戏模式
    /// 基类
    /// 只能用字段，不能用属性
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract partial class GachaTemplate
    {
        protected GachaTemplate()
        {
        }

        /// <summary>
        /// 总经验的加成倍数
        /// 可以通过json更改
        /// </summary>
        [JsonProperty] 
        public float ExpMultiply = 1;


        /// <summary>
        /// 自由选卡加成
        /// </summary>
        [JsonProperty]
        public float PocketPackMultiply = 1;

        /// <summary>
        /// 是否需要随机出生点
        /// </summary>
        public virtual bool NeedRandomStart => false;


        /// <summary>
        /// 固定出生点，null为无指定（继承对应猫默认）
        /// </summary>
        [CanBeNull]
        [JsonProperty] 
        public string ForceStartPos = null;


        public abstract GachaTemplateID ID { get; }

        /// <summary>
        /// 创建新游戏时触发
        /// </summary>
        public abstract void NewGame();


        /// <summary>
        /// 轮回结束时触发
        /// </summary>
        /// <param name="game"></param>
        public abstract void SessionEnd(RainWorldGame game);

        /// <summary>
        /// 游戏进行时update触发
        /// </summary>
        public virtual void InGameUpdate(RainWorldGame game) {}

        /// <summary>
        /// 每轮回进入游戏时触发
        /// </summary>
        public virtual void EnterGame(RainWorldGame game) {}

        /// <summary>
        /// 当数据读取完成触发
        /// </summary>
        /// <returns>返回false证明数据损坏</returns>
        public virtual bool TemplateLoaded() => true;



        /// <summary>
        /// 当前抽卡模版的详情信息
        /// </summary>
        public virtual string TemplateDetail
        {
            get
            {
                if(PocketPackMultiply == 0)
                    return string.Format(BuffResourceString.Get("GachaTemplate_Detail_Base_NoFreePick") + "\n", ExpMultiply);
                return string.Format(BuffResourceString.Get("GachaTemplate_Detail_Base") + "\n", ExpMultiply,
                    PocketPackMultiply);
            }
        }

        /// <summary>
        /// 当前抽卡模版的通用介绍
        /// </summary>
        [JsonProperty]
        public string TemplateDescription = "";

        /// <summary>
        /// 当前的抽卡信息
        /// </summary>
        [JsonProperty]
        public CachaPacket CurrentPacket { get; protected set; } = new ();

        public class CachaPacket
        {
            public (int selectCount, int showCount, int pickTimes) positive;
            public (int selectCount, int showCount, int pickTimes) negative;

            public bool NeedMenu => positive.pickTimes + negative.pickTimes != 0;
        }
    }

    public abstract partial class GachaTemplate
    {

        internal static void Init()
        {
            _ = BuffID.None;
            BuffRegister.RegisterGachaTemplate<NormalGachaTemplate>(GachaTemplateID.Normal);
            BuffRegister.RegisterGachaTemplate<QuickGachaTemplate>(GachaTemplateID.Quick,ConditionID.Card);
            BuffRegister.RegisterGachaTemplate<MissionGachaTemplate>(GachaTemplateID.Mission);
            BuffRegister.RegisterGachaTemplate<SandboxGachaTemplate>(GachaTemplateID.SandBox);
        }
    }

}
