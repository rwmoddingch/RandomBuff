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
using System.Text;
using System.Threading.Tasks;
using HotDogGains.Positive;

namespace BuiltinBuffs.Duality
{
    internal class SpiderShapedMutationBuff : Buff<SpiderShapedMutationBuff, SpiderShapedMutationBuffData>
    {
        public override BuffID ID => SpiderShapedMutationBuffEntry.SpiderShapedMutation;

        public int SpiderLevel
        {
            get
            {
                int num = 0;
                if (GetTemporaryBuffPool().allBuffIDs.Contains(BuiltinBuffs.Negative.ArachnophobiaIBuffEntry.arachnophobiaID))
                    num++;
                if (GetTemporaryBuffPool().allBuffIDs.Contains(HotDogGains.Positive.SpiderSenseBuffEntry.SpiderSenseID))
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
                    var spider = new SpiderCat(player);
                    SpiderShapedMutationBuffEntry.SpiderFeatures.Add(player, spider);
                    spider.BirdArthropod(player.graphicsModule as PlayerGraphics);
                    spider.InitiateSprites(game.cameras[0].spriteLeasers.
                        First(i => i.drawableObject == player.graphicsModule), game.cameras[0]);
                }
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

        public static ConditionalWeakTable<Player, SpiderCat> SpiderFeatures = new ConditionalWeakTable<Player, SpiderCat>();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SpiderShapedMutationBuff, SpiderShapedMutationBuffData, SpiderShapedMutationBuffEntry>(SpiderShapedMutation);
        }

        public static void HookOn()
        {
            IL.FlareBomb.Update += FlareBomb_UpdateIL; 
            On.SporeCloud.Update += SporeCloud_Update;
            On.SlugcatStats.SlugcatCanMaul += SlugcatStats_SlugcatCanMaul;
            On.Player.CanEatMeat += Player_CanEatMeat;
            On.Player.BiteEdibleObject += Player_BiteEdibleObject;

            On.BigSpiderAI.IUseARelationshipTracker_UpdateDynamicRelationship += BigSpiderAI_IUseARelationshipTracker_UpdateDynamicRelationship;
            On.Spider.ConsiderPrey += Spider_ConsiderPrey;
            On.Spider.Centipede.SeePrey += Spider_Centipede_SeePrey;

            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
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
                            player.Die();
                            if (self.thrownBy != null)
                            {
                                player.SetKillTag(self.thrownBy.abstractCreature);
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

        //修改食谱
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
            if (SpiderFeatures.TryGetValue(self.owner.owner as Player, out var spider))
                (self.owner.owner as Player).craftingObject = true;
            orig(self);

            if (SpiderFeatures.TryGetValue(self.owner.owner as Player, out spider))
                spider.SlugcatHandUpdate(self);
        }

        //只能一次叼一个东西
        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            Player.ObjectGrabability result = orig(self, obj);

            if (SpiderFeatures.TryGetValue(self, out var spider))
            {
                result = spider.Grabability(result);
            }

            return result;
        }

        private static int Player_FreeHand(On.Player.orig_FreeHand orig, Player self)
        {
            int result = orig(self);
            if (SpiderFeatures.TryGetValue(self, out var spider))
                if (self.grasps[0] != null || self.grasps[0] != null)
                {
                    result = -1;
                }
            return result;
        }
        #endregion
        #region 生物关系
        //修改生物关系（狼蛛不再攻击玩家）
        private static CreatureTemplate.Relationship BigSpiderAI_IUseARelationshipTracker_UpdateDynamicRelationship(On.BigSpiderAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, BigSpiderAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            CreatureTemplate.Relationship result = orig(self, dRelation);
            if (dRelation.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat)
                result = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f);
            return result;
        }

        //修改生物关系（小蜘蛛不再攻击玩家）
        private static bool Spider_ConsiderPrey(On.Spider.orig_ConsiderPrey orig, Spider self, Creature crit)
        {
            bool result = orig(self, crit);
            if (crit is Player)
                result = true;
            return result;
        }

        private static void Spider_Centipede_SeePrey(On.Spider.Centipede.orig_SeePrey orig, Spider.Centipede self, Creature creature)
        {
            orig(self, creature);
            if(self.prey is Player)
                self.prey = null;
        }
        #endregion

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!SpiderFeatures.TryGetValue(self, out _))
                SpiderFeatures.Add(self, new SpiderCat(self));
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (SpiderFeatures.TryGetValue(self, out var spider))
            {
                spider.Update();
                self.GetExPlayerData().HaveHands = false;
            }
        }

        private static void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            orig(self);
            if (SpiderFeatures.TryGetValue(self, out var spider))
                spider.Jump(self.bodyChunks[0].vel.normalized, 1f);
        }

        #region 外观
        private static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (SpiderFeatures.TryGetValue(self.player, out var spider))
                spider.ApplyPalette(sLeaser, rCam, palette);
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (SpiderFeatures.TryGetValue(self.player, out var spider))
                spider.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (SpiderFeatures.TryGetValue(self.player, out var spider))
            {
                spider.InitiateSprites(sLeaser, rCam);
            }
        }

        private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (SpiderFeatures.TryGetValue(self.player, out var spider))
                spider.AddToContainer(sLeaser, rCam, newContatiner);
        }

        private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if (SpiderFeatures.TryGetValue(self.player, out var spider))
                spider.BirdArthropod(self);
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            if (SpiderFeatures.TryGetValue(self.player, out var spider))
                spider.GraphicsUpdate();
        }

        private static void PlayerGraphics_Reset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig(self);
            if (SpiderFeatures.TryGetValue(self.player, out var spider))
                spider.Reset(self);
        }
        #endregion
    }

    internal class SpiderCat
    {
        WeakReference<Player> ownerRef;

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

        Vector2 modify = Vector2.down;

        public bool Footing
        {
            get
            {
                return this.footingCounter > 20 || this.outOfWaterFooting > 0;
            }
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

        public void BirdArthropod(PlayerGraphics self)
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

            this.arthropodSpeed = 30f;
            this.wantPos = player.bodyChunks[0].pos;

            this.legLength = 65f + SpiderShapedMutationBuff.spiderLevel * 20f;
            this.legFlips = new float[2, 4, 2];
            this.legsTravelDirs = new Vector2[2, 4];
            this.legsThickness = Mathf.Lerp(0.7f, 1.1f, UnityEngine.Random.value);

            this.deathConvulsions = (player.State.alive ? 1f : 0f);
        }

        #region 外观
        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            arthropodSprite = sLeaser.sprites.Length;
            Array.Resize(ref sLeaser.sprites, arthropodSprite + this.legs.GetLength(0) * this.legs.GetLength(1) * 3);

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
                }
            }

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
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (!ownerRef.TryGetTarget(out var player) || player.graphicsModule == null || sLeaser == null)
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            
            if (sLeaser.sprites.Length >= 9)
                for (int i = 4; i <= 8; i++)
                    sLeaser.sprites[i].isVisible = false;
            
            float flip = Mathf.Lerp(this.lastFlip, this.flip, timeStacker);
            float num = 0f; // player.superLaunchJump / 20f;
            Vector2 bodyPos = Vector2.Lerp(player.mainBodyChunk.lastPos, player.mainBodyChunk.pos, timeStacker);
            Vector2 hipsPos = Vector2.Lerp(player.bodyChunks[1].lastPos, player.bodyChunks[1].pos, timeStacker) + Custom.RNV() * Random.value * 3.5f * num;
            Vector2 formHipsToBody = Custom.DirVec(hipsPos, bodyPos);
            Vector2 a = Custom.PerpendicularVector(formHipsToBody);
            for (int j = 0; j < this.legs.GetLength(0); j++)
            {
                for (int k = 0; k < this.legs.GetLength(1); k++)
                {
                    float t = Mathf.InverseLerp(0f, (float)(this.legs.GetLength(1) - 1), (float)k);
                    Vector2 rootPos = Vector2.Lerp(bodyPos, hipsPos, 0.3f);
                    //蛛腿根部位置
                    rootPos += a * ((j == 0) ? -1f : 1f) * 3f * (1f - Mathf.Abs(flip));
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
                    jointPos = Vector2.Lerp(jointPos, (rootPos + legPos) * 0.5f - a * flip * Custom.LerpMap(Vector2.Distance(rootPos, legPos), 0f, this.legLength * 1.3f, this.legLength * 0.7f, 0f, 3f), Mathf.Abs(flip));
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
        }

        public void GraphicsUpdate()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
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
                            modify = Vector2.down;
                            //如果可以抓天花板，就抓天花板
                            for(int t = 0; t <= Mathf.Floor(legLength / 20f); t++)
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
                        }
                        else if (!Custom.DistLess(vector3, this.legs[n, m].absoluteHuntPos, this.legLength * num11 * Mathf.Pow(1f - num8, 0.2f)))
                        {
                            this.legs[n, m].mode = Limb.Mode.Dangle;
                        }
                    }
                    num6++;
                }
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
        }
        #endregion

        //进行更新
        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var self))
                return;
            if (self.graphicsModule != null &&
                self.room != null && self.Consious && 
                !self.room.aimap.TileAccessibleToCreature(self.mainBodyChunk.pos, self.Template) && 
                !self.room.aimap.TileAccessibleToCreature(self.bodyChunks[1].pos, self.Template))
            {
                for (int l = 0; l < this.legs.GetLength(0); l++)
                {
                    for (int m = 0; m < this.legs.GetLength(1); m++)
                    {
                        if (this.legs[l, m].reachedSnapPosition &&
                            (!TileAccessibleToPlayer() ||
                            (Custom.DistLess(wantPos, self.bodyChunks[0].pos, 20f) &&
                            !Custom.DistLess(self.mainBodyChunk.pos, this.legs[l, m].absoluteHuntPos, this.legLength) &&
                            Custom.DistLess(self.mainBodyChunk.pos, this.legs[l, m].absoluteHuntPos, this.legLength + 15f))))
                        {
                            Vector2 a = Custom.DirVec(self.mainBodyChunk.pos, this.legs[l, m].absoluteHuntPos) * (Vector2.Distance(self.mainBodyChunk.pos, this.legs[l, m].absoluteHuntPos) - this.legLength);
                            self.mainBodyChunk.pos += a * 0.8f;
                            self.mainBodyChunk.vel += a * 0.8f;
                        }/*
                        if (this.legs[l, m].reachedSnapPosition && modify == Vector2.up)
                        {
                            Vector2 a = Vector2.up * (Vector2.Distance(self.mainBodyChunk.pos, this.legs[l, m].absoluteHuntPos) - this.legLength);
                            self.mainBodyChunk.pos += a * 0.8f;
                            self.mainBodyChunk.vel += a * 0.8f;
                        }*/
                    }
                }
            }
            if (self.Consious)
            {
                if (self.room.aimap.TileAccessibleToCreature(self.bodyChunks[0].pos, self.Template) || self.room.aimap.TileAccessibleToCreature(self.bodyChunks[0].pos, self.Template)) //
                {
                    this.footingCounter++;
                }
                this.Act(self);
            }
            else
            {
                this.footingCounter = 0;
                this.jumping = false;
                if (this.deathConvulsions > 0f)
                {
                    if (self.dead)
                    {
                        this.deathConvulsions = Mathf.Max(0f, this.deathConvulsions - UnityEngine.Random.value / 80f);
                    }
                    if (self.mainBodyChunk.ContactPoint.x != 0 || self.mainBodyChunk.ContactPoint.y != 0)
                    {
                        self.mainBodyChunk.vel += Custom.RNV() * UnityEngine.Random.value * 8f * Mathf.Pow(this.deathConvulsions, 0.5f);
                    }
                    if (self.bodyChunks[1].ContactPoint.x != 0 || self.bodyChunks[1].ContactPoint.y != 0)
                    {
                        self.bodyChunks[1].vel += Custom.RNV() * UnityEngine.Random.value * 4f * Mathf.Pow(this.deathConvulsions, 0.5f);
                    }
                    if (UnityEngine.Random.value < 0.05f)
                    {
                        self.room.PlaySound(SoundID.Big_Spider_Death_Rustle, self.mainBodyChunk, false, 0.5f + UnityEngine.Random.value * 0.5f * this.deathConvulsions, 0.9f + 0.3f * this.deathConvulsions);
                    }
                    if (UnityEngine.Random.value < 0.025f)
                    {
                        self.room.PlaySound(SoundID.Big_Spider_Take_Damage, self.mainBodyChunk, false, UnityEngine.Random.value * 0.5f, 1f);
                    }
                }
            }
        }

        //移动速度
        private void Act(Player self)
        {
            if (self.Submersion > 0.3f)
            {
                //this.Swim();
                return;
            }

            PlayerMoveByLegs();

            if (this.jumping)
            {
                bool flag = false;
                for (int i = 0; i < self.bodyChunks.Length; i++)
                {
                    if ((self.bodyChunks[i].ContactPoint.x != 0 || 
                        self.bodyChunks[i].ContactPoint.y != 0 ||
                        self.bodyMode == Player.BodyModeIndex.WallClimb ||
                        self.animation == Player.AnimationIndex.HangFromBeam || 
                        self.animation == Player.AnimationIndex.ClimbOnBeam ||
                        self.animation == Player.AnimationIndex.AntlerClimb ||
                        self.animation == Player.AnimationIndex.VineGrab ||
                        self.animation == Player.AnimationIndex.ZeroGPoleGrab ||
                        self.animation == Player.AnimationIndex.HangUnderVerticalBeam) && 
                        self.room.aimap.TileAccessibleToCreature(self.bodyChunks[i].pos, self.Template))
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
                self.bodyChunks[0].vel += Custom.DirVec(self.bodyChunks[0].pos, pos) * 1f;
                self.bodyChunks[1].vel -= Custom.DirVec(self.bodyChunks[0].pos, pos) * 0.5f;

                if (self.graphicsModule != null)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        for (int k = 0; k < 2; k++)
                        {
                            this.legs[j, k].mode = Limb.Mode.Dangle;
                            this.legFlips[j, k, 0] = ((j == 0) ? -1f : 1f);
                            this.legs[j, k].vel += Vector3.Slerp(Custom.DirVec(self.mainBodyChunk.pos, wantPos), Custom.PerpendicularVector(self.mainBodyChunk.pos, wantPos) * ((j == 0) ? -1f : 1f), (k == 0) ? 0.1f : 0.5f).ToVector2InPoints() * 3f;
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
            if (!ownerRef.TryGetTarget(out var self))
                return;
            float scale = self.simulateHoldJumpButton >= 6 ? 2f : 1f;
            float num = Custom.AimFromOneVectorToAnother(self.bodyChunks[1].pos, self.bodyChunks[0].pos);
            if (self.superLaunchJump >= 19)
            {
                for (int i = 0; i < this.legs.GetLength(0); i++)
                    for (int j = 0; j < this.legs.GetLength(1); j++)
                        this.legs[i, j].FindGrip(self.room, self.bodyChunks[0].pos, self.bodyChunks[0].pos + 20f * Mathf.Abs(Mathf.Sin(num)) * Vector2.down, this.legLength * 1.1f, self.bodyChunks[0].pos + 100f * Mathf.Abs(Mathf.Sin(num)) * Vector2.down, -2, -2, false);
            }
            float d = Custom.LerpMap(jumpDir.y, -1f, 1f, 0.7f, 1.2f, 1.1f);
            this.footingCounter = 0;
            self.mainBodyChunk.vel *= 0.5f;
            self.bodyChunks[1].vel *= 0.5f;
            self.mainBodyChunk.vel += jumpDir * 8f * d * scale;
            self.bodyChunks[1].vel += jumpDir * 5.5f * d * scale;
            this.jumping = true;
            if (self.graphicsModule != null)
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
            self.room.PlaySound(SoundID.Big_Spider_Jump, self.mainBodyChunk, false, soundVol, 1f);
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
            if (!ownerRef.TryGetTarget(out var self))
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
            if (!ownerRef.TryGetTarget(out var self))
                return false;
            int num = 0;
            for (int l = 0; l < 2; l++)
            {
                for (int m = 0; m < 4; m++)
                {
                    if (this.legs[l, m].reachedSnapPosition &&
                        Custom.DistLess(self.bodyChunks[0].pos, this.legs[l, m].pos, 1.1f * this.legLength))
                    {
                        num++;
                    }
                }
            }
            if (num >= 2)
                return true;
            return false;
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
                result = Player.ObjectGrabability.Drag;
            else if (result == Player.ObjectGrabability.Drag)
                result = Player.ObjectGrabability.Drag;

            return result;
        }
        #endregion
    }
}
