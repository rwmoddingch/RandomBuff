using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive
{


    internal class DozerBuffEntry : IBuffEntry
    {
        public static BuffID dozerBuffID = new BuffID("Dozer", true);
        static float VelocityThreshold = 4f;
        static float RangeExpandFactor = 0.1f;

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<DozerBuffEntry>(dozerBuffID);
        }

        public static void HookOn()
        {
            On.Player.Update += Player_Update;
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            //BuffPlugin.Log("Dozer Update");

            if (self.room == null)
                return;

            bool reachLimit = false;
            foreach(var chunk in self.bodyChunks)
            {
                if((chunk.lastPos - chunk.pos).magnitude > VelocityThreshold / 40f)
                {
                    reachLimit = true;
                    
                    if(chunk.ContactPoint.y == -1)
                    {
                        for(int i = 0;i < Random.Range(0, 2); i++)
                        {
                            self.room.AddObject(new WaterDrip(chunk.pos, -chunk.vel + Vector2.up * 4f + Custom.RNV() * 4, true));
                        }
                    }
                }
            }

            if (!reachLimit)
                return;

            foreach (var physicalObj in self.room.physicalObjects[self.collisionLayer])
            {
                if (physicalObj is Player)
                    continue;

                if (Mathf.Abs(self.bodyChunks[0].pos.x - physicalObj.bodyChunks[0].pos.x) < self.collisionRange + physicalObj.collisionRange && 
                    Mathf.Abs(self.bodyChunks[0].pos.y - physicalObj.bodyChunks[0].pos.y) < self.collisionRange + physicalObj.collisionRange)
                {
                    bool anyGrabbed = false;
                    foreach (Creature.Grasp grasp in self.grasps)
                    {
                        if (grasp != null && grasp.grabbed == physicalObj)
                        {
                            anyGrabbed = true;
                            break;
                        }
                    }

                    if (!anyGrabbed && physicalObj is Creature crit && crit.Template.grasps > 0)
                    {
                        foreach (Creature.Grasp grasp2 in crit.grasps)
                        {
                            if (grasp2 != null && grasp2.grabbed == self)
                            {
                                anyGrabbed = true;
                                break;
                            }
                        }
                    }

                    if (!anyGrabbed)
                    {
                        foreach (var playerChunk in self.bodyChunks)
                        {
                            foreach (var objChunk in physicalObj.bodyChunks)
                            {
                                if (playerChunk.collideWithObjects && objChunk.collideWithObjects && Custom.DistLess(playerChunk.pos, objChunk.pos, playerChunk.rad + objChunk.rad + RangeExpandFactor))
                                {
                                    float totalRad = playerChunk.rad + objChunk.rad;
                                    float distance = Vector2.Distance(playerChunk.pos, objChunk.pos);
                                    Vector2 dir = Custom.DirVec(playerChunk.pos, objChunk.pos);

                                    objChunk.vel += (totalRad - distance) * dir * 8f;

                                    for (int i = 0; i < Random.Range(4, 10); i++)
                                    {
                                        self.room.AddObject(new WaterDrip(objChunk.pos, (totalRad - distance) * dir + Vector2.up * 5f + Custom.RNV() * 2, true));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
