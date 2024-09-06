﻿using BuiltinBuffs.Duality;
using BuiltinBuffs.Negative;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuff.Core.Game.Settings;
using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BuiltinBuffs.Positive;
using RandomBuff.Core.Buff;

namespace BuiltinBuffs.Missions
{
    internal class TunnelBattleMissionMission : Mission, IMissionEntry
    {
        public static MissionID tunnelBattle = new MissionID("TunnelBattle", true);
        public static Color missionCol = Color.red;

        public override MissionID ID => tunnelBattle;

        public override SlugcatStats.Name BindSlug => null;
        public override Color TextCol => missionCol;

        public override string MissionName => BuffResourceString.Get("Mission_Display_TunnelBattle");

        public TunnelBattleMissionMission()
        {
            gameSetting = new GameSetting(BindSlug, startPos: "SB_S04")
            {
                conditions = new List<Condition>()
                {
                    new ExterminationCondition() { weaponType = AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, killRequirement = 200 },
                    new DeathCondition(){deathCount = 10},
                    new PermanentHoldCondition() { HoldBuff =  new BuffID("NoPassDay") }
                }
            };

            startBuffSet.Add(SensorBombEntry.sensorBombBuffID);
            startBuffSet.Add(BombManiaBuffEntry.bombManiaBuffID);
            startBuffSet.Add(new BuffID("Multiplier"));
            startBuffSet.Add(new BuffID("FasterShortCuts"));
            startBuffSet.Add(new BuffID("DeathFreeMedallion"));
        }

        public void RegisterMission()
        {
            MissionRegister.RegisterMission(tunnelBattle, new TunnelBattleMissionMission());
        }
    }
}

