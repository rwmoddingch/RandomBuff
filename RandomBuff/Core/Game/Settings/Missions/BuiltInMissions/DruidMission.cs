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

        public override Color TextCol => Color.yellow;

        public override SlugcatStats.Name BindSlug => SlugcatStats.Name.Yellow;

        public override string MissionName => "DANCES WITH DRAGONS";

        public DruidMission()
        {
            gameSetting = new (BindSlug)
            {
                conditions = new()
                    {new AchievementCondition() { achievementID = WinState.EndgameID.Friend },
                    new AchievementCondition() { achievementID = WinState.EndgameID.Chieftain }}
            };

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
