using RandomBuff;
using RandomBuff;
using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;

namespace BuiltinBuffs.Duality
{
    internal class ArmstronBuff : Buff<ArmstronBuff, ArmstronBuffData>
    {
        public override BuffID ID => ArmstronIBuffEntry.ArmstronBuffID;
    }

    internal class ArmstronBuffData : BuffData
    {
        public override BuffID ID => ArmstronIBuffEntry.ArmstronBuffID;
    }

    internal class ArmstronIBuffEntry : IBuffEntry
    {
        public static BuffID ArmstronBuffID = new BuffID("Armstron", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ArmstronBuff, ArmstronBuffData, ArmstronIBuffEntry>(ArmstronBuffID);
        }

        public static void HookOn()
        {
            On.Room.ctor += Room_ctor;
            On.AntiGravity.Update += AntiGravity_Update;
        }

        private static void AntiGravity_Update(On.AntiGravity.orig_Update orig, AntiGravity self, bool eu)
        {
            orig.Invoke(self, eu);
            if (!self.active)
                return;
            self.room.gravity /= 6f;
        }

        private static void Room_ctor(On.Room.orig_ctor orig, Room self, RainWorldGame game, World world, AbstractRoom abstractRoom)
        {
            orig.Invoke(self, game, world, abstractRoom);
            if (game.session is StoryGameSession storyGameSession)
            {
                if (storyGameSession.saveState.miscWorldSaveData.EverMetMoon)
                {
                    self.gravity /= 6f;
                }
            }
        }
    }
}
