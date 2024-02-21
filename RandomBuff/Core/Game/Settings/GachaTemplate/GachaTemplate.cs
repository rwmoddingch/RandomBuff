using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game.Settings.GachaTemplate;

namespace RandomBuff.Core.Game
{
    /// <summary>
    /// 抽卡模式ID
    /// </summary>
    public class GachaTemplateID : ExtEnum<GachaTemplateID>
    {
        public static GachaTemplateID Normal;
        public static GachaTemplateID Quick;
        static GachaTemplateID()
        {
            Normal = new GachaTemplateID("Normal", true);
            Quick = new GachaTemplateID("Quick", true);
        }

        public GachaTemplateID(string value, bool register = false) : base(value, register)
        {
        }
    }

    /// <summary>
    /// 负责控制游戏模式
    /// 基类
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract partial class GachaTemplate
    {
        protected GachaTemplate()
        {
        }


        /// <summary>
        /// 是否需要随机出生点
        /// </summary>
        public virtual bool NeedRandomStart => false;

        public abstract GachaTemplateID ID { get; }

        /// <summary>
        /// 创建新游戏时触发
        /// </summary>
        public abstract void NewGame();


        /// <summary>
        /// 轮回结束时触发
        /// </summary>
        public abstract void SessionEnd();

        /// <summary>
        /// 游戏进行时update触发
        /// </summary>
        public virtual void InGameUpdate(RainWorldGame game) {}

        /// <summary>
        /// 每轮回进入游戏时触发
        /// </summary>
        public virtual void EnterGame() {}


        /// <summary>
        /// 当前的抽卡信息
        /// </summary>
        [JsonProperty]
        public CachaPacket CurrentPacket { get; protected set; } = new ();

        public class CachaPacket
        {
            public (int selectCount, int showCount, int pickTimes) positive;
            public (int selectCount, int showCount, int pickTimes) negative;
            public bool isEnd;

            public bool NeedMenu => positive.pickTimes + negative.pickTimes != 0;
        }
    }

    public abstract partial class GachaTemplate
    {

        internal static void Init()
        {
            BuffRegister.RegisterGachaTemplate<NormalGachaTemplate>(GachaTemplateID.Normal);
            BuffRegister.RegisterGachaTemplate<QuickGachaTemplate>(GachaTemplateID.Quick);
        }
    }

}
