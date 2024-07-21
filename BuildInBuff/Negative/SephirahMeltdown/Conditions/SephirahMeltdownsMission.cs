using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuffUtils;
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
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new MeltdownHuntCondition(){killCount = 1,minConditionCycle = 4,maxConditionCycle = 8,type = CreatureTemplate.Type.RedCentipede},
                    new BinahCondition(){minConditionCycle =8, maxConditionCycle = 12},
                    new FixedCycleCondition() {SetCycle = 12},
                    new DeathCondition(){deathCount = 20}
                },
                gachaTemplate = new SephirahMeltdownsTemplate()
                {
                    cardPick = new Dictionary<int, List<string>>()
                    {
                        {4, new List<string>()
                        {
                            TipherethBuffData.Tiphereth.value,
                            ChesedBuffData.Chesed.value,
                            "bur-pursued"
                        }},
                        {8, new List<string>()
                        {
                            HokmaBuffData.Hokma.value,
                            BinahBuffData.Binah.value
                        }},
                    }, 
                    NCount = 0, NSelect = 0, NShow = 0,
                    PCount = 0, PSelect = 0, PShow = 0,
                    ForceStartPos = "CC_S03"
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
            BuffRegister.RegisterCondition<FixedCycleCondition>(FixedCycleCondition.FixedCycle, "Fix Cycles", true);
            BuffRegister.RegisterCondition<BinahCondition>(BinahCondition.Binah, "Binah", true);

            MissionRegister.RegisterMission(SephirahMeltdowns, new SephirahMeltdownsMission());
            BuffRegister.RegisterGachaTemplate<SephirahMeltdownsTemplate>(SephirahMeltdownsTemplate.SephirahMeltdowns);
        }

    }

    internal class SephirahMeltdownsTemplate : MissionGachaTemplate
    {
        public static readonly GachaTemplateID SephirahMeltdowns=
            new GachaTemplateID(nameof(SephirahMeltdowns), true);

        public override GachaTemplateID ID => SephirahMeltdowns;

        public override void EnterGame(RainWorldGame game)
        {
            if (game.GetStorySession.saveState.cycleNumber == 4)
            {
                ExpeditionGame.burdenTrackers.Add(new ExpeditionGame.PursuedTracker(game));
                BuffUtils.Log("SephirahMeltdown", "Add bur-pursued at 4 cycles");
            }

            base.EnterGame(game);
        }

        public override void SessionEnd(RainWorldGame game)
        {
            if (game.GetStorySession.saveState.cycleNumber == 7)
            {
                BuffPoolManager.Instance.RemoveBuffAndData(new BuffID("bur-pursued"));
                BuffUtils.Log("SephirahMeltdown","Remove bur-pursued at 8 cycles");
            }
            CurrentPacket = (game.GetStorySession.saveState.cycleNumber + 1) % 4 == 0 ?
                new CachaPacket() { negative = (0, 0, 0), positive = (2, 6, 1) } : new CachaPacket();
        }

    }
}
