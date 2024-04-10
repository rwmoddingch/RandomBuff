using RandomBuff.Core.Game.Settings.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.SaveData;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Missions.BuiltInMissions
{
    public sealed class MidnightSnackMission : Mission, IMissionEntry
    {
        public override MissionID ID => new MissionID("MidnightSnack", true);

        public override SlugcatStats.Name BindSlug => null;

        public override Color TextCol => Color.white;

        public override string MissionName => "HAUNTED\nMIDNIGHT SNACK";


        public MidnightSnackMission()
        {
            gameSetting = new(BindSlug)
            {
                conditions = new()
                {
                    new AchievementCondition() { achievementID = WinState.EndgameID.Monk },
                    new AchievementCondition() { achievementID = WinState.EndgameID.Hunter }
                },
            };
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
