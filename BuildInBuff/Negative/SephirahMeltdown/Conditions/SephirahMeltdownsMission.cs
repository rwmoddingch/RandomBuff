using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Game.Settings.Missions;
using UnityEngine;

namespace BuiltinBuffs.Negative.SephirahMeltdown.Conditions
{
    internal class SephirahMeltdownsMission : Mission, IMissionEntry
    {
        public static readonly MissionID SephirahMeltdowns = new MissionID(nameof(SephirahMeltdowns), true);
        public override MissionID ID => SephirahMeltdowns;
        public override SlugcatStats.Name BindSlug { get; }
        public override Color TextCol => Color.white;
        public override string MissionName => BuffResourceString.Get("Mission_Display_Meltdown");


        public SephirahMeltdownsMission()
        {
            gameSetting = new GameSetting(BindSlug,"Normal","CC_S03")
            {
                conditions = new List<Condition>()
                {
                    new MeltdownHuntCondition(){killCount = 1,minConditionCycle = 4,maxConditionCycle = 8,type = CreatureTemplate.Type.RedLizard},
                    new MeltdownHuntCondition(){killCount = 1,minConditionCycle = 8,maxConditionCycle = 12,type = CreatureTemplate.Type.KingVulture},
                    new FixedCycleCondition() {SetCycle = 12},
                    new DeathCondition(){deathCount = 12}
                },
                gachaTemplate = new MissionGachaTemplate()
                {
                    cardPick = new Dictionary<int, List<string>>()
                    {
                        {4, new List<string>()
                        {
                            TipherethBuffData.Tiphereth.value,
                            ChesedBuffData.Chesed.value,
                        }},
                        {8, new List<string>()
                        {
                            HokmaBuffData.Hokma.value,
                        }},
                    }, 
                    NCount = 0, NSelect = 0, NShow = 0,
                    PCount = 0, PSelect = 0, PShow = 0,
                }
            };
            startBuffSet.Add(MalkuthBuffData.Malkuth);
            startBuffSet.Add(YesodBuffData.Yesod);
            startBuffSet.Add(NetzachBuffData.Netzach);
            startBuffSet.Add(HodBuffData.Hod);



        }

        public void RegisterMission()
        {
            BuffRegister.RegisterCondition<MeltdownHuntCondition>(MeltdownHuntCondition.MeltdownHunt, "Meltdown Hunt",true);
            BuffRegister.RegisterCondition<DeathCondition>(DeathCondition.Death, "Death");
            BuffRegister.RegisterCondition<FixedCycleCondition>(FixedCycleCondition.FixedCycle, "Fix Cycles");

            MissionRegister.RegisterMission(SephirahMeltdowns, new SephirahMeltdownsMission());



        }
    }
}
