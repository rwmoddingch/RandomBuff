
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;

namespace BuiltinBuffs.Negative
{
    internal class HellIBuffEntry : IBuffEntry
    {
        public static BuffID hellBuffID = new BuffID("Hell", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<HellBuff,HellBuffData,HellIBuffEntry>(hellBuffID);
        }

        public static void HookOn()
        {
            On.Room.Loaded += Room_Loaded;
            On.RoomCamera.ChangeRoom += RoomCamera_ChangeRoom;
        }

        private static void RoomCamera_ChangeRoom(On.RoomCamera.orig_ChangeRoom orig, RoomCamera self, Room newRoom, int cameraPosition)
        {
            orig(self, newRoom, cameraPosition);
            self.ChangeBothPalettes(31,75,0.75f);
            self.ApplyEffectColorsToAllPaletteTextures(15,13);
            Shader.EnableKeyword("HR");
        }

        private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            orig(self);
            if (self.waterObject != null)
                self.waterObject.WaterIsLethal = true;
        }
    }

    class HellBuffData : BuffData
    {
        public override BuffID ID => HellIBuffEntry.hellBuffID;
    }

    class HellBuff : Buff<HellBuff, HellBuffData>
    {
        public override BuffID ID => HellIBuffEntry.hellBuffID;

        
    }
}
