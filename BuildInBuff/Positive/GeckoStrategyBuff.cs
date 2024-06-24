using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Runtime.CompilerServices;
using MonoMod;

namespace BuiltinBuffs.Positive
{
    internal class GeckoStrategyBuff : Buff<GeckoStrategyBuff, GeckoStrategyBuffData>
    {
        public static bool collisionChecked;
        public static bool discoveredCollision;

        public GeckoStrategyBuff()
        {
            collisionChecked = false;
            discoveredCollision = false;
        }

        public override BuffID ID => GeckoStrategyBuffEntry.GeckoStrategy;
    }

    class GeckoStrategyBuffData : CountableBuffData
    {
        public override int MaxCycleCount => 5;
        public override BuffID ID => GeckoStrategyBuffEntry.GeckoStrategy;
    }

    class GeckoStrategyBuffEntry : IBuffEntry
    {
        public static BuffID GeckoStrategy = new BuffID("GeckoStrategy", true);
        public static ConditionalWeakTable<Player, GeckoModule> geckoModule = new ConditionalWeakTable<Player, GeckoModule>();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<GeckoStrategyBuff, GeckoStrategyBuffData, GeckoStrategyBuffEntry>(GeckoStrategy);
        }

        public static void HookOn()
        {
            On.AbstractPhysicalObject.Realize += AbstractPhysicalObject_Realize;
            On.Creature.Update += Creature_Update;
            On.Lizard.CarryObject += Lizard_CarryObject;
            On.LizardAI.DetermineBehavior += LizardAI_DetermineBehavior;
            On.Vulture.Carry += Vulture_Carry;
            IL.VultureAI.Update += VultureAI_Update;
            On.DropBug.CarryObject += DropBug_CarryObject;
            IL.DropBugAI.Update += DropBugAI_Update;
            On.BigSpider.CarryObject += BigSpider_CarryObject;
            IL.BigSpiderAI.Update += BigSpiderAI_Update;
            On.Player.Update += Player_Update;
            On.Player.Die += Player_Die;
            On.TailSegment.Update += TailSegment_Update;
            On.PlayerGraphics.TailSpeckles.DrawSprites += TailSpeckles_DrawSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        }

        private static void Player_Die(On.Player.orig_Die orig, Player self)
        {
            if (geckoModule.TryGetValue(self, out var module) && module.escapeCount >= 0 && !self.playerState.permaDead)
            {
                return;
            }
            orig(self);           
        }


        private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
        {
            orig(self, eu);
            try
            {
                if (!(self is Player) && self.grasps != null && self.grasps[0] != null && self.grasps[0].grabbed is GeckoTail)
                {
                    if (self.room != null && self.room.world != null && self.room.world.GetNode(self.abstractCreature.pos).type == AbstractRoomNode.Type.Den)
                    {
                        var tail = self.grasps[0].grabbed;
                        tail.Destroy();
                        self.LoseAllGrasps();
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(ex);
            }            
        }

        private static void VultureAI_Update(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld(typeof(VultureAI.Behavior).GetField(nameof(VultureAI.Behavior.Disencouraged))),
                i => i.MatchStfld(typeof(VultureAI).GetField(nameof(VultureAI.behavior))),
                i => i.Match(OpCodes.Ldarg_0)))
            {               
                c.EmitDelegate<Action<VultureAI>>(delegate (VultureAI self)
                {
                    if (self.vulture.grasps[0] != null && self.vulture.grasps[0].grabbed is GeckoTail)
                    {
                        self.behavior = VultureAI.Behavior.ReturnPrey;
                    }
                });
                c.Emit(OpCodes.Ldarg_0);
            }
        }

        private static void Vulture_Carry(On.Vulture.orig_Carry orig, Vulture self)
        {
            try
            {                
                if (self.grasps[0].grabbedChunk.owner is GeckoTail)
                {
                    if (!self.Consious)
                    {
                        self.LoseAllGrasps();
                        return;
                    }
                    self.AI.behavior = VultureAI.Behavior.ReturnPrey;

                    BodyChunk grabbedChunk = self.grasps[0].grabbedChunk;
                    
                    float num2 = grabbedChunk.mass / (grabbedChunk.mass + self.bodyChunks[4].mass);
                    float num3 = grabbedChunk.mass / (grabbedChunk.mass + self.bodyChunks[0].mass);
                    if (self.neck.backtrackFrom != -1 || self.enteringShortCut != null)
                    {
                        num2 = 0f;
                        num3 = 0f;
                    }
                    if (!Custom.DistLess(grabbedChunk.pos, self.neck.tChunks[self.neck.tChunks.Length - 1].pos, 20f))
                    {
                        Vector2 a = Custom.DirVec(grabbedChunk.pos, self.neck.tChunks[self.neck.tChunks.Length - 1].pos);
                        float num4 = Vector2.Distance(grabbedChunk.pos, self.neck.tChunks[self.neck.tChunks.Length - 1].pos);
                        grabbedChunk.pos -= (20f - num4) * a * (1f - num2);
                        grabbedChunk.vel -= (20f - num4) * a * (1f - num2);
                        self.neck.tChunks[self.neck.tChunks.Length - 1].pos += (20f - num4) * a * num2;
                        self.neck.tChunks[self.neck.tChunks.Length - 1].vel += (20f - num4) * a * num2;
                    }
                    if (self.enteringShortCut == null)
                    {
                        self.bodyChunks[4].pos = Vector2.Lerp(self.neck.tChunks[self.neck.tChunks.Length - 1].pos, grabbedChunk.pos, 0.1f);
                        self.bodyChunks[4].vel = self.neck.tChunks[self.neck.tChunks.Length - 1].vel;
                    }
                    float num5 = 70f;
                    if (!Custom.DistLess(self.mainBodyChunk.pos, grabbedChunk.pos, num5))
                    {
                        Vector2 a2 = Custom.DirVec(grabbedChunk.pos, self.bodyChunks[0].pos);
                        float num6 = Vector2.Distance(grabbedChunk.pos, self.bodyChunks[0].pos);
                        grabbedChunk.pos -= (num5 - num6) * a2 * (1f - num3);
                        grabbedChunk.vel -= (num5 - num6) * a2 * (1f - num3);
                        self.bodyChunks[0].pos += (num5 - num6) * a2 * num3;
                        self.bodyChunks[0].vel += (num5 - num6) * a2 * num3;
                    }
                    return;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
            orig(self);
        }

        private static LizardAI.Behavior LizardAI_DetermineBehavior(On.LizardAI.orig_DetermineBehavior orig, LizardAI self)
        {
            if (self.lizard.grasps[0] != null && self.lizard.grasps[0].grabbed is GeckoTail)
            {
                return LizardAI.Behavior.ReturnPrey;
            }
            return orig(self);
        }

        private static void Lizard_CarryObject(On.Lizard.orig_CarryObject orig, Lizard self, bool eu)
        {
            if (self.grasps[0].grabbed is GeckoTail)
            {
                Vector2 vector = self.mainBodyChunk.pos + Custom.DirVec(self.bodyChunks[1].pos, self.mainBodyChunk.pos) * 25f * self.lizardParams.headSize;
                PhysicalObject grabbed = self.grasps[0].grabbed;
                Vector2 vector2 = grabbed.bodyChunks[self.grasps[0].chunkGrabbed].vel - self.mainBodyChunk.vel;
                
                grabbed.bodyChunks[self.grasps[0].chunkGrabbed].vel = self.mainBodyChunk.vel;
                if (self.enteringShortCut == null && (vector2.magnitude * grabbed.bodyChunks[self.grasps[0].chunkGrabbed].mass > 30f || !Custom.DistLess(vector, grabbed.bodyChunks[self.grasps[0].chunkGrabbed].pos, 70f + grabbed.bodyChunks[self.grasps[0].chunkGrabbed].rad)))
                {
                    self.LoseAllGrasps();
                }
                else
                {
                    grabbed.bodyChunks[self.grasps[0].chunkGrabbed].MoveFromOutsideMyUpdate(eu, vector);
                }

                if (self.grasps[0] != null)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        self.grasps[0].grabbed.PushOutOf(self.bodyChunks[i].pos, self.bodyChunks[i].rad, self.grasps[0].chunkGrabbed);
                    }
                }

                self.AI.behavior = LizardAI.Behavior.ReturnPrey;
                return;
            }
            orig(self, eu);
        }

        private static void BigSpiderAI_Update(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld(typeof(BigSpiderAI.Behavior).GetField(nameof(BigSpiderAI.Behavior.GetUnstuck))),
                i => i.MatchStfld(typeof(BigSpiderAI).GetField(nameof(BigSpiderAI.behavior))),
                i => i.Match(OpCodes.Ldarg_0)))
            {
                c.EmitDelegate<Action<BigSpiderAI>>(delegate (BigSpiderAI self)
                {
                    if (self.behavior != BigSpiderAI.Behavior.Flee && self.bug.grasps[0] != null && self.bug.grasps[0].grabbed is GeckoTail)
                    {
                        self.behavior = BigSpiderAI.Behavior.ReturnPrey;
                        self.currentUtility = 1f;
                    }
                });
                c.Emit(OpCodes.Ldarg_0);
            }
        }


        private static void BigSpider_CarryObject(On.BigSpider.orig_CarryObject orig, BigSpider self, bool eu)
        {
            if (self.grasps[0] != null && self.grasps[0].grabbed is GeckoTail)
            {
                PhysicalObject grabbed = self.grasps[0].grabbed;
                self.carryObjectMass = grabbed.bodyChunks[self.grasps[0].chunkGrabbed].owner.TotalMass;
                if (self.carryObjectMass <= self.TotalMass * 1.1f)
                {
                    self.carryObjectMass /= 2f;
                }
                else if (self.carryObjectMass <= self.TotalMass / 5f)
                {
                    self.carryObjectMass = 0f;
                }
                float num = self.mainBodyChunk.rad + self.grasps[0].grabbed.bodyChunks[self.grasps[0].chunkGrabbed].rad;
                Vector2 a = -Custom.DirVec(self.mainBodyChunk.pos, grabbed.bodyChunks[self.grasps[0].chunkGrabbed].pos) * (num - Vector2.Distance(self.mainBodyChunk.pos, grabbed.bodyChunks[self.grasps[0].chunkGrabbed].pos));
                float num2 = grabbed.bodyChunks[self.grasps[0].chunkGrabbed].mass / (self.mainBodyChunk.mass + grabbed.bodyChunks[self.grasps[0].chunkGrabbed].mass);
                num2 *= 0.2f * (1f - self.AI.stuckTracker.Utility());
                self.mainBodyChunk.pos += a * num2;
                self.mainBodyChunk.vel += a * num2;
                grabbed.bodyChunks[self.grasps[0].chunkGrabbed].pos -= a * (1f - num2);
                grabbed.bodyChunks[self.grasps[0].chunkGrabbed].vel -= a * (1f - num2);
                Vector2 vector = self.mainBodyChunk.pos + Custom.DirVec(self.bodyChunks[1].pos, self.mainBodyChunk.pos) * num;
                Vector2 vector2 = grabbed.bodyChunks[self.grasps[0].chunkGrabbed].vel - self.mainBodyChunk.vel;
                grabbed.bodyChunks[self.grasps[0].chunkGrabbed].vel = self.mainBodyChunk.vel;
                if (self.enteringShortCut == null && (vector2.magnitude * grabbed.bodyChunks[self.grasps[0].chunkGrabbed].mass > 30f || !Custom.DistLess(vector, grabbed.bodyChunks[self.grasps[0].chunkGrabbed].pos, 70f + grabbed.bodyChunks[self.grasps[0].chunkGrabbed].rad)))
                {
                    self.LoseAllGrasps();
                }
                else
                {
                    grabbed.bodyChunks[self.grasps[0].chunkGrabbed].MoveFromOutsideMyUpdate(eu, vector);
                }
                if (self.grasps[0] != null)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        self.grasps[0].grabbed.PushOutOf(self.bodyChunks[i].pos, self.bodyChunks[i].rad, self.grasps[0].chunkGrabbed);
                    }
                }
                return;
            }
            orig(self, eu);
        }

        private static void DropBugAI_Update(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld(typeof(DropBugAI.Behavior).GetField(nameof(DropBugAI.Behavior.ReturnPrey))),
                i => i.MatchStfld(typeof(DropBugAI).GetField(nameof(DropBugAI.behavior))),
                i => i.Match(OpCodes.Ldarg_0),
                i => i.Match(OpCodes.Ldc_R4),
                i => i.Match(OpCodes.Stfld),
                i => i.Match(OpCodes.Ldarg_0)
                ))
            {
                c.EmitDelegate<Action<DropBugAI>>(delegate (DropBugAI self)
                {
                    if (self.behavior != DropBugAI.Behavior.Flee && self.bug.grasps[0] != null && self.bug.grasps[0].grabbed is GeckoTail)
                    {
                        self.behavior = DropBugAI.Behavior.ReturnPrey;
                        self.currentUtility = 1f;
                    }
                });
                c.Emit(OpCodes.Ldarg_0);
            }
        }

        private static void DropBug_CarryObject(On.DropBug.orig_CarryObject orig, DropBug self, bool eu)
        {
            if (self.grasps[0] != null && self.grasps[0].grabbed is GeckoTail)
            {
                PhysicalObject grabbed = self.grasps[0].grabbed;
                self.carryObjectMass = grabbed.bodyChunks[self.grasps[0].chunkGrabbed].owner.TotalMass;
                if (self.carryObjectMass <= self.TotalMass * 1.1f)
                {
                    self.carryObjectMass /= 2f;
                }
                else if (self.carryObjectMass <= self.TotalMass / 5f)
                {
                    self.carryObjectMass = 0f;
                }
                float num = self.mainBodyChunk.rad + self.grasps[0].grabbed.bodyChunks[self.grasps[0].chunkGrabbed].rad;
                Vector2 a = -Custom.DirVec(self.mainBodyChunk.pos, grabbed.bodyChunks[self.grasps[0].chunkGrabbed].pos) * (num - Vector2.Distance(self.mainBodyChunk.pos, grabbed.bodyChunks[self.grasps[0].chunkGrabbed].pos));
                float num2 = grabbed.bodyChunks[self.grasps[0].chunkGrabbed].mass / (self.mainBodyChunk.mass + grabbed.bodyChunks[self.grasps[0].chunkGrabbed].mass);
                num2 *= 0.2f * (1f - self.AI.stuckTracker.Utility());
                self.mainBodyChunk.pos += a * num2;
                self.mainBodyChunk.vel += a * num2;
                grabbed.bodyChunks[self.grasps[0].chunkGrabbed].pos -= a * (1f - num2);
                grabbed.bodyChunks[self.grasps[0].chunkGrabbed].vel -= a * (1f - num2);
                Vector2 vector = self.mainBodyChunk.pos + Custom.DirVec(self.bodyChunks[1].pos, self.mainBodyChunk.pos) * num;
                Vector2 vector2 = grabbed.bodyChunks[self.grasps[0].chunkGrabbed].vel - self.mainBodyChunk.vel;
                grabbed.bodyChunks[self.grasps[0].chunkGrabbed].vel = self.mainBodyChunk.vel;
                if (self.enteringShortCut == null && (vector2.magnitude * grabbed.bodyChunks[self.grasps[0].chunkGrabbed].mass > 30f || !Custom.DistLess(vector, grabbed.bodyChunks[self.grasps[0].chunkGrabbed].pos, 70f + grabbed.bodyChunks[self.grasps[0].chunkGrabbed].rad)))
                {
                    self.LoseAllGrasps();
                }
                else
                {
                    grabbed.bodyChunks[self.grasps[0].chunkGrabbed].MoveFromOutsideMyUpdate(eu, vector);
                }
                if (self.grasps[0] != null)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        self.grasps[0].grabbed.PushOutOf(self.bodyChunks[i].pos, self.bodyChunks[i].rad, self.grasps[0].chunkGrabbed);
                    }
                }
                return;
            }
            orig(self, eu);
        }

        private static void AbstractPhysicalObject_Realize(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            orig(self);
            if (self.type == GeckoTail.SlugTail)
            {
                self.realizedObject = new GeckoTail(self);
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            try
            {
                if (!geckoModule.TryGetValue(self, out var _module))
                {
                    if (!GeckoStrategyBuff.collisionChecked)
                    {
                        var tempPool = BuffPoolManager.Instance.GetTemporaryBuffPool(GeckoStrategy);
                        //闪卡改了的部分

                        //for (int i = 0; i < tempPool.allBuffIDs.Count; i++)
                        //{
                        //    if (tempPool.allBuffIDs[i].value == "CatNeuroID" || tempPool.allBuffIDs[i].value == "TailcopterID")
                        //    {
                        //        GeckoStrategyBuff.discoveredCollision = true;
                        //        break;
                        //    }
                        //}

                        //if(!self.GetExPlayerData().HaveTail)GeckoStrategyBuff.discoveredCollision = true;

                        GeckoStrategyBuff.collisionChecked = true;
                    }
                    
                    if(!GeckoStrategyBuff.discoveredCollision)
                        geckoModule.Add(self, new GeckoModule());
                }

                if (self.room != null && self.grabbedBy.Count > 0)
                {
                    if (geckoModule.TryGetValue(self, out var module))
                    {                        
                        if (self.GetExPlayerData().HaveTail)
                        {
                            for (int i = 0; i < self.grabbedBy.Count; i++)
                            {
                                if (self.grabbedBy[i].grabber is Player) continue;
                                
                                int num = self.grabbedBy[i].graspUsed;
                                var grabber = self.grabbedBy[i].grabber;
                                self.grabbedBy[i].grabber.ReleaseGrasp(num);

                                var tail = new AbstractPhysicalObject(self.room.world, GeckoTail.SlugTail, null, new WorldCoordinate(self.room.abstractRoom.index, -1, -1, 0), self.room.game.GetNewID());
                                tail.RealizeInRoom();
                                tail.realizedObject.firstChunk.pos = self.firstChunk.pos + 5f * Custom.RNV();
                                tail.realizedObject.bodyChunks[1].pos = self.firstChunk.pos;
                                (tail.realizedObject as GeckoTail).Reset();
                                if (self.graphicsModule != null && (self.graphicsModule as PlayerGraphics).tail.Length > 0)
                                {
                                    List<Vector2> tailStat = new List<Vector2>();
                                    for (int j = 0; j < (self.graphicsModule as PlayerGraphics).tail.Length; j++)
                                    {
                                        tailStat.Add(new Vector2((self.graphicsModule as PlayerGraphics).tail[i].rad, (self.graphicsModule as PlayerGraphics).tail[i].connectionRad));
                                    }
                                    (tail.realizedObject as GeckoTail).RefreshSegments(tailStat.ToArray());
                                }
                                (tail.realizedObject as GeckoTail).tailColor = self.ShortCutColor();
                                grabber.Grab(tail.realizedObject, num, 0, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 1f, false, false);
                                //module.tailCut = true;
                                self.GetExPlayerData().HaveTail = false;
                                break;
                            }
                        }
                        else
                        {
                            if (module.escapeCount > -1) module.escapeCount--;

                        }
                    }                   
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }

        }

        private static void TailSegment_Update(On.TailSegment.orig_Update orig, TailSegment self)
        {
            if (self.owner is PlayerGraphics && geckoModule.TryGetValue((self.owner as PlayerGraphics).player, out var geckoData) && geckoData.tailCut)
            {
                self.Reset(self.owner.owner.bodyChunks[1].pos);
                return;
            }
            orig(self);
        }

        private static void TailSpeckles_DrawSprites(On.PlayerGraphics.TailSpeckles.orig_DrawSprites orig, PlayerGraphics.TailSpeckles self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (geckoModule.TryGetValue(self.pGraphics.player, out var geckoData) && geckoData.tailCut)
            {
                sLeaser.sprites[self.startSprite + self.rows * self.lines].rotation = Custom.VecToDeg(Custom.PerpendicularVector(self.pGraphics.drawPositions[0, 0] - self.pGraphics.drawPositions[1, 0]));
                Vector2 pos = Vector2.Lerp(self.pGraphics.drawPositions[1, 1], self.pGraphics.drawPositions[1, 0], timeStacker);
                for (int i = 0; i < self.rows; i++)
                {
                    for (int j = 0; j < self.lines; j++)
                    {
                        sLeaser.sprites[self.startSprite + i * self.lines + j].isVisible = false;

                    }
                }
                Vector2 pos2 = Vector2.Lerp(self.pGraphics.drawPositions[1, 1], self.pGraphics.drawPositions[1, 0], timeStacker);
                sLeaser.sprites[self.startSprite + self.rows * self.lines].SetPosition(Vector2.Lerp(pos2, pos, 0.2f) - camPos);
            }
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (geckoModule.TryGetValue(self.player, out var geckoData) && geckoData.tailCut)
            {
                Vector2 pos = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
                for (int i = 0; i < (sLeaser.sprites[2] as TriangleMesh).vertices.Length; i++)
                {
                    (sLeaser.sprites[2] as TriangleMesh).MoveVertice(i, pos - camPos);
                }
            }
        }

    }

    public class GeckoTail : PhysicalObject
    {
        public static readonly AbstractPhysicalObject.AbstractObjectType SlugTail = new AbstractPhysicalObject.AbstractObjectType("GeckoTail", true);

        public string spriteName;
        public Color tailColor;
        public List<TailSegment> segments;

        public GeckoTail(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
        {
            segments = new List<TailSegment>();
            base.bodyChunks = new BodyChunk[2];
            base.bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0f, 0f), 6f, 0.2f);
            base.bodyChunks[1] = new BodyChunk(this, 1, new Vector2(0f, 0f), 0f, 0.01f);
            this.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[0];
            base.airFriction = 0.999f;
            base.gravity = 0.9f;
            this.bounce = 0.4f;
            this.surfaceFriction = 0.5f;
            this.collisionLayer = 1;
            base.waterFriction = 0.95f;
            base.buoyancy = 0.8f;

        }

        public override void InitiateGraphicsModule()
        {
            if (graphicsModule == null)
            {
                base.graphicsModule = new TailGrafModule(this);
                segments.Add(new TailSegment(graphicsModule, 6f, 4f, null, 0.85f, 1f, 1f, true));
                segments.Add(new TailSegment(graphicsModule, 4f, 7f, segments[0], 0.85f, 1f, 0.5f, true));
                segments.Add(new TailSegment(graphicsModule, 2.5f, 7f, segments[1], 0.85f, 1f, 0.5f, true));
                segments.Add(new TailSegment(graphicsModule, 1f, 7f, segments[2], 0.85f, 1f, 0.5f, true));
                Reset();
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            bodyChunks[1].pos = firstChunk.pos;

            segments[0].connectedPoint = firstChunk.pos;
            for (int i = 0; i < segments.Count; i++)
            {
                segments[i].vel -= 0.9f * gravity * Vector2.up;
                segments[i].Update();
                segments[i].vel *= 0.9f;
            }
        }

        public override void NewRoom(Room newRoom)
        {
            base.NewRoom(newRoom);
            bodyChunks[1].pos = firstChunk.pos;
            if (segments.Count > 0)
                Reset();
        }

        public void Reset()
        {
            for (int i = 0; i < segments.Count; i++)
            {
                segments[i].pos = firstChunk.pos;
                segments[i].lastPos = segments[i].pos;
            }
        }

        public void RefreshSegments(Vector2[] newStats)
        {
            for (int i = 0; i < newStats.Length; i++)
            {
                if (i >= segments.Count) break;
                segments[i].rad = newStats[i].x;
                segments[i].connectionRad = newStats[i].y;
            }
        }

        public class TailGrafModule : GraphicsModule
        {            
            public TailGrafModule(GeckoTail ow) : base(ow, false)
            {
                owner = ow;
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[1];
                sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(4, true, false);
                AddToContainer(sLeaser, rCam, null);
            }

            public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                sLeaser.sprites[0].RemoveFromContainer();
                if (newContatiner == null)
                {
                    rCam.ReturnFContainer("Background").AddChild(sLeaser.sprites[0]);
                }
                else
                {
                    newContatiner.AddChild(sLeaser.sprites[0]);
                }
            }

            public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                base.ApplyPalette(sLeaser, rCam, palette);
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                if ((owner as GeckoTail).segments.Count <= 0) return;

                if ((owner as GeckoTail).tailColor != null) sLeaser.sprites[0].color = (owner as GeckoTail).tailColor;

                float d2 = 6f;
                Vector2 vector = Vector2.Lerp((owner as GeckoTail).segments[0].lastPos, (owner as GeckoTail).segments[0].pos, timeStacker);
                
                for (int i = 0; i < 4; i++)
                {
                    Vector2 vector3 = Vector2.Lerp((owner as GeckoTail).segments[i].lastPos, (owner as GeckoTail).segments[i].pos, timeStacker);
                    Vector2 normalized;
                    if (i < 3)
                    {
                        if (i == 0)
                        {
                            normalized = (Vector2.Lerp(owner.firstChunk.lastPos, owner.firstChunk.pos, timeStacker) - vector).normalized;
                        }
                        else
                        {
                            Vector2 vector4 = Vector2.Lerp((owner as GeckoTail).segments[i + 1].lastPos, (owner as GeckoTail).segments[i + 1].pos, timeStacker);
                            normalized = (vector3 - vector4).normalized;
                        }
                    }
                    else
                    {
                        Vector2 vector4 = Vector2.Lerp((owner as GeckoTail).segments[2].lastPos, (owner as GeckoTail).segments[2].pos, timeStacker);
                        normalized = (vector4 - vector3).normalized;
                    }
                    Vector2 a = Custom.PerpendicularVector(normalized);
                    float d3 = Vector2.Distance(vector3, vector) / 5f;
                    if (i == 0)
                    {
                        d3 = 0f;
                    }
                    (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4, (i != 0 ? vector : owner.firstChunk.pos) - a * d2 + normalized * d3 - camPos);
                    (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 1, (i != 0 ? vector : owner.firstChunk.pos) + a * d2 + normalized * d3 - camPos);
                    if (i < 3)
                    {
                        (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 2, vector3 - a * (owner as GeckoTail).segments[i].StretchedRad - normalized * d3 - camPos);
                        (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 3, vector3 + a * (owner as GeckoTail).segments[i].StretchedRad - normalized * d3 - camPos);
                    }
                    else
                    {
                        (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 2, vector3 - camPos);
                    }
                    d2 = (owner as GeckoTail).segments[i].StretchedRad;
                    vector = vector3;
                }
            }
        }
    }

    public class GeckoModule
    {
        public bool tailCut;
        public int escapeCount;

        public GeckoModule()
        {
            tailCut = false;
            escapeCount = 40;
        }
    }
}
