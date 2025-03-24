using MonoMod.RuntimeDetour;
using On.Menu.Remix.MixedUI;

namespace RandomBuffUtils.MixedUI;

internal static class MixedUIHooks
{
    public static void OnModsInit()
    {
        On.Menu.Remix.MixedUI.OpScrollBox._UpdateCam += OpScrollBox__UpdateCam;
        On.Menu.Remix.MixedUI.OpScrollBox._MoveCam += OpScrollBox__MoveCam;
        On.Menu.Remix.MixedUI.OpScrollBox.Update += OpScrollBox_Update;
    }

    private static void OpScrollBox_Update(OpScrollBox.orig_Update orig, Menu.Remix.MixedUI.OpScrollBox self)
    {
        if (self is BindOpScrollBox)
            return;
        orig(self);
    }

    private static void OpScrollBox__MoveCam(OpScrollBox.orig__MoveCam orig, Menu.Remix.MixedUI.OpScrollBox self)
    {
        if (self is BindOpScrollBox)
            return;
        orig(self);
    }

    private static void OpScrollBox__UpdateCam(OpScrollBox.orig__UpdateCam orig, Menu.Remix.MixedUI.OpScrollBox self)
    {
        if (self is BindOpScrollBox)
            return;
        orig(self);
    }
}