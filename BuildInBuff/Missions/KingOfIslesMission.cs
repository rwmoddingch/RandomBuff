using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Duality;
using BuiltinBuffs.Negative;
using HotDogGains.Negative;
using RandomBuff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Game.Settings.Missions;
using RWCustom;
using UnityEngine;

namespace BuiltinBuffs.Missions
{
    internal class KingOfIslesMission : Mission, IMissionEntry
    {
        public static readonly MissionID KingOfIsles = new MissionID(nameof(KingOfIsles), true);
        public override MissionID ID => KingOfIsles;
        public override SlugcatStats.Name BindSlug { get; }
        public override Color TextCol => Custom.hexToColor("FFB4A4");
        public override string MissionName => BuffResourceString.Get("Mission_Display_KingOfIsles");

        public KingOfIslesMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new WithInCycleCondition() { SetCycle = 11 },
                    new CycleCondition() { SetCycle = 10 },
                    new DeathCondition() { deathCount = 5 },
                    new HuntAllCondition() { huntCount = 40 },
                    new PermanentHoldCondition() { HoldBuff = NoPassDayBuffEntry.noPassDayBuffID },
                },
                gachaTemplate = new NormalGachaTemplate()
                {
                    ForceStartPos = "SI_S04",
                    boostCreatureInfos = new List<GachaTemplate.BoostCreatureInfo>()
                    {
                        new GachaTemplate.BoostCreatureInfo()
                        {
                            baseCrit = CreatureTemplate.Type.Vulture,
                            boostCrit = CreatureTemplate.Type.KingVulture,
                            boostCount = 1,
                            boostType = GachaTemplate.BoostCreatureInfo.BoostType.Add
                        },
                        new GachaTemplate.BoostCreatureInfo()
                        {
                            baseCrit = CreatureTemplate.Type.KingVulture,
                            boostCrit = CreatureTemplate.Type.KingVulture,
                            boostCount = 1.5f,
                            boostType = GachaTemplate.BoostCreatureInfo.BoostType.Add
                        }
                    }
                }
            };
            startBuffSet.Add(VultureShapedMutationBuffEntry.VultureShapedMutation);
            startBuffSet.Add(ArmedKingVultureBuffEntry.ArmedKingVultureID);
            startBuffSet.Add(NoPassDayBuffEntry.noPassDayBuffID);
            startBuffSet.Add(RocketBoostTusksEntry.RocketBoostTusks);

        }

        public void RegisterMission()
        {
            BuffRegister.RegisterMission(KingOfIsles,new KingOfIslesMission());
        }
    }
}
