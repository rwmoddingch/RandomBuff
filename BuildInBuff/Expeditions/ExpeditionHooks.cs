using MonoMod.RuntimeDetour;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuffUtils.ObjectExtend;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Reflection.Emit;
using BuiltinBuffs.Negative;
using BuiltinBuffs.Negative.SephirahMeltdown;
using BuiltinBuffs.Positive;
using MonoMod.Cil;
using RandomBuffUtils.FutileExtend;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace BuiltinBuffs.Expeditions
{
    internal static class ExpeditionHooks
    {
        #region DEBUG

        private static void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig(self, dt);
            if (Input.GetKey(KeyCode.Y) && BuffPlugin.DevEnabled)
            {
                if (self.rainWorld.BuffMode() && Input.GetKeyDown(KeyCode.K))
                {
                    //var all = ExpeditionProgression.burdenGroups.First().Value;
                    //var id = new BuffID(all[Random.Range(0, all.Count)]);
                    //id.CreateNewBuff();
                    new BuffID("unl-agility").CreateNewBuff();
                }

                if (self.rainWorld.BuffMode() && Input.GetKeyDown(KeyCode.A))
                {
                    BinahBuff.Instance.Data.Health -= 0.1f;
                    if (BinahBuff.Instance.Data.Health < 0)
                    {
                        BinahBuff.Instance.Data.Health = 0;
                        BinahGlobalManager.Die();
                    }
                }

                if (self.rainWorld.BuffMode() && Input.GetKeyDown(KeyCode.S))
                {
                    BinahGlobalManager.DEBUG_ForceSetCd(BinahAttackType.Fairy, 0);

                }
                if (self.rainWorld.BuffMode() && Input.GetKeyDown(KeyCode.L) )
                {
                    BinahBuffData.Binah.CreateNewBuff();

                }
                if (self.rainWorld.BuffMode() && Input.GetKeyDown(KeyCode.U))
                {
                    BinahGlobalManager.DEBUG_ForceSetCd(BinahAttackType.Key,0);
                    self.Players[0].realizedCreature.room.AddObject(new BinahShowEffect(self.Players[0].realizedCreature.room, self.Players[0].realizedCreature.DangerPos,300,RainWorld.SaturatedGold));
                }
            }

        }

       



        #endregion
        public static void OnModsInit()
        {
            _ = new Hook(typeof(RainWorld).GetProperty(nameof(RainWorld.ExpeditionMode)).GetGetMethod(),
                typeof(ExpeditionHooks).GetMethod(nameof(RainWorldExpeditionModeGet), BindingFlags.NonPublic | BindingFlags.Static));
            _ = new Hook(typeof(ExpeditionGame).GetProperty(nameof(ExpeditionGame.activeUnlocks)).GetGetMethod(),
                typeof(ExpeditionHooks).GetMethod(nameof(ExpeditionGameActiveUnlocksGet),BindingFlags.NonPublic | BindingFlags.Static));

            _ = new Hook(typeof(BuffPoolManager).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,null,new Type[] { typeof(RainWorldGame) },Array.Empty<ParameterModifier>()),
                typeof(ExpeditionHooks).GetMethod(nameof(BuffPoolManager_ctor), BindingFlags.NonPublic | BindingFlags.Static));
            On.Expedition.ExpeditionProgression.UnlockSprite += ExpeditionProgression_UnlockSprite;

            On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
            On.RainWorldGame.GoToDeathScreen += RainWorldGame_GoToDeathScreen;
            On.WinState.CycleCompleted += WinState_CycleCompleted;

            IL.RainWorldGame.Update += RainWorldGame_Update;

            _ = new Hook(typeof(ExpeditionData).GetProperty(nameof(ExpeditionData.challengeList),BindingFlags.Static | BindingFlags.Public).GetGetMethod(),
                typeof(ExpeditionHooks).GetMethod(nameof(ExpeditionData_ChallengeListGet), BindingFlags.Static | BindingFlags.NonPublic));
             
        }

        private static void RainWorldGame_Update(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            while (c.TryGotoNext(MoveType.Before,
                       i => i.MatchLdsfld(out var fld) && fld.Name.Contains("expeditionComplete")))
            {
                c.EmitDelegate<Action>(() => ExpeditionGame.expeditionComplete &= !Custom.rainWorld.BuffMode());
                c.GotoNext(MoveType.After, i => i.MatchLdsfld(out var fld) && fld.Name.Contains("expeditionComplete"));
            }
        }

        private static void WinState_CycleCompleted(On.WinState.orig_CycleCompleted orig, WinState self, RainWorldGame game)
        {
            forceDisable = true;
            orig(self,game);
            forceDisable = false;
        }

        private static List<Challenge> ExpeditionData_ChallengeListGet(Func<List<Challenge>> orig)
        {

            if (Custom.rainWorld.BuffMode())
                return new List<Challenge>();
            return orig();

        }
    

        private static bool forceDisable = false;

        private static void RainWorldGame_GoToDeathScreen(On.RainWorldGame.orig_GoToDeathScreen orig, RainWorldGame self)
        {
            forceDisable = true;
            orig(self);
            forceDisable = false;
        }

        private static string ExpeditionProgression_UnlockSprite(On.Expedition.ExpeditionProgression.orig_UnlockSprite orig, string key, bool alwaysShow)
        {
            return orig(key, alwaysShow || Custom.rainWorld.BuffMode());
        }

        private static void BuffPoolManager_ctor(Action<BuffPoolManager,RainWorldGame> orig, BuffPoolManager self, RainWorldGame game)
        {
            activeUnlocks.Clear();
            orig(self, game);
            if (activeUnlocks.Any())
            {
                BuffUtils.Log("BuffExtend", "SetUp expedition trackers");
                ExpeditionGame.SetUpBurdenTrackers(game as RainWorldGame);
                ExpeditionGame.SetUpUnlockTrackers(game as RainWorldGame);
            }
        }




        private static bool RainWorldExpeditionModeGet(Func<RainWorld, bool> orig, RainWorld self)
        {
            return (orig(self) || (self.BuffMode() && activeUnlocks.Any())) && !forceDisable;
        }

        private static List<string> ExpeditionGameActiveUnlocksGet(Func<List<string>> orig)
        {
            if (Custom.rainWorld.BuffMode())
                return activeUnlocks;
            return orig();
        }


        public static List<string> activeUnlocks = new List<string>();

    }
}
