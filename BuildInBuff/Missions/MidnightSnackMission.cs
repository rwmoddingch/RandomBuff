using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.Missions;
using System.Collections.Generic;
using MoreSlugcats;
using RandomBuff;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using UnityEngine;

namespace BuiltinBuffs.Missions
{
    public sealed class MidnightSnackMission : Mission, IMissionEntry
    {
        public override MissionID ID => new MissionID("MidnightSnack", true);

        public override SlugcatStats.Name BindSlug => MoreSlugcatsEnums.SlugcatStatsName.Gourmand;

        public override Color TextCol => new Color(0.94118f, 0.75686f, 0.59216f);

        public override string MissionName => BuffResourceString.Get("Mission_Display_MidnightSnack");


        public MidnightSnackMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new AchievementCondition() { achievementID = WinState.EndgameID.Monk },
                    new AchievementCondition() { achievementID = WinState.EndgameID.Hunter },
                    new GourmandCondition()
                },
                gachaTemplate = new NormalGachaTemplate(true)
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
