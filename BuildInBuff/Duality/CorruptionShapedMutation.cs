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

        public bool SizeClass
        {
            get
            {
                return false;
                //return (ModManager.MSC && (base.Template.type == MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs || this.world.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Spear || this.world.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer)) || base.Template.type == CreatureTemplate.Type.DaddyLongLegs;
            }
        }

        public Vector2 MiddleOfBody
        {
            get
            {
                if (!ownerRef.TryGetTarget(out var player))
                    return Vector2.zero;
                Vector2 vector = player.mainBodyChunk.pos * player.mainBodyChunk.mass;
                for (int i = 1; i < player.bodyChunks.Length; i++)
                {
                    vector += player.bodyChunks[i].pos * player.bodyChunks[i].mass;
                }
                return vector / player.TotalMass;
            }
        }

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
        public int TotalGrip
        {
            get
            {
                int num = 0;
                for (int n = 0; n < this.tentacles.GetLength(0); n++)
                {
                    if (this.tentacles[n].atGrabDest)
                    {
                        num++;
                    }
                }
                return num;
            }
        }
        public int MoveDirGrip
        {
            get
            {
                int num = 0;
                for (int n = 0; n < this.tentacles.GetLength(0); n++)
                {
                    if (this.tentacles[n].atGrabDest && !this.tentacles[n].OppositeDir)
                    {
                        num++;
                    }
                }
                return num;
            }
        }
        public int ChoosedTentaclesCount
        {
            get
            {
                int num = 0;
                for (int n = 0; n < this.tentacles.GetLength(0); n++)
                {
                    if (this.tentacles[n].chooseToMoveByPlayer)
                        num++;
                }
                return num;
            }
        }
        public int OppositeDirCount
        {
            get
            {
                int num = 0;
                for (int n = 0; n < this.tentacles.GetLength(0); n++)
                {
                    if (this.tentacles[n].OppositeDir)
                        num++;
                }
                return num;
            }
        }



        public List<IntVector2> pastPositions;
        public PlacedObject stuckPos;
        public bool squeeze;
        public float squeezeFac;
        public bool moving;
        public int notFollowingPathToCurrentGoalCounter;
        public float unconditionalSupport;
        public bool isHD; 
        public int eyesClosed;
        public int digestingCounter;

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

        public bool HaveDirInput => ownerRef.TryGetTarget(out var player) && (player.input[0].x != 0 || player.input[0].y != 0);
        #endregion
        #region 外观
        public CorruptionCatGraphics graphics;
        public CorruptionCatTentacle[] tentacles;
        public int totalLegSprites;
        public Color EffectColor => Color.blue;
        #endregion

        public CorruptionCat(Player player)
        {
            this.ownerRef = new WeakReference<Player>(player);
            this.origThrowingSkill = player.slugcatStats.throwingSkill;
            this.moveSpeed = DefaultMoveSpeed;
            this.tentacles = new CorruptionCatTentacle[6];
            for (int i = 0; i < 6; i++)
            {
                this.tentacles[i] = new CorruptionCatTentacle(player, this, player.bodyChunks[1], 300f, 0, Custom.DegToVec(i * 60f + 30f));
            }
            this.graphics = new CorruptionCatGraphics(player, this);
            this.wantPos = player.mainBodyChunk.pos;
            this.bodyWantPos = player.mainBodyChunk.pos;
            this.NewRoom(player.room);
        }

        //进行更新
        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (player.room == null)
                return;

            //无法站立
            player.standing = false;
            //无法投掷
            player.slugcatStats.throwingSkill = -1;//Mathf.Max(-1, origThrowingSkill - VultureShapedMutationBuffEntry.StackLayer + 1);
            //无法拾取任何东西
            for (int i = 0; i < player.grasps.Length; i++)
                if (player.grasps[i] != null)
                    player.ReleaseGrasp(i);
            
            if (player.graphicsModule != null && player.Consious && (player.room.aimap == null ||
                (!player.room.aimap.TileAccessibleToCreature(player.mainBodyChunk.pos, StaticWorld.GetCreatureTemplate(this.Type)) &&
                !player.room.aimap.TileAccessibleToCreature(player.bodyChunks[1].pos, StaticWorld.GetCreatureTemplate(this.Type)))))
            {
                for (int l = 0; l < this.tentacles.GetLength(0); l++)
                {
                    if (this.tentacles[l].atGrabDest && !this.tentacles[l].chooseToMoveByPlayer &&
                        (!TileAccessibleToPlayer() ||
                        (Custom.DistLess(wantPos, player.mainBodyChunk.pos, 20f) && this.tentacles[l].OppositeDir)))// &&
                        //!Custom.DistLess(player.mainBodyChunk.pos, this.tentacles[l].Tip.pos, this.tentacles[l].idealLength * 0.7f)
                    {
                        Vector2 a = Custom.DirVec(player.mainBodyChunk.pos, this.tentacles[l].Tip.pos) *
                            Mathf.Pow(Custom.LerpMap(Mathf.Pow((Vector2.Distance(player.mainBodyChunk.pos, this.tentacles[l].Tip.pos) / this.tentacles[l].idealLength), 2f),
                                                     0f, 1f, 
                                                     -0.5f, 1f), 3f);
                        player.mainBodyChunk.pos += a * 0.8f * (player.room.gravity - player.Submersion);
                        player.mainBodyChunk.vel += a * 0.8f * (player.room.gravity - player.Submersion);
                    }
                }
            }

            int num = 0;
            for (int m = 0; m < this.tentacles.Length; m++)
            {/*
                if (ModManager.MSC)
                {
                    if ((base.State as DaddyLongLegs.DaddyState).tentacleHealth[m] < 1f)
                    {
                        if (base.abstractCreature.superSizeMe || this.isHD)
                        {
                            (base.State as DaddyLongLegs.DaddyState).tentacleHealth[m] += 0.0012f;
                        }
                        else if (this.SizeClass)
                        {
                            (base.State as DaddyLongLegs.DaddyState).tentacleHealth[m] += 0.0003f;
                        }
                    }
                    if ((base.State as DaddyLongLegs.DaddyState).tentacleHealth[m] > 1f)
                    {
                        (base.State as DaddyLongLegs.DaddyState).tentacleHealth[m] = 1f;
                    }
                }*/
                this.tentacles[m].Update();
                if (this.tentacles[m].atGrabDest)
                {
                    num++;
                }
                this.tentacles[m].retractFac = this.squeezeFac;
            }

            if (player.Consious)
            {
                this.Act(num);
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

        public void Act(int legsGrabbing)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            this.PlayerMoveByLegs();
            //this.AI.Update();
            float num = 0.6f;
            int num2 = 3;
            float num3 = 1.2f;
            float num4 = 0.2f;
            if (true)
            {
                num = 0.5f;
                num2 = 3;
                num3 = 1.1f;
                num4 = 1f;
            }
            Vector2? vector = null;
            //MovementConnection movementConnection = default(MovementConnection);
            if (true)
            {
                this.stuckPos = null;
                if (this.isHD)
                {
                    num = 0.45f;
                }
                else if (player.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.BrotherLongLegs)
                {
                    num = 0.35f;
                }
                else if (player.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.DaddyLongLegs)
                {
                    num = 0.25f;
                }
                else if (player.abstractCreature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs)
                {
                    num = 0.15f;
                }
                MovementConnection.MovementType type = MovementConnection.MovementType.Standard;
                if (player.room.GetTile(player.mainBodyChunk.pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
                {
                    type = MovementConnection.MovementType.ShortCut;
                }
                else
                {
                    for (int i = 0; i < Custom.fourDirections.Length; i++)
                    {
                        if (player.room.GetTile(player.mainBodyChunk.pos + Custom.fourDirections[i].ToVector2() * 20f).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
                        {
                            type = MovementConnection.MovementType.BigCreatureShortCutSqueeze;
                            break;
                        }
                    }
                }
                if (player.input[0].jmp)
                {
                    if (HaveDirInput)
                    {
                        bool flag = false;
                        for (int j = 0; j < this.tentacles.Length; j++)
                        {
                            if (this.tentacles[j].grabChunk != null && this.tentacles[j].grabChunk.owner is Creature)
                            {
                                flag = true;
                                break;
                            }
                        }
                        //?，或抓住了物品
                        if (player.input[0].thrw || flag)//!player.input[0].pckp || flag
                        {
                            vector = new Vector2?(player.mainBodyChunk.pos + new Vector2((float)player.input[0].x, (float)player.input[0].y) * 40f);
                            //movementConnection = new MovementConnection(type, player.room.GetWorldCoordinate(player.mainBodyChunk.pos), player.room.GetWorldCoordinate(vector.Value), 2);
                        }
                        else
                        {
                            this.moving = false;
                        }
                    }
                    else
                    {
                        this.moving = false;
                    }
                    //松开触手抓握，(或需要移动)
                    if ((!player.input[0].thrw && player.input[1].thrw) || (!player.input[0].pckp && player.input[1].pckp))// || player.input[0].jmp
                    {
                        for (int k = 0; k < this.tentacles.Length; k++)
                        {
                            this.tentacles[k].neededForLocomotion = true;
                            this.tentacles[k].SwitchTask(CorruptionCatTentacle.Task.Locomotion);
                        }
                    }
                }
                else
                {
                    this.moving = false;
                }
                if (!this.moving)
                {
                    this.unconditionalSupport = 1f;
                    if (this.isHD)
                    {
                        num3 = Mathf.InverseLerp(0f, 3f, (float)legsGrabbing);
                    }
                    else if (legsGrabbing > this.tentacles.Length / 2)
                    {
                        num3 = 1f;
                    }
                    else
                    {
                        num3 = 0.5f + Mathf.Lerp(0f, 0.5f, (float)(legsGrabbing / (this.tentacles.Length / 2)));
                    }
                }
                else if (legsGrabbing < this.tentacles.Length / 2)
                {
                    num3 *= Mathf.Lerp(0.6f, 1f, (float)(legsGrabbing / (this.tentacles.Length / 2)));
                }
                if (player.input[0].thrw)
                {
                    this.unconditionalSupport = 0f;
                    for (int l = 0; l < this.tentacles.Length; l++)
                    {
                        this.tentacles[l].neededForLocomotion = false;
                        this.tentacles[l].SwitchTask(CorruptionCatTentacle.Task.Hunt);
                    }
                    num3 = -1f;
                }
            }
            if (this.stuckPos == null)
            {
                if (this.notFollowingPathToCurrentGoalCounter < 200 && Custom.Dist(this.wantPos, player.mainBodyChunk.pos) > 20f)
                ///if (this.notFollowingPathToCurrentGoalCounter < 200 && this.AI.pathFinder.GetEffectualDestination != this.AI.pathFinder.GetDestination)
                {
                    this.notFollowingPathToCurrentGoalCounter++;
                }
                else if (this.notFollowingPathToCurrentGoalCounter > 0)
                {
                    this.notFollowingPathToCurrentGoalCounter--;
                }
                if (this.notFollowingPathToCurrentGoalCounter > 100)
                {
                    int num5 = 0;
                    while (num5 < player.bodyChunks.Length && legsGrabbing == 0)
                    {
                        if (player.bodyChunks[num5].ContactPoint.x != 0 || player.bodyChunks[num5].ContactPoint.y != 0)
                        {
                            legsGrabbing = 1;
                        }
                        num5++;
                    }
                }
                int num6 = 0;
                if ((Custom.ManhattanDistance(player.abstractCreature.pos.Tile, Room.StaticGetTilePosition(this.wantPos)) > num2 || //(Custom.ManhattanDistance(player.abstractCreature.pos, this.AI.pathFinder.GetEffectualDestination) > num2 || 
                    this.notFollowingPathToCurrentGoalCounter > 100) && legsGrabbing > 0)
                {
                    this.pastPositions.Insert(0, player.abstractCreature.pos.Tile);
                    if (this.pastPositions.Count > 80)
                    {
                        this.pastPositions.RemoveAt(this.pastPositions.Count - 1);
                    }
                    for (int m = 40; m < this.pastPositions.Count; m++)
                    {
                        if (Custom.DistLess(player.abstractCreature.pos.Tile, this.pastPositions[m], 4f))
                        {
                            num6++;
                        }
                    }
                }
                if (num6 > 30)
                {
                    this.stuckCounter++;
                }
                else
                {
                    this.stuckCounter -= 2;
                }
                this.stuckCounter = Custom.IntClamp(this.stuckCounter, 0, 200);
                if (this.stuckCounter > 100)
                {
                    for (int n = 0; n < player.bodyChunks.Length; n++)
                    {
                        player.bodyChunks[n].vel += Custom.RNV() * 3f * Random.value * Mathf.InverseLerp(100f, 200f, (float)this.stuckCounter);
                    }
                }
                if (true)
                {
                    this.stuckCounter = 0;
                }
            }
            else
            {
                this.stuckCounter = 0;
            }
            if ((legsGrabbing > this.tentacles.Length / 2 && this.moving) || this.stuckCounter > 100)
            //if ((legsGrabbing > this.tentacles.Length / 2 && this.moving) || this.stuckCounter > 100)
            {
                float num7 = float.MinValue;
                int num8 = -1;
                for (int num9 = 0; num9 < this.tentacles.Length; num9++)
                {
                    if (this.tentacles[num9].atGrabDest && this.tentacles[num9].huntCreature == null && this.tentacles[num9].ReleaseScore() > num7)
                    {
                        num7 = this.tentacles[num9].ReleaseScore();
                        num8 = num9;
                    }
                }
                if (num8 > -1)// && this.totalGrip >= 1
                {
                    List<IntVector2> list = null;
                    this.tentacles[num8].UpdateClimbGrabPos(ref list);
                }/*
                if (this.oppositeDirCount >= 4 && num82 > -1)
                {
                    List<IntVector2> list = null;
                    this.tentacles[num8].UpdateClimbGrabPos(ref list);
                }*/
            }
            float num10 = 0f;
            float num11 = 0f;
            for (int num12 = 0; num12 < this.tentacles.Length; num12++)
            {
                float num13 = Mathf.Pow(this.tentacles[num12].chunksGripping, 0.5f);
                if (this.tentacles[num12].atGrabDest && this.tentacles[num12].grabDest != null)
                {
                    num11 += Mathf.Pow(Mathf.InverseLerp(Custom.LerpMap((float)this.stuckCounter, 0f, 100f, -0.1f, -1f), 
                                                         0.85f, 
                                                         Vector2.Dot((this.tentacles[num12].floatGrabDest.Value - player.mainBodyChunk.pos).normalized, this.moveDirection)), 
                                       0.8f) / 
                            (float)this.tentacles.Length;
                    num13 = Mathf.Lerp(num13, 1f, 0.75f);
                }
                num10 += num13 / (float)this.tentacles.Length;
            }
            num11 = Mathf.Pow(num11 * num10, Custom.LerpMap((float)this.stuckCounter, 100f, 200f, 0.8f, 0.1f));
            num10 = Mathf.Pow(num10, 0.3f);
            num11 = Mathf.Max(num11, this.squeezeFac);
            num10 = Mathf.Max(num10, this.squeezeFac);
            num10 = Mathf.Max(num10, this.unconditionalSupport);
            num11 = Mathf.Max(num11, this.unconditionalSupport);
            float num14 = 0f;
            for (int num15 = 0; num15 < this.tentacles.Length; num15++)
            {
                if (this.tentacles[num15].neededForLocomotion)
                {
                    num14 += 1f / (float)this.tentacles.Length;
                }
            }
            if (num10 < 1f - num14)
            {
                float num16 = float.MinValue;
                int num17 = Random.Range(0, this.tentacles.Length);
                for (int num18 = 0; num18 < this.tentacles.Length; num18++)
                {
                    if (!this.tentacles[num18].neededForLocomotion)
                    {
                        float num19 = 1000f / Mathf.Lerp(this.tentacles[num18].idealLength * (float)player.room.aimap.getTerrainProximity(this.tentacles[num18].Tip.pos), 200f, 0.8f);
                        if (this.tentacles[num18].task == CorruptionCatTentacle.Task.Grabbing)
                        {
                            num19 *= 0.01f;
                        }
                        if (this.tentacles[num18].task == CorruptionCatTentacle.Task.Hunt)
                        {
                            num19 *= 0.1f;
                        }
                        if (this.tentacles[num18].task == CorruptionCatTentacle.Task.ExamineSound)
                        {
                            num19 *= 0.6f;
                        }
                        if (num19 > num16)
                        {
                            num16 = num19;
                            num17 = num18;
                        }
                    }
                }
                this.tentacles[num17].neededForLocomotion = true;
            }
            else if ((double)num10 > 0.85)
            {
                this.tentacles[Random.Range(0, this.tentacles.Length)].neededForLocomotion = false;
            }
            for (int num20 = 0; num20 < player.bodyChunks.Length; num20++)
            {
                player.bodyChunks[num20].vel *= Mathf.Lerp(1f, Mathf.Lerp(0.95f, 0.8f, this.squeezeFac), num10);
                BodyChunk bodyChunk = player.bodyChunks[num20];
                bodyChunk.vel.y = bodyChunk.vel.y + (player.gravity - player.buoyancy * player.bodyChunks[num20].submersion) * num10 * num3;
            }
            MovementConnection movementConnection2 = default(MovementConnection);
            if (!true && Custom.ManhattanDistance(player.abstractCreature.pos.Tile, Room.StaticGetTilePosition(this.wantPos)) < num2)//Custom.ManhattanDistance(player.abstractCreature.pos, this.AI.pathFinder.GetEffectualDestination) < num2
            {
                for (int num21 = 0; num21 < player.bodyChunks.Length; num21++)
                {
                    player.bodyChunks[num21].vel += Vector2.ClampMagnitude(this.wantPos - player.bodyChunks[0].pos, 30f) / 30f * num * num11;
                    //player.bodyChunks[num21].vel += Vector2.ClampMagnitude(player.room.MiddleOfTile(this.AI.pathFinder.GetEffectualDestination) - player.bodyChunks[0].pos, 30f) / 30f * num * num11;
                }
            }
            else if (true && vector != null && Custom.ManhattanDistance(player.abstractCreature.pos, Custom.MakeWorldCoordinate(new IntVector2((int)vector.Value.x / 20, (int)vector.Value.y / 20), player.abstractCreature.Room.index)) < num2)
            {
                for (int num22 = 0; num22 < player.bodyChunks.Length; num22++)
                {
                    player.bodyChunks[num22].vel += Vector2.ClampMagnitude(player.room.MiddleOfTile((int)vector.Value.x / 20, (int)vector.Value.y / 20) - player.bodyChunks[0].pos, 30f) / 30f * num * num11;
                }
            }
            else
            {
                int num23 = 0;/*
                while (num23 < player.bodyChunks.Length && movementConnection2 == default(MovementConnection))
                {
                    int num24 = 0;
                    while (num24 < 9 && movementConnection2 == default(MovementConnection))
                    {
                        movementConnection2 = (this.AI.pathFinder as StandardPather).
                            FollowPath(player.room.GetWorldCoordinate(player.bodyChunks[num23].pos + Custom.zeroAndEightDirectionsDiagonalsLast[num24].ToVector2() * 20f), true);
                        num24++;
                    }
                    num23++;
                }*/
                if (movementConnection2 == default(MovementConnection))
                {
                    movementConnection2 = this.CheckTentaclesForAccessibleTerrain();
                }
            }/*
            if (true && (movementConnection2 == default(MovementConnection) || !player.AllowableControlledAIOverride(movementConnection2.type)))
            {
                movementConnection2 = movementConnection;
            }*/
            this.moving = player.input[0].jmp && (HaveDirInput);
            //this.moving = (movementConnection2 != default(MovementConnection));
            /*
            if (ModManager.MMF && movementConnection2 == default(MovementConnection))
            {
                if (!true)
                {
                    this.moveDirection = (this.moveDirection + new Vector2(0f, -(num / 10f))).normalized;
                }
                return;
            }
            if (movementConnection2 != default(MovementConnection))
            {
                if (player.shortcutDelay < 1)
                {
                    this.squeeze = (movementConnection2.type == MovementConnection.MovementType.BigCreatureShortCutSqueeze);
                }
                if (player.shortcutDelay < 1 && movementConnection2.type == MovementConnection.MovementType.ShortCut)
                {
                    player.enteringShortCut = new IntVector2?(movementConnection2.StartTile);
                    return;
                }
                player.GoThroughFloors = (movementConnection2.DestTile.y < movementConnection2.StartTile.y);
                for (int num25 = 0; num25 < player.bodyChunks.Length; num25++)
                {
                    player.bodyChunks[num25].vel += Custom.DirVec(player.bodyChunks[0].pos, player.room.MiddleOfTile(movementConnection2.DestTile)) * num * num11;
                }
                MovementConnection movementConnection3 = movementConnection2;
                Vector2 vector2 = Custom.DirVec(movementConnection3.StartTile.ToVector2(), movementConnection3.DestTile.ToVector2());
                for (int num26 = 0; num26 < 10; num26++)
                {
                    movementConnection3 = (this.AI.pathFinder as StandardPather).FollowPath(movementConnection3.destinationCoord, false);
                    if (movementConnection3 == default(MovementConnection))
                    {
                        break;
                    }
                    vector2 += Custom.DirVec(movementConnection3.StartTile.ToVector2(), movementConnection3.DestTile.ToVector2());
                    if (num26 < 2 && movementConnection3.type == MovementConnection.MovementType.BigCreatureShortCutSqueeze && player.shortcutDelay < 1)
                    {
                        this.squeeze = true;
                    }
                }
                this.moveDirection = (this.moveDirection + vector2.normalized * num4).normalized;
                if (true && movementConnection != default(MovementConnection) && movementConnection.type == MovementConnection.MovementType.BigCreatureShortCutSqueeze)
                {
                    this.squeeze = true;
                    return;
                }
            }
            else
            {
                this.moveDirection = (this.moveDirection + new Vector2(0f, -(num / 10f))).normalized;
            }*/

            this.moveDirection = (wantPos - player.bodyChunks[0].pos).normalized;
        }

        private void PlayerMoveByLegs()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            
            wantPos = player.bodyChunks[0].lastPos + moveSpeed * new Vector2(player.input[0].x, player.input[0].y);
            moveDirection = (wantPos - player.bodyChunks[0].lastPos).normalized;
            
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
            /*
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
            */
            if (true)//TileAccessibleToPlayer()
            {
                //随机速度
                //player.bodyChunks[0].vel += Custom.RNV() * Random.value * 0.5f;
                //player.bodyChunks[1].vel *= 0.9f;

                for (int n = 0; n < this.tentacles.GetLength(0); n++)
                {
                    if (this.tentacles[n].atGrabDest)
                        this.tentacles[n].chooseToMoveByPlayer = false;

                    if (this.tentacles[n].chooseToMoveByPlayer &&
                        player.input[0].jmp && this.tentacles[n].grabDest != null &&
                        Custom.Dist(this.tentacles[n].Tip.pos, this.tentacles[n].connectedChunk.pos) > this.tentacles[n].idealLength / 3f)
                        this.tentacles[n].chooseToMoveByPlayerButNotControl = true;
                    else
                        this.tentacles[n].chooseToMoveByPlayerButNotControl = false;
                }

                float num7 = float.MinValue;
                int num8 = -1;
                //int num82 = -1;
                for (int num9 = 0; num9 < this.tentacles.Length; num9++)
                {
                    if (this.tentacles[num9].atGrabDest && this.tentacles[num9].huntCreature == null && this.tentacles[num9].ReleaseScore() > num7)
                    {
                        num7 = this.tentacles[num9].ReleaseScore();
                        num8 = num9;
                        //num82 = num8;
                    }
                }
                if (num8 > -1 && this.OppositeDirCount > 1 &&
                    !this.tentacles[num8].atGrabDest && this.tentacles[num8].OppositeDir &&
                    this.ChoosedTentaclesCount < 2)// && this.totalGrip >= 1
                {
                    this.tentacles[num8].chooseToMoveByPlayer = true;
                }
                if (this.ChoosedTentaclesCount == 0 && this.OppositeDirCount >= 1)
                {
                    int n = num8 > -1 ? num8 : Random.Range(0, this.tentacles.GetLength(0));
                    this.tentacles[n].chooseToMoveByPlayer = true;
                }

                //按住跳跃键时，可以按方向键使核心移动
                if (player.input[0].jmp && TileAccessibleToPlayer())//TileAccessibleToPlayer()
                {
                    bodyWantPos = wantPos;
                    player.bodyChunks[0].vel *= Custom.LerpMap(player.bodyChunks[0].vel.magnitude, 1f, 6f, 0.99f, 0.9f);
                    player.bodyChunks[0].vel += Mathf.Clamp01((float)(this.TotalGrip + 2f * this.MoveDirGrip) / 2f) * Vector2.ClampMagnitude(wantPos - player.bodyChunks[0].pos, moveSpeed) / moveSpeed * 3f;
                }
                //不按跳跃键时，核心位置不移动
                else
                {
                    player.bodyChunks[0].vel *= Custom.LerpMap(player.bodyChunks[0].vel.magnitude, 1f, 6f, 0.99f, 0.9f);
                    player.bodyChunks[0].vel += Mathf.Clamp01((float)(this.TotalGrip + 2f * this.MoveDirGrip) / 2f) * Vector2.ClampMagnitude(bodyWantPos - player.bodyChunks[0].pos, moveSpeed) / moveSpeed * 3f;
                }
            }
            else
            {
                if (!player.input[0].jmp)
                {
                    bodyWantPos = Vector2.Lerp(bodyWantPos, player.bodyChunks[0].pos, player.bodyChunks[0].vel.magnitude / 10f);
                }
                for (int n = 0; n < this.tentacles.GetLength(0); n++)
                {
                    if (this.tentacles[n].atGrabDest)
                    {
                        for (int i = 0; i < player.bodyChunks.GetLength(0); i++)
                            player.bodyChunks[i].vel *= 0.9f;
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
            BuffPlugin.Log("this.totalGrip + 2f * moveDirGrip: " + (this.TotalGrip + 2f * MoveDirGrip));
            if (this.TotalGrip >= 3 && 
                this.TotalGrip + 2f * this.MoveDirGrip >= 5f * player.room.gravity - 4.5f * player.Submersion)
                return true;
            return false;
        }

        public MovementConnection CheckTentaclesForAccessibleTerrain()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return default(MovementConnection);
            Vector2 pos = player.mainBodyChunk.pos;
            float num = float.MaxValue;
            Vector2 vector = player.mainBodyChunk.pos;

            vector = player.room.MiddleOfTile(this.wantPos);
            /*
            if (this.AI.pathFinder.GetDestination.room == player.abstractCreature.pos.room && this.AI.pathFinder.GetDestination.NodeDefined)
            {
                vector = player.room.MiddleOfTile(this.AI.pathFinder.GetDestination);
            }*/
            for (int i = 0; i < this.tentacles.Length; i++)
            {
                for (int j = 0; j < this.tentacles[i].tChunks.Length; j++)
                {
                    if (player.room.aimap.TileAccessibleToCreature(this.tentacles[i].tChunks[j].pos, player.Template) && Custom.DistLess(this.tentacles[i].tChunks[j].pos, vector, num))
                    {
                        pos = this.tentacles[i].tChunks[j].pos;
                        num = Vector2.Distance(this.tentacles[i].tChunks[j].pos, vector);
                    }
                }
            }
            if (num < 3.4028235E+38f)
            {
                return new MovementConnection(MovementConnection.MovementType.Standard, player.room.GetWorldCoordinate(player.mainBodyChunk.pos), player.room.GetWorldCoordinate(pos), (int)(num / 20f));
            }
            return default(MovementConnection);
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
            this.pastPositions = new List<IntVector2>();
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

        public CorruptionCatGraphics.Eye[] eyes;
        public float[,] chunksRotats;
        public int feelSomethingReactionDelay;

        public CorruptionCatGraphics(Player player, CorruptionCat corruptionCat)
        {
            this.player = player;
            this.corruptionCat = corruptionCat;
            this.totalLegSprites = 0;
            this.legGraphics = new CorruptionCatLegGraphics[corruptionCat.tentacles.Length];

            this.chunksRotats = new float[this.player.bodyChunks.Length, 2];
            this.eyes = new CorruptionCatGraphics.Eye[this.player.bodyChunks.Length];
            for (int m = 0; m < this.player.bodyChunks.Length; m++)
            {
                this.chunksRotats[m, 0] = Random.value * 360f;
                this.chunksRotats[m, 1] = Random.value;
                this.eyes[m] = new CorruptionCatGraphics.Eye(this, m);
            }
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
            for (int l = 0; l < this.player.bodyChunks.Length; l++)
            {
                this.eyes[l].Update();
            }
            if (this.feelSomethingReactionDelay > 0)
            {
                this.feelSomethingReactionDelay--;
            }
        }
        #endregion

        public void FeelSomethingWithTentacle(Creature creatureRep, Vector2 feelPos)
        {
            if (this.feelSomethingReactionDelay > 0)
            {
                return;
            }
            this.feelSomethingReactionDelay = Random.Range(10, 40);
            if (!this.corruptionCat.SizeClass)
            {
                this.player.room.PlaySound(SoundID.Bro_React_To_Tentacle_Touch, this.corruptionCat.MiddleOfBody);
                return;
            }
            Vector2 middleOfBody = this.corruptionCat.MiddleOfBody;
            float num = float.MinValue;
            int num2 = -1;
            for (int i = 0; i < this.eyes.Length; i++)
            {
                float num3 = Vector2.Dot(Custom.DirVec(middleOfBody, this.player.bodyChunks[i].pos), Custom.DirVec(middleOfBody, feelPos));
                if (this.eyes[i].soundSource != null)
                {
                    num3 -= 1f;
                }
                if (this.eyes[i].creatureRep != null)
                {
                    num3 -= 2f;
                }
                if (num3 > num)
                {
                    num = num3;
                    num2 = i;
                }
            }
            this.eyes[num2].ReactToCreature(creatureRep);
            this.player.room.PlaySound(SoundID.Daddy_React_To_Tentacle_Touch, middleOfBody);
        }

        public class Eye : DaddyGraphics.DaddyBubbleOwner
        {
            public BodyChunk chunk
            {
                get
                {
                    return this.owner.player.bodyChunks[this.index];
                }
            }

            public Eye(CorruptionCatGraphics owner, int index)
            {
                this.index = index;
                this.owner = owner;
                this.dir = new Vector2(0f, 0f);
                this.lastDir = new Vector2(0f, 0f);
                this.centerRenderPos = owner.player.bodyChunks[index].pos;
            }

            public void Update()
            {
                this.lastDir = this.dir;
                this.lastClosed = this.closed;
                this.closed = Mathf.Max(Mathf.Lerp(this.closed, Mathf.InverseLerp(0f, (float)this.eyesClosedDelay, (float)this.owner.corruptionCat.eyesClosed), 1f / (float)this.eyesClosedDelay), this.owner.player.Deaf);
                if (this.owner.corruptionCat.eyesClosed == 0)
                {
                    this.eyesClosedDelay = Random.Range(1, 20);
                }
                if (ModManager.MMF && this.owner.player.dead)
                {
                    this.eyesClosedDelay = Mathf.Min(this.eyesClosedDelay + 2, 15);
                }
                Vector2 vector = Vector2.zero;
                if (this.soundSource != null)
                {
                    vector = this.soundSource.pos;
                    if (this.soundSource.slatedForDeletion)
                    {
                        this.soundSource = null;
                    }
                }
                else if (this.creatureRep != null)
                {
                    if (CorruptionCatTentacle.VisualContact(this.creatureRep))
                    {
                        vector = this.creatureRep.DangerPos;
                    }
                    else
                    {
                        vector = this.owner.player.room.MiddleOfTile(this.creatureRep.abstractCreature.pos);
                    }
                    if (this.creatureRep.slatedForDeletetion)
                    {
                        this.creatureRep = null;
                    }
                }
                float num = 0f;
                if (vector.x != 0f && vector.y != 0f)
                {
                    this.dir = Vector3.Slerp(this.dir, Custom.DirVec(this.chunk.pos, vector) * Mathf.InverseLerp(0f, 200f, Vector2.Distance(this.chunk.pos, vector)), 0.3f);
                    num = this.light * Mathf.InverseLerp(0f, 1f, Vector2.Distance(this.lastDir, this.dir));
                    this.light = Mathf.Max(this.owner.player.dead ? 0f : 0.2f, this.light - 0.05f);
                }
                else
                {
                    this.dir *= 0.9f;
                    this.light = Mathf.Max(this.owner.player.dead ? 0f : 0.1f, this.light - 0.05f);
                    this.FindNewLookObject();
                }
                this.flash = Mathf.Max(0f, this.flash - 0.16666667f);
                if (Random.value < num)
                {
                    this.getToFocus = Mathf.Max(this.getToFocus, Random.value);
                }
                else if (Random.value < 0.014285714f)
                {
                    this.getToFocus = 0f;
                }
                this.lastFocus = this.focus;
                if (this.focus < this.getToFocus)
                {
                    this.focus = Mathf.Min(this.focus + 0.05f, this.getToFocus);
                    return;
                }
                this.focus = Mathf.Max(this.focus - 0.05f, this.getToFocus);
            }

            public void FindNewLookObject()
            {
                bool flag = false;
                /*
                if (this.owner.player.AI.tracker.CreaturesCount > 0)
                {
                    flag = true;
                    Tracker.CreatureRepresentation rep = this.owner.player.AI.tracker.GetRep(Random.Range(0, this.owner.player.AI.tracker.CreaturesCount));
                    int num = 0;
                    while (num < this.owner.eyes.Length && flag)
                    {
                        if (this.owner.eyes[num].creatureRep == rep)
                        {
                            flag = false;
                        }
                        num++;
                    }
                    if (flag)
                    {
                        this.creatureRep = rep;
                        this.light = Mathf.Max(0.75f, this.light);
                    }
                }
                if (this.owner.corruptionCat.SizeClass && !flag && (float)this.owner.player.AI.noiseTracker.sources.Count > 0f)
                {
                    flag = true;
                    NoiseTracker.TheorizedSource theorizedSource = this.owner.player.AI.noiseTracker.sources[Random.Range(0, this.owner.player.AI.noiseTracker.sources.Count)];
                    int num2 = 0;
                    while (num2 < this.owner.eyes.Length && flag)
                    {
                        if (this.owner.eyes[num2].soundSource == theorizedSource)
                        {
                            flag = false;
                        }
                        num2++;
                    }
                    if (flag)
                    {
                        this.soundSource = theorizedSource;
                        this.light = Mathf.Max(0.5f, this.light);
                    }
                }*/
            }

            public void ReactToSound(NoiseTracker.TheorizedSource newSound)
            {
                this.creatureRep = null;
                this.soundSource = newSound;
                this.light = 1f;
                this.flash = 1f;
            }

            public void ReactToCreature(Creature newCrit)
            {
                this.soundSource = null;
                this.creatureRep = newCrit;
                this.light = Mathf.Max(this.light, Random.value);
            }

            public Color GetColor()
            {
                return this.renderColor;
            }

            public Vector2 GetPosition()
            {
                return this.centerRenderPos;
            }

            public int index;
            public Vector2 dir;
            public Vector2 lastDir;
            public float focus; 
            public float getToFocus;
            public float lastFocus;
            public float closed;
            public float lastClosed;
            public int eyesClosedDelay;
            public CorruptionCatGraphics owner;
            public NoiseTracker.TheorizedSource soundSource;
            public Creature creatureRep;
            public float light;
            public float flash;
            public Vector2 centerRenderPos;
            public Color renderColor;
        }
    }

    internal class CorruptionCatTentacle : Tentacle
    {
        //attr
        public Player player => owner as Player;
        public CorruptionCat corruptionCat;
        public bool chooseToMoveByPlayer;
        public bool chooseToMoveByPlayerButNotControl;

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
        public Creature huntCreature;
        //public Tracker.CreatureRepresentation huntCreature;
        private Vector2 preliminaryGrabDest;

        private IntVector2[] _cachedRays1 = new IntVector2[200];
        private readonly List<IntVector2> _cachedRays2 = new List<IntVector2>(10);

        private float SpeedFac => this.corruptionCat.DefaultMoveSpeed / 5f;
        public bool OppositeDir => Vector2.Dot(this.corruptionCat.moveDirection, this.Tip.pos - connectedChunk.pos) < 0;


        public float sticky;
        public NoiseTracker.TheorizedSource checkSound;
        public Vector2 huntDirection;
        public int soundCheckCounter;
        public int soundCheckTimer; 
        public Vector2? examineSoundPos;



        public CorruptionCatTentacle(Player player, CorruptionCat corruptionCat, BodyChunk bodyChunk, float length, int tentacleNumber, Vector2 tentacleDir) : base(player, bodyChunk, length)
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
            if (this.grabChunk != null && (this.grabChunk.owner.room == null || this.grabChunk.owner.room != this.player.room))
            {
                this.stun = 10;
                this.grabChunk = null;
            }
            if (this.player.dead)
            {
                this.neededForLocomotion = true;
                this.grabChunk = null;
                this.limp = true;
            }
            if (this.stun > 0)
            {
                this.stun--;
                this.grabChunk = null;
            }/*
            if (Mathf.Pow(Random.value, 0.35f) > (this.corruptionCat.State as DaddyLongLegs.DaddyState).tentacleHealth[this.tentacleNumber])
            {
                this.stun = Math.Max(this.stun, (int)Mathf.Lerp(-4f, 14f, Mathf.Pow(Random.value, 0.5f + 20f * Mathf.Max(0f, (this.corruptionCat.State as DaddyLongLegs.DaddyState).tentacleHealth[this.tentacleNumber]))));
            }*/
            if (this.grabChunk != null)
            {
                float num = Vector2.Distance(base.Tip.pos, this.grabChunk.pos);
                float num2 = (base.Tip.rad + this.grabChunk.rad) / 4f;
                Vector2 vector = Custom.DirVec(base.Tip.pos, this.grabChunk.pos);
                float num3 = this.grabChunk.mass / (this.grabChunk.mass + 0.01f);
                float num4 = 1f;
                base.Tip.pos += vector * (num - num2) * num3 * num4 * SpeedFac;
                base.Tip.vel += vector * (num - num2) * num3 * num4 * SpeedFac;
                this.grabChunk.pos -= vector * (num - num2) * (1f - num3) * num4 * SpeedFac;
                this.grabChunk.vel -= vector * (num - num2) * (1f - num3) * num4 * SpeedFac;
                if (this.grabChunk.owner is Player && Random.value < Mathf.Lerp(0f, 1f / (this.corruptionCat.SizeClass ? 20f : 10f), (this.grabChunk.owner as Player).GraspWiggle))
                {
                    this.stun = Math.Max(this.stun, Random.Range(1, this.corruptionCat.SizeClass ? 7 : 17));
                    this.grabChunk = null;
                }
            }
            this.limp = (!this.player.Consious || this.stun > 0);
            for (int i = 0; i < this.tChunks.Length; i++)
            {
                this.tChunks[i].vel *= 0.9f;
                if (this.limp)
                {
                    Tentacle.TentacleChunk tentacleChunk = this.tChunks[i];
                    tentacleChunk.vel.y = tentacleChunk.vel.y - 0.5f;
                }
                if (this.stun > 0 && !this.player.dead)
                {
                    this.tChunks[i].vel += Custom.RNV() * 10f;
                }
            }
            if (this.limp)
            {
                for (int j = 0; j < this.tChunks.Length; j++)
                {
                    Tentacle.TentacleChunk tentacleChunk2 = this.tChunks[j];
                    tentacleChunk2.vel.y = tentacleChunk2.vel.y - 0.7f;
                }
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
            Vector2 vector2 = this.player.mainBodyChunk.pos;
            for (int k = 1; k < this.player.bodyChunks.Length; k++)
            {
                vector2 += this.player.bodyChunks[k].pos;
            }
            vector2 /= (float)this.player.bodyChunks.Length;
            this.awayFromBodyRotation = Custom.AimFromOneVectorToAnother(vector2, this.connectedChunk.pos);
            this.chunksGripping = 0f;
            if (!this.neededForLocomotion)
            {
                bool flag = !this.player.input[0].pckp && !this.player.input[0].thrw;
                //bool flag = !this.player.safariControlled || (this.player.inputWithDiagonals != null && this.player.inputWithDiagonals.Value.pckp);
                if (this.task != CorruptionCatTentacle.Task.Grabbing && flag)
                {
                    this.LookForCreaturesToHunt();
                    if (this.huntCreature == null && this.checkSound == null)
                    {
                        this.LookForSoundsToExamine();
                    }
                }
            }
            else if (this.task != CorruptionCatTentacle.Task.Locomotion)
            {
                this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
            }
            if (this.task == CorruptionCatTentacle.Task.Hunt && (this.huntCreature == null || this.huntCreature.slatedForDeletetion))
            {
                this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
            }
            else if (this.task != CorruptionCatTentacle.Task.Hunt && this.huntCreature != null)
            {
                this.huntCreature = null;
            }
            if (this.task == CorruptionCatTentacle.Task.ExamineSound && (this.checkSound == null || this.checkSound.slatedForDeletion))
            {
                this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
            }
            else if (this.task != CorruptionCatTentacle.Task.ExamineSound && this.checkSound != null)
            {
                this.checkSound = null;
            }
            if (this.task == CorruptionCatTentacle.Task.Grabbing && 
                (this.grabChunk == null || this.grabChunk.owner.room != this.room || (ModManager.MMF && !this.player.Consious)))
            {
                this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
            }
            else if (this.task != CorruptionCatTentacle.Task.Grabbing && this.grabChunk != null)
            {
                this.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Release_Creature, this.grabChunk.pos);
                this.grabChunk = null;
            }
            if (this.task == CorruptionCatTentacle.Task.Locomotion)
            {
                this.Climb(ref this.scratchPath);
            }
            else if (this.task == CorruptionCatTentacle.Task.Hunt)
            {
                this.Hunt(ref this.scratchPath);
            }
            else if (this.task == CorruptionCatTentacle.Task.ExamineSound)
            {
                this.ExamineSound(ref this.scratchPath);
            }
            else if (this.task == CorruptionCatTentacle.Task.Grabbing)
            {
                base.MoveGrabDest(vector2 + Custom.DirVec(vector2, this.grabChunk.pos) * 20f, ref this.scratchPath);
                Vector2 p = vector2;
                bool flag2 = this.room.VisualContact(this.grabChunk.pos, vector2);
                for (int l = this.tChunks.Length - 1; l >= 0; l--)
                {
                    Vector2 p2 = base.FloatBase;
                    if (l > 0)
                    {
                        p2 = this.tChunks[l - 1].pos;
                        if (!flag2 && !this.room.VisualContact(this.grabChunk.pos, this.tChunks[l - 1].pos))
                        {
                            p = this.tChunks[l].pos;
                            flag2 = true;
                        }
                    }
                    this.tChunks[l].vel += Custom.DirVec(this.tChunks[l].pos, p2) * 1.2f * SpeedFac;
                    if (this.tChunks[l].phase > -1f || this.room.GetTile(this.tChunks[l].pos).Solid)
                    {
                        this.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Release_Creature, this.grabChunk.pos);
                        this.grabChunk = null;
                        this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
                        break;
                    }
                }
                if (this.task == CorruptionCatTentacle.Task.Grabbing)
                {
                    this.grabChunk.vel += (Vector2)Vector3.Slerp(Custom.DirVec(this.grabChunk.pos, p), Custom.DirVec(base.Tip.pos, this.tChunks[this.tChunks.Length - 2].pos), 0.5f) * Custom.LerpMap((float)this.grabPath.Count, 3f, 18f, 0.65f, 0.25f) * (this.corruptionCat.SizeClass ? 1f : 0.45f) / this.grabChunk.mass;
                }
            }
            for (int m = 0; m < this.tChunks.Length; m++)
            {
                float num5 = (float)m / (float)(this.tChunks.Length - 1);
                if (num5 < 0.2f)
                {
                    this.tChunks[m].vel += Custom.DegToVec(this.awayFromBodyRotation) * Mathf.InverseLerp(0.2f, 0f, num5) * 5f * SpeedFac;
                }
                for (int n = m + 1; n < this.tChunks.Length; n++)
                {
                    base.PushChunksApart(m, n);
                }
            }
            this.Touch();
            /*
            base.Update();
            if (this.stun > 0)
            {
                this.stun--;
            }

            limp = this.stun > 0;

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
            }*/
        }

        public void Climb(ref List<IntVector2> path)
        {
            float t = Custom.LerpMap((float)this.corruptionCat.stuckCounter, 50f, 200f, 0.5f, 0.95f);
            if (this.player.input[0].jmp && this.corruptionCat.HaveDirInput)
                t = Mathf.Clamp01(Mathf.Pow(t, 0.6f));
            if (this.chooseToMoveByPlayer && !this.chooseToMoveByPlayerButNotControl && 
                !this.player.input[0].jmp && this.corruptionCat.HaveDirInput)
                t = 1f;
            Vector2 moveDirection = (this.chooseToMoveByPlayer || player.input[0].jmp) ? this.corruptionCat.moveDirection : Vector2.zero;
            this.idealGrabPos = base.FloatBase + (Vector2)Vector3.Slerp(this.tentacleDir, moveDirection, t) * this.idealLength * 0.7f;
            Vector2 vector = base.FloatBase + 
                (Vector2)Vector3.Slerp((Vector2)Vector3.Slerp(this.tentacleDir, moveDirection, t), Custom.RNV(), Mathf.InverseLerp(20f, 200f, (float)this.foundNoGrabPos)) * 
                this.idealLength * Custom.LerpMap((float)Math.Max(this.foundNoGrabPos, this.corruptionCat.stuckCounter), 20f, 200f, 0.7f, 1.2f);
            int i;
            for (i = SharedPhysics.RayTracedTilesArray(base.FloatBase, vector, this._cachedRays1); i >= this._cachedRays1.Length; i = SharedPhysics.RayTracedTilesArray(base.FloatBase, vector, this._cachedRays1))
            {
                Custom.LogWarning(new string[]
                {
                string.Format("CorruptionCatTentacle Climb ray tracing limit exceeded, extending cache to {0} and trying again!", this._cachedRays1.Length + 100)
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
                if (!(this.chooseToMoveByPlayer && !this.chooseToMoveByPlayerButNotControl) && (this.backtrackFrom == -1 || this.backtrackFrom > k))
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
                            this.tChunks[k].vel += Vector2.ClampMagnitude(this.floatGrabDest.Value - this.tChunks[k].pos, 20f) / 20f * 1.2f * SpeedFac;
                        }
                        else if (k > 1 && this.segments.Count > this.grabPath.Count && flag2)
                        {
                            float num = Mathf.InverseLerp((float)this.grabPath.Count, (float)this.segments.Count, (float)this.tChunks[k].currentSegment);
                            Vector2 a = Custom.DirVec(this.tChunks[k - 2].pos, this.tChunks[k].pos) * (1f - num) * 0.6f;
                            a += Custom.DirVec(this.tChunks[k].pos, this.room.MiddleOfTile(base.grabDest.Value)) * Mathf.Pow(1f - num, 4f) * 2f;
                            a += Custom.DirVec(this.tChunks[k].pos, this.room.MiddleOfTile(this.secondaryGrabPos)) * Mathf.Pow(num, 4f) * 2f;
                            a += Custom.DirVec(this.tChunks[k].pos, base.FloatBase) * Mathf.Sin(num * 3.1415927f) * 0.3f;
                            this.tChunks[k].vel += a.normalized * 1.2f * SpeedFac;
                            if (k == this.tChunks.Length - 1)
                            {
                                this.tChunks[k].vel += Vector2.ClampMagnitude(this.room.MiddleOfTile(this.secondaryGrabPos) - this.tChunks[k].pos, 20f) / 20f * 4.2f * SpeedFac;
                            }
                        }
                    }
                }
            }
            if (base.grabDest != null)
            {
                this.ConsiderSecondaryGrabPos(base.grabDest.Value + new IntVector2(UnityEngine.Random.Range(-20, 21), UnityEngine.Random.Range(-20, 21)));
            }
            if ((base.grabDest == null || !this.atGrabDest))
            {
                this.UpdateClimbGrabPos(ref path);
            }
            if (this.chooseToMoveByPlayer && !this.chooseToMoveByPlayerButNotControl)
            {
                this.neededForLocomotion = true;
                this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
                for (int k = 0; k < this.tChunks.Length; k++)
                {
                    this.tChunks[k].vel += Vector2.ClampMagnitude(vector - this.tChunks[k].pos, 20f) / 20f * 1.2f * 1.5f * SpeedFac;
                    if (k == this.tChunks.Length - 1)
                    {
                        this.tChunks[k].vel += Vector2.ClampMagnitude(vector - this.tChunks[k].pos, 20f) / 20f * 3f * 1.5f * SpeedFac;
                    }
                    /*
                    if (this.tChunks[k].currentSegment <= this.grabPath.Count || !flag2)
                    {
                        this.tChunks[k].vel += Vector2.ClampMagnitude(this.floatGrabDest.Value - this.tChunks[k].pos, 20f) / 20f * 1.2f;
                    }
                    else if (k > 1 && this.segments.Count > this.grabPath.Count && flag2)
                    {
                        Vector2 a = Custom.DirVec(this.tChunks[k - 2].pos, this.tChunks[k].pos) * (1f - num) * 0.6f;
                        a += Custom.DirVec(this.tChunks[k].pos, this.room.MiddleOfTile(base.grabDest.Value)) * Mathf.Pow(1f - num, 4f) * 2f;
                        a += Custom.DirVec(this.tChunks[k].pos, this.room.MiddleOfTile(this.secondaryGrabPos)) * Mathf.Pow(num, 4f) * 2f;
                        a += Custom.DirVec(this.tChunks[k].pos, base.FloatBase) * Mathf.Sin(num * 3.1415927f) * 0.3f;
                        this.tChunks[k].vel += a.normalized * 1.2f;
                        if (k == this.tChunks.Length - 1)
                        {
                            this.tChunks[k].vel += Vector2.ClampMagnitude(this.room.MiddleOfTile(this.secondaryGrabPos) - this.tChunks[k].pos, 20f) / 20f * 4.2f;
                        }
                    }*/
                }
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
                chunk.vel.x = chunk.vel.x + (vector.x - chunk.pos.x) * 0.1f * SpeedFac;
                chunk.vel.y = chunk.vel.y * 0.9f;
            }
            if (vector.y != 0f)
            {
                chunk.vel.y = chunk.vel.y + (vector.y - chunk.pos.y) * 0.1f * SpeedFac;
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
            }/*
            if (this.OppositeDir && this.chooseToMoveByPlayer &&
                Vector2.Dot(this.corruptionCat.moveDirection, testPos - connectedChunk.pos) >= 0 &&
                this.corruptionCat.oppositeDirCount > 1)
            {
                num *= 2f;
            }*/
            return num;
        }

        public float ReleaseScore()
        {
            float num = float.MaxValue;
            for (int i = this.tChunks.Length / 2; i < this.tChunks.Length; i++)
            {
                if (Custom.DistLess(this.tChunks[i].pos, this.idealGrabPos, num))
                {
                    num = Vector2.Distance(this.tChunks[i].pos, this.idealGrabPos);
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

        public void UpdateClimbGrabPos(ref List<IntVector2> path)
        {
            //按投掷键会自动抓猎物
            if (this.player.input[0].thrw && this.huntCreature != null)
            {
                return;
            }
            if (this.corruptionCat.TotalGrip <= 2 && this.atGrabDest)
                return;
            base.MoveGrabDest(this.preliminaryGrabDest, ref path);
        }

        public void LookForCreaturesToHunt()
        {
            if (this.neededForLocomotion)//this.neededForLocomotion || this.player.AI.preyTracker.TotalTrackedPrey == 0
            {
                return;
            }
            //Tracker.CreatureRepresentation creatureRepresentation = this.player.AI.preyTracker.GetTrackedPrey(Random.Range(0, this.player.AI.preyTracker.TotalTrackedPrey));
            if (true)//this.corruptionCat.safariControlled
            {
                if (this.huntDirection == Vector2.zero)
                {
                    this.huntDirection = Custom.RNV() * 80f;
                }
                if (this.corruptionCat.HaveDirInput)
                {
                    this.huntDirection = new Vector2(this.player.input[0].x, this.player.input[0].y) * 80f;
                }
                Creature creature = null;
                float num = float.MaxValue;
                float num2 = Custom.VecToDeg(this.huntDirection);
                for (int i = 0; i < this.player.room.abstractRoom.creatures.Count; i++)
                {
                    if (this.player.abstractCreature != this.player.room.abstractRoom.creatures[i] && this.player.room.abstractRoom.creatures[i].realizedCreature != null)
                    {
                        float num3 = Custom.AimFromOneVectorToAnother(this.player.mainBodyChunk.pos, this.player.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos);
                        float num4 = Custom.Dist(this.player.mainBodyChunk.pos, this.player.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos);
                        if (Mathf.Abs(Mathf.DeltaAngle(num2, num3)) < 22.5f && num4 < num)
                        {
                            num = num4;
                            creature = this.player.room.abstractRoom.creatures[i].realizedCreature;
                        }
                    }
                }
                if (creature != null)
                {
                    this.huntCreature = creature;
                    //creatureRepresentation = this.player.AI.tracker.RepresentationForCreature(creature.abstractCreature, true);
                }
            }
            if (this.huntCreature != null)
            {
                return;
            }
            for (int j = 0; j < this.corruptionCat.tentacles.Length; j++)
            {
                if (this.corruptionCat.tentacles[j].huntCreature == this.huntCreature)
                {
                    return;
                }
            }
            if (this.IsCreatureCaughtEnough(this.huntCreature.abstractCreature))
            {
                return;
            }
            if (this.huntCreature.abstractCreature.pos.room != this.player.abstractCreature.pos.room)
            {
                return;
            }
            if (Vector2.Distance(this.room.MiddleOfTile(this.huntCreature.abstractCreature.pos), base.FloatBase) > this.idealLength + 40f)
            {
                return;
            }
            if (this.checkSound != null)
            {
                this.checkSound.Destroy();
                this.checkSound = null;
            }
            //this.huntCreature = creatureRepresentation;
            this.SwitchTask(CorruptionCatTentacle.Task.Hunt);
        }

        public void ExamineSound(ref List<IntVector2> path)
        {
            if (!Custom.DistLess(this.checkSound.pos, base.FloatBase, this.idealLength * 1.1f) || this.checkSound.slatedForDeletion)
            {
                this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
                return;
            }
            this.soundCheckTimer--;
            if (this.soundCheckTimer < 1 || this.examineSoundPos == null || Custom.DistLess(this.examineSoundPos.Value, base.Tip.pos, 20f))
            {
                if (this.examineSoundPos != null)
                {
                    this.soundCheckTimer = Random.Range(40, 180);
                    this.soundCheckCounter++;
                    if (this.soundCheckCounter > 17)
                    {
                        this.checkSound.Destroy();
                        this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
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
                    if (this.room.aimap.getTerrainProximity(this._cachedRays2[index]) < 2 || ((this.room.GetTile(this._cachedRays2[index]).horizontalBeam || this.room.GetTile(this._cachedRays2[index]).verticalBeam) && Random.value < 0.05f))
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
                        this.tChunks[k].vel += Vector2.ClampMagnitude(this.floatGrabDest.Value - this.tChunks[k].pos, 20f) / 20f * 1.2f * SpeedFac;
                    }
                    else
                    {
                        this.tChunks[k].vel += Vector2.ClampMagnitude(this.room.MiddleOfTile(this.segments[this.tChunks[k].currentSegment]) - this.tChunks[k].pos, 20f) / 20f * 1.2f * SpeedFac;
                    }
                }
            }
        }

        public void LookForSoundsToExamine()
        {
            if (!this.corruptionCat.SizeClass || this.neededForLocomotion)// || this.player.AI.noiseTracker.sources.Count == 0
            {
                return;
            }
            int num = int.MaxValue;
            int num2 = -1;
            /*
            for (int i = 0; i < this.player.AI.noiseTracker.sources.Count; i++)
            {
                if (Custom.DistLess(this.player.AI.noiseTracker.sources[i].pos, base.FloatBase, this.idealLength * 0.85f) && this.player.AI.noiseTracker.sources[i].age < num && this.player.AI.noiseTracker.sources[i].creatureRep == null)
                {
                    bool flag = false;
                    int num3 = 0;
                    while (num3 < this.corruptionCat.tentacles.Length && !flag)
                    {
                        if (this.corruptionCat.tentacles[num3].checkSound == this.player.AI.noiseTracker.sources[i] || (this.player.AI.noiseTracker.sources[i].creatureRep != null && this.corruptionCat.tentacles[num3].huntCreature == this.player.AI.noiseTracker.sources[i].creatureRep))
                        {
                            flag = true;
                        }
                        num3++;
                    }
                    if (!flag)
                    {
                        num = this.player.AI.noiseTracker.sources[i].age;
                        num2 = i;
                    }
                }
            }
            */
            if (num2 > -1)
            {
                //this.checkSound = this.player.AI.noiseTracker.sources[num2];
                this.SwitchTask(CorruptionCatTentacle.Task.ExamineSound);
            }
        }

        public bool IsCreatureCaughtEnough(AbstractCreature crit)
        {
            int num = 0;
            for (int i = 0; i < this.corruptionCat.tentacles.Length; i++)
            {
                if (this.corruptionCat.tentacles[i].grabChunk != null && this.corruptionCat.tentacles[i].grabChunk.owner is Creature && (this.corruptionCat.tentacles[i].grabChunk.owner as Creature).abstractCreature == crit)
                {
                    num++;
                }
            }
            return (float)num >= crit.creatureTemplate.bodySize * (this.corruptionCat.SizeClass ? 1.5f : 2.5f);
        }

        public void Hunt(ref List<IntVector2> path)
        {
            if (this.huntCreature.abstractCreature.pos.room != this.player.abstractCreature.pos.room || this.huntCreature.slatedForDeletetion)
            {
                this.huntCreature = null;
                this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
                return;
            }
            if (VisualContact(huntCreature))
            {
                base.MoveGrabDest(this.huntCreature.mainBodyChunk.pos, ref path);
            }
            else
            {
                if (this.huntCreature.abstractCreature.pos.TileDefined)
                {
                    base.MoveGrabDest(this.room.MiddleOfTile(this.huntCreature.abstractCreature.pos), ref path);
                }/*
                for (int i = 0; i < this.tChunks.Length; i++)
                {
                    if (this.huntCreature is Tracker.ElaborateCreatureRepresentation)
                    {
                        for (int j = 0; j < (this.huntCreature as Tracker.ElaborateCreatureRepresentation).ghosts.Count; j++)
                        {
                            if (this.room.GetTilePosition(this.tChunks[i].pos) == (this.huntCreature as Tracker.ElaborateCreatureRepresentation).ghosts[j].coord.Tile)
                            {
                                (this.huntCreature as Tracker.ElaborateCreatureRepresentation).ghosts[j].Push();
                            }
                        }
                    }
                }*/
            }
            if ((float)this.grabPath.Count * 20f > this.idealLength || this.neededForLocomotion)
            {
                float num = float.MaxValue;
                int num2 = -1;
                for (int k = 0; k < this.corruptionCat.tentacles.Length; k++)
                {
                    if (this.corruptionCat.tentacles[k].task == CorruptionCatTentacle.Task.Locomotion && !this.corruptionCat.tentacles[k].neededForLocomotion && (this.corruptionCat.tentacles[k].idealLength > this.idealLength || this.neededForLocomotion) && !this.corruptionCat.tentacles[k].atGrabDest && Mathf.Abs(this.corruptionCat.tentacles[k].idealLength - (float)this.grabPath.Count * 20f) < num)
                    {
                        num = Mathf.Abs(this.corruptionCat.tentacles[k].idealLength - (float)this.grabPath.Count * 20f);
                        num2 = k;
                    }
                }
                if (num2 > -1)
                {
                    this.corruptionCat.tentacles[num2].huntCreature = this.huntCreature;
                    this.corruptionCat.tentacles[num2].task = CorruptionCatTentacle.Task.Hunt;
                    this.huntCreature = null;
                    this.UpdateClimbGrabPos(ref path);
                    return;
                }
            }
            if (Vector2.Distance(this.room.MiddleOfTile(this.huntCreature.abstractCreature.pos), base.FloatBase) > this.idealLength * 1.5f)
            {
                this.huntCreature = null;
                this.UpdateClimbGrabPos(ref path);
                return;
            }
            for (int l = 0; l < this.tChunks.Length; l++)
            {
                if (this.backtrackFrom == -1 || this.backtrackFrom > l)
                {
                    if (base.grabDest != null && this.room.VisualContact(this.tChunks[l].pos, this.floatGrabDest.Value))
                    {
                        this.tChunks[l].vel += Vector2.ClampMagnitude(this.floatGrabDest.Value - this.tChunks[l].pos, 20f) / 20f * 1.2f;
                    }
                    else
                    {
                        this.tChunks[l].vel += Vector2.ClampMagnitude(this.room.MiddleOfTile(this.segments[this.tChunks[l].currentSegment]) - this.tChunks[l].pos, 20f) / 20f * 1.2f;
                    }
                }
            }
        }

        public void SwitchTask(Task newTask)
        {
            task = newTask;
        }

        public void Touch()
        {
            bool flag = false;
            bool flag2 = this.player.input[0].pckp || this.player.input[0].thrw;//this.player.inputWithDiagonals != null && this.player.inputWithDiagonals.Value.pckp;
            for (int i = 0; i < this.room.abstractRoom.creatures.Count; i++)
            {
                if (this.room.abstractRoom.creatures[i].realizedCreature != null && !this.room.abstractRoom.creatures[i].realizedCreature.inShortcut && 
                    this.room.abstractRoom.creatures[i].realizedCreature != this.player && !this.room.abstractRoom.creatures[i].tentacleImmune && flag2)
                {
                    Creature realizedCreature = this.room.abstractRoom.creatures[i].realizedCreature;
                    for (int j = 0; j < this.tChunks.Length; j++)
                    {
                        int k = 0;
                        while (k < realizedCreature.bodyChunks.Length)
                        {
                            if (Custom.DistLess(this.tChunks[j].pos, realizedCreature.bodyChunks[k].pos, this.tChunks[j].rad + realizedCreature.bodyChunks[k].rad))
                            {/*
                                if (this.corruptionCat.eyesClosed < 1 || Random.value < 0.05f)
                                {
                                    this.player.AI.tracker.SeeCreature(realizedCreature.abstractCreature);
                                    if (this.corruptionCat.graphics != null)
                                    {
                                        Tracker.CreatureRepresentation creatureRep = this.player.AI.tracker.RepresentationForObject(realizedCreature, false);
                                        (this.corruptionCat.graphics as CorruptionCatGraphics).FeelSomethingWithTentacle(creatureRep, this.tChunks[j].pos);
                                    }
                                }*/
                                if (realizedCreature.abstractCreature.creatureTemplate.AI && realizedCreature.abstractCreature.abstractAI.RealAI != null && realizedCreature.abstractCreature.abstractAI.RealAI.tracker != null)
                                {
                                    realizedCreature.abstractCreature.abstractAI.RealAI.tracker.SeeCreature(this.player.abstractCreature);
                                }
                                this.CollideWithCreature(j, realizedCreature.bodyChunks[k]);
                                if (!this.neededForLocomotion && realizedCreature.newToRoomInvinsibility < 1 && 
                                    this.grabChunk == null && j == this.tChunks.Length - 1 && 
                                    (this.corruptionCat.SizeClass || this.corruptionCat.digestingCounter < 1) && 
                                    (this.corruptionCat.eyesClosed < 1 || Random.value < (this.corruptionCat.SizeClass ? 0.5f : 0.15f)) && 
                                    (this.task == CorruptionCatTentacle.Task.Hunt || !this.IsCreatureCaughtEnough(realizedCreature.abstractCreature)))
                                {
                                    flag = true;
                                    if (Vector2.Distance(this.tChunks[j].vel, realizedCreature.bodyChunks[k].vel) >= Mathf.Lerp(1f, 8f, this.sticky))
                                    {
                                        break;
                                    }
                                    bool flag3 = false;/*
                                    if (this.player.AI.tracker.RepresentationForObject(realizedCreature, false) != null && this.player.AI.DynamicRelationship(this.player.AI.tracker.RepresentationForObject(realizedCreature, false)).type == CreatureTemplate.Relationship.Type.Eats)
                                    {
                                        flag3 = true;
                                    }*/
                                    int num = 0;
                                    while (num < this.tChunks.Length && flag3)
                                    {
                                        if (this.tChunks[num].phase > -1f || this.room.GetTile(this.tChunks[num].pos).Solid)
                                        {
                                            flag3 = false;
                                        }
                                        num++;
                                    }
                                    if (flag3)
                                    {
                                        this.grabChunk = realizedCreature.bodyChunks[k];
                                        this.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Grab_Creature, this.tChunks[j].pos, 1f, 1f);
                                        this.SwitchTask(CorruptionCatTentacle.Task.Grabbing);
                                        return;
                                    }
                                    break;
                                }
                                else
                                {
                                    if (this.neededForLocomotion || (!(this.task == CorruptionCatTentacle.Task.Locomotion) && !(this.task == CorruptionCatTentacle.Task.ExamineSound)) || this.IsCreatureCaughtEnough(realizedCreature.abstractCreature))
                                    {
                                        break;
                                    }/*
                                    Tracker.CreatureRepresentation creatureRepresentation = this.player.AI.tracker.RepresentationForObject(realizedCreature, false);
                                    if (creatureRepresentation == null || !(this.player.AI.DynamicRelationship(creatureRepresentation).type == CreatureTemplate.Relationship.Type.Eats))
                                    {
                                        break;
                                    }
                                    bool flag4 = false;
                                    int num2 = 0;
                                    while (num2 < this.corruptionCat.tentacles.Length && !flag4)
                                    {
                                        if (this.corruptionCat.tentacles[num2].huntCreature == creatureRepresentation)
                                        {
                                            flag4 = true;
                                        }
                                        num2++;
                                    }
                                    if (!flag4)
                                    {
                                        this.huntCreature = creatureRepresentation;
                                        if (this.checkSound != null)
                                        {
                                            this.checkSound.Destroy();
                                            this.checkSound = null;
                                        }
                                        this.SwitchTask(CorruptionCatTentacle.Task.Hunt);
                                        break;
                                    }
                                    break;*/
                                }
                            }
                            else
                            {
                                k++;
                            }
                        }
                    }
                }
            }
            if (flag)
            {
                this.sticky = Mathf.Min(1f, this.sticky + 0.033333335f);
                return;
            }
            this.sticky = Mathf.Max(0f, this.sticky - 0.016666668f);
        }

        public void CollideWithCreature(int tChunk, BodyChunk creatureChunk)
        {
            if (this.backtrackFrom > -1 && this.backtrackFrom <= tChunk)
            {
                return;
            }
            float num = Vector2.Distance(this.tChunks[tChunk].pos, creatureChunk.pos);
            float num2 = (this.tChunks[tChunk].rad + creatureChunk.rad) / 4f;
            Vector2 vector = Custom.DirVec(this.tChunks[tChunk].pos, creatureChunk.pos);
            float num3 = creatureChunk.mass / (creatureChunk.mass + 0.01f);
            float num4 = 0.8f;
            this.tChunks[tChunk].pos += vector * (num - num2) * num3 * num4 * SpeedFac;
            this.tChunks[tChunk].vel += vector * (num - num2) * num3 * num4 * SpeedFac;
            creatureChunk.pos -= vector * (num - num2) * (1f - num3) * num4 * SpeedFac;
            creatureChunk.vel -= vector * (num - num2) * (1f - num3) * num4 * SpeedFac;
        }

        public static bool VisualContact(Creature huntCreature)
        {
            return true;
        }

        /*
        private void ExamineSound(ref List<IntVector2> path)
        {
            if (!Custom.DistLess(this.checkSound.pos, base.FloatBase, this.idealLength * 1.1f) || this.checkSound.slatedForDeletion)
            {
                this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
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
                        this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
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
            ExamineSound,
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

        internal CorruptionCatTentacle tentacle
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
