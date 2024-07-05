using RandomBuffUtils;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using UnityEngine;
using RWCustom;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using MoreSlugcats;
using System.Collections.Generic;
using UnityEngine.LowLevel;
using System.Numerics;
using static RandomBuff.Render.UI.Component.RandomBuffFlag;
using BuiltinBuffs.Positive;
using CustomSaveTx;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace BuiltinBuffs.Duality
{
    internal class JellyfishShapedMutationBuff : Buff<JellyfishShapedMutationBuff, JellyfishShapedMutationBuffData>
    {
        public override bool Triggerable => true;

        public override BuffID ID => JellyfishShapedMutationBuffEntry.JellyfishShapedMutation;

        public int JellyfishCatLevel
        {
            get
            {
                int num = 0;
                if (GetTemporaryBuffPool().allBuffIDs.Contains(BuiltinBuffs.Positive.BreathlessBuffEntry.Breathless))
                    num++;
                if (GetTemporaryBuffPool().allBuffIDs.Contains(BuiltinBuffs.Positive.SuperCapacitanceBuffEntry.SuperCapacitance))
                    num++;
                if (GetTemporaryBuffPool().allBuffIDs.Contains(BuiltinBuffs.Positive.WaterDancerBuffEntry.WaterDancer))
                    num++;
                if (GetTemporaryBuffPool().allBuffIDs.Contains(HotDogBuff.WaterNobleBuffEntry.WaterNobleID))
                    num++;
                if (GetTemporaryBuffPool().allBuffIDs.Contains(BuiltinBuffs.Negative.HydrophobiaBuffEntry.Hydrophobia))
                    num--;
                return num;
            }
        }

        public JellyfishShapedMutationBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    var jellyfishCat = new JellyfishCat(player);
                    JellyfishShapedMutationBuffEntry.JellyfishCatFeatures.Add(player, jellyfishCat);
                    jellyfishCat.JellyfishBody(player.graphicsModule as PlayerGraphics);
                    jellyfishCat.JellyfishTentacle();
                    jellyfishCat.JellyfishMouthBeads();
                    jellyfishCat.JellyfishOralArm(player.graphicsModule as PlayerGraphics);
                    jellyfishCat.InitiateSprites(game.cameras[0].spriteLeasers.
                        First(i => i.drawableObject == player.graphicsModule), game.cameras[0]);/*
                    jellyfishCat.AddToContainer(game.cameras[0].spriteLeasers.
                        First(i => i.drawableObject == player.graphicsModule), game.cameras[0], null);*/
                }
            }
        }

        public override bool Trigger(RainWorldGame game)
        {
            foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
            {
                if (JellyfishShapedMutationBuffEntry.JellyfishCatFeatures.TryGetValue(player, out var jellyfishCat))
                {
                    jellyfishCat.TryToAttack = true;
                }
            }
            return false;
        }
    }

    internal class JellyfishShapedMutationBuffData : BuffData
    {
        public override BuffID ID => JellyfishShapedMutationBuffEntry.JellyfishShapedMutation;

        [JsonProperty]
        public int dehydrationCycle;
    }

    internal class JellyfishShapedMutationBuffEntry : IBuffEntry
    {
        public static BuffID JellyfishShapedMutation = new BuffID("JellyfishShapedMutation", true);
        public static ConditionalWeakTable<Player, JellyfishCat> JellyfishCatFeatures = new ConditionalWeakTable<Player, JellyfishCat>();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<JellyfishShapedMutationBuff, JellyfishShapedMutationBuffData, JellyfishShapedMutationBuffEntry>(JellyfishShapedMutation);
        }

        public static void HookOn()
        {
            On.SaveState.SessionEnded += SaveState_SessionEnded;
            On.Player.Grabability += Player_Grabability;
            On.Player.FreeHand += Player_FreeHand;
            On.SlugcatHand.Update += SlugcatHand_Update;

            On.StaticWorld.InitStaticWorld += StaticWorld_InitStaticWorld;
            On.JellyFish.Collide += JellyFish_Collide;
            On.Centipede.Shock += Centipede_Shock;

            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.Player.NewRoom += Player_NewRoom;
            On.Player.Die += Player_Die;

            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.Reset += PlayerGraphics_Reset;
            On.PlayerGraphics.ctor += PlayerGraphics_ctor;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
            //On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
        }
        #region 额外特性
        //脱水致死
        private static void SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
        {
            foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player).Where(i => i != null))
            {
                if (JellyfishCatFeatures.TryGetValue(player, out var jellyfishCat))
                {
                    if (!jellyfishCat.HasContactWater)
                        (BuffCore.GetBuffData(JellyfishShapedMutationBuffEntry.JellyfishShapedMutation) as JellyfishShapedMutationBuffData).dehydrationCycle++;
                    else
                        (BuffCore.GetBuffData(JellyfishShapedMutationBuffEntry.JellyfishShapedMutation) as JellyfishShapedMutationBuffData).dehydrationCycle = 0;

                    if ((BuffCore.GetBuffData(JellyfishShapedMutationBuffEntry.JellyfishShapedMutation) as JellyfishShapedMutationBuffData).dehydrationCycle == 2)
                        survived = false;
                }
            }
            orig(self, game, survived, newMalnourished);
        }

        //双手位置
        private static void SlugcatHand_Update(On.SlugcatHand.orig_Update orig, SlugcatHand self)
        {
            if (JellyfishCatFeatures.TryGetValue(self.owner.owner as Player, out var jellyfishCat))
                (self.owner.owner as Player).craftingObject = true;
            orig(self);

            if (JellyfishCatFeatures.TryGetValue(self.owner.owner as Player, out jellyfishCat))
                jellyfishCat.SlugcatHandUpdate(self);
        }

        //只能一次叼一个东西
        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            Player.ObjectGrabability result = orig(self, obj);

            if (JellyfishCatFeatures.TryGetValue(self, out var jellyfishCat))
            {
                result = jellyfishCat.Grabability(result);
            }

            return result;
        }

        private static int Player_FreeHand(On.Player.orig_FreeHand orig, Player self)
        {
            int result = orig(self);
            if (JellyfishCatFeatures.TryGetValue(self, out var jellyfishCat))
                if (self.grasps[0] != null || self.grasps[0] != null)
                {
                    result = -1;
                }
            return result;
        }
        #endregion
        #region 生物关系
        //修改生物关系（巨型水母、水蛭不再攻击玩家）
        private static void StaticWorld_InitStaticWorld(On.StaticWorld.orig_InitStaticWorld orig)
        {
            orig();

            StaticWorld.EstablishRelationship(MoreSlugcatsEnums.CreatureTemplateType.BigJelly, CreatureTemplate.Type.Slugcat, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.Leech, CreatureTemplate.Type.Slugcat, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f));
        }

        //玩家逐渐免疫小水母的攻击
        private static void JellyFish_Collide(On.JellyFish.orig_Collide orig, JellyFish self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            int origStun = -1;
            if (otherObject is Player && otherObject != self.thrownBy && self.Electric &&
               JellyfishCatFeatures.TryGetValue(otherObject as Player, out var jellyfishCat))
            {
                Player player = (Player)otherObject;
                origStun = player.stun;
            }

            orig(self, otherObject, myChunk, otherChunk);

            if(otherObject is Player && origStun != -1 &&
               JellyfishCatFeatures.TryGetValue(otherObject as Player, out jellyfishCat))
            {
                Player player = (Player)otherObject;
                player.stun = Mathf.FloorToInt(Mathf.Lerp(origStun, player.stun, Mathf.InverseLerp(2f, -1f, Mathf.Sqrt(JellyfishShapedMutationBuff.Instance.JellyfishCatLevel + 1))));
            }
        }

        //玩家逐渐免疫蜈蚣的攻击
        private static void Centipede_Shock(On.Centipede.orig_Shock orig, Centipede self, PhysicalObject shockObj)
        {
            int origStun = -1;
            if (shockObj is Player &&
               JellyfishCatFeatures.TryGetValue(shockObj as Player, out var jellyfishCat))
            {
                Player player = (Player)shockObj;
                origStun = player.stun;
                jellyfishCat.ImmuneShock = true;
            }

            orig(self, shockObj);

            if (shockObj is Player && origStun != -1 &&
               JellyfishCatFeatures.TryGetValue(shockObj as Player, out jellyfishCat))
            {
                Player player = (Player)shockObj;
                if (!jellyfishCat.ImmuneShock)
                    player.Stun(6);
                else
                    jellyfishCat.ImmuneShock = false;
                player.stun = Mathf.FloorToInt(Mathf.Lerp(origStun, player.stun, Mathf.InverseLerp(2f, -1f, Mathf.Sqrt(JellyfishShapedMutationBuff.Instance.JellyfishCatLevel + 1))));
            }
        }
        #endregion

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!JellyfishCatFeatures.TryGetValue(self, out _))
                JellyfishCatFeatures.Add(self, new JellyfishCat(self));
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (JellyfishCatFeatures.TryGetValue(self, out var jellyfishCat))
            {
                jellyfishCat.Update();
                self.GetExPlayerData().HaveHands = false;

                //水下呼吸，但游泳速度降低50%
                self.airInLungs = 1f; 
                if (self.animation == Player.AnimationIndex.SurfaceSwim || self.animation == Player.AnimationIndex.DeepSwim)
                {
                    Vector2 addPos = 0.5f * Vector2.Lerp(self.bodyChunks[0].pos - self.bodyChunks[0].lastPos, self.bodyChunks[1].pos - self.bodyChunks[1].lastPos, 0.5f);
                    self.bodyChunks[0].pos -= addPos;
                    self.bodyChunks[1].pos -= addPos;
                }

                if (self.grabbedBy.Count > 0)
                {
                    for (int i = 0; i < self.grabbedBy.Count; i++)
                    {
                        Creature grabber = self.grabbedBy[i].grabber;
                        if (grabber != null && grabber.grasps != null)
                        {
                            if (jellyfishCat.TryElectricAttack(grabber))
                            {
                                for (int j = 0; j < grabber.grasps.Length; j++)
                                {
                                    if (grabber.grasps[j] != null &&
                                        grabber.grasps[j].grabbed != null &&
                                        grabber.grasps[j].grabbed == self)
                                    {
                                        grabber.ReleaseGrasp(j);
                                    }
                                }
                                jellyfishCat.ElectricAttack(grabber);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            orig(self, newRoom);

            if (JellyfishCatFeatures.TryGetValue(self, out var jellyfishCat))
                jellyfishCat.Reset();
        }

        private static void Player_Die(On.Player.orig_Die orig, Player self)
        {
            if (JellyfishCatFeatures.TryGetValue(self, out var jellyfishCat))
            {
                if (jellyfishCat.ImmuneShock)
                {
                    jellyfishCat.ImmuneShock = false;
                    return;
                }
            }
            orig(self);
        }
        #region 外观
        private static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (JellyfishCatFeatures.TryGetValue(self.player, out var jellyfishCat))
                jellyfishCat.ApplyPalette(sLeaser, rCam, palette);
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (JellyfishCatFeatures.TryGetValue(self.player, out var jellyfishCat))
                jellyfishCat.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (JellyfishCatFeatures.TryGetValue(self.player, out var jellyfishCat))
                jellyfishCat.InitiateSprites(sLeaser, rCam);
        }

        private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (JellyfishCatFeatures.TryGetValue(self.player, out var jellyfishCat))
                jellyfishCat.AddToContainer(sLeaser, rCam, newContatiner);
        }

        private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if (JellyfishCatFeatures.TryGetValue(self.player, out var jellyfishCat))
            {
                jellyfishCat.JellyfishBody(self);
                jellyfishCat.JellyfishTentacle();
                jellyfishCat.JellyfishMouthBeads();
                jellyfishCat.JellyfishOralArm(self);
            }
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            if (JellyfishCatFeatures.TryGetValue(self.player, out var jellyfishCat))
                jellyfishCat.GraphicsUpdate();
        }

        private static void PlayerGraphics_Reset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig(self);
            if (JellyfishCatFeatures.TryGetValue(self.player, out var jellyfishCat))
                jellyfishCat.Reset();
        }
        #endregion
    }

    internal class JellyfishCat
    {
        WeakReference<Player> ownerRef;

        #region 行动相关
        private JellyfishCatState abstractState;
        
        private bool hasContactWater;
        private int dehydrationCycle;

        private bool goHome;
        private bool tryToAttack;
        private bool immuneShock;
        private int mooCounter;
        private int SMSuckCounter;
        private int timeAbove;
        private int electricCounter;
        private int electricChargingTime;

        private int pressPckpTime;

        public bool ImmuneShock
        {
            get
            {
                return immuneShock;
            }
            set
            {
                immuneShock = value;
            }
        }

        public bool TryToAttack
        {
            get
            {
                return this.tryToAttack;
            }
            set
            {
                this.tryToAttack = value;
            }
        }

        public bool Electric
        {
            get
            {
                return this.electricCounter <= 0;
            }
        }

        public bool canBeSurfaceMode
        {
            get
            {
                if (!ownerRef.TryGetTarget(out var player))
                    return false;
                if (surfaceMode)
                {
                    return player.room.FloatWaterLevel(newBody[CoreChunk].pos.x) < newBody[0].pos.y + 14f;
                }
                return false;
            }
        }

        public bool HasContactWater
        {
            get
            {
                return this.hasContactWater;
            }
            set
            {
                this.hasContactWater = value;
            }
        }

        public bool WantToMoveTentacle
        {
            get
            {
                return pressPckpTime >= 10;
            }
        }
        #endregion

        #region 绘图相关
        BodyPart[] newBody;
        float canopyLength;

        private BodyChunk[,] latchOnToBodyChunks;
        public Vector2[,][,] tentacles;
        public bool anyTentaclePulled;
        public float tentaclesWithdrawn;
        private float[,] tentacleScaler;

        public Vector2 rotation;
        public Vector2 lastRotation;
        private Color color;

        private LightSource myLight;
        private float LightCounter;
        public float darkness;
        public float lastDarkness;

        private Vector2? huntPos;
        private int huntingCounter;
        private List<Creature> consumedCreatures;

        private int CoreChunk;
        private float coreLength;
        private Color coreColor;
        private Color coreColorDark;

        private int leftHoodChunk;
        private int rightHoodChunk;
        private float hoodPulse;
        private float hoodSwayingPulse;
        float hoodLength;

        private bool surfaceMode;

        private float minTGap;
        private float maxTGap;

        private float[] mouthBeads;

        private Vector2 driftGoalPos;
        private float driftCounter;
        private float driftMaxim;

        private TailSegment[,][] oralArm;
        private Vector2[,] oralArmOffsets;
        private float oralArmSway;
        private int oralArmTimeAdd;
        private float oralArmSpacing = 6f;
        private float oralArmWidth = 1f;
        #endregion

        #region sprite序号数据
        private int firstSprite;
        public int TotalSprites => CoreSpriteLength + oralArmOffsets.Length + tentacles.Length + BodySpriteLength + hoodSpriteLength + mouthBeads.Length;
        //核心
        public int CoreSpriteStart => firstSprite;
        public int CoreSpriteLength => 3;
        //口腕
        public int OralArmsStart => CoreSpriteStart + CoreSpriteLength;
        //触须
        public int TentaclesStart => OralArmsStart + oralArmOffsets.Length;
        //身体
        public int BodySpriteStart => TentaclesStart + tentacles.Length;
        public int BodySpriteLength => 5;
        //伞盖
        private int hoodSpriteStart => BodySpriteStart + BodySpriteLength;
        private int hoodSpriteLength => 2;
        //口珠
        private int MouthSpriteStart => hoodSpriteStart + hoodSpriteLength;

        public int TentacleSprite(int i, int j)
        {
            return TentaclesStart + i * tentacles.GetLength(1) + j;
        }

        public int OralArmSprite(int i, int j)
        {
            return OralArmsStart + i * oralArmOffsets.GetLength(1) + j;
        }
        #endregion

        #region 身体部件
        public void JellyfishBody(PlayerGraphics self)
        {
            newBody = new BodyPart[7];
            newBody[0] = new GenericBodyPart(self, 12.1f, 0.7f, 0.999f, self.owner.firstChunk);

            CoreChunk = 1;
            newBody[CoreChunk] = new BodyPart(self);//new GenericBodyPart(self, 0.28f * newBody[0].rad, 0.7f, 0.999f, self.owner.bodyChunks[1]);
            newBody[CoreChunk].pos = self.tail[self.tail.Length - 1].pos;//newBody[0].pos + new Vector2(0f, -coreLength);

            leftHoodChunk = CoreChunk + 1;
            newBody[leftHoodChunk] = new GenericBodyPart(self, 0f, 0.7f, 0.999f, self.owner.firstChunk);

            rightHoodChunk = CoreChunk + 2;
            newBody[rightHoodChunk] = new GenericBodyPart(self, 0f, 0.7f, 0.999f, self.owner.firstChunk);

            newBody[4] = new GenericBodyPart(self, 0.28f * newBody[0].rad, 0.7f, 0.999f, self.owner.firstChunk);
            newBody[4].pos = newBody[0].pos + new Vector2(0f, 10f);
            newBody[5] = new GenericBodyPart(self, 0.28f * newBody[0].rad, 0.7f, 0.999f, self.owner.firstChunk);
            newBody[5].pos = newBody[5].pos + new Vector2(-10f, 0f);
            newBody[6] = new GenericBodyPart(self, 0.28f * newBody[0].rad, 0.7f, 0.999f, self.owner.firstChunk);
            newBody[6].pos = newBody[6].pos + new Vector2(10f, 0f);

            JellyfishBodyConnectToPoint(self);
        }

        public void JellyfishBodyConnectToPoint(PlayerGraphics self)
        {
            newBody[CoreChunk].ConnectToPoint(self.tail[self.tail.Length - 1].pos, coreLength, false, 0.4f, self.tail[self.tail.Length - 1].vel, 0.1f, 0.4f);
            newBody[4].ConnectToPoint(newBody[0].pos, canopyLength, false, 0.7f, newBody[0].vel, 0.1f, 0.7f);
            newBody[5].ConnectToPoint(newBody[0].pos, canopyLength, false, 0.7f, newBody[0].vel, 0.1f, 0.7f);
            newBody[6].ConnectToPoint(newBody[0].pos, canopyLength, false, 0.7f, newBody[0].vel, 0.1f, 0.7f);
            newBody[leftHoodChunk].ConnectToPoint(newBody[0].pos, hoodLength, false, 0.1f, newBody[0].vel, 0.1f, 0.1f);
            newBody[rightHoodChunk].ConnectToPoint(newBody[0].pos, hoodLength, false, 0.1f, newBody[0].vel, 0.1f, 0.1f);
        }

        //触须
        public void JellyfishTentacle()
        {
            int singleSideNum = 2;
            tentacles = new Vector2[2, singleSideNum][,];
            if (abstractState.deadArmDriftPos == null)
            {
                abstractState.deadArmDriftPos = new Vector2[2, singleSideNum];
            }
            latchOnToBodyChunks = new BodyChunk[2, singleSideNum];
            tentacleScaler = new float[2, singleSideNum];
            for (int i = 0; i < tentacles.GetLength(0); i++)
            {
                for (int j = 0; j < tentacles.GetLength(1); j++)
                {
                    int length = j == 0 ? 11 : 10;
                    tentacles[i, j] = new Vector2[length, 3];
                    tentacleScaler[i, j] = Mathf.Lerp(6f, 8f, Mathf.InverseLerp(4f, 17f, length));//Mathf.Lerp(18f, 60f, Mathf.InverseLerp(4f, 17f, num2));
                }
            }
        }

        //口珠
        public void JellyfishMouthBeads()
        {
            mouthBeads = new float[Random.Range(3, 5)];
            //mouthBeads = new float[Random.Range(17, 23)];
            for (int j = 0; j < mouthBeads.Length; j++)
            {
                mouthBeads[j] = 18f + (-1.6f + Random.value * 3.2f);
                if (Random.value < 0.5f)
                {
                    mouthBeads[j] += 36f;
                }
            }
        }

        //口腕
        public void JellyfishOralArm(PlayerGraphics self)
        {
            int singleSideNum = 2;
            int length = 8;
            oralArmOffsets = new Vector2[2, singleSideNum];
            float height = -12f;
            float width = 8f;
            for (int i = 0; i < oralArmOffsets.GetLength(0); i++)
            {
                for (int j = 0; j < oralArmOffsets.GetLength(1); j++)
                {
                    oralArmOffsets[i, j] = new Vector2(width * (i == 0 ? 1f : -1f) * (j == 0 ? 0.5f : 1f), height * (j == 0 ? 0f : 1f));
                }
            }

            oralArm = new TailSegment[2, singleSideNum][];
            for (int i = 0; i < oralArm.GetLength(0); i++)
            {
                for (int j = 0; j < oralArm.GetLength(1); j++)
                {
                    oralArm[i, j] = new TailSegment[length];
                    oralArm[i, j][0] = new TailSegment(self, 5f, 4f, null, 0.85f, 0.9f, 3f, true);
                    for (int k = 1; k < length; k++)
                        oralArm[i, j][k] = new TailSegment(self, 5f, 7f, oralArm[i, j][k - 1], 0.55f, 0.9f, 0.1f, true);
                }
            }
        }
        #endregion

        public JellyfishCat(Player player)
        {
            this.ownerRef = new WeakReference<Player>(player);

            Random.State state = Random.state;
            Random.InitState(player.abstractCreature.ID.RandomSeed);
            abstractState = new JellyfishCatState(player.abstractCreature);
            this.hasContactWater = false;
            this.dehydrationCycle = (BuffCore.GetBuffData(JellyfishShapedMutationBuffEntry.JellyfishShapedMutation) as JellyfishShapedMutationBuffData).dehydrationCycle;
            minTGap = 0.96f;//2.4f;
            maxTGap = 2.84f;//7.1f;
            coreLength = 24f; 
            canopyLength = 10f; 
            hoodLength = 30f;
            player.airFriction = 0.999f;
            player.gravity = 0.9f;
            player.bounce = 0.2f;
            player.surfaceFriction = 0.7f;
            //player.collisionLayer = 1;
            player.waterFriction = 0.95f;
            player.buoyancy = 0f;
            consumedCreatures = new List<Creature>();

            driftCounter = 0f;
            goHome = false;
            //StartPos = Vector2.zero;

            coreColor = new Color(0.82f, 0.42f, 0.24f);
            coreColorDark = new Color(0.64f, 0.14f, 0.09f);
            Random.state = state;

            electricChargingTime = 1200;
            electricCounter = 0;
        }

        #region 外观
        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            firstSprite = sLeaser.sprites.Length;
            Array.Resize(ref sLeaser.sprites, firstSprite + TotalSprites);

            coreColor = sLeaser.sprites[0].color;
            Color.RGBToHSV(coreColor, out var h, out var s, out var v);
            coreColorDark = Color.Lerp(coreColor,
            Color.HSVToRGB(h, s, v > 0.5f ? v - 0.3f : v + 0.3f), 0.8f);

            sLeaser.sprites[CoreSpriteStart] = TriangleMesh.MakeLongMesh(8, pointyTip: false, customColor: true);
            sLeaser.sprites[CoreSpriteStart + 1] = new FSprite("Futile_White");
            sLeaser.sprites[CoreSpriteStart + 1].scale = 2.5f / 5f;
            sLeaser.sprites[CoreSpriteStart + 1].shader = rCam.room.game.rainWorld.Shaders["VectorCircle"];
            sLeaser.sprites[CoreSpriteStart + 2] = new FSprite("Futile_White");
            sLeaser.sprites[CoreSpriteStart + 2].scale = 1.9230769f / 5f;
            sLeaser.sprites[CoreSpriteStart + 2].shader = rCam.room.game.rainWorld.Shaders["VectorCircle"];
            #region 口腕
            for (int i = 0; i < oralArmOffsets.GetLength(0); i++)
            {
                for (int j = 0; j < oralArmOffsets.GetLength(1); j++)
                {
                    TriangleMesh triangleMesh = MakeLongMesh(oralArm[i, j].GetLength(0), pointyTip: false, customColor: true, "OralArm", atlasedImage : true); 
                    FAtlasElement elementWithName = Futile.atlasManager.GetElementWithName("OralArm");
                    Vector2 vec1 = elementWithName.uvBottomRight;
                    Vector2 vec2 = elementWithName.uvBottomLeft;
                    Vector2 vec3 = elementWithName.uvTopLeft;
                    Vector2 vec4 = elementWithName.uvTopRight;
                    triangleMesh.UVvertices[0] = vec1;
                    triangleMesh.UVvertices[1] = vec2;
                    triangleMesh.UVvertices[triangleMesh.vertices.Length - 2] = vec3;
                    triangleMesh.UVvertices[triangleMesh.vertices.Length - 1] = vec4;
                    for (int m = 2; m <= triangleMesh.vertices.Length - 1; m += 2)
                    {
                        triangleMesh.UVvertices[m].x = triangleMesh.UVvertices[0].x;
                        triangleMesh.UVvertices[m].y = Mathf.Lerp(triangleMesh.UVvertices[0].y,
                                                                  triangleMesh.UVvertices[triangleMesh.vertices.Length - 1].y,
                                                                  Mathf.InverseLerp(0f, (float)triangleMesh.vertices.Length - 1f, (float)m));
                    }
                    for (int n = 3; n <= triangleMesh.vertices.Length - 2; n += 2)
                    {
                        triangleMesh.UVvertices[n].x = triangleMesh.UVvertices[1].x;
                        triangleMesh.UVvertices[n].y = Mathf.Lerp(triangleMesh.UVvertices[1].y,
                                                                  triangleMesh.UVvertices[triangleMesh.vertices.Length - 2].y,
                                                                  Mathf.InverseLerp(1f, (float)triangleMesh.vertices.Length - 2f, (float)n));
                    }

                    sLeaser.sprites[OralArmSprite(i, j)] = triangleMesh;
                }
            }
            #endregion
            for (int i = 0; i < tentacles.GetLength(0); i++)
                for (int j = 0; j < tentacles.GetLength(1); j++)
                    sLeaser.sprites[TentacleSprite(i, j)] = TriangleMesh.MakeLongMesh(tentacles[i, j].GetLength(0), pointyTip: false, customColor: true);

            sLeaser.sprites[BodySpriteStart] = new FSprite("Futile_White");
            sLeaser.sprites[BodySpriteStart].scale = newBody[0].rad / 20f;
            sLeaser.sprites[BodySpriteStart].shader = rCam.room.game.rainWorld.Shaders["VectorCircle"];
            sLeaser.sprites[BodySpriteStart].isVisible = false;
            sLeaser.sprites[BodySpriteStart + 1] = new FSprite("Futile_White");
            sLeaser.sprites[BodySpriteStart + 1].scale = newBody[4].rad / 2f;
            sLeaser.sprites[BodySpriteStart + 1].shader = rCam.room.game.rainWorld.Shaders["VectorCircle"];
            sLeaser.sprites[BodySpriteStart + 2] = new FSprite("Futile_White");
            sLeaser.sprites[BodySpriteStart + 2].scale = newBody[5].rad / 2f;
            sLeaser.sprites[BodySpriteStart + 2].shader = rCam.room.game.rainWorld.Shaders["VectorCircle"];
            sLeaser.sprites[BodySpriteStart + 3] = new FSprite("Futile_White");
            sLeaser.sprites[BodySpriteStart + 3].scale = newBody[6].rad / 2f;
            sLeaser.sprites[BodySpriteStart + 3].shader = rCam.room.game.rainWorld.Shaders["VectorCircle"];
            sLeaser.sprites[BodySpriteStart + 4] = TriangleMesh.MakeLongMesh(3, pointyTip: false, customColor: true);

            sLeaser.sprites[hoodSpriteStart] = TriangleMesh.MakeLongMesh(6, pointyTip: false, customColor: true);
            sLeaser.sprites[hoodSpriteStart + 1] = TriangleMesh.MakeLongMesh(6, pointyTip: false, customColor: true);

            for (int j = 0; j < mouthBeads.Length; j++)
            {
                sLeaser.sprites[MouthSpriteStart + j] = new FSprite("DangleFruit0A");
                sLeaser.sprites[MouthSpriteStart + j].rotation = mouthBeads[j];
                sLeaser.sprites[MouthSpriteStart + j].scale = 1.34f / 3f;
            }

            //self.AddToContainer(sLeaser, rCam, null);
            this.AddToContainer(sLeaser, rCam, null);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;

            Color color = new Color(0.87f, 0.78f, 0.55f);
            float num = rCam.PaletteDarkness();
            Color a = Color.Lerp(Color.Lerp(palette.fogColor, color, 0.3f), palette.blackColor, Mathf.Pow(num, 2f));
            Color a2 = Color.Lerp(Color.Lerp(a, new Color(1f, 1f, 1f), 0.5f), palette.blackColor, Mathf.Pow(num, 2f));
            Color a3 = Color.Lerp(color, palette.blackColor, Mathf.Clamp(num, 0.1f, 1f));
            color = Color.Lerp(color, palette.blackColor, darkness);
            a = Color.Lerp(a, palette.blackColor, darkness);
            a2 = Color.Lerp(a2, palette.blackColor, darkness);
            a3 = Color.Lerp(a3, palette.blackColor, darkness);
            sLeaser.sprites[BodySpriteStart].color = a2;
            sLeaser.sprites[BodySpriteStart + 1].color = Color.Lerp(a2, coreColorDark, 0.2f);
            sLeaser.sprites[BodySpriteStart + 2].color = Color.Lerp(a2, coreColorDark, 0.2f);
            sLeaser.sprites[BodySpriteStart + 3].color = Color.Lerp(a2, coreColorDark, 0.3f);
            sLeaser.sprites[CoreSpriteStart + 1].color = coreColor;
            sLeaser.sprites[CoreSpriteStart + 2].color = coreColorDark;
            this.color = a;
            for (int i = 0; i < oralArmOffsets.GetLength(0); i++)
                for (int j = 0; j < oralArmOffsets.GetLength(1); j++) 
                    for (int k = 0; k < (sLeaser.sprites[OralArmSprite(i, j)] as TriangleMesh).verticeColors.Length; k++)
                        (sLeaser.sprites[OralArmSprite(i, j)] as TriangleMesh).verticeColors[k] = Color.Lerp(a, coreColor, 0.4f);
            for (int i = 0; i < tentacles.GetLength(0); i++)
                for (int j = 0; j < tentacles.GetLength(1); j++)
                    for (int k = 0; k < (sLeaser.sprites[TentacleSprite(i, j)] as TriangleMesh).verticeColors.Length; k++)
                        (sLeaser.sprites[TentacleSprite(i, j)] as TriangleMesh).verticeColors[k] = Color.Lerp(a, a3, (float)k / (float)((sLeaser.sprites[TentacleSprite(i, j)] as TriangleMesh).verticeColors.Length - 1));
            Color a4 = a2;
            for (int k = 0; k < (sLeaser.sprites[BodySpriteStart + 4] as TriangleMesh).verticeColors.Length; k++)
            {
                (sLeaser.sprites[BodySpriteStart + 4] as TriangleMesh).verticeColors[k] = Color.Lerp(a2, sLeaser.sprites[BodySpriteStart + 3].color, (float)k / (float)((sLeaser.sprites[BodySpriteStart + 4] as TriangleMesh).verticeColors.Length - 1));
                if (k == 5)
                {
                    a4 = (sLeaser.sprites[BodySpriteStart + 4] as TriangleMesh).verticeColors[k];
                }
            }
            for (int l = 0; l < (sLeaser.sprites[hoodSpriteStart] as TriangleMesh).verticeColors.Length; l++)
            {
                (sLeaser.sprites[hoodSpriteStart] as TriangleMesh).verticeColors[l] = Color.Lerp(a4, a, (float)l / (float)((sLeaser.sprites[hoodSpriteStart + 1] as TriangleMesh).verticeColors.Length - 1));
                (sLeaser.sprites[hoodSpriteStart + 1] as TriangleMesh).verticeColors[l] = Color.Lerp(a4, a, (float)l / (float)((sLeaser.sprites[hoodSpriteStart + 1] as TriangleMesh).verticeColors.Length - 1));
            }
            for (int m = 0; m < (sLeaser.sprites[CoreSpriteStart] as TriangleMesh).verticeColors.Length; m++)
            {
                (sLeaser.sprites[CoreSpriteStart] as TriangleMesh).verticeColors[m] = Color.Lerp(a, coreColor, (float)m / (float)((sLeaser.sprites[CoreSpriteStart] as TriangleMesh).verticeColors.Length - 1));
            }
            for (int n = 0; n < mouthBeads.Length; n++)
            {
                sLeaser.sprites[MouthSpriteStart + n].color = a;
            }
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            if (firstSprite >= 1 && sLeaser.sprites.Length >= firstSprite + TotalSprites)
            {
                var midgroundContainer = rCam.ReturnFContainer("Midground");
                for (int i = 0; i < TotalSprites; i++)
                {
                    sLeaser.sprites[firstSprite + i].RemoveFromContainer();
                    midgroundContainer.AddChild(sLeaser.sprites[i]);
                }

                for (int i = 0; i < this.oralArm.GetLength(0); i++)
                {
                    for (int j = 0; j < this.oralArm.GetLength(1); j++)
                    {
                        var sprite = sLeaser.sprites[OralArmSprite(i, j)];
                        sprite.MoveBehindOtherNode(sLeaser.sprites[0]);
                    }
                }
                for (int i = 0; i < this.tentacles.GetLength(0); i++)
                {
                    for (int j = 0; j < this.tentacles.GetLength(1); j++)
                    {
                        var sprite = sLeaser.sprites[TentacleSprite(i, j)];
                        sprite.MoveBehindOtherNode(sLeaser.sprites[0]);
                    }
                }
            }
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (!ownerRef.TryGetTarget(out var player) || player.graphicsModule == null || sLeaser == null || player.room == null)
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            
            if (sLeaser.sprites.Length >= 9)
                for (int i = 4; i <= 8; i++)
                    sLeaser.sprites[i].isVisible = false;
            sLeaser.sprites[CoreSpriteStart].isVisible = false;//口珠的纽带，直接隐藏
            
            for (int i = 0; i < BodySpriteLength; i++)
            {
                sLeaser.sprites[BodySpriteStart + i].isVisible = false;
            }

            Vector2 p = Vector2.Lerp(newBody[5].lastPos, newBody[5].pos, timeStacker);
            Vector2 bodyPos = Vector2.Lerp(newBody[0].lastPos, newBody[0].pos, timeStacker);
            Vector2 corePos = Vector2.Lerp(newBody[CoreChunk].lastPos, newBody[CoreChunk].pos, timeStacker);
            Vector2 p2 = Vector3.Slerp(lastRotation, rotation, timeStacker);
            p2 = Custom.DirVec(Vector2.zero, p2);
            lastDarkness = darkness;
            darkness = rCam.room.Darkness(bodyPos) * (1f - rCam.room.LightSourceExposure(bodyPos));
            if (darkness != lastDarkness)
            {
                ApplyPalette(sLeaser, rCam, rCam.currentPalette);
            }
            sLeaser.sprites[BodySpriteStart + 1].scale = newBody[4].rad / 10f;
            sLeaser.sprites[BodySpriteStart + 2].scale = newBody[5].rad / 10f;
            sLeaser.sprites[BodySpriteStart + 3].scale = newBody[6].rad / 10f;
            Vector2 mouthLeftPos = MouthLeftPos(timeStacker);
            Vector2 mouthRightPos = MouthRightPos(timeStacker);
            for (int i = 0; i < mouthBeads.Length; i++)
            {
                float t = (float)i / (float)mouthBeads.Length;
                float num = 0f;
                Vector2 vector5 = Custom.PerpendicularVector(p2) * -1.6f + p2 * 0.6f + Vector2.Lerp(MouthLeftPos(timeStacker), MouthRightPos(timeStacker), t); //Custom.PerpendicularVector(p2) * -8f + p2 * 3f + Vector2.Lerp(MouthLeftPos(timeStacker), MouthRightPos(timeStacker), t);
                if (canBeSurfaceMode && player.Submersion > 0.1f)
                {
                    num = player.room.FloatWaterLevel(vector5.x) - (float)(player.room.defaultWaterLevel + 1) * 4f;//player.room.FloatWaterLevel(vector5.x) - (float)(player.room.defaultWaterLevel + 1) * 20f;
                    num /= 2f;
                }
                Vector2 vector6 = p2 * num;
                sLeaser.sprites[MouthSpriteStart + i].x = vector5.x + vector6.x - camPos.x;
                sLeaser.sprites[MouthSpriteStart + i].y = vector5.y + vector6.y - 5f - camPos.y;
                sLeaser.sprites[MouthSpriteStart + i].rotation = mouthBeads[i];
                if (i == 0)
                {
                    mouthLeftPos = vector5 + vector6;
                }
                if (i == mouthBeads.Length - 1)
                {
                    mouthRightPos = vector5 + vector6;
                }
            }
            int num2 = 8;
            Vector2 vector7 = bodyPos;
            Vector2 vector8 = Custom.DirVec(bodyPos, corePos).normalized;
            Vector2 vector9 = Custom.PerpendicularVector(vector8);
            float num3 = Vector2.Distance(bodyPos, corePos) / (float)num2;
            float num4 = Vector2.Distance(bodyPos, corePos) / 3.137254f;
            float num5 = 0f;
            for (int j = 0; j < num2; j++)
            {
                vector8 = Vector2.Lerp(Custom.DirVec(bodyPos, corePos).normalized, Custom.DirVec(p, bodyPos).normalized, j / num2);
                float t2 = (float)SMSuckCounter / 100f;
                Vector2 vector10 = vector7;
                float num6 = Mathf.Sin(num5 / num4);
                Vector2 vector11 = vector9 * (Mathf.Lerp(4f, 1.6f, t2) - 2f * num6); //vector9 * (Mathf.Lerp(20f, 8f, t2) - 10f * num6);
                if (j == 0)
                {
                    (sLeaser.sprites[CoreSpriteStart] as TriangleMesh).MoveVertice(j * 4, mouthLeftPos - camPos);
                    (sLeaser.sprites[CoreSpriteStart] as TriangleMesh).MoveVertice(j * 4 + 1, mouthRightPos - camPos);
                    (sLeaser.sprites[CoreSpriteStart] as TriangleMesh).MoveVertice(j * 4 + 2, Vector2.Lerp(vector10 - vector11, mouthLeftPos, 0.5f) - camPos);
                    (sLeaser.sprites[CoreSpriteStart] as TriangleMesh).MoveVertice(j * 4 + 3, Vector2.Lerp(vector10 + vector11, mouthRightPos, 0.5f) - camPos);
                }
                else
                {
                    (sLeaser.sprites[CoreSpriteStart] as TriangleMesh).MoveVertice(j * 4, vector10 - vector11 - camPos);
                    (sLeaser.sprites[CoreSpriteStart] as TriangleMesh).MoveVertice(j * 4 + 1, vector10 + vector11 - camPos);
                    (sLeaser.sprites[CoreSpriteStart] as TriangleMesh).MoveVertice(j * 4 + 2, vector10 - vector11 - camPos);
                    (sLeaser.sprites[CoreSpriteStart] as TriangleMesh).MoveVertice(j * 4 + 3, vector10 + vector11 - camPos);
                }
                num5 += num3;
                vector7 = vector10 + vector8 * num3;
            }
            for (int k = 0; k < BodySpriteLength - 1; k++)
            {
                Vector2 vector12 = bodyPos;
                if (k == 1)
                {
                    vector12 = Vector2.Lerp(newBody[6].lastPos, newBody[6].pos, timeStacker);
                }
                if (k == 2)
                {
                    vector12 = Vector2.Lerp(newBody[5].lastPos, newBody[5].pos, timeStacker);
                }
                if (k == 3)
                {
                    vector12 = Vector2.Lerp(newBody[4].lastPos, newBody[4].pos, timeStacker);
                }
                sLeaser.sprites[BodySpriteStart + k].x = vector12.x - camPos.x;
                sLeaser.sprites[BodySpriteStart + k].y = vector12.y - camPos.y;
                sLeaser.sprites[BodySpriteStart + k].rotation = Custom.VecToDeg(p2);
            }
            num2 = 6;
            Vector2 a = (vector7 = bodyPos + p2 * 2f); //(vector7 = bodyPos + p2 * 10f);
            Vector2 vector13 = p2 * -1f;
            num3 = Vector2.Distance(a, AttachPos(0, 1f)) / (float)num2;
            num4 = Vector2.Distance(a, AttachPos(0, 1f)) / 3.137254f;
            num5 = 0f;
            for (int l = 0; l < num2; l++)
            {
                float t3 = (float)l / ((float)num2 - 1f);
                Vector2 vector14 = vector7;
                Vector2 vector15 = vector7 + p2 * -5f;//vector7 + p2 * -25f;
                float num7 = Mathf.Sin(num5 / num4);
                Vector2 vector16 = vector9 * (10f + 4f * num7); //vector9 * (50f + 20f * num7);
                Vector2 vector17 = vector9 * (2f + 12f * num7);//vector9 * (10f + 60f * num7);
                (sLeaser.sprites[hoodSpriteStart] as TriangleMesh).MoveVertice(l * 4, Vector2.Lerp(vector15 - vector17, mouthLeftPos, t3) - camPos);
                (sLeaser.sprites[hoodSpriteStart] as TriangleMesh).MoveVertice(l * 4 + 1, Vector2.Lerp(vector15 + vector17, mouthRightPos, t3) - camPos);
                (sLeaser.sprites[hoodSpriteStart] as TriangleMesh).MoveVertice(l * 4 + 2, Vector2.Lerp(vector15 - vector17, mouthLeftPos, t3) - camPos);
                (sLeaser.sprites[hoodSpriteStart] as TriangleMesh).MoveVertice(l * 4 + 3, Vector2.Lerp(vector15 + vector17, mouthRightPos, t3) - camPos);
                (sLeaser.sprites[hoodSpriteStart + 1] as TriangleMesh).MoveVertice(l * 4, Vector2.Lerp(vector14 - vector16, mouthLeftPos, t3) - camPos);
                (sLeaser.sprites[hoodSpriteStart + 1] as TriangleMesh).MoveVertice(l * 4 + 1, Vector2.Lerp(vector14 + vector16, mouthRightPos, t3) - camPos);
                (sLeaser.sprites[hoodSpriteStart + 1] as TriangleMesh).MoveVertice(l * 4 + 2, Vector2.Lerp(vector14 - vector16, mouthLeftPos, t3) - camPos);
                (sLeaser.sprites[hoodSpriteStart + 1] as TriangleMesh).MoveVertice(l * 4 + 3, Vector2.Lerp(vector14 + vector16, mouthRightPos, t3) - camPos);
                num5 += num3;
                vector7 = vector14 + vector13 * num3;
            }
            Vector2 vector18 = Vector2.Lerp(newBody[4].lastPos, newBody[4].pos, timeStacker) + p2 * newBody[4].rad / 2f;
            Vector2 vector19 = Vector2.Lerp(newBody[6].lastPos, newBody[6].pos, timeStacker);
            Vector2 vector20 = Vector2.Lerp(newBody[5].lastPos, newBody[5].pos, timeStacker);
            (sLeaser.sprites[BodySpriteStart + 4] as TriangleMesh).MoveVertice(0, bodyPos + p2 * -15f + Custom.PerpendicularVector(p2) * (newBody[4].rad * -0.3f) - camPos);
            (sLeaser.sprites[BodySpriteStart + 4] as TriangleMesh).MoveVertice(1, bodyPos + p2 * -15f + Custom.PerpendicularVector(p2) * (newBody[4].rad * 0.3f) - camPos);
            (sLeaser.sprites[BodySpriteStart + 4] as TriangleMesh).MoveVertice(2, bodyPos + p2 * -10f + Custom.PerpendicularVector(p2) * (newBody[4].rad * -0.45f) - camPos);
            (sLeaser.sprites[BodySpriteStart + 4] as TriangleMesh).MoveVertice(3, bodyPos + p2 * -10f + Custom.PerpendicularVector(p2) * (newBody[4].rad * 0.45f) - camPos);
            (sLeaser.sprites[BodySpriteStart + 4] as TriangleMesh).MoveVertice(4, bodyPos + p2 * 5f + Custom.PerpendicularVector(vector8) * Mathf.Lerp(48f, 55f, 1f - hoodSwayingPulse) - camPos);
            (sLeaser.sprites[BodySpriteStart + 4] as TriangleMesh).MoveVertice(5, bodyPos + p2 * 5f + Custom.PerpendicularVector(vector8) * Mathf.Lerp(-48f, -55f, 1f - hoodSwayingPulse) - camPos);
            (sLeaser.sprites[BodySpriteStart + 4] as TriangleMesh).MoveVertice(6, vector19 + Custom.PerpendicularVector(p2) * (newBody[6].rad * Mathf.Lerp(-0.8f, -0.6f, 1f - hoodSwayingPulse)) - camPos);
            (sLeaser.sprites[BodySpriteStart + 4] as TriangleMesh).MoveVertice(7, vector20 + Custom.PerpendicularVector(p2) * (newBody[5].rad * Mathf.Lerp(0.8f, 0.6f, 1f - hoodSwayingPulse)) - camPos);
            (sLeaser.sprites[BodySpriteStart + 4] as TriangleMesh).MoveVertice(8, vector19 + p2 * newBody[6].rad / 1.9f + Custom.PerpendicularVector(p2) * (newBody[5].rad * -0.4f) - camPos);
            (sLeaser.sprites[BodySpriteStart + 4] as TriangleMesh).MoveVertice(9, vector20 + p2 * newBody[5].rad / 1.9f + Custom.PerpendicularVector(p2) * (newBody[6].rad * 0.4f) - camPos);
            (sLeaser.sprites[BodySpriteStart + 4] as TriangleMesh).MoveVertice(10, vector18 + Custom.PerpendicularVector(p2) * (newBody[4].rad * -0.5f) - camPos);
            (sLeaser.sprites[BodySpriteStart + 4] as TriangleMesh).MoveVertice(11, vector18 + Custom.PerpendicularVector(p2) * (newBody[4].rad * 0.5f) - camPos);
            sLeaser.sprites[CoreSpriteStart + 1].x = corePos.x - camPos.x;
            sLeaser.sprites[CoreSpriteStart + 1].y = corePos.y - camPos.y;
            sLeaser.sprites[CoreSpriteStart + 2].x = corePos.x - camPos.x;
            sLeaser.sprites[CoreSpriteStart + 2].y = corePos.y - camPos.y;

            #region 口腕
            //身体位置
            Vector2 drawPos1 = Vector2.Lerp(player.bodyChunks[0].lastPos, player.bodyChunks[0].pos, timeStacker);
            //臀部位置
            Vector2 drawPos2 = Vector2.Lerp(player.bodyChunks[1].lastPos, player.bodyChunks[1].pos, timeStacker);
            //身体至臀部方向的向量
            Vector2 dif = (drawPos1 - drawPos2).normalized;
            //身体旋转角度
            float bodyRotation = Mathf.Atan2(dif.x, dif.y);

            //通过身体角度判断移动
            var moveDeg = Mathf.Clamp(Custom.AimFromOneVectorToAnother(Vector2.zero, (drawPos2 - drawPos1).normalized), -22.5f, 22.5f);
            //身体方向
            var dir = Custom.DirVec(player.bodyChunks[0].pos, player.bodyChunks[1].pos).normalized;
            var lastDir = Custom.DirVec(player.bodyChunks[0].lastPos, player.bodyChunks[1].lastPos).normalized;

            for (int i = 0; i < oralArm.GetLength(0); i++)
            {
                for (int j = 0; j < oralArm.GetLength(1); j++)
                {
                    //实际偏移
                    var nowSpacing = oralArmSpacing * (Mathf.Abs(moveDeg) > 10 ? 0.3f : 1f) * (j == 0 ? 0.5f : 1f);
                    var rootPos = player.bodyChunks[0].pos + 5f * dif + (i == 0 ? -1 : 1) * Custom.PerpendicularVector(dir).normalized * nowSpacing + dir * -0.2f;
                    Vector2 vector2 = Vector2.Lerp(Vector2.Lerp(player.bodyChunks[1].lastPos, player.bodyChunks[0].lastPos, 0.35f) + 
                                      (i == 0 ? -1 : 1) * Custom.PerpendicularVector(lastDir).normalized * nowSpacing + lastDir * 5f, 
                                      rootPos, 
                                      timeStacker);
                    Vector2 vector4 = (vector2 * 3f + rootPos) / 4f;
                    float d2 = 6f;
                    for (int k = 0; k < oralArm[i, j].Length; k++)
                    {
                        Vector2 vector5 = Vector2.Lerp(oralArm[i, j][k].lastPos, oralArm[i, j][k].pos, timeStacker);
                        Vector2 normalized = (vector5 - vector4).normalized;
                        Vector2 widthDir = Custom.PerpendicularVector(normalized);
                        float d3 = Vector2.Distance(vector5, vector4) / 5f;

                        if (k == 0)
                        {
                            d3 = 0f;
                        }

                        TriangleMesh oralArmMesh = sLeaser.sprites[OralArmSprite(i, j)] as TriangleMesh;
                        //设置坐标
                        oralArmMesh.MoveVertice(k * 4, vector4 - widthDir * d2 * oralArmWidth + normalized * d3 - camPos);
                        oralArmMesh.MoveVertice(k * 4 + 1, vector4 + widthDir * d2 * oralArmWidth + normalized * d3 - camPos);

                        d2 = oralArm[i, j][k].StretchedRad;
                        oralArmMesh.MoveVertice(k * 4 + 2, vector5 - widthDir * d2 * oralArmWidth - normalized * d3 - camPos);
                        oralArmMesh.MoveVertice(k * 4 + 3, vector5 + widthDir * d2 * oralArmWidth - normalized * d3 - camPos);
                        /*
                        if (k < oralArm[i, j].Length - 1)
                        {
                            oralArmMesh.MoveVertice(k * 4 + 2, vector5 - widthDir * oralArm[i, j][k].StretchedRad * oralArmWidth - normalized * d3 - camPos);
                            oralArmMesh.MoveVertice(k * 4 + 3, vector5 + widthDir * oralArm[i, j][k].StretchedRad * oralArmWidth - normalized * d3 - camPos);
                        }
                        else
                        {
                            oralArmMesh.MoveVertice(k * 4 + 2, vector5 - camPos);
                            oralArmMesh.MoveVertice(k * 4 + 3, vector5 - camPos);
                        }*/
                        vector4 = vector5;
                    }
                }
            }
            #endregion
            for (int i = 0; i < tentacles.GetLength(0); i++)
            {
                for (int j = 0; j < tentacles.GetLength(1); j++)
                {
                    float lastTentacleWidth = 0f;
                    Vector2 attachPos = AttachPos(i * tentacles.GetLength(1) + j, timeStacker);
                    for (int num10 = 0; num10 < tentacles[i, j].GetLength(0); num10++)
                    {
                        Vector2 tentaclePos = Vector2.Lerp(tentacles[i, j][num10, 1], tentacles[i, j][num10, 0], timeStacker);
                        //宽度
                        float tentacleWidth = Mathf.Lerp(1.5f, 0.2f, (float)num10 / (float)tentacles[i, j].GetLength(0));//Mathf.Lerp(3f, 0.2f, (float)num10 / (float)tentacles[n].GetLength(0));
                        Vector2 normalized = (attachPos - tentaclePos).normalized;
                        Vector2 widthDirection = Custom.PerpendicularVector(normalized);
                        float d = Vector2.Distance(attachPos, tentaclePos) / 5f;
                        (sLeaser.sprites[TentacleSprite(i, j)] as TriangleMesh).MoveVertice(num10 * 4, attachPos - normalized * d - widthDirection * (tentacleWidth + lastTentacleWidth) * 0.5f - camPos);
                        (sLeaser.sprites[TentacleSprite(i, j)] as TriangleMesh).MoveVertice(num10 * 4 + 1, attachPos - normalized * d + widthDirection * (tentacleWidth + lastTentacleWidth) * 0.5f - camPos);
                        (sLeaser.sprites[TentacleSprite(i, j)] as TriangleMesh).MoveVertice(num10 * 4 + 2, tentaclePos + normalized * d - widthDirection * tentacleWidth - camPos);
                        (sLeaser.sprites[TentacleSprite(i, j)] as TriangleMesh).MoveVertice(num10 * 4 + 3, tentaclePos + normalized * d + widthDirection * tentacleWidth - camPos);
                        attachPos = tentaclePos;
                        lastTentacleWidth = tentacleWidth;
                    }
                }
            }
        }

        public void GraphicsUpdate()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;

            JellyfishBodyConnectToPoint(self);
            /*
            oralArmTimeAdd++;
            if (oralArmTimeAdd > 100)
                oralArmTimeAdd = 0;
            */
            Vector2 smoothedBodyPos = player.firstChunk.lastPos;
            Vector2 bodyDir = Custom.DirVec(player.bodyChunks[1].lastPos, smoothedBodyPos);
            Vector2 perpBodyDir = Custom.PerpendicularVector(bodyDir);
            for (int i = 0; i < oralArm.GetLength(0); i++)
            {
                //通过身体角度判断移动
                var moveDeg = Mathf.Clamp(Custom.AimFromOneVectorToAnother(Vector2.zero, bodyDir), -22.5f, 22.5f);

                var num3 = 1f - Mathf.Clamp((Mathf.Abs(Mathf.Lerp(player.bodyChunks[1].vel.x, player.bodyChunks[0].vel.x, 0.35f)) - 1f) * 0.5f, 0f, 1f);
                for (int j = 0; j < oralArm.GetLength(1); j++)
                {
                    //实际偏移
                    var nowSpacing = oralArmSpacing * (Mathf.Abs(moveDeg) > 15 ? 0.3f : 1f) * (1f - this.hoodSwayingPulse) * (j == 0 ? 0.5f : 1f);
                    var rootPos = smoothedBodyPos + (i == 0 ? -1 : 1) * perpBodyDir.normalized * nowSpacing; // + bodyDir * 5f;
                    Vector2 vector2 = rootPos;
                    Vector2 pos = rootPos;
                    float num9 = 28f;

                    oralArm[i, j][0].connectedPoint = new Vector2?(rootPos);
                    for (int k = 0; k < oralArm[i, j].Length; k++)
                    {
                        oralArm[i, j][k].Update();
                        oralArm[i, j][k].vel *= Mathf.Lerp(0.75f, 0.9f, num3 * (1f - player.bodyChunks[1].submersion));//水中减少速度

                        TailSegment tailSegment = oralArm[i, j][k];
                        tailSegment.vel.y = tailSegment.vel.y - Mathf.Lerp(0.1f, 0.5f, num3) * (1f - player.bodyChunks[1].submersion) * player.EffectiveRoomGravity;
                        num3 = (num3 * 10f + 1f) / 11f;

                        Vector2 perp = Custom.PerpendicularVector(rootPos, oralArm[i, j][k].pos);
                        /*
                        if (k == 0)
                        {
                            oralArm[i, j][k].vel += (perp * (Random.value - 0.5f) / Vector2.Distance(vector2, oralArm[i, j][k].pos) * 3f *
                                             (Random.value > 0.95f || Random.value < 0.05f ? 1f : 0f)) *
                                              Mathf.Min(player.Submersion, 1f - player.EffectiveRoomGravity); 
                        }*/
                        /*
                        if (k > 0)
                        {
                            oralArm[i, j][k].vel += (Vector2.Dot(Custom.DirVec(oralArm[i, j][k].lastPos, oralArm[i, j][k - 1].lastPos), perp.normalized) * perp.normalized *
                                             (Vector2.SignedAngle(Custom.DirVec(oralArm[i, j][k].lastPos, oralArm[i, j][k - 1].lastPos), perp.normalized) > 0 ? 1f : -1f)) *
                                             0.3f * Mathf.Pow(((float)(oralArm[i, j].Length - k)) / (float)oralArm[i, j].Length, 1.5f) * 
                                             Mathf.Min(player.Submersion, 1f - player.EffectiveRoomGravity);
                        }*/

                        if (k > 1)
                        {
                            Vector2 normalized = (oralArm[i, j][k].pos - oralArm[i, j][k - 2].pos).normalized;
                            oralArm[i, j][k].vel += normalized * 0.2f;
                            oralArm[i, j][k - 2].vel -= normalized * 0.2f;
                        }

                        num9 *= 0.25f;
                        vector2 = pos;
                        pos = oralArm[i, j][k].pos;
                    }
                }
            }
        }

        public void Reset()
        {
            if (!ownerRef.TryGetTarget(out var self))
                return;
            for (int i = 0; i < tentacles.GetLength(0); i++)
            {
                for (int j = 0; j < tentacles.GetLength(1); j++)
                {
                    for (int k = 0; k < tentacles[i, j].GetLength(0); k++)
                    {
                        tentacles[i, j][k, 0] = self.firstChunk.pos;//pos
                        tentacles[i, j][k, 1] = tentacles[i, j][k, 0];//lastPos
                        tentacles[i, j][k, 2] *= 0f;//速度
                    }
                }
            }
            for (int i = 0; i < oralArm.GetLength(0); i++)
            {
                for (int j = 0; j < oralArm.GetLength(1); j++)
                {
                    for (int k = 0; k < oralArm[i, j].GetLength(0); k++)
                    {
                        oralArm[i, j][k].pos = self.firstChunk.pos;//pos
                        oralArm[i, j][k].lastPos = oralArm[i, j][k].pos;//lastPos
                        oralArm[i, j][k].vel *= 0f;//速度
                    }
                }
            }
        }
        #endregion

        //进行更新
        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var self))
                return;
            if (self.room == null)
                return;
            if (self.Submersion > 0.1f && !this.hasContactWater)
            {
                this.hasContactWater = true;
                BuffPlugin.Log("JellyfishCat has contact with water!");
            }
            if (!this.Electric)
            {
                this.electricCounter--;
                if (this.Electric)
                {
                    for (int j = 0; j < 15; j++)
                    {
                        Vector2 a = Custom.DegToVec(360f * UnityEngine.Random.value);
                        self.room.AddObject(new MouseSpark(self.firstChunk.pos + a * 18f, self.firstChunk.vel + a * 36f * UnityEngine.Random.value, 20f, new Color(0.7f, 1f, 1f)));
                        self.room.PlaySound(SoundID.Jelly_Fish_Tentacle_Latch_On_Player, self.firstChunk.pos);
                    }
                }
            }
            mooCounter--;
            if (SMSuckCounter > 0)
            {
                SMSuckCounter++;
                for (int i = 0; i < newBody.Length; i++)
                {
                    if (i != CoreChunk)
                    {
                        newBody[i].rad *= 0.98f;
                        newBody[i].vel += Custom.RNV();
                    }
                }
                for (int j = 0; j < newBody.Length; j++)
                {
                    newBody[j].rad *= 0.98f;//bodyChunkConnections[j].distance *= 0.98f;
                }
                if (SMSuckCounter >= 100)
                {
                    //Die();
                    return;
                }
            }
            if (huntingCounter > 0)
            {
                huntingCounter--;
            }
            else
            {
                huntingCounter = 0;
                huntPos = null;
            }
            oralArmSway += 0.05f + 0.05f * Mathf.Clamp(newBody[0].vel.magnitude, 0f, 5f);
            ConsumeCreateUpdate();
            surfaceMode = self.animation == Player.AnimationIndex.SurfaceSwim;/*
            if (StartPos == Vector2.zero)
            {
                if (abstractState.HomePos == Vector2.zero)
                {
                    StartPos = self.room.MiddleOfTile(self.abstractCreature.pos);
                }
                else
                {
                    StartPos = abstractState.HomePos;
                }
                //surfaceMode = !self.room.PointSubmerged(StartPos + new Vector2(0f, 80f));
                if (surfaceMode)
                {
                    StartPos.y = (float)(self.room.defaultWaterLevel + 1) * 20f;
                }
                BuffPlugin.Log("Jelly home at " + StartPos.ToString());
            }*/
            if (driftGoalPos == Vector2.zero)//abstractState.DriftPos
            {
                driftGoalPos = self.room.MiddleOfTile(self.abstractCreature.pos);
            }
            else if(self.animation == Player.AnimationIndex.DeepSwim)
            {
                driftGoalPos = self.bodyChunks[0].pos + 30f * new Vector2(self.input[0].x, self.input[0].y);// abstractState.DriftPos;
            }/*
            if (!surfaceMode && driftGoalPos.y > (float)(self.room.defaultWaterLevel + 1) * 4f)
            {
                driftGoalPos.y = (float)(self.room.defaultWaterLevel + 1) * 4f;
            }*/
            /*
            BuffPlugin.Log("Jelly goal at " + driftGoalPos.ToString());
            BuffPlugin.Log("Jelly on surf " + surfaceMode.ToString());*/
            driftMaxim = Vector2.Distance(newBody[0].pos, driftGoalPos);

            for (int i = 0; i < newBody.Length; i++)
            {
                newBody[i].lastPos = newBody[i].pos;
            }
            newBody[0].pos = self.firstChunk.pos;
            PlayerGraphics g = self.graphicsModule as PlayerGraphics;
            newBody[CoreChunk].pos = g.tail[g.tail.Length - 1].pos;// newBody[0].pos + new Vector2(0f, -8f);
            newBody[CoreChunk].vel *= 0f;
            newBody[leftHoodChunk].pos = newBody[0].pos + new Vector2(-4f, -2f);
            newBody[leftHoodChunk].vel *= 0f;
            newBody[rightHoodChunk].pos = newBody[0].pos + new Vector2(4f, -2f);
            newBody[rightHoodChunk].vel *= 0f;
            if (canBeSurfaceMode)
            {/*
                if (self.firstChunk.pos.y < StartPos.y)
                {
                    newBody[0].vel += new Vector2(0f, self.gravity * 2.7f * Mathf.InverseLerp(0f, 0.2f, self.Submersion));
                    newBody[4].vel = new Vector2(0f, self.gravity * 4.1f * Mathf.InverseLerp(0f, 0.3f, self.Submersion));
                    newBody[5].vel = new Vector2(0f, self.gravity * 3f * Mathf.InverseLerp(0f, 0.5f, self.Submersion));
                    newBody[6].vel = new Vector2(0f, self.gravity * 3f * Mathf.InverseLerp(0f, 0.5f, self.Submersion));
                }
                if (self.firstChunk.pos.y > StartPos.y - 10f)
                {
                    BodyChunk bodyChunk = newBody[0];
                    bodyChunk.vel.y = bodyChunk.vel.y * 0.4f;
                }*//*
                Vector2 vector = Custom.DirVec(self.firstChunk.pos, newBody[0].pos);
                vector.y *= 0f;
                vector.x *= Mathf.InverseLerp(0f, 30f, Vector2.Distance(self.firstChunk.pos, newBody[0].pos)) / 2f;
                self.firstChunk.vel *= 0.9f;
                self.firstChunk.vel += vector;*/
            }
            else
            {
                newBody[CoreChunk].vel += new Vector2(0f, -0.28f);
                newBody[0].vel += new Vector2(0f, (self.gravity * 1.95f + 1f) * 0.72f);
                newBody[4].vel = new Vector2(0f, self.gravity * 3f * 0.72f);
                newBody[5].vel = new Vector2(0f, self.gravity * 3f * 0.72f);
                newBody[6].vel = new Vector2(0f, self.gravity * 3f * 0.72f);
            }
            newBody[4].pos = Custom.MoveTowards(newBody[4].pos, newBody[0].pos + rotation * 8f, 5f);
            Vector2 vector2 = Custom.PerpendicularVector(Custom.DirVec(newBody[4].pos, newBody[0].pos + rotation));
            newBody[5].pos = Custom.MoveTowards(newBody[4].pos, newBody[0].pos + rotation * -4f + vector2 * -27f, 5f);
            newBody[6].pos = Custom.MoveTowards(newBody[4].pos, newBody[0].pos + rotation * -4f + vector2 * 27f, 5f);
            bool flag = true;/*
            if (!self.safariControlled)
            {
                Vector2 b = (goHome ? driftGoalPos : newBody[0].pos);
                driftCounter += 1f;
                if (driftCounter > driftMaxim * 2f || Vector2.Distance(self.firstChunk.pos, b) > driftMaxim)
                {
                    goHome = !goHome;
                    driftCounter = 0f;
                }
            }
            else*/
            if (self.inputWithDiagonals.HasValue)
            {
                flag = false;
                if (self.inputWithDiagonals.Value.AnyDirectionalInput)
                {
                    if (!huntPos.HasValue)
                    {
                        huntPos = newBody[CoreChunk].pos;
                    }
                    if (Vector2.Distance(huntPos.Value + new Vector2(self.inputWithDiagonals.Value.x, self.inputWithDiagonals.Value.y) * 5f, newBody[CoreChunk].pos) < 180f)
                    {
                        newHuntPos(huntPos.Value + new Vector2(self.inputWithDiagonals.Value.x, self.inputWithDiagonals.Value.y) * 5f);
                    }
                }
                if (!canBeSurfaceMode)
                {
                    if (self.inputWithDiagonals.Value.thrw)
                    {
                        if (driftGoalPos.y > newBody[0].pos.y)
                        {
                            goHome = true;
                        }
                        else
                        {
                            goHome = false;
                        }
                        flag = true;
                    }
                    else if (self.inputWithDiagonals.Value.jmp)
                    {
                        if (driftGoalPos.y > newBody[0].pos.y)
                        {
                            goHome = false;
                        }
                        else
                        {
                            goHome = true;
                        }
                        flag = true;
                    }
                }
                if (self.inputWithDiagonals.Value.pckp)
                {
                    PlayHorrifyingMoo();
                }
            }
            //newBody[leftHoodChunk].vel *= 0.2f;
            //newBody[rightHoodChunk].vel *= 0.2f;
            Vector2 zero = Vector2.zero;
            zero = ((!goHome) ? (Custom.DirVec(newBody[0].pos, driftGoalPos) / 10f) : (Custom.DirVec(newBody[0].pos, newBody[0].pos) / 10f));
            bool flag2 = false;
            float num = Mathf.Clamp(Mathf.Abs(zero.y * 10f), 0f, 1f);
            if (Mathf.Abs(newBody[0].pos.y - driftGoalPos.y) < 8f)
            {
                hoodPulse += 0.02f;
                hoodSwayingPulse = 0.5f + Mathf.Sin(hoodPulse) / 2f;
                flag2 = true;
            }
            if (!flag)
            {
                zero = new Vector2(0f, -0.09f * self.gravity);
                hoodPulse = Mathf.Lerp(hoodPulse, 0.63f, 0.08f);
            }
            if (zero.y < 0f)
            {
                if (!flag2)
                {
                    hoodPulse = Mathf.Clamp(hoodPulse - 0.01f, 0.1f, 1f);
                }
                zero.y *= 8f * num;
            }
            if (zero.y > 0f)
            {
                if (!flag2)
                {
                    hoodPulse = Mathf.Clamp(hoodPulse + 0.03f, 0.1f, 1f);
                }
                zero.y *= -7f * (1f - num);
            }
            newBody[0].vel += zero;
            if (canBeSurfaceMode)
            {
                hoodSwayingPulse = 0.1f + Mathf.Pow(1f - self.Submersion, 20f) * 0.9f;
            }
            else if (!flag2)
            {
                hoodSwayingPulse = 0.1f + hoodPulse * 0.9f;
            }
            else
            {
                hoodSwayingPulse = 0.1f + hoodSwayingPulse * 0.9f;
            }
            newBody[CoreChunk].vel += Custom.DirVec(newBody[CoreChunk].pos, newBody[0].pos) / 5f;
            Custom.DirVec(newBody[CoreChunk].pos, self.firstChunk.pos);
            Vector2 vector3 = self.firstChunk.pos + Custom.DirVec(self.firstChunk.pos, newBody[4].pos).normalized * 4f + Custom.DirVec(self.firstChunk.pos, newBody[CoreChunk].pos) * 26f * hoodSwayingPulse;
            float speed = 5.8f;
            Vector2 vector4 = Custom.PerpendicularVector(vector3) * 6f;
            newBody[leftHoodChunk].pos = Custom.MoveTowards(newBody[leftHoodChunk].pos, vector3 - vector4, speed);
            newBody[rightHoodChunk].pos = Custom.MoveTowards(newBody[rightHoodChunk].pos, vector3 + vector4, speed);
            tentaclesWithdrawn = 0f;/*
            if (!anyTentaclePulled)
            {
                rotation = Vector3.Slerp(rotation, new Vector2(0f, 1f), (1f - 2f * Mathf.Abs(0.5f - self.firstChunk.submersion)) * 0.1f);
            }
            rotation = Vector3.Slerp(rotation, new Vector2(0f, 1f), 1f - Mathf.Abs(rotation.y));*/
            rotation = (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized;
            if (self.firstChunk.ContactPoint.y < 0)
            {
                BodyChunk bodyChunk2 = self.firstChunk;
                bodyChunk2.vel.x = bodyChunk2.vel.x * 0.8f;
            }
            LightUpdate();
            MoveTentacleToAttack();
            this.TryToAttack = false;
        }

        public bool TryElectricAttack(Creature creature)
        {
            if (!ownerRef.TryGetTarget(out var self) || creature is BigEel || creature is Player)
                return false;

            if (this.Electric && self.FoodInStomach >= 1)
            {
                return true;
            }

            return false;
        }

        public void ElectricAttack(Creature creature)
        {
            if (!ownerRef.TryGetTarget(out var self) || creature is BigEel || creature is Player)
                return;
            self.SubtractFood(1);
            creature.Violence(self.firstChunk,
                             new Vector2?(Custom.DirVec(self.firstChunk.pos, creature.firstChunk.pos) * 5f),
                             creature.firstChunk, null, Creature.DamageType.Electric, 0.1f,
                             (320f * Mathf.Lerp(creature.Template.baseStunResistance, 1f, 0.5f)));//(creature is Player) ? 140f : (320f * Mathf.Lerp(creature.Template.baseStunResistance, 1f, 0.5f)));
            self.room.AddObject(new CreatureSpasmer(creature, false, creature.stun));
            self.room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, self.firstChunk.pos);
            self.room.AddObject(new Explosion.ExplosionLight(self.firstChunk.pos, 200f, 1f, 4, new Color(0.7f, 1f, 1f)));
            this.electricCounter = this.electricChargingTime;
        }

        public void MoveTentacleToAttack()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;

            if (player.input[0].pckp)
                pressPckpTime++;
            else
                pressPckpTime = 0;

            if (WantToMoveTentacle)
            {
                //身体不再移动
                for (int i = 0; i < player.bodyChunks.Length; i++)
                {
                    player.bodyChunks[i].pos = player.bodyChunks[i].lastPos;
                    player.bodyChunks[i].vel *= 0;
                }
                //设置触须移动位置
                huntPos = player.bodyChunks[0].pos + 100f * new Vector2(player.input[0].x, player.input[0].y);
            }
            anyTentaclePulled = false;
            for (int i = 0; i < tentacles.GetLength(0); i++)
            {
                for (int j = 0; j < tentacles.GetLength(1); j++)
                {
                    float num2 = Mathf.Lerp(tentacleScaler[i, j], 1f, tentaclesWithdrawn);
                    for (int l = 0; l < tentacles[i, j].GetLength(0); l++)
                    {
                        float t = (float)l / (float)(tentacles[i, j].GetLength(0) - 1);//在单根触须上的长度占比
                        tentacles[i, j][l, 1] = tentacles[i, j][l, 0];//lastPos
                        tentacles[i, j][l, 0] += tentacles[i, j][l, 2];//pos
                        tentacles[i, j][l, 2] -= rotation * Mathf.InverseLerp(4f, 0f, l) * 0.8f;//vel
                        if (player.room.PointSubmerged(tentacles[i, j][l, 0]))
                        {
                            tentacles[i, j][l, 2] *= Custom.LerpMap(tentacles[i, j][l, 2].magnitude, 1f, 10f, 1f, 0.5f, Mathf.Lerp(1.4f, 0.4f, t));
                            Vector2 vector5 = new Vector2(0f, 0f);
                            if (huntPos.HasValue)
                            {
                                vector5 = Custom.DirVec(tentacles[i, j][l, 0], huntPos.Value);
                            }
                            tentacles[i, j][l, 2] += Custom.RNV() * 0.2f + vector5 * 0.2f;
                        }
                        else
                        {
                            tentacles[i, j][l, 2] *= 0.999f;
                            tentacles[i, j][l, 2].y -= player.room.gravity * 0.6f;
                            SharedPhysics.TerrainCollisionData cd2 = SharedPhysics.HorizontalCollision(cd: new SharedPhysics.TerrainCollisionData(tentacles[i, j][l, 0], tentacles[i, j][l, 1], tentacles[i, j][l, 2], 1f, new IntVector2(0, 0), goThroughFloors: false), room: player.room);
                            cd2 = SharedPhysics.VerticalCollision(player.room, cd2);
                            cd2 = SharedPhysics.SlopesVertically(player.room, cd2);
                            tentacles[i, j][l, 0] = cd2.pos;
                            tentacles[i, j][l, 2] = cd2.vel;
                        }
                    }
                    for (int m = 0; m < tentacles[i, j].GetLength(0); m++)
                    {
                        if (m > 0)
                        {
                            Vector2 normalized = (tentacles[i, j][m, 0] - tentacles[i, j][m - 1, 0]).normalized;
                            float num3 = Vector2.Distance(tentacles[i, j][m, 0], tentacles[i, j][m - 1, 0]);
                            tentacles[i, j][m, 0] += normalized * (num2 - num3) * 0.5f;
                            tentacles[i, j][m, 2] += normalized * (num2 - num3) * 0.5f;
                            tentacles[i, j][m - 1, 0] -= normalized * (num2 - num3) * 0.5f;
                            tentacles[i, j][m - 1, 2] -= normalized * (num2 - num3) * 0.5f;
                            if (m > 1)
                            {
                                normalized = (tentacles[i, j][m, 0] - tentacles[i, j][m - 2, 0]).normalized;
                                tentacles[i, j][m, 2] += normalized * 0.2f;
                                tentacles[i, j][m - 2, 2] -= normalized * 0.2f;
                            }
                        }
                        //触须根部位置
                        else
                        {
                            float num4 = 0f;
                            float width = 8f;
                            Vector2 vector6 = AttachPos(i * tentacles.GetLength(1) + j, 1f);
                            if (canBeSurfaceMode && player.Submersion > 0.1f)
                            {
                                num4 = (player.room.FloatWaterLevel(vector6.x) - (float)(player.room.defaultWaterLevel + 1) * 20f) / 1.9f;
                            }
                            Vector2 vector7 = rotation * num4;
                            Vector2 perp = width * Custom.PerpendicularVector(rotation);
                            tentacles[i, j][m, 0] = vector6 + vector7 + perp * (i == 0 ? 1f : -1f) * (j == 0 ? 0.5f : 1f);
                            tentacles[i, j][m, 2] *= 0f;
                        }
                    }
                    if (latchOnToBodyChunks[i, j] != null && latchOnToBodyChunks[i, j].owner is Creature && (latchOnToBodyChunks[i, j].owner as Creature).enteringShortCut.HasValue)
                    {
                        BuffPlugin.Log($"JellyCat released door traveling object {latchOnToBodyChunks[i, j].owner}");
                        latchOnToBodyChunks[i, j] = null;
                    }
                    if (latchOnToBodyChunks[i, j] != null)
                    {
                        bool flag3 = false;
                        if (latchOnToBodyChunks[i, j].owner is Player && MMF.cfgGraspWiggling.Value)
                        {
                            flag3 = (latchOnToBodyChunks[i, j].owner as Player).GraspWiggle > 0.8f;
                        }
                        if (!player.dead && player.room.PointSubmerged(tentacles[i, j][tentacles[i, j].GetLength(0) - 1, 0]) &&
                            !flag3 && !player.Stunned && !consumedCreatures.Contains(latchOnToBodyChunks[i, j].owner as Creature))
                        {
                            if (this.TryToAttack && this.TryElectricAttack(latchOnToBodyChunks[i, j].owner as Creature))
                            {
                                ElectricAttack(latchOnToBodyChunks[i, j].owner as Creature);
                            }
                            newHuntPos(latchOnToBodyChunks[i, j].pos);
                            float num5 = newBody[0].pos.y - newBody[0].rad;
                            float num6 = Mathf.InverseLerp(num5 - 10f, num5 + 10f, latchOnToBodyChunks[i, j].pos.y);
                            float num7 = Mathf.Sign(latchOnToBodyChunks[i, j].pos.x - newBody[0].pos.x) * Mathf.InverseLerp(num5 - 10f, num5 + 5f, latchOnToBodyChunks[i, j].pos.y) / 5f;
                            num7 *= Mathf.InverseLerp(50f, 0f, Mathf.Abs(latchOnToBodyChunks[i, j].pos.x - newBody[0].pos.x));
                            Vector2 vector8 = Custom.DirVec(latchOnToBodyChunks[i, j].pos + rotation * (-40f * num6), new Vector2(newBody[0].pos.x, num5)) / 10f;
                            vector8.x += num7;
                            latchOnToBodyChunks[i, j].vel += vector8;
                            if (latchOnToBodyChunks[i, j].pos.y > num5)
                            {
                                timeAbove++;
                                BodyChunk bodyChunk3 = latchOnToBodyChunks[i, j];
                                float f = latchOnToBodyChunks[i, j].pos.x - newBody[0].pos.x;
                                float f2 = latchOnToBodyChunks[i, j].pos.y - newBody[0].pos.y;
                                float num8 = (1f - Mathf.InverseLerp(0f, newBody[0].rad * 3f, Mathf.Abs(f))) * 1.8f;
                                bodyChunk3.vel.x = bodyChunk3.vel.x + Mathf.Sign(f) * num8;
                                bodyChunk3.vel.y = bodyChunk3.vel.y + Mathf.Sign(f2) * num8;
                            }
                            else
                            {
                                timeAbove = Mathf.Max(timeAbove - 1, 0);
                            }
                            anyTentaclePulled = true;
                            Vector2 normalized2 = (tentacles[i, j][tentacles[i, j].GetLength(0) - 1, 0] - latchOnToBodyChunks[i, j].pos).normalized;
                            float num9 = Vector2.Distance(tentacles[i, j][tentacles[i, j].GetLength(0) - 1, 0], latchOnToBodyChunks[i, j].pos);
                            tentacles[i, j][tentacles[i, j].GetLength(0) - 1, 0] += normalized2 * (latchOnToBodyChunks[i, j].rad * 0.5f - num9) * 0.5f;
                            tentacles[i, j][tentacles[i, j].GetLength(0) - 1, 2] += normalized2 * (latchOnToBodyChunks[i, j].rad * 0.5f - num9) * 0.5f;
                            if (!Custom.DistLess(player.firstChunk.pos, latchOnToBodyChunks[i, j].pos, (float)tentacles[i, j].GetLength(0) * num2 * 1.1f))
                            {
                                latchOnToBodyChunks[i, j] = null;
                                player.room.PlaySound(SoundID.Jelly_Fish_Tentacle_Release, tentacles[i, j][tentacles[i, j].GetLength(0) - 1, 0]);
                            }
                            else if (Random.value < 0.00045f && player.room.PointSubmerged(new Vector2(tentacles[i, j][tentacles[i, j].GetLength(0) - 1, 0].x, tentacles[i, j][tentacles[i, j].GetLength(0) - 1, 0].y + 30f)))
                            {
                                if (latchOnToBodyChunks[i, j].owner is Creature)
                                {
                                    (latchOnToBodyChunks[i, j].owner as Creature).Stun(100 * (int)Mathf.InverseLerp(50f, 10f, latchOnToBodyChunks[i, j].owner.TotalMass));
                                    if (latchOnToBodyChunks[i, j].owner is Player && (latchOnToBodyChunks[i, j].owner as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint)
                                    {
                                        player.room.AddObject(new CreatureSpasmer(latchOnToBodyChunks[i, j].owner as Creature, allowDead: true, 80));
                                        (latchOnToBodyChunks[i, j].owner as Player).SaintStagger(500);
                                    }
                                    player.room.AddObject(new ShockWave(tentacles[i, j][tentacles[i, j].GetLength(0) - 1, 0], Mathf.Lerp(40f, 60f, Random.value), 0.07f, 6));
                                    tentacles[i, j][tentacles[i, j].GetLength(0) - 1, 0] = Vector2.Lerp(tentacles[i, j][tentacles[i, j].GetLength(0) - 1, 0], player.firstChunk.pos, 0.2f);
                                }
                                player.room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, tentacles[i, j][tentacles[i, j].GetLength(0) - 1, 0]);
                                latchOnToBodyChunks[i, j] = null;
                            }
                            else if (!Custom.DistLess(player.firstChunk.pos, latchOnToBodyChunks[i, j].pos, (float)tentacles[i, j].GetLength(0) * num2 * 1.4f))
                            {
                                normalized2 = (player.firstChunk.pos - latchOnToBodyChunks[i, j].pos).normalized;
                                num9 = Vector2.Distance(player.firstChunk.pos, latchOnToBodyChunks[i, j].pos);
                                float num10 = player.firstChunk.mass / (player.firstChunk.mass + latchOnToBodyChunks[i, j].mass);
                                latchOnToBodyChunks[i, j].pos -= normalized2 * ((float)tentacles[i, j].GetLength(0) * num2 * 1.4f - num9) * num10;
                                latchOnToBodyChunks[i, j].vel -= normalized2 * ((float)tentacles[i, j].GetLength(0) * num2 * 1.4f - num9) * num10;
                                rotation = (rotation + normalized2 * Mathf.InverseLerp((float)tentacles[i, j].GetLength(0) * num2 * 0.4f, (float)tentacles[i, j].GetLength(0) * num2 * 2.4f, Vector2.Distance(player.firstChunk.pos, latchOnToBodyChunks[i, j].pos))).normalized;
                            }
                        }
                        else
                        {
                            latchOnToBodyChunks[i, j] = null;
                            player.room.PlaySound(SoundID.Jelly_Fish_Tentacle_Release, tentacles[i, j][tentacles[i, j].GetLength(0) - 1, 0]);
                        }
                    }
                    if (latchOnToBodyChunks[i, j] != null || !player.room.PointSubmerged(tentacles[i, j][tentacles[i, j].GetLength(0) - 1, 0]))
                    {
                        continue;
                    }
                    Vector2 vector9 = tentacles[i, j][tentacles[i, j].GetLength(0) - 1, 0];
                    int num11 = 0;
                    //寻找抓取的生物
                    while (latchOnToBodyChunks[i, j] == null && num11 < player.room.abstractRoom.creatures.Count)
                    {
                        if (ValidGrabCreature(player.room.abstractRoom.creatures[num11]))
                        {
                            int num12 = 0;
                            while (latchOnToBodyChunks[i, j] == null && num12 < player.room.abstractRoom.creatures[num11].realizedCreature.bodyChunks.Length)
                            {
                                if (Custom.DistLess(player.room.abstractRoom.creatures[num11].realizedCreature.bodyChunks[num12].pos,
                                    vector9,
                                    player.room.abstractRoom.creatures[num11].realizedCreature.bodyChunks[num12].rad * 1.15f))
                                {
                                    latchOnToBodyChunks[i, j] = player.room.abstractRoom.creatures[num11].realizedCreature.bodyChunks[num12];
                                    //player.roomPlaySound((!(player.roomabstractplayer.roomcreatures[num11].realizedCreature is Player)) ? SoundID.Jelly_Fish_Tentacle_Latch_On_NPC : SoundID.Jelly_Fish_Tentacle_Latch_On_Player, vector9);
                                    PlayHorrifyingMoo();
                                }
                                num12++;
                            }
                        }
                        num11++;
                    }
                }
            }
        }

        #region 拿东西
        //拿东西的手的位置
        public void SlugcatHandUpdate(SlugcatHand self)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;

            Vector2 headPos = (self.owner as PlayerGraphics).head.pos;
            Vector2 headLastPos = (self.owner as PlayerGraphics).head.lastPos;
            Vector2 pos = headPos;
            Vector2 lastPos = headLastPos;

            pos = pos + 7f * Vector2.down;
            lastPos = lastPos + 7f * Vector2.down;
            self.mode = Limb.Mode.HuntAbsolutePosition;
            self.lastPos = lastPos;
            self.pos = pos;
            self.absoluteHuntPos = pos;
            (self.owner.owner as Player).craftingObject = true;

            for (int i = 0; i < 2; i++)
            {
                if ((self.owner.owner as Player).grasps[i] != null)
                {
                    PhysicalObject obj = (self.owner.owner as Player).grasps[i].grabbed as PhysicalObject;

                    IDrawable drawable = obj is IDrawable ? obj as IDrawable : obj.graphicsModule;

                    foreach (var sLeaser in self.owner.owner.room.game.cameras[0].spriteLeasers)
                    {
                        if (sLeaser.drawableObject == drawable)
                        {
                            var midgroundContainer = self.owner.owner.room.game.cameras[0].ReturnFContainer("Midground");
                            self.owner.owner.room.game.cameras[0].MoveObjectToContainer(drawable, midgroundContainer);
                        }
                    }
                }
            }
        }

        //只能一次叼一个东西
        public Player.ObjectGrabability Grabability(Player.ObjectGrabability result)
        {
            if (!ownerRef.TryGetTarget(out var self))
                return result;

            if (result == Player.ObjectGrabability.OneHand)
                result = Player.ObjectGrabability.BigOneHand;
            else if (result == Player.ObjectGrabability.BigOneHand)
                result = Player.ObjectGrabability.BigOneHand;
            else if (result == Player.ObjectGrabability.TwoHands)
                result = Player.ObjectGrabability.CantGrab;
            else if (result == Player.ObjectGrabability.Drag)
                result = Player.ObjectGrabability.CantGrab;

            return result;
        }
        #endregion

        #region 确定位置
        public Vector2 AttachPos(int rag, float timeStacker)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return Vector2.zero;
            float num = Mathf.Lerp(maxTGap, minTGap, hoodSwayingPulse);
            return Vector2.Lerp(Vector2.Lerp(newBody[leftHoodChunk].lastPos, newBody[leftHoodChunk].pos, timeStacker), 
                                Vector2.Lerp(newBody[rightHoodChunk].lastPos, newBody[rightHoodChunk].pos, timeStacker), 
                                0.5f) + 
                                new Vector2(Mathf.Sin(rag) * (float)rag * num, 0f);
        }

        private void newHuntPos(Vector2 pos)
        {
            huntPos = pos;
            huntingCounter = Random.Range(60, 100);
        }

        public Vector2 MouthLeftPos(float timestacker)
        {
            float num = (float)tentacles.Length * Mathf.Lerp(maxTGap, minTGap, hoodSwayingPulse);
            return AttachPos(0, timestacker) + new Vector2(0f - num, 0f);
        }

        public Vector2 MouthRightPos(float timestacker)
        {
            float x = (float)tentacles.Length * Mathf.Lerp(maxTGap, minTGap, hoodSwayingPulse);
            return AttachPos(0, timestacker) + new Vector2(x, 0f);
        }

        public void newCuriousHuntPos(Vector2 pos)
        {
            huntPos = pos;
            huntingCounter = Random.Range(10, 90);
        }
        #endregion

        public void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (otherObject == player && ((myChunk == 0 && (otherChunk == 4 || otherChunk == 5 || otherChunk == 6)) || (otherChunk == 0 && (myChunk == 4 || myChunk == 5 || myChunk == 6))))
            {
                return;
            }
            player.Collide(otherObject, myChunk, otherChunk);
            if ((myChunk != CoreChunk && (myChunk != 0 || !(otherObject.Submersion > 0.8f) || !(otherObject.firstChunk.pos.y < player.firstChunk.pos.y - 10f))) || !(otherObject is Creature) || consumedCreatures.Contains(otherObject as Creature) || !(otherObject.TotalMass < player.TotalMass))
            {
                return;
            }
            if (!(otherObject as Creature).dead)
            {
                player.room.AddObject(new UnderwaterShock(player.room, player, otherObject.bodyChunks[otherChunk].pos, 14, 80f, 10f, player, new Color(0.7f, 0.7f, 1f)));
                for (int i = 0; i < 4; i++)
                {
                    player.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Volt_Shock, otherObject.bodyChunks[otherChunk].pos, 1f, Random.value * 0.5f + 0.4f);
                }
                player.room.PlaySound(SoundID.Zapper_Zap, otherObject.bodyChunks[otherChunk].pos, 1f, 2f);
                (otherObject as Creature).Die();
            }
            consumedCreatures.Add(otherObject as Creature);
        }

        private bool ValidGrabCreature(AbstractCreature abs)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return false;
            if (abs.creatureTemplate.type != MoreSlugcatsEnums.CreatureTemplateType.BigJelly && 
                abs.creatureTemplate.type != CreatureTemplate.Type.Leech && 
                abs.creatureTemplate.type != CreatureTemplate.Type.SeaLeech && 
                abs.creatureTemplate.type != CreatureTemplate.Type.BigEel && 
                abs.creatureTemplate.type != MoreSlugcatsEnums.CreatureTemplateType.MirosVulture && 
                abs.realizedCreature != null && 
                abs.realizedCreature != player && 
                !consumedCreatures.Contains(abs.realizedCreature) && 
                abs.realizedCreature.room == player.room)
            {
                return !abs.realizedCreature.enteringShortCut.HasValue;
            }
            return false;
        }

        private void ConsumeCreateUpdate()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            Vector2 vector = player.firstChunk.pos + new Vector2(0f, -60f);
            for (int i = 0; i < consumedCreatures.Count; i++)
            {
                float num = Vector2.Distance(consumedCreatures[i].firstChunk.pos, vector);
                float num2 = Mathf.Sign(consumedCreatures[i].firstChunk.pos.x - vector.x);
                Vector2 vector2 = Custom.DirVec(consumedCreatures[i].firstChunk.pos, vector + new Vector2(num2 * 15f, 0f)) * 1.3f;
                vector2.y /= 10f;
                vector2.x *= Mathf.InverseLerp(10f, 60f, num);
                consumedCreatures[i].firstChunk.vel = (Custom.DirVec(consumedCreatures[i].firstChunk.pos, vector) + vector2) / 3f;
                if (num > 100f || consumedCreatures[i].slatedForDeletetion || consumedCreatures[i].room != player.room)
                {
                    consumedCreatures.RemoveAt(i);
                    break;
                }
                if (num < 9f)
                {
                    player.SessionRecord.AddEat(consumedCreatures[i]);
                    player.AddFood(consumedCreatures[i].State.meatLeft);
                    consumedCreatures[i].Destroy();
                    consumedCreatures.RemoveAt(i);
                    break;
                }
            }
        }

        private void LightUpdate()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            LightCounter += Random.Range(0.01f, 0.2f);
            if (myLight != null && (myLight.room != player.room || !myLight.room.BeingViewed))
            {
                myLight.slatedForDeletetion = true;
                myLight = null;
            }
            if (myLight == null && player.room.BeingViewed)
            {
                LightCounter = Random.Range(0f, 100f);
                myLight = new LightSource(player.firstChunk.pos, environmentalLight: true, coreColor, player);
                player.room.AddObject(myLight);
                myLight.colorFromEnvironment = false;
                myLight.flat = true;
                myLight.noGameplayImpact = true;
                myLight.stayAlive = true;
                myLight.requireUpKeep = true;
            }
            else if (myLight != null)
            {
                myLight.HardSetPos(newBody[CoreChunk].pos);
                myLight.HardSetRad(180f);
                myLight.HardSetAlpha(Mathf.Lerp(0f, 0.765f, (0.5f + (1f - hoodSwayingPulse) / 2f) * player.room.Darkness(myLight.Pos)));
                myLight.stayAlive = true;
            }
        }

        public void Violence(BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, Creature.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (source != null && source.owner is Spear && (source.owner as Spear).Spear_NeedleCanFeed() && SMSuckCounter == 0)
            {
                player.room.PlaySound(SoundID.Daddy_Digestion_Init, player.firstChunk);
                SMSuckCounter = 1;
                if ((source.owner as Spear).thrownBy != null && (source.owner as Spear).thrownBy is Player)
                {
                    ((source.owner as Spear).thrownBy as Player).AddFood(3);
                }
            }
            if (type == Creature.DamageType.Explosion && damage >= 1f)
            {
                player.Die();
            }
            else
            {
                player.Violence(source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
            }
        }

        private void PlayHorrifyingMoo()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (mooCounter < 0)
            {
                BuffPlugin.Log("Moo!");
                mooCounter = 140;
                player.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Terror_Moo, newBody[CoreChunk].pos, 1f, 0.75f + Random.value * 0.5f);
                player.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Terror_Moo, newBody[CoreChunk].pos, 1f, 0.75f + Random.value * 0.5f);
                for (int num = Random.Range(16, 36); num > 0; num--)
                {
                    player.room.AddObject(new Bubble(newBody[CoreChunk].pos + new Vector2(Random.Range(-28f, 28f), Random.Range(-38f, 20f)), new Vector2(Random.Range(-4f, 4f), 0f), bottomBubble: false, fakeWaterBubble: false));
                }
            }
        }

        private static TriangleMesh MakeLongMesh(int segments, bool pointyTip, bool customColor, string texture, bool atlasedImage)
        {
            TriangleMesh.Triangle[] array = new TriangleMesh.Triangle[(segments - 1) * 4 + (pointyTip ? 1 : 2)];
            for (int i = 0; i < segments - 1; i++)
            {
                int num = i * 4;
                for (int j = 0; j < 4; j++)
                {
                    array[num + j] = new TriangleMesh.Triangle(num + j, num + j + 1, num + j + 2);
                }
            }
            array[(segments - 1) * 4] = new TriangleMesh.Triangle((segments - 1) * 4, (segments - 1) * 4 + 1, (segments - 1) * 4 + 2);
            if (!pointyTip)
            {
                array[(segments - 1) * 4 + 1] = new TriangleMesh.Triangle((segments - 1) * 4 + 1, (segments - 1) * 4 + 2, (segments - 1) * 4 + 3);
            }
            TriangleMesh triangleMesh = new TriangleMesh(texture, array, customColor, atlasedImage);
            float num2 = 1f / (float)((segments - 1) * 2 + 1);
            for (int k = 0; k < triangleMesh.UVvertices.Length; k++)
            {
                triangleMesh.UVvertices[k].x = ((k % 2 == 0) ? 0f : 1f);
                triangleMesh.UVvertices[k].y = (float)(k / 2) * num2;
            }
            if (pointyTip)
            {
                triangleMesh.UVvertices[triangleMesh.UVvertices.Length - 1].x = 0.5f;
            }
            return triangleMesh;
        }
    }

    internal class JellyfishCatState : HealthState
    {
        public JellyfishCatState(AbstractCreature creature) : base(creature)
        {
            this.HomePos = Vector2.zero;
            this.DriftPos = Vector2.zero;
            this.bodyReleasedGoo = false;
        }

        public Vector2 HomePos;
        public Vector2 DriftPos;
        public Vector2[,] deadArmDriftPos;
        public bool bodyReleasedGoo;
    }
}
