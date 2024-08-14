using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Positive;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;

namespace BuiltinBuffs.Negative.SephirahMeltdown
{
    internal class HokmaBuffData : SephirahMeltdownBuffData
    {
        public static readonly BuffID Hokma = new BuffID(nameof(Hokma), true);

        public override BuffID ID => Hokma;
        public int FramePreSecond => (int)Custom.LerpMap(CycleUse, 0, MaxCycleCount - 1, 45, 70);


    }

    internal class HokmaBuff : Buff<HokmaBuff, HokmaBuffData>
    {
        public override BuffID ID => HokmaBuffData.Hokma;

        private SingleColorEffect effect;
        public HokmaBuff()
        {
            BuffPostEffectManager.AddEffect(effect = new SingleColorEffect(0, Color.black, Color.white,
                Custom.LerpMap(Data.CycleUse, 0, Data.MaxCycleCount - 1, 0, 1f,0.5f)));
        }
        public override void Destroy()
        {
            base.Destroy();
            effect.Destroy();
        }
    }

    internal class SingleColorEffect : BuffPostEffect
    {
        public SingleColorEffect(int layer,  Color start, Color end, float maxInst) : base(layer)
        {
            this.start = start;
            this.end = end;
            this.maxInst = maxInst;
            material = new Material(StormIsApproachingEntry.SingleColor);

        }



        public override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            material.SetColor("singleColorStart", start);
            material.SetColor("singleColorEnd", end);
            material.SetFloat("lerpValue", maxInst );

            Graphics.Blit(source, destination, material);
        }

        private Color start;
        private Color end;
        private float maxInst;


    }

    internal class HokmaHook
    {
        private static int count = 0;

        public static void HookOn()
        {
            new Hook(typeof(RainWorldGame).GetProperty(nameof(RainWorldGame.GamePaused)).GetGetMethod(),
                typeof(HokmaHook).GetMethod("RainWorldGamePausedHook", BindingFlags.NonPublic | BindingFlags.Static));
            IL.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
            On.Menu.PauseMenu.ctor += PauseMenu_ctor;
            On.Player.checkInput += Player_checkInput;
            IL.RainWorldGame.Update += RainWorldGame_Update;
            IL.RainWorldGame.GrafUpdate += RainWorldGame_Update;

            count = 0;
        }

        private static void Player_checkInput(On.Player.orig_checkInput orig, Player self)
        {
            orig(self);
            if (self.input[0].mp && !self.input[1].mp && BinahBuff.Instance == null && self.abstractCreature.world.game.Players.Count == 1 && SephirahMeltdownEntry.Hell)
            {
                BuffPostEffectManager.AddEffect(new CutEffect(0, 0.8f, 0.02f, 0.5f, 11, true)
                    { inst = 0.3f, IgnoreGameSpeed = true, IgnorePaused = true });
                self.abstractCreature.world.game.cameras[0].virtualMicrophone.PlaySound(StormIsApproachingEntry.EndSound2, 1, 0.1f, 1);
                foreach (var player in self.abstractCreature.world.game.AlivePlayers)
                {
                    if (player.realizedCreature is Player crit)
                        crit.Stun(200 + count * 80);
                }

                count++;
            }
        }



        private static void RainWorldGame_Update(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After, i => i.MatchCall<RainWorldGame>("get_GamePaused"));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<bool,RainWorldGame,bool>>((b,game) => game.pauseMenu != null);
        }



        private static bool RainWorldGamePausedHook(Func<RainWorldGame, bool> orig, RainWorldGame self)
        {
            var re = orig(self);
            return self.pauseMenu != null;
        }

        private static void PauseMenu_ctor(On.Menu.PauseMenu.orig_ctor orig, Menu.PauseMenu self, ProcessManager manager, RainWorldGame game)
        {


            BuffPostEffectManager.AddEffect(new CutEffect(0, 0.8f, 0.02f, 0.5f, 11, true)
            { inst = 0.3f, IgnoreGameSpeed = true, IgnorePaused = true });
            game.cameras[0].virtualMicrophone.PlaySound(StormIsApproachingEntry.EndSound2, 1, 0.1f, 1);
            foreach (var player in game.AlivePlayers)
            {
                if (player.realizedCreature is Player crit)
                    crit.Stun(200 + count * 80);
            }

            count++;


            orig(self, manager, game);
        }

       



        private static void RainWorldGame_RawUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(i => i.MatchCall<MainLoopProcess>("RawUpdate"));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<RainWorldGame>>((self) =>
            {
                self.framesPerSecond = HokmaBuffData.Hokma.GetBuffData<HokmaBuffData>().FramePreSecond;
               
            });
        }
    }
}
