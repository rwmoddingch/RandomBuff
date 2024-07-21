using System.Collections.Generic;
using RandomBuff;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.Missions;
using UnityEngine;

namespace BuiltinBuffs.Missions
{
    public sealed class DodgeTheRockMission : Mission, IMissionEntry
    {
        public static readonly MissionID DodgeTheRock = new MissionID("DodgeTheRock", true);

        public override MissionID ID => DodgeTheRock;

        public override SlugcatStats.Name BindSlug => null;

        public override Color TextCol => Color.white;

        public override string MissionName => BuffResourceString.Get("Mission_Display_DodgeTheRock");

        public DodgeTheRockMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new AchievementCondition { achievementID = WinState.EndgameID.Outlaw },
                    new AchievementCondition { achievementID = WinState.EndgameID.Survivor },
                    new AchievementCondition { achievementID = WinState.EndgameID.DragonSlayer },
                    new CycleCondition { SetCycle = 10 }
                }
            };

            startBuffSet.Add(new RandomBuff.Core.Buff.BuffID("EjectionRock"));
            startBuffSet.Add(new RandomBuff.Core.Buff.BuffID("StoneThrower"));
            startBuffSet.Add(new RandomBuff.Core.Buff.BuffID("ThundThrow"));
            startBuffSet.Add(new RandomBuff.Core.Buff.BuffID("RockCob"));
            startBuffSet.Add(new RandomBuff.Core.Buff.BuffID("StoneofBlessing"));
        }

        public void RegisterMission()
        {
            MissionRegister.RegisterMission(ID, new DodgeTheRockMission());
        }
    }
}
