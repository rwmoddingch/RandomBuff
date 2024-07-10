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

namespace BuiltinBuffs.Negative.SephirahMeltdown
{
    internal class HokmaBuffData : SephirahMeltdownBuffData
    {
        public static readonly BuffID Hokma = new BuffID(nameof(Hokma), true);

        public override BuffID ID => Hokma;
        public int FramePreSecond => (int)Custom.LerpMap(CycleUse, 0, MaxCycleCount - 1, 40, 80);

    }

    internal class HokmaBuff : Buff<HokmaBuff, HokmaBuffData>
    {
        public override BuffID ID => HokmaBuffData.Hokma;
    }

    internal class HokmaHook
    {
        public static void HookOn()
        {
            new Hook(typeof(RainWorldGame).GetProperty(nameof(RainWorldGame.GamePaused)).GetGetMethod(),
                typeof(HokmaHook).GetMethod("RainWorldGamePausedHook", BindingFlags.NonPublic | BindingFlags.Static));
            IL.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
            On.Menu.PauseMenu.ctor += PauseMenu_ctor;
            IL.RainWorldGame.Update += RainWorldGame_Update;
            new Hook(typeof(RoomCamera).GetProperty(nameof(RoomCamera.roomSafeForPause)).GetGetMethod(),
                typeof(HokmaHook).GetMethod("RoomCameraRoomSafeForPauseHook", BindingFlags.NonPublic | BindingFlags.Static));

        }

        private static bool RoomCameraRoomSafeForPauseHook(Func<RoomCamera, bool> orig, RoomCamera self)
        {
            return orig(self) && self.game.pauseMenu == null;
        }

        private static void PauseMenu_ctor(On.Menu.PauseMenu.orig_ctor orig, Menu.PauseMenu self, ProcessManager manager, RainWorldGame game)
        {
            BuffPostEffectManager.AddEffect(new CutEffect(0, 0.8f, 0.02f, 0.5f, 6, true));
            orig(self, manager, game);
        }

        private static void RainWorldGame_Update(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After, i => i.MatchCall<RainWorldGame>("get_GamePaused"));
            c.EmitDelegate<Func<bool, bool>>(b => false);
        }

 

        private static bool RainWorldGamePausedHook(Func<RainWorldGame, bool> orig, RainWorldGame self)
        {
            var re = orig(self);
            return false;
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
