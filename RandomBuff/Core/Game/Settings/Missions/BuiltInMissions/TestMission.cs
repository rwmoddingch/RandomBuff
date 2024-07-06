using RandomBuff.Core.Game.Settings.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Missions.BuiltInMissions
{
    public sealed class TestMission : Mission, IMissionEntry
    {
        public override MissionID ID => MissionID.Test;

        public override SlugcatStats.Name BindSlug => null;

        public override Color TextCol => Color.white;

        public override string MissionName => "TEST MISSION";

        public TestMission()
        {
            gameSetting = new GameSetting(BindSlug,"Normal","SS_AI")
            {
                conditions = new() { new CycleCondition() { SetCycle = 10 } },
            };
            startBuffSet.Add(new Buff.BuffID("Hell"));
        }

        public void RegisterMission()
        {
            MissionRegister.RegisterMission(ID, new TestMission());
        }
    }
}
