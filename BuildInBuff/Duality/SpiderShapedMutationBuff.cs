﻿using RandomBuffUtils;
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
using Color = UnityEngine.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using MoreSlugcats;
using RandomBuff.Core.SaveData;
using System.Collections.Generic;

namespace BuiltinBuffs.Duality
{
    internal class SpiderShapedMutationBuff : Buff<SpiderShapedMutationBuff, SpiderShapedMutationBuffData>
    {
        public override BuffID ID => SpiderShapedMutationBuffEntry.SpiderShapedMutation;

        public int SpeedLevel
        {
            get
            {
                int num = 0;
                if (new BuffID("unl-agility").GetBuffData()?.StackLayer >= 2)
                    num++;
                return num;
            }
        }

        public int SpiderLevel
        {
            get
            {
                int num = 0;
                if (GetTemporaryBuffPool().allBuffIDs.Contains(BuiltinBuffs.Negative.ArachnophobiaIBuffEntry.arachnophobiaID))
                    num++;
                if (GetTemporaryBuffPool().allBuffIDs.Contains(HotDogGains.Positive.SpiderSenseBuffEntry.SpiderSenseID))
                    num++;
                if (GetTemporaryBuffPool().allBuffIDs.Contains(BuiltinBuffs.Negative.PhotophobiaBuffEntry.Photophobia))
                    num++;
                return num;
            }
        }

        public static int spiderLevel;

        public SpiderShapedMutationBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    if (SpiderShapedMutationBuffEntry.SpiderCatFeatures.TryGetValue(player, out _))
                        SpiderShapedMutationBuffEntry.SpiderCatFeatures.Remove(player);
                    var spider = new SpiderCat(player);
                    SpiderShapedMutationBuffEntry.SpiderCatFeatures.Add(player, spider);
                    spider.SpiderArthropod(player.graphicsModule as PlayerGraphics);
                    spider.InitiateSprites(game.cameras[0].spriteLeasers.
                        First(i => i.drawableObject == player.graphicsModule), game.cameras[0]);
                }
                SpiderShapedMutationBuffEntry.EstablishRelationship();
            }
        }

        public override void Update(RainWorldGame game)
        {
            base.Update(game);
            spiderLevel = SpiderLevel;
        }
    }

    internal class SpiderShapedMutationBuffData : BuffData
    {
        public override BuffID ID => SpiderShapedMutationBuffEntry.SpiderShapedMutation;
    }

    internal class SpiderShapedMutationBuffEntry : IBuffEntry
    {
        public static BuffID SpiderShapedMutation = new BuffID("SpiderShapedMutation", true);

        public static ConditionalWeakTable<Player, SpiderCat> SpiderCatFeatures = new ConditionalWeakTable<Player, SpiderCat>();

        public static int StackLayer
        {
            get
            {
                return SpiderShapedMutation.GetBuffData()?.StackLayer ?? 0;
            }
        }

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SpiderShapedMutationBuff, SpiderShapedMutationBuffData, SpiderShapedMutationBuffEntry>(SpiderShapedMutation);
        }

        public static void HookOn()
        {
            IL.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;

            IL.FlareBomb.Update += FlareBomb_UpdateIL;
            On.SporeCloud.Update += SporeCloud_Update;
            On.SlugcatStats.SlugcatCanMaul += SlugcatStats_SlugcatCanMaul;
            On.Player.IsCreatureLegalToHoldWithoutStun += Player_IsCreatureLegalToHoldWithoutStun;
            On.Creature.Violence += Creature_Violence;
            On.Player.CanEatMeat += Player_CanEatMeat;
            On.SlugcatStats.NourishmentOfObjectEaten += SlugcatStats_NourishmentOfObjectEaten;
            On.MoreSlugcats.SlugNPCAI.TheoreticallyEatMeat += SlugNPCAI_TheoreticallyEatMeat;

            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.Player.MaulingUpdate += Player_MaulingUpdate;
            On.Player.Collide += Player_Collide;
            On.Player.Grabability += Player_Grabability;
            On.Player.FreeHand += Player_FreeHand;
            On.Player.Jump += Player_Jump;
            On.SlugcatHand.Update += SlugcatHand_Update;

            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.Reset += PlayerGraphics_Reset;
            On.PlayerGraphics.ctor += PlayerGraphics_ctor;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
        }

        public static void LongLifeCycleHookOn()
        {
            On.SlugcatStats.SlugcatFoodMeter += SlugcatStats_SlugcatFoodMeter;
        }
        #region 额外特性
        //被闪光果致死
        private static void FlareBomb_UpdateIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(MoveType.After,
                    (i) => i.MatchCallvirt<Creature>("get_abstractCreature"),
                    (i) => i.MatchCallvirt<Creature>("SetKillTag"),
                    (i) => i.Match(OpCodes.Ldarg_0)))
                {
                    c.Emit(OpCodes.Ldloc_0);
                    c.EmitDelegate<Action<FlareBomb, int>>((self, i) =>
                    {
                            if (self.room.abstractRoom.creatures[i].realizedCreature is Player)
                        {
                            Player player = self.room.abstractRoom.creatures[i].realizedCreature as Player;
                            if (SpiderCatFeatures.TryGetValue(player, out var spider) &&
                                !spider.IsSpitter)
                            {
                                player.Die();
                                if (self.thrownBy != null)
                                {
                                    player.SetKillTag(self.thrownBy.abstractCreature);
                                }
                            }
                        }
                    });
                    c.Emit(OpCodes.Ldarg_0);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        //被烟雾果致死
        private static void SporeCloud_Update(On.SporeCloud.orig_Update orig, SporeCloud self, bool eu)
        {
            orig(self, eu);
            if (!self.nonToxic && self.checkInsectsDelay > -1)
            {
                self.checkInsectsDelay--;
                if (self.checkInsectsDelay < 1)
                {
                    self.checkInsectsDelay = 20;
                    for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
                    {
                        if (self.room.abstractRoom.creatures[i].realizedCreature != null)
                        {
                            if (self.room.abstractRoom.creatures[i].realizedCreature is Player)
                            {
                                if (Custom.DistLess(self.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos, self.rad + self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.rad + 20f))
                                {
                                    self.room.abstractRoom.creatures[i].realizedCreature.Die();
                                }
                            }
                        }
                    }
                }
            }
        }

        //允许撕咬
        private static bool SlugcatStats_SlugcatCanMaul(On.SlugcatStats.orig_SlugcatCanMaul orig, SlugcatStats.Name slugcatNum)
        {
            bool result = orig(slugcatNum);
            result = true;
            return result;
        }

        //允许撕咬未眩晕生物
        private static bool Player_IsCreatureLegalToHoldWithoutStun(On.Player.orig_IsCreatureLegalToHoldWithoutStun orig, Player self, Creature grabCheck)
        {
            bool result = orig(self, grabCheck);
            if (SpiderCatFeatures.TryGetValue(self, out var spider) && spider.LikeSpider)
                result = true;
            return result;
        }

        //撕咬伤害改变
        public static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (source != null && source.owner is Player player && type == Creature.DamageType.Bite &&
                SpiderCatFeatures.TryGetValue(player, out var spider) && spider.LikeSpider)
            {
                //第二层0.4，第三层0.6，以此类推
                damage *= 0.2f * StackLayer;//参考：普通狼蛛撕咬伤害0.4或1.2（各为50%概率），烈焰狼蛛撕咬伤害0.6
            }
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        //允许吃肉
        private static bool Player_CanEatMeat(On.Player.orig_CanEatMeat orig, Player self, Creature crit)
        {
            bool result = orig(self, crit);
            if (self.EatMeatOmnivoreGreenList(crit) && crit.dead)
            {
                return !ModManager.MSC || self.pyroJumpCooldown <= 60f;
            }
            result = !(crit is IPlayerEdible) && crit.dead &&
                     (!ModManager.CoopAvailable || !(crit is Player)) &&
                     (!ModManager.MSC || self.pyroJumpCooldown <= 60f);
            return result;
        }

        //让猫仔也可以吃肉
        private static bool SlugNPCAI_TheoreticallyEatMeat(On.MoreSlugcats.SlugNPCAI.orig_TheoreticallyEatMeat orig, SlugNPCAI self, Creature crit, bool excludeCentipedes)
        {
            bool flag = orig.Invoke(self, crit, excludeCentipedes);
            return true;
        }

        //修改获取的食物点数
        private static int SlugcatStats_NourishmentOfObjectEaten(On.SlugcatStats.orig_NourishmentOfObjectEaten orig, SlugcatStats.Name slugcatIndex, IPlayerEdible eatenobject)
        {
            SlugcatStats.Name newSlugcatIndex = SlugcatStats.Name.Red;
            int result = orig(newSlugcatIndex, eatenobject);
            return result;
        }

        //食量增大（需求+2，存储+1）
        private static IntVector2 SlugcatStats_SlugcatFoodMeter(On.SlugcatStats.orig_SlugcatFoodMeter orig, SlugcatStats.Name slugcat)
        {
            IntVector2 origFoodRequirement = orig(slugcat);
            int newHibernateRequirement = origFoodRequirement.y + (StackLayer >= 3 ? 4 : 2);
            int newTotalFoodRequirement = origFoodRequirement.x + (StackLayer >= 3 ? 5 : 3);

            return new IntVector2(newTotalFoodRequirement, newHibernateRequirement);
        }

        //修改食谱（不再使用）
        private static void Player_BiteEdibleObject(On.Player.orig_BiteEdibleObject orig, Player self, bool eu)
        {
            for (int i = 0; i < 2; i++)
            {
                if (self.grasps[i] != null && self.grasps[i].grabbed is IPlayerEdible && (self.grasps[i].grabbed as IPlayerEdible).Edible)
                {
                    //不吃素
                    if (self.grasps[i].grabbed is DangleFruit ||
                        self.grasps[i].grabbed is DandelionPeach ||
                        self.grasps[i].grabbed is GlowWeed ||
                        self.grasps[i].grabbed is GooieDuck ||
                        self.grasps[i].grabbed is LillyPuck ||
                        self.grasps[i].grabbed is Mushroom ||
                        self.grasps[i].grabbed is OracleSwarmer ||
                        self.grasps[i].grabbed is SlimeMold ||
                        self.grasps[i].grabbed is SwollenWaterNut)
                    {
                        return;
                    }
                    //只吃狼蛛吃的东西
                    if (self.grasps[i].grabbed is Creature &&
                        !(self.grasps[i].grabbed is LanternMouse ||
                          self.grasps[i].grabbed is Scavenger ||
                          self.grasps[i].grabbed is Player ||
                          self.grasps[i].grabbed is Cicada ||
                          self.grasps[i].grabbed is Centipede ||
                          self.grasps[i].grabbed is NeedleWorm ||
                          self.grasps[i].grabbed is BigNeedleWorm ||
                          self.grasps[i].grabbed is DropBug ||
                          self.grasps[i].grabbed is VultureGrub ||
                          self.grasps[i].grabbed is Hazer))
                        return;
                }
            }
            orig(self, eu);
        }

        //双手位置
        private static void SlugcatHand_Update(On.SlugcatHand.orig_Update orig, SlugcatHand self)
        {
            if (SpiderCatFeatures.TryGetValue(self.owner.owner as Player, out var spider))
                (self.owner.owner as Player).craftingObject = true;
            orig(self);

            if (SpiderCatFeatures.TryGetValue(self.owner.owner as Player, out spider))
                spider.SlugcatHandUpdate(self);
        }

        //只能一次叼一个东西
        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            Player.ObjectGrabability result = orig(self, obj);

            if (SpiderCatFeatures.TryGetValue(self, out var spider))
            {
                result = spider.Grabability(result, obj);
            }

            return result;
        }

        private static int Player_FreeHand(On.Player.orig_FreeHand orig, Player self)
        {
            int result = orig(self);
            if (SpiderCatFeatures.TryGetValue(self, out var spider))
                if (self.grasps[0] != null || self.grasps[1] != null)
                {
                    result = -1;
                }
            return result;
        }
        #endregion
        #region 生物关系
        //修改生物关系（狼蛛、小蜘蛛不再攻击玩家）
        public static void EstablishRelationship()
        {
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.BigSpider, CreatureTemplate.Type.Slugcat, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.Spider, CreatureTemplate.Type.Slugcat, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.BigSpider, MoreSlugcatsEnums.CreatureTemplateType.SlugNPC, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.Spider, MoreSlugcatsEnums.CreatureTemplateType.SlugNPC, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f));
        }
        #endregion

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!SpiderCatFeatures.TryGetValue(self, out _))
            {
                SpiderCatFeatures.Add(self, new SpiderCat(self));
                EstablishRelationship();
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (SpiderCatFeatures.TryGetValue(self, out var spider))
            {
                spider.Update();
                self.GetExPlayerData().HaveHands = false;
            }
        }

        private static void Player_MaulingUpdate(On.Player.orig_MaulingUpdate orig, Player self, int graspIndex)
        {
            orig(self, graspIndex);
            if (SpiderCatFeatures.TryGetValue(self, out var spider))
            {
                spider.MaulingUpdate(graspIndex);
            }
        }

        private static void Player_Collide(On.Player.orig_Collide orig, Player self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            orig(self, otherObject, myChunk, otherChunk);
            if (SpiderCatFeatures.TryGetValue(self, out var spider) && spider.LikeSpider)
            {
                spider.Collide(otherObject, myChunk, otherChunk);
            }
        }

        private static void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            orig(self);
            if (SpiderCatFeatures.TryGetValue(self, out var spider))
                spider.Jump(self.bodyChunks[0].vel.normalized, 1f);
        }

        #region 外观
        private static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (SpiderCatFeatures.TryGetValue(self.player, out var spider))
                spider.ApplyPalette(sLeaser, rCam, palette);
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (SpiderCatFeatures.TryGetValue(self.player, out var spider))
                spider.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (SpiderCatFeatures.TryGetValue(self.player, out var spider))
            {
                spider.InitiateSprites(sLeaser, rCam);
            }
        }

        private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (SpiderCatFeatures.TryGetValue(self.player, out var spider))
                spider.AddToContainer(sLeaser, rCam, newContatiner);
        }

        private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if (SpiderCatFeatures.TryGetValue(self.player, out var spider))
                spider.SpiderArthropod(self);
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            if (SpiderCatFeatures.TryGetValue(self.player, out var spider))
                spider.GraphicsUpdate();
        }

        private static void PlayerGraphics_Reset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig(self);
            if (SpiderCatFeatures.TryGetValue(self.player, out var spider))
                spider.Reset(self);
        }
        #endregion
        #region 时缓
        private static void RainWorldGame_RawUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, i => i.MatchLdfld<MainLoopProcess>("framesPerSecond"),
                                              i => i.MatchStfld<MainLoopProcess>("framesPerSecond")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<RainWorldGame>>(game =>
                {
                    if (UpdateSpeed < game.framesPerSecond)
                        game.framesPerSecond = UpdateSpeed;
                });
            }
            else
                BuffUtils.LogError(SpiderShapedMutation, "IL HOOK FAILED");
        }

        public static int UpdateSpeed = 1000;
        #endregion
    }

    internal class SpiderCat
    {
        WeakReference<Player> ownerRef;
        private int origThrowingSkill;

        public bool LikeSpider => SpiderShapedMutationBuffEntry.StackLayer >= 2;

        public bool IsSpitter => SpiderShapedMutationBuffEntry.StackLayer >= 3;

        #region 行动相关
        //指定位置
        Vector2 wantPos;
        bool wantPosIsSetX;
        bool wantPosIsSetY;

        float arthropodSpeed;
        private int footingCounter;
        public int outOfWaterFooting;

        public float flip;
        public float lastFlip;
        public float deathConvulsions;
        bool Spitter = true;
        bool jumping;
        Vector2 travelDir;
        public float runCycle;
        private int standCount;

        Vector2 modify = Vector2.down;

        public float DefaultArthropodSpeed
        {
            get
            {
                return 60f + 30f * SpiderShapedMutationBuff.Instance.SpeedLevel + 10f * SpiderShapedMutationBuff.spiderLevel;
            }
        }

        public bool Footing
        {
            get
            {
                return this.footingCounter > 5 || this.outOfWaterFooting > 0;
            }
        }
        #endregion

        #region 大跳击晕相关
        private bool hasSuperLaunchJump;
        private int hasSuperLaunchJumpCount;
        #endregion

        #region 毒镖相关
        private float charging;
        private Vector2? spitPos;
        private Vector2 spitDir;

        private Vector2 wantShootDir;
        private Vector2 aimDir;
        private Vector2 targetPoint;
        private Tracker.CreatureRepresentation spitAtCrit;
        private int ammo = 4;
        private float ammoRegen;
        private bool fastAmmoRegen;
        private bool goToSpitPos;
        private int noSitDelay;

        private int laserSprite;
        private float laserAlpha;
        private float lastLaserAlpha;
        private float laserPower;

        public Vector2 LookDirection
        {
            get
            {
                if (!ownerRef.TryGetTarget(out var player))
                    return Vector2.zero;
                if (player.room != null && player.room == player.room.game.cameras[0].room && player.graphicsModule != null)
                {
                    RoomCamera.SpriteLeaser spriteLeaser = player.room.game.cameras[0].spriteLeasers.FirstOrDefault(i => i.drawableObject == player.graphicsModule);
                    if (spriteLeaser != null)
                    {
                        for (int i = 3; i <= 7; i++)
                        {
                            if (spriteLeaser.sprites[3].element.name.Contains(i.ToString()))
                            {
                                if (player.input[0].x != 0 || !(player.graphicsModule is PlayerGraphics graphic))
                                    return new Vector2(player.input[0].x, 0);
                                else
                                    return (graphic.head.pos - player.bodyChunks[0].pos).normalized;
                            }
                        }
                    }
                }

                if (player.graphicsModule is PlayerGraphics graphics)
                {
                    if (graphics.lookDirection != Vector2.zero)
                        return graphics.lookDirection;
                    return (graphics.head.pos - player.bodyChunks[0].pos).normalized;
                }
                return new Vector2(player.input[0].x, 0);
            }
        }
        
        public int LaserSprite()
        {
            return laserSprite;
        }
        #endregion

        #region 蛛腿相关
        int arthropodSprite;
        BodyPart[] arthropod;

        public float[,,] legFlips;
        public float legLength;
        public Limb[,] legs;
        public int legsDangleCounter;
        private float legsThickness;
        public Vector2[,] legsTravelDirs;
        private IntVector2 deadLeg = new IntVector2(-1, -1);

        public int LegSprite(int side, int leg, int part)
        {
            return arthropodSprite + side * 12 + leg * 3 + part;
        }

        public void SpiderArthropod(PlayerGraphics self)
        {
            int num = 0;
            this.legs = new Limb[2, 4];
            for (int j = 0; j < this.legs.GetLength(0); j++)
            {
                for (int k = 0; k < this.legs.GetLength(1); k++)
                {
                    this.legs[j, k] = new Limb(self, self.player.mainBodyChunk, j * 4 + k, 0.1f, 0.7f, 0.99f, 12f, 0.95f);
                }
            }

            this.arthropod = new BodyPart[8];
            for (int l = 0; l < this.legs.GetLength(0); l++)
            {
                for (int m = 0; m < this.legs.GetLength(1); m++)
                {
                    this.arthropod[num] = this.legs[l, m];
                    num++;
                }
            }
        }
        #endregion

        public SpiderCat(Player player)
        {
            this.ownerRef = new WeakReference<Player>(player);

            this.arthropodSpeed = DefaultArthropodSpeed;
            this.wantPos = player.bodyChunks[0].pos;

            this.legLength = 65f + SpiderShapedMutationBuff.spiderLevel * 10f;
            this.legFlips = new float[2, 4, 2];
            this.legsTravelDirs = new Vector2[2, 4];
            this.legsThickness = Mathf.Lerp(0.7f, 1.1f, UnityEngine.Random.value);

            this.deathConvulsions = (player.State.alive ? 1f : 0f);
            this.standCount = 0;
            //走路速度下降，投掷技巧下降，爬行速度提升
            foreach (var self in (BuffCustom.TryGetGame(out var game) ? game.Players : new List<AbstractCreature>())
                .Select(i => i.realizedCreature as Player).Where(i => !(i is null)))
            {
                self.slugcatStats.Modify(this, PlayerUtils.Multiply, "corridorClimbSpeedFac", 2f);
                self.slugcatStats.Modify(this, PlayerUtils.Multiply, "poleClimbSpeedFac", 1.5f);
                self.slugcatStats.Modify(this, PlayerUtils.Multiply, "runspeedFac", 0.5f);
                self.slugcatStats.Modify(this, PlayerUtils.Subtraction, "throwingSkill", SpiderShapedMutationBuffEntry.StackLayer - 1);
            }
        }

        #region 外观
        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            arthropodSprite = sLeaser.sprites.Length;
            laserSprite = arthropodSprite + this.legs.GetLength(0) * this.legs.GetLength(1) * 3;
            Array.Resize(ref sLeaser.sprites, laserSprite + 1);

            for (int i = 0; i < this.legs.GetLength(0); i++)
            {
                for (int j = 0; j < this.legs.GetLength(1); j++)
                {
                    sLeaser.sprites[this.LegSprite(i, j, 0)] = new FSprite("CentipedeLegA", true);
                    sLeaser.sprites[this.LegSprite(i, j, 1)] = new FSprite("SpiderLeg" + j.ToString() + "A", true);
                    if (j == 0)
                    {
                        sLeaser.sprites[this.LegSprite(i, j, 2)] = new FSprite("CentipedeLegB", true);
                    }
                    else
                    {
                        sLeaser.sprites[this.LegSprite(i, j, 2)] = new FSprite("SpiderLeg" + j.ToString() + "B", true);
                    }
                    sLeaser.sprites[LegSprite(i, j, 0)].color = sLeaser.sprites[0].color;
                    sLeaser.sprites[LegSprite(i, j, 1)].color = sLeaser.sprites[0].color;
                    sLeaser.sprites[LegSprite(i, j, 2)].color = sLeaser.sprites[0].color;
                }
            }

            sLeaser.sprites[LaserSprite()] = new CustomFSprite("Futile_White");
            sLeaser.sprites[LaserSprite()].shader = rCam.game.rainWorld.Shaders["HologramBehindTerrain"];

            self.AddToContainer(sLeaser, rCam, null);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;

            for (int i = 0; i < this.legs.GetLength(0); i++)
            {
                for (int j = 0; j < this.legs.GetLength(1); j++)
                {
                    sLeaser.sprites[LegSprite(i, j, 0)].color = sLeaser.sprites[0].color;
                    sLeaser.sprites[LegSprite(i, j, 1)].color = sLeaser.sprites[0].color;
                    sLeaser.sprites[LegSprite(i, j, 2)].color = sLeaser.sprites[0].color;
                }
            }
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            if (arthropodSprite >= 1 && sLeaser.sprites.Length >= arthropodSprite + 6)
            {
                var foregroundContainer = rCam.ReturnFContainer("Foreground");
                var midgroundContainer = rCam.ReturnFContainer("Midground");
                //让节肢移到中景
                for (int i = 0; i < this.legs.GetLength(0); i++)
                {
                    for (int j = 0; j < this.legs.GetLength(1); j++)
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            var sprite = sLeaser.sprites[LegSprite(i, j, k)];
                            foregroundContainer.RemoveChild(sprite);
                            midgroundContainer.AddChild(sprite);
                            sprite.MoveBehindOtherNode(sLeaser.sprites[0]);
                        }
                    }
                }
            }
            if (laserSprite >= 1 && sLeaser.sprites.Length >= laserSprite + 1)
            {
                rCam.ReturnFContainer(ModManager.MMF ? "Midground" : "Foreground").AddChild(sLeaser.sprites[LaserSprite()]);
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

            float flip = Mathf.Lerp(this.lastFlip, this.flip, timeStacker);
            float num = 0f; // player.superLaunchJump / 20f;
            Vector2 bodyPos = Vector2.Lerp(player.mainBodyChunk.lastPos, player.mainBodyChunk.pos, timeStacker);
            Vector2 hipsPos = Vector2.Lerp(player.bodyChunks[1].lastPos, player.bodyChunks[1].pos, timeStacker) + Custom.RNV() * Random.value * 3.5f * num;
            //BuffPlugin.Log($"bodyPos:" + bodyPos);
            //BuffPlugin.Log($"hipsPos:" + hipsPos);
            Vector2 formHipsToBody = Custom.DirVec(hipsPos, bodyPos);
            Vector2 a = Custom.PerpendicularVector(formHipsToBody);
            for (int j = 0; j < this.legs.GetLength(0); j++)
            {
                for (int k = 0; k < this.legs.GetLength(1); k++)
                {
                    float t = Mathf.InverseLerp(0f, (float)(this.legs.GetLength(1) - 1), (float)k);
                    Vector2 rootPos = Vector2.Lerp(bodyPos, hipsPos, 0.3f);
                    //蛛腿根部位置
                    rootPos += a * ((j == 0) ? -1f : 1f) * 3f * (1f - Mathf.Abs(flip));//问题在这一步
                    rootPos += formHipsToBody * Mathf.Lerp(10f, -6f, t);
                    Vector2 legPos = Vector2.Lerp(this.legs[j, k].lastPos, this.legs[j, k].pos, timeStacker);
                    float legFlips = Mathf.Lerp(this.legFlips[j, k, 1], this.legFlips[j, k, 0], timeStacker);
                    //是否需要翻转关节
                    bool flag = true;//Custom.AimFromOneVectorToAnother(player.bodyChunks[1].pos, player.bodyChunks[0].pos);
                    if (player.room.GetTile(player.mainBodyChunk.pos + Custom.PerpendicularVector(player.mainBodyChunk.pos, player.bodyChunks[1].pos) * 20f).Solid)
                    {
                        flag = false;
                    }
                    else if (player.room.GetTile(player.mainBodyChunk.pos - Custom.PerpendicularVector(player.mainBodyChunk.pos, player.bodyChunks[1].pos) * 20f).Solid)
                    {
                        flag = false;
                    }
                    a = (j == 0 && flag) ? -a : a;
                    //蛛腿关节位置
                    /*
                    Vector2 jointPos = (j == 0 && flag) ?
                        InverseKinematic_Reverse(rootPos, legPos, this.legLength * 0.7f, this.legLength * 0.7f, legFlips) :
                        Custom.InverseKinematic(rootPos, legPos, this.legLength * 0.7f, this.legLength * 0.7f, legFlips);*/
                    Vector2 jointPos = Custom.InverseKinematic(rootPos, legPos, this.legLength * 0.7f, this.legLength * 0.7f, legFlips);
                    jointPos = Vector2.Lerp(jointPos,
                                            (rootPos + legPos) * 0.5f - a * flip * Custom.LerpMap(Vector2.Distance(rootPos, legPos), 0f, this.legLength * 1.3f, this.legLength * 0.7f, 0f, 3f),
                                            Mathf.Abs(flip));
                    //内侧蛛腿
                    Vector2 innerLeg = Vector2.Lerp(rootPos, jointPos, 0.5f);
                    //外侧蛛腿
                    Vector2 outerLeg = Vector2.Lerp(jointPos, legPos, 0.5f);
                    //半个节肢长度
                    float d = this.legLength / 4f;
                    //两节肢中点的中点
                    Vector2 midpoint = Vector2.Lerp(innerLeg, outerLeg, 0.5f);
                    innerLeg = midpoint + Custom.DirVec(midpoint, innerLeg) * d / 2f;
                    outerLeg = midpoint + Custom.DirVec(midpoint, outerLeg) * d / 2f;
                    sLeaser.sprites[this.LegSprite(j, k, 0)].x = rootPos.x - camPos.x;
                    sLeaser.sprites[this.LegSprite(j, k, 0)].y = rootPos.y - camPos.y;
                    sLeaser.sprites[this.LegSprite(j, k, 0)].rotation = Custom.AimFromOneVectorToAnother(rootPos, innerLeg);
                    sLeaser.sprites[this.LegSprite(j, k, 0)].scaleY = Vector2.Distance(rootPos, innerLeg) / sLeaser.sprites[this.LegSprite(j, k, 0)].element.sourcePixelSize.y;
                    sLeaser.sprites[this.LegSprite(j, k, 0)].anchorY = 0f;
                    sLeaser.sprites[this.LegSprite(j, k, 0)].scaleX = -Mathf.Sign(legFlips) * 1.5f * this.legsThickness;
                    sLeaser.sprites[this.LegSprite(j, k, 1)].x = innerLeg.x - camPos.x;
                    sLeaser.sprites[this.LegSprite(j, k, 1)].y = innerLeg.y - camPos.y;
                    sLeaser.sprites[this.LegSprite(j, k, 1)].rotation = Custom.AimFromOneVectorToAnother(innerLeg, outerLeg);
                    sLeaser.sprites[this.LegSprite(j, k, 1)].scaleY = (Vector2.Distance(innerLeg, outerLeg) + 2f) / sLeaser.sprites[this.LegSprite(j, k, 1)].element.sourcePixelSize.y;
                    sLeaser.sprites[this.LegSprite(j, k, 1)].anchorY = 0.1f;
                    sLeaser.sprites[this.LegSprite(j, k, 1)].scaleX = -Mathf.Sign(legFlips) * 1.2f * this.legsThickness;
                    sLeaser.sprites[this.LegSprite(j, k, 2)].anchorY = 0.1f;
                    sLeaser.sprites[this.LegSprite(j, k, 2)].scaleX = -Mathf.Sign(legFlips) * 1.2f * this.legsThickness;
                    sLeaser.sprites[this.LegSprite(j, k, 2)].x = outerLeg.x - camPos.x;
                    sLeaser.sprites[this.LegSprite(j, k, 2)].y = outerLeg.y - camPos.y;
                    sLeaser.sprites[this.LegSprite(j, k, 2)].rotation = Custom.AimFromOneVectorToAnother(outerLeg, legPos);
                    sLeaser.sprites[this.LegSprite(j, k, 2)].scaleY = (Vector2.Distance(outerLeg, legPos) + 1f) / sLeaser.sprites[this.LegSprite(j, k, 2)].element.sourcePixelSize.y;
                }
            }

            //瞄准线
            Vector2 headPos = Vector2.Lerp((player.graphicsModule as PlayerGraphics).head.lastPos, (player.graphicsModule as PlayerGraphics).head.pos, timeStacker);
            float nowLaserAlpha = Mathf.Lerp(lastLaserAlpha, laserAlpha, timeStacker);
            Color color = Custom.HSL2RGB(Custom.RGB2HSL(sLeaser.sprites[0].color).x, 1f, 0.5f);
            if (this.charging > 0)
            {
                nowLaserAlpha = ((Mathf.FloorToInt(10f * this.charging) % 2 < 1) ? 1f : 0f);//((modeCounter % 6 < 3) ? 1f : 0f);
                if (Mathf.FloorToInt(10f * this.charging) % 2 == 0)
                    color = Color.Lerp(color, Color.white, UnityEngine.Random.value);
                if (this.charging >= 1)
                {
                    nowLaserAlpha = 1f;
                    color = Color.white;
                }
            }
            Vector2 laserRootPos = headPos;
            Vector2 aimDir = AimDir(timeStacker);
            if (nowLaserAlpha <= 0f)
            {
                sLeaser.sprites[LaserSprite()].isVisible = false;
            }
            else
            {
                sLeaser.sprites[LaserSprite()].isVisible = true;
                sLeaser.sprites[LaserSprite()].alpha = nowLaserAlpha;
                Vector2 corner = Custom.RectCollision(laserRootPos, laserRootPos + aimDir * 100000f, rCam.room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);
                IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(rCam.room, laserRootPos, corner);
                if (intVector.HasValue)
                {
                    corner = Custom.RectCollision(corner, laserRootPos, rCam.room.TileRect(intVector.Value)).GetCorner(FloatRect.CornerLabel.D);
                }
                if (Custom.Dist(laserRootPos, corner) > 150f)
                    corner = laserRootPos + 150f * Custom.DirVec(laserRootPos, corner);
                (sLeaser.sprites[LaserSprite()] as CustomFSprite).verticeColors[0] = Custom.RGB2RGBA(color, nowLaserAlpha);
                (sLeaser.sprites[LaserSprite()] as CustomFSprite).verticeColors[1] = Custom.RGB2RGBA(color, nowLaserAlpha);
                (sLeaser.sprites[LaserSprite()] as CustomFSprite).verticeColors[2] = Custom.RGB2RGBA(color, Mathf.Pow(nowLaserAlpha, 2f) * 0.5f);
                (sLeaser.sprites[LaserSprite()] as CustomFSprite).verticeColors[3] = Custom.RGB2RGBA(color, Mathf.Pow(nowLaserAlpha, 2f) * 0.5f);
                (sLeaser.sprites[LaserSprite()] as CustomFSprite).MoveVertice(0, laserRootPos + aimDir * 2f + Custom.PerpendicularVector(aimDir) * 0.5f - camPos);
                (sLeaser.sprites[LaserSprite()] as CustomFSprite).MoveVertice(1, laserRootPos + aimDir * 2f - Custom.PerpendicularVector(aimDir) * 0.5f - camPos);
                (sLeaser.sprites[LaserSprite()] as CustomFSprite).MoveVertice(2, corner - Custom.PerpendicularVector(aimDir) * 0.5f - camPos);
                (sLeaser.sprites[LaserSprite()] as CustomFSprite).MoveVertice(3, corner + Custom.PerpendicularVector(aimDir) * 0.5f - camPos);
            }
        }

        public void GraphicsUpdate()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            if (float.IsNaN(this.flip))
                this.flip = 0;

            if (!player.Consious || !Footing)
            {
                this.legsDangleCounter = 30;
            }
            else if (this.legsDangleCounter > 0)
            {
                this.legsDangleCounter--;
                if (Footing)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (player.room.aimap.TileAccessibleToCreature(player.bodyChunks[i].pos, player.Template))
                        {
                            this.legsDangleCounter = 0;
                        }
                    }
                }
            }
            this.lastFlip = this.flip;
            float num = Custom.AimFromOneVectorToAnother(player.bodyChunks[1].pos, player.bodyChunks[0].pos);
            float num2 = 0f;
            float num3 = 0f;
            int num4 = 0;
            for (int k = 0; k < this.legs.GetLength(0); k++)
            {
                for (int l = 0; l < this.legs.GetLength(1); l++)
                {
                    num3 += Custom.LerpMap(Vector2.Dot(Custom.DirVec(player.mainBodyChunk.pos, this.legs[k, l].absoluteHuntPos), this.travelDir.normalized), -0.6f, 0.6f, 0f, 0.125f);
                    num2 += Custom.DistanceToLine(this.legs[k, l].pos, player.bodyChunks[1].pos, player.bodyChunks[0].pos);
                    if (this.legs[k, l].OverLappingHuntPos)
                    {
                        num4++;
                    }
                }
            }
            num3 *= Mathf.InverseLerp(0f, 0.1f, this.travelDir.magnitude);
            this.flip = Custom.LerpAndTick(this.flip, Mathf.Clamp(num2 / 40f, -1f, 1f), 0.07f, 0.1f);
            float num5 = 0f;
            if (player.Consious)
            {
                if (player.room.GetTile(player.mainBodyChunk.pos + Custom.PerpendicularVector(player.mainBodyChunk.pos, player.bodyChunks[1].pos) * 20f).Solid)
                {
                    num5 += 1f;
                }
                else if (player.room.GetTile(player.mainBodyChunk.pos - Custom.PerpendicularVector(player.mainBodyChunk.pos, player.bodyChunks[1].pos) * 20f).Solid)
                {
                    num5 -= 1f;
                }
            }
            if (num5 != 0f)
            {
                this.flip = Custom.LerpAndTick(this.flip, num5, 0.07f, 0.05f);
            }
            int num6 = 0;
            for (int m = 0; m < this.legs.GetLength(1); m++)
            {
                for (int n = 0; n < this.legs.GetLength(0); n++)
                {
                    float num7 = Mathf.InverseLerp(0f, (float)(this.legs.GetLength(1) - 1), (float)m);
                    float num8 = 0.5f + 0.5f * Mathf.Sin((this.runCycle + (float)num6 * 0.25f) * 3.1415927f);
                    this.legFlips[n, m, 1] = this.legFlips[n, m, 0];
                    if (UnityEngine.Random.value < num8 * 0.5f && !Custom.DistLess(this.legs[n, m].lastPos, this.legs[n, m].pos, 2f))
                    {
                        if (UnityEngine.Random.value < num8)
                        {
                            this.legFlips[n, m, 0] = Custom.LerpAndTick(this.legFlips[n, m, 0], Mathf.Lerp((n == 0) ? -1f : 1f, this.flip, Mathf.Abs(this.flip)), 0.01f, UnityEngine.Random.value / 6f);
                        }
                        if (UnityEngine.Random.value < num8)
                        {
                            this.legsTravelDirs[n, m] = Vector2.Lerp(this.legsTravelDirs[n, m], this.travelDir, Mathf.Pow(UnityEngine.Random.value, 1f - 0.9f * num8));
                        }
                    }
                    if (!player.Consious && UnityEngine.Random.value < this.deathConvulsions)
                    {
                        this.legsTravelDirs[n, m] = Custom.RNV() * UnityEngine.Random.value;
                    }
                    else if (player.superLaunchJump > 1f)
                    {
                        this.legsTravelDirs[n, m] *= 0f;
                    }
                    this.legs[n, m].Update();
                    if (this.legs[n, m].mode == Limb.Mode.HuntRelativePosition || this.legsDangleCounter > 0 || (this.deadLeg.x == n && this.deadLeg.y == m))
                    {
                        this.legs[n, m].mode = Limb.Mode.Dangle;
                    }
                    Vector2 vector2 = Custom.DegToVec(num + Mathf.Lerp(40f, 160f, num7) * ((num5 != 0f) ? (-num5) : ((n == 0) ? 1f : -1f)));
                    Vector2 vector3 = player.bodyChunks[0].pos + Vector3.Slerp(this.legsTravelDirs[n, m], vector2, 0.1f).ToVector2InPoints() * this.legLength * 0.85f * Mathf.Pow(num8, 0.5f);
                    this.legs[n, m].ConnectToPoint(vector3, this.legLength, false, 0f, player.mainBodyChunk.vel, 0.1f, 0f);
                    this.legs[n, m].ConnectToPoint(player.bodyChunks[0].pos, this.legLength, false, 0f, player.mainBodyChunk.vel, 0.1f, 0f);
                    if (this.legsDangleCounter > 0 || num8 < 0.1f || (this.deadLeg.x == n && this.deadLeg.y == m))
                    {
                        Vector2 a2 = vector3 + vector2 * this.legLength * 0.5f;
                        if (!player.Consious)
                        {
                            a2 = vector3 + this.legsTravelDirs[n, m] * this.legLength * 0.5f;
                        }
                        this.legs[n, m].vel = Vector2.Lerp(this.legs[n, m].vel, a2 - this.legs[n, m].pos, 0.05f);
                        Limb limb = this.legs[n, m];
                        limb.vel.y = limb.vel.y - 0.4f;
                        if (player.Consious && (this.deadLeg.x != n || this.deadLeg.y != m))
                        {
                            this.legs[n, m].vel += Custom.RNV() * 3f;
                        }
                    }
                    else
                    {
                        Vector2 vector4 = vector3 + vector2 * this.legLength;
                        for (int num9 = 0; num9 < this.legs.GetLength(0); num9++)
                        {
                            for (int num10 = 0; num10 < this.legs.GetLength(1); num10++)
                            {
                                if (num9 != n && num10 != m && Custom.DistLess(vector4, this.legs[num9, num10].absoluteHuntPos, this.legLength * 0.1f))
                                {
                                    vector4 = this.legs[num9, num10].absoluteHuntPos + Custom.DirVec(this.legs[num9, num10].absoluteHuntPos, vector4) * this.legLength * 0.1f;
                                }
                            }
                        }
                        float num11 = 1.2f;
                        if (!this.legs[n, m].reachedSnapPosition)
                        {
                            modify = Vector2.zero;
                            if (player.standing)
                                modify = Vector2.down;
                            //如果可以抓天花板，就抓天花板
                            for (int t = 0; t <= Mathf.Floor(legLength / 20f); t++)
                            {
                                if (Custom.DistLess(this.legs[n, m].pos, player.room.MiddleOfTile(player.room.GetTilePosition(vector3 + t * 20f * Mathf.Abs(Mathf.Sin(num)) * Vector2.up)), 0.1f * legLength + 10f) &&
                                    (player.room.GetTile(vector3 + t * 20f * Mathf.Abs(Mathf.Sin(num)) * Vector2.up).Terrain == Room.Tile.TerrainType.Solid ||
                                    player.room.GetTile(vector3 + t * 20f * Mathf.Abs(Mathf.Sin(num)) * Vector2.up).Terrain == Room.Tile.TerrainType.Slope ||
                                    player.room.GetTile(vector3 + t * 20f * Mathf.Abs(Mathf.Sin(num)) * Vector2.up).Terrain == Room.Tile.TerrainType.Floor))
                                {
                                    modify = Vector2.up;
                                    break;
                                }
                            }
                            this.legs[n, m].FindGrip(player.room, vector3, vector3 + 20f * Mathf.Abs(Mathf.Sin(num)) * modify, this.legLength * num11, vector4, -2, -2, false);
                            if (modify == Vector2.down)
                                this.legs[n, m].vel += Vector2.down;
                        }
                        else if (!Custom.DistLess(vector3, this.legs[n, m].absoluteHuntPos, this.legLength * num11 * Mathf.Pow(1f - num8, 0.2f)))
                        {
                            this.legs[n, m].mode = Limb.Mode.Dangle;
                        }
                    }
                    num6++;
                }
            }

            //瞄准线
            lastLaserAlpha = laserAlpha;
            if (charging <= 0)
            {
                laserAlpha = Mathf.Max(laserAlpha - 0.1f, 0f);
            }
            else if (UnityEngine.Random.value < 0.25f)
            {
                laserAlpha = ((UnityEngine.Random.value < laserPower) ? Mathf.Lerp(laserAlpha, Mathf.Pow(laserPower, 0.25f), Mathf.Pow(UnityEngine.Random.value, 0.5f)) : (laserAlpha * UnityEngine.Random.value * UnityEngine.Random.value));
            }
        }

        public void Reset(PlayerGraphics self)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            //防止拉丝
            for (int i = 0; i < arthropod.Length; i++)
            {
                arthropod[i].Reset(player.bodyChunks[0].pos);
            }
            this.flip = 0;
            this.lastFlip = 0;
        }
        #endregion

        //进行更新
        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (player.room == null)
                return;

            if (player.grasps[0] != null && player.grasps[1] != null)
            {
                player.ReleaseGrasp(1);
            }/*
            if (player.bodyMode == Player.BodyModeIndex.Stand || 
                player.bodyMode == Player.BodyModeIndex.Crawl ||
                player.bodyMode == Player.BodyModeIndex.Swimming ||
                hasSuperLaunchJumpCount > 80)
            {
                hasSuperLaunchJump = false;
                hasSuperLaunchJumpCount = 0;
            }
            if (hasSuperLaunchJump)
                hasSuperLaunchJumpCount++;*/
            /*
            if (player.animation == Player.AnimationIndex.StandUp)
                this.standCount++;
            else
                this.standCount = 0;
            if (this.standCount > 10)
            {
                this.standCount = 0;
                player.bodyMode = Player.BodyModeIndex.Crawl;
                player.animation = Player.AnimationIndex.LedgeCrawl;
            }*/

            if (player.graphicsModule != null && player.Consious && (player.room.aimap == null ||
                (!player.room.aimap.TileAccessibleToCreature(player.mainBodyChunk.pos, player.Template) && 
                !player.room.aimap.TileAccessibleToCreature(player.bodyChunks[1].pos, player.Template))))
            {
                for (int l = 0; l < this.legs.GetLength(0); l++)
                {
                    for (int m = 0; m < this.legs.GetLength(1); m++)
                    {
                        if (this.legs[l, m].reachedSnapPosition &&
                            (!TileAccessibleToPlayer() ||
                            (Custom.DistLess(wantPos, player.mainBodyChunk.pos, 20f) &&
                            Vector2.Dot(wantPos - player.mainBodyChunk.pos, this.legs[l, m].absoluteHuntPos - player.mainBodyChunk.pos) < 0 &&
                            !Custom.DistLess(player.mainBodyChunk.pos, this.legs[l, m].absoluteHuntPos, this.legLength) &&
                            Custom.DistLess(player.mainBodyChunk.pos, this.legs[l, m].absoluteHuntPos, this.legLength + 15f) &&
                            (player.room.gravity > 0.3f && player.Submersion < 0.5f))))
                        {
                            Vector2 a = Custom.DirVec(player.mainBodyChunk.pos, this.legs[l, m].absoluteHuntPos) * (Vector2.Distance(player.mainBodyChunk.pos, this.legs[l, m].absoluteHuntPos) - this.legLength);
                            player.mainBodyChunk.pos += a * 0.8f;
                            player.mainBodyChunk.vel += a * 0.8f;
                        }/*
                        if (this.legs[l, m].reachedSnapPosition && modify == Vector2.up)
                        {
                            Vector2 a = Vector2.up * (Vector2.Distance(player.mainBodyChunk.pos, this.legs[l, m].absoluteHuntPos) - this.legLength);
                            player.mainBodyChunk.pos += a * 0.8f;
                            player.mainBodyChunk.vel += a * 0.8f;
                        }*/
                    }
                }
            }
            if (player.Consious)
            {
                if (true) //player.room.aimap.TileAccessibleToCreature(player.bodyChunks[0].pos, player.Template) || player.room.aimap.TileAccessibleToCreature(player.bodyChunks[0].pos, player.Template)
                {
                    this.footingCounter++;
                }
                this.Act(player);
            }
            else
            {
                this.footingCounter = 0;
                this.jumping = false;
                if (this.deathConvulsions > 0f)
                {
                    if (player.dead)
                    {
                        this.deathConvulsions = Mathf.Max(0f, this.deathConvulsions - UnityEngine.Random.value / 80f);
                    }
                    if (player.mainBodyChunk.ContactPoint.x != 0 || player.mainBodyChunk.ContactPoint.y != 0)
                    {
                        player.mainBodyChunk.vel += Custom.RNV() * UnityEngine.Random.value * 8f * Mathf.Pow(this.deathConvulsions, 0.5f);
                    }
                    if (player.bodyChunks[1].ContactPoint.x != 0 || player.bodyChunks[1].ContactPoint.y != 0)
                    {
                        player.bodyChunks[1].vel += Custom.RNV() * UnityEngine.Random.value * 4f * Mathf.Pow(this.deathConvulsions, 0.5f);
                    }
                    if (UnityEngine.Random.value < 0.05f)
                    {
                        player.room.PlaySound(SoundID.Big_Spider_Death_Rustle, player.mainBodyChunk, false, 0.5f + UnityEngine.Random.value * 0.5f * this.deathConvulsions, 0.9f + 0.3f * this.deathConvulsions);
                    }
                    if (UnityEngine.Random.value < 0.025f)
                    {
                        player.room.PlaySound(SoundID.Big_Spider_Take_Damage, player.mainBodyChunk, false, UnityEngine.Random.value * 0.5f, 1f);
                    }
                }
            }
        }

        public void MaulingUpdate(int graspIndex)
        {
            if (!ownerRef.TryGetTarget(out var player) || !this.LikeSpider)
                return;
            if (player.room == null)
                return; 
            if (player.grasps[graspIndex] == null || 
                !(player.grasps[graspIndex].grabbed is Creature crit) ||
                !ShouldFired(crit))
                return;

            Vector2 vector = player.grasps[graspIndex].grabbedChunk.pos * player.grasps[graspIndex].grabbedChunk.mass;
            float num = player.grasps[graspIndex].grabbedChunk.mass;
            for (int i = 0; i < player.grasps[graspIndex].grabbed.bodyChunkConnections.Length; i++)
            {
                if (player.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk1 == player.grasps[graspIndex].grabbedChunk)
                {
                    vector += player.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk2.pos * player.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk2.mass;
                    num += player.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk2.mass;
                }
                else if (player.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk2 == player.grasps[graspIndex].grabbedChunk)
                {
                    vector += player.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk1.pos * player.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk1.mass;
                    num += player.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk1.mass;
                }
            }
            vector /= num;
            if (player.maulTimer >= 8)
            {
                player.mainBodyChunk.pos += Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * 4f;
                player.grasps[graspIndex].grabbedChunk.vel += Custom.DirVec(vector, player.mainBodyChunk.pos) * 0.9f / player.grasps[graspIndex].grabbedChunk.mass;
                for (int j = UnityEngine.Random.Range(0, 3); j >= 0; j--)
                {
                    player.room.AddObject(new WaterDrip(Vector2.Lerp(player.grasps[graspIndex].grabbedChunk.pos, player.mainBodyChunk.pos, UnityEngine.Random.value) + player.grasps[graspIndex].grabbedChunk.rad * Custom.RNV() * UnityEngine.Random.value, Custom.RNV() * 6f * UnityEngine.Random.value + Custom.DirVec(vector, (player.mainBodyChunk.pos + (player.graphicsModule as PlayerGraphics).head.pos) / 2f) * 7f * UnityEngine.Random.value + Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * UnityEngine.Random.value * player.EffectiveRoomGravity * 7f, false));
                }
                if (player.Grab(player.grasps[graspIndex].grabbed, 0, player.grasps[graspIndex].grabbedChunk.index, Creature.Grasp.Shareability.CanNotShare, 0.5f, false, true))
                    player.room.PlaySound(SoundID.Big_Spider_Grab_Creature, player.mainBodyChunk);
                else
                    player.room.PlaySound(SoundID.Big_Spider_Slash_Creature, player.mainBodyChunk);
                player.room.PlaySound(SoundID.Slugcat_Eat_Meat_B, player.mainBodyChunk);
                player.room.PlaySound(SoundID.Drop_Bug_Grab_Creature, player.mainBodyChunk, false, 1f, 0.76f);
                BuffPlugin.Log("Mauled target");
                if (!(player.grasps[graspIndex].grabbed as Creature).dead)
                {
                    for (int num12 = UnityEngine.Random.Range(8, 14); num12 >= 0; num12--)
                    {
                        player.room.AddObject(new WaterDrip(Vector2.Lerp(player.grasps[graspIndex].grabbedChunk.pos, player.mainBodyChunk.pos, UnityEngine.Random.value) + player.grasps[graspIndex].grabbedChunk.rad * Custom.RNV() * UnityEngine.Random.value, Custom.RNV() * 6f * UnityEngine.Random.value + Custom.DirVec(player.grasps[graspIndex].grabbed.firstChunk.pos, (player.mainBodyChunk.pos + (player.graphicsModule as PlayerGraphics).head.pos) / 2f) * 7f * UnityEngine.Random.value + Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * UnityEngine.Random.value * player.EffectiveRoomGravity * 7f, false));
                    }
                    Creature creature = player.grasps[graspIndex].grabbed as Creature;
                    creature.SetKillTag(player.abstractCreature);
                    creature.Violence(player.bodyChunks[0], new Vector2?(new Vector2(0f, 0f)), player.grasps[graspIndex].grabbedChunk, null, Creature.DamageType.Bite, 1f, 15f);
                    creature.stun = 5;
                    if (creature.abstractCreature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.Inspector)
                    {
                        creature.Die();
                    }
                }
                player.maulTimer = 0;
                player.wantToPickUp = 0;
                if (player.grasps[graspIndex] != null)
                {
                    player.TossObject(graspIndex, false);
                    player.ReleaseGrasp(graspIndex);
                }
                player.standing = true;
            }
            /*
            while (player.maulTimer % 5 != 0 && player.maulTimer < 40)
            {
                BuffPlugin.Log(player.maulTimer);
                player.maulTimer++;
                player.MaulingUpdate(graspIndex);
            }*/
        }

        public void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (player.room == null)
                return;
            if (hasSuperLaunchJump && otherObject is Creature)
            {
                bool flag4 = otherObject is Player && !Custom.rainWorld.options.friendlyFire;
                if (!(otherObject as Creature).dead && (otherObject as Creature).abstractCreature.creatureTemplate.type != MoreSlugcatsEnums.CreatureTemplateType.SlugNPC && (!ModManager.CoopAvailable || !flag4))
                {
                    player.room.ScreenMovement(new Vector2?(player.bodyChunks[0].pos), player.mainBodyChunk.vel * player.bodyChunks[0].mass * 5f * 0.1f, Mathf.Max((player.bodyChunks[0].mass - 30f) / 50f, 0f));
                    player.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Hard, player.mainBodyChunk);
                    (otherObject as Creature).SetKillTag(player.abstractCreature);
                    (otherObject as Creature).Violence(player.mainBodyChunk, 
                                                       new Vector2?(new Vector2(player.mainBodyChunk.vel.x * 5f, player.mainBodyChunk.vel.y)), 
                                                       otherObject.firstChunk, null, Creature.DamageType.Blunt, 0f, 30f);
                    player.animation = Player.AnimationIndex.None;
                    player.mainBodyChunk.vel.Scale(new Vector2(-0.5f, -0.5f));
                    if (((otherObject as Creature).State is HealthState && ((otherObject as Creature).State as HealthState).ClampedHealth == 0f) || (otherObject as Creature).State.dead)
                    {
                        player.room.PlaySound(SoundID.Spear_Stick_In_Creature, player.mainBodyChunk, false, 1.7f, 1f);
                    }
                    else
                    {
                        player.room.PlaySound(SoundID.Big_Needle_Worm_Impale_Terrain, player.mainBodyChunk, false, 1.2f, 1f);
                    }
                }
            }
            hasSuperLaunchJump = false;
        }

        //移动速度
        private void Act(Player self)
        {
            if (self.Submersion > 0.3f)
            {
                //this.Swim();
                return;
            }

            SpitUpdate();
            PlayerMoveByLegs();

            if (this.jumping)
            {
                bool flag = false;
                for (int i = 0; i < self.bodyChunks.Length; i++)
                {
                    if (self.bodyChunks[i].ContactPoint.x != 0 ||
                        self.bodyChunks[i].ContactPoint.y != 0 ||
                        self.bodyMode == Player.BodyModeIndex.WallClimb ||
                        self.animation == Player.AnimationIndex.HangFromBeam ||
                        self.animation == Player.AnimationIndex.ClimbOnBeam ||
                        self.animation == Player.AnimationIndex.AntlerClimb ||
                        self.animation == Player.AnimationIndex.VineGrab ||
                        self.animation == Player.AnimationIndex.ZeroGPoleGrab ||
                        self.animation == Player.AnimationIndex.HangUnderVerticalBeam)
                    {
                        flag = true;
                    }
                }

                if (flag)
                {
                    this.footingCounter++;
                }
                else
                {
                    this.footingCounter = 0;
                }

                Vector2 pos = new Vector2(self.input[0].x, self.input[0].y);
                if (pos == Vector2.zero) 
                    pos = Vector2.up;
                self.bodyChunks[0].vel += pos.normalized * 1f;
                self.bodyChunks[1].vel -= pos.normalized * 0.5f;

                if (self.graphicsModule != null)
                {
                    for (int j = 0; j < this.legs.GetLength(0); j++)
                    {
                        for (int k = 0; k < 2; k++)
                        {
                            this.legs[j, k].mode = Limb.Mode.Dangle;
                            this.legFlips[j, k, 0] = ((j == 0) ? -1f : 1f);
                            this.legs[j, k].vel += Vector3.Slerp(Custom.DirVec(self.mainBodyChunk.pos, wantPos), 
                                                                 Custom.PerpendicularVector(self.mainBodyChunk.pos, wantPos) * ((j == 0) ? -1f : 1f), 
                                                                 (k == 0) ? 0.1f : 0.5f).ToVector2InPoints() * 3f;
                        }
                    }
                }

                if (this.Footing)
                {
                    this.jumping = false;
                }
                return;
            }
            if (self.Consious && !Custom.DistLess(self.mainBodyChunk.pos, self.mainBodyChunk.lastPos, 2f))
            {
                this.runCycle += 0.0625f;
            }
        }

        //跳跃
        public void Jump(Vector2 jumpDir, float soundVol)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            float scale = player.simulateHoldJumpButton >= 6 ? 2f : 1f;
            float num = Custom.AimFromOneVectorToAnother(player.bodyChunks[1].pos, player.bodyChunks[0].pos);
            if (player.killSuperLaunchJumpCounter > 0)
            {
                hasSuperLaunchJump = true;
            }
            /*if (killSuperLaunchJumpCounter > 0)
            {
                for (int i = 0; i < this.legs.GetLength(0); i++)
                    for (int j = 0; j < this.legs.GetLength(1); j++)
                        this.legs[i, j].FindGrip(player.room, player.bodyChunks[0].pos, player.bodyChunks[0].pos + 20f * Mathf.Abs(Mathf.Sin(num)) * Vector2.down, this.legLength * 1.1f, player.bodyChunks[0].pos + 100f * Mathf.Abs(Mathf.Sin(num)) * Vector2.down, -2, -2, false);
            }*/
            float d = Custom.LerpMap(jumpDir.y, -1f, 1f, 0.7f, 1.2f, 1.1f);
            this.footingCounter = 0;
            player.mainBodyChunk.vel *= 0.5f;
            player.bodyChunks[1].vel *= 0.5f;
            player.mainBodyChunk.vel += jumpDir * 8f * d * scale;
            player.bodyChunks[1].vel += jumpDir * 5.5f * d * scale;
            this.jumping = true;
            if (player.graphicsModule != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        this.legs[i, j].mode = Limb.Mode.Dangle;
                        this.legs[i, j].vel += jumpDir * 15f * ((j < 2) ? 1f : -1f) * scale;
                    }
                }
            }
            player.room.PlaySound(SoundID.Big_Spider_Jump, player.mainBodyChunk, false, soundVol, 1f);
        }

        private void Swim()
        {
            if (!ownerRef.TryGetTarget(out var self))
                return;
            if (self.graphicsModule != null)
            {
                this.flip = Mathf.Lerp(this.flip, wantPos.x - self.bodyChunks[0].pos.x, 0.25f);
            }
        }

        private void PlayerMoveByLegs()
        {
            if (!ownerRef.TryGetTarget(out var self) || this.legs == null)
                return;

            travelDir = (wantPos - self.bodyChunks[0].pos).normalized;
            wantPos = self.bodyChunks[0].pos + arthropodSpeed * new Vector2(self.input[0].x, self.input[0].y);
            /*
            wantPos += arthropodSpeed * new Vector2(self.input[0].x, self.input[0].y);
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
            }*/

            if (TileAccessibleToPlayer())
            {
                self.bodyChunks[0].vel *= Custom.LerpMap(self.bodyChunks[0].vel.magnitude, 1f, 6f, 0.99f, 0.9f);
                self.bodyChunks[0].vel += Vector2.ClampMagnitude(wantPos - self.bodyChunks[0].pos, arthropodSpeed) / arthropodSpeed * 3f;

                int num = 0;
                for (int m = 0; m < this.legs.GetLength(1); m++)
                {
                    for (int n = 0; n < this.legs.GetLength(0); n++)
                    {
                        if (!this.legs[n, m].reachedSnapPosition)
                        {
                            if (!Custom.DistLess(wantPos, self.bodyChunks[0].pos, 10f) && num < 4)
                            {
                                this.legs[n, m].vel = Vector2.Lerp(this.legs[n, m].vel, wantPos - this.legs[n, m].pos, 0.05f);
                                num++;
                            }
                        }
                    }
                }
                if (num == 0)
                {
                    int n = Random.Range(0, this.legs.GetLength(0));
                    int m = Random.Range(0, this.legs.GetLength(1));
                    this.legs[n, m].vel = Vector2.Lerp(this.legs[n, m].vel, wantPos - this.legs[n, m].pos, 0.1f);
                }
            }
            else
            {
                for (int m = 0; m < this.legs.GetLength(1); m++)
                {
                    for (int n = 0; n < this.legs.GetLength(0); n++)
                    {
                        if (this.legs[n, m].reachedSnapPosition)
                        {
                            self.bodyChunks[0].vel *= 0.9f;
                            return;
                        }
                    }
                }
            }
        }

        private bool TileAccessibleToPlayer()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return false;
            if (this.legs == null)
                return false;
            int num = 0;
            for (int l = 0; l < this.legs.GetLength(0); l++)
            {
                for (int m = 0; m < this.legs.GetLength(1); m++)
                {
                    if (this.legs[l, m].reachedSnapPosition &&
                        Custom.DistLess(player.bodyChunks[0].pos, this.legs[l, m].pos, 1.1f * this.legLength))
                    {
                        num++;
                    }
                }
            }
            if (num >= 2 ||
                (num >= 1 && (player.room.gravity <= 0.3f || player.Submersion >= 0.5f)))
                return true;
            return false;
        }

        public bool ShouldFired(Creature creature)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return false;

            bool shouldFire = !creature.dead;
            bool inRange = true;
            
            if (creature is Player)
                shouldFire = false;
            if (creature is Overseer && (creature as Overseer).AI.LikeOfPlayer(player.abstractCreature) > 0.5f)
            {
                shouldFire = false;
            }
            if (creature is Lizard)
            {
                foreach (RelationshipTracker.DynamicRelationship relationship in (creature as Lizard).AI.relationshipTracker.relationships.
                    Where((RelationshipTracker.DynamicRelationship m) => m.trackerRep.representedCreature == player.abstractCreature))
                {
                    if ((creature as Lizard).AI.LikeOfPlayer(relationship.trackerRep) > 0.5f)
                        shouldFire = false;
                }
            }
            if (creature is Scavenger &&
                (double)(creature as Scavenger).abstractCreature.world.game.session.creatureCommunities.
                LikeOfPlayer(CreatureCommunities.CommunityID.Scavengers,
                            (creature as Scavenger).abstractCreature.world.game.world.RegionNumber,
                            player.playerState.playerNumber) > 0.5)
            {
                shouldFire = false;
            }
            if (creature is Cicada)
            {
                foreach (RelationshipTracker.DynamicRelationship relationship in (creature as Cicada).AI.relationshipTracker.relationships.
                    Where((RelationshipTracker.DynamicRelationship m) => m.trackerRep.representedCreature == player.abstractCreature))
                {
                    if ((creature as Cicada).AI.LikeOfPlayer(relationship.trackerRep) > 0.5f)
                        shouldFire = false;
                }
            }

            return shouldFire && inRange;
        }

        #region 发射毒镖
        public void SpitUpdate()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (player.room == null)
                return;
            if (!this.IsSpitter)
                return;
            /*
            if (this.spitPos != null)
            {
                if (this.AbandonSitAndSpit())
                {
                    this.spitPos = null;
                    this.noSitDelay = 60;
                }
                else
                {
                    if (!Custom.DistLess(this.spitDir, this.aimDir, 0.3f))
                    {
                        this.spitDir = (this.spitDir + this.aimDir * 0.2f).normalized;
                    }
                    float dist = 25f;//this.bodyChunkConnections[0].distance
                    player.bodyChunks[0].vel *= 0.5f;
                    player.bodyChunks[1].vel -= this.spitDir;
                    Vector2 a = this.spitPos.Value + this.spitDir * Mathf.Lerp(5f, -5f, this.charging);
                    player.bodyChunks[1].pos = Vector2.Lerp(player.bodyChunks[1].pos, a + -this.spitDir * dist * 0.5f, 0.2f);
                    player.bodyChunks[0].vel *= 0.5f;
                    player.bodyChunks[0].vel += this.spitDir;
                    player.bodyChunks[0].pos = Vector2.Lerp(player.bodyChunks[0].pos, a + this.spitDir * dist * 0.5f, 0.2f);
                    this.footingCounter = 30;
                }
                //this.AI.Update();
            }
            else if (this.SitAndSpit())
            {
                this.spitPos = new Vector2?(player.bodyChunks[0].pos);
                this.spitDir = Custom.DirVec(player.bodyChunks[1].pos, player.bodyChunks[0].pos);
            }*/
            AmmoUpdate();

            if (this.charging > 0f)
            {
                if (player.room.aimap.getAItile(player.mainBodyChunk.pos).fallRiskTile.y > player.abstractCreature.pos.Tile.y - 20)
                {
                    player.bodyChunks[1].vel -= this.aimDir * this.charging * 2f;
                    player.bodyChunks[0].vel += this.aimDir * this.charging;
                }

                //身体几乎不再移动
                for (int j = 0; j < player.bodyChunks.Length; j++)
                {
                    player.bodyChunks[j].vel *= 0.05f;
                }
                if (player.bodyMode == Player.BodyModeIndex.Stand)
                    player.bodyMode = Player.BodyModeIndex.Default;
                else if (player.bodyMode == Player.BodyModeIndex.Swimming)
                    player.bodyMode = Player.BodyModeIndex.Default;

                this.spitDir = (this.spitDir + this.aimDir * 0.2f).normalized;
                this.aimDir = AimDir(1f);
                if (this.charging > 1f && !BuffInput.GetKey(BuffPlayerData.Instance.GetKeyBind(SpiderShapedMutationBuffEntry.SpiderShapedMutation)))
                {
                    this.Spit();
                }
            }
            if (BuffInput.GetKey(BuffPlayerData.Instance.GetKeyBind(SpiderShapedMutationBuffEntry.SpiderShapedMutation)) &&
                this.CanSpit(false))
                this.charging += 0.05f;
            else
                this.charging = 0f;
            /*
            if (this.spitPos != null || this.charging > 0f)
            {
                return;
            }
            */

            SpiderShapedMutationBuffEntry.UpdateSpeed = 1000;
            if (this.charging > 0)
            {
                //时缓
                for (int j = 0; j < player.room.game.AlivePlayers.Count; j++)
                {
                    if (player.room.game.AlivePlayers[j].realizedCreature != null &&
                         SpiderShapedMutationBuffEntry.SpiderCatFeatures.TryGetValue(player.room.game.AlivePlayers[j].realizedCreature as Player, out var spiderCat))
                    {
                        SpiderShapedMutationBuffEntry.UpdateSpeed = 10;
                        break;
                    }
                }
            }
        }

        public void Spit()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            Vector2 shootDir = this.aimDir;
            if (shootDir == Vector2.zero)
                shootDir = this.travelDir.normalized;
            Creature creature = null;
            float num = float.MaxValue;
            float current = Custom.VecToDeg(shootDir);
            for (int i = 0; i < player.abstractCreature.Room.creatures.Count; i++)
            {
                if (player.abstractCreature != player.abstractCreature.Room.creatures[i] && player.abstractCreature.Room.creatures[i].realizedCreature != null)
                {
                    float target = Custom.AimFromOneVectorToAnother(player.mainBodyChunk.pos, player.abstractCreature.Room.creatures[i].realizedCreature.mainBodyChunk.pos);
                    float num2 = Custom.Dist(player.mainBodyChunk.pos, player.abstractCreature.Room.creatures[i].realizedCreature.mainBodyChunk.pos);
                    if (Mathf.Abs(Mathf.DeltaAngle(current, target)) < 22.5f && num2 < num)
                    {
                        num = num2;
                        creature = player.abstractCreature.Room.creatures[i].realizedCreature;
                    }
                }
            }
            if (creature != null)
            {
                shootDir = Custom.DirVec(player.mainBodyChunk.pos, creature.mainBodyChunk.pos);
            }
            this.charging = 0f;
            player.mainBodyChunk.pos += shootDir * 12f;
            player.mainBodyChunk.vel += shootDir * 2f;
            AbstractPhysicalObject absPhysicalObject = new AbstractPhysicalObject(player.room.world, AbstractPhysicalObject.AbstractObjectType.DartMaggot, null, player.abstractCreature.pos, player.room.game.GetNewID());
            absPhysicalObject.RealizeInRoom();
            (absPhysicalObject.realizedObject as DartMaggot).Shoot(player.mainBodyChunk.pos, shootDir, player);
            player.room.PlaySound(SoundID.Big_Spider_Spit, player.mainBodyChunk);
            this.SpiderHasSpit();
        }

        public void SpiderHasSpit()
        {
            this.ammo--;
            this.ammoRegen = 0f;
            if (this.ammo < 1)
                this.fastAmmoRegen = true;
            else
                this.fastAmmoRegen = false;
        }

        public void AmmoUpdate()
        {
            if (this.ammo < 4)
            {
                this.ammoRegen += 1f / (this.fastAmmoRegen ? 60f : 1200f);
                if (this.ammoRegen > 1f)
                {
                    this.ammo++;
                    this.ammoRegen -= 1f;
                    if (this.ammo > 3)
                    {
                        this.ammoRegen = 0f;
                        this.fastAmmoRegen = false;
                    }
                }
            }
        }

        public Vector2 AimDir(float timeStacker)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return Vector2.zero;
            if (this.charging > 0f)
            {
                Vector2 inputDir = new Vector2(player.input[0].x, player.input[0].y).normalized;
                wantShootDir = Vector3.Slerp(wantShootDir, inputDir, 0.02f);
            }
            else
            {
                Vector2 vector = LookDirection;
                wantShootDir = Vector2.Lerp(wantShootDir, vector, 0.02f);
            }
            return wantShootDir.normalized;
        }

        public bool CanSpit(bool initiate)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return false;
            return player.Consious && player.grasps[0] == null && IsSpitter && this.ammo > 0;
        }

        public bool AbandonSitAndSpit()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return false;
            Vector2 spitPos = this.spitPos == null ? Vector2.zero : this.spitPos.Value;
            return !this.goToSpitPos || !Custom.DistLess(player.mainBodyChunk.pos, spitPos, 120f) || 
                   this.spitAtCrit == null || this.spitAtCrit.TicksSinceSeen > 20 || //this.bugAI.behavior != BigSpiderAI.Behavior.Hunt || 
                   Custom.DistLess(spitPos, this.targetPoint, 220f);
        }

        public bool SitAndSpit()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return false;
            Vector2 spitPos = this.spitPos == null ? Vector2.zero : this.spitPos.Value;
            return this.goToSpitPos && this.noSitDelay < 1 && //this.bugAI.behavior == BigSpiderAI.Behavior.Hunt && 
                    Custom.DistLess(player.bodyChunks[0].pos, spitPos, 80f) && 
                    !Custom.DistLess(spitPos, this.targetPoint, 300f) && this.spitAtCrit != null && 
                    this.spitAtCrit.VisualContact && 
                    (player.room.aimap.TileAccessibleToCreature(player.bodyChunks[0].pos, player.Template) || 
                     player.room.aimap.TileAccessibleToCreature(player.bodyChunks[1].pos, player.Template)) && 
                    player.room.VisualContact(player.bodyChunks[0].pos, this.spitAtCrit.representedCreature.realizedCreature.DangerPos);
        }
        #endregion

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
        public Player.ObjectGrabability Grabability(Player.ObjectGrabability result, PhysicalObject obj)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return result;

            if (result == Player.ObjectGrabability.OneHand)
                result = Player.ObjectGrabability.BigOneHand;
            else if (result == Player.ObjectGrabability.BigOneHand)
                result = Player.ObjectGrabability.BigOneHand;
            else if (result == Player.ObjectGrabability.TwoHands)
                result = Player.ObjectGrabability.Drag;
            else if (result == Player.ObjectGrabability.Drag)
                result = Player.ObjectGrabability.Drag;

            //允许抓住其他生物（不含小生物）
            if (this.LikeSpider && obj is Creature creature && 
                player.dontGrabStuff < 1 && player.grabbedBy.Count <= 0 &&
                creature != player && !creature.Template.smallCreature &&
                result == Player.ObjectGrabability.CantGrab)
                result = Player.ObjectGrabability.Drag;

            return result;
        }
        #endregion
    }
}
