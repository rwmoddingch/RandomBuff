using RandomBuff.Core.Game.Settings.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Missions.BuiltInMissions
{
    public sealed class DodgeTheRockMission : Mission, IMissionEntry
    {
        public static readonly MissionID DodgeTheRock = new MissionID("DodgeTheRock", true);

        public override MissionID ID => DodgeTheRock;

        public override SlugcatStats.Name BindSlug => null;

        public override Color TextCol => Color.white;

        public override string MissionName => "DODGE THE ROCKS";

        public DodgeTheRockMission()
        {
            gameSetting = new(BindSlug)
            {
                conditions = new()
                {
                    new AchievementCondition { achievementID = WinState.EndgameID.Outlaw },
                    new AchievementCondition { achievementID = WinState.EndgameID.Survivor },
                    new AchievementCondition { achievementID = WinState.EndgameID.DragonSlayer },
                    new CycleCondition { SetCycle = 10 }
                }
            };

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
