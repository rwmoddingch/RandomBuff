using System.Collections.Generic;
using System.Linq;
using Expedition;
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
        public virtual void EnterGame(RainWorldGame game)
        {
            On.WorldLoader.GeneratePopulation += WorldLoader_GeneratePopulation;
            if (game.GetStorySession.saveState.cycleNumber == 0)
                BoostCreatureSpawn(game, game.world);


        }



        /// <summary>
        /// 在任何删除时候触发
        /// </summary>
        public virtual void OnDestroy()
        {
            On.WorldLoader.GeneratePopulation -= WorldLoader_GeneratePopulation;

        }

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
                if (ChallengeTools.creatureNames == null)
                    ChallengeTools.CreatureName(ref ChallengeTools.creatureNames);
                var str = string.Empty;
                if (PocketPackMultiply == 0)
                {
                    str = string.Format(BuffResourceString.Get("GachaTemplate_Detail_Base_NoFreePick") + "<ENTRY>",
                        ExpMultiply,
                        BuffResourceString.Get(CanStackByPassage
                            ? "GachaTemplate_Detail_Base_Yes"
                            : "GachaTemplate_Detail_Base_No"));
                }
                else
                {
                    str = string.Format(BuffResourceString.Get("GachaTemplate_Detail_Base") + "<ENTRY>", ExpMultiply,
                        PocketPackMultiply,
                        BuffResourceString.Get(CanStackByPassage
                            ? "GachaTemplate_Detail_Base_Yes"
                            : "GachaTemplate_Detail_Base_No"));
                }
              
                if (boostCreatureInfos is { Count: > 0 })
                {
                    var names = " ";
                    HashSet<string> already = new();
                    for (int i = 0; i < boostCreatureInfos.Count; i++)
                    {
                        if(already.Contains(ChallengeTools.creatureNames[boostCreatureInfos[i].boostCrit.index]))
                            continue;
                        already.Add(ChallengeTools.creatureNames[boostCreatureInfos[i].boostCrit.index]);
                        names += (i == 0 ? "" : ", ") +
                                 ChallengeTools.creatureNames[boostCreatureInfos[i].boostCrit.index];
                    }

                    str += "<ENTRY>" + BuffResourceString.Get("GachaTemplate_Detail_Base_Boost") + names + "<ENTRY>";
                }

                return str;
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

        /// <summary>
        /// 是否能消耗通行证来堆叠
        /// </summary>
        [JsonProperty]
        public bool CanStackByPassage = true;


        /// <summary>
        /// 补充生物生成
        /// </summary>
        [JsonProperty]
        public List<BoostCreatureInfo> boostCreatureInfos = new();

        public class CachaPacket
        {
            public (int selectCount, int showCount, int pickTimes) positive;
            public (int selectCount, int showCount, int pickTimes) negative;

            public bool NeedMenu => positive.pickTimes*positive.selectCount + negative.pickTimes * negative.selectCount != 0;
        }


        /// <summary>
        /// 生物生成扩展
        /// </summary>
        public class BoostCreatureInfo
        {
            public enum BoostType
            {
                Replace,
                Add,
            }

            public BoostType boostType;
            public int boostCount;

            public CreatureTemplate.Type boostCrit;
            public CreatureTemplate.Type baseCrit;

        }


        private void WorldLoader_GeneratePopulation(On.WorldLoader.orig_GeneratePopulation orig, WorldLoader self, bool fresh)
        {
            orig(self, fresh);
            BoostCreatureSpawn(self.game, self.world);
        }

        private void BoostCreatureSpawn(RainWorldGame self, World world)
        {
            boostCreatureInfos ??= new List<BoostCreatureInfo>();
            BuffPlugin.Log($"Try Boost creatures, info count:{boostCreatureInfos.Count}");

            foreach (var info in boostCreatureInfos)
            {
                BuffPlugin.LogDebug($"{info.baseCrit}, {info.boostCrit}, {info.boostType}, {info.boostCount}");
                switch (info.boostType)
                {
                    case BoostCreatureInfo.BoostType.Add:
                        foreach (var room in world.abstractRooms.Append(world.offScreenDen))
                        {
                            foreach (var crit in room.entities.OfType<AbstractCreature>().ToList())
                            {
                                if (crit.creatureTemplate.type == info.baseCrit)
                                {
                                    BuffPlugin.LogDebug($"Add {info.boostCrit}, at room: {room.name}");
                                    for (int i = 0; i < info.boostCount; i++)
                                        room.AddEntity(new AbstractCreature(room.world,
                                            StaticWorld.GetCreatureTemplate(info.boostCrit), null, crit.pos,
                                            self.GetNewID(-1)));
                                }
                            }

                            foreach (var crit in room.entitiesInDens.OfType<AbstractCreature>().ToList())
                            {
                                if (crit.creatureTemplate.type == info.baseCrit)
                                {
                                    BuffPlugin.LogDebug($"Add {info.boostCrit}, at room: {room.name}");

                                    for (int i = 0; i < info.boostCount; i++)
                                        room.MoveEntityToDen(new AbstractCreature(room.world,
                                            StaticWorld.GetCreatureTemplate(info.boostCrit), null, crit.pos,
                                            self.GetNewID(-1)));
                                }
                            }
                        }

                        break;
                    case BoostCreatureInfo.BoostType.Replace:
                        foreach (var room in world.abstractRooms.Append(world.offScreenDen))
                        {
                            foreach (var crit in room.entities.OfType<AbstractCreature>().ToList())
                            {
                                if (crit.creatureTemplate.type == info.baseCrit)
                                {
                                    BuffPlugin.LogDebug(
                                        $"Replace {crit.creatureTemplate.type} to {info.boostCrit}, at room: {room.name}");
                                    room.RemoveEntity(crit);
                                    room.AddEntity(new AbstractCreature(room.world,
                                        StaticWorld.GetCreatureTemplate(info.boostCrit), null, crit.pos, crit.ID));
                                }
                            }

                            foreach (var crit in room.entitiesInDens.OfType<AbstractCreature>().ToList())
                            {
                                if (crit.creatureTemplate.type == info.baseCrit)
                                {
                                    BuffPlugin.LogDebug(
                                        $"Replace {crit.creatureTemplate.type} to {info.boostCrit}, at room: {room.name}");
                                    room.RemoveEntity(crit);
                                    room.MoveEntityToDen(new AbstractCreature(room.world,
                                        StaticWorld.GetCreatureTemplate(info.boostCrit), null, crit.pos, crit.ID));
                                }
                            }
                        }

                        break;
                }
            }
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
