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
    public sealed class StrayerMission : Mission, IMissionEntry
    {
        public override MissionID ID => new MissionID("Strayer",true);

        public override SlugcatStats.Name BindSlug => SlugcatStats.Name.White;

        public override Color TextCol => Color.white;

        public override string MissionName => "STRANGE LAND STRAYER";

        public StrayerMission()
        {
            gameSetting = new(BindSlug)
            {
                conditions = new()
                {
                    new AchievementCondition() { achievementID = WinState.EndgameID.Traveller },
                    new AchievementCondition() { achievementID = WinState.EndgameID.Survivor },
                }
            };

            if(ModManager.MSC)
                gameSetting.conditions.Add(new AchievementCondition() { achievementID = MoreSlugcatsEnums.EndgameID.Nomad });

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
