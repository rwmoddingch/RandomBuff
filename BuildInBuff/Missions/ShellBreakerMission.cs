using BuiltinBuffs.Positive;
using RandomBuff;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Missions;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RandomBuff.Core.Entry;
using Newtonsoft.Json;
using Random = UnityEngine.Random;
using MonoMod.Cil;
using RandomBuffUtils;
using Mono.Cecil.Cil;
using System.Runtime.CompilerServices;
using MoreSlugcats;

namespace BuiltinBuffs.Missions
{
    internal class ShellBreakerMission : Mission, IMissionEntry
    {
        public static readonly MissionID shellBreakerMissionID= new MissionID("ShellBreaker", true);

        public override MissionID ID => shellBreakerMissionID;

        public override SlugcatStats.Name BindSlug => null;

        public override Color TextCol => Custom.hexToColor("FFCC4D");

        public override string MissionName => BuffResourceString.Get("Mission_Display_ShellBreaker");

        public ShellBreakerMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new BreakShellCondition(){killRequirement = 25},
                    new HuntAllCondition(){huntCount = 40},
                    new CycleCondition(){SetCycle = 5},
                    new DeathCondition(){deathCount = 10}
                },

                gachaTemplate = new NormalGachaTemplate()
                {
                    ForceStartPos = "CC_S03",
                    boostCreatureInfos = new List<GachaTemplate.BoostCreatureInfo>()
                    {
                        new GachaTemplate.BoostCreatureInfo()
                        {
                            baseCrit = CreatureTemplate.Type.BlueLizard,
                            boostCrit = CreatureTemplate.Type.BlueLizard,
                            boostCount = 4,
                            boostType = GachaTemplate.BoostCreatureInfo.BoostType.Add,
                        },
                        new GachaTemplate.BoostCreatureInfo()
                        {
                            baseCrit = CreatureTemplate.Type.PinkLizard,
                            boostCrit = CreatureTemplate.Type.PinkLizard,
                            boostCount = 4,
                            boostType = GachaTemplate.BoostCreatureInfo.BoostType.Add,
                        },
                        new GachaTemplate.BoostCreatureInfo()
                        {
                            baseCrit = CreatureTemplate.Type.YellowLizard,
                            boostCrit = CreatureTemplate.Type.YellowLizard,
                            boostCount = 4,
                            boostType = GachaTemplate.BoostCreatureInfo.BoostType.Add,
                        },
                        new GachaTemplate.BoostCreatureInfo()
                        {
                            baseCrit = CreatureTemplate.Type.CyanLizard,
                            boostCrit = CreatureTemplate.Type.CyanLizard,
                            boostCount = 4,
                            boostType = GachaTemplate.BoostCreatureInfo.BoostType.Add,
                        }
                    },
                    PocketPackMultiply = 0,
                }
            };
            startBuffSet.Add(LanceThrowerBuffEntry.lanceThrowerBuffID);
            startBuffSet.Add(StagnantForcefieldBuffEntry.stagnantForcefieldBuffID);
            startBuffSet.Add(SpearMasterIBuffEntry.spearMasterBuffID);
            startBuffSet.Add(DivineBeingIBuffEntry.DivineBeingBuffID);
        }

        public void RegisterMission()
        {
            BuffRegister.RegisterCondition<BreakShellCondition>(BreakShellCondition.breakShellConditionID, "BreakShell", true);
            BuffRegister.RegisterMission(shellBreakerMissionID, new ShellBreakerMission());
        }
    }

    internal class BreakShellCondition : Condition
    {
        public static ConditionID breakShellConditionID = new ConditionID("BreakShell", true);

        ConditionalWeakTable<AbstractCreature, PenetrateDmgRecord> dmgRecords = new ConditionalWeakTable<AbstractCreature, PenetrateDmgRecord>();
        public override ConditionID ID => breakShellConditionID;

        public override int Exp => killRequirement * 100;

        [JsonProperty]
        public int killRequirement;

        [JsonProperty]
        public int penetrateKills;

        public override void HookOn()
        {
            IL.Lizard.Violence += Lizard_Violence;
            On.Lizard.Violence += Lizard_Violence1;
            On.Creature.Die += Creature_Die;
        }


        private void Creature_Die(On.Creature.orig_Die orig, Creature self)
        {
            if (!self.dead)
            {
                if (dmgRecords.TryGetValue(self.abstractCreature, out var record))
                {
                    if (record.penetrateDmg > 0.2f && record.lastHitByPlayerSpear)
                    {
                        penetrateKills++;
                        if (penetrateKills >= killRequirement)
                            Finished = true;
                        onLabelRefresh?.Invoke(this);
                    }
                }
            }
            orig.Invoke(self);
        }

        private void Lizard_Violence1(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
        {
            orig.Invoke(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);

            if (!dmgRecords.TryGetValue(self.abstractCreature, out var re))
            {
                re = new PenetrateDmgRecord();
                dmgRecords.Add(self.abstractCreature, re);
            }

            if (source != null && source.owner != null && source.owner is Spear spear && spear.thrownBy != null && spear.thrownBy is Player)
                re.lastHitByPlayerSpear = true;
            else
                re.lastHitByPlayerSpear = false;
        }

        private void Lizard_Violence(MonoMod.Cil.ILContext il)
        {
            ILCursor c1 = new ILCursor(il);
            if (!c1.TryGotoNext(MoveType.After, 
                (i) => i.MatchLdarg(0),
                (i) => i.Match(OpCodes.Ldarga_S),
                (i) => i.Match(OpCodes.Call), 
                (i) => i.MatchCall<Lizard>("HitHeadShield")))
            {
                BuffUtils.Log("BreakShellCondition", "c1 1 failed");
                return;
            }

            if (!c1.TryGotoNext(MoveType.After, 
                (i) => i.MatchLdloc(0),
                (i) => i.MatchLdcR4(0.1f),
                (i) => i.MatchMul(),
                (i) => i.MatchStloc(0)))
            {
                BuffUtils.Log("BreakShellCondition", "c1 2 failed");
                return;
            }

            c1.Emit(OpCodes.Ldarg_0);
            c1.Emit(OpCodes.Ldarg_3);
            c1.Emit(OpCodes.Ldloc_0);
            c1.Emit(OpCodes.Ldarg, 5);

            c1.EmitDelegate<Action<Lizard, BodyChunk, float, Creature.DamageType>>((self, hitChunk, dmg, damageType) =>
            {
                if(!dmgRecords.TryGetValue(self.abstractCreature, out var re))
                {
                    re = new PenetrateDmgRecord();
                    dmgRecords.Add(self.abstractCreature, re);
                }

                re.penetrateDmg += dmg;

                BuffUtils.Log("BreakShellCondition", $"{self} penetrate shell {self.bodyChunks.IndexOf(hitChunk)}, {dmg} - {damageType}");
            });
        }

        public override void OnDestroy()
        {
            IL.Lizard.Violence -= Lizard_Violence;
            On.Lizard.Violence -= Lizard_Violence1;
            On.Creature.Die -= Creature_Die;
            base.OnDestroy();
            dmgRecords = null;
        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            killRequirement = Random.Range(5, 10);
            return ConditionState.Ok_NoMore;
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(BuffResourceString.Get("DisplayName_BreakShellCondition"), killRequirement);
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"({penetrateKills}/{killRequirement})";
        }

        class PenetrateDmgRecord
        {
            public float penetrateDmg;
            public bool lastHitByPlayerSpear;
        }
    }
}
