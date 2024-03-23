using RandomBuff.Core.Game.Settings.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MoreSlugcats;

namespace RandomBuff.Core.Game.Settings.Missions.BuiltInMissions
{
    public class StrayerMission : Mission, IMissionEntry
    {
        public override MissionID ID => new MissionID("Strayer",true);

        public override SlugcatStats.Name bindSlug => null;

        public override Color textCol => Color.white;

        public override string missionName => "STRANGE LAND STRAYER";

        public StrayerMission()
        {
            conditions.Add(new AchievementCondition()
            {
                achievementID = WinState.EndgameID.Traveller
            });
            conditions.Add(new AchievementCondition()
            {
                achievementID = WinState.EndgameID.Survivor
            });
            if (ModManager.MSC)
            {
                conditions.Add(new AchievementCondition()
                {
                    achievementID = MoreSlugcatsEnums.EndgameID.Nomad
                });
            }           
            
            startBuffSet.Add(new Buff.BuffID("RandomRain"));
            startBuffSet.Add(new Buff.BuffID("ShortCircuitGate"));
            startBuffSet.Add(new Buff.BuffID("Armstron"));
        }

        public void RegisterMission()
        {
            MissionRegister.RegisterMission(ID, new StrayerMission());
        }
    }
}
