using RandomBuff.Core.Entry;
using UnityEngine;
using RWCustom;
using System.Runtime.CompilerServices;
using RandomBuff.Core.Buff;
using System.Collections.Generic;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using MoreSlugcats;
using System.Linq;
using System;
using RandomBuff.Core.Game;
using Mono.Cecil;
using System.Reflection;

namespace BuiltinBuffs.Negative
{
    internal class UpsideDown : Buff<UpsideDown, UpsideDownBuffData>
    {
        public override BuffID ID => UpsideDownBuffEntry.UpsideDownID;
    }

    internal class UpsideDownBuffData : BuffData
    {
        public override BuffID ID => UpsideDownBuffEntry.UpsideDownID;
    }

    internal class UpsideDownBuffEntry : IBuffEntry
    {
        public static bool conflicted = false;
        public static int detectCounter = 40;
        public static BuffID UpsideDownID = new BuffID("UpsideDown", true);
        public static ConditionalWeakTable<PlayerGraphics, HeadTailData> module = new ConditionalWeakTable<PlayerGraphics, HeadTailData>();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<UpsideDown, UpsideDownBuffData, UpsideDownBuffEntry>(UpsideDownID);
        }

        public static void HookOn()
        {
            IL.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites1;
            IL.PlayerGraphics.MSCUpdate += PlayerGraphics_MSCUpdate;
            IL.PlayerGraphics.Update += PlayerGraphics_Update1;
            IL.Player.TongueUpdate += Player_TongueUpdate;
            IL.Player.TerrainImpact += Player_TerrainImpact;

            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ctor += PlayerGraphics_ctor;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
            On.PlayerGraphics.Reset += PlayerGraphics_Reset;
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
        }

        static FieldInfo fieldInfo;
        private static void Player_TerrainImpact(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel label = null;
            fieldInfo = typeof(UpsideDownBuffEntry).GetField(nameof(conflicted), BindingFlags.Static | BindingFlags.Public);
            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdarg((byte)3),
                i => i.Match(OpCodes.Ldloc_0),
                i => i.MatchBleUn(out label)
                ))
            {
                c.Emit(OpCodes.Ldarg_1);
                c.Emit(OpCodes.Ldc_I4_2);
                c.Emit(OpCodes.Bge_S, label.Target);
                c.Emit(OpCodes.Ldsfld, fieldInfo);
                c.Emit(OpCodes.Brtrue, label.Target);
            }

            ILLabel label2 = null;
            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdarg((byte)3),
                i => i.Match(OpCodes.Ldloc_1),
                i => i.MatchBleUn(out label2)
                ))
            {
                c.Emit(OpCodes.Ldarg_1);
                c.Emit(OpCodes.Ldc_I4_2);
                c.Emit(OpCodes.Bge_S, label2.Target);
                c.Emit(OpCodes.Ldsfld, fieldInfo);
                c.Emit(OpCodes.Brtrue, label2.Target);
            }
        }

        private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (module.TryGetValue(self, out var _module))
            {
                if (_module.headTailSprite > 0 && _module.headTailSprite < sLeaser.sprites.Length)
                {
                    if (newContatiner == null)
                        rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[_module.headTailSprite]);
                    else
                        newContatiner.AddChild(sLeaser.sprites[_module.headTailSprite]);
                }
            }
        }

        private static void PlayerGraphics_Update1(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                i => i.Match(OpCodes.Ldc_I4_1),
                i => i.Match(OpCodes.Ldc_I4_0),
                i => i.Match(OpCodes.Call),
                i => i.Match(OpCodes.Newobj),
                i => i.Match(OpCodes.Stfld)
                ))
            {
                //UnityEngine.Debug.Log("Tongue IL Hook 3");
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<PlayerGraphics>>(delegate (PlayerGraphics self)
                {
                    Vector2 vector = self.owner.firstChunk.pos + 30f * (self.owner.firstChunk.pos - self.owner.bodyChunks[1].pos).normalized;
                    self.tail[0].connectedPoint = vector;
                });
            }
        }

        private static void PlayerGraphics_MSCUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                i => i.Match(OpCodes.Callvirt),
                i => i.Match(OpCodes.Ldfld),
                i => i.Match(OpCodes.Callvirt)
                ))
            {
                //UnityEngine.Debug.Log("Tongue IL Hook 2");
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_0);
                c.EmitDelegate<Action<PlayerGraphics, List<Vector2>>>(delegate (PlayerGraphics self, List<Vector2> list)
                {
                    int num = list.Count;
                    list.RemoveAt(num - 1);
                    list.Add(self.owner.bodyChunks.Last().pos);
                });

            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            detectCounter--;
            if (!conflicted && detectCounter <= 0)
            {
                detectCounter = 40;
                var poolInstance = BuffPoolManager.Instance.GetTemporaryBuffPool(UpsideDownID);
                for (int i = 0; i < poolInstance.allBuffIDs.Count; i++)
                {
                    if (poolInstance.allBuffIDs[i].value == "SpringSlugID")
                    {
                        conflicted = true;
                        break;
                    }
                }
            }

            if (self.graphicsModule != null && module.TryGetValue(self.graphicsModule as PlayerGraphics, out var _module))
            {
                if (self.tongue != null)
                {
                    if (self.tongue.mode == Player.Tongue.Mode.AttachedToObject || self.tongue.mode == Player.Tongue.Mode.AttachedToTerrain)
                    {
                        self.bodyChunkConnections.Last().weightSymmetry = 0.8f;
                        //self.bodyChunkConnections.Last().type = PhysicalObject.BodyChunkConnection.Type.Normal;
                    }
                    else
                    {
                        self.bodyChunkConnections.Last().weightSymmetry = 0.05f;
                        self.bodyChunkConnections.Last().type = PhysicalObject.BodyChunkConnection.Type.Pull;
                    }
                }

                if (self.room != null && self.room.game != null && self.bodyChunks.Length > 2 && _module.headTails.Count > 0)
                {
                    Vector2 mousePos = (Vector2)Futile.mousePosition + self.room.game.cameras[0].pos;
                    bool flag = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(self.room, self.bodyChunks[1].pos, _module.headTails.Last().pos) != null;
                    bool flag2 = Custom.DistLess(self.bodyChunks.Last().pos, self.bodyChunks[1].pos, 100f);
                    bool flag3 = self.tongue != null && self.tongue.mode.value.Contains("Attach");
                    bool flag4 = !self.Consious && self.grabbedBy.Count > 0;

                    if (flag4) return;

                    for (int i = 0; i < _module.headTails.Count; i++)
                    {
                        if (self.room != null && self.room.game != null && self.room == self.room.game.cameras[0].room && self.room.game.devToolsActive && Input.GetKey(KeyCode.V))
                        {                           
                            _module.headTails[i].Reset(mousePos);
                        }

                        if (!flag2 || flag)
                        {
                            if (!flag3)
                                _module.headTails[i].Reset(self.bodyChunks[1].pos);
                            else
                                self.bodyChunks.Last().HardSetPosition(self.bodyChunks[1].pos);
                        }
                    }

                }
            }
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            int num = self.bodyChunks.Length;
            BodyChunk[] newChunks = new BodyChunk[num + 1];
            for (int i = 0; i < num; i++)
            {
                newChunks[i] = self.bodyChunks[i];
            }
            newChunks[num] = new BodyChunk(self, num, new Vector2(0f, 0f), 8f, 0.5f);
            self.bodyChunks = newChunks;

            int num2 = self.bodyChunkConnections.Length;
            Array.Resize<PhysicalObject.BodyChunkConnection>(ref self.bodyChunkConnections, num2 + 1);
            self.bodyChunkConnections[num2] = new PhysicalObject.BodyChunkConnection(self.bodyChunks[1], self.bodyChunks[num], 32f, PhysicalObject.BodyChunkConnection.Type.Pull, 1f, 0.2f);
        }

        private static void Player_TongueUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                i => i.Match(OpCodes.Ldarg_0),
                i => i.Match(OpCodes.Ldfld),
                i => i.Match(OpCodes.Ldarg_0),
                i => i.Match(OpCodes.Call),
                i => i.Match(OpCodes.Stfld)
                ))
            {
                //UnityEngine.Debug.Log("Tongue IL Hook");
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<Player>>(delegate (Player self)
                {
                    self.tongue.baseChunk = self.bodyChunks.Last();

                });
            }
        }

        private static void PlayerGraphics_Reset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig(self);
            if (module.TryGetValue(self, out var _module))
            {
                if (_module.headTailSprite <= 0) return;
                for (int i = 0; i < 4; i++)
                {
                    _module.headTails[i].Reset(self.owner.bodyChunks[1].pos);
                }
            }
            self.tail[0].pos = self.owner.firstChunk.pos;
            if (self.ropeSegments != null)
            {
                for (int i = 0; i < self.ropeSegments.Length; i++)
                {
                    self.ropeSegments[i].pos = self.owner.bodyChunks.Last().pos;
                    self.ropeSegments[i].vel = self.owner.bodyChunks.Last().vel;
                }
            }

        }

        private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if (!module.TryGetValue(self, out var _module))
            {
                module.Add(self, new HeadTailData(self));
            }
            if (self.ropeSegments != null)
            {
                int num = self.ropeSegments.Length;
                for (int i = 0; i < num; i++)
                {
                    self.ropeSegments[i].pos = self.owner.bodyChunks.Last().pos;
                }
            }
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            try
            {
                if (module.TryGetValue(self, out var _module))
                {

                    if (_module.headTails.Count > 2)
                    {
                        _module.headTails[0].connectedPoint = self.legs.pos;
                        for (int i = 0; i < 4; i++)
                        {
                            _module.headTails[i].vel -= new Vector2(0, self.owner.gravity);
                            _module.headTails[i].Update();
                            _module.headTails[i].vel *= 0.9f;
                        }
                    }

                    if (self.ropeSegments != null)
                    {
                        if (self.player.tongue.mode.value.Contains("Attach"))
                        {
                            _module.headTails.Last().pos = self.owner.bodyChunks.Last().pos;
                            _module.headTails[_module.headTails.Count - 2].pos = self.owner.bodyChunks.Last().pos;
                        }
                        else
                        {
                            self.owner.bodyChunks.Last().pos = _module.headTails.Last().pos;
                        }

                    }
                    else
                        self.owner.bodyChunks.Last().pos = _module.headTails.Last().pos;

                }

                self.tail[0].pos += (self.head.pos - self.tail[0].pos);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }

        }

        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            float num;
            float num2;
            if (module.TryGetValue(self, out var _module))
            {
                if (ModManager.MSC && self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Slugpup)
                {
                    num = 0.85f + 0.3f * Mathf.Lerp(self.player.npcStats.Wideness, 0.5f, self.player.playerState.isPup ? 0.5f : 0f);
                    num2 = (0.75f + 0.5f * self.player.npcStats.Size) * (self.player.playerState.isPup ? 0.5f : 1f);
                }
                else
                {
                    num = 1;
                    num2 = 1;
                }

                if (_module.headTails.Count == 0)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        _module.headTails.Add(new TailSegment(self, 6f * num, 4f * num2, i == 0 ? null : _module.headTails[i - 1], 0.85f, 1f, 0.5f, true));
                    }
                }


                int num3 = self.bodyParts.Length;
                Array.Resize<BodyPart>(ref self.bodyParts, num3 + 4);
                for (int j = 0; j < 4; j++)
                {
                    self.bodyParts[num3 + j] = _module.headTails[j];
                }


                TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
                {
                    new TriangleMesh.Triangle(0, 1, 2),
                    new TriangleMesh.Triangle(1, 2, 3),
                    new TriangleMesh.Triangle(4, 5, 6),
                    new TriangleMesh.Triangle(5, 6, 7),
                    new TriangleMesh.Triangle(8, 9, 10),
                    new TriangleMesh.Triangle(9, 10, 11),
                    new TriangleMesh.Triangle(12, 13, 14),
                    new TriangleMesh.Triangle(2, 3, 4),
                    new TriangleMesh.Triangle(3, 4, 5),
                    new TriangleMesh.Triangle(6, 7, 8),
                    new TriangleMesh.Triangle(7, 8, 9),
                    new TriangleMesh.Triangle(10, 11, 12),
                    new TriangleMesh.Triangle(11, 12, 13)
                };
                TriangleMesh triangleMesh = new TriangleMesh("Futile_White", tris, false, false);
                _module.headTailSprite = sLeaser.sprites.Length;
                Array.Resize<FSprite>(ref sLeaser.sprites, _module.headTailSprite + 1);
                sLeaser.sprites[_module.headTailSprite] = triangleMesh;
                self.AddToContainer(sLeaser, rCam, null);
            }
        }


        private static void PlayerGraphics_DrawSprites1(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILCursor c2 = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                (i) => i.Match(OpCodes.Ldfld),
                (i) => i.Match(OpCodes.Ldfld),
                (i) => i.Match(OpCodes.Ldarg_3),
                (i) => i.Match(OpCodes.Call),
                (i) => i.Match(OpCodes.Stloc_3)
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_3);
                c.Emit(OpCodes.Ldarg_3);
                c.EmitDelegate<Func<PlayerGraphics, Vector2, float, Vector2>>(delegate (PlayerGraphics self, Vector2 vector3, float timeStacker)
                {
                    if (module.TryGetValue(self, out var _module) && _module.headTails.Count > 0)
                    {
                        int num = _module.headTails.Count - 1;
                        vector3 = Vector2.Lerp(_module.headTails[num].lastPos, _module.headTails[num].pos, timeStacker);
                        return vector3;
                    }
                    return self.legs.pos;
                }
                );
                c.Emit(OpCodes.Stloc_3);
            }

            if (c2.TryGotoNext(MoveType.After,
                i => i.Match(OpCodes.Ldc_R4),
                i => i.Match(OpCodes.Call),
                i => i.Match(OpCodes.Stloc_S)
                ))
            {
                c.Emit(OpCodes.Ldloc_2);
                c.Emit(OpCodes.Ldloc_1);
                c.Emit(OpCodes.Ldloc, 5);
                c.EmitDelegate<Func<Vector2, Vector2, Vector2, Vector2>>(delegate (Vector2 vector2, Vector2 vector, Vector2 vector4)
                {
                    vector4 = (30f * vector + vector2) / 4f;
                    return vector4;
                });
                c.Emit(OpCodes.Stloc, 5);
            }
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            float d = 1f - 0.2f * self.malnourished;
            float d2 = 6f;
            Vector2 vector = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker);
            Vector2 vector2 = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
            if (module.TryGetValue(self, out var _module))
            {
                if (_module.headTailSprite <= 0) { return; }
                Vector2 vector4 = (vector + vector2 * 3f) / 4f;

                for (int i = 0; i < 4; i++)
                {
                    Vector2 vector5 = Vector2.Lerp(_module.headTails[i].lastPos, _module.headTails[i].pos, timeStacker);
                    Vector2 normalized = (vector5 - vector4).normalized;
                    Vector2 a = Custom.PerpendicularVector(normalized);
                    float d3 = Vector2.Distance(vector5, vector4) / 5f;
                    if (i == 0)
                    {
                        d3 = 0f;
                    }
                    (sLeaser.sprites[_module.headTailSprite] as TriangleMesh).MoveVertice(i * 4, vector4 - a * d2 * d + normalized * d3 - camPos);
                    (sLeaser.sprites[_module.headTailSprite] as TriangleMesh).MoveVertice(i * 4 + 1, vector4 + a * d2 * d + normalized * d3 - camPos);
                    if (i < 3)
                    {
                        (sLeaser.sprites[_module.headTailSprite] as TriangleMesh).MoveVertice(i * 4 + 2, vector5 - a * _module.headTails[i].StretchedRad * d - normalized * d3 - camPos);
                        (sLeaser.sprites[_module.headTailSprite] as TriangleMesh).MoveVertice(i * 4 + 3, vector5 + a * _module.headTails[i].StretchedRad * d - normalized * d3 - camPos);
                    }
                    else
                    {
                        (sLeaser.sprites[_module.headTailSprite] as TriangleMesh).MoveVertice(i * 4 + 2, vector5 - camPos);
                    }
                    d2 = _module.headTails[i].StretchedRad;
                    vector4 = vector5;
                }
            }

        }

        public class HeadTailData
        {
            public List<TailSegment> headTails;
            public int headTailSprite;
            public HeadTailData(PlayerGraphics playerGraphics)
            {
                headTails = new List<TailSegment>();
            }
        }
    }
}
