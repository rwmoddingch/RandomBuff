using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.Missions;
using System.Collections.Generic;
using RandomBuff.Core.Game.Settings;
using UnityEngine;

namespace BuiltinBuffs.Missions
{
    public sealed class MidnightSnackMission : Mission, IMissionEntry
    {
        public override MissionID ID => new MissionID("MidnightSnack", true);

        public override SlugcatStats.Name BindSlug => null;

        public override Color TextCol => Color.white;

        public override string MissionName => "HAUNTED\nMIDNIGHT SNACK";


        public MidnightSnackMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new AchievementCondition() { achievementID = WinState.EndgameID.Monk },
                    new AchievementCondition() { achievementID = WinState.EndgameID.Hunter }
                },
            };
            startBuffSet.Add(new RandomBuff.Core.Buff.BuffID("Hell"));
            startBuffSet.Add(new RandomBuff.Core.Buff.BuffID("FakeCreature"));
            startBuffSet.Add(new RandomBuff.Core.Buff.BuffID("InvisibleKiller"));
            startBuffSet.Add(new RandomBuff.Core.Buff.BuffID("Ramenpede"));
            startBuffSet.Add(new RandomBuff.Core.Buff.BuffID("NoodleHand"));
            startBuffSet.Add(new RandomBuff.Core.Buff.BuffID("WalkingMushroom"));
        }

        public void RegisterMission()
        {
            MissionRegister.RegisterMission(ID, new MidnightSnackMission());
        }
    }
}
