
using MoreSlugcats;
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
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
            On.AbstractCreature.Update += AbstractCreature_Update;
        }

        private static void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            orig(self,timeStacker, timeSpeed);
            Shader.DisableKeyword("SNOW_ON");
            Shader.EnableKeyword("HR");

        }

        private static void AbstractCreature_Update(On.AbstractCreature.orig_Update orig, AbstractCreature self, int time)
        {
            orig(self, time);
            if (!self.lavaImmune && self.creatureTemplate.type != CreatureTemplate.Type.Slugcat)
                self.lavaImmune = true;
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
            if (self.waterObject != null && !self.abstractRoom.shelter)
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

        public bool isInit = false;

        public override void Update(RainWorldGame game)
        {
            base.Update(game);
            if (!isInit && game.cameras != null && game.cameras[0]?.room != null)
            {
                game.cameras[0].ChangeBothPalettes(31, 75, 0.75f);
                game.cameras[0].ApplyEffectColorsToAllPaletteTextures(15, 13);
                if(game.cameras[0]?.room.waterObject != null)
                    game.cameras[0].room.waterObject.WaterIsLethal = true;
                Shader.EnableKeyword("HR");
                isInit = true;
            }
        }

        public override void Destroy()
        {
            Shader.DisableKeyword("HR");
            base.Destroy();
        }
    }
}
