using Noise;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HotDogGains.Negative
{
    class TemblorBuff : Buff<TemblorBuff, TemblorBuffData> { public override BuffID ID => TemblorBuffEntry.TemblorID; }
    class TemblorBuffData : CountableBuffData
    {
        public override BuffID ID => TemblorBuffEntry.TemblorID;
        public override int MaxCycleCount => 1;
    }
    class TemblorBuffEntry : IBuffEntry
    {
        public static BuffID TemblorID = new BuffID("TemblorID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<TemblorBuff, TemblorBuffData, TemblorBuffEntry>(TemblorID);
        }
        public static void HookOn()
        {
            On.RainWorldGame.Update += RainWorldGame_Update;
        }

        private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig.Invoke(self);
            if (self.roomRealizer != null)
            {
                foreach (var absRoom in self.world.activeRooms)
                {
                    if (absRoom != null && Random.value > 0.9f)
                    {
                        float power = Random.value;
                        var room = absRoom;
                        var vibPos = new Vector2(Random.Range(0, room.PixelWidth), Random.Range(0, room.PixelHeight));
                        room.ScreenMovement(vibPos, Custom.RNV(), power * 10f);

                        if (room.PlayersInRoom!=null)
                        {
                            foreach (var player in room.PlayersInRoom.Where(i => i != null))
                            {
                                if (player.bodyMode == Player.BodyModeIndex.Stand && power > 0.8f && Random.value > 0.4f)
                                {
                                    player.standing = false;
                                }
                            }

                        }
                        //room.InGameNoise(new InGameNoise(vibPos,Random.value*30,room.physicalObjects,1));
                    }
                }
            }


        }
    }
}