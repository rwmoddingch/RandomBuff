using RandomBuff.Core.Game.Settings.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Missions.BuiltInMissions
{
    public class TestMission : Mission, IMissionEntry
    {
        public override MissionID ID => MissionID.Test;

        public override SlugcatStats.Name bindSlug => null;

        public override Color textCol => Color.white;

        public override string missionName => "TEST MISSION";

        public TestMission()
        {
            conditions.Add(new CycleCondition()
            {
                SetCycle = 10
            }) ;
            startBuffSet.Add(new Buff.BuffID("Hell"));
        }

        public void RegisterMission()
        {
            MissionRegister.RegisterMission(ID, new TestMission());
        }
    }
}
