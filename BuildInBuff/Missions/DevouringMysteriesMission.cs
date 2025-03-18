using BuiltinBuffs.Positive;
using RandomBuff;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Missions;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RandomBuff.Core.Entry;
using Newtonsoft.Json;
using Random = UnityEngine.Random;
using MonoMod.Cil;
using RandomBuffUtils;
using Mono.Cecil.Cil;
using System.Runtime.CompilerServices;
using MoreSlugcats;
using BuiltinBuffs.Duality;
using BuiltinBuffs.Negative;

namespace BuiltinBuffs.Missions
{
    internal class DevouringMysteriesMission : Mission, IMissionEntry
    {
        public static readonly MissionID devouringMysteriesMissionID= new MissionID("DevouringMysteries", true);

        public override MissionID ID => devouringMysteriesMissionID;

        public override SlugcatStats.Name BindSlug => null;

        public override Color TextCol => Custom.hexToColor("FF0000");

        public override string MissionName => BuffResourceString.Get("Mission_Display_DevouringMysteries");

        public DevouringMysteriesMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new EatOracleCondition(){killRequirement = 1},
                    new HuntAllCondition(){huntCount = 80},
                    new CycleScoreCondition(){targetScore = 120},
                    new CycleCondition(){SetCycle = 10},
                    new DeathCondition(){deathCount = 5},
                },

                gachaTemplate = new NormalGachaTemplate()
                {
                    ForceStartPos = "GW_S01",
                    boostCreatureInfos = new List<GachaTemplate.BoostCreatureInfo>()
                    {
                        new GachaTemplate.BoostCreatureInfo()
                        {
                            baseCrit = CreatureTemplate.Type.BlueLizard,
                            boostCrit = CreatureTemplate.Type.BlueLizard,
                            boostCount = 2,
                            boostType = GachaTemplate.BoostCreatureInfo.BoostType.Add,
                        },
                        new GachaTemplate.BoostCreatureInfo()
                        {
                            baseCrit = CreatureTemplate.Type.PinkLizard,
                            boostCrit = CreatureTemplate.Type.PinkLizard,
                            boostCount = 2,
                            boostType = GachaTemplate.BoostCreatureInfo.BoostType.Add,
                        },
                        new GachaTemplate.BoostCreatureInfo()
                        {
                            baseCrit = CreatureTemplate.Type.WhiteLizard,
                            boostCrit = CreatureTemplate.Type.WhiteLizard,
                            boostCount = 1,
                            boostType = GachaTemplate.BoostCreatureInfo.BoostType.Add,
                        },
                        new GachaTemplate.BoostCreatureInfo()
                        {
                            baseCrit = CreatureTemplate.Type.CyanLizard,
                            boostCrit = CreatureTemplate.Type.CyanLizard,
                            boostCount = 1,
                            boostType = GachaTemplate.BoostCreatureInfo.BoostType.Add,
                        }
                    },
                    PocketPackMultiply = 0,
                }
            };
            startBuffSet.Add(CorruptionShapedMutationBuffEntry.CorruptionShapedMutation);
            startBuffSet.Add(CorruptionSpreadBuffEntry.corruptionSpread);
        }

        public void RegisterMission()
        {
            BuffRegister.RegisterCondition<EatOracleCondition>(EatOracleCondition.eatOracleConditionID, "EatOracle", true);
            BuffRegister.RegisterMission(devouringMysteriesMissionID, new DevouringMysteriesMission());
        }
    }

    internal class EatOracleCondition : Condition
    {
        public static ConditionID eatOracleConditionID = new ConditionID("EatOracle", true);

        ConditionalWeakTable<AbstractCreature, PenetrateDmgRecord> dmgRecords = new ConditionalWeakTable<AbstractCreature, PenetrateDmgRecord>();
        public override ConditionID ID => eatOracleConditionID;

        public override int Exp => killRequirement * 100;

        [JsonProperty]
        public int killRequirement;

        [JsonProperty]
        public int oracleKills;

        public override void HookOn()
        {
            On.Oracle.Destroy += Oracle_Destroy;
        }


        private void Oracle_Destroy(On.Oracle.orig_Destroy orig, Oracle self)
        {
            if (!self.slatedForDeletetion && self.room != null)
            {
                for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
                {
                    if (self.room.abstractRoom.creatures[i].realizedCreature != null &&
                        self.room.abstractRoom.creatures[i].realizedCreature is Player player)
                        if (CorruptionShapedMutationBuffEntry.CorruptionCatFeatures.TryGetValue(player, out var corruption))
                        {
                            foreach(var eatObj in corruption.eatObjects)
                            if (eatObj.chunk.owner == self)
                            {
                                oracleKills++;
                                if (oracleKills >= killRequirement)
                                    Finished = true;
                                onLabelRefresh?.Invoke(this);
                            }
                        }
                }
            }
            orig.Invoke(self);
        }

        public override void OnDestroy()
        {
            On.Oracle.Destroy -= Oracle_Destroy;
            base.OnDestroy();
            dmgRecords = null;
        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            killRequirement = Random.Range(5, 10);
            return ConditionState.Ok_NoMore;
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(BuffResourceString.Get("DisplayName_EatOracleCondition"), oracleKills, killRequirement);
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"({oracleKills}/{killRequirement})";
        }

        class PenetrateDmgRecord
        {
            public float penetrateDmg;
            public bool lastHitByPlayerSpear;
        }
    }
}
