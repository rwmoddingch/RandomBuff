using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff;
using RandomBuff.Core.Entry;

namespace BuiltinBuffs.Negative
{
    internal class ParonychiaBuff
    {
    }

    internal class ParonychiaBuffData
    {
    }

    internal class ParonychiaIBuffEntry : IBuffEntry
    {
        public void OnEnable()
        {
            
        }

        public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig(self,sLeaser, rCam, timeStacker, camPos);
            sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("Face" + (self.player.dead ? "Dead" : "Stunned"));
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            self.slugcatStats.runspeedFac *= 0.5f;
            self.slugcatStats.corridorClimbSpeedFac *= 0.5f;
            self.slugcatStats.poleClimbSpeedFac *= 0.5f;
        }
    }
}
