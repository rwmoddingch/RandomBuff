using System.Collections.Generic;
using RandomBuff;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.Missions;
using UnityEngine;

namespace BuiltinBuffs.Missions
{
    public class DruidMission : Mission, IMissionEntry
    {
        public override MissionID ID => MissionID.Druid;

        public override Color TextCol => Color.yellow;

        public override SlugcatStats.Name BindSlug => SlugcatStats.Name.Yellow;

        public override string MissionName => BuffResourceString.Get("Mission_Display_Druid");

        public DruidMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                    {new AchievementCondition() { achievementID = WinState.EndgameID.Friend },
                    new AchievementCondition() { achievementID = WinState.EndgameID.Chieftain }}
            };

            startBuffSet.Add(new RandomBuff.Core.Buff.BuffID("ChronoLizard"));
            startBuffSet.Add(new RandomBuff.Core.Buff.BuffID("MobileAssault"));
            startBuffSet.Add(new RandomBuff.Core.Buff.BuffID("StoneofBlessing"));
        }

        public void RegisterMission()
        {
            MissionRegister.RegisterMission(ID, new DruidMission());
        }
    }
}
