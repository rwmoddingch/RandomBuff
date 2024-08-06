using BuiltinBuffs.Duality;
using BuiltinBuffs.Negative;
using BuiltinBuffs.Positive;
using HotDogBuff.Negative;
using Newtonsoft.Json;
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
    internal class SlugOfTheFlameMission : Mission, IMissionEntry
    {
        public static readonly MissionID slugOfTheFlameMissionID = new MissionID("SlugOfTheFlameMission", true);
        public override MissionID ID => slugOfTheFlameMissionID;

        public override SlugcatStats.Name BindSlug => null;

        public override Color TextCol => Helper.GetRGBColor(255, 77,  0);

        public override string MissionName => BuffResourceString.Get("Mission_Display_SlugOfTheFlame");

        public SlugOfTheFlameMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new ScorchCreatureCondition(){ scorchRequirement =  200}
                },
            };
            startBuffSet.AddRange(new[]
            {
                NapalmBuffEntry.napalmBuffID,
                FireShieldBuffEntry.FireShield,
                AggressiveVentingBuffEntry.aggresiveVentingBuffID,
                ScorchingSunBuffEntry.ScorchingSun
            });
        }

        public void RegisterMission()
        {
            BuffRegister.RegisterCondition<ScorchCreatureCondition>(ScorchCreatureCondition.scorchCrreatureConditionID, "ScorchCreature", true);
            MissionRegister.RegisterMission(slugOfTheFlameMissionID, new SlugOfTheFlameMission());
        }
    }

    internal class ScorchCreatureCondition : Condition
    {
        public static readonly ConditionID scorchCrreatureConditionID = new ConditionID("ScorchCreature", true);
        public override ConditionID ID => scorchCrreatureConditionID;

        public override int Exp => 300;

        [JsonProperty]
        public int scorchRequirement;

        [JsonProperty]
        public int scorched;


        public override void EnterGame(RainWorldGame game)
        {
            base.EnterGame(game);
            On.Creature.Die += Creature_Die;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            On.Creature.Die -= Creature_Die;
        }

        private void Creature_Die(On.Creature.orig_Die orig, Creature self)
        {
            bool origDead = self.dead;
            orig.Invoke(self);
            if(!origDead && self.dead)
            {
                if (TemperatrueModule.TryGetTemperatureModule(self, out var temperature) && temperature.burn)
                {
                    scorched++;
                    onLabelRefresh?.Invoke(this);
                    if (scorched >= scorchRequirement)
                        Finished = true;
                }
            }
        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            return ConditionState.Fail;
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            if (Finished || (scorched >= scorchRequirement))
                return "";
            else
                return $"({scorched}/{scorchRequirement})";
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(BuffResourceString.Get("DisplayName_ScorchCreature"), scorchRequirement);
        }
    }
}
