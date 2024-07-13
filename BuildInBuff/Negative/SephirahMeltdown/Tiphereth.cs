using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Negative.SephirahMeltdown
{
    internal class TipherethBuffData : SephirahMeltdownBuffData
    {
        public static readonly BuffID Tiphereth = new BuffID(nameof(Tiphereth), true);

        public override BuffID ID => Tiphereth;
    }

    internal class TipherethBuff : Buff<TipherethBuff, TipherethBuffData>
    {
        public override BuffID ID => TipherethBuffData.Tiphereth;


        public TipherethBuff()
        {
            if (BuffCustom.TryGetGame(out var game) && game.Players[0].Room.shelterIndex != -1)
            {
                game.world.brokenShelters[game.Players[0].Room.shelterIndex] = true;
                game.world.brokenShelterIndexDueToPrecycle = game.Players[0].Room.shelterIndex;
            }
        }
        public override void Destroy()
        {
            base.Destroy();
            TipherethHook.isInit = false;
            TipherethHook.RainInst = 0;
        }
    }

    internal class TipherethHook
    {
        public static bool isInit = false;

        private static (int center, int duringTime)[] rainTuples;

        public static void HookOn()
        {
            On.RainCycle.Update += RainCycle_Update;
            IL.RainCycle.Update += RainCycle_UpdateIL;
            On.RainWorldGame.AllowRainCounterToTick += RainWorldGame_AllowRainCounterToTick;
            new Hook(typeof(RainCycle).GetProperty(nameof(RainCycle.MicroScreenShake)).GetGetMethod(),
                typeof(TipherethHook).GetMethod(nameof(MicroScreenShakeHook), BindingFlags.NonPublic | BindingFlags.Static));
            new Hook(typeof(RainCycle).GetProperty(nameof(RainCycle.ScreenShake)).GetGetMethod(),
                typeof(TipherethHook).GetMethod(nameof(ScreenShakeHook), BindingFlags.NonPublic | BindingFlags.Static));
            new Hook(typeof(RainCycle).GetProperty(nameof(RainCycle.RainApproaching)).GetGetMethod(),
                typeof(TipherethHook).GetMethod(nameof(RainApproachingHook), BindingFlags.NonPublic | BindingFlags.Static));
            new Hook(typeof(RainCycle).GetProperty(nameof(RainCycle.RainDarkPalette)).GetGetMethod(),
                typeof(TipherethHook).GetMethod(nameof(RainDarkPaletteHook), BindingFlags.NonPublic | BindingFlags.Static));

            new Hook(typeof(RainCycle).GetProperty(nameof(RainCycle.preCycleRain_Intensity)).GetGetMethod(),
                typeof(TipherethHook).GetMethod(nameof(preCycleRain_IntensityHook), BindingFlags.NonPublic | BindingFlags.Static));

            
          
        }

        public static void LongLifeCycleHookOn()
        {
            On.RainCycle.ctor += RainCycle_ctor;
        }
        private static void RainCycle_UpdateIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if(c.TryGotoNext(MoveType.After,i => i.MatchStfld<RainCycle>("pause")
                   ,i => i.Match(OpCodes.Br)
                   ,i => i.MatchLdsfld<ModManager>("MSC")))
            {
                c.EmitDelegate<Func<bool, bool>>((re) => re && RainInst <= 0);
            }
            else
                BuffUtils.LogError(TipherethBuffData.Tiphereth,"hook failed");
            
        }

        private static bool RainWorldGame_AllowRainCounterToTick(On.RainWorldGame.orig_AllowRainCounterToTick orig, RainWorldGame self)
        {
            var re = orig(self);
            return re || RainInst > 0;
        }

        private static float MicroScreenShakeHook(Func<RainCycle, float> orig, RainCycle self)
        {
            if (RainInst > 0)
                return self.preCycleRain_Intensity / 5;
            return orig(self);
        }
        private static float RainApproachingHook(Func<RainCycle, float> orig, RainCycle self)
        {
            if (RainInst > 0)
                return Mathf.Clamp01(RainInst * 4);
            return orig(self);
        }

        private static float preCycleRain_IntensityHook(Func<RainCycle, float> orig, RainCycle self)
        {
            if(RainInst > 0)
                return (Mathf.Sin(self.preCycleRainPulse_WaveA) +
                        Mathf.Sin(self.preCycleRainPulse_WaveB) / 2f + Mathf.Cos(self.preCycleRainPulse_WaveC) * RainAlpha * 2f) * RainInst;
            return orig(self);
        }

        private static float RainDarkPaletteHook(Func<RainCycle, float> orig, RainCycle self)
        {
            if (RainInst > 0)
                return RainAlpha * 0.85f;
            return orig(self);
        }

        private static float ScreenShakeHook(Func<RainCycle, float> orig, RainCycle self)
        {
            if (RainInst > 0)
                return Mathf.Clamp(self.preCycleRain_Intensity, 0.15f, 1f) / 3f;
            return orig(self);
        }


        public static float RainInst;
        public static float RainAlpha;


        private static void RainCycle_Update(On.RainCycle.orig_Update orig, RainCycle self)
        {

            if (!isInit)
            {
                rainTuples =
                    new (int center, int duringTime)[TipherethBuffData.Tiphereth.GetBuffData<TipherethBuffData>().CycleUse + 1];
                var maxLength = self.cycleLength / (rainTuples.Length + 1);
                var minLength = self.cycleLength / (rainTuples.Length + 1) / (8 / rainTuples.Length);
                for (int i = 0; i < rainTuples.Length; i++)
                    rainTuples[i] = ((int)((self.cycleLength + 1f) / rainTuples.Length), Random.Range(minLength, maxLength));
                isInit = true;
            }
            var (inst, alpha) = rainTuples.Max(i => Intensity(self, i.center, i.duringTime));
            RainInst = inst;
            RainAlpha = alpha;
            orig(self);


            if (self.preTimer < 5000)
            {
                if (RainInst > 0)
                    self.preTimer = 2500;
                self.world.game.globalRain.preCycleRainPulse_Intensity = inst;
                self.preCycleRainPulse_WaveA += 0.006f;
                self.preCycleRainPulse_WaveB += 0.01f;
                self.preCycleRainPulse_WaveC += 0.003f;
      
                self.world.game.globalRain.preCycleRainPulse_Scale =
                    Mathf.Lerp(self.world.game.globalRain.preCycleRainPulse_Scale, alpha, 0.03f);


                if (self.world.game.globalRain.drainWorldFastDrainCounter > 0)
                    self.world.game.globalRain.drainWorldFastDrainCounter--;

                self.world.game.globalRain.drainWorldFlood -= self.world.game.globalRain.drainWorldDrainSpeed * (1f + Mathf.InverseLerp(0f,
                        self.cycleLength / 2, self.timer) +
                    Mathf.InverseLerp(self.cycleLength / 2, self.cycleLength, self.timer) +
                    6f * Mathf.InverseLerp(self.cycleLength / 8 * 7f,
                        self.cycleLength, self.timer)) * (0.66f + Mathf.Sin(self.timer / 30) / 3f);
                if (self.world.game.IsStorySession)
                {
                    if (self.world.game.GetStorySession.saveStateNumber != MoreSlugcatsEnums.SlugcatStatsName.Rivulet)
                        self.world.game.globalRain.drainWorldFlood -= 0.522f;

                    else
                        self.world.game.globalRain.drainWorldFlood -= 0.21f;
                }
                self.world.game.globalRain.drainWorldFlood += Mathf.Pow(self.world.game.globalRain.Intensity * 2f, 4f) * 1.85f;
                self.world.game.globalRain.drainWorldFlood -=
                    ((self.world.game.GetStorySession.saveStateNumber != MoreSlugcatsEnums.SlugcatStatsName.Rivulet) ? 0.55f : 0.61f) *
                    Mathf.InverseLerp(0f, 200f, (float)self.world.game.globalRain.drainWorldFastDrainCounter);

                if (self.world.game.globalRain.drainWorldFlood <= 0f)
                    self.world.game.globalRain.drainWorldFlood = 0f;


            }

        }

        private static (float inst, float alpha) Intensity(RainCycle cycle, int center, int duringTime)
        {
            float alpha = Mathf.Min(Mathf.InverseLerp(center, center - duringTime / 2, cycle.timer),
                Mathf.InverseLerp(center, center + duringTime / 2, cycle.timer));
            float edge = 1f - Mathf.Pow(alpha, 24f);
            return (Mathf.Sin(cycle.preCycleRainPulse_WaveA) + Mathf.Sin(cycle.preCycleRainPulse_WaveB) / 2f +
                    Mathf.Cos(cycle.preCycleRainPulse_WaveC)
                    * (1 - alpha) * 2 * edge, 1 - alpha);
        }
   
        private static void RainCycle_ctor(On.RainCycle.orig_ctor orig, RainCycle self, World world, float minutes)
        {
            world.game.rainWorld.setup.forcePrecycles = true;
            orig(self, world, minutes);
            world.game.rainWorld.setup.forcePrecycles = false;
        }
    }
}
