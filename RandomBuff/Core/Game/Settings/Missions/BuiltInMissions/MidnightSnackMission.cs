using RandomBuff.Core.Game.Settings.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Missions.BuiltInMissions
{
    public class MidnightSnackMission : Mission, IMissionEntry
    {
        public override MissionID ID => new MissionID("MidnightSnack", true);

        public override SlugcatStats.Name bindSlug => null;

        public override Color textCol => Color.white;

        public override string missionName => "HAUNTED\nMIDNIGHT SNACK";

        public MidnightSnackMission()
        {
            conditions.Add(new AchievementCondition()
            {
                achievementID = WinState.EndgameID.Monk
            });
            conditions.Add(new AchievementCondition()
            {
                achievementID = WinState.EndgameID.Hunter
            });
            startBuffSet.Add(new Buff.BuffID("Hell"));
            startBuffSet.Add(new Buff.BuffID("FakeCreature"));
            startBuffSet.Add(new Buff.BuffID("InvisibleKiller"));
            startBuffSet.Add(new Buff.BuffID("Ramenpede"));
            startBuffSet.Add(new Buff.BuffID("NoodleHand"));
            startBuffSet.Add(new Buff.BuffID("WalkingMushroom"));
        }

        public void RegisterMission()
        {
            MissionRegister.RegisterMission(ID, new MidnightSnackMission());
        }
    }
}
