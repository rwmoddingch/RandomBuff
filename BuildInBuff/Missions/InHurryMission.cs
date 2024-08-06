using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Duality;
using BuiltinBuffs.Positive;
using Newtonsoft.Json;
using RandomBuff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Missions
{
    internal class InHurryMission : Mission, IMissionEntry
    {
        public static readonly MissionID InHurry = new MissionID(nameof(InHurry), true);

        public override MissionID ID => InHurry;
        public override SlugcatStats.Name BindSlug { get; }
        public override Color TextCol { get; }
        public override string MissionName => BuffResourceString.Get("Mission_Display_InHurry");

        public InHurryMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new DistanceCondition(){targetTileCount = 1500, needFaster = true},
                    new DeathCondition(){deathCount = 5}
                }
            };
            startBuffSet.Add(ShockingSpeedBuffEntry.shockingSpeedBuffID);
            startBuffSet.Add(ThundThrowBuffEntry.thundTHrowBuffID);
            startBuffSet.Add(IntenseSituationBuffEntry.IntenseSituation);
            startBuffSet.Add(SuperConductorBuffEntry.superConductorBuffID);
            startBuffSet.Add(Negative.MobileAssaultIBuffEntry.mobileAssaultBuffID);
            startBuffSet.Add(Negative.ChronoLizardIBuffEntry.ChronoLizardBuffID);
        }

        public void RegisterMission()
        {
            BuffRegister.RegisterCondition<DistanceCondition>(DistanceCondition.Distance, "Distance");
            BuffRegister.RegisterMission(InHurry, new InHurryMission());
        }
    }

    public class DistanceCondition : Condition
    {

        class PlayerModule
        {
            public PlayerModule() { }

            public void Update(Player self)
            {
                lastPos = self.abstractCreature.pos.Tile;
            }

            public IntVector2? lastPos;
        }

        private ConditionalWeakTable<Player, PlayerModule> modules = new ConditionalWeakTable<Player, PlayerModule>();

        public static readonly ConditionID Distance = new ConditionID(nameof(Distance), true);

        public override ConditionID ID => Distance;
        public override int Exp => Mathf.RoundToInt(targetTileCount / (needFaster ? 2.5f : 25f));


        [JsonProperty]
        private float moveTiles;


        [JsonProperty] 
        public bool needFaster;

        [JsonProperty]
        public int targetTileCount;


        private int lastMoveTiles;


        public override void HookOn()
        {
            base.HookOn();
            On.Player.Update += Player_Update;
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self.isNPC) return;

            if (needFaster && self.slugcatStats.runspeedFac <= self.slugcatStats.Original().runspeedFac) return;

            if (!modules.TryGetValue(self, out var module))
                modules.Add(self, module = new PlayerModule());
            if (module.lastPos == null)
                module.lastPos = self.abstractCreature.pos.Tile;

            var dist = self.abstractCreature.pos.Tile.ToVector2() - module.lastPos.Value.ToVector2();
            if (self.bodyMode != Player.BodyModeIndex.ZeroG)
            {
                dist.y = Mathf.Max(self.input[0].y > 0 ? dist.y : 0, 0);
            }

            if (!self.Consious || self.grabbedBy.Count > 0 || self.inShortcut || self.room == null)
            {
                module.lastPos = null;
                return;
            }
            moveTiles +=(dist.magnitude/2 / self.room.game.Players.Count);
            if (lastMoveTiles != Mathf.RoundToInt(moveTiles))
            {
                lastMoveTiles = Mathf.RoundToInt(moveTiles);
                onLabelRefresh?.Invoke(this);
                if (lastMoveTiles >= targetTileCount)
                    Finished = true;
            }

            module.Update(self);
        }


        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            needFaster = Random.value > 0.5f;
            targetTileCount = Random.Range(2000, 10000) / (needFaster ? 10 : 1);
            return ConditionState.Ok_NoMore;
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"({lastMoveTiles}/{targetTileCount})";
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(BuffResourceString.Get($"DisplayName_Distance{(needFaster ? "_Faster" : "")}"), targetTileCount);
        }


    }
}
