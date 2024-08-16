using BuiltinBuffs.Duality;
using BuiltinBuffs.Negative;
using BuiltinBuffs.Positive;
using RandomBuff;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Game.Settings.Missions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BuiltinBuffs.Missions
{
    internal class RegularCleaningMission : Mission, IMissionEntry
    {
        public static MissionID regularCleaningMissionID = new MissionID("RegularCleaning", true);
        public override MissionID ID => regularCleaningMissionID;

        public override SlugcatStats.Name BindSlug => null;

        public override Color TextCol => SpearBurster.scanlineCol;

        public override string MissionName => BuffResourceString.Get("Mission_Display_RegularCleaning");

        public RegularCleaningMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new HuntCondition() { type = CreatureTemplate.Type.DaddyLongLegs, killCount = 20 },
                    new WithInCycleCondition() { SetCycle = 5 },
                    new PermanentHoldCondition() { HoldBuff = NoPassDayBuffEntry.noPassDayBuffID },
                },
                gachaTemplate = new NormalGachaTemplate()
                {
                    ForceStartPos = "SS_S03",
                    boostCreatureInfos = new List<GachaTemplate.BoostCreatureInfo>()
                    {
                        new GachaTemplate.BoostCreatureInfo()
                        {
                            baseCrit = CreatureTemplate.Type.DaddyLongLegs,
                            boostCrit = CreatureTemplate.Type.DaddyLongLegs,
                            boostCount = 2,
                            boostType = GachaTemplate.BoostCreatureInfo.BoostType.Add
                        }
                    },
                    PocketPackMultiply = 0,
                }
            };
            startBuffSet.Add(BombManiaBuffEntry.bombManiaBuffID);
            startBuffSet.Add(SpearBursterBuffEntry.spearBursterBuff);
            startBuffSet.Add(TurboPropulsionIBuffEntry.turboPropulsionBuffID);
        }

        public void RegisterMission()
        {
            MissionRegister.RegisterMission(regularCleaningMissionID, new RegularCleaningMission());
        }
    }
}
