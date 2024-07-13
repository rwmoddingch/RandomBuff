using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Negative;
using BuiltinBuffs.Positive;
using MoreSlugcats;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.Missions;
using TemplateGains;
using UnityEngine;

namespace BuiltinBuffs.Missions
{
    internal class DuelsOfSnipersMission : Mission, IMissionEntry
    {
        public static readonly MissionID DuelsOfSnipers = new MissionID(nameof(DuelsOfSnipers), true);
        public override MissionID ID => DuelsOfSnipers;
        public override SlugcatStats.Name BindSlug => MoreSlugcatsEnums.SlugcatStatsName.Spear;
        public override Color TextCol => new Color(0.31f, 0.18f, 0.41f);
        public override string MissionName => BuffResourceString.Get("Mission_Display_DuelsOfSnipers");

        public DuelsOfSnipersMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new HuntCondition() { killCount = 20, type = CreatureTemplate.Type.Scavenger },
                    new HuntCondition() { killCount = 10, type = MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite },
                    new HuntCondition() { killCount = 5, type = CreatureTemplate.Type.KingVulture },
                    new HuntCondition() { killCount = 5, type = MoreSlugcatsEnums.CreatureTemplateType.MirosVulture }
                }
            };
            startBuffSet.Add(LightSpeedSpearIBuffEntry.LightSpeedSpearBuffID);
            startBuffSet.Add(SpearMasterIBuffEntry.spearMasterBuffID);
            startBuffSet.Add(StillMyTurnBuffEntry.StillMyTurnID);
            startBuffSet.Add(UpgradationIBuffEntry.UpgradationBuffID);

            //TODO:超视距打击

        }
        public void RegisterMission()
        {
            MissionRegister.RegisterMission(DuelsOfSnipers,new DuelsOfSnipersMission());
        }
    }
}
