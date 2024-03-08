using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BuiltinBuffs.Positive
{


    internal class DesolateDiveBuffEntry : IBuffEntry
    {
        public static BuffID desolateDiveBuffID = new BuffID("DesolateDive", true);
        static float DiveVelThreshold = 18f;

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<DesolateDiveBuffEntry>(desolateDiveBuffID);
        }

        public static void HookOn()
        {
            On.Player.TerrainImpact += Player_TerrainImpact;
            On.Player.Collide += Player_Collide;
        }

        private static void Player_Collide(On.Player.orig_Collide orig, Player self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            if (self.bodyChunks[myChunk].vel.magnitude / 1.4f > DiveVelThreshold)
            {
                float speed = self.bodyChunks[myChunk].vel.magnitude / 1.4f;

                self.room.AddObject(new ShockWave(self.bodyChunks[myChunk].pos, speed * 4, speed / 96f, 4));

                self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Death, self.mainBodyChunk);
                foreach (var obj in self.room.updateList)
                {
                    if (obj is Creature creature && creature != self)
                    {
                        if ((creature.DangerPos - self.DangerPos).magnitude < speed * 4f)
                            creature.stun += Mathf.CeilToInt(speed * 2);
                    }
                }
            }
            orig.Invoke(self, otherObject, myChunk, otherChunk);
        }

        private static void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player self, int chunk, RWCustom.IntVector2 direction, float speed, bool firstContact)
        {
            if (firstContact && self.room != null && speed > DiveVelThreshold && direction.y < 0)
            {
                self.room.AddObject(new ShockWave(self.bodyChunks[chunk].pos, speed * 6, speed / 96f, 4));

                self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Death, self.mainBodyChunk);
                foreach(var obj in self.room.updateList)
                {
                    if(obj is Creature creature && creature != self)
                    {
                        if ((creature.DangerPos - self.DangerPos).magnitude < speed * 4f)
                            creature.stun += Mathf.CeilToInt(speed * 2);
                    }
                }
            }
            orig.Invoke(self, chunk, direction, speed, firstContact);
        }
    }
}
