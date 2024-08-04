using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Duality;
using BuiltinBuffs.Negative;
using MoreSlugcats;
using RandomBuff.Core.Entry;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuff.Core.SaveData;
using RandomBuffUtils;
using TemplateGains;
using UnityEngine;

namespace BuiltinBuffs.Missions
{
    internal class DoomExpressMission : Mission, IMissionEntry
    {

        public static readonly MissionID DoomExpress = new MissionID(nameof(DoomExpress), true);
        public override MissionID ID => DoomExpress;
        public override SlugcatStats.Name BindSlug => MoreSlugcatsEnums.SlugcatStatsName.Rivulet;
        public override Color TextCol => new Color(0.56863f, 0.8f, 0.94118f);
        public override string MissionName => BuffResourceString.Get("Mission_Display_DoomExpress");

        public DoomExpressMission()
        {
            gameSetting = new GameSetting(BindSlug, "Normal", "RM_SFINAL")
            {
                conditions = new List<Condition>()
                {
                    new BatteryCondition()
                }
            };
            startBuffSet.Add(RandomRainIBuffEntry.RandomRainBuffID);
            startBuffSet.Add(DequantizeBuffEntry.DequantizeID);
            startBuffSet.Add(HypothermiaIBuffEntry.HypothermiaID);
            startBuffSet.Add(FlyingAquaIBuffEntry.FlyingAquaBuffID);
            //startBuffSet.Add(); TODO:大洪水?
        }

        public void RegisterMission()
        {
            MissionRegister.RegisterMission(DoomExpress,new DoomExpressMission());
            BuffRegister.RegisterCondition<BatteryCondition>(BatteryCondition.Battery,"Battery", true);
        }
    }

    public class BatteryCondition : Condition
    {
        public override void HookOn()
        {
            base.HookOn();
            On.RainWorldGame.ForceSaveNewDenLocation += RainWorldGame_ForceSaveNewDenLocation;
        }

        private void RainWorldGame_ForceSaveNewDenLocation(On.RainWorldGame.orig_ForceSaveNewDenLocation orig, RainWorldGame game, string roomName, bool saveWorldStates)
        {
            orig(game,roomName, saveWorldStates);
            if (roomName == "MS_bitterstart")
            {
                 BuffUtils.Log(nameof(BatteryCondition),"Player finished battery condition");
                 BuffFile.Instance.SaveFile();
                 Finished = true;
            }
        }

        public static readonly ConditionID Battery = new ConditionID(nameof(Battery), true);
        public override ConditionID ID => Battery;
        public override int Exp => 350; //TODO
        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            if (name == MoreSlugcatsEnums.SlugcatStatsName.Rivulet)
                return ConditionState.Ok_NoMore;
            return ConditionState.Fail;
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return string.Empty;
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return BuffResourceString.Get("DisplayName_Battery");
        }
    }
}
