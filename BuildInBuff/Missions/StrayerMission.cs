using System.Collections.Generic;
using BuiltinBuffs.Negative.SephirahMeltdown.Conditions;
using MoreSlugcats;
using RandomBuff;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.Missions;
using UnityEngine;

namespace BuiltinBuffs.Missions
{
    public sealed class StrayerMission : Mission, IMissionEntry
    {
        public override MissionID ID => new MissionID("Strayer",true);

        public override SlugcatStats.Name BindSlug => SlugcatStats.Name.White;

        public override Color TextCol => Color.white;

        public override string MissionName => BuffResourceString.Get("Mission_Display_Strayer");

        public StrayerMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new AchievementCondition() { achievementID = WinState.EndgameID.Traveller },
                    new AchievementCondition() { achievementID = WinState.EndgameID.Survivor },
                    new MeetSS_SLCondition(){cycleRequirement = 15,meetSL = true,meetSS = true}
                }
            };

            if(ModManager.MSC)
                gameSetting.conditions.Add(new AchievementCondition() { achievementID = MoreSlugcatsEnums.EndgameID.Nomad });

            startBuffSet.Add(new RandomBuff.Core.Buff.BuffID("RandomRain"));
            startBuffSet.Add(new RandomBuff.Core.Buff.BuffID("ShortCircuitGate"));
            startBuffSet.Add(new RandomBuff.Core.Buff.BuffID("Armstron"));
        }

        public void RegisterMission()
        {
            MissionRegister.RegisterMission(ID, new StrayerMission());
        }
    }
}
