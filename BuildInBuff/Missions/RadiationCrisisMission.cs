using BuiltinBuffs.Duality;
using BuiltinBuffs.Negative;
using RandomBuff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.Missions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BuiltinBuffs.Missions
{
    internal class RadiationCrisisMission : Mission, IMissionEntry
    {
        public static MissionID radiationCrisisMissionID = new MissionID("RadiationCrisis", true);
        public static Color missionCol = Helper.GetRGBColor(35, 255, 153);

        public override MissionID ID => radiationCrisisMissionID;

        public override SlugcatStats.Name BindSlug => null;

        public override Color TextCol => missionCol;

        public override string MissionName => BuffResourceString.Get("Mission_Display_RadiationCrisis");

        public RadiationCrisisMission()
        {
            gameSetting = new GameSetting(BindSlug, startPos:"HI_S01")
            {
                conditions = new List<Condition>()
                {
                    new ExterminationCondition() { weaponType = AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, killRequirement = 200 }
                }
            };

            startBuffSet.Add(BigPebbleNukeBuffEntry.BigPebbleNukeID);
            startBuffSet.Add(BombManiaBuffEntry.bombManiaBuffID);
            startBuffSet.Add(MultiplierBuffEntry.multiplierBuffID);
        }

        public void RegisterMission()
        {
            MissionRegister.RegisterMission(radiationCrisisMissionID, new RadiationCrisisMission());
        }
    }
}
