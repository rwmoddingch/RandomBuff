using Mono.Cecil.Cil;
using MonoMod.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HotDogGains.Negative
{
    class ArmedKingVultureBuff : Buff<ArmedKingVultureBuff, ArmedKingVultureBuffData> { public override BuffID ID => ArmedKingVultureBuffEntry.ArmedKingVultureID; }
    class ArmedKingVultureBuffData : BuffData { public override BuffID ID => ArmedKingVultureBuffEntry.ArmedKingVultureID; }
    class ArmedKingVultureBuffEntry : IBuffEntry
    {
        public static BuffID ArmedKingVultureID = new BuffID("ArmedKingVultureID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ArmedKingVultureBuff, ArmedKingVultureBuffData, ArmedKingVultureBuffEntry>(ArmedKingVultureID);

            On.KingTusks.Tusk.InitiateSprites += Tusk_InitiateSprites;
            On.KingTusks.Tusk.AddToContainer += Tusk_AddToContainer;
            On.KingTusks.Tusk.ApplyPalette += Tusk_ApplyPalette;
            On.KingTusks.Tusk.DrawSprites += Tusk_DrawSprites;


            On.KingTusks.Tusk.Update += Tusk_Update;
            On.KingTusks.TryToShoot += KingTusks_TryToShoot;
            On.KingTusks.Tusk.UpdateTuskColors += Tusk_UpdateTuskColors;

            
        }

        public static void HookOn()
        {
            On.KingTusks.ctor += KingTusks_ctor;
            On.KingTusks.Tusk.Shoot += Tusk_Shoot;
            On.KingTusks.GoodShootAngle += KingTusks_GoodShootAngle;
            IL.KingTusks.Update += KingTusks_Update;
        }

        private static void KingTusks_Update(MonoMod.Cil.ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchCall<KingTusks>("TryToShoot"),
                i => i.Match(OpCodes.Br_S)
                ))
            {
                c.Remove();
                c.EmitDelegate<Action<KingTusks>>(
                   (kingTusks) =>
                   {
                       //Debug.Log("检测是否有其他在蓄力");
                       for (int i = 0; i < kingTusks.tusks.Length; i++)
                       {
                           if (kingTusks.tusks[i].mode==KingTusks.Tusk.Mode.Charging)return;
                       }
                       kingTusks.TryToShoot();
                   }
               );
            }
        }

        private static void Tusk_Shoot(On.KingTusks.Tusk.orig_Shoot orig, KingTusks.Tusk self, Vector2 tuskHangPos)
        {
            orig.Invoke(self, tuskHangPos);

            for (int i = 0; i < self.vulture.kingTusks.tusks.Length; i++)
            {
                self.vulture.kingTusks.tusks[i].ArmeKingTusk().shootCD = Custom.LerpAndTick(25, 1, self.vulture.kingTusks.tusks[i].ArmeKingTusk().shootCD, 12f);
                self.vulture.kingTusks.tusks[i].laserPower = Custom.LerpMap(self.vulture.kingTusks.tusks[i].ArmeKingTusk().shootCD, 25, 20, 0, 1);
            }
            self.owner.noShootDelay =(int)Mathf.Min(self.ArmeKingTusk().shootCD, self.owner.noShootDelay);

        }

        public static float spacing = 0.8f;
        private static void KingTusks_TryToShoot(On.KingTusks.orig_TryToShoot orig, KingTusks self)
        {
            if (self.tusks.Length <= 2)
            {
                orig.Invoke(self);
                return;
            }

            int num = Random.Range(0, 5);
            //int num = Random.Range(0, self.tusks.Length);
            for (int i = num; i < self.tusks.Length; i++)
            {
                if (self.tusks[i].ReadyToShoot)
                {
                    num = i;
                    break;
                }
                if ((i + 1) >= self.tusks.Length) return;
            }
            self.tusks[num].SwitchMode(KingTusks.Tusk.Mode.Charging);
            self.vulture.room.PlaySound(SoundID.King_Vulture_Tusk_Aim, self.vulture.bodyChunks[4]);
            if (!Custom.DistLess(self.vulture.bodyChunks[1].lastPos, self.vulture.bodyChunks[1].pos, 5f))
            {
                self.vulture.AirBrake(15);
            }
        }

        private static void Tusk_Update(On.KingTusks.Tusk.orig_Update orig, KingTusks.Tusk self)
        {
            if (self.side < 2)
            {
                orig.Invoke(self);
                return;
            }
            else
            {
                //恢复射击加成
                self.ArmeKingTusk().shootCD = Custom.LerpAndTick(0, 25, self.ArmeKingTusk().shootCD, 0.01f);

                self.lastZRot = self.zRot;
                self.lastWireLoose = self.wireLoose;
                self.lastLaserAlpha = self.laserAlpha;
                self.zRot = Vector3.Slerp(self.zRot, Custom.DegToVec(self.owner.headRot + ((self.side % 2 == 0) ? -90f : 90f)), 0.9f * self.attached);
                Vector2 vector = Custom.DirVec(self.vulture.neck.tChunks[self.vulture.neck.tChunks.Length - 1].pos, self.vulture.bodyChunks[4].pos);
                Vector2 a = Custom.PerpendicularVector(vector);
                Vector2 vector2 = self.vulture.bodyChunks[4].pos + vector * -5f;

                vector2 += a * self.zRot.x * 15f * (self.side / 2f * spacing);
                vector2 += a * self.zRot.y * ((self.side % 2 == 0) ? -1f : 1f) * 7f;
                //vector2 += a * self.zRot.y * ((self.side%2 == 0) ? -1f : 1f) * 7f*(self.side/2f*0.2f);//间距相关

                self.laserPower = Custom.LerpAndTick(self.laserPower, self.attached, 0.01f, 0.008333334f);

                bool charging = false;
                for (int i = 0; i < self.owner.tusks.Length; i++)
                {
                    if (self.owner.tusks[i].mode == KingTusks.Tusk.Mode.Charging && i != self.side) charging = true;
                }

                //int shootCD = 0;
                //for (int i = 0; i < self.owner.tusks.Length; i++)
                //{
                //    if (self.owner.tusks[i].mode == KingTusks.Tusk.Mode.Charging && i != self.side) charging = true;
                //}

                if (charging || !self.vulture.Consious) self.laserAlpha = Mathf.Max(self.laserAlpha - 0.1f, 0f);
                else if (Random.value < 0.25f)
                {
                    self.laserAlpha = ((Random.value < self.laserPower) ? Mathf.Lerp(self.laserAlpha, Mathf.Pow(self.laserPower, 0.25f), Mathf.Pow(Random.value, 0.5f)) : (self.laserAlpha * Random.value * Random.value));
                }

                self.modeCounter++;
                if (self.mode != KingTusks.Tusk.Mode.ShootingOut)
                {
                    self.wireExtraSlack = Mathf.Max(0f, self.wireExtraSlack - 0.033333335f);
                    self.elasticity = Mathf.Min(0.9f, self.elasticity + 0.025f);
                }
                if (self.mode == KingTusks.Tusk.Mode.Attached)
                {
                    self.attached = 1f;
                }
                else if (self.mode == KingTusks.Tusk.Mode.Charging)
                {
                    self.attached = Custom.LerpMap((float)self.modeCounter, 0f, 25f, 0.2f, 1f);

                    if (self.modeCounter > Mathf.Min((self.owner.CloseQuarters ? 10 : 25),self.ArmeKingTusk().shootCD))
                    {
                        if (self.vulture.Consious && (self.owner.targetRep != null || self.vulture.safariControlled) && self.owner.noShootDelay < 1 && (self.vulture.safariControlled || self.owner.GoodShootAngle(self.side, false) > (self.owner.CloseQuarters ? 0.6f : 0.4f)) && (self.owner.VisualOnAnyTargetChunk() || self.vulture.safariControlled))
                        {
                            self.Shoot(vector2);
                        }
                        else
                        {
                            self.room.PlaySound(SoundID.King_Vulture_Tusk_Cancel_Shot, self.chunkPoints[0, 0]);
                            self.SwitchMode(KingTusks.Tusk.Mode.Attached);
                            self.owner.noShootDelay = Mathf.Max(self.owner.noShootDelay, 10);
                            Debug.Log("cancel shot");
                        }
                    }
                    if (self.modeCounter % 6 == 0)
                    {
                        self.room.PlaySound(SoundID.King_Vulture_Tusk_Aim_Beep, self.chunkPoints[0, 0]);
                    }
                }
                else if (self.mode == KingTusks.Tusk.Mode.ShootingOut)
                {
                    self.attached = 0f;
                    self.currWireLength = KingTusks.Tusk.maxWireLength;
                    if (self.modeCounter > (self.room.PointSubmerged(self.chunkPoints[0, 0]) ? 6 : 10))
                    {
                        self.SwitchMode(KingTusks.Tusk.Mode.Dangling);
                        self.room.PlaySound(SoundID.King_Vulture_Tusk_Wire_End, self.chunkPoints[0, 0], 0.4f, 1f);
                    }
                }
                else if (self.mode == KingTusks.Tusk.Mode.Dangling)
                {
                    self.attached = 0f;
                    if (self.modeCounter > 80)
                    {
                        self.SwitchMode(KingTusks.Tusk.Mode.Retracting);
                    }
                }
                else if (self.mode == KingTusks.Tusk.Mode.Retracting)
                {
                    if (self.currWireLength > 0f)
                    {
                        self.currWireLength = Mathf.Max(0f, self.currWireLength - KingTusks.Tusk.maxWireLength / 90f);
                        self.attached = 0f;
                    }
                    else
                    {
                        float num = self.attached;
                        if (self.attached < 1f)
                        {
                            self.attached = Mathf.Min(1f, self.attached + 0.05f);
                        }
                        else
                        {
                            self.SwitchMode(KingTusks.Tusk.Mode.Attached);
                        }
                        if (num < 0.5f && self.attached >= 0.5f)
                        {
                            self.room.PlaySound(SoundID.King_Vulture_Tusk_Reattach, self.chunkPoints[0, 0]);
                        }
                    }
                }
                else if (self.mode == KingTusks.Tusk.Mode.StuckInCreature)
                {
                    self.attached = 0f;
                    if (self.modeCounter > 80)
                    {
                        self.currWireLength = Mathf.Max(100f, self.currWireLength - KingTusks.Tusk.maxWireLength / 180f);
                    }
                    if (self.impaleChunk == null)
                    {
                        self.SwitchMode(KingTusks.Tusk.Mode.Dangling);
                    }
                }
                else if (self.mode == KingTusks.Tusk.Mode.StuckInWall)
                {
                    self.attached = 0f;
                    if (self.modeCounter > 240)
                    {
                        self.currWireLength = Mathf.Max(100f, self.currWireLength - KingTusks.Tusk.maxWireLength / 180f);
                    }
                    if (self.stuckInWallPos == null || self.stuck <= 0f)
                    {
                        self.SwitchMode(KingTusks.Tusk.Mode.Dangling);
                    }
                    else
                    {
                        for (int i = 0; i < self.chunkPoints.GetLength(0); i++)
                        {
                            self.chunkPoints[i, 1] = self.chunkPoints[i, 0];
                            self.chunkPoints[i, 2] *= 0f;
                        }
                        self.chunkPoints[0, 0] = self.stuckInWallPos.Value;
                        self.chunkPoints[1, 0] = self.stuckInWallPos.Value - self.shootDir * KingTusks.Tusk.length;
                        if (self.rope != null && self.rope.totalLength >= self.currWireLength)
                        {
                            self.chunkPoints[1, 0] += Custom.DirVec(self.chunkPoints[1, 0], self.head.pos) * Random.value * 10f * (1f - self.stuck);
                        }
                    }
                }
                Vector2 a2 = vector;
                if (self.mode == KingTusks.Tusk.Mode.Charging)
                {
                    a2 = Vector3.Slerp(vector, self.AimDir(1f), Mathf.InverseLerp(0f, 25f, (float)self.modeCounter));
                }
                if (!self.StuckOrShooting)
                {
                    Vector2 vector4;
                    for (int j = 0; j < self.chunkPoints.GetLength(0); j++)
                    {
                        self.chunkPoints[j, 1] = self.chunkPoints[j, 0];
                        self.chunkPoints[j, 0] += self.chunkPoints[j, 2];
                        if (self.room.PointSubmerged(self.chunkPoints[j, 0]))
                        {
                            self.chunkPoints[j, 2] *= 0.95f;
                            self.chunkPoints[j, 2].y += 0.1f;
                        }
                        else
                        {
                            self.chunkPoints[j, 2] *= 0.98f;
                            self.chunkPoints[j, 2].y -= 0.9f;
                        }
                        if (!self.FullyAttached && Custom.DistLess(self.chunkPoints[j, 0], self.chunkPoints[j, 1], 200f))
                        {
                            SharedPhysics.TerrainCollisionData terrainCollisionData = self.scratchTerrainCollisionData.Set(self.chunkPoints[j, 0], self.chunkPoints[j, 1], self.chunkPoints[j, 2], 2f, new IntVector2(0, 0), true);
                            terrainCollisionData = SharedPhysics.VerticalCollision(self.room, terrainCollisionData);
                            terrainCollisionData = SharedPhysics.HorizontalCollision(self.room, terrainCollisionData);
                            self.chunkPoints[j, 0] = terrainCollisionData.pos;
                            self.chunkPoints[j, 2] = terrainCollisionData.vel;
                            if ((float)terrainCollisionData.contactPoint.y != 0f)
                            {
                                self.chunkPoints[j, 2].x *= 0.5f;
                            }
                            if ((float)terrainCollisionData.contactPoint.x != 0f)
                            {
                                self.chunkPoints[j, 2].y *= 0.5f;
                            }
                        }
                        if (self.attached > 0f)
                        {
                            Vector2 vector3 = vector2 + a2 * KingTusks.Tusk.length * ((j == 0) ? 0.5f : -0.5f);
                            float num2 = Mathf.Lerp(6f, 1f, self.attached);
                            if (!Custom.DistLess(self.chunkPoints[j, 0], vector3, num2))
                            {
                                vector4 = Custom.DirVec(self.chunkPoints[j, 0], vector3) * (Vector2.Distance(self.chunkPoints[j, 0], vector3) - num2);
                                self.chunkPoints[j, 0] += vector4;
                                self.chunkPoints[j, 2] += vector4;
                            }
                        }
                    }
                    vector4 = Custom.DirVec(self.chunkPoints[0, 0], self.chunkPoints[1, 0]) * (Vector2.Distance(self.chunkPoints[0, 0], self.chunkPoints[1, 0]) - KingTusks.Tusk.length);
                    self.chunkPoints[0, 0] += vector4 / 2f;
                    self.chunkPoints[0, 2] += vector4 / 2f;
                    self.chunkPoints[1, 0] -= vector4 / 2f;
                    self.chunkPoints[1, 2] -= vector4 / 2f;
                }
                self.wireLoose = Custom.LerpAndTick(self.wireLoose, (self.attached > 0f) ? 0f : 1f, 0.07f, 0.033333335f);
                if (self.lastWireLoose == 0f && self.wireLoose == 0f)
                {
                    for (int k = 0; k < self.wire.GetLength(0); k++)
                    {
                        self.wire[k, 0] = self.head.pos + Custom.RNV();
                        self.wire[k, 1] = self.wire[k, 0];
                        self.wire[k, 0] *= 0f;
                    }
                }
                else
                {
                    float num3 = 1f;
                    if (self.rope != null)
                    {
                        num3 = self.rope.totalLength / (float)self.wire.GetLength(0) * 0.5f;
                    }
                    num3 *= self.wireLoose;
                    num3 += 10f * self.wireExtraSlack;
                    float num4 = Mathf.InverseLerp(self.currWireLength * 0.75f, self.currWireLength, (self.rope != null) ? self.rope.totalLength : Vector2.Distance(self.head.pos, self.chunkPoints[1, 0]));
                    num4 *= 1f - self.wireExtraSlack;
                    for (int l = 0; l < self.wire.GetLength(0); l++)
                    {
                        self.wire[l, 1] = self.wire[l, 0];
                        self.wire[l, 0] += self.wire[l, 2];
                        if (self.room.PointSubmerged(self.wire[l, 0]))
                        {
                            self.wire[l, 2] *= 0.7f;
                            self.wire[l, 2].y += 0.2f;
                        }
                        else
                        {
                            self.wire[l, 2] *= Mathf.Lerp(0.98f, 1f, self.wireExtraSlack);
                            self.wire[l, 2].y -= 0.9f * (1f - self.wireExtraSlack);
                        }
                        if (self.rope != null)
                        {
                            Vector2 a3 = self.OnRopePos(Mathf.InverseLerp(0f, (float)(self.wire.GetLength(0) - 1), (float)l));
                            self.wire[l, 2] += (a3 - self.wire[l, 0]) * (1f - self.wireExtraSlack) / Mathf.Lerp(60f, 2f, num4);
                            self.wire[l, 0] += (a3 - self.wire[l, 0]) * (1f - self.wireExtraSlack) / Mathf.Lerp(60f, 2f, num4);
                            self.wire[l, 0] = Vector2.Lerp(a3, self.wire[l, 0], self.wireLoose);
                            if (self.wire[l, 3].x == 0f && self.wireLoose == 1f && Custom.DistLess(self.wire[l, 0], self.wire[l, 1], 500f))
                            {
                                SharedPhysics.TerrainCollisionData terrainCollisionData2 = self.scratchTerrainCollisionData.Set(self.wire[l, 0], self.wire[l, 1], self.wire[l, 2], 3f, new IntVector2(0, 0), true);
                                terrainCollisionData2 = SharedPhysics.VerticalCollision(self.room, terrainCollisionData2);
                                terrainCollisionData2 = SharedPhysics.HorizontalCollision(self.room, terrainCollisionData2);
                                self.wire[l, 0] = terrainCollisionData2.pos;
                                self.wire[l, 2] = terrainCollisionData2.vel;
                            }
                        }
                        self.wire[l, 3].x = 0f;
                    }
                    for (int m = 1; m < self.wire.GetLength(0); m++)
                    {
                        if (!Custom.DistLess(self.wire[m, 0], self.wire[m - 1, 0], num3))
                        {
                            Vector2 vector4 = Custom.DirVec(self.wire[m, 0], self.wire[m - 1, 0]) * (Vector2.Distance(self.wire[m, 0], self.wire[m - 1, 0]) - num3);
                            self.wire[m, 0] += vector4 / 2f;
                            self.wire[m, 2] += vector4 / 2f;
                            self.wire[m - 1, 0] -= vector4 / 2f;
                            self.wire[m - 1, 2] -= vector4 / 2f;
                        }
                    }
                    if (self.rope != null && self.wireLoose == 1f)
                    {
                        self.AlignWireToRopeSim();
                    }
                    Vector2 vector5 = self.owner.vulture.neck.tChunks[self.owner.vulture.neck.tChunks.Length - 1].pos;
                    vector5 += a * self.zRot.x * 15f;

                    //vector5 += a * self.zRot.y * ((self.side % 2 == 0) ? -1f : 1f) * 7f * (self.side / 2f * 0.4f);
                    vector5 += a * self.zRot.y * ((self.side % 2 == 0) ? -1f : 1f) * 7f;

                    if (!Custom.DistLess(self.wire[0, 0], vector5, num3))
                    {
                        Vector2 vector4 = Custom.DirVec(self.wire[0, 0], vector5) * (Vector2.Distance(self.wire[0, 0], vector5) - num3);
                        self.wire[0, 0] += vector4;
                        self.wire[0, 2] += vector4;
                    }
                    vector5 = self.WireAttachPos(1f);
                    if (!Custom.DistLess(self.wire[self.wire.GetLength(0) - 1, 0], vector5, num3))
                    {
                        Vector2 vector4 = Custom.DirVec(self.wire[self.wire.GetLength(0) - 1, 0], vector5) * (Vector2.Distance(self.wire[self.wire.GetLength(0) - 1, 0], vector5) - num3);
                        self.wire[self.wire.GetLength(0) - 1, 0] += vector4;
                        self.wire[self.wire.GetLength(0) - 1, 2] += vector4;
                    }
                }
                if (self.mode == KingTusks.Tusk.Mode.ShootingOut)
                {
                    self.ShootUpdate(Custom.LerpMap((float)self.modeCounter, 0f, 8f, 50f, 30f, 3f));
                }
                if (self.impaleChunk != null)
                {
                    if (!(self.impaleChunk.owner is Creature) || self.mode != KingTusks.Tusk.Mode.StuckInCreature || (self.impaleChunk.owner as Creature).enteringShortCut != null || (self.impaleChunk.owner as Creature).room != self.room)
                    {
                        self.impaleChunk = null;
                    }
                    else if (self.vulture.Consious && self.modeCounter > 20 && Random.value < Custom.LerpMap((float)self.modeCounter, 20f, 80f, 0.0016666667f, 0.033333335f) && !self.owner.DoIWantToHoldCreature(self.impaleChunk.owner as Creature))
                    {
                        if (self.vulture.grasps[0] != null && self.vulture.grasps[0].grabbed == self.impaleChunk.owner)
                        {
                            self.currWireLength = 0f;
                            self.SwitchMode(KingTusks.Tusk.Mode.Retracting);
                        }
                        else
                        {
                            self.SwitchMode(KingTusks.Tusk.Mode.Dangling);
                        }
                        self.impaleChunk = null;
                    }
                    else
                    {
                        for (int n = 0; n < 2; n++)
                        {
                            self.chunkPoints[n, 1] = self.chunkPoints[n, 0];
                            self.chunkPoints[n, 2] *= 0f;
                        }
                        Vector2 vector6 = self.shootDir;
                        if (self.impaleChunk.rotationChunk != null)
                        {
                            vector6 = Custom.RotateAroundOrigo(vector6, Custom.AimFromOneVectorToAnother(self.impaleChunk.pos, self.impaleChunk.rotationChunk.pos));
                        }
                        else if (self.rope != null)
                        {
                            vector6 = Custom.DirVec(self.impaleChunk.pos, self.rope.BConnect);
                        }
                        else
                        {
                            vector6 = Custom.DirVec(self.impaleChunk.pos, self.head.pos);
                        }
                        self.chunkPoints[0, 0] = self.impaleChunk.pos - vector6 * self.impaleChunk.rad;
                        self.chunkPoints[1, 0] = self.impaleChunk.pos - vector6 * (self.impaleChunk.rad + KingTusks.Tusk.length);
                        if (self.vulture.AI.behavior == VultureAI.Behavior.Hunt && self.vulture.grasps[0] == null && self.vulture.AI.focusCreature != null && self.impaleChunk.owner is Creature && (self.impaleChunk.owner as Creature).abstractCreature == self.vulture.AI.focusCreature.representedCreature)
                        {
                            for (int num5 = 0; num5 < self.impaleChunk.owner.bodyChunks.Length; num5++)
                            {
                                if (Custom.DistLess(self.impaleChunk.owner.bodyChunks[num5].pos, self.vulture.bodyChunks[4].pos, self.impaleChunk.owner.bodyChunks[num5].rad + self.vulture.bodyChunks[4].rad))
                                {
                                    Debug.Log("grab impaled");
                                    self.vulture.Grab(self.impaleChunk.owner, 0, num5, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 1f, true, true);
                                    self.room.PlaySound(SoundID.Vulture_Grab_NPC, self.vulture.bodyChunks[4]);
                                    break;
                                }
                            }
                        }
                        if (Random.value < 0.05f && self.impaleChunk.owner.grabbedBy.Count > 0)
                        {
                            for (int num6 = 0; num6 < self.impaleChunk.owner.grabbedBy.Count; num6++)
                            {
                                if (self.impaleChunk.owner.grabbedBy[num6].shareability != Creature.Grasp.Shareability.NonExclusive)
                                {
                                    self.SwitchMode(KingTusks.Tusk.Mode.Dangling);
                                    self.impaleChunk = null;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (self.attached == 0f)
                {
                    Vector2 vector7 = (self.impaleChunk != null) ? self.impaleChunk.pos : self.chunkPoints[1, 0];
                    if (self.rope == null && self.room.VisualContact(self.head.pos, vector7))
                    {
                        self.rope = new Rope(self.room, self.head.pos, vector7, 1f);
                    }
                    if (self.rope != null)
                    {
                        self.rope.Update(self.head.pos, vector7);
                        if (self.rope.totalLength > self.currWireLength)
                        {
                            self.wireExtraSlack = Mathf.Max(0f, self.wireExtraSlack - 0.1f);
                            float num7 = self.stuck;
                            self.stuck -= Mathf.InverseLerp(30f, 90f, (float)self.modeCounter) * Random.value / Custom.LerpMap(self.rope.totalLength / self.currWireLength, 1f, 1.3f, 120f, 10f, 0.7f);
                            if (self.vulture.grasps[0] != null)
                            {
                                self.stuck -= 0.1f;
                            }
                            if (self.mode == KingTusks.Tusk.Mode.StuckInWall && self.stuck <= 0f && num7 > 0f)
                            {
                                self.room.PlaySound(SoundID.King_Vulture_Tusk_Out_Of_Terrain, self.chunkPoints[0, 0], 1f, 1f);
                            }
                            float num8 = self.head.mass / (0.1f + self.head.mass);
                            float d = self.rope.totalLength - self.currWireLength;
                            if (self.mode == KingTusks.Tusk.Mode.StuckInWall)
                            {
                                Vector2 vector4 = Custom.DirVec(self.head.pos, self.rope.AConnect) * d;
                                self.head.pos += vector4 * self.elasticity;
                                self.head.vel += vector4 * self.elasticity;
                            }
                            else if (self.mode == KingTusks.Tusk.Mode.StuckInCreature && self.impaleChunk != null)
                            {
                                num8 = self.head.mass / (self.impaleChunk.mass + self.head.mass);
                                Vector2 vector4 = Custom.DirVec(self.head.pos, self.rope.AConnect) * d;
                                self.head.pos += vector4 * (1f - num8) * self.elasticity;
                                self.head.vel += vector4 * (1f - num8) * self.elasticity;
                                vector4 = Custom.DirVec(self.impaleChunk.pos, self.rope.BConnect) * d;
                                self.impaleChunk.pos += vector4 * num8 * self.elasticity;
                                self.impaleChunk.vel += vector4 * num8 * self.elasticity;
                            }
                            else
                            {
                                Vector2 vector4 = Custom.DirVec(self.head.pos, self.rope.AConnect) * d;
                                self.head.pos += vector4 * (1f - num8) * self.elasticity;
                                self.head.vel += vector4 * (1f - num8) * self.elasticity;
                                vector4 = Custom.DirVec(self.chunkPoints[1, 0], self.rope.BConnect) * d;
                                self.chunkPoints[1, 0] += vector4 * num8 * self.elasticity;
                                self.chunkPoints[1, 2] += vector4 * num8 * self.elasticity;
                            }
                        }
                    }
                    if (self.StuckInAnything && !Custom.DistLess(self.head.pos, self.vulture.bodyChunks[0].pos, self.vulture.neck.idealLength * 0.75f))
                    {
                        Vector2 vector4 = Custom.DirVec(self.head.pos, self.vulture.bodyChunks[0].pos) * (Vector2.Distance(self.head.pos, self.vulture.bodyChunks[0].pos) - self.vulture.neck.idealLength * 0.75f);
                        float num9 = self.head.mass / (self.vulture.bodyChunks[0].mass + self.head.mass);
                        self.head.pos += vector4 * (1f - num9);
                        self.head.vel += vector4 * (1f - num9);
                        self.vulture.bodyChunks[0].pos -= vector4 * num9;
                        self.vulture.bodyChunks[0].vel -= vector4 * num9;
                        return;
                    }
                }
                else
                {
                    if (self.rope != null)
                    {
                        if (self.rope.visualizer != null)
                        {
                            self.rope.visualizer.ClearSprites();
                        }
                        self.rope = null;
                    }
                    for (int num10 = 0; num10 < self.wire.GetLength(0); num10++)
                    {
                        self.wire[num10, 0] = self.head.pos + Custom.RNV();
                    }
                }
            }
        }

        private static void Tusk_DrawSprites(On.KingTusks.Tusk.orig_DrawSprites orig, KingTusks.Tusk self, VultureGraphics vGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            if (self.side < 2)
            {
                orig.Invoke(self, vGraphics, sLeaser, rCam, timeStacker, camPos);
                return;
            }
            var armeTusk = self.ArmeKingTusk();

            if (vGraphics.shadowMode)
            {
                camPos.y -= rCam.room.PixelHeight + 300f;
            }
            if (ModManager.MMF)
            {
                self.UpdateTuskColors(sLeaser);
            }

            Vector2 a = Vector2.Lerp(self.vulture.bodyChunks[4].lastPos, self.vulture.bodyChunks[4].pos, timeStacker);
            Vector2 vector = Custom.DirVec(Vector2.Lerp(self.vulture.neck.tChunks[self.vulture.neck.tChunks.Length - 1].lastPos, self.vulture.neck.tChunks[self.vulture.neck.tChunks.Length - 1].pos, timeStacker), Vector2.Lerp(self.vulture.bodyChunks[4].lastPos, self.vulture.bodyChunks[4].pos, timeStacker));
            Vector2 a2 = Custom.PerpendicularVector(vector);
            Vector2 vector2 = Vector3.Slerp(self.lastZRot, self.zRot, timeStacker);
            float num = Mathf.Lerp(self.lastLaserAlpha, self.laserAlpha, timeStacker);
            Color color = Custom.HSL2RGB(vGraphics.ColorB.hue, 1f, 0.5f);

            if (self.mode == KingTusks.Tusk.Mode.Charging)
            {
                num = ((self.modeCounter % 6 < 3) ? 1f : 0f);
                if (self.modeCounter % 2 == 0)
                {
                    color = Color.Lerp(color, Color.white, Random.value);
                }
            }
            Vector2 vector3 = a + vector * 15f * (self.side / 2f * spacing / 4) + a2 * Vector3.Slerp(Custom.DegToVec(self.owner.lastHeadRot + ((self.side % 2 == 0) ? -90f : 90f)), Custom.DegToVec(self.owner.headRot + ((self.side % 2 == 0) ? -90f : 90f)), timeStacker).x * 7f;
            Vector2 vector4 = self.AimDir(timeStacker);

        
            Vector2 vector5 = (Vector2.Lerp(self.chunkPoints[0, 1], self.chunkPoints[0, 0], timeStacker) + Vector2.Lerp(self.chunkPoints[1, 1], self.chunkPoints[1, 0], timeStacker)) / 2f;
            Vector2 vector6 = Custom.DirVec(Vector2.Lerp(self.chunkPoints[1, 1], self.chunkPoints[1, 0], timeStacker), Vector2.Lerp(self.chunkPoints[0, 1], self.chunkPoints[0, 0], timeStacker));
            Vector2 a3 = Custom.PerpendicularVector(vector6);
            if (self.mode == KingTusks.Tusk.Mode.Charging)
            {
                vector5 += vector6 * Mathf.Lerp(-6f, 6f, Random.value);
            }
            Vector2 vector7 = a - vector6 * 10f;
            Vector2 vector8 = Vector2.Lerp(a, vector5, Mathf.InverseLerp(0f, 0.25f, self.attached));

            #region  魔王枪连接处

            sLeaser.sprites[vGraphics.NeckLumpSprite(self.side%2)].x = vector7.x - camPos.x;
            sLeaser.sprites[vGraphics.NeckLumpSprite(self.side % 2)].y = vector7.y - camPos.y;
            sLeaser.sprites[vGraphics.NeckLumpSprite(self.side % 2)].scaleY = (Vector2.Distance(vector7, vector8) + 4f) / 20f;
            sLeaser.sprites[vGraphics.NeckLumpSprite(self.side % 2)].rotation = Custom.AimFromOneVectorToAnother(vector7, vector8);
            sLeaser.sprites[vGraphics.NeckLumpSprite(self.side % 2)].scaleX = 0.6f;

            #endregion
            Vector2 vector9 = vector5 + vector6 * -35f + a3 * vector2.y * ((self.side % 2 == 0) ? -1f : 1f) * -15f;
            float num2 = 0f;
            for (int i = 0; i < KingTusks.Tusk.tuskSegs; i++)
            {
                float num3 = Mathf.InverseLerp(0f, (float)(KingTusks.Tusk.tuskSegs - 1), (float)i);
                Vector2 vector10 = vector5 + vector6 * Mathf.Lerp(-30f, 60f, num3) + self.TuskBend(num3) * a3 * 20f * vector2.x + self.TuskProfBend(num3) * a3 * vector2.y * ((self.side % 2 == 0) ? -1f : 1f) * 10f;
                Vector2 normalized = (vector10 - vector9).normalized;
                Vector2 a4 = Custom.PerpendicularVector(normalized);
                float d = Vector2.Distance(vector10, vector9) / 5f;
                float num4 = self.TuskRad(num3, Mathf.Abs(vector2.y));
                (sLeaser.sprites[armeTusk.TuskSprite()] as TriangleMesh).MoveVertice(i * 4, vector9 - a4 * (num4 + num2) * 0.5f + normalized * d - camPos);
                (sLeaser.sprites[armeTusk.TuskSprite()] as TriangleMesh).MoveVertice(i * 4 + 1, vector9 + a4 * (num4 + num2) * 0.5f + normalized * d - camPos);
                if (i == KingTusks.Tusk.tuskSegs - 1)
                {
                    var tuskEnd = (vector10 +normalized * d);
                    (sLeaser.sprites[armeTusk.TuskSprite()] as TriangleMesh).MoveVertice(i * 4 + 2, tuskEnd- camPos);

                    #region 魔王枪镭射
                    if (num <= 0f||self.mode!=KingTusks.Tusk.Mode.Charging)
                    {
                        sLeaser.sprites[armeTusk.LaserSprite()].isVisible = false;
                    }

                    else
                    {
                        sLeaser.sprites[armeTusk.LaserSprite()].isVisible = true;
                        sLeaser.sprites[armeTusk.LaserSprite()].alpha = num;
                        vector3 = tuskEnd;
                        Vector2 corner = Custom.RectCollision(vector3, vector3 + vector4 * 100000f, rCam.room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);
                        IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(rCam.room, vector3, corner);
                        if (intVector != null)
                        {
                            corner = Custom.RectCollision(corner, vector3, rCam.room.TileRect(intVector.Value)).GetCorner(FloatRect.CornerLabel.D);
                        }
                        (sLeaser.sprites[armeTusk.LaserSprite()] as CustomFSprite).verticeColors[0] = Custom.RGB2RGBA(color, num);
                        (sLeaser.sprites[armeTusk.LaserSprite()] as CustomFSprite).verticeColors[1] = Custom.RGB2RGBA(color, num);
                        (sLeaser.sprites[armeTusk.LaserSprite()] as CustomFSprite).verticeColors[2] = Custom.RGB2RGBA(color, Mathf.Pow(num, 2f) * ((self.mode == KingTusks.Tusk.Mode.Charging) ? 1f : 0.5f));
                        (sLeaser.sprites[armeTusk.LaserSprite()] as CustomFSprite).verticeColors[3] = Custom.RGB2RGBA(color, Mathf.Pow(num, 2f) * ((self.mode == KingTusks.Tusk.Mode.Charging) ? 1f : 0.5f));
                        (sLeaser.sprites[armeTusk.LaserSprite()] as CustomFSprite).MoveVertice(0, vector3 + vector4 * 2f + Custom.PerpendicularVector(vector4) * 0.5f - camPos);
                        (sLeaser.sprites[armeTusk.LaserSprite()] as CustomFSprite).MoveVertice(1, vector3 + vector4 * 2f - Custom.PerpendicularVector(vector4) * 0.5f - camPos);
                        (sLeaser.sprites[armeTusk.LaserSprite()] as CustomFSprite).MoveVertice(2, corner - Custom.PerpendicularVector(vector4) * 0.5f - camPos);
                        (sLeaser.sprites[armeTusk.LaserSprite()] as CustomFSprite).MoveVertice(3, corner + Custom.PerpendicularVector(vector4) * 0.5f - camPos);
                    }
                    #endregion
                }
                else
                {
                    (sLeaser.sprites[armeTusk.TuskSprite()] as TriangleMesh).MoveVertice(i * 4 + 2, vector10 - a4 * num4 - normalized * d - camPos);
                    (sLeaser.sprites[armeTusk.TuskSprite()] as TriangleMesh).MoveVertice(i * 4 + 3, vector10 + a4 * num4 - normalized * d - camPos);
                }
                num2 = num4;
                vector9 = vector10;
            }
            for (int j = 0; j < (sLeaser.sprites[armeTusk.TuskSprite()] as TriangleMesh).vertices.Length; j++)
            {
                (sLeaser.sprites[armeTusk.TuskDetailSprite()] as TriangleMesh).MoveVertice(j, (sLeaser.sprites[armeTusk.TuskSprite()] as TriangleMesh).vertices[j]);
            }
            if (self.lastWireLoose > 0f || self.wireLoose > 0f)
            {
                sLeaser.sprites[armeTusk.TuskWireSprite()].isVisible = true;
                float num5 = Mathf.Lerp(self.lastWireLoose, self.wireLoose, timeStacker);
                vector9 = a - vector * 14f;
                for (int k = 0; k < self.wire.GetLength(0); k++)
                {
                    Vector2 vector11 = Vector2.Lerp(self.wire[k, 1], self.wire[k, 0], timeStacker);
                    if (num5 < 1f)
                    {
                        vector11 = Vector2.Lerp(Vector2.Lerp(a - vector * 14f, vector5 + vector6 * 6f, Mathf.InverseLerp(0f, (float)(self.wire.GetLength(0) - 1), (float)k)), vector11, num5);
                    }
                    if (k == self.wire.GetLength(0) - 1)
                    {
                        vector11 = self.WireAttachPos(timeStacker);
                    }
                    Vector2 normalized2 = (vector11 - vector9).normalized;
                    Vector2 b = Custom.PerpendicularVector(normalized2);
                    float d2 = Vector2.Distance(vector11, vector9) / 5f;
                    if (k == self.wire.GetLength(0) - 1)
                    {
                        d2 = 0f;
                    }
                    (sLeaser.sprites[armeTusk.TuskWireSprite()] as TriangleMesh).MoveVertice(k * 4, vector9 - b + normalized2 * d2 - camPos);
                    (sLeaser.sprites[armeTusk.TuskWireSprite()] as TriangleMesh).MoveVertice(k * 4 + 1, vector9 + b + normalized2 * d2 - camPos);
                    (sLeaser.sprites[armeTusk.TuskWireSprite()] as TriangleMesh).MoveVertice(k * 4 + 2, vector11 - b - normalized2 * d2 - camPos);
                    (sLeaser.sprites[armeTusk.TuskWireSprite()] as TriangleMesh).MoveVertice(k * 4 + 3, vector11 + b - normalized2 * d2 - camPos);
                    vector9 = vector11;
                }
                return;
            }
            sLeaser.sprites[armeTusk.TuskWireSprite()].isVisible = false;
        }

        private static void Tusk_UpdateTuskColors(On.KingTusks.Tusk.orig_UpdateTuskColors orig, KingTusks.Tusk self, RoomCamera.SpriteLeaser sLeaser)
        {
            if (self.side < 2)
            {
                orig.Invoke(self, sLeaser);
                return;
            }
            var armeTusk = self.ArmeKingTusk();

            VultureGraphics vultureGraphics = self.vulture.graphicsModule as VultureGraphics;
            for (int i = 0; i < (sLeaser.sprites[armeTusk.TuskDetailSprite()] as TriangleMesh).verticeColors.Length; i++)
            {
                float num = Mathf.InverseLerp(0f, (float)((sLeaser.sprites[armeTusk.TuskSprite()] as TriangleMesh).verticeColors.Length - 1), (float)i);
                (sLeaser.sprites[armeTusk.TuskSprite()] as TriangleMesh).verticeColors[i] = Color.Lerp(Color.Lerp(self.armorColor, Color.white, Mathf.Pow(num, 2f)), vultureGraphics.palette.blackColor, vultureGraphics.darkness);
                (sLeaser.sprites[armeTusk.TuskDetailSprite()] as TriangleMesh).verticeColors[i] = Color.Lerp(Color.Lerp(Color.Lerp(HSLColor.Lerp(vultureGraphics.ColorA, vultureGraphics.ColorB, num).rgb, vultureGraphics.palette.blackColor, 0.65f - 0.4f * num), self.armorColor, Mathf.Pow(num, 2f)), vultureGraphics.palette.blackColor, vultureGraphics.darkness);
            }
        }
        private static void Tusk_ApplyPalette(On.KingTusks.Tusk.orig_ApplyPalette orig, KingTusks.Tusk self, VultureGraphics vGraphics, RoomPalette palette, UnityEngine.Color armorColor, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (self.side < 2)
            {
                orig.Invoke(self, vGraphics, palette, armorColor, sLeaser, rCam);
                return;
            }

            var armeTusk = self.ArmeKingTusk();
            self.armorColor = armorColor;

            for (int i = 0; i < (sLeaser.sprites[armeTusk.TuskDetailSprite()] as TriangleMesh).verticeColors.Length; i++)
            {
                float num = Mathf.InverseLerp(0f, (float)((sLeaser.sprites[armeTusk.TuskSprite()] as TriangleMesh).verticeColors.Length - 1), (float)i);
                (sLeaser.sprites[armeTusk.TuskSprite()] as TriangleMesh).verticeColors[i] = Color.Lerp(armorColor, Color.white, Mathf.Pow(num, 2f));
                (sLeaser.sprites[armeTusk.TuskDetailSprite()] as TriangleMesh).verticeColors[i] = Color.Lerp(Color.Lerp(HSLColor.Lerp(vGraphics.ColorA, vGraphics.ColorB, num).rgb, palette.blackColor, 0.65f - 0.4f * num), armorColor, Mathf.Pow(num, 2f));
            }
            (sLeaser.sprites[armeTusk.TuskDetailSprite()] as TriangleMesh).alpha = self.owner.patternDisplace;
            for (int j = 0; j < (sLeaser.sprites[armeTusk.TuskWireSprite()] as TriangleMesh).verticeColors.Length; j++)
            {
                (sLeaser.sprites[armeTusk.TuskWireSprite()] as TriangleMesh).verticeColors[j] = Color.Lerp(palette.blackColor, palette.fogColor, 0.33f * Mathf.Sin(Mathf.InverseLerp(0f, (float)((sLeaser.sprites[armeTusk.TuskWireSprite()] as TriangleMesh).verticeColors.Length - 1), (float)j) * 3.1415927f));
            }

        }

        private static void Tusk_AddToContainer(On.KingTusks.Tusk.orig_AddToContainer orig, KingTusks.Tusk self, VultureGraphics vGraphics, int spr, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (self.side < 2)
            {
                orig.Invoke(self, vGraphics, spr, sLeaser, rCam, newContatiner);
                return;
            }

            var armeTusk = self.ArmeKingTusk();
            if (spr == armeTusk.LaserSprite())
            {
                rCam.ReturnFContainer(ModManager.MMF ? "Midground" : "Foreground").AddChild(sLeaser.sprites[spr]);
                return;
            }
            if (spr == armeTusk.TuskWireSprite() || spr == armeTusk.TuskSprite() || spr == armeTusk.TuskDetailSprite())
            {
                rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[spr]);
            }

        }

        private static void Tusk_InitiateSprites(On.KingTusks.Tusk.orig_InitiateSprites orig, KingTusks.Tusk self, VultureGraphics vGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (self.side < 2)
            {
                orig.Invoke(self, vGraphics, sLeaser, rCam);
                return;
            }
            var armeTusk = self.ArmeKingTusk();
            armeTusk.index = sLeaser.sprites.Length;
            Array.Resize<FSprite>(ref sLeaser.sprites, armeTusk.index + 4);
            //Array.Resize<FSprite>(ref sLeaser.sprites, armeTusk.index + 5);


            sLeaser.sprites[armeTusk.LaserSprite()] = new CustomFSprite("Futile_White");
            sLeaser.sprites[armeTusk.LaserSprite()].shader = rCam.game.rainWorld.Shaders["HologramBehindTerrain"];

            //sLeaser.sprites[armeTusk.NeckLumpSprite()] = new FSprite("Circle20", true);
            //sLeaser.sprites[armeTusk.NeckLumpSprite()].anchorY = 0f;

            sLeaser.sprites[armeTusk.TuskSprite()] = TriangleMesh.MakeLongMesh(KingTusks.Tusk.tuskSegs, true, true);

            sLeaser.sprites[armeTusk.TuskDetailSprite()] = TriangleMesh.MakeLongMesh(KingTusks.Tusk.tuskSegs, true, true);
            sLeaser.sprites[armeTusk.TuskDetailSprite()].shader = rCam.game.rainWorld.Shaders["KingTusk"];

            sLeaser.sprites[armeTusk.TuskWireSprite()] = TriangleMesh.MakeLongMesh(self.wire.GetLength(0), false, true);

        }

        private static float KingTusks_GoodShootAngle(On.KingTusks.orig_GoodShootAngle orig, KingTusks self, int tusk, bool checkMinDistance)
        {
            var num = orig.Invoke(self, tusk, checkMinDistance);//让秃鹫对自己的准度提高三倍自信(?
            return num;
        }

        private static void KingTusks_ctor(On.KingTusks.orig_ctor orig, KingTusks self, Vulture vulture)
        {
            orig.Invoke(self, vulture);
            self.tusks = new KingTusks.Tusk[20];
            for (int i = 0; i < self.tusks.Length; i++)
            {
                self.tusks[i] = new KingTusks.Tusk(self, i);
                //self.tusks[i] = new KingTusks.Tusk(self, i% 2);
            }
        }
    }
    public static class ExTusk
    {
        public static ConditionalWeakTable<KingTusks.Tusk, ExGraphicsTusk> modules = new ConditionalWeakTable<KingTusks.Tusk, ExGraphicsTusk>();
        public static ExGraphicsTusk ArmeKingTusk(this KingTusks.Tusk tusk) => modules.GetValue(tusk, (KingTusks.Tusk t) => new ExGraphicsTusk());
    }
    public class ExGraphicsTusk
    {
        public int index = 0;

        public float shootCD = 25;

        internal int LaserSprite() => index;
        internal int TuskSprite() => index + 1;
        internal int TuskDetailSprite() => index + 2;
        internal int TuskWireSprite() => index + 3;

        //internal int NeckLumpSprite() => index + 4;
    }

}