using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuff.Core.Buff;
using UnityEngine;
using System.Reflection;
using System;
using MonoMod.Cil;
using System.Linq;
using System.Runtime.CompilerServices;
using RWCustom;
using Random = UnityEngine.Random;
using System.Collections.Generic;

namespace BuiltinBuffs.Duality
{
    internal class BirdShapedMutationBuff : Buff<BirdShapedMutationBuff, BirdShapedMutationBuffData>
    {
        public override bool Triggerable => true;

        public override BuffID ID => BirdShapedMutationBuffEntry.BirdShapedMutation;

        public BirdShapedMutationBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    var fly = new Fly(player);
                    BirdShapedMutationBuffEntry.FlyFeatures.Add(player, fly);
                    fly.BirdWing(player.graphicsModule as PlayerGraphics);
                    fly.InitiateSprites(game.cameras[0].spriteLeasers.
                        First(i => i.drawableObject == player.graphicsModule), game.cameras[0]);
                }
            }
        }


        public override bool Trigger(RainWorldGame game)
        {
            foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
            {
                if (BirdShapedMutationBuffEntry.FlyFeatures.TryGetValue(player, out var fly))
                {
                    if (fly.isFlying)
                        fly.StopFlight();
                    else if (fly.CanSustainFlight(player))
                        fly.InitiateFlight(player);
                }
            }
            return false;
        }
    }

    internal class BirdShapedMutationBuffData : BuffData
    {
        public override BuffID ID => BirdShapedMutationBuffEntry.BirdShapedMutation;
    }

    internal class BirdShapedMutationBuffEntry : IBuffEntry
    {
        public static BuffID BirdShapedMutation = new BuffID("BirdShapedMutation", true);

        public static ConditionalWeakTable<Player, Fly> FlyFeatures = new ConditionalWeakTable<Player, Fly>();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<BirdShapedMutationBuff, BirdShapedMutationBuffData, BirdShapedMutationBuffEntry>(BirdShapedMutation);
        }

        public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.Player.MovementUpdate += Player_MovementUpdate;
            On.Player.Grabability += Player_Grabability;
            On.Player.FreeHand += Player_FreeHand;
            On.SlugcatHand.Update += SlugcatHand_Update;

            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.Reset += PlayerGraphics_Reset;
            On.PlayerGraphics.ctor += PlayerGraphics_ctor;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!FlyFeatures.TryGetValue(self, out _))
                FlyFeatures.Add(self, new Fly(self));
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (FlyFeatures.TryGetValue(self, out var fly))
            {
                fly.Update();
                self.GetExPlayerData().HaveHands = false;
            }
        }

        private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            if (FlyFeatures.TryGetValue(self, out var fly))
                fly.MovementUpdate(orig, eu);
            orig(self, eu);
        }

        //双手位置
        private static void SlugcatHand_Update(On.SlugcatHand.orig_Update orig, SlugcatHand self)
        {
            if (FlyFeatures.TryGetValue(self.owner.owner as Player, out var fly))
                (self.owner.owner as Player).craftingObject = true;
            orig(self);

            if (FlyFeatures.TryGetValue(self.owner.owner as Player, out fly))
                fly.SlugcatHandUpdate(self);
        }

        //只能一次叼一个东西
        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            Player.ObjectGrabability result = orig(self, obj);

            if (FlyFeatures.TryGetValue(self, out var fly))
            {
                result = fly.Grabability(result);
            }

            return result;
        }

        private static int Player_FreeHand(On.Player.orig_FreeHand orig, Player self)
        {
            int result = orig(self);
            if (FlyFeatures.TryGetValue(self, out var fly))
                if (self.grasps[0] != null || self.grasps[0] != null)
                {
                    result = -1;
                }
            return result;
        }
        #region 外观
        private static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (FlyFeatures.TryGetValue(self.player, out var fly))
                fly.ApplyPalette(sLeaser, rCam, palette);
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (FlyFeatures.TryGetValue(self.player, out var fly))
                fly.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (FlyFeatures.TryGetValue(self.player, out var fly))
            {
                fly.InitiateSprites(sLeaser, rCam);
            }
        }

        private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (FlyFeatures.TryGetValue(self.player, out var fly))
                fly.AddToContainer(sLeaser, rCam, newContatiner);
        }

        private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if (FlyFeatures.TryGetValue(self.player, out var fly))
                fly.BirdWing(self);
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            if (FlyFeatures.TryGetValue(self.player, out var fly))
                fly.GraphicsUpdate();
        }

        private static void PlayerGraphics_Reset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig(self);
            if (FlyFeatures.TryGetValue(self.player, out var fly))
                fly.Reset(self);
        }
        #endregion
    }

    internal class Fly
    {
        WeakReference<Player> ownerRef;

        #region 飞行相关
        //飞行相关属性
        float normalGravity = 0.9f;
        float normalAirFriction = 0.999f;
        float flightAirFriction = 0.7f;

        public bool isFlying;
        float flutterTimeAdd;
        float wingSpeed;
        float upFlightTime;
        int preventGrabs;
        int flightTime;
        int preventFlight;

        //指定位置
        Vector2 wantPos;
        bool wantPosIsSetX;
        bool wantPosIsSetY;
        //姿势
        bool spreadWings;
        bool foldUpWings;

        public void StopFlight()
        {
            flightTime = -1;
            isFlying = false;
        }

        public void InitiateFlight(Player self)
        {
            if (self.input[0].y < 0)
            {
                return;
            }
            self.bodyMode = Player.BodyModeIndex.Default;
            self.animation = Player.AnimationIndex.None;
            self.wantToJump = 0;
            flightTime = 0;
            isFlying = true;
            wantPos = self.bodyChunks[0].pos;
        }

        public bool CanSustainFlight(Player self)
        {
            return preventFlight == 0
                && self.canJump <= 0
                && self.Consious && !self.Stunned
                && self.bodyMode != Player.BodyModeIndex.Crawl
                && self.bodyMode != Player.BodyModeIndex.CorridorClimb
                && self.bodyMode != Player.BodyModeIndex.ClimbIntoShortCut
                && self.bodyMode != Player.BodyModeIndex.WallClimb
                && self.bodyMode != Player.BodyModeIndex.Swimming
                && self.animation != Player.AnimationIndex.HangFromBeam
                && self.animation != Player.AnimationIndex.ClimbOnBeam
                && self.animation != Player.AnimationIndex.AntlerClimb
                && self.animation != Player.AnimationIndex.VineGrab
                && self.animation != Player.AnimationIndex.ZeroGPoleGrab
                && self.animation != Player.AnimationIndex.HangUnderVerticalBeam;
        }
        #endregion

        #region 翅膀相关
        //翅膀长度及宽度
        float wingLength;
        float wingWidth;

        //图像
        int wingSprite;
        GenericBodyPart[] wing;
        public FAtlas wingAtlas;

        //飞行时，内层翅膀最大/最小旋转角度
        float innerRotationMin = -70f;
        float innerRotationMax = 50f;
        //飞行时，外层翅膀最大/最小旋转角度
        float outerRotationMin = -70f;
        float outerRotationMax = 50f;

        //内层翅膀和外层翅膀骨架
        Vector2 innerWing;
        Vector2 outerWing;

        //内层翅膀旋转角度
        float innerRotation;
        //外层翅膀旋转角度
        float outerRotation;
        //身体至臀部方向的向量
        Vector2 dif;
        //身体旋转角度
        float bodyRotation;
        //振翅时翅膀旋转角度
        float flutterRotation;
        //收翅时翅膀旋转角度
        float foldScale;
        
        public int WingSprite(int side, int wing)
        {
            return wingSprite + side + wing + wing;
        }

        public void BirdWing(PlayerGraphics self)
        {
            wing = new GenericBodyPart[36];
            for (int i = 0; i < wing.Length; i++)
            {
                wing[i] = new GenericBodyPart(self, 1f, 0.5f, 0.9f, self.player.bodyChunks[0]);
            }

        }
        #endregion

        public Fly(Player player)
        {
            ownerRef = new WeakReference<Player>(player);

            wingSpeed = 10f;
            upFlightTime = 30;

            if (player.playerState.isPup)
            {
                wingLength = 10f;
                wingWidth = 14f;
            }
            else
            {
                wingLength = 15f;
                wingWidth = 20f;
            }
        }

        #region 外观
        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            wingSprite = sLeaser.sprites.Length;
            Array.Resize(ref sLeaser.sprites, wingSprite + 6);
            wingAtlas = Futile.atlasManager.LoadAtlas("buffassets/cardinfos/duality/birdshapedmutation/birdwings");

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    //内侧翅膀
                    if (j != 1)
                    {
                        TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
                        {
                            new TriangleMesh.Triangle(0, 1, 5),
                            new TriangleMesh.Triangle(1, 2, 3),
                            new TriangleMesh.Triangle(1, 3, 4),
                            new TriangleMesh.Triangle(1, 4, 5)
                        };
                        TriangleMesh triangleMesh = new TriangleMesh("BirdWing" + "A" + j, tris, true, true);

                        triangleMesh.UVvertices[0] = wingAtlas._elementsByName["BirdWing" + "A" + j].uvTopLeft;
                        triangleMesh.UVvertices[2] = wingAtlas._elementsByName["BirdWing" + "A" + j].uvTopRight;
                        triangleMesh.UVvertices[3] = wingAtlas._elementsByName["BirdWing" + "A" + j].uvBottomRight;
                        triangleMesh.UVvertices[5] = wingAtlas._elementsByName["BirdWing" + "A" + j].uvBottomLeft;
                        triangleMesh.UVvertices[1] = Vector2.Lerp(triangleMesh.UVvertices[0], triangleMesh.UVvertices[2], 0.5f);
                        triangleMesh.UVvertices[4] = Vector2.Lerp(triangleMesh.UVvertices[3], triangleMesh.UVvertices[5], 0.5f);

                        sLeaser.sprites[WingSprite(i, j)] = triangleMesh;

                        if (j == 2)
                        {
                            triangleMesh._alpha = 0.8f;
                        }
                    }
                    //外侧翅膀
                    else
                    {
                        TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
                        {
                            new TriangleMesh.Triangle(0, 1, 2),
                            new TriangleMesh.Triangle(0, 2, 3),
                            new TriangleMesh.Triangle(0, 3, 4),
                            new TriangleMesh.Triangle(0, 4, 5)
                        };
                        TriangleMesh triangleMesh = new TriangleMesh("BirdWing" + "A" + j, tris, true, true);

                        triangleMesh.UVvertices[0] = wingAtlas._elementsByName["BirdWing" + "A" + j].uvTopLeft;
                        triangleMesh.UVvertices[2] = wingAtlas._elementsByName["BirdWing" + "A" + j].uvTopRight;
                        triangleMesh.UVvertices[3] = wingAtlas._elementsByName["BirdWing" + "A" + j].uvBottomRight;
                        triangleMesh.UVvertices[5] = wingAtlas._elementsByName["BirdWing" + "A" + j].uvBottomLeft;
                        triangleMesh.UVvertices[1] = Vector2.Lerp(triangleMesh.UVvertices[0], triangleMesh.UVvertices[2], 0.5f);
                        triangleMesh.UVvertices[4] = Vector2.Lerp(triangleMesh.UVvertices[3], triangleMesh.UVvertices[5], 0.5f);

                        triangleMesh.alpha = 0.8f;

                        sLeaser.sprites[WingSprite(i, j)] = triangleMesh;
                    }
                }
            }

            self.AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (!ownerRef.TryGetTarget(out var player) || player.graphicsModule == null || sLeaser == null)
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;

            if (sLeaser.sprites.Length >= 9)
                for (int i = 5; i <= 8; i++)
                    sLeaser.sprites[i].isVisible = false;

            Vector2 drawPos1;

            //身体位置
            Vector2 bodyPos = Vector2.Lerp(self.player.bodyChunks[0].lastPos, self.player.bodyChunks[0].pos, timeStacker);
            //头部位置
            Vector2 headPos = Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker);

            if (isFlying || spreadWings)
            {
                drawPos1 = bodyPos;
            }
            else
            {
                drawPos1 = Vector2.Lerp(headPos, bodyPos, 0.3f);
            }
            //臀部位置
            Vector2 drawPos2 = Vector2.Lerp(self.player.bodyChunks[1].lastPos, self.player.bodyChunks[1].pos, timeStacker);
            //身体至臀部方向的向量
            dif = wingWidth * (drawPos1 - drawPos2).normalized;
            //身体旋转角度
            bodyRotation = Mathf.Atan2(dif.x, dif.y);

            //抖动修正
            Vector2 shake = sLeaser.sprites[0].GetPosition() - bodyPos + camPos;

            //设置图层
            if (isFlying)
            {
                WingLevel(sLeaser, bodyRotation);
            }
            else
            {
                if (Mathf.Abs(bodyRotation) < 105f / 180f * 3.1415926f / 2.5f)
                {
                    WingLevel(sLeaser, -bodyRotation * 2.5f);
                }
                else
                {
                    WingLevel(sLeaser, bodyRotation * 2.5f);
                }
                sLeaser.sprites[WingSprite(0, 2)].isVisible = false;
                sLeaser.sprites[WingSprite(1, 2)].isVisible = false;
            }

            //i = 0为右侧翅膀， i = 1 为左侧翅膀
            for (int i = 0; i < 2; i++)
            {
                //设置位置
                for (int j = 0; j < 3; j++)
                {
                    var wing = sLeaser.sprites[WingSprite(i, j)] as TriangleMesh;
                    for (int k = 0; k < 6; k++)
                    {
                        wing.MoveVertice(k, shake + Vector2.Lerp(this.wing[i * 18 + j * 6 + k].lastPos, this.wing[i * 18 + j * 6 + k].pos, timeStacker) - camPos);
                    }
                }
                //飞行时抬一下腿
                if (isFlying)
                {
                    sLeaser.sprites[4].SetPosition(sLeaser.sprites[4].GetPosition().x, sLeaser.sprites[4].GetPosition().y + 6f * Mathf.Abs(Mathf.Sin(bodyRotation)));
                }
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            //i = 0为右侧翅膀， i = 1 为左侧翅膀
            for (int i = 0; i < 2; i++)
            {
                //内层翅膀颜色
                sLeaser.sprites[WingSprite(i, 0)].color = sLeaser.sprites[0].color;
                sLeaser.sprites[WingSprite(i, 2)].color = Color.Lerp(sLeaser.sprites[0].color, Color.white, 0.5f);
                //外层翅膀颜色
                float h; float s; float v;
                Color.RGBToHSV(sLeaser.sprites[0].color, out h, out s, out v);
                if (s == 0f || v == 0f)
                    h = 0.54f;
                else
                    h += 0.15f;
                s = 1f;
                v = 1f;
                sLeaser.sprites[WingSprite(i, 1)].color = Color.HSVToRGB(h, s, v);
            }
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            if (wingSprite >= 1 && sLeaser.sprites.Length >= wingSprite + 6)
            {
                var foregroundContainer = rCam.ReturnFContainer("Foreground");
                var midgroundContainer = rCam.ReturnFContainer("Midground");

                //让翅膀移到中景
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        var sprite = sLeaser.sprites[WingSprite(i, j)];
                        foregroundContainer.RemoveChild(sprite);
                        midgroundContainer.AddChild(sprite);
                    }
                }
            }
        }

        public void GraphicsUpdate()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            Vector2 drawPos1;
            //身体位置
            Vector2 bodyPos = self.player.bodyChunks[0].lastPos;
            //头部位置
            Vector2 headPos = self.head.lastPos;

            if (isFlying || spreadWings)
            {
                drawPos1 = bodyPos;
            }
            else
            {
                drawPos1 = Vector2.Lerp(headPos, bodyPos, 0.3f);
            }
            //臀部位置
            Vector2 drawPos2 = self.player.bodyChunks[1].lastPos;  //身体至臀部方向的向量
            dif = wingWidth * (drawPos1 - drawPos2).normalized;
            //身体旋转角度
            bodyRotation = Mathf.Atan2(dif.x, dif.y);

            //飞行展开翅膀
            if (isFlying)
            {
                if (flightTime == 0)
                {
                    flutterTimeAdd = 0f;
                    //飞行时，内层翅膀最大/最小旋转角度
                    innerRotationMin = -70f;
                    innerRotationMax = 50f;
                    //飞行时，外层翅膀最大/最小旋转角度
                    outerRotationMin = -70f;
                    outerRotationMax = 50f;
                }
                //无重力时扑腾翅膀
                else if (self.player.Consious && !self.player.Stunned)//self.player.room.gravity <= 0.5 && 
                {
                    flutterTimeAdd += 1f;
                    if (flutterTimeAdd >= upFlightTime)
                    {
                        flutterTimeAdd = 0f;
                    }
                }
                else if (flutterTimeAdd <= upFlightTime)
                {
                    flutterTimeAdd += 1f;
                }
            }
            //不是飞行状态
            else
            {
                //站在杆子上时展开翅膀
                if ((self.player.animation == Player.AnimationIndex.HangFromBeam || self.player.animation == Player.AnimationIndex.StandOnBeam) && self.player.input[0].x != 0 && self.player.Consious && !self.player.Stunned)
                {
                    flutterTimeAdd += 1f;
                    flightTime += 1;
                    if (flutterTimeAdd >= 2 * upFlightTime)
                    {
                        flutterTimeAdd = upFlightTime;
                    }
                    if (flightTime >= upFlightTime)
                    {
                        //不飞行时，内层翅膀最大/最小旋转角度
                        innerRotationMin = 0f;
                        innerRotationMax = 20f;
                        //不飞行时，外层翅膀最大/最小旋转角度
                        outerRotationMin = 0f;
                        outerRotationMax = 20f;
                    }
                    else
                    {
                        //飞行时，内层翅膀最大/最小旋转角度
                        innerRotationMin = -70f;
                        innerRotationMax = 50f;
                        //飞行时，外层翅膀最大/最小旋转角度
                        outerRotationMin = -70f;
                        outerRotationMax = 50f;
                    }
                    spreadWings = true;
                    foldUpWings = false;
                }
                //飞行结束后收起翅膀
                else if (flutterTimeAdd > 0f)// && self.canJump > 0
                {
                    if (flutterTimeAdd > upFlightTime)
                    {
                        flutterTimeAdd = upFlightTime;
                    }
                    flutterTimeAdd -= 1f;
                    spreadWings = false;
                    foldUpWings = true;
                }
                else
                {
                    flightTime = 0;
                    flutterTimeAdd = 0f;
                    spreadWings = false;
                    foldUpWings = false;
                }
            }

            //飞行动画计算
            if (!isFlying && !spreadWings)
            {
                outerRotation = Mathf.Lerp(outerRotation, outerRotation + bodyRotation / 3, Mathf.Abs((1 - Mathf.Cos(bodyRotation))));
            }
            for (int i = 0; i < 2; i++)
            {
                WingPos(self.player, dif, bodyRotation, innerRotation, outerRotation, i);
            }
        }

        //设置骨架
        public void WingAnimation(Vector2 dif, float bodyRotation, float innerRotation, float outerRotation, int i)
        {
            if (!isFlying && !spreadWings)
            {
                outerRotation = Mathf.Lerp(outerRotation, outerRotation + bodyRotation / 3, Mathf.Abs((1 - Mathf.Cos(bodyRotation))));
            }

            //翅膀骨架向量
            innerWing = 0.8f * wingLength * new Vector2(Mathf.Cos(innerRotation - bodyRotation), Mathf.Sin(innerRotation - bodyRotation));
            outerWing = 1.0f * wingLength * new Vector2(Mathf.Cos(outerRotation - bodyRotation), Mathf.Sin(outerRotation - bodyRotation));

            //左和右决定是否镜像
            innerWing = i == 0 ? innerWing : 2 * Vector2.Dot(innerWing, (dif).normalized) * (dif).normalized - innerWing;
            outerWing = i == 0 ? outerWing : 2 * Vector2.Dot(outerWing, (dif).normalized) * (dif).normalized - outerWing;

            //细节修正
            //侧身飞行
            innerRotation = Mathf.Atan2(innerWing.x, innerWing.y);
            float drift = Mathf.Lerp(0, innerRotation, 2 * Mathf.Abs(Mathf.Sin(bodyRotation)));
            float driftScale = Mathf.Lerp(1, 0, Mathf.Cos(drift));
            //胡乱透视(翅膀镜像)
            innerWing = Vector2.Lerp(innerWing, 2 * Vector2.Dot(innerWing, (dif).normalized) * (dif).normalized - innerWing, driftScale);
            outerWing = Vector2.Lerp(outerWing, 2 * Vector2.Dot(outerWing, (dif).normalized) * (dif).normalized - outerWing, 0.5f * driftScale);
            //旋转
            innerWing = VectorRotation(innerWing, -bodyRotation / 2);
            outerWing = VectorRotation(outerWing, -bodyRotation / 2);
            //减少内外层翅膀的角度差距
            if (isFlying)
            {
                outerWing = Vector2.Lerp(outerWing, innerWing, Mathf.Abs(Mathf.Sin(bodyRotation)));
            }

            //俯冲
            if (Mathf.Abs(bodyRotation) > 100f / 180f * 3.1415926f)
            {
                //把翅膀镜像回去
                float scale = Mathf.InverseLerp(100f / 180f * 3.1415926f, 110f / 180f * 3.1415926f, Mathf.Abs(bodyRotation));
                innerWing = i == 0 ? innerWing : Vector2.Lerp(innerWing, 2 * Vector2.Dot(innerWing, (dif).normalized) * (dif).normalized - innerWing, scale);
                outerWing = i == 0 ? outerWing : Vector2.Lerp(outerWing, 2 * Vector2.Dot(outerWing, (dif).normalized) * (dif).normalized - outerWing, scale);
                //调整翅膀大小
                innerWing = Vector2.Lerp(0.5f * innerWing, innerWing, Mathf.Abs(Mathf.Sin(bodyRotation * 2 - 3.1415926f / 2)));
                outerWing = Vector2.Lerp(0.5f * outerWing, outerWing, Mathf.Abs(Mathf.Sin(bodyRotation * 2 - 3.1415926f / 2)));
            }

            //Debug.Log("inner: " + innerWing);
            //Debug.Log("outer: " + outerWing);
        }

        //设置翅膀位置
        public void WingPos(Player self, Vector2 dif, float bodyRotation, float innerRotation, float outerRotation, int i)
        {
            for (int j = 0; j < 18; j++)
            {
                wing[i * 18 + j].lastPos = wing[i * 18 + j].pos;
                wing[i * 18 + j].Update();
            }

            //垂直修正
            float wingH = Mathf.Lerp(0f, 3f, Mathf.Abs(Mathf.Sin(bodyRotation)));

            //幼崽翅膀下移一些
            if (self.playerState.isPup)
            {
                wingH -= 4f;
            }

            //水平修正
            float wingW;
            if (self.input[0].x > 0)
            {
                wingW = Mathf.Lerp(0f, 5f, Mathf.Abs(Mathf.Sin(bodyRotation * 2f)));
            }
            else if (self.input[0].x < 0)
            {
                wingW = -Mathf.Lerp(0f, 5f, Mathf.Abs(Mathf.Sin(bodyRotation * 2f)));
            }
            else
            {
                wingW = 0;
            }

            //飞行时
            if (isFlying)
            {
                //时间插值
                float t = flutterTimeAdd / upFlightTime * 3.1415927f;
                //内层翅膀旋转角度
                flutterRotation = Mathf.Cos(t + 0.5f);
                flutterRotation = Mathf.Abs(flutterRotation);
                innerRotation = 3.1415927f / 180f * Mathf.Lerp(innerRotationMin, innerRotationMax, flutterRotation);
                //外层翅膀旋转角度（滞后于内层翅膀）
                flutterRotation = Mathf.Cos(t + 0.2f);
                flutterRotation = Mathf.Abs(flutterRotation);
                outerRotation = 3.1415927f / 180f * Mathf.Lerp(outerRotationMin, outerRotationMax, flutterRotation);
                //设置骨架
                WingAnimation(dif, bodyRotation, innerRotation, outerRotation, i);

                //设置位置
                for (int j = 0; j < 3; j++)
                {
                    int startNum = i * 18 + j * 6;
                    //内层翅膀
                    if (j != 1)
                    {
                        wing[startNum + 0].pos = self.bodyChunks[0].pos + wingH * Vector2.up;
                        wing[startNum + 5].pos = wing[startNum + 0].pos - dif + wingH * Vector2.up;
                        wing[startNum + 1].pos = wing[startNum + 0].pos + 1.5f * innerWing + Custom.RNV() * Random.value * 1f;
                        wing[startNum + 2].pos = wing[startNum + 1].pos + 0.5f * outerWing + Custom.RNV() * Random.value * 1f;
                        wing[startNum + 4].pos = wing[startNum + 5].pos + outerWing;
                        wing[startNum + 3].pos = wing[startNum + 4].pos + 1.5f * innerWing;
                    }
                    //外层翅膀
                    else
                    {
                        wing[startNum + 0].pos = wing[i * 18 + 0 * 6 + 1].pos;
                        wing[startNum + 5].pos = wing[i * 18 + 0 * 6 + 5].pos;
                        wing[startNum + 2].pos = wing[startNum + 0].pos + 2.0f * outerWing;
                        wing[startNum + 1].pos = Vector2.Lerp(wing[startNum + 1].pos, wing[startNum + 2].pos, 0.5f);
                        wing[startNum + 4].pos = wing[startNum + 5].pos + 1.0f * innerWing;
                        wing[startNum + 3].pos = wing[startNum + 4].pos + 2.0f * innerWing;
                    }
                }
            }
            //不飞行时
            else
            {
                float t;
                //时间插值
                if (foldUpWings || spreadWings)
                {
                    t = flutterTimeAdd / upFlightTime * 3.1415927f;
                }
                else
                {
                    t = 0;
                }
                t = upFlightTime - t;
                foldScale = (upFlightTime - flutterTimeAdd) / upFlightTime;

                //内层翅膀旋转角度
                flutterRotation = Mathf.Cos(t + 0.5f + upFlightTime);
                flutterRotation = Mathf.Abs(flutterRotation);
                innerRotation = 3.1415927f / 180f * Mathf.Lerp(Mathf.Lerp(innerRotationMin, 0, foldScale), Mathf.Lerp(innerRotationMax, innerRotationMax - 10, foldScale), flutterRotation);

                //外层翅膀旋转角度（滞后于内层翅膀）
                flutterRotation = Mathf.Cos(t + 0.2f + upFlightTime);
                flutterRotation = Mathf.Abs(flutterRotation);
                outerRotation = 3.1415927f / 180f * Mathf.Lerp(Mathf.Lerp(outerRotationMin, outerRotationMin - 20, foldScale), Mathf.Lerp(outerRotationMax, outerRotationMax - 130, foldScale), flutterRotation);

                //长度修正
                float foldLength = Mathf.Lerp(1, 2, foldScale);

                //沿身体方向位置修正
                float difScale = Mathf.Lerp(0f, 0.6f, Mathf.Abs(1 - Mathf.Cos(bodyRotation)));

                //设置骨架
                WingAnimation(dif, bodyRotation, innerRotation, outerRotation, i);

                //设置位置
                for (int j = 0; j < 3; j++)
                {
                    int startNum = i * 18 + j * 6;
                    float width = 5f * (foldLength - 1) * (i == 0 ? 1 : -1);
                    //内层翅膀
                    if (j != 1)
                    {

                        wing[startNum + 0].pos = self.bodyChunks[0].pos - wingH * Vector2.up * (self.playerState.isPup ? (0.1f - foldScale) : (1f - foldScale)) + wingW * Vector2.left * foldScale - 0.8f * difScale * dif;
                        wing[startNum + 5].pos = wing[startNum + 0].pos - dif - wingH * Vector2.up * (self.playerState.isPup ? (0.1f - foldScale) : (1f - foldScale)) + wingW * Vector2.left * foldScale - 0.2f * difScale * dif;
                        wing[startNum + 1].pos = wing[startNum + 0].pos + 1.5f / foldLength * innerWing + width * Vector2.right * 0.5f;
                        wing[startNum + 2].pos = wing[startNum + 1].pos + 0.5f * foldLength * outerWing + width * Vector2.right * 0.5f;
                        wing[startNum + 4].pos = wing[startNum + 5].pos + 1.0f / foldLength * outerWing + width * Vector2.right * 0.5f;
                        wing[startNum + 3].pos = wing[startNum + 4].pos + 1.5f / foldLength * innerWing + width * Vector2.right;
                        //爬行姿态调整
                        wing[startNum + 2].pos = Vector2.Lerp(wing[startNum + 2].pos, wing[startNum + 0].pos, Mathf.Abs((1 - Mathf.Cos(bodyRotation))) / 2);
                        wing[startNum + 3].pos = Vector2.Lerp(wing[startNum + 3].pos, wing[startNum + 2].pos, Mathf.Abs((1 - Mathf.Cos(bodyRotation))) / 2);
                    }
                    //外层翅膀
                    else
                    {
                        wing[startNum + 0].pos = wing[i * 18 + 0 * 6 + 1].pos;
                        wing[startNum + 5].pos = wing[i * 18 + 0 * 6 + 5].pos - 0.5f * (foldLength - 1) * dif;
                        wing[startNum + 2].pos = wing[startNum + 0].pos + 2.0f * outerWing + width * Vector2.right * 0.5f;
                        wing[startNum + 1].pos = Vector2.Lerp(Vector2.Lerp(wing[startNum + 1].pos, wing[startNum + 2].pos, 0.5f), wing[i * 18 + 0 * 6 + 2].pos, foldScale);
                        wing[startNum + 4].pos = wing[startNum + 5].pos + 1.0f / (3.5f * foldLength) * innerWing + width * Vector2.right * 0.3f;
                        wing[startNum + 3].pos = wing[startNum + 4].pos + 2.0f / (1.5f * foldLength) * innerWing + width * Vector2.right * 0.5f;
                        //爬行姿态调整
                        wing[startNum + 2].pos = Vector2.Lerp(wing[startNum + 2].pos, wing[startNum + 0].pos, Mathf.Abs((1 - Mathf.Cos(bodyRotation))) / 2);
                        wing[startNum + 3].pos = Vector2.Lerp(wing[startNum + 3].pos, wing[startNum + 5].pos, Mathf.Abs((1 - Mathf.Cos(bodyRotation))) / 2);
                    }
                }
            }
        }

        //设置图层
        public void WingLevel(RoomCamera.SpriteLeaser sLeaser, float bodyRotation)
        {
            //设置图层
            //俯冲
            if (bodyRotation < -1.6f || bodyRotation > 1.6f)
            {
                sLeaser.sprites[WingSprite(1, 1)].MoveInFrontOfOtherNode(sLeaser.sprites[9]);
                sLeaser.sprites[WingSprite(1, 0)].MoveInFrontOfOtherNode(sLeaser.sprites[WingSprite(1, 1)]);
                for (int i = 0; i < 2; i++)
                {
                    sLeaser.sprites[WingSprite(i, 1)].MoveInFrontOfOtherNode(sLeaser.sprites[9]);
                    sLeaser.sprites[WingSprite(i, 0)].MoveInFrontOfOtherNode(sLeaser.sprites[WingSprite(i, 1)]);
                    sLeaser.sprites[WingSprite(i, 2)].isVisible = false;
                }
            }
            //侧飞
            else if (isFlying && (bodyRotation < -0.8f || (bodyRotation < -0.3f && flutterTimeAdd <= upFlightTime)))
            {
                sLeaser.sprites[WingSprite(1, 1)].MoveInFrontOfOtherNode(sLeaser.sprites[9]);
                sLeaser.sprites[WingSprite(1, 0)].MoveInFrontOfOtherNode(sLeaser.sprites[WingSprite(1, 1)]);
                sLeaser.sprites[WingSprite(1, 2)].MoveInFrontOfOtherNode(sLeaser.sprites[WingSprite(1, 0)]);
                sLeaser.sprites[WingSprite(1, 2)].isVisible = true;
            }
            else if (isFlying && (bodyRotation > 0.8f || (bodyRotation > 0.3f && flutterTimeAdd <= upFlightTime)))
            {
                sLeaser.sprites[WingSprite(0, 1)].MoveInFrontOfOtherNode(sLeaser.sprites[9]);
                sLeaser.sprites[WingSprite(0, 0)].MoveInFrontOfOtherNode(sLeaser.sprites[WingSprite(0, 1)]);
                sLeaser.sprites[WingSprite(0, 2)].MoveInFrontOfOtherNode(sLeaser.sprites[WingSprite(0, 0)]);
                sLeaser.sprites[WingSprite(0, 2)].isVisible = true;
            }
            //侧身
            else if (!isFlying && bodyRotation < -0.3f && bodyRotation > -1f)
            {
                sLeaser.sprites[WingSprite(1, 1)].MoveInFrontOfOtherNode(sLeaser.sprites[9]);
                sLeaser.sprites[WingSprite(1, 0)].MoveInFrontOfOtherNode(sLeaser.sprites[WingSprite(1, 1)]);
                sLeaser.sprites[WingSprite(1, 2)].MoveInFrontOfOtherNode(sLeaser.sprites[WingSprite(1, 0)]);
                sLeaser.sprites[WingSprite(1, 2)].isVisible = true;
            }
            else if (!isFlying && bodyRotation > 0.3f && bodyRotation < 1f)
            {
                sLeaser.sprites[WingSprite(0, 1)].MoveInFrontOfOtherNode(sLeaser.sprites[9]);
                sLeaser.sprites[WingSprite(0, 0)].MoveInFrontOfOtherNode(sLeaser.sprites[WingSprite(0, 1)]);
                sLeaser.sprites[WingSprite(0, 2)].MoveInFrontOfOtherNode(sLeaser.sprites[WingSprite(0, 0)]);
                sLeaser.sprites[WingSprite(0, 2)].isVisible = true;
            }
            //平飞
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    //让翅膀移到身体后
                    for (int j = 0; j < 3; j++)
                    {
                        sLeaser.sprites[WingSprite(i, j)].MoveBehindOtherNode(sLeaser.sprites[0]);
                    }
                    //让外层翅膀移到内层翅膀后
                    sLeaser.sprites[WingSprite(i, 1)].MoveBehindOtherNode(sLeaser.sprites[WingSprite(i, 0)]);
                    sLeaser.sprites[WingSprite(i, 2)].isVisible = true;
                }
            }
        }

        public void Reset(PlayerGraphics self)
        {
            //防止拉丝
            for (int i = 0; i < wing.Length; i++)
            {
                wing[i].Reset((self as GraphicsModule).owner.bodyChunks[0].pos);
            }
        }

        //向量旋转（请注意，非正常旋转）
        public static Vector2 VectorRotation(Vector2 vector, float rotation)
        {
            Vector2 newVector = new Vector2(vector.x * rotation, vector.y * rotation);
            newVector.x = vector.x * Mathf.Cos(rotation) + vector.y * Mathf.Sin(rotation);
            //newVector.y = -vector.x * Mathf.Sin(rotation) + vector.y * Mathf.Cos(rotation);
            newVector.y = -newVector.x * Mathf.Sin(rotation) + vector.y * Mathf.Cos(rotation);
            return newVector;
        }
        #endregion

        //进行飞行
        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var self))
                return;
            if (!self.Consious) return;

            if (self.animation == Player.AnimationIndex.HangFromBeam || self.animation == Player.AnimationIndex.SurfaceSwim)
            {
                preventFlight = 15;
            }
            else if (self.bodyMode == Player.BodyModeIndex.WallClimb)
            {
                preventFlight = 10;//下次试试 8，或者更少
            }
            else if (preventFlight > 0)
            {
                preventFlight--;
            }

            //如果正在飞行
            if (isFlying)
            {
                flightTime++;

                self.AerobicIncrease(0.01f);

                self.gravity = 0f;
                self.airFriction = flightAirFriction;

                //飞行速度
                FlightSpeed(self);
            }
            else
            {
                self.airFriction = normalAirFriction;
                self.gravity = normalGravity;
            }

            if (preventGrabs > 0)
            {
                preventGrabs--;
            }
            /*
            for (int i = 0; i < 2; i++)
                this.SlugcatHandUpdate((self.graphicsModule as PlayerGraphics).hands[i]);

            //self.craftingObject = true;*/
        }

        //调整姿势
        public void MovementUpdate(On.Player.orig_MovementUpdate orig, bool eu)
        {
            if (!ownerRef.TryGetTarget(out var self))
                return;
            if (!self.Consious) return;
            if (isFlying)
            {
                self.bodyMode = Player.BodyModeIndex.Default;
                self.animation = Player.AnimationIndex.None;

                orig(self, eu);

                if (!CanSustainFlight(self))
                {
                    StopFlight();
                }
                else
                {
                    if (self.input[0].x != 0)
                    {
                        self.bodyMode = Player.BodyModeIndex.Default;
                        self.animation = Player.AnimationIndex.LedgeCrawl;
                    }
                    else
                    {
                        self.bodyMode = Player.BodyModeIndex.Default;
                        self.animation = Player.AnimationIndex.None;
                    }
                }
            }
            /*
            for (int i = 0; i < 2; i++)
                this.SlugcatHandUpdate((self.graphicsModule as PlayerGraphics).hands[i]);
            //self.craftingObject = true;*/
        }

        //飞行速度
        private void FlightSpeed(Player self)
        {
            wantPos += wingSpeed * new Vector2(self.input[0].x, self.input[0].y);
            if (self.input[0].x == 0 && !wantPosIsSetX)
            {
                wantPosIsSetX = true;
                self.bodyChunks[0].vel.x *= 0.3f;
                self.bodyChunks[1].vel.x *= 0.3f;
                wantPos.x = self.bodyChunks[0].pos.x;
            }
            if (self.input[0].y == 0 && !wantPosIsSetY)
            {
                wantPosIsSetY = true;
                self.bodyChunks[0].vel.y *= 0.3f;
                self.bodyChunks[1].vel.y *= 0.3f;
                wantPos.y = self.bodyChunks[0].pos.y;
            }
            if (self.input[0].x != 0)
            {
                wantPosIsSetX = false;
            }
            if (self.input[0].y != 0)
            {
                wantPosIsSetY = false;
            }

            self.bodyChunks[0].vel *= Custom.LerpMap(self.bodyChunks[0].vel.magnitude, 1f, 6f, 0.99f, 0.9f);
            self.bodyChunks[0].vel += Vector2.ClampMagnitude(wantPos - self.bodyChunks[0].pos, 100f) / 100f * 3f;
            //俯身加速
            if (self.input[0].x != 0 && self.input[0].y < 0)
                self.bodyChunks[0].vel.x *= 1.2f;
            //抵消重力
            if (self.input[0].y >= 0)
                self.bodyChunks[0].vel += 1.85f * Vector2.up;
            else
                self.bodyChunks[0].vel -= 1.05f * Vector2.up;
            //根据动画产生的速度
            if (flutterTimeAdd <= upFlightTime)
                self.bodyChunks[0].vel += 1f * Vector2.up;
            else
                self.bodyChunks[0].vel += -1f * Vector2.up;
            //随机速度
            self.bodyChunks[0].vel += Custom.RNV() * Random.value * 0.5f;

            self.bodyChunks[1].vel *= 0.8f;
        }

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
    }
}
