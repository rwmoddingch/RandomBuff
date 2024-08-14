using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Duality;
using BuiltinBuffs.Negative;
using HotDogGains.Negative;
using RandomBuff;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.Missions;
using UnityEngine;

namespace BuiltinBuffs.Missions
{
    internal class KingOfIslesMission : Mission, IMissionEntry
    {
        public static readonly MissionID KingOfIsles = new MissionID(nameof(KingOfIsles), true);
        public override MissionID ID => KingOfIsles;
        public override SlugcatStats.Name BindSlug { get; }
        public override Color TextCol { get; }
        public override string MissionName => BuffResourceString.Get("Mission_Display_KingOfIsles");

        public KingOfIslesMission()
        {
            gameSetting = new GameSetting(BindSlug,startPos:"SI_S05")
            {
                conditions = new List<Condition>()
                {
                    new WithInCycleCondition() { SetCycle = 11 },
                    new CycleCondition() { SetCycle = 10 },
                    new DeathCondition() { deathCount = 5 },
                    new HuntAllCondition() { huntCount = 40 },
                    new PermanentHoldCondition() { HoldBuff = NoPassDayBuffEntry.noPassDayBuffID }
                }
            };
            startBuffSet.Add(VultureShapedMutationBuffEntry.VultureShapedMutation);
            startBuffSet.Add(ArmedKingVultureBuffEntry.ArmedKingVultureID);
            startBuffSet.Add(NoPassDayBuffEntry.noPassDayBuffID);

        }

        public void RegisterMission()
        {
            
        }
    }
}
