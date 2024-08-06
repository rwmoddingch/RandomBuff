using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Duality;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuff.Core.SaveData;
using UnityEngine;

namespace BuiltinBuffs.Missions
{
    internal class SurvivalOfFittestMission : Mission, IMissionEntry
    {
        public static readonly MissionID SurvivalOfFittest = new MissionID(nameof(SurvivalOfFittest), true);
        public override MissionID ID => SurvivalOfFittest;
        public override SlugcatStats.Name BindSlug { get; }
        public override Color TextCol => Color.white;
        public override string MissionName => BuffResourceString.Get("Mission_Display_SurvivalOfFittest");


        public SurvivalOfFittestMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new AchievementCondition() { achievementID = WinState.EndgameID.Traveller }
                },
                gachaTemplate = new SurvivalOfFittestTemplate()

            };
        }

        public void RegisterMission()
        {
            BuffRegister.RegisterGachaTemplate<SurvivalOfFittestTemplate>(SurvivalOfFittestTemplate.SurvivalOfFittest);
            BuffRegister.RegisterMission(SurvivalOfFittest,new SurvivalOfFittestMission());
        }
    }

    internal class SurvivalOfFittestTemplate : GachaTemplate
    {
        public static readonly GachaTemplateID SurvivalOfFittest = new GachaTemplateID(nameof(SurvivalOfFittest), true);

        public override GachaTemplateID ID => SurvivalOfFittest;

        public static readonly BuffID[] randomIds = new[]
        {
            VultureShapedMutationBuffEntry.VultureShapedMutation,
            JellyfishShapedMutationBuffEntry.JellyfishShapedMutation,
            SpiderShapedMutationBuffEntry.SpiderShapedMutation,
            PixieSlugBuffEntry.PixieSlug,
            SlugSlugBuffEntry.SlugSlugID,
        };


        public SurvivalOfFittestTemplate()
        {
            PocketPackMultiply = 0;
            ExpMultiply = 3f;
            TemplateDescription = "GachaTemplate_Desc_SurvivalOfFittest";

        }

        public override bool NeedRandomStart => true;


        public override void NewGame()
        {
        }

        public override void EnterGame(RainWorldGame game)
        {
            base.EnterGame(game);
            if (game.GetStorySession.saveState.cycleNumber == 0)
            {
                var id = RXRandom.AnyItem(randomIds);
                for(int i =0;i<4;i++)
                    BuffPoolManager.Instance.CreateBuff(id,true);
            }
        }

        private void StackMaxId(BuffID id)
        {
            if (id.GetStaticData().Stackable)
            {
                for (int i = 0; i < 4; i++)
                    id.CreateNewBuff();
            }
            else
            {
                id.CreateNewBuff();
            }
        }

        public override void SessionEnd(RainWorldGame game)
        {
            if (( game.GetStorySession.saveState.cycleNumber+1) % 5 == 0 )
            {
                var allIds = BuffCore.GetAllBuffIds();

                foreach (var id in allIds)
                {
                    for(int i =0;i< (id.GetStaticData().Stackable ? id.GetBuffData().StackLayer:1);i++)
                        id.UnstackBuff();
                }

                StackMaxId(RXRandom.AnyItem(randomIds.Where(i => !allIds.Contains(i)).ToArray()));

              
            }
            else
            {
                foreach(var id in BuffCore.GetAllBuffIds())
                    if (id.GetBuffData() is CountableBuffData countable)
                        countable.GetType().GetProperty("CycleUse").SetValue(countable, 0);
            }
        }

    }
}
