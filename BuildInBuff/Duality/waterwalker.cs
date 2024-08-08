
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuildInBuff.Duality
{
    class waterwalkerBuff : Buff<waterwalkerBuff, waterwalkerBuffData> { public override BuffID ID => waterwalkerBuffEntry.waterwalkerID; }
    class waterwalkerBuffData : BuffData { public override BuffID ID => waterwalkerBuffEntry.waterwalkerID; }
    class waterwalkerBuffEntry : IBuffEntry
    {
        public static BuffID waterwalkerID = new BuffID("waterwalkerID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<waterwalkerBuff, waterwalkerBuffData, waterwalkerBuffEntry>(waterwalkerID);
        }
        public static void HookOn()
        {
            On.Player.MovementUpdate += Player_MovementUpdate;
            On.PhysicalObject.IsTileSolid += PhysicalObject_IsTileSolid;
        }

        public static float walkRange = 10;
        public static bool canWalk(Player self)
        {
            foreach (var body in self.bodyChunks)
            {
                var waterPosY = (self.room.FloatWaterLevel(body.pos.x) + body.rad);
                if (body.pos.y < waterPosY && Math.Abs(body.pos.y - waterPosY) < walkRange && !self.GoThroughFloors)
                {
                    return true;
                }
            }
            return false;
        }
        private static bool PhysicalObject_IsTileSolid(On.PhysicalObject.orig_IsTileSolid orig, PhysicalObject self, int bChunk, int relativeX, int relativeY)
        {
            bool flag = orig.Invoke(self, bChunk, relativeX, relativeY);
            var player = self as Player;
            if (player != null &&canWalk(player) && relativeY == -1)
            {
                    return true;
            }
            return flag;
        }
        private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            foreach (var body in self.bodyChunks)
            {
                var waterPosY = (self.room.FloatWaterLevel(body.pos.x) + body.rad) + 2f;
                if (body.pos.y < waterPosY && Math.Abs(body.pos.y - waterPosY) < walkRange)
                {
                    body.contactPoint.y = -1;
                }
            }

            orig.Invoke(self, eu);
            if (canWalk(self)&& !(self.GoThroughFloors) && self.bodyMode != Player.BodyModeIndex.Swimming)
            {
                foreach (var body in self.bodyChunks)
                {
                    var waterPosY = (self.room.FloatWaterLevel(body.pos.x) + body.rad) + 1;
                    if (body.pos.y < waterPosY && Math.Abs(body.pos.y - waterPosY) < walkRange)
                    {
                        self.canJump = 5;
                        body.vel.y = Custom.LerpMap(waterPosY - body.pos.y, 1, 20, 1, 5);

                        if (self.bodyMode == Player.BodyModeIndex.Stand && body == self.bodyChunks[1] && self.input[0].x == 0)
                        {
                            self.bodyChunks[1].pos.y += 2f;
                        }

                    }
                }
            }


        }
    }
}