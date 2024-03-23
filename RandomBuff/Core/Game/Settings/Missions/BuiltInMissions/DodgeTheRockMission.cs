using RandomBuff.Core.Game.Settings.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Missions.BuiltInMissions
{
    public class DodgeTheRockMission : Mission, IMissionEntry
    {
        public static readonly MissionID DodgeTheRock = new MissionID("DodgeTheRock", true);

        public override MissionID ID => DodgeTheRock;

        public override SlugcatStats.Name bindSlug => null;

        public override Color textCol => Color.white;

        public override string missionName => "DODGE THE ROCKS";

        public DodgeTheRockMission()
        {
            conditions.Add(new AchievementCondition
            {
                achievementID = WinState.EndgameID.Outlaw                
            });
            conditions.Add(new AchievementCondition
            {
                achievementID = WinState.EndgameID.Survivor
            });
            conditions.Add(new AchievementCondition
            {
                achievementID = WinState.EndgameID.DragonSlayer
            });
            conditions.Add(new CycleCondition
            {
                SetCycle = 10
            });
            startBuffSet.Add(new Buff.BuffID("EjectionRock"));
            startBuffSet.Add(new Buff.BuffID("StoneThrower"));
            startBuffSet.Add(new Buff.BuffID("ThundThrow"));
            startBuffSet.Add(new Buff.BuffID("RockCob"));
            startBuffSet.Add(new Buff.BuffID("StoneofBlessing"));
        }

        public void RegisterMission()
        {
            MissionRegister.RegisterMission(ID, new DodgeTheRockMission());
        }
    }
}
