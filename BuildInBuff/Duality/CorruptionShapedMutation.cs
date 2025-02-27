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
using Color = UnityEngine.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using MoreSlugcats;
using HotDogGains.Positive;
using RandomBuff.Core.SaveData;
using BuiltinBuffs.Positive;
using System.Collections.Generic;
using System.IO;

namespace BuiltinBuffs.Duality
{
    internal class CorruptionShapedMutationBuff : Buff<CorruptionShapedMutationBuff, CorruptionShapedMutationBuffData>
    {
        public override BuffID ID => CorruptionShapedMutationBuffEntry.CorruptionShapedMutation;

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

        public int CorruptionLevel
        {
            get
            {
                int num = 0;
                if (GetTemporaryBuffPool().allBuffIDs.Contains(BuiltinBuffs.Negative.CorruptionSpreadBuffEntry.corruptionSpread))
                    num++;
                return num;
            }
        }

        public static int corruptionLevel;

        public CorruptionShapedMutationBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    if (CorruptionShapedMutationBuffEntry.CorruptionCatFeatures.TryGetValue(player, out _))
                        CorruptionShapedMutationBuffEntry.CorruptionCatFeatures.Remove(player);
                    var corruption = new CorruptionCat(player);
                    CorruptionShapedMutationBuffEntry.CorruptionCatFeatures.Add(player, corruption);
                    //corruption.CorruptionArthropod(player.graphicsModule as PlayerGraphics);
                    corruption.graphics.InitiateSprites(game.cameras[0].spriteLeasers.
                        First(i => i.drawableObject == player.graphicsModule), game.cameras[0]);
                }
                CorruptionShapedMutationBuffEntry.EstablishRelationship();
            }
        }

        public override void Update(RainWorldGame game)
        {
            base.Update(game);
            corruptionLevel = CorruptionLevel;
        }
    }

    internal class CorruptionShapedMutationBuffData : BuffData
    {
        public override BuffID ID => CorruptionShapedMutationBuffEntry.CorruptionShapedMutation;
    }

    internal class CorruptionShapedMutationBuffEntry : IBuffEntry
    {
        public static BuffID CorruptionShapedMutation = new BuffID("CorruptionShapedMutation", true);

        public static ConditionalWeakTable<Player, CorruptionCat> CorruptionCatFeatures = new ConditionalWeakTable<Player, CorruptionCat>();

        public static int StackLayer
        {
            get
            {
                return CorruptionShapedMutation.GetBuffData().StackLayer;
            }
        }

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<CorruptionShapedMutationBuff, CorruptionShapedMutationBuffData, CorruptionShapedMutationBuffEntry>(CorruptionShapedMutation);
        }

        public static void HookOn()
        {
            IL.RainWorldGame.RawUpdate += RainWorldGame_RawUpdateIL;

            IL.Player.MovementUpdate += Player_MovementUpdateIL;

            On.SlugcatStats.SlugcatCanMaul += SlugcatStats_SlugcatCanMaul;
            On.Player.IsCreatureLegalToHoldWithoutStun += Player_IsCreatureLegalToHoldWithoutStun;
            On.Player.CanEatMeat += Player_CanEatMeat;
            On.SlugcatStats.NourishmentOfObjectEaten += SlugcatStats_NourishmentOfObjectEaten;
            On.MoreSlugcats.SlugNPCAI.TheoreticallyEatMeat += SlugNPCAI_TheoreticallyEatMeat;

            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.Player.Collide += Player_Collide;
            On.Player.Grabability += Player_Grabability;
            On.Player.MovementUpdate += Player_MovementUpdate;
            On.Player.FreeHand += Player_FreeHand;
            On.Player.Jump += Player_Jump;
            On.Player.NewRoom += Player_NewRoom;
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
        //不抓杆子
        private static void Player_MovementUpdateIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                              i => i.Match(OpCodes.Ldc_I4_1),
                              i => i.Match(OpCodes.Br_S),
                              i => i.Match(OpCodes.Ldc_I4_0),
                              i => i.MatchStfld<Player>("wantToGrab")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<Player>>((player) =>
                {
                    if (CorruptionCatFeatures.TryGetValue(player, out _))
                    {
                        player.wantToGrab = 0;
                    }
                });
            }
            else
                BuffUtils.LogError(CorruptionCatFeatures, "IL HOOK FAILED");
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
            if (CorruptionCatFeatures.TryGetValue(self, out var corruption))
                result = true;
            return result;
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

        //食量增大（需求+4，存储-2）
        private static IntVector2 SlugcatStats_SlugcatFoodMeter(On.SlugcatStats.orig_SlugcatFoodMeter orig, SlugcatStats.Name slugcat)
        {
            IntVector2 origFoodRequirement = orig(slugcat);
            int newHibernateRequirement = origFoodRequirement.y + (StackLayer >= 3 ? 6 : 4);
            int newTotalFoodRequirement = origFoodRequirement.x + (StackLayer >= 3 ? 3 : 2);

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
            if (CorruptionCatFeatures.TryGetValue(self.owner.owner as Player, out var corruption))
                (self.owner.owner as Player).craftingObject = true;
            orig(self);

            if (CorruptionCatFeatures.TryGetValue(self.owner.owner as Player, out corruption))
                corruption.SlugcatHandUpdate(self);
        }

        //只能一次叼一个东西
        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            Player.ObjectGrabability result = orig(self, obj);

            if (CorruptionCatFeatures.TryGetValue(self, out var corruption))
            {
                result = corruption.Grabability(result, obj);
            }

            return result;
        }

        private static int Player_FreeHand(On.Player.orig_FreeHand orig, Player self)
        {
            int result = orig(self);
            if (CorruptionCatFeatures.TryGetValue(self, out var corruption))
                if (self.grasps[0] != null || self.grasps[1] != null)
                {
                    result = -1;
                }
            return result;
        }
        #endregion
        #region 生物关系
        //修改生物关系（棕色长腿菌、猎手长腿菌不再攻击玩家，其他生物对蛞蝓猫的生物关系变成对棕色长腿菌的生物关系）
        public static void EstablishRelationship()
        {
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.BrotherLongLegs, CreatureTemplate.Type.Slugcat, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f));
            StaticWorld.EstablishRelationship(MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy, CreatureTemplate.Type.Slugcat, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f));

            //其他生物对蛞蝓猫的生物关系变成对棕色长腿菌的生物关系
            CreatureTemplate bro = StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.BrotherLongLegs);
            CreatureTemplate slug = StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat);
            if (bro == null || slug == null || slug.type.Index == -1)
            {
                return;
            }
            foreach (CreatureTemplate other in StaticWorld.creatureTemplates)
            {
                if (other != null)
                {
                    StaticWorld.EstablishRelationship(other.type, slug.type, other.relationships[bro.type.Index]);
                }
            }
        }
        #endregion

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!CorruptionCatFeatures.TryGetValue(self, out _))
            {
                CorruptionCatFeatures.Add(self, new CorruptionCat(self));
                EstablishRelationship();
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (CorruptionCatFeatures.TryGetValue(self, out var corruption))
            {
                corruption.Update();
                self.GetExPlayerData().HaveHands = false;
            }
        }

        private static void Player_Collide(On.Player.orig_Collide orig, Player self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            orig(self, otherObject, myChunk, otherChunk);
            if (CorruptionCatFeatures.TryGetValue(self, out var corruption))
            {
            }
        }

        private static void Player_Jump(On.Player.orig_Jump orig, Player self)
        {/*
            if (CorruptionCatFeatures.TryGetValue(self, out var corruption))
                return;*/
            orig(self);
            if (CorruptionCatFeatures.TryGetValue(self, out var corruption))
            {
                self.bodyChunks[0].vel = self.bodyChunks[0].vel.normalized * Custom.LerpMap(self.bodyChunks[0].vel.magnitude, 0f, 5f, 0f, 1f);
                self.bodyChunks[1].vel = self.bodyChunks[0].vel.normalized * Custom.LerpMap(self.bodyChunks[1].vel.magnitude, 0f, 5f, 0f, 1f);
            }
        }

        private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            if (CorruptionCatFeatures.TryGetValue(self, out var corruption))
                corruption.MovementUpdate(orig, eu);
            orig(self, eu);
        }

        public static void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            if (CorruptionCatFeatures.TryGetValue(self, out var corruption))
            {
                corruption.NewRoom(newRoom);
            }
            orig(self, newRoom);
        }

        #region 外观
        private static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (CorruptionCatFeatures.TryGetValue(self.player, out var corruption))
                corruption.graphics.ApplyPalette(sLeaser, rCam, palette);
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (CorruptionCatFeatures.TryGetValue(self.player, out var corruption))
                corruption.graphics.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (CorruptionCatFeatures.TryGetValue(self.player, out var corruption))
            {
                corruption.graphics.InitiateSprites(sLeaser, rCam);
            }
        }

        private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (CorruptionCatFeatures.TryGetValue(self.player, out var corruption))
                corruption.graphics.AddToContainer(sLeaser, rCam, newContatiner);
        }

        private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if (CorruptionCatFeatures.TryGetValue(self.player, out var corruption))
            {

            }
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            if (CorruptionCatFeatures.TryGetValue(self.player, out var corruption))
                corruption.graphics.GraphicsUpdate();
        }

        private static void PlayerGraphics_Reset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig(self);
            if (CorruptionCatFeatures.TryGetValue(self.player, out var corruption))
                corruption.Reset(self);
        }
        #endregion
        #region 时缓
        private static void RainWorldGame_RawUpdateIL(ILContext il)
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
                BuffUtils.LogError(CorruptionShapedMutation, "IL HOOK FAILED");
        }

        public static int UpdateSpeed = 1000;
        #endregion
    }

    internal class CorruptionCat
    {
        WeakReference<Player> ownerRef;
        private int origThrowingSkill;

        public bool IsDaddy => CorruptionShapedMutationBuffEntry.StackLayer >= 2;

        public bool IsTerror => CorruptionShapedMutationBuffEntry.StackLayer >= 3;

        public CreatureTemplate.Type Type
        {
            get
            {
                switch (CorruptionShapedMutationBuffEntry.StackLayer)
                {
                    case 0:
                    case 1:
                        return CreatureTemplate.Type.BrotherLongLegs;
                    case 2:
                        return CreatureTemplate.Type.DaddyLongLegs;
                    case 3:
                        return MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs;
                    default:
                        return MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs;
                }
            }
        }

        #region 行动
        private float moveSpeed;
        private Vector2 bodyWantPos;
        private Vector2 wantPos;
        bool wantPosIsSetX;
        bool wantPosIsSetY;
        public Vector2 moveDirection;
        public int stuckCounter;
        public float totalGrip;
        public float moveDirGrip;
        public int outRangeCount;

        private float MassFac
        {
            get
            {
                if (!ownerRef.TryGetTarget(out var player))
                    return 1f;

                float graspMass = 0f;
                if (player.grasps != null)
                {
                    for (int i = 0; i < player.grasps.Length; i++)
                    {
                        if (player.grasps[i] != null)
                            graspMass += player.grasps[i].grabbed.TotalMass;
                    }
                }

                float result = (player.TotalMass + graspMass) / player.slugcatStats.runspeedFac;
                return result;
            }
        }

        private bool MassFacCondition
        {
            get
            {
                if (!ownerRef.TryGetTarget(out var player))
                    return false;
                return MassFac >= 3f + 1f * CorruptionShapedMutationBuff.corruptionLevel;
            }
        }

        public float DefaultMoveSpeed
        {
            get
            {
                return 4f + 2f * CorruptionShapedMutationBuff.Instance.SpeedLevel + 1f * CorruptionShapedMutationBuff.corruptionLevel;
            }
        }
        #endregion
        #region 外观
        public CorruptionCatGraphics graphics;
        public CorruptionCatTentacles[] tentacles;
        public int totalLegSprites;
        public Color EffectColor => Color.blue;
        #endregion

        public CorruptionCat(Player player)
        {
            this.ownerRef = new WeakReference<Player>(player);
            this.origThrowingSkill = player.slugcatStats.throwingSkill;
            this.moveSpeed = DefaultMoveSpeed;
            this.tentacles = new CorruptionCatTentacles[6];
            for (int i = 0; i < 6; i++)
            {
                this.tentacles[i] = new CorruptionCatTentacles(player, this, player.bodyChunks[1], 300f, 0, Custom.DegToVec(60f * i + 30f));
            }
            this.graphics = new CorruptionCatGraphics(player, this);
            this.wantPos = player.mainBodyChunk.pos;
            this.bodyWantPos = player.mainBodyChunk.pos;
        }

        //进行更新
        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (player.room == null)
                return;

            player.standing = false;
            player.slugcatStats.throwingSkill = Mathf.Max(-1, origThrowingSkill - VultureShapedMutationBuffEntry.StackLayer + 1);

            if (player.grasps[0] != null && player.grasps[1] != null)
            {
                player.ReleaseGrasp(1);
            }

            if (player.graphicsModule != null && player.Consious && (player.room.aimap == null ||
                (!player.room.aimap.TileAccessibleToCreature(player.mainBodyChunk.pos, player.Template) &&
                !player.room.aimap.TileAccessibleToCreature(player.bodyChunks[1].pos, player.Template))))
            {
                for (int l = 0; l < this.tentacles.GetLength(0); l++)
                {
                    if (this.tentacles[l].chunksGripping > 0.1f &&
                        (!TileAccessibleToPlayer() ||
                        (Custom.DistLess(wantPos, player.mainBodyChunk.pos, 20f) &&
                        Vector2.Dot(wantPos - player.mainBodyChunk.pos, this.tentacles[l].idealGrabPos - player.mainBodyChunk.pos) < 0 &&
                        !Custom.DistLess(player.mainBodyChunk.pos, this.tentacles[l].idealGrabPos, this.tentacles[l].idealLength * 0.7f) &&
                        Custom.DistLess(player.mainBodyChunk.pos, this.tentacles[l].idealGrabPos, this.tentacles[l].idealLength * 0.7f + 15f) &&
                        (player.room.gravity > 0.3f && player.Submersion < 0.5f))))
                    {
                        Vector2 a = Custom.DirVec(player.mainBodyChunk.pos, this.tentacles[l].idealGrabPos);// * 
                            //(Vector2.Distance(player.mainBodyChunk.pos, this.tentacles[l].idealGrabPos) - this.tentacles[l].idealLength * 0.7f);
                        player.mainBodyChunk.pos += a * 0.8f * this.tentacles[l].chunksGripping;
                        player.mainBodyChunk.vel += a * 0.8f * this.tentacles[l].chunksGripping;
                    }
                }
            }

            if (player.Consious)
            {
                this.PlayerMoveByLegs();
            }

            foreach (var tentacle in tentacles)
            {
                tentacle.Update();
            }

            player.bodyMode = Player.BodyModeIndex.Default;
            player.animation = Player.AnimationIndex.None;
        }

        //调整姿势
        public void MovementUpdate(On.Player.orig_MovementUpdate orig, bool eu)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (!player.Consious) return;

            player.bodyMode = Player.BodyModeIndex.Default;
            player.animation = Player.AnimationIndex.None;
        }

        private void PlayerMoveByLegs()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            /*
            wantPos = player.bodyChunks[0].lastPos + moveSpeed * new Vector2(player.input[0].x, player.input[0].y);
            moveDirection = (wantPos - player.bodyChunks[0].lastPos).normalized;
            */
            float massSpeedFac = 1f;
            if (player.aerobicLevel >= 0.5f && MassFacCondition)
            {
                massSpeedFac = Custom.LerpMap(player.aerobicLevel, 0.5f, 1f, 1f, 0f);
                moveSpeed = Custom.LerpMap(player.aerobicLevel, 0.5f, 1f, DefaultMoveSpeed, 0f);
                wantPos += Custom.LerpMap(player.aerobicLevel, 0.5f, 1f, 0f, 4f) * Vector2.down;
            }
            else
            {
                moveSpeed = DefaultMoveSpeed;
            }

            wantPos += moveSpeed * new Vector2(player.input[0].x, player.input[0].y);//player.bodyChunks[0].pos + 
            if (player.input[0].x == 0 && !wantPosIsSetX)
            {
                player.bodyChunks[0].vel.x *= 0.8f;
                player.bodyChunks[1].vel.x *= 0.8f;
                if (Mathf.Abs(player.bodyChunks[0].vel.x) < 1f)
                {
                    wantPosIsSetX = true;
                    wantPos.x = player.bodyChunks[0].pos.x;
                }
            }
            if (player.input[0].y == 0 && !wantPosIsSetY)
            {
                player.bodyChunks[0].vel.y *= 0.8f;
                player.bodyChunks[1].vel.y *= 0.8f;
                if (Mathf.Abs(player.bodyChunks[0].vel.y) < 1f)
                {
                    wantPosIsSetY = true;
                    wantPos.y = player.bodyChunks[0].pos.y;
                }
            }
            if (player.input[0].x != 0 || Mathf.Abs(wantPos.x - player.bodyChunks[0].pos.x) > 100f)
            {
                wantPosIsSetX = false;
            }
            if (player.input[0].y != 0 || Mathf.Abs(wantPos.y - player.bodyChunks[0].pos.y) > 100f)
            {
                wantPosIsSetY = false;
            }
            moveDirection = (wantPos - player.bodyChunks[0].pos).normalized;
            if (player.input[0].jmp && TileAccessibleToPlayer()) 
                bodyWantPos = wantPos;
            if (true)//TileAccessibleToPlayer()
            {
                this.totalGrip = 0f;
                this.moveDirGrip = 0f;
                for (int n = 0; n < this.tentacles.GetLength(0); n++)
                    this.totalGrip += this.tentacles[n].chunksGripping;
                
                for (int n = 0; n < this.tentacles.GetLength(0); n++)
                {
                    if (Vector2.Dot(moveDirection, this.tentacles[n].Tip.pos - player.mainBodyChunk.pos) > 0)
                    {
                        moveDirGrip += this.tentacles[n].chunksGripping;
                    }
                }

                int num = 0;
                this.outRangeCount = 0;
                for (int n = 0; n < this.tentacles.GetLength(0); n++)
                {
                    if (this.tentacles[n].chunksGripping > 0.2f || Vector2.Dot(moveDirection, this.tentacles[n].Tip.pos - player.mainBodyChunk.pos) > 0)
                        this.tentacles[n].chooseToMoveByPlayer = false;
                    bool addToMove = !Custom.DistLess(wantPos, player.bodyChunks[0].pos, 10f) && num < 3 &&
                        this.tentacles[n].chunksGripping <= 0.2f && Vector2.Dot(moveDirection, this.tentacles[n].Tip.pos - player.mainBodyChunk.pos) < 0;
                    if (addToMove || this.tentacles[n].chooseToMoveByPlayer)
                    {
                        this.tentacles[n].chooseToMoveByPlayer = true;
                        num++;
                    }
                    else
                    {
                        this.tentacles[n].chooseToMoveByPlayer = false;
                    }

                    if (Custom.Dist(this.tentacles[n].Tip.pos, player.mainBodyChunk.pos) > 0.65f * this.tentacles[n].idealLength &&
                        Vector2.Dot(moveDirection, this.tentacles[n].Tip.pos - player.mainBodyChunk.pos) < 0)
                        outRangeCount++;
                }
                if (num == 0)
                {
                    int n = Random.Range(0, this.tentacles.GetLength(0));
                    this.tentacles[n].chooseToMoveByPlayer = true;
                }

                //随机速度
                //player.bodyChunks[0].vel += Custom.RNV() * Random.value * 0.5f;
                player.bodyChunks[1].vel *= 0.8f;
                //按住跳跃键时，可以按方向键使核心移动
                if (player.input[0].jmp && TileAccessibleToPlayer())
                {
                    player.bodyChunks[0].vel *= Custom.LerpMap(player.bodyChunks[0].vel.magnitude, 1f, 6f, 0.99f, 0.9f);
                    player.bodyChunks[0].vel += Mathf.Clamp01((this.totalGrip + 2f * moveDirGrip) / 3f) * Vector2.ClampMagnitude(wantPos - player.bodyChunks[0].pos, moveSpeed) / moveSpeed * 3f;
                }
                //不按跳跃键时，核心位置不移动
                else
                {
                    player.bodyChunks[0].vel *= Custom.LerpMap(player.bodyChunks[0].vel.magnitude, 1f, 6f, 0.99f, 0.95f);
                    player.bodyChunks[0].vel += Mathf.Clamp01((this.totalGrip + 2f * moveDirGrip) / 3f) * Vector2.ClampMagnitude(bodyWantPos - player.bodyChunks[0].pos, moveSpeed) / moveSpeed * 3f;
                }
            }
            else
            {
                for (int n = 0; n < this.tentacles.GetLength(0); n++)
                {
                    if (this.tentacles[n].chunksGripping > 0.1f)
                    {
                        player.bodyChunks[0].vel *= 0.9f;
                        return;
                    }
                }
            }
        }

        private bool TileAccessibleToPlayer()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return false;
            if (this.tentacles == null)
                return false;
            this.totalGrip = 0;
            for (int l = 0; l < this.tentacles.GetLength(0); l++)
            {
                this.totalGrip += this.tentacles[l].chunksGripping;
            }
            BuffPlugin.Log("this.totalGrip + 2f * moveDirGrip: " + (this.totalGrip + 2f * moveDirGrip));
            if (this.totalGrip > 0 &&
                this.totalGrip + 2f * moveDirGrip >= 5f * player.room.gravity - 4.5f * player.Submersion &&
                outRangeCount < (float)this.tentacles.GetLength(0) / 3f)
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

        public void Reset(PlayerGraphics self)
        {
            //防止拉丝
            foreach (var tentacle in tentacles)
            {
                tentacle.Reset(tentacle.connectedChunk.pos);
            }
        }

        public void NewRoom(Room newRoom)
        {
            foreach (var tentacle in tentacles)
            {
                tentacle.NewRoom(newRoom);
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

            return result;
        }
        #endregion
    }

    internal class CorruptionCatGraphics
    {
        public Player player;
        public CorruptionCat corruptionCat;

        public bool IsDaddy => CorruptionShapedMutationBuffEntry.StackLayer >= 2;

        public bool IsTerror => CorruptionShapedMutationBuffEntry.StackLayer >= 3;

        #region 外观
        public CorruptionCatLegGraphics[] legGraphics;
        public int totalLegSprites;
        public int originLength;
        public Color EffectColorA => PlayerGraphics.DefaultSlugcatColor(player.SlugCatClass);
        public Color EffectColorB => PlayerGraphics.JollyUniqueColorMenu(player.SlugCatClass,
                                                        player.abstractCreature.world.game.rainWorld.options.jollyPlayerOptionsArray[player.playerState.playerNumber].playerClass,
                                                        player.playerState.playerNumber);
        #endregion

        public CorruptionCatGraphics(Player player, CorruptionCat corruptionCat)
        {
            this.player = player;
            this.corruptionCat = corruptionCat;
            this.totalLegSprites = 0;
            this.legGraphics = new CorruptionCatLegGraphics[corruptionCat.tentacles.Length];
        }

        #region 外观
        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            this.originLength = sLeaser.sprites.Length;
            this.totalLegSprites = 0;
            for (int i = 0; i < this.legGraphics.Length; i++)
            {
                this.legGraphics[i] = new CorruptionCatLegGraphics(this, i, this.originLength + this.totalLegSprites);// + 12
                this.totalLegSprites += this.legGraphics[i].sprites;
            }
            Array.Resize(ref sLeaser.sprites, this.originLength + this.totalLegSprites);

            foreach (var tentacle in legGraphics)
            {
                tentacle.InitiateSprites(sLeaser, rCam);
            }

            this.AddToContainer(sLeaser, rCam, null);
            self.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            foreach (var tentacle in legGraphics)
            {
                tentacle.ApplyPalette(sLeaser, rCam, palette);
            }
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            if (self == null || corruptionCat == null)
                return;
            if (originLength >= 1 && sLeaser.sprites.Length >= originLength + totalLegSprites)
            {
                var foregroundContainer = rCam.ReturnFContainer("Foreground");
                var midgroundContainer = newContatiner != null ? newContatiner : rCam.ReturnFContainer("Midground");

                for (int i = 0; i < totalLegSprites; i++)
                {
                    var sprite = sLeaser.sprites[originLength + i];
                    sprite.RemoveFromContainer();
                    midgroundContainer.AddChild(sprite);
                }
            }
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (player.graphicsModule == null || sLeaser == null || player.room == null)
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;

            if (sLeaser.sprites.Length >= 9)
                for (int i = 4; i <= 8; i++)
                    sLeaser.sprites[i].isVisible = false;
            foreach (var tentacle in legGraphics)
            {
                tentacle.DrawSprite(sLeaser, rCam, timeStacker, camPos);
            }
        }

        public void GraphicsUpdate()
        {
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            foreach (var tentacle in legGraphics)
            {
                tentacle.Update();
            }
        }
        #endregion
    }

    internal class CorruptionCatTentacles : Tentacle
    {
        //attr
        public Player player => owner as Player;
        public CorruptionCat corruptionCat;
        public bool chooseToMoveByPlayer;

        //var
        public int stun;
        public int tentacleNumber;
        public int secondaryGrabBackTrackCounter;
        private IntVector2 secondaryGrabPos;
        public int foundNoGrabPos;
        public int[] chunksStickSounds;

        public bool atGrabDest;
        public bool lastBackTrack;
        public bool neededForLocomotion;

        public float awayFromBodyRotation;
        public float chunksGripping;


        public Vector2 tentacleDir;
        public Vector2 idealGrabPos;
        public Task task;

        public BodyChunk grabChunk;
        public Tracker.CreatureRepresentation huntCreature;
        private Vector2 preliminaryGrabDest;

        private IntVector2[] _cachedRays1 = new IntVector2[200];
        private readonly List<IntVector2> _cachedRays2 = new List<IntVector2>(10);

        public CorruptionCatTentacles(Player player, CorruptionCat corruptionCat, BodyChunk bodyChunk, float length, int tentacleNumber, Vector2 tentacleDir) : base(player, bodyChunk, length)
        {
            base.connectedChunk = bodyChunk;
            this.corruptionCat = corruptionCat;
            this.tentacleNumber = tentacleNumber;
            this.tentacleDir = tentacleDir;
            this.room = player.room;
            this.grabPath = new List<IntVector2>();
            this.segments = new List<IntVector2>();
            for (int i = 0; i < (int)(this.idealLength / 20f); i++)
            {
                this.segments.Add(player.abstractCreature.pos.Tile);//room.GetTilePosition(this.owner.firstChunk.pos)
            }
            tProps = new Tentacle.TentacleProps(false, true, false, 0.5f, 0f, 0f, 0f, 0f, 3.2f, 10f, 0.25f, 5f, 15, 60, 12, 20);
            tChunks = new Tentacle.TentacleChunk[(int)(length / 40f)];
            for (int i = 0; i < this.tChunks.Length; i++)
            {
                tChunks[i] = new Tentacle.TentacleChunk(this, i, (float)(i + 1) / (float)this.tChunks.Length, 3f);
                tChunks[i].PhaseToSegment();
                tChunks[i].Reset();
            }
            chunksStickSounds = new int[this.tChunks.Length];
        }

        public override void Update()
        {
            base.Update();
            if (this.stun > 0)
            {
                this.stun--;
            }

            limp = false;

            foreach (var chunk in tChunks)
            {
                chunk.vel *= 0.9f;
                if (limp)
                {
                    chunk.vel.y -= 0.5f;
                }
            }

            if (limp)
            {
                return;
            }
            this.atGrabDest = false;
            if (this.backtrackFrom > -1)
            {
                this.secondaryGrabBackTrackCounter++;
                if (!this.lastBackTrack)
                {
                    this.secondaryGrabBackTrackCounter += 20;
                }
            }
            this.lastBackTrack = (this.backtrackFrom > -1);
            Vector2 centerPos = player.mainBodyChunk.pos;
            for (int k = 1; k < player.bodyChunks.Length; k++)
            {
                centerPos += player.bodyChunks[k].pos;
            }
            centerPos /= (float)player.bodyChunks.Length;
            //Vector2 centerPos = player.bodyChunks[0].pos;
            awayFromBodyRotation = Custom.AimFromOneVectorToAnother(centerPos, this.connectedChunk.pos);
            chunksGripping = 0f;

            Climb(ref this.scratchPath);
            for (int m = 0; m < this.tChunks.Length; m++)
            {
                float num4 = (float)m / (float)(this.tChunks.Length - 1);
                if (num4 < 0.2f)
                {
                    this.tChunks[m].vel += Custom.DegToVec(this.awayFromBodyRotation) * Mathf.InverseLerp(0.2f, 0f, num4) * 5f;
                }
                for (int n = m + 1; n < this.tChunks.Length; n++)
                {
                    base.PushChunksApart(m, n);
                }
            }
        }

        public void Climb(ref List<IntVector2> path)
        {
            float t = Custom.LerpMap(player.mainBodyChunk.vel.magnitude, 0f, 10f, 0.5f, 0.95f);
            if (this.chooseToMoveByPlayer)
                t = 1f;
            //float t = Custom.LerpMap((float)this.corruptionCat.stuckCounter, 50f, 200f, 0.5f, 0.95f);
            Vector2 moveDirection = this.corruptionCat.moveDirection;
            if (this.chooseToMoveByPlayer || this.corruptionCat.totalGrip > 0.4f)
            this.idealGrabPos = base.FloatBase + (Vector2)Vector3.Slerp(this.tentacleDir, moveDirection, t) * this.idealLength * 0.7f;
            Vector2 vector = base.FloatBase + 
                (Vector2)Vector3.Slerp(Vector3.Slerp(this.tentacleDir, moveDirection, t), Custom.RNV(), Mathf.InverseLerp(20f, 200f, (float)this.foundNoGrabPos)) * 
                this.idealLength * Custom.LerpMap((float)Math.Max(this.foundNoGrabPos, this.corruptionCat.stuckCounter), 20f, 200f, 0.7f, 1.2f);
            int i;
            for (i = SharedPhysics.RayTracedTilesArray(base.FloatBase, vector, this._cachedRays1); i >= this._cachedRays1.Length; i = SharedPhysics.RayTracedTilesArray(base.FloatBase, vector, this._cachedRays1))
            {
                Custom.LogWarning(new string[]
                {
                string.Format("DaddyTentacle Climb ray tracing limit exceeded, extending cache to {0} and trying again!", this._cachedRays1.Length + 100)
                });
                Array.Resize<IntVector2>(ref this._cachedRays1, this._cachedRays1.Length + 100);
            }
            bool flag = false;
            for (int j = 0; j < i - 1; j++)
            {
                if (this.room.GetTile(this._cachedRays1[j + 1]).IsSolid())
                {
                    this.ConsiderGrabPos(Custom.RestrictInRect(vector, this.room.TileRect(this._cachedRays1[j]).Shrink(1f)), this.idealGrabPos);
                    flag = true;
                    break;
                }
                if (this.room.GetTile(this._cachedRays1[j]).horizontalBeam || this.room.GetTile(this._cachedRays1[j]).verticalBeam)
                {
                    this.ConsiderGrabPos(this.room.MiddleOfTile(this._cachedRays1[j]), this.idealGrabPos);
                    flag = true;
                }
            }
            if (flag)
            {
                this.foundNoGrabPos = 0;
            }
            else
            {
                this.foundNoGrabPos++;
            }
            bool flag2 = this.secondaryGrabBackTrackCounter < 200 && this.SecondaryGrabPosScore(this.secondaryGrabPos) > 0f;
            for (int k = 0; k < this.tChunks.Length; k++)
            {
                if (this.backtrackFrom == -1 || this.backtrackFrom > k)
                {
                    this.StickToTerrain(this.tChunks[k]);
                    if (base.grabDest != null)
                    {
                        if (!this.atGrabDest && Custom.DistLess(this.tChunks[k].pos, this.floatGrabDest.Value, 20f))
                        {
                            this.atGrabDest = true;
                        }
                        if (this.tChunks[k].currentSegment <= this.grabPath.Count || !flag2)
                        {
                            this.tChunks[k].vel += Vector2.ClampMagnitude(this.floatGrabDest.Value - this.tChunks[k].pos, 20f) / 20f * 1.2f;
                        }
                        else if (k > 1 && this.segments.Count > this.grabPath.Count && flag2)
                        {
                            float num = Mathf.InverseLerp((float)this.grabPath.Count, (float)this.segments.Count, (float)this.tChunks[k].currentSegment);
                            Vector2 a = Custom.DirVec(this.tChunks[k - 2].pos, this.tChunks[k].pos) * (1f - num) * 0.6f;
                            a += Custom.DirVec(this.tChunks[k].pos, this.room.MiddleOfTile(base.grabDest.Value)) * Mathf.Pow(1f - num, 4f) * 2f;
                            a += Custom.DirVec(this.tChunks[k].pos, this.room.MiddleOfTile(this.secondaryGrabPos)) * Mathf.Pow(num, 4f) * 2f;
                            a += Custom.DirVec(this.tChunks[k].pos, base.FloatBase) * Mathf.Sin(num * 3.1415927f) * 0.3f;
                            this.tChunks[k].vel += a.normalized * 1.2f;
                            if (k == this.tChunks.Length - 1)
                            {
                                this.tChunks[k].vel += Vector2.ClampMagnitude(this.room.MiddleOfTile(this.secondaryGrabPos) - this.tChunks[k].pos, 20f) / 20f * 4.2f;
                            }
                        }
                    }
                }
            }
            if (base.grabDest != null)
            {
                this.ConsiderSecondaryGrabPos(base.grabDest.Value + new IntVector2(UnityEngine.Random.Range(-20, 21), UnityEngine.Random.Range(-20, 21)));
            }
            if ((base.grabDest == null || !this.atGrabDest) && !this.chooseToMoveByPlayer)
            {
                this.UpdateClimbGrabPos(ref path);
            }

            /*
            float t = 0.5f;
            Vector2 dir = Vector3.Slerp(this.tentacleDir, player.mainBodyChunk.vel, t);
            idealGrabPos = base.FloatBase + dir * this.idealLength * 0.7f;
            Vector2 vector = base.FloatBase + Vector3.Slerp(dir, Custom.RNV(), Mathf.InverseLerp(20f, 200f, (float)this.foundNoGrabPos)).ToVector2InPoints() * this.idealLength * Custom.LerpMap((float)Math.Max(this.foundNoGrabPos, 0), 20f, 200f, 0.7f, 1.2f);
            List<IntVector2> list = new List<IntVector2>();
            SharedPhysics.RayTracedTilesArray(base.FloatBase, vector, list);
            bool flag = false;
            for (int i = 0; i < list.Count - 1; i++)
            {
                if (this.room.GetTile(list[i + 1]).Solid)
                {
                    ConsiderGrabPos(Custom.RestrictInRect(vector, this.room.TileRect(list[i]).Shrink(1f)), this.idealGrabPos);
                    flag = true;
                    break;
                }
                if (this.room.GetTile(list[i]).horizontalBeam || this.room.GetTile(list[i]).verticalBeam)
                {
                    ConsiderGrabPos(this.room.MiddleOfTile(list[i]), this.idealGrabPos);
                    flag = true;
                }
            }
            if (flag)
            {
                foundNoGrabPos = 0;
            }
            else
            {
                foundNoGrabPos++;
            }
            bool flag2 = this.secondaryGrabBackTrackCounter < 200 && this.SecondaryGrabPosScore(this.secondaryGrabPos) > 0f;
            for (int j = 0; j < this.tChunks.Length; j++)
            {
                if (this.backtrackFrom == -1 || this.backtrackFrom > j)
                {
                    StickToTerrain(this.tChunks[j]);
                    if (base.grabDest != null)
                    {
                        if (!this.atGrabDest && Custom.DistLess(this.tChunks[j].pos, this.floatGrabDest.Value, 20f))
                        {
                            this.atGrabDest = true;
                        }
                        if (this.tChunks[j].currentSegment <= this.grabPath.Count || !flag2)
                        {
                            this.tChunks[j].vel += Vector2.ClampMagnitude(this.floatGrabDest.Value - this.tChunks[j].pos, 20f) / 20f * 1.2f;
                        }
                        else if (j > 1 && this.segments.Count > this.grabPath.Count && flag2)
                        {
                            float num = Mathf.InverseLerp((float)this.grabPath.Count, (float)this.segments.Count, (float)this.tChunks[j].currentSegment);
                            Vector2 a = Custom.DirVec(this.tChunks[j - 2].pos, this.tChunks[j].pos) * (1f - num) * 0.6f;
                            a += Custom.DirVec(this.tChunks[j].pos, this.room.MiddleOfTile(base.grabDest.Value)) * Mathf.Pow(1f - num, 4f) * 2f;
                            a += Custom.DirVec(this.tChunks[j].pos, this.room.MiddleOfTile(this.secondaryGrabPos)) * Mathf.Pow(num, 4f) * 2f;
                            a += Custom.DirVec(this.tChunks[j].pos, base.FloatBase) * Mathf.Sin(num * 3.1415927f) * 0.3f;
                            this.tChunks[j].vel += a.normalized * 1.2f;
                            if (j == this.tChunks.Length - 1)
                            {
                                this.tChunks[j].vel += Vector2.ClampMagnitude(this.room.MiddleOfTile(this.secondaryGrabPos) - this.tChunks[j].pos, 20f) / 20f * 4.2f;
                            }
                        }
                    }
                }
            }
            if (grabDest != null)
            {
                ConsiderSecondaryGrabPos(base.grabDest.Value + new IntVector2(Random.Range(-20, 21), Random.Range(-20, 21)));
            }
            if (base.grabDest == null || !this.atGrabDest)
            {
                UpdateClimbGrabPos(ref path);
            }*/
        }

        public void ConsiderGrabPos(Vector2 testPos, Vector2 idealGrabPos)
        {
            if (this.GrabPosScore(testPos, idealGrabPos) > this.GrabPosScore(this.preliminaryGrabDest, idealGrabPos))
            {
                this.preliminaryGrabDest = testPos;
            }
        }

        public void ConsiderSecondaryGrabPos(IntVector2 testPos)
        {
            if (this.room.GetTile(testPos).Solid)
            {
                return;
            }
            if (this.SecondaryGrabPosScore(testPos) > this.SecondaryGrabPosScore(this.secondaryGrabPos))
            {
                this.secondaryGrabBackTrackCounter = 0;
                this.secondaryGrabPos = testPos;
            }
        }

        public void StickToTerrain(Tentacle.TentacleChunk chunk)
        {
            if (this.floatGrabDest != null && !Custom.DistLess(chunk.pos, this.floatGrabDest.Value, 200f))
            {
                return;
            }
            int num = (int)Mathf.Sign(chunk.pos.x - this.room.MiddleOfTile(chunk.pos).x);
            Vector2 vector = new Vector2(0f, 0f);
            IntVector2 tilePosition = this.room.GetTilePosition(chunk.pos);
            for (int i = 0; i < 8; i++)
            {
                if (this.room.GetTile(tilePosition + new IntVector2(Custom.eightDirectionsDiagonalsLast[i].x * num, Custom.eightDirectionsDiagonalsLast[i].y)).Solid)
                {
                    if (Custom.eightDirectionsDiagonalsLast[i].x != 0)
                    {
                        vector.x = this.room.MiddleOfTile(chunk.pos).x + (float)(Custom.eightDirectionsDiagonalsLast[i].x * num) * (20f - chunk.rad);
                    }
                    if (Custom.eightDirectionsDiagonalsLast[i].y != 0)
                    {
                        vector.y = this.room.MiddleOfTile(chunk.pos).y + (float)Custom.eightDirectionsDiagonalsLast[i].y * (20f - chunk.rad);
                    }
                    break;
                }
            }
            if (vector.x == 0f && this.room.GetTile(chunk.pos).verticalBeam)
            {
                vector.x = this.room.MiddleOfTile(chunk.pos).x;
            }
            if (vector.y == 0f && this.room.GetTile(chunk.pos).horizontalBeam)
            {
                vector.y = this.room.MiddleOfTile(chunk.pos).y;
            }
            if (chunk.tentacleIndex > this.tChunks.Length / 2)
            {
                bool flag = vector.x != 0f || vector.y != 0f;
                if (flag)
                {
                    if (this.chunksStickSounds[chunk.tentacleIndex] > 10)
                    {
                        this.owner.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Grab_Terrain, chunk.pos, Mathf.InverseLerp((float)(this.tChunks.Length / 2), (float)(this.tChunks.Length - 1), (float)chunk.tentacleIndex), 1f);
                    }
                    if (this.chunksStickSounds[chunk.tentacleIndex] > 0)
                    {
                        this.chunksStickSounds[chunk.tentacleIndex] = 0;
                    }
                    else
                    {
                        this.chunksStickSounds[chunk.tentacleIndex]--;
                    }
                }
                else
                {
                    if (this.chunksStickSounds[chunk.tentacleIndex] < -10)
                    {
                        this.owner.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Release_Terrain, chunk.pos, Mathf.InverseLerp((float)(this.tChunks.Length / 2), (float)(this.tChunks.Length - 1), (float)chunk.tentacleIndex), 1f);
                    }
                    if (this.chunksStickSounds[chunk.tentacleIndex] < 0)
                    {
                        this.chunksStickSounds[chunk.tentacleIndex] = 0;
                    }
                    else
                    {
                        this.chunksStickSounds[chunk.tentacleIndex]++;
                    }
                }
            }
            if (vector.x != 0f)
            {
                chunk.vel.x = chunk.vel.x + (vector.x - chunk.pos.x) * 0.1f;
                chunk.vel.y = chunk.vel.y * 0.9f;
            }
            if (vector.y != 0f)
            {
                chunk.vel.y = chunk.vel.y + (vector.y - chunk.pos.y) * 0.1f;
                chunk.vel.x = chunk.vel.x * 0.9f;
            }
            if (vector.x != 0f || vector.y != 0f)
            {
                this.chunksGripping += 1f / (float)this.tChunks.Length;
            }
        }

        public float GrabPosScore(Vector2 testPos, Vector2 idealGrabPos)
        {
            float num = 100f / Vector2.Distance(testPos, idealGrabPos);
            if (base.grabDest != null && this.room.GetTilePosition(testPos) == base.grabDest.Value)
            {
                num *= 1.5f;
            }
            for (int i = 0; i < 4; i++)
            {
                if (this.room.GetTile(testPos + Custom.fourDirections[i].ToVector2() * 20f).Solid)
                {
                    num *= 2f;
                    break;
                }
            }
            return num;
        }

        public float SecondaryGrabPosScore(IntVector2 testPos)
        {
            if (base.grabDest == null)
            {
                return 0f;
            }
            if (testPos.FloatDist(base.BasePos) < 7f)
            {
                return 0f;
            }
            float num = this.idealLength - (float)this.grabPath.Count * 20f;
            if (Vector2.Distance(this.room.MiddleOfTile(testPos), this.floatGrabDest.Value) > num)
            {
                return 0f;
            }
            if (!SharedPhysics.RayTraceTilesForTerrain(this.room, base.grabDest.Value, testPos))
            {
                return 0f;
            }
            float num2 = 0f;
            for (int i = 0; i < 8; i++)
            {
                if (this.room.GetTile(testPos + Custom.eightDirections[i]).Solid)
                {
                    num2 += 1f;
                }
            }
            if (this.room.GetTile(testPos).horizontalBeam || this.room.GetTile(testPos).verticalBeam)
            {
                num2 += 1f;
            }
            if (num2 > 0f && testPos == this.secondaryGrabPos)
            {
                num2 += 1f;
            }
            if (num2 == 0f)
            {
                return 0f;
            }
            num2 += testPos.FloatDist(base.BasePos) / 10f;
            return num2 / (1f + Mathf.Abs(num * 0.75f - Vector2.Distance(this.room.MiddleOfTile(testPos), this.floatGrabDest.Value)) + Vector2.Distance(this.room.MiddleOfTile(testPos), this.room.MiddleOfTile(this.segments[this.segments.Count - 1])));
        }

        private void UpdateClimbGrabPos(ref List<IntVector2> path)
        {
            //按投掷键会自动抓猎物
            if (this.player.input[0].thrw && this.huntCreature != null)
            {
                return;
            }
            base.MoveGrabDest(this.preliminaryGrabDest, ref path);
        }

        public void Hunt()
        {

        }

        public void SwitchTask(Task newTask)
        {
            task = newTask;
        }
        /*
        private void ExamineSound(ref List<IntVector2> path)
        {
            if (!Custom.DistLess(this.checkSound.pos, base.FloatBase, this.idealLength * 1.1f) || this.checkSound.slatedForDeletion)
            {
                this.SwitchTask(DaddyTentacle.Task.Locomotion);
                return;
            }
            this.soundCheckTimer--;
            if (this.soundCheckTimer < 1 || this.examineSoundPos == null || Custom.DistLess(this.examineSoundPos.Value, base.Tip.pos, 20f))
            {
                if (this.examineSoundPos != null)
                {
                    this.soundCheckTimer = UnityEngine.Random.Range(40, 180);
                    this.soundCheckCounter++;
                    if (this.soundCheckCounter > 17)
                    {
                        this.checkSound.Destroy();
                        this.SwitchTask(DaddyTentacle.Task.Locomotion);
                        return;
                    }
                    this.examineSoundPos = null;
                }
                SharedPhysics.RayTracedTilesArray(this.checkSound.pos, this.checkSound.pos + Custom.RNV() * 150f, this._cachedRays2);
                int num = this._cachedRays2.Count;
                for (int i = 0; i < this._cachedRays2.Count - 1; i++)
                {
                    if (this.room.GetTile(this._cachedRays2[i + 1]).Solid)
                    {
                        num = i;
                        break;
                    }
                }
                for (int j = this._cachedRays2.Count - 1; j > num; j--)
                {
                    this._cachedRays2.RemoveAt(j);
                }
                while (this._cachedRays2.Count > 0)
                {
                    int index = this._cachedRays2.Count - 1;
                    if (this.room.aimap.getTerrainProximity(this._cachedRays2[index]) < 2 || ((this.room.GetTile(this._cachedRays2[index]).horizontalBeam || this.room.GetTile(this._cachedRays2[index]).verticalBeam) && UnityEngine.Random.value < 0.05f))
                    {
                        this.examineSoundPos = new Vector2?(Custom.RestrictInRect(this.room.MiddleOfTile(this._cachedRays2[index]) + Custom.DirVec(base.FloatBase, this.room.MiddleOfTile(this._cachedRays2[index])) * 20f, this.room.TileRect(this._cachedRays2[index]).Shrink(1f)));
                        break;
                    }
                    this._cachedRays2.RemoveAt(index);
                }
            }
            if (this.examineSoundPos != null)
            {
                base.MoveGrabDest(this.examineSoundPos.Value, ref path);
            }
            for (int k = 0; k < this.tChunks.Length; k++)
            {
                if (this.backtrackFrom == -1 || this.backtrackFrom > k)
                {
                    if (base.grabDest != null && this.room.VisualContact(this.tChunks[k].pos, this.floatGrabDest.Value))
                    {
                        this.tChunks[k].vel += Vector2.ClampMagnitude(this.floatGrabDest.Value - this.tChunks[k].pos, 20f) / 20f * 1.2f;
                    }
                    else
                    {
                        this.tChunks[k].vel += Vector2.ClampMagnitude(this.room.MiddleOfTile(this.segments[this.tChunks[k].currentSegment]) - this.tChunks[k].pos, 20f) / 20f * 1.2f;
                    }
                }
            }
        }
        */
        public enum Task
        {
            Locomotion,
            Hunt,
            Grabbing,
            CarryObject
        }
    }

    internal class CorruptionCatLegGraphics : CorruptionCatTubeGraphic
    {
        public CorruptionCatLegGraphics(CorruptionCatGraphics owner, int tentacleIndex, int firstSprite) : base(owner, (int)(owner.corruptionCat.tentacles[tentacleIndex].idealLength / 10f), firstSprite)
        {
            this.tentacleIndex = tentacleIndex;
            this.sprites = 1;
            int num = (int)(owner.corruptionCat.tentacles[tentacleIndex].idealLength / 10f);
            this.bumps = new CorruptionCatTubeGraphic.Bump[num / 2 + Random.Range(5, 8)];
            for (int i = 0; i < this.bumps.Length; i++)
            {
                float num2 = Mathf.Pow(Random.value, 0.3f);
                if (i == 0)//尖端囊肿？
                {
                    num2 = 1f;
                }/*
                if (i == 1)//试图添加根部囊肿
                {
                    num2 = 0f;
                }*/
                this.bumps[i] = new CorruptionCatTubeGraphic.Bump(new Vector2(Mathf.Lerp(-1f, 1f, Random.value) * 3f * num2, Mathf.Lerp(Mathf.InverseLerp(0f, (float)num, (float)(num - 20)), 1f, num2)), Mathf.Lerp(Random.value, num2, Random.value), (Random.value >= Mathf.Lerp(0f, 0.6f, num2)) ? 0f : Mathf.Lerp(0.2f, 0.8f, Mathf.Pow(Random.value, Mathf.Lerp(1.5f, 0.5f, num2))));
                this.sprites++;
                if (this.bumps[i].eyeSize > 0f)
                {
                    this.sprites++;
                }
            }
        }

        internal CorruptionCatTentacles tentacle
        {
            get
            {
                return this.owner.corruptionCat.tentacles[this.tentacleIndex];
            }
        }

        public override void Update()
        {
            base.Update();
            int listCount = 0;
            base.AddToPositionsList(listCount++, this.tentacle.FloatBase);
            for (int i = 0; i < this.tentacle.tChunks.Length; i++)
            {
                for (int j = 1; j < this.tentacle.tChunks[i].rope.TotalPositions; j++)
                {
                    base.AddToPositionsList(listCount++, 
                        this.tentacle.tChunks[i].rope.GetPosition(j) + Custom.RNV() * Mathf.InverseLerp(4f, 14f, (float)this.tentacle.stun) * 4f * UnityEngine.Random.value);
                }
            }
            base.AlignAndConnect(listCount);
        }
        public int tentacleIndex;
    }

    internal abstract class CorruptionCatTubeGraphic : RopeGraphic
    {
        public CorruptionCatTubeGraphic(CorruptionCatGraphics owner, int segments, int firstSprite) : base(segments)
        {
            this.owner = owner;
            this.firstSprite = firstSprite;
        }

        public override void Update()
        {
        }

        public override void ConnectPhase(float totalRopeLength)
        {
        }

        public override void MoveSegment(int segment, Vector2 goalPos, Vector2 smoothedGoalPos)
        {
            this.segments[segment].vel *= 0f;
            if (this.owner.player.room.GetTile(smoothedGoalPos).Solid && !this.owner.player.room.GetTile(goalPos).Solid)
            {
                FloatRect floatRect = Custom.RectCollision(smoothedGoalPos, goalPos, this.owner.player.room.TileRect(this.owner.player.room.GetTilePosition(smoothedGoalPos)).Grow(3f));
                this.segments[segment].pos = new Vector2(floatRect.left, floatRect.bottom);
            }
            else
            {
                this.segments[segment].pos = smoothedGoalPos;
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites[this.firstSprite] = TriangleMesh.MakeLongMeshAtlased(this.segments.Length, false, true);
            int num = 0;
            for (int i = 0; i < this.bumps.Length; i++)
            {
                sLeaser.sprites[this.firstSprite + 1 + i] = new FSprite("Circle20", false)
                {
                    scale = Mathf.Lerp(2f, 6f, this.bumps[i].size) / 10f
                };
                if (this.bumps[i].eyeSize > 0f)
                {
                    sLeaser.sprites[this.firstSprite + 1 + this.bumps.Length + num] = new FSprite("Circle20", false)
                    {
                        scale = Mathf.Lerp(2f, 6f, this.bumps[i].size) * this.bumps[i].eyeSize / 10f
                    };
                    num++;
                }
            }
        }

        public override void DrawSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 vector = Vector2.Lerp(this.segments[0].lastPos, this.segments[0].pos, timeStacker);
            vector += Custom.DirVec(Vector2.Lerp(this.segments[1].lastPos, this.segments[1].pos, timeStacker), vector) * 1f;
            float d = 1.7f;
            for (int i = 0; i < this.segments.Length; i++)
            {
                Vector2 vector2 = Vector2.Lerp(this.segments[i].lastPos, this.segments[i].pos, timeStacker);
                Vector2 normalized = (vector - vector2).normalized;
                Vector2 a = Custom.PerpendicularVector(normalized);
                (sLeaser.sprites[this.firstSprite] as TriangleMesh).MoveVertice(i * 4, vector - a * d - camPos);
                (sLeaser.sprites[this.firstSprite] as TriangleMesh).MoveVertice(i * 4 + 1, vector + a * d - camPos);
                (sLeaser.sprites[this.firstSprite] as TriangleMesh).MoveVertice(i * 4 + 2, vector2 - a * d - camPos);
                (sLeaser.sprites[this.firstSprite] as TriangleMesh).MoveVertice(i * 4 + 3, vector2 + a * d - camPos);
                vector = vector2;
            }
            int num2 = 0;
            for (int j = 0; j < this.bumps.Length; j++)
            {
                Vector2 vector3 = this.OnTubePos(this.bumps[j].pos, timeStacker);
                sLeaser.sprites[this.firstSprite + 1 + j].x = vector3.x - camPos.x;
                sLeaser.sprites[this.firstSprite + 1 + j].y = vector3.y - camPos.y;
                if (this.bumps[j].eyeSize > 0f)
                {
                    sLeaser.sprites[this.firstSprite + 1 + this.bumps.Length + num2].x = vector3.x - camPos.x;
                    sLeaser.sprites[this.firstSprite + 1 + this.bumps.Length + num2].y = vector3.y - camPos.y;
                    num2++;
                }
            }
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            for (int i = 0; i < (sLeaser.sprites[this.firstSprite] as TriangleMesh).vertices.Length; i++)
            {
                float floatPos = Mathf.InverseLerp(0.3f, 1f, (float)i / (float)((sLeaser.sprites[this.firstSprite] as TriangleMesh).vertices.Length - 1));
                (sLeaser.sprites[this.firstSprite] as TriangleMesh).verticeColors[i] = Color.Lerp(this.owner.EffectColorA, owner.EffectColorB, OnTubeEffectColorFac(floatPos));
            }
            int num = 0;
            for (int j = 0; j < this.bumps.Length; j++)
            {
                sLeaser.sprites[this.firstSprite + 1 + j].color = Color.Lerp(this.owner.EffectColorA, this.owner.EffectColorB, OnTubeEffectColorFac(bumps[j].pos.y));
                if (this.bumps[j].eyeSize > 0f)
                {
                    sLeaser.sprites[this.firstSprite + 1 + this.bumps.Length + num].color = this.owner.EffectColorB;
                    num++;
                }
            }
        }

        public virtual float OnTubeEffectColorFac(float floatPos)
        {
            return Mathf.Pow(floatPos, 1.5f) * 0.4f;
        }

        public Vector2 OnTubePos(Vector2 pos, float timeStacker)
        {
            Vector2 p = this.OneDimensionalTubePos(pos.y - 1f / (float)this.segments.Length, timeStacker);
            Vector2 p2 = this.OneDimensionalTubePos(pos.y + 1f / (float)this.segments.Length, timeStacker);
            return this.OneDimensionalTubePos(pos.y, timeStacker) + Custom.PerpendicularVector(Custom.DirVec(p, p2)) * pos.x;
        }

        public Vector2 OneDimensionalTubePos(float floatPos, float timeStacker)
        {
            int num = Custom.IntClamp(Mathf.FloorToInt(floatPos * (float)(this.segments.Length - 1)), 0, this.segments.Length - 1);
            int num2 = Custom.IntClamp(num + 1, 0, this.segments.Length - 1);
            float t = Mathf.InverseLerp((float)num, (float)num2, floatPos * (float)(this.segments.Length - 1));
            return Vector2.Lerp(Vector2.Lerp(this.segments[num].lastPos, this.segments[num2].lastPos, t), Vector2.Lerp(this.segments[num].pos, this.segments[num2].pos, t), timeStacker);
        }

        public CorruptionCatGraphics owner;

        public int firstSprite;

        public int sprites;

        public Bump[] bumps;

        public struct Bump
        {
            public Bump(Vector2 pos, float size, float eyeSize)
            {
                this.pos = pos;
                this.size = size;
                this.eyeSize = eyeSize;
            }

            public Vector2 pos;
            public float size;
            public float eyeSize;
        }
    }
}
