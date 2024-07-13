using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Duality;
using BuiltinBuffs.Negative;
using MoreSlugcats;
using RandomBuff;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Game.Settings.Missions;
using UnityEngine;

namespace BuiltinBuffs.Missions
{
    internal class ArtOfExplosionMission : Mission, IMissionEntry
    {
        public static readonly MissionID ArtOfExplosion = new MissionID(nameof(ArtOfExplosion), true);

        public override MissionID ID => ArtOfExplosion;
        public override SlugcatStats.Name BindSlug => MoreSlugcatsEnums.SlugcatStatsName.Artificer;
        public override Color TextCol => new Color(0.66667f, 0.9451f, 0.33725f);
        public override string MissionName => BuffResourceString.Get("Mission_Display_ArtOfExplosion");

        public ArtOfExplosionMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new CycleScoreCondition(){targetScore = 120},
                    new HuntCondition()
                        { killCount = 30, type = MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite },
                    new HuntCondition() { killCount = 10, type = CreatureTemplate.Type.RedCentipede },
                    new HuntCondition() { killCount = 10, type = CreatureTemplate.Type.RedLizard }

                },
                gachaTemplate = new NormalGachaTemplate(true)
            };
            startBuffSet.Add(UpgradationIBuffEntry.UpgradationBuffID);
            startBuffSet.Add(MobileAssaultIBuffEntry.mobileAssaultBuffID);
            startBuffSet.Add(ExplosiveJawIBuffEntry.explosiveJawBuffID);
            startBuffSet.Add(UnlimitedFirepowerIBuffEntry.UnlimitedFirepowerBuffID);
            startBuffSet.Add(DangleBombIBuffEntry.dangleBombBuffID);
            startBuffSet.Add(GiveALightIBuffEntry.GiveALightBuffID);
        }

        public void RegisterMission()
        {
            MissionRegister.RegisterMission(ArtOfExplosion,new ArtOfExplosionMission());
        }
    }

    
}
