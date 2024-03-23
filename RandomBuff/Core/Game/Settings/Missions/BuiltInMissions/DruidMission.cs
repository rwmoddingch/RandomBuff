using RandomBuff.Core.Game.Settings.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Missions.BuiltInMissions
{
    public class DruidMission : Mission, IMissionEntry
    {
        public override MissionID ID => MissionID.Druid;

        public override Color textCol => Color.yellow;

        public override SlugcatStats.Name bindSlug => SlugcatStats.Name.Yellow;

        public override string missionName => "DANCES WITH DRAGONS";

        public DruidMission()
        {
            conditions.Add(new AchievementCondition()
            {
                achievementID = WinState.EndgameID.Friend
            });
            conditions.Add(new AchievementCondition()
            {
                achievementID = WinState.EndgameID.Chieftain
            });

            startBuffSet.Add(new Buff.BuffID("Upgradation"));
            startBuffSet.Add(new Buff.BuffID("MobileAssault"));
            startBuffSet.Add(new Buff.BuffID("StoneofBlessing"));
        }

        public void RegisterMission()
        {
            MissionRegister.RegisterMission(ID, new DruidMission());
        }
    }
}
