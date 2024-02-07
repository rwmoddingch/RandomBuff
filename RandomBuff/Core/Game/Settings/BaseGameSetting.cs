using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Game.Settings;

namespace RandomBuff.Core.Game
{
    /// <summary>
    /// 游戏模式ID
    /// </summary>
    internal class BuffSettingID : ExtEnum<BuffSettingID>
    {
        public static BuffSettingID Normal;
        public static BuffSettingID Quick;
        static BuffSettingID()
        {
            Normal = new BuffSettingID("Normal", true);
            Quick = new BuffSettingID("Quick", true);
        }

        public BuffSettingID(string value, bool register = false) : base(value, register)
        {
        }
    }

    /// <summary>
    /// 负责控制游戏模式
    /// 基类
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    internal abstract partial class BaseGameSetting
    {
        protected BaseGameSetting()
        {
        }

        public abstract BuffSettingID ID { get; }

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
        public CachaPacket CurrentPacket { get; protected set; }

        public class CachaPacket
        {
            public (int selectCount, int showCount, int pickTimes) positive;
            public (int selectCount, int showCount, int pickTimes) negative;
            public bool isEnd;

            public bool NeedMenu => positive.pickTimes + negative.pickTimes != 0;
        }
    }

    internal abstract partial class BaseGameSetting
    {
        public static readonly Dictionary<BuffSettingID, Type> settingDict = new ();

        public static void Init()
        {
            settingDict.Add(BuffSettingID.Normal, typeof(NormalGameSetting));
            settingDict.Add(BuffSettingID.Quick, typeof(QuickGameSetting));

        }
    }

}
