using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Duality;
using BuiltinBuffs.Negative;
using BuiltinBuffs.Positive;
using Newtonsoft.Json;
using RandomBuff.Core.Entry;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Game.Settings.Missions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Missions
{
    internal class TreatmentMission : Mission, IMissionEntry
    {
        public static readonly MissionID EmergnshyTreatment = new MissionID(nameof(EmergnshyTreatment), true);

        public override MissionID ID => EmergnshyTreatment;
        public override SlugcatStats.Name BindSlug => SlugcatStats.Name.Red;
        public override Color TextCol => new Color(1f, 0.4509804f, 0.4509804f);
        public override string MissionName => BuffResourceString.Get("Mission_Display_EmergnshyTreatment");

        public TreatmentMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new SaveMoonCondition(){targetCycle = 5}
                },
                gachaTemplate = new NormalGachaTemplate(true){ }
            };
            startBuffSet.Add(RamenpedeBuffEntry.ramenpedeBuffID);
            startBuffSet.Add(ChronoLizardIBuffEntry.ChronoLizardBuffID);
            startBuffSet.Add(HerbicideIBuffEntry.HerbicideBuffID);
            startBuffSet.Add(NMRIBuffEntry.NMRBuffID);
            //startBuffSet.Add();
            //;


        }

        public void RegisterMission()
        {
            BuffRegister.RegisterCondition<SaveMoonCondition>(SaveMoonCondition.SaveMoon,"Save Moon",true);

            MissionRegister.RegisterMission(TreatmentMission.EmergnshyTreatment,new TreatmentMission());
        }
    }

    public class SaveMoonCondition : Condition
    {

        public static readonly ConditionID SaveMoon = new ConditionID(nameof(SaveMoon), true);
        public override ConditionID ID => SaveMoon;
        public override int Exp => 100;//TODO

        [JsonProperty]
        private int currentCycle;

        [JsonProperty]
        public int targetCycle;
        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            if(name != SlugcatStats.Name.Red)
                return ConditionState.Fail;
            targetCycle = (int)Random.Range(Mathf.Lerp(2, 5, difficulty), Mathf.Lerp(5, 10, difficulty));
            return ConditionState.Ok_NoMore;
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"({currentCycle}/{targetCycle})";
        }

        public override void HookOn()
        {
            base.HookOn();
            if (currentCycle <= targetCycle)
                On.SLOracleWakeUpProcedure.NextPhase += SLOracleWakeUpProcedure_NextPhase;
        }


        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(BuffResourceString.Get("DisplayName_SaveMoon"), targetCycle);
        }

        public override void EnterGame(RainWorldGame game)
        {
            currentCycle = game.GetStorySession.saveState.cycleNumber;
            base.EnterGame(game);
        }

        private void SLOracleWakeUpProcedure_NextPhase(On.SLOracleWakeUpProcedure.orig_NextPhase orig, SLOracleWakeUpProcedure self)
        {
            orig(self);
            if (self.phase == SLOracleWakeUpProcedure.Phase.Done && currentCycle <= targetCycle)
                Finished = true;
        }

        public override void SessionEnd(SaveState save)
        {
            currentCycle = save.cycleNumber + 1;

        }
    }
}
