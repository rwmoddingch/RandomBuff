using RandomBuff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuff.Core.Buff;
using UnityEngine;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using RWCustom;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Color = UnityEngine.Color;
using System.Collections.Generic;
using MoreSlugcats;
using RandomBuff.Core.SaveData;
using BuiltinBuffs.Duality;
using System.Text.RegularExpressions;
using System.Numerics;
using System.Runtime.Remoting.Messaging;
using Newtonsoft.Json.Linq;
using BuiltinBuffs.Negative;
using System.Reflection;
using System.Drawing;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace BuiltinBuffs.Duality
{
    internal class VultureShapedMutationBuff : Buff<VultureShapedMutationBuff, VultureShapedMutationBuffData>
    {
        public override bool Triggerable => true;//VultureShapedMutationBuffEntry.StackLayer >= 3;//

        public override BuffID ID => VultureShapedMutationBuffEntry.VultureShapedMutation;
        //飞行速度
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
        //发射角矛时会发射可以击落飞行物的防御导弹
        public bool ActiveProtectionSysytem => GetTemporaryBuffPool().allBuffIDs.Contains(BuiltinBuffs.Negative.ActiveProtectionSystemBuffEntry.activeProtectionSysytem);
        //大幅提高角矛的射击和回收速度
        public bool AerialFirepower => GetTemporaryBuffPool().allBuffIDs.Contains(HotDogGains.Negative.AerialFirepowerBuffEntry.AerialFirepowerID);
        //角矛载弹量提升两倍，并大幅增加发射速度
        public bool ArmedKingVulture => GetTemporaryBuffPool().allBuffIDs.Contains(HotDogGains.Negative.ArmedKingVultureBuffEntry.ArmedKingVultureID);
        //角矛发射后会追踪一点时间后快速射出，并且获得更快的发射速度和更远的攻击范围，攻击到目标或墙的情况会发生一次无伤害的爆炸
        //public bool RocketBoostTusks => GetTemporaryBuffPool().allBuffIDs.Contains(BuiltinBuffs.Negative.RocketBoostTusksEntry.RocketBoostTusks);

        public VultureShapedMutationBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    if (VultureShapedMutationBuffEntry.VultureCatFeatures.TryGetValue(player, out _))
                        VultureShapedMutationBuffEntry.VultureCatFeatures.Remove(player);
                    var vultureCat = new VultureCat(player);
                    VultureShapedMutationBuffEntry.VultureCatFeatures.Add(player, vultureCat);
                    vultureCat.VultureWing(player.graphicsModule as PlayerGraphics);
                    vultureCat.KingTusk(player.graphicsModule as PlayerGraphics);
                    vultureCat.InitiateSprites(game.cameras[0].spriteLeasers.
                        First(i => i.drawableObject == player.graphicsModule), game.cameras[0]);
                }
            }
        }
    }

    internal class VultureShapedMutationBuffData : BuffData
    {
        public override BuffID ID => VultureShapedMutationBuffEntry.VultureShapedMutation;
    }

    internal class VultureShapedMutationBuffEntry : IBuffEntry
    {
        public static BuffID VultureShapedMutation = new BuffID("VultureShapedMutation", true);

        public static ConditionalWeakTable<Player, VultureCat> VultureCatFeatures = new ConditionalWeakTable<Player, VultureCat>();

        public static int StackLayer
        {
            get
            {
                return VultureShapedMutation.GetBuffData().StackLayer;
            }
        }

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<VultureShapedMutationBuff, VultureShapedMutationBuffData, VultureShapedMutationBuffEntry>(VultureShapedMutation);
        }

        public static void HookOn()
        {
            IL.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;

            IL.Player.MovementUpdate += Player_MovementUpdate;

            On.VultureAI.IUseARelationshipTracker_UpdateDynamicRelationship += VultureAI_UpdateDynamicRelationship;
            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += LizardAI_UpdateDynamicRelationship;

            On.Player.CanEatMeat += Player_CanEatMeat;
            On.SlugcatStats.NourishmentOfObjectEaten += SlugcatStats_NourishmentOfObjectEaten;
            On.MoreSlugcats.SlugNPCAI.TheoreticallyEatMeat += SlugNPCAI_TheoreticallyEatMeat;

            On.SlugcatHand.Update += SlugcatHand_Update;
            On.Player.Grabability += Player_Grabability;

            On.VultureMask.DrawSprites += VultureMask_DrawSprites;

            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.Player.MovementUpdate += Player_MovementUpdate;
            On.Player.Jump += Player_Jump;
            On.Player.NewRoom += Player_NewRoom;
            
            On.PlayerGraphics.ctor += PlayerGraphics_ctor;
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
            On.PlayerGraphics.Reset += PlayerGraphics_Reset;
        }

        public static void LongLifeCycleHookOn()
        {
            On.SlugcatStats.SlugcatFoodMeter += SlugcatStats_SlugcatFoodMeter;
        }

        #region 额外特性
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
        
        //食量增大（需求+2，存储-1）
        private static IntVector2 SlugcatStats_SlugcatFoodMeter(On.SlugcatStats.orig_SlugcatFoodMeter orig, SlugcatStats.Name slugcat)
        {
            IntVector2 origFoodRequirement = orig(slugcat);
            int newHibernateRequirement = origFoodRequirement.y + (StackLayer >= 3 ? 4 : 2);
            int newTotalFoodRequirement = origFoodRequirement.x + (StackLayer >= 3 ? 2 : 1);
            if (newHibernateRequirement > newTotalFoodRequirement)
            {
                newHibernateRequirement++;
                newTotalFoodRequirement = newHibernateRequirement;
            }

            return new IntVector2(newTotalFoodRequirement, newHibernateRequirement);
        }

        //双手位置
        private static void SlugcatHand_Update(On.SlugcatHand.orig_Update orig, SlugcatHand self)
        {
            if (VultureCatFeatures.TryGetValue(self.owner.owner as Player, out var vultureCat))
                (self.owner.owner as Player).craftingObject = true;
            orig(self);

            if (VultureCatFeatures.TryGetValue(self.owner.owner as Player, out vultureCat))
                vultureCat.SlugcatHandUpdate(self);
        }

        //只能一次叼一个东西
        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            Player.ObjectGrabability result = orig(self, obj);

            if (VultureCatFeatures.TryGetValue(self, out var vultureCat))
            {
                result = vultureCat.Grabability(result, obj);
            }

            return result;
        }
        #endregion
        #region 生物关系
        //修改生物关系（携带面具时，秃鹫、魔王鹫不再攻击玩家）
        /*
        public static void EstablishRelationship()
        {
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.Vulture, CreatureTemplate.Type.Slugcat, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.KingVulture, CreatureTemplate.Type.Slugcat, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f));
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.MirosBird, CreatureTemplate.Type.Slugcat, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f));
            StaticWorld.EstablishRelationship(MoreSlugcatsEnums.CreatureTemplateType.MirosVulture, CreatureTemplate.Type.Slugcat, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f));
        }*/
        public static CreatureTemplate.Relationship VultureAI_UpdateDynamicRelationship(On.VultureAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, VultureAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            CreatureTemplate.Relationship result = orig(self, dRelation);
            if ((self.vulture.State as Vulture.VultureState).mask && !self.IsMiros)
            {
                CreatureTemplate.Relationship relationship = self.StaticRelationship(dRelation.trackerRep.representedCreature);
                if (dRelation.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat &&
                    dRelation.trackerRep.representedCreature.realizedCreature is Player player &&
                    VultureCatFeatures.TryGetValue(player, out var vultureCat))
                {
                    if (vultureCat.State.mask && self.vulture.killTag != player.abstractCreature)
                        result = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f);
                }
            }
            return result;
        }

        //修改生物关系（蜥蜴会恐惧携带面具的鹫形玩家）
        public static CreatureTemplate.Relationship LizardAI_UpdateDynamicRelationship(On.LizardAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, LizardAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            CreatureTemplate.Relationship result = orig(self, dRelation);
            if (StackLayer >= 2)
            {
                CreatureTemplate.Relationship relationship = self.StaticRelationship(dRelation.trackerRep.representedCreature);
                if (dRelation.trackerRep.representedCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat &&
                    dRelation.trackerRep.representedCreature.realizedCreature is Player player &&
                    VultureCatFeatures.TryGetValue(player, out var vultureCat) &&
                    (vultureCat.mask != null || vultureCat.State.mask))
                {
                    float maskFac = ((dRelation.state as LizardAI.LizardTrackState).vultureMask == 2) ? 1.5f : 1f;
                    bool immunity = (self.creature.creatureTemplate.type == CreatureTemplate.Type.GreenLizard && (dRelation.state as LizardAI.LizardTrackState).vultureMask < 2) ||
                                    (self.creature.creatureTemplate.type == CreatureTemplate.Type.RedLizard);
                    bool canIntimidate = (self.lizard.lizardParams.biteDamage * Mathf.Pow(self.lizard.TotalMass, 0.5f) <= (1.2f + 0.1f * StackLayer) * maskFac);
                    if (!immunity && canIntimidate)//永久恐吓
                    {
                        self.usedToVultureMask = 0;//恐惧计时一直归零
                    }
                    /*
                    if (self.creature.creatureTemplate.type == CreatureTemplate.Type.GreenLizard && (dRelation.state as LizardAI.LizardTrackState).vultureMask < 2)
                    {
                        return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f);
                    }
                    return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid, Mathf.InverseLerp((float)(((dRelation.state as LizardAI.LizardTrackState).vultureMask == 2) ? 1200 : 700), 
                        600f, 
                        (float)self.usedToVultureMask) * (((dRelation.state as LizardAI.LizardTrackState).vultureMask == 2) ? 
                        ((self.creature.creatureTemplate.type == CreatureTemplate.Type.GreenLizard) ? 0.4f : 0.9f) : 
                        ((self.creature.creatureTemplate.type == CreatureTemplate.Type.BlueLizard) ? 0.8f : 0.6f)));*/
                }
            }
            return result;
        }

        //修改生物关系（钢鸟、钢鹫不再攻击玩家）（未应用）
        public static bool VultureAI_DoIWantToBiteCreature(On.VultureAI.orig_DoIWantToBiteCreature orig, VultureAI self, AbstractCreature creature)
        {
            bool result = orig(self, creature);
            if (creature.creatureTemplate.type == CreatureTemplate.Type.Slugcat)
                result = false;
            return result;
        }
        #endregion
        #region 调整
        //佩戴面具时让面具抬一下
        private static void VultureMask_DrawSprites(On.VultureMask.orig_DrawSprites orig, VultureMask self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (self.grabbedBy.Count > 0 && self.grabbedBy[0].grabber is Player player &&
                VultureCatFeatures.TryGetValue(player, out var vultureCat))
            {
                if (vultureCat.wearCount >= 20 && vultureCat.wearCount <= 40)
                {
                    float num = Mathf.Lerp(self.lastDonned, self.donned, timeStacker);
                    Vector2 vector3 = Vector3.Slerp(self.lastRotationB, self.rotationB, timeStacker);
                    self.maskGfx.overrideDrawVector += Custom.DirVec(Vector2.Lerp(player.bodyChunks[1].lastPos, player.bodyChunks[1].pos, timeStacker), Vector2.Lerp(player.bodyChunks[0].lastPos, player.bodyChunks[0].pos, timeStacker)) *
                                                       7f * Mathf.InverseLerp(40f, 20f, vultureCat.wearCount);
                    self.maskGfx.overrideAnchorVector = Vector3.Slerp(vector3, new Vector2(0f, -1f), num);
                    self.maskGfx.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                    if (self.slatedForDeletetion || self.room != rCam.room)
                    {
                        sLeaser.CleanSpritesAndRemove();
                    }
                }
            }
        }

        //飞行时不抓杆子
        private static void Player_MovementUpdate(ILContext il)
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
                    if (VultureCatFeatures.TryGetValue(player, out var vultureCat) &&
                        vultureCat.isFlying)
                    {
                        player.wantToGrab = 0;
                    }
                });
            }
            else
                BuffUtils.LogError(VultureShapedMutation, "IL HOOK FAILED");
        }
        #endregion

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!VultureCatFeatures.TryGetValue(self, out _))
            {
                VultureCatFeatures.Add(self, new VultureCat(self));
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (VultureCatFeatures.TryGetValue(self, out var vultureCat))
            {
                vultureCat.Update();
                self.GetExPlayerData().HaveHands = false;
            }
        }

        private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            if (VultureCatFeatures.TryGetValue(self, out var vultureCat))
                vultureCat.MovementUpdate(orig, eu);
            orig(self, eu);
        }

        private static void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            orig(self);
            if (VultureCatFeatures.TryGetValue(self, out var vultureCat))
                vultureCat.Jump();
        }

        private static void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            orig(self, newRoom);

            if (VultureCatFeatures.TryGetValue(self, out var vultureCat))
                vultureCat.NewRoom(self.room);
        }

        #region 外观
        private static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (VultureCatFeatures.TryGetValue(self.player, out var vultureCat))
                vultureCat.ApplyPalette(sLeaser, rCam, palette);
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (VultureCatFeatures.TryGetValue(self.player, out var vultureCat))
                vultureCat.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (VultureCatFeatures.TryGetValue(self.player, out var vultureCat))
            {
                vultureCat.InitiateSprites(sLeaser, rCam);
            }
        }

        private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (VultureCatFeatures.TryGetValue(self.player, out var vultureCat))
                vultureCat.AddToContainer(sLeaser, rCam, newContatiner);
        }

        private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if (VultureCatFeatures.TryGetValue(self.player, out var vultureCat))
            {
                vultureCat.VultureWing(self);
                vultureCat.KingTusk(self);
            }
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            if (VultureCatFeatures.TryGetValue(self.player, out var vultureCat))
                vultureCat.GraphicsUpdate();
        }
        
        private static void PlayerGraphics_Reset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig(self);
            if (VultureCatFeatures.TryGetValue(self.player, out var vultureCat))
                vultureCat.Reset(self);
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
                BuffUtils.LogError(VultureShapedMutation, "IL HOOK FAILED");
        }

        public static int UpdateSpeed = 1000;
        #endregion
    }

    internal class VultureCat
    {
        WeakReference<Player> ownerRef;
        private int origThrowingSkill;

        public bool ChargingSnap
        {
            get
            {
                return this.snapFrames > 0 && this.snapFrames > 21;
            }
        }

        public bool Snapping
        {
            get
            {
                return this.snapFrames > 0 && this.snapFrames <= 21;
            }
        }

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
                return MassFac >= 1f + 0.1f * VultureShapedMutationBuffEntry.StackLayer || player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand;
            }
        }

        #region 等级相关
        public bool CanWearMask => VultureShapedMutationBuffEntry.StackLayer >= 2;

        public bool IsKing => VultureShapedMutationBuffEntry.StackLayer >= 3;

        public bool IsMiros => false;//VultureShapedMutationBuffEntry.StackLayer >= 10;
        #endregion

        #region 面具相关
        public VultureMask mask;
        public int wearCount;
        #endregion

        #region 行为相关
        //飞行相关属性
        public VultureCatState State
        {
            get
            {
                return this.state;
            }
            set
            {
                this.state = value;
            }
        }
        VultureCatState state;
        float normalGravity = 0.9f;
        float normalAirFriction = 0.999f;
        float flightAirFriction = 0.7f;

        public bool isFlying;
        float wingSpeed;
        int preventFlight;

        //指定位置
        public Vector2 wantPos;
        bool wantPosIsSetX;
        bool wantPosIsSetY;

        //秃鹫相关
        public KingTusks kingTusks;
        public VultureAI.Behavior behavior;
        private int snapFrames;
        public int landingBrake;
        public Vector2 landingBrakePos;

        public Vector2 moveDirection;
        public float wingFlap;
        public float wingFlapAmplitude;

        public bool hangingInTentacle;

        public IntVector2 hoverPos;
        public bool hoverStill;

        public int laserCounter;

        public int cantFindNewGripCounter;

        public float DefaultWingSpeed
        {
            get
            {
                return 10f + 5f * VultureShapedMutationBuff.Instance.SpeedLevel;
            }
        }

        public void StopFlight()
        {
            isFlying = false;
            if (this.wings != null)
            {
                for (int i = 0; i < this.wings.GetLength(0); i++)
                    for (int j = 0; j < this.wings.GetLength(1); j++)
                        this.wings[i, j].SwitchMode(VultureCatTentacle.Mode.Climb);
            }
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
            isFlying = true;
            wantPos = self.bodyChunks[0].pos;

            if (this.wings != null)
                for (int i = 0; i < this.wings.GetLength(0); i++)
                    for (int j = 0; j < this.wings.GetLength(1); j++)
                        this.wings[i, j].SwitchMode(VultureCatTentacle.Mode.Fly);
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

        public bool PlayerHasBindKey()
        {
            if (BuffPlayerData.Instance.GetKeyBind(VultureShapedMutationBuffEntry.VultureShapedMutation) == KeyCode.None.ToString())
                return false;
            return true;
        }
        #endregion

        #region 图像相关
        //翅膀长度及宽度
        public float wingLength;
        public float wingWidth;
        private float featherLength;
        private float featherWidth;
        public float foldScaleWhenClimb;
        public float[] randomValueForWingColor;

        public float groundRetractionScale;

        public VultureCatTentacle[,] wings;
        private VultureCatFeather[,,] feather;
        private List<VultureGraphics.WingColorWave> colorWaves;
        private int feathersPerWing;

        public HSLColor ColorA;
        public HSLColor ColorB;
        public bool albino;

        public RoomPalette palette;
        public float darkness; 
        #endregion

        #region sprite序号数据
        private int firstSprite;

        private int WingsSpriteStart => firstSprite;

        private int FeatherSpriteStart => WingsSpriteStart + wings.Length;

        private int FeatherSpritesLength => feathersPerWing * 2 * wings.Length;

        private int NeckLumpStart => FeatherSpriteStart + FeatherSpritesLength;

        private int NeckLumpLength => (this.kingTusks == null ? 0 : this.kingTusks.tusks.Length);

        private int TuskSpriteStart => NeckLumpStart + NeckLumpLength;

        private int TuskSpriteLength => kngtskSprCount * (this.kingTusks == null ? 0 : this.kingTusks.tusks.Length);

        private int TuskWireSpriteStart => TuskSpriteStart + TuskSpriteLength;

        private int TuskWireSpriteLength => (this.kingTusks == null ? 0 : this.kingTusks.tusks.Length);

        private int TotalSprites => wings.Length + FeatherSpritesLength + (IsKing ? NeckLumpLength + TuskSpriteLength + TuskWireSpriteLength : 0); 
        
        public int kngtskSprCount => (this.kingTusks == null ? 0 : KingTusks.Tusk.TotalSprites);

        private int WingsSprite(int index, int side)
        {
            return WingsSpriteStart + index * wings.GetLength(1) + side;
        }

        private int FeatherSprite(int index, int side, int i)
        {
            return FeatherSpriteStart + i * 2 + this.feathersPerWing * (index * wings.GetLength(1) + side) * 2;
        }

        private int FeatherColorSprite(int index, int side, int i)
        {
            return FeatherSpriteStart + i * 2 + 1 + this.feathersPerWing * (index * wings.GetLength(1) + side) * 2;

        }

        public int NeckLumpSprite(int s)
        {
            return NeckLumpStart + s;
        }

        public int TuskSprite(int i)
        {
            return TuskSpriteStart + KingTusks.Tusk.TotalSprites * i;
        }

        public int TuskWireSprite(int side)
        {
            return TuskWireSpriteStart + side;
        }

        public bool IsKingTuskSprite(int i)
        {
            bool isTuskWireSprite = false;
            for (int j = 0; j < TuskWireSpriteLength; j++)
                if (i == this.TuskWireSprite(j))
                    isTuskWireSprite = true;
            return this.IsKing &&
                ((i >= TuskSpriteStart && i < TuskSpriteStart + KingTusks.Tusk.TotalSprites) || isTuskWireSprite);
        }

        public int TusksLength => VultureShapedMutationBuff.Instance.ArmedKingVulture ? 4 : 2;
        #endregion

        public void VultureWing(PlayerGraphics self)
        {
            this.wings = new VultureCatTentacle[this.IsMiros ? 2 : 1, 2];
            for (int i = 0; i < this.wings.GetLength(0); i++)
                for (int j = 0; j < this.wings.GetLength(1); j++)
                    this.wings[i, j] = new VultureCatTentacle(self.owner as Player, self.owner.bodyChunks[i == 0 ? 0 : 1], (this.IsKing ? 9f : 7f) * wingLength * 4f, i, j, this);

            randomValueForWingColor = new float[10];
            if (state.randomValueForWingColor == null)
                for (int i = 0; i < randomValueForWingColor.Length; i++)
                    randomValueForWingColor[i] = UnityEngine.Random.value;
            else
                randomValueForWingColor = state.randomValueForWingColor;
            WingColorForLevel();
            if (this.IsMiros)
            {
                this.feathersPerWing = UnityEngine.Random.Range(6, 8);
            }
            else
            {
                this.feathersPerWing = UnityEngine.Random.Range(this.IsKing ? 15 : 13, this.IsKing ? 25 : 20);
            }
            this.colorWaves = new List<VultureGraphics.WingColorWave>();
            float num = (UnityEngine.Random.value < 0.5f) ? 40f : Mathf.Lerp(8f, 15f, UnityEngine.Random.value);
            float num2 = (UnityEngine.Random.value < 0.5f) ? 40f : Mathf.Lerp(8f, 15f, UnityEngine.Random.value);
            float num3 = (UnityEngine.Random.value < 0.5f) ? 20f : Mathf.Lerp(3f, 6f, UnityEngine.Random.value);
            this.feather = new VultureCatFeather[this.wings.GetLength(0), this.wings.GetLength(1), this.feathersPerWing];
            for (int i = 0; i < this.wings.GetLength(0); i++)
            {
                for (int j = 0; j < this.wings.GetLength(1); j++)
                {
                    for (int k = 0; k < this.feathersPerWing; k++)
                    {
                        float num5 = ((float)k + 0.5f) / (float)this.feathersPerWing;
                        float num6 = Mathf.Lerp(1f - Mathf.Pow(this.IsMiros ? 0.95f : 0.89f, (float)k), Mathf.Sqrt(num5), 0.5f);
                        num6 = Mathf.InverseLerp(0.1f, 1.1f, num6);
                        if (this.IsMiros && k == this.feathersPerWing - 1)
                        {
                            num6 = 0.8f;
                        }
                        this.feather[i, j, k] = new VultureCatFeather(self, this.wings[i, j], num6,
                            featherLength * VultureCatTentacle.FeatherContour(num5, 0f) * Mathf.Lerp(this.IsMiros ? 12f : 10f, this.IsMiros ? 16f : 15f, UnityEngine.Random.value),
                            featherLength * VultureCatTentacle.FeatherContour(num5, 1f) * Mathf.Lerp(this.IsMiros ? 16f : 13f, this.IsMiros ? 20f : 15f, UnityEngine.Random.value) * (this.IsKing ? 1.3f : 1f),
                            featherWidth * Mathf.Lerp(this.IsMiros ? 5f : 3f, this.IsMiros ? 8f : 6f, VultureCatTentacle.FeatherWidth(num5)), this);
                        /*
                        bool flag = UnityEngine.Random.value < 0.025f;
                        if (UnityEngine.Random.value < 1f / num || (flag && UnityEngine.Random.value < 0.5f))
                        {
                            this.feather[i, j, k].lose = 1f - UnityEngine.Random.value * UnityEngine.Random.value * UnityEngine.Random.value;
                            if (UnityEngine.Random.value < 0.4f)
                            {
                                this.feather[i, j, k].brokenColor = 1f - UnityEngine.Random.value * UnityEngine.Random.value;
                            }
                        }
                        if (UnityEngine.Random.value < 1f / num2)
                        {
                            this.feather[i, j, k].extendedLength /= 5f;
                            this.feather[i, j, k].contractedLength = this.feather[i, j, k].extendedLength;
                            this.feather[i, j, k].brokenColor = 1f;
                            this.feather[i, j, k].width /= 1.7f;
                        }
                        if (UnityEngine.Random.value < 0.025f || (flag && UnityEngine.Random.value < 0.5f))
                        {
                            this.feather[i, j, k].contractedLength = this.feather[i, j, k].extendedLength * 0.7f;
                        }
                        if (UnityEngine.Random.value < 1f / num3 || (flag && UnityEngine.Random.value < 0.5f))
                        {
                            this.feather[i, j, k].brokenColor = ((UnityEngine.Random.value < 0.5f) ? 1f : UnityEngine.Random.value);
                        }*/
                    }
                }
            }

            //白化
            if (ModManager.MSC)
            {
                this.albino = (UnityEngine.Random.value < 0.001f);
            }
            if (this.IsMiros)
            {
                this.albino = false;
            }
            this.albino = false;

            this.Reset(self);
        }

        public void KingTusk(PlayerGraphics self)
        {
            if (this.IsKing && (this.kingTusks == null || this.kingTusks.tusks.Length != TusksLength))
            {
                this.kingTusks = new KingTusks(self.owner as Player, this);
            }
            if (!this.IsKing && this.kingTusks != null)
            {
                this.kingTusks = null;
            }
        }

        public VultureCat(Player player)
        {
            ownerRef = new WeakReference<Player>(player);
            state = new VultureCatState(player.abstractCreature, this);
            wingSpeed = DefaultWingSpeed;
            wingFlapAmplitude = 1f;
            foldScaleWhenClimb = 0.5f;
            groundRetractionScale = 0.9f;
            origThrowingSkill = player.slugcatStats.throwingSkill;
            mask = null; 
            if (player.playerState.isPup)
            {
                wingLength = 3f;
                wingWidth = 0.3f;
                featherLength = 1.2f;
                featherWidth = 0.3f;
            }
            else
            {
                wingLength = 5f;
                wingWidth = 0.5f;
                featherLength = 1.8f;
                featherWidth = 0.5f;
            }

            if (this.IsKing)
            {
                this.kingTusks = new KingTusks(player, this);
            }
        }

        #region 外观
        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;

            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            KingTusk(self);

            firstSprite = sLeaser.sprites.Length;
            Array.Resize(ref sLeaser.sprites, firstSprite + TotalSprites);

            if (this.kingTusks != null)
            {
                this.kingTusks.InitiateSprites(self, sLeaser, rCam);
            }
            for (int i = 0; i < this.wings.GetLength(0); i++)
                for (int j = 0; j < this.wings.GetLength(1); j++)
                    sLeaser.sprites[this.WingsSprite(i, j)] = TriangleMesh.MakeLongMesh(this.wings[i, j].tChunks.Length, false, false);
            for (int i = 0; i < this.wings.GetLength(0); i++)
            {
                for (int j = 0; j < this.wings.GetLength(1); j++)
                {
                    for (int l = 0; l < this.feathersPerWing; l++)
                    {
                        sLeaser.sprites[this.IsMiros ? this.FeatherColorSprite(i, j, l) : this.FeatherSprite(i, j, l)] = new FSprite(this.IsMiros ? "MirosWingColor" : "KrakenFeather", true);
                        sLeaser.sprites[this.IsMiros ? this.FeatherColorSprite(i, j, l) : this.FeatherSprite(i, j, l)].anchorY = (this.IsMiros ? 0.94f : 0.97f);
                        if (this.IsMiros && l == this.feathersPerWing - 1)
                        {
                            sLeaser.sprites[this.FeatherSprite(i, j, l)] = new FSprite("MirosClaw", true);
                            sLeaser.sprites[this.FeatherSprite(i, j, l)].anchorY = 0.3f;
                            sLeaser.sprites[this.FeatherSprite(i, j, l)].anchorX = 0f;
                        }
                        else
                        {
                            sLeaser.sprites[this.IsMiros ? this.FeatherSprite(i, j, l) : this.FeatherColorSprite(i, j, l)] = new FSprite(this.IsMiros ? "MirosWingSolid" : "KrakenFeatherColor", true);
                            sLeaser.sprites[this.IsMiros ? this.FeatherSprite(i, j, l) : this.FeatherColorSprite(i, j, l)].anchorY = (this.IsMiros ? 0.94f : 0.97f);
                        }
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

            this.WingColorForSpecificSlugcat(sLeaser);
            this.darkness = rCam.room.Darkness(Vector2.Lerp(player.mainBodyChunk.lastPos, player.mainBodyChunk.pos, timeStacker));
            this.darkness *= 1f - 0.5f * rCam.room.LightSourceExposure(Vector2.Lerp(player.mainBodyChunk.lastPos, player.mainBodyChunk.pos, timeStacker));
            if (this.kingTusks != null)
            {
                this.kingTusks.DrawSprites(self, sLeaser, rCam, timeStacker, camPos);
            }
            Vector2 vector = Vector2.Lerp(player.bodyChunks[1].lastPos, player.bodyChunks[1].pos, timeStacker);
            Vector2 vector2 = Vector2.Lerp(player.bodyChunks[0].lastPos, player.bodyChunks[0].pos, timeStacker);
            Vector2 vector5 = Vector2.Lerp(player.bodyChunks[0].lastPos, player.bodyChunks[0].pos, this.Snapping ? Mathf.Lerp(-1.5f, 1.5f, UnityEngine.Random.value) : timeStacker);//Vector2.Lerp(this.vulture.bodyChunks[4].lastPos, this.vulture.bodyChunks[4].pos, this.Snapping ? Mathf.Lerp(-1.5f, 1.5f, UnityEngine.Random.value) : timeStacker);
            if (this.ChargingSnap)
            {
                vector5 += Custom.DegToVec(UnityEngine.Random.value * 360f) * UnityEngine.Random.value * 4f;
            }
            Vector2 vector6 = vector2;//Vector2.Lerp(this.vulture.neck.connectedChunk.lastPos, this.vulture.neck.connectedChunk.pos, timeStacker);
            for (int k = 0; k < this.wings.GetLength(0); k++)
            {
                for (int l = 0; l < this.wings.GetLength(1); l++)
                {
                    /*
                    if (this.IsKing)
                    {
                        vector6 = Vector2.Lerp(this.vulture.bodyChunks[4].lastPos, this.vulture.bodyChunks[4].pos, timeStacker);
                        num4 = 0f;
                        for (int l = 0; l < this.neckTubes.GetLength(1); l++)
                        {
                            Vector2 vector8 = Vector2.Lerp(this.neckTubes[k, l, l, 1], this.neckTubes[k, l, l, 0], timeStacker);
                            if (l == this.neckTubes.GetLength(1) - 1)
                            {
                                vector8 = Vector2.Lerp(this.vulture.bodyChunks[0].lastPos, this.vulture.bodyChunks[0].pos, timeStacker);
                            }
                            Vector2 wireDir = (vector8 - vector6).normalized;
                            Vector2 a2 = Custom.PerpendicularVector(wireDir);
                            float d2 = Vector2.Distance(vector8, vector6) / 5f;
                            float num5 = (l % 3 == 0) ? 2.5f : 1.5f;
                            (sLeaser.sprites[this.TubeSprite(k, l)] as TriangleMesh).MoveVertice(l * 4, vector6 - a2 * (num5 + num4) * 0.5f + wireDir * d2 - camPos);
                            (sLeaser.sprites[this.TubeSprite(k, l)] as TriangleMesh).MoveVertice(l * 4 + 1, vector6 + a2 * (num5 + num4) * 0.5f + wireDir * d2 - camPos);
                            (sLeaser.sprites[this.TubeSprite(k, l)] as TriangleMesh).MoveVertice(l * 4 + 2, vector8 - a2 * num5 - wireDir * d2 - camPos);
                            (sLeaser.sprites[this.TubeSprite(k, l)] as TriangleMesh).MoveVertice(l * 4 + 3, vector8 + a2 * num5 - wireDir * d2 - camPos);
                            num4 = num5;
                            vector6 = vector8;
                        }
                    }*/
                    vector6 = Vector2.Lerp(this.wings[k, l].connectedChunk.lastPos, this.wings[k, l].connectedChunk.pos, timeStacker);
                    for (int m = 0; m < this.wings[k, l].tChunks.Length; m++)
                    {
                        Vector2 vector9 = Vector2.Lerp(this.wings[k, l].tChunks[m].lastPos, this.wings[k, l].tChunks[m].pos, timeStacker);
                        Vector2 normalized3 = (vector9 - vector6).normalized;
                        Vector2 a3 = Custom.PerpendicularVector(normalized3);
                        float d3 = Vector2.Distance(vector9, vector6) / 5f;
                        float num6 = this.wings[k, l].TentacleContour(((float)m + 0.5f) / (float)this.wings[k, l].tChunks.Length);
                        num6 *= Mathf.Clamp(Mathf.Pow(this.wings[k, l].tChunks[m].stretchedFac, 0.35f), 0.5f, 1.5f);
                        (sLeaser.sprites[this.WingsSprite(k, l)] as TriangleMesh).MoveVertice(m * 4, vector6 - a3 * num6 + normalized3 * d3 - camPos);
                        (sLeaser.sprites[this.WingsSprite(k, l)] as TriangleMesh).MoveVertice(m * 4 + 1, vector6 + a3 * num6 + normalized3 * d3 - camPos);
                        num6 = this.wings[k, l].TentacleContour(((float)m + 1f) / (float)this.wings[k, l].tChunks.Length);
                        num6 *= Mathf.Clamp(Mathf.Pow(this.wings[k, l].tChunks[m].stretchedFac, 0.35f), 0.5f, 1.5f);
                        (sLeaser.sprites[this.WingsSprite(k, l)] as TriangleMesh).MoveVertice(m * 4 + 2, vector9 - a3 * num6 - normalized3 * d3 - camPos);
                        (sLeaser.sprites[this.WingsSprite(k, l)] as TriangleMesh).MoveVertice(m * 4 + 3, vector9 + a3 * num6 - normalized3 * d3 - camPos);
                        vector6 = vector9;
                    }
                    for (int n = 0; n < this.feathersPerWing; n++)
                    {
                        sLeaser.sprites[this.FeatherSprite(k, l, n)].x = Mathf.Lerp(this.feather[k, l, n].ConnectedLastPos.x, this.feather[k, l, n].ConnectedPos.x, timeStacker) - camPos.x;
                        sLeaser.sprites[this.FeatherSprite(k, l, n)].y = Mathf.Lerp(this.feather[k, l, n].ConnectedLastPos.y, this.feather[k, l, n].ConnectedPos.y, timeStacker) - camPos.y;
                        sLeaser.sprites[this.FeatherSprite(k, l, n)].rotation = Custom.AimFromOneVectorToAnother(Vector2.Lerp(this.feather[k, l, n].lastPos, this.feather[k, l, n].pos, timeStacker), Vector2.Lerp(this.feather[k, l, n].ConnectedLastPos, this.feather[k, l, n].ConnectedPos, timeStacker));
                        if (!this.IsMiros || n != this.feathersPerWing - 1)
                        {
                            sLeaser.sprites[this.FeatherSprite(k, l, n)].scaleX = Mathf.Lerp(3f, this.feather[k, l, n].width, (this.feather[k, l, n].extendedFac + this.wings[k, l].flyingMode) * 0.5f) / 9f * ((l == 0) ? 1f : -1f) * (this.IsKing ? 1.3f : 1f);
                            sLeaser.sprites[this.FeatherSprite(k, l, n)].scaleY = Vector2.Distance(Vector2.Lerp(this.feather[k, l, n].ConnectedLastPos, this.feather[k, l, n].ConnectedPos, timeStacker), Vector2.Lerp(this.feather[k, l, n].lastPos, this.feather[k, l, n].pos, timeStacker)) / 107f;
                        }
                        else if (this.IsMiros)
                        {
                            sLeaser.sprites[this.FeatherSprite(k, l, n)].scaleX = (float)((l == 0) ? 1 : -1) * Mathf.Pow(this.feather[k, l, n].extendedFac, 3f);
                            sLeaser.sprites[this.FeatherSprite(k, l, n)].scaleY = Mathf.Pow(this.feather[k, l, n].extendedFac, 3f);
                            sLeaser.sprites[this.FeatherSprite(k, l, n)].rotation += (float)(200 * ((l == 0) ? 1 : -1));
                        }
                        if (!this.IsMiros || n != this.feathersPerWing - 1)
                        {
                            sLeaser.sprites[this.FeatherColorSprite(k, l, n)].x = Mathf.Lerp(this.feather[k, l, n].ConnectedLastPos.x, this.feather[k, l, n].ConnectedPos.x, timeStacker) - camPos.x;
                            sLeaser.sprites[this.FeatherColorSprite(k, l, n)].y = Mathf.Lerp(this.feather[k, l, n].ConnectedLastPos.y, this.feather[k, l, n].ConnectedPos.y, timeStacker) - camPos.y;
                            sLeaser.sprites[this.FeatherColorSprite(k, l, n)].scaleY = Vector2.Distance(Vector2.Lerp(this.feather[k, l, n].ConnectedLastPos, this.feather[k, l, n].ConnectedPos, timeStacker), Vector2.Lerp(this.feather[k, l, n].lastPos, this.feather[k, l, n].pos, timeStacker)) / 107f;
                            sLeaser.sprites[this.FeatherColorSprite(k, l, n)].rotation = Custom.AimFromOneVectorToAnother(Vector2.Lerp(this.feather[k, l, n].lastPos, this.feather[k, l, n].pos, timeStacker), Vector2.Lerp(this.feather[k, l, n].ConnectedLastPos, this.feather[k, l, n].ConnectedPos, timeStacker));
                            sLeaser.sprites[this.FeatherColorSprite(k, l, n)].scaleX = Mathf.Lerp(3f, this.feather[k, l, n].width, (this.feather[k, l, n].extendedFac + this.wings[k, l].flyingMode) * 0.5f) / 9f * ((l == 0) ? 1f : -1f) * (this.IsKing ? 1.3f : 1f);
                        }
                        else if (this.IsMiros)
                        {
                            sLeaser.sprites[this.FeatherColorSprite(k, l, n)].isVisible = false;
                        }
                        if (true)//!this.shadowMode
                        {
                            sLeaser.sprites[this.FeatherColorSprite(k, l, n)].color = this.feather[k, l, n].CurrentColor(); // Color.Lerp(this.feather[k, l, n].CurrentColor(), this.palette.blackColor, (ModManager.MMF && !this.IsMiros) ? this.darkness : 0f);
                        }
                    }
                }
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            this.palette = palette;
            if (this.kingTusks != null)
            {
                Color color3 = Color.Lerp(this.ColorA.rgb, new Color(1f, 1f, 1f), 0.35f);
                this.kingTusks.ApplyPalette(self, this.palette, color3, sLeaser, rCam);
            }
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            if (firstSprite >= 1 && sLeaser.sprites.Length >= firstSprite + TotalSprites)
            {
                var foregroundContainer = rCam.ReturnFContainer("Foreground");
                var midgroundContainer = rCam.ReturnFContainer("Midground");
                if (newContatiner == null)
                {
                    newContatiner = rCam.ReturnFContainer("Midground");
                }
                if (this.IsMiros)
                {
                    for (int i = 0; i < TotalSprites; i++)
                    {/*
                        if (i == this.EyeTrailSprite())
                        {
                            rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[i]);
                        }
                        else if (i >= this.FirstBeakSprite() && i <= this.LastBeakSprite())
                        {
                            rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[i]);
                        }
                        else
                        {*/
                        newContatiner.AddChild(sLeaser.sprites[firstSprite + i]);
                        sLeaser.sprites[firstSprite + i].MoveBehindOtherNode(sLeaser.sprites[0]);
                        //}
                    }
                    if (sLeaser.containers != null)
                    {
                        foreach (FContainer node in sLeaser.containers)
                        {
                            newContatiner.AddChild(node);
                        }
                        return;
                    }
                }
                else
                {
                    for (int k = 0; k < TotalSprites; k++)
                    {
                        sLeaser.sprites[firstSprite + k].RemoveFromContainer();
                        if (this.IsKingTuskSprite(firstSprite + k))
                        {
                            this.kingTusks.AddToContainer(self, firstSprite + k, sLeaser, rCam, newContatiner);

                            if (firstSprite + k >= TuskSpriteStart && 
                                firstSprite + k < TuskSpriteStart + TuskWireSpriteLength)
                                sLeaser.sprites[firstSprite + k].MoveBehindOtherNode(sLeaser.sprites[3]);
                        }
                        else
                        {
                            newContatiner.AddChild(sLeaser.sprites[firstSprite + k]);
                            sLeaser.sprites[firstSprite + k].MoveBehindOtherNode(sLeaser.sprites[0]);
                        }
                    }
                }
            }
        }

        public void GraphicsUpdate()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            /*
            if (this.IsMiros)
            {
                this.lastHeadFlip = this.headFlip;
                if (Custom.DistanceToLine(this.vulture.Head().pos, this.vulture.bodyChunks[1].pos, this.vulture.bodyChunks[0].pos) < 0f)
                {
                    this.headFlip = Mathf.Min(1f, this.headFlip + 0.16666667f);
                }
                else
                {
                    this.headFlip = Mathf.Max(-1f, this.headFlip - 0.16666667f);
                }
                if (this.soundLoop == null && this.laserActive > 0f)
                {
                    this.soundLoop = new ChunkDynamicSoundLoop(this.vulture.bodyChunks[4]);
                    this.soundLoop.sound = SoundID.Vulture_Grub_Laser_LOOP;
                }
                else if (this.soundLoop != null)
                {
                    this.soundLoop.Volume = Mathf.InverseLerp(0.3f, 1f, this.laserActive);
                    this.soundLoop.Pitch = 0.2f + 0.8f * Mathf.Pow(this.laserActive, 0.6f);
                    this.soundLoop.Update();
                    if (this.laserActive == 0f)
                    {
                        if (this.soundLoop.emitter != null)
                        {
                            this.soundLoop.emitter.slatedForDeletetion = true;
                        }
                        this.soundLoop = null;
                    }
                }
                this.lastLaserActive = this.laserActive;
                this.laserActive = Custom.LerpAndTick(this.laserActive, (!this.vulture.isLaserActive()) ? 0f : 1f, 0.05f, 0.05f);
                this.lastLaserColor = this.laserColor;
                this.lastFlash = this.flash;
                this.flash = Custom.LerpAndTick(this.flash, 0f, 0.02f, 0.025f);
            }
            if (this.DEBUGLABELS != null)
            {
                this.DEBUGLABELS[0].label.text = string.Concat(new string[]
                {
                    this.vulture.abstractCreature.pos.x.ToString(),
                    " ",
                    this.vulture.abstractCreature.pos.y.ToString(),
                    "   ",
                    this.vulture.AI.behavior.ToString()
                });
                this.DEBUGLABELS[0].label.x = 10f;
                this.DEBUGLABELS[0].label.y = 10f;
            }*/
            for (int i = 0; i < this.wings.GetLength(0); i++)
            {
                for (int j = 0; j < this.wings.GetLength(1); j++)
                {
                    for (int k = 0; k < this.feathersPerWing; k++)
                    {
                        this.feather[i, j, k].Update();
                    }
                }
            }/*
            for (int k = 0; k < this.appendages.Length; k++)
            {
                for (int l = 0; l < this.appendages[k].Length; l++)
                {
                    this.appendages[k][l].Update();
                }
            }
            if (this.IsMiros)
            {
                for (int m = 0; m < 2; m++)
                {
                    this.beak[m].Update();
                }
            }*/
            if (UnityEngine.Random.value < 0.005f)
            {
                this.MakeColorWave(0);
            }
            for (int n = this.colorWaves.Count - 1; n >= 0; n--)
            {
                if (this.colorWaves[n].lastPosition > 1f)
                {
                    this.colorWaves.RemoveAt(n);
                }
                else if (this.colorWaves[n].delay > 0)
                {
                    this.colorWaves[n].delay--;
                }
                else
                {
                    this.colorWaves[n].lastPosition = this.colorWaves[n].position;
                    this.colorWaves[n].position += this.colorWaves[n].speed / (1f + this.colorWaves[n].position);
                    for (int i = 0; i < this.wings.GetLength(0); i++)
                    {
                        for (int j = 0; j < this.wings.GetLength(1); j++)
                        {
                            for (int k = 0; k < this.feathersPerWing; k++)
                            {
                                if (this.colorWaves[n].lastPosition < this.feather[i, j, k].wingPosition && this.colorWaves[n].position >= this.feather[i, j, k].wingPosition)
                                {
                                    this.feather[i, j, k].saturationBonus = Mathf.Max(this.feather[i, j, k].saturationBonus, this.colorWaves[n].saturation);
                                    this.feather[i, j, k].lightnessBonus = Mathf.Max(this.feather[i, j, k].lightnessBonus, this.colorWaves[n].lightness);
                                    this.feather[i, j, k].forcedAlpha = Mathf.Max(this.feather[i, j, k].forcedAlpha, this.colorWaves[n].forceAlpha);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Reset(PlayerGraphics self)
        {
            //防止拉丝
            for (int i = 0; i < this.wings.GetLength(0); i++)
                for (int j = 0; j < this.wings.GetLength(1); j++)
                    wings[i, j].Reset((self as GraphicsModule).owner.bodyChunks[0].pos);
            for (int i = 0; i < this.wings.GetLength(0); i++)
                for (int j = 0; j < this.wings.GetLength(1); j++)
                    for (int k = 0; k < this.feathersPerWing; k++)
                        this.feather[i, j, k].Reset((self as GraphicsModule).owner.bodyChunks[0].pos);
        }

        public void MakeColorWave(int delay)
        {
            this.colorWaves.Add(new VultureGraphics.WingColorWave(delay, 1f / Mathf.Lerp(10f, 30f, UnityEngine.Random.value), 1f - UnityEngine.Random.value * UnityEngine.Random.value * UnityEngine.Random.value, 1f - UnityEngine.Random.value * UnityEngine.Random.value * UnityEngine.Random.value, 1f - UnityEngine.Random.value * UnityEngine.Random.value * UnityEngine.Random.value));
        }

        public void WingColorForSpecificSlugcat(RoomCamera.SpriteLeaser sLeaser)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;

            if ((self.owner as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Rivulet)
            {
                Vector3 vector = Custom.RGB2HSL(self.gills.effectColor);
                this.ColorA = new HSLColor(Mathf.Clamp(vector.x, 0f, 0.99f), vector.y, Mathf.Clamp(vector.z, 0.01f, 1f));
                this.ColorB = new HSLColor(this.ColorA.hue + Mathf.Lerp(-0.1f, 0.1f, randomValueForWingColor[5]), 
                                           Mathf.Lerp(0.8f, 1f, 1f - randomValueForWingColor[6] * randomValueForWingColor[7]), 
                                           Mathf.Lerp(0.45f, 1f, randomValueForWingColor[8] * randomValueForWingColor[9]));
            }
            else if ((self.owner as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)
            {
                Vector3 vector = Custom.RGB2HSL(sLeaser.sprites[self.tailSpecks.startSprite + self.tailSpecks.rows * self.tailSpecks.lines - 1].color);
                this.ColorA = new HSLColor(Mathf.Clamp(vector.x, 0f, 0.99f), vector.y, Mathf.Clamp(vector.z, 0.01f, 1f));
                this.ColorB = new HSLColor(this.ColorA.hue + Mathf.Lerp(-0.05f, 0.05f, randomValueForWingColor[5]), 
                                           Mathf.Lerp(0.8f, 1f, 1f - randomValueForWingColor[6] * randomValueForWingColor[7]),
                                           Mathf.Lerp(ColorA.lightness, Mathf.Lerp(0.45f, 1f, randomValueForWingColor[8] * randomValueForWingColor[9]), 0.1f));
            }
            else if ((self.owner as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
            {
                FSprite sp = null; 
                Vector3 vector = Custom.RGB2HSL(sLeaser.sprites[12].color);
                foreach (var sprite in sLeaser.sprites)
                    if (sprite.element.name == "MushroomA")
                        sp = sprite;
                if (sp != null)
                {
                    vector = Custom.RGB2HSL(sp.color);
                }
                this.ColorA = new HSLColor(Mathf.Clamp(vector.x, 0f, 0.99f), vector.y, Mathf.Clamp(vector.z, 0.01f, 1f));
                this.ColorB = new HSLColor(this.ColorA.hue + Mathf.Lerp(-0.05f, 0.05f, randomValueForWingColor[5]),
                                           Mathf.Lerp(0.8f, 1f, 1f - randomValueForWingColor[6] * randomValueForWingColor[7]),
                                           Mathf.Lerp(ColorA.lightness, Mathf.Lerp(0.45f, 1f, randomValueForWingColor[8] * randomValueForWingColor[9]), 0.1f));
            }
            else if ((self.owner as Player).SlugCatClass.value == "Outsider")
            {
                FSprite sp = null;
                Vector3 vector = new Vector3(161f / 360f, 49f / 100f, 90f / 100f);
                foreach (var sprite in sLeaser.sprites)
                    if (sprite.element.name == "MothWingA1")
                        sp = sprite;
                if (sp != null)
                    vector = Custom.RGB2HSL(sp.color);
                this.ColorA = new HSLColor(Mathf.Clamp(vector.x, 0f, 0.99f), vector.y, Mathf.Clamp(vector.z, 0.01f, 1f));
                this.ColorB = new HSLColor(this.ColorA.hue + Mathf.Lerp(-0.05f, 0.05f, randomValueForWingColor[5]),
                                           Mathf.Lerp(0.8f, 1f, 1f - randomValueForWingColor[6] * randomValueForWingColor[7]),
                                           Mathf.Lerp(ColorA.lightness, Mathf.Lerp(0.45f, 1f, randomValueForWingColor[8] * randomValueForWingColor[9]), 0.3f));
            }
            else if (sLeaser.sprites[0].color == Color.white)
                this.albino = true;
        }

        public void WingColorForLevel()
        {
            if (this.IsMiros)
            {
                randomValueForWingColor[0] = Custom.WrappedRandomVariation(0.0025f, 0.02f, 0.6f);
                randomValueForWingColor[3] = Custom.ClampedRandomVariation(0.5f, 0.15f, 0.1f);
                this.ColorA = new HSLColor(randomValueForWingColor[0], 1f, randomValueForWingColor[3]);
                this.ColorB = new HSLColor(this.ColorA.hue + Mathf.Lerp(-0.25f, 0.25f, randomValueForWingColor[5]), Mathf.Lerp(0.8f, 1f, 1f - randomValueForWingColor[6] * randomValueForWingColor[7]), Mathf.Lerp(0.45f, 1f, randomValueForWingColor[8] * randomValueForWingColor[9]));
            }
            else if (this.IsKing)
            {
                this.ColorB = new HSLColor(Mathf.Lerp(0.93f, 1.07f, randomValueForWingColor[0]), Mathf.Lerp(0.8f, 1f, 1f - randomValueForWingColor[1] * randomValueForWingColor[2]), Mathf.Lerp(0.45f, 1f, randomValueForWingColor[3] * randomValueForWingColor[4]));
                this.ColorA = new HSLColor(this.ColorB.hue + Mathf.Lerp(-0.25f, 0.25f, randomValueForWingColor[5]), Mathf.Lerp(0.5f, 0.7f, randomValueForWingColor[6]), Mathf.Lerp(0.7f, 0.8f, randomValueForWingColor[8]));
            }
            else
            {
                this.ColorA = new HSLColor(Mathf.Lerp(0.9f, 1.6f, randomValueForWingColor[0]), Mathf.Lerp(0.5f, 0.7f, randomValueForWingColor[1]), Mathf.Lerp(0.7f, 0.8f, randomValueForWingColor[3]));
                this.ColorB = new HSLColor(this.ColorA.hue + Mathf.Lerp(-0.25f, 0.25f, randomValueForWingColor[5]), Mathf.Lerp(0.8f, 1f, 1f - randomValueForWingColor[6] * randomValueForWingColor[7]), Mathf.Lerp(0.45f, 1f, randomValueForWingColor[8] * randomValueForWingColor[9]));
            }
        }
        #endregion

        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (!player.Consious) 
                StopFlight();

            player.slugcatStats.throwingSkill = Mathf.Max(-1, origThrowingSkill - VultureShapedMutationBuffEntry.StackLayer + 1);

            WearMaskUpdate();
            if (player.grasps != null)
            {
                if (player.grasps[1] != null && !(CanWearMask && (player.grasps[1].grabbed is VultureMask) && this.mask != null))
                {
                    if (player.grasps[0] != null)
                        player.ReleaseGrasp(1);
                    else
                        player.SwitchGrasps(1, 0);
                }

                if ((player.grasps[0] != null && player.grasps[0].grabbed is VultureMask) ||
                    (player.grasps[1] != null && player.grasps[1].grabbed is VultureMask) ||
                    this.mask != null)
                {
                    this.State.mask = true;
                }
                else
                {
                    this.State.mask = false;
                }
            }

            this.hangingInTentacle = false;
            if (player.room != null && this.wings != null)
                for (int i = 0; i < this.wings.GetLength(0); i++)
                    for (int j = 0; j < this.wings.GetLength(1); j++)
                        this.wings[i, j].Update();
            if (this.hangingInTentacle)
            {
                this.cantFindNewGripCounter += 2;
                if (this.cantFindNewGripCounter > (this.IsMiros ? 200 : 400))
                {
                    for (int i = 0; i < this.wings.GetLength(0); i++)
                    {
                        for (int j = 0; j < this.wings.GetLength(1); j++)
                        {
                            if (this.wings[i, j].hasAnyGrip)
                            {
                                this.wings[i, j].ReleaseGrip();
                            }
                        }
                    }
                }
            }
            else if (this.cantFindNewGripCounter > 0)
            {
                this.cantFindNewGripCounter--;
            }

            if (player.animation == Player.AnimationIndex.HangFromBeam || player.animation == Player.AnimationIndex.SurfaceSwim)
            {
                preventFlight = 15;
            }
            else if (player.bodyMode == Player.BodyModeIndex.WallClimb)
            {
                preventFlight = 10;//下次试试 8，或者更少
            }
            else if (preventFlight > 0)
            {
                preventFlight--;
            }

            moveDirection = new Vector2(player.input[0].x, player.input[0].y).normalized;

            if (player.wantToJump > 0)
            {
                if (isFlying)
                {
                    StopFlight();
                    preventFlight = 10;
                }
                else if (CanSustainFlight(player))
                    InitiateFlight(player);
            }
            //如果正在飞行
            if (isFlying)
            {
                player.AerobicIncrease(0.04f * Mathf.Pow(MassFac, 2f));
                if (this.wings != null)
                {
                    if (MassFacCondition)
                    {
                        if (player.aerobicLevel >= 0.5f)
                            this.wingFlapAmplitude = Mathf.Clamp(this.wingFlapAmplitude - 0.05f, 1f - player.aerobicLevel, 1f);
                        if (player.aerobicLevel >= 0.95f)
                            player.gourmandExhausted = true;
                    }
                    if (player.gourmandExhausted)
                    {
                        for (int i = 0; i < this.wings.GetLength(0); i++)
                            for (int j = 0; j < this.wings.GetLength(1); j++)
                                this.wings[i, j].SwitchMode(VultureCatTentacle.Mode.Climb);
                    }
                }

                player.gravity = 0f;
                player.airFriction = flightAirFriction;

                //飞行速度
                FlightUpdate(player);

                if (CheckTentacleModeAnd(VultureCatTentacle.Mode.Climb))
                    StopFlight();
            }
            else
            {
                player.airFriction = normalAirFriction;
                player.gravity = normalGravity;

                wantPos = player.bodyChunks[0].pos + wingSpeed * new Vector2(player.input[0].x, player.input[0].y);
            }
            if (this.kingTusks != null)
            {
                this.kingTusks.Update();
            }
        }

        //调整姿势
        public void MovementUpdate(On.Player.orig_MovementUpdate orig, bool eu)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (!player.Consious) return;

            if (isFlying)
            {
                player.bodyMode = Player.BodyModeIndex.Default;
                player.animation = Player.AnimationIndex.None;

                orig(player, eu);

                if (!CanSustainFlight(player))
                {
                    StopFlight();
                }
                else
                {
                    if (player.input[0].x != 0)
                    {
                        player.bodyMode = Player.BodyModeIndex.Default;
                        player.animation = Player.AnimationIndex.LedgeCrawl;
                    }
                    else
                    {
                        player.bodyMode = Player.BodyModeIndex.Default;
                        player.animation = Player.AnimationIndex.None;
                    }
                }
            }
        }

        public void NewRoom(Room room)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (this.wings != null)
            {
                for (int i = 0; i < this.wings.GetLength(0); i++)
                    for (int j = 0; j < this.wings.GetLength(1); j++)
                        this.wings[i, j].NewRoom(room);
            }
            if (this.kingTusks != null)
            {
                this.kingTusks.NewRoom(room);
            }
        }

        //飞行速度
        private void FlightUpdate(Player player)
        {
            if (player.room == null)
                return;
            float massSpeedFac = 1f;
            if (player.aerobicLevel >= 0.5f && MassFacCondition)
            {
                massSpeedFac = Custom.LerpMap(player.aerobicLevel, 0.5f, 1f, 1f, 0f);
                wingSpeed = Custom.LerpMap(player.aerobicLevel, 0.5f, 1f, DefaultWingSpeed, 0f);
                wantPos += Custom.LerpMap(player.aerobicLevel, 0.5f, 1f, 0f, 4f) * Vector2.down;
            }
            else
            {
                wingSpeed = DefaultWingSpeed;
            }

            wantPos += wingSpeed * new Vector2(player.input[0].x, player.input[0].y);//player.bodyChunks[0].pos + 
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

            player.bodyChunks[0].vel *= Custom.LerpMap(player.bodyChunks[0].vel.magnitude, 1f, 6f, 0.99f, 0.9f);
            player.bodyChunks[0].vel += Vector2.ClampMagnitude(wantPos - player.bodyChunks[0].pos, 100f) / 100f * 3f;
            //俯身加速
            if (player.input[0].x != 0 && player.input[0].y < 0)
                player.bodyChunks[0].vel.x *= 1.2f;
            //抵消重力
            if (player.input[0].y >= 0)
            {
                player.bodyChunks[0].vel += 1.85f * Vector2.up * player.room.gravity * massSpeedFac;
            }
            else
                player.bodyChunks[0].vel -= 1.05f * Vector2.up * massSpeedFac;
            //随机速度
            player.bodyChunks[0].vel += Custom.RNV() * Random.value * 0.5f;

            player.bodyChunks[1].vel *= 0.8f;


            if (this.wingFlap < 0.5f)
                this.wingFlap += 0.033333335f;
            else
                this.wingFlap += 0.02f;
            if (this.wingFlap > 1f)
                this.wingFlap -= 1f;
            if (player.input[0].y < 0)
                this.wingFlap = 0f;

            if (this.CheckTentacleModeOr(VultureCatTentacle.Mode.Fly))
            {
                this.wingFlapAmplitude = Mathf.Clamp(this.wingFlapAmplitude + 0.033333335f, 0f, 1f);
            }
            else if (this.CheckTentacleModeAnd(VultureCatTentacle.Mode.Climb))//!= VultureCatTentacle.Mode.Fly
            {
                this.wingFlapAmplitude = 0f;
            }
            else
            {
                this.wingFlapAmplitude = Mathf.Clamp(this.wingFlapAmplitude + 0.0125f, 0f, 0.5f);
            }
            float num2 = 0f;
            if (this.wings != null)
                for (int i = 0; i < this.wings.GetLength(0); i++)
                    for (int j = 0; j < this.wings.GetLength(1); j++)
                        num2 += this.wings[i, j].Support() * (this.IsMiros ? 0.75f : 0.5f);
            num2 = Mathf.Pow(num2, 0.5f);
            num2 = Mathf.Max(num2, 0.1f);
            this.hoverStill = false;
        }

        public void Jump()
        {
            if (this.wings != null)
            {
                for (int i = 0; i < this.wings.GetLength(0); i++)
                {
                    for (int j = 0; j < this.wings.GetLength(1); j++)
                    {
                        if (this.wings[i, j].hasAnyGrip)
                        {
                            this.wings[i, j].ReleaseGrip();
                        }
                    }
                }
            }
        }

        #region 面具相关
        public void WearMaskUpdate()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (!this.CanWearMask)
            {
                if (this.mask != null)
                    this.ReleaseGraspMask();
                return;
            }
            if (this.mask == null && player.input[0].pckp && player.input[0].y > 0)
                wearCount++;
            else
                wearCount = 0;

            if (wearCount >= 40)
            {
                //戴上面具
                if (this.mask == null && FindMaskInHands(player) != -1)
                {
                    wearCount = 0;
                    int hand = FindMaskInHands(player);
                    this.mask = player.grasps[hand].grabbed as VultureMask;
                    player.SwitchGrasps(0, 1);
                    player.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, player.mainBodyChunk);
                    BuffPlugin.Log("Player wear mask on face!");
                }
            }
            if (this.mask != null)
            {
                if (player.grasps != null && player.grasps[1] == null)
                {
                    if (player.grasps[0] != null && player.grasps[0].grabbed == this.mask)//这是换手
                        player.SwitchGrasps(0, 1);
                    else//这是考虑按拾取 + ↓放下面具的情况
                        this.ReleaseGraspMask();
                }
            }
            /*
            if (this.mask != null && player.room != null && player.graphicsModule != null)
            {
                foreach (var sLeaser in player.room.game.cameras[0].spriteLeasers)
                {
                    if (sLeaser.drawableObject == player.graphicsModule)
                    {
                        mask.firstChunk.HardSetPosition(sLeaser.sprites[9].GetPosition() + player.room.game.cameras[0].CamPos(player.room.game.cameras[0].currentCameraPosition));
                        mask.firstChunk.vel *= 0f;
                    }
                }
            }
            */
        }

        public void ReleaseGraspMask()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            try
            {
                mask = null;
                int hand = FindMaskInHands(player);
                if (hand != -1)
                    player.grasps[hand].Release();
                player.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, player.mainBodyChunk);
                BuffPlugin.Log("Player remove mask on face!");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }

        }

        public int FindMaskInHands(Player player)
        {
            int hand = -1;
            for(int i = 0; i < player.grasps.Length; i++)
            {
                if (player.grasps[i] != null && player.grasps[i].grabbed is VultureMask)
                {
                    return i;
                }
            }
            return hand;
        }
        #endregion

        public bool isLaserActive()
        {
            if (!ownerRef.TryGetTarget(out var self))
                return false;
            return !self.dead && this.laserCounter > 0 && self.grasps[0] == null;
        }

        public void AirBrake(int frames)
        {
            if (!ownerRef.TryGetTarget(out var self))
                return;
            this.landingBrake = frames;
            this.landingBrakePos = self.bodyChunks[1].pos;
            if (frames > 5)
            {
                self.room.PlaySound(SoundID.Vulture_Jets_Air_Brake, self.mainBodyChunk);
            }
        }

        public bool CheckTentacleModeOr(VultureCatTentacle.Mode mode)
        {
            bool flag = false; 
            for (int i = 0; i < this.wings.GetLength(0); i++)
                for (int j = 0; j < this.wings.GetLength(1); j++)
                    flag = (flag || this.wings[i, j].mode == mode);
            return flag;
        }

        public bool CheckTentacleModeAnd(VultureCatTentacle.Mode mode)
        {
            bool flag = true;
            for (int i = 0; i < this.wings.GetLength(0); i++)
                for (int j = 0; j < this.wings.GetLength(1); j++)
                    flag = (flag && this.wings[i, j].mode == mode);
            return flag;
        }
        #region 拾取
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

            if (this.mask != null && result == Player.ObjectGrabability.BigOneHand)
                result = Player.ObjectGrabability.OneHand;
            return result;
        }
        #endregion
    }

    #region 翅膀
    internal class VultureCatFeather : BodyPart
    {
        private enum ContractMode
        {
            Even,
            Jerky,
            Jammed
        }

        public PlayerGraphics kGraphics;
        public VultureCatTentacle wing;
        private VultureCat vultureCat;
        public float wingPosition;
        private float ef;
        public float width;
        public float contractedLength;
        public float extendedLength;
        private ContractMode contractMode;
        public float contractSpeed;
        public float lose;
        public float brokenColor;
        public float forcedAlpha;
        public float lightnessBonus;
        public float saturationBonus;
        private int terrainContactTimer;

        public float extendedFac
        {
            get
            {
                return ef;
            }
            set
            {
                ef = Mathf.Clamp(value, 0f, 1f);
            }
        }

        public float CurrentLength => Mathf.Lerp(contractedLength, extendedLength, extendedFac);

        public Tentacle.TentacleChunk PreviousPreviousChunk => wing.tChunks[Custom.IntClamp(Mathf.FloorToInt(wingPosition * (float)wing.tChunks.Length) - 1, 0, wing.tChunks.Length - 1)];

        public Tentacle.TentacleChunk PreviousChunk => wing.tChunks[Custom.IntClamp(Mathf.FloorToInt(wingPosition * (float)wing.tChunks.Length), 0, wing.tChunks.Length - 1)];

        public Tentacle.TentacleChunk NextChunk => wing.tChunks[Custom.IntClamp(Mathf.FloorToInt(wingPosition * (float)wing.tChunks.Length) + 1, 0, wing.tChunks.Length - 1)];

        public float BetweenChunksLerp => wingPosition * (float)wing.tChunks.Length - Mathf.Floor(wingPosition * (float)wing.tChunks.Length);

        public Vector2 ConnectedPos => Vector2.Lerp(PreviousChunk.pos, NextChunk.pos, BetweenChunksLerp);

        public Vector2 ConnectedLastPos => Vector2.Lerp(PreviousChunk.lastPos, NextChunk.lastPos, BetweenChunksLerp);

        public VultureCatFeather(PlayerGraphics kGraphics, VultureCatTentacle wing, float wingPosition, float contractedLength, float extendedLength, float width, VultureCat vultureCat)
            : base(kGraphics)
        {
            this.kGraphics = kGraphics;
            this.wing = wing;
            this.wingPosition = wingPosition;
            this.contractedLength = contractedLength;
            this.extendedLength = extendedLength;
            this.width = width;
            this.vultureCat = vultureCat;
            lose = 0f;
        }

        public override void Update()
        {
            base.Update();
            if (kGraphics.owner.room.PointSubmerged(pos))
            {
                vel *= 0.1f;
            }
            lastPos = pos;
            pos += vel;
            vel *= 0.7f;
            Vector2 normalized = Vector2.Lerp(PreviousChunk.pos - PreviousPreviousChunk.pos, NextChunk.pos - PreviousChunk.pos, (PreviousPreviousChunk == PreviousChunk) ? 1f : BetweenChunksLerp).normalized;
            Vector2 vector = Custom.PerpendicularVector(normalized) * (vultureCat.IsMiros ? GetTentacleAngle(wing.index, wing.side) : ((wing.side == 1) ? (-1f) : 1f));
            float num = Mathf.Lerp(Mathf.Lerp(1f, Mathf.Lerp(-0.9f, 1.5f, Mathf.InverseLerp(wing.idealLength * 0.5f, wing.idealLength, Vector2.Distance(wing.FloatBase, wing.Tip.pos))), wingPosition), Mathf.Lerp(-0.5f, 4f, wingPosition), extendedFac);
            Vector2 vector2 = ConnectedPos + (vector + normalized * num).normalized * CurrentLength;
            vel += (vector2 - pos) * Mathf.Lerp(0.3f, 0.8f, wing.flyingMode) * (1f - lose);
            if (wing.flyingMode > extendedFac)
            {
                extendedFac += 1f / Mathf.Lerp(10f, 40f, UnityEngine.Random.value);
            }
            else if (wing.flyingMode < extendedFac)
            {
                if (extendedFac == 1f)
                {
                    contractMode = (vultureCat.IsMiros ? ContractMode.Jerky : ContractMode.Even);
                    contractSpeed = 1f / Mathf.Lerp(20f, 800f, UnityEngine.Random.value * UnityEngine.Random.value * UnityEngine.Random.value);
                    if (UnityEngine.Random.value < 0.3f)
                    {
                        if (UnityEngine.Random.value < 0.7f)
                        {
                            contractMode = ContractMode.Jerky;
                        }
                        else
                        {
                            contractMode = ContractMode.Jammed;
                        }
                    }
                }
                if (contractMode != 0 && extendedFac > 0.5f)
                {
                    extendedFac -= 1f / 120f;
                }
                switch (contractMode)
                {
                    case ContractMode.Even:
                        extendedFac -= contractSpeed;
                        break;
                    case ContractMode.Jerky:
                        if (UnityEngine.Random.value < 0.0016666667f)
                        {
                            extendedFac -= 1f / Mathf.Lerp(4f, 30f, UnityEngine.Random.value);
                        }
                        break;
                    case ContractMode.Jammed:
                        if (UnityEngine.Random.value < 0.0007142857f)
                        {
                            contractMode = ContractMode.Jerky;
                        }
                        break;
                }
            }
            lightnessBonus = Mathf.Max(lightnessBonus - 0.1f, 0f);
            if (lightnessBonus == 0f)
            {
                saturationBonus = Mathf.Max(saturationBonus - 0.02f, 0f);
            }
            forcedAlpha = Mathf.Lerp(forcedAlpha, 0f, 0.05f);
            ConnectToPoint(ConnectedPos, CurrentLength, push: true, 0f, PreviousChunk.vel, 0.3f, 0f);
            if (terrainContact)
            {
                terrainContactTimer++;
            }
            else
            {
                terrainContactTimer = 0;
            }
            Vector2 vector3 = 0.25f * vel;
            PushOutOfTerrain(kGraphics.owner.room, ConnectedPos);
            if (terrainContact && terrainContactTimer > 4)
            {
                if (vultureCat.IsMiros)
                {
                    //kGraphics.owner.room.PlaySound((Random.value < 0.5f) ? SoundID.Spear_Fragment_Bounce : SoundID.Spear_Bounce_Off_Wall, pos, Mathf.InverseLerp(10f, 60f, vector3.magnitude), Mathf.Lerp(3.5f, 0.5f, Mathf.InverseLerp(7f, 70f, CurrentLength)));
                }
                else
                {
                    //kGraphics.owner.room.PlaySound(SoundID.Vulture_Feather_Hit_Terrain, pos, Mathf.InverseLerp(0.2f, 20f, vector3.magnitude), Mathf.Lerp(3.5f, 0.5f, Mathf.InverseLerp(7f, 70f, CurrentLength)));
                }
                terrainContactTimer = 0;
            }
        }

        public Color CurrentColor()
        {
            if (vultureCat.IsMiros)
            {
                Color rgb = HSLColor.Lerp(new HSLColor(vultureCat.ColorB.hue, Mathf.Lerp(vultureCat.ColorB.saturation, 1f, saturationBonus), Mathf.Lerp(vultureCat.ColorB.lightness, 1f, lightnessBonus)), vultureCat.ColorA, Mathf.Cos(Mathf.Pow(wingPosition, 0.75f) * (float)Math.PI)).rgb;
                rgb.a = Mathf.Max(0.4f, forcedAlpha, Mathf.Lerp(0.4f, 0.8f, Mathf.Cos(Mathf.Pow(wingPosition, 1.7f) * (float)Math.PI))) * (extendedFac + wing.flyingMode) * 0.5f * (1f - brokenColor);
                if (vultureCat.isLaserActive())
                {
                    rgb.a = UnityEngine.Random.value;
                }
                return rgb;
            }
            HSLColor colorB = vultureCat.ColorB;
            HSLColor colorA = vultureCat.ColorA;
            if (vultureCat.albino)
            {
                colorB.saturation = Mathf.Lerp(colorB.saturation, 1f, 0.2f);
                colorB.hue = 0f;
                colorB.lightness = Mathf.Lerp(colorB.saturation, 0.2f, 0.8f);
                colorA.saturation = 0.8f;
                colorA.lightness = 0.6f;
            }
            Color rgb2 = HSLColor.Lerp(new HSLColor(colorB.hue, 
                                                    Mathf.Lerp(colorB.saturation, 1f, saturationBonus), 
                                                    Mathf.Lerp(colorB.lightness, 1f, lightnessBonus)), 
                                       colorA, 
                                       Mathf.Cos(Mathf.Pow(wingPosition, 0.75f) * (float)Math.PI)).rgb;
            rgb2.a = Mathf.Max(forcedAlpha, Mathf.Lerp(0.2f, 0.6f, Mathf.Cos(Mathf.Pow(wingPosition, 1.7f) * (float)Math.PI))) * (extendedFac + wing.flyingMode) * 0.5f * (1f - brokenColor);
            return rgb2;
        }

        public float GetTentacleAngle(int index, int side)
        {
            if (index == 0 && side % 2 == 0)
            {
                return 1f;
            }
            if (index == 0 && side == 1)
            {
                return -1f;
            }
            if (index == 1 && side % 2 == 0)
            {
                return 4f;
            }
            return -4f;
        }
    }

    internal class VultureCatTentacle : Tentacle
    {
        public class Mode : ExtEnum<Mode>
        {
            public static readonly Mode Climb = new Mode("Climb", register: true);

            public static readonly Mode Fly = new Mode("Fly", register: true);

            public Mode(string value, bool register = false)
                : base(value, register)
            {
            }
        }

        public VultureCat vultureCat;
        private DebugSprite[] grabGoalSprites;

        public int index;
        public int side;
        public Mode mode;
        public Vector2 desiredGrabPos;
        private bool attachedAtTip;
        public int segmentsGrippingTerrain;
        public int framesWithoutReaching;
        public int otherTentacleIsFlying;
        public int grabDelay;
        public int framesOfHittingTerrain;
        public bool playGrabSound;
        public int stun;
        private float fm;
        public StaticSoundLoop wooshSound;
        private List<IntVector2> scratchPath;

        public Player player => owner as Player;

        public float tentacleDir
        {
            get
            {
                if (index == 0 && side % 2 == 0)
                {
                    return -1f;
                }
                if (!vultureCat.IsMiros)
                {
                    return 1f;
                }
                if (index == 0 && side == 1)
                {
                    return 1f;
                }
                if (index == 1 && side % 2 == 0)
                {
                    return -4f;
                }
                return 4f;
            }
        }

        public bool hasAnyGrip
        {
            get
            {
                if (!attachedAtTip)
                {
                    return segmentsGrippingTerrain > 0;
                }
                return true;
            }
        }

        public float flyingMode
        {
            get
            {
                return fm;
            }
            set
            {
                fm = Mathf.Clamp(value, 0f, 1f);
            }
        }

        private VultureCatTentacle OtherTentacle => vultureCat.wings[index, 1 - side];

        #region 翅膀、羽毛的尺寸
        public float TentacleContour(float x)
        {
            float num = Mathf.Lerp(0.45f, 0.1f, flyingMode);
            float num2 = Mathf.Lerp(0.51f, 0.25f, flyingMode);
            float num3 = Mathf.Lerp(0.85f, 0.4f, flyingMode);
            float num4 = Mathf.Lerp(6.5f, 5.5f, flyingMode);
            float num5 = Mathf.Lerp(0.5f, 0.35f, flyingMode);
            float num6 = Mathf.Lerp(0.85f, 0f, flyingMode);
            float num7 = num6 + (1f - num6) * Mathf.Cos(Mathf.InverseLerp(num2, 1.2f, x) * (float)Math.PI * 0.5f);
            float num8 = (vultureCat.IsKing ? 1.2f : 1f);
            if (x < num)
            {
                return vultureCat.wingWidth * num4 * num5 * num8;
            }
            if (x < num2)
            {
                return vultureCat.wingWidth * num4 * Mathf.Lerp(num5, 1f, Custom.SCurve(Mathf.InverseLerp(num, num2, x), 0.1f)) * num8;
            }
            if (x < num3)
            {
                return vultureCat.wingWidth * num4 * num7 * num8;
            }
            return vultureCat.wingWidth * num4 * Mathf.Lerp(0.5f, 1f, Mathf.Cos(Mathf.Pow(Mathf.InverseLerp(num3, 1f, x), 4f) * (float)Math.PI * 0.5f)) * num7 * num8;
        }

        public float FeatherContour(float x)
        {
            return FeatherContour(x, flyingMode);
        }

        public static float FeatherContour(float x, float k)
        {
            float num = Mathf.Lerp(0.2f, 1f, Custom.SCurve(Mathf.Pow(x, 1.5f), 0.1f));
            if (Mathf.Pow(x, 1.5f) > 0.5f)
            {
                num *= Mathf.Sqrt(1f - Mathf.Pow(Mathf.InverseLerp(0.5f, 1f, Mathf.Pow(x, 1.5f)), 4.5f));
            }
            float num2 = 1f;
            num2 *= Mathf.Pow(Mathf.Sin(Mathf.Pow(x, 0.5f) * (float)Math.PI), 0.7f);
            if (x < 0.3f)
            {
                num2 *= Mathf.Lerp(0.7f, 1f, Custom.SCurve(Mathf.InverseLerp(0f, 0.3f, x), 0.5f));
            }
            return Mathf.Lerp(num * 0.5f, num2, k);
        }

        public static float FeatherWidth(float x)
        {
            return Mathf.Pow(Mathf.Sin(Mathf.InverseLerp(-0.45f, 1f, x) * (float)Math.PI), 2.6f);
        }
        #endregion

        public VultureCatTentacle(Player player, BodyChunk chunk, float length, int index, int side, VultureCat vultureCat)
            : base(player, chunk, length)
        {
            this.index = index;
            this.side = side;
            this.vultureCat = vultureCat;
            this.room = player.room;
            this.grabPath = new List<IntVector2>();
            this.segments = new List<IntVector2>();
            for (int i = 0; i < (int)(this.idealLength / 20f); i++)
            {
                this.segments.Add(player.abstractCreature.pos.Tile);//room.GetTilePosition(this.owner.firstChunk.pos)
            }
            tProps = new TentacleProps(stiff: false, rope: false, shorten: true, 0.5f, 0f, 0.2f, 1.2f, 0.2f, 1.2f, 10f, 0.25f, 5f, 15, 60, 12, 0);
            tChunks = new TentacleChunk[vultureCat.IsKing ? 10 : 8];
            for (int i = 0; i < tChunks.Length; i++)
            {
                tChunks[i] = new TentacleChunk(this, i, (float)(i + 1) / (float)tChunks.Length, vultureCat.wingWidth);
                tChunks[i].PhaseToSegment();
                tChunks[i].Reset();
            }
            mode = Mode.Climb;
            debugViz = false; 
            this.wooshSound = new StaticSoundLoop(SoundID.Vulture_Wing_Woosh_LOOP, base.Tip.pos, room, 1f, 1f);
        }

        public override void NewRoom(Room room)
        {
            /*//idealLength < 20f将导致segments不存在元素
            idealLength = Mathf.Lerp(vultureCat.wingLength * (vultureCat.IsKing ? 9f : 7f),
                                     vultureCat.wingLength * (vultureCat.IsKing ? 13.5f : 11f),
                                     flyingMode);*/
            base.NewRoom(room);
            wooshSound = new StaticSoundLoop(SoundID.Vulture_Wing_Woosh_LOOP, base.Tip.pos, room, 1f, 1f);
            if (debugViz)
            {
                if (grabGoalSprites != null)
                {
                    grabGoalSprites[0].RemoveFromRoom();
                    grabGoalSprites[1].RemoveFromRoom();
                }
                grabGoalSprites = new DebugSprite[2];
                grabGoalSprites[0] = new DebugSprite(new Vector2(0f, 0f), new FSprite("pixel"), room);
                grabGoalSprites[0].sprite.scale = 10f;
                grabGoalSprites[0].sprite.color = new Color(1f, 0f, 0f);
                grabGoalSprites[1] = new DebugSprite(new Vector2(0f, 0f), new FSprite("pixel"), room);
                grabGoalSprites[1].sprite.scale = 10f;
                grabGoalSprites[1].sprite.color = new Color(0f, 5f, 0f);
            }
        }

        public override void Update()
        {
            base.Update();
            if (player.enteringShortCut.HasValue)
            {
                base.retractFac = Mathf.Min(0f, base.retractFac - 0.1f);
                for (int i = 0; i < tChunks.Length; i++)
                {
                    tChunks[i].vel += Vector2.ClampMagnitude(room.MiddleOfTile(player.enteringShortCut.Value) - tChunks[i].pos, 50f) / 10f;
                }
                if (segments.Count > 1)
                {
                    segments.RemoveAt(segments.Count - 1);
                }
                return;
            }
            attachedAtTip = false;
            idealLength = Mathf.Lerp(vultureCat.wingLength * (vultureCat.IsKing ? 9f : 7f), 
                                     vultureCat.wingLength * (vultureCat.IsKing ? 13.5f : 11f), 
                                     flyingMode);
            if (stun > 0)
            {
                stun--;
            }
            if (Mathf.Pow(UnityEngine.Random.value, 0.25f) > 2f * (vultureCat.State as VultureCatState).wingHealth[index, side])
            {
                stun = Math.Max(stun, (int)Mathf.Lerp(-2f, 12f, Mathf.Pow(UnityEngine.Random.value, 0.5f + 20f * Mathf.Max(0f, (vultureCat.State as VultureCatState).wingHealth[index, side]))));
            }
            limp = stun > 0;//!vultureCat.Consious || 
            if (limp)
            {
                floatGrabDest = null;
                for (int j = 0; j < tChunks.Length; j++)
                {
                    tChunks[j].vel *= 0.9f;
                    tChunks[j].vel.y -= 0.5f;
                }
            }
            for (int k = 0; k < tChunks.Length; k++)
            {
                tChunks[k].rad = TentacleContour(tChunks[k].tPos);
                if (backtrackFrom == -1 || k < backtrackFrom)
                {
                    if (k > 1 && Custom.DistLess(tChunks[k].pos, tChunks[k - 2].pos, 30f))
                    {
                        tChunks[k].vel -= Custom.DirVec(tChunks[k].pos, tChunks[k - 2].pos) * (30f - Vector2.Distance(tChunks[k].pos, tChunks[k - 2].pos)) * 0.1f;
                    }
                    else if (k <= 1)
                    {
                        tChunks[k].vel = Custom.DirVec(OtherTentacle.connectedChunk.pos, connectedChunk.pos) * ((k == 0) ? 2f : 1.2f);
                    }
                }
                if (room.PointSubmerged(tChunks[k].pos))
                {
                    tChunks[k].vel *= 0.5f;
                }
                if (tChunks[k].contactPoint.x != 0 && tChunks[k].lastContactPoint.x == 0 && Mathf.Abs(tChunks[k].pos.x - tChunks[k].lastPos.x) > 6f)
                {
                    room.PlaySound(SoundID.Vulture_Tentacle_Collide_Terrain, tChunks[k].pos, 0.25f * Mathf.InverseLerp(6f, 16f, Mathf.Abs(tChunks[k].pos.x - tChunks[k].lastPos.x)), 1f);
                }
                else if (tChunks[k].contactPoint.y != 0 && tChunks[k].lastContactPoint.y == 0 && Mathf.Abs(tChunks[k].pos.y - tChunks[k].lastPos.y) > 6f)
                {
                    room.PlaySound(SoundID.Vulture_Tentacle_Collide_Terrain, tChunks[k].pos, 0.25f * Mathf.InverseLerp(6f, 16f, Mathf.Abs(tChunks[k].pos.y - tChunks[k].lastPos.y)), 1f);
                }
            }
            if (!limp)
            {
                if (mode == Mode.Climb)
                {
                    if (floatGrabDest.HasValue && Custom.DistLess(tChunks[tChunks.Length - 1].pos, floatGrabDest.Value, 40f) && backtrackFrom == -1)
                    {
                        tChunks[tChunks.Length - 1].pos = floatGrabDest.Value;
                        tChunks[tChunks.Length - 1].vel *= 0f;
                        attachedAtTip = true;
                    }
                    flyingMode -= 0.025f;
                    base.Tip.collideWithTerrain = !attachedAtTip;
                    UpdateDesiredGrabPos();
                    bool flag = side % 2 == 0;
                    BodyChunk bodyChunk = player.bodyChunks[0];//(flag ? vultureCat.bodyChunks[3] : vultureCat.bodyChunks[2]);
                    segmentsGrippingTerrain = 0;
                    for (int l = 0; l < tChunks.Length; l++)
                    {
                        tChunks[l].vel *= Mathf.Lerp(0.95f, 0.85f, Support());
                        if (attachedAtTip && (backtrackFrom == -1 || l < backtrackFrom) && GripTerrain(l))
                        {
                            segmentsGrippingTerrain++;
                            for (int num = l - 1; num > 0; num--)
                            {
                                PushChunksApart(l, num);
                            }
                        }
                        else
                        {
                            tChunks[l].vel.y += 0.1f;
                            tChunks[l].vel += connectedChunk.vel * 0.1f;
                            if (!hasAnyGrip)
                            {
                                if (floatGrabDest.HasValue)
                                {
                                    tChunks[l].vel += Custom.DirVec(tChunks[l].pos, floatGrabDest.Value) * 0.3f;
                                }
                                else
                                {
                                    tChunks[l].vel += Custom.DirVec(tChunks[l].pos, desiredGrabPos + Custom.DirVec(base.FloatBase, desiredGrabPos) * 70f) * 0.6f;
                                }
                            }
                        }
                        tChunks[l].vel += Custom.DirVec(bodyChunk.pos, tChunks[l].pos) * 0.5f / ((float)l + 1f);
                    }
                    if (attachedAtTip)
                    {
                        framesWithoutReaching = 0;
                        if (SharedPhysics.RayTraceTilesForTerrain(room, base.BasePos, base.grabDest.Value))
                        {
                            if (//!Custom.DistLess(base.Tip.pos, connectedChunk.pos, idealLength)
                                Custom.DistLess(vultureCat.wantPos, connectedChunk.pos, 20f) &&
                                Vector2.Dot(vultureCat.wantPos - connectedChunk.pos, base.Tip.pos - connectedChunk.pos) < 0 &&
                                !Custom.DistLess(base.Tip.pos, connectedChunk.pos, idealLength * 0.9f) &&
                                Custom.DistLess(base.Tip.pos, connectedChunk.pos, idealLength * 1.0f + 15f))
                            {
                                Vector2 vector = Custom.DirVec(base.Tip.pos, connectedChunk.pos);
                                float num2 = Vector2.Distance(base.Tip.pos, connectedChunk.pos);
                                float num3 = idealLength * 0.9f;
                                connectedChunk.pos += vector * (num3 - num2) * 0.2f;
                                connectedChunk.vel += vector * (num3 - num2) * 0.2f;
                            }/*
                            if (!Custom.DistLess(base.Tip.pos, connectedChunk.pos, idealLength * 0.9f))
                            {
                                vultureCat.hangingInTentacle = true;
                            }*/
                            if(!Custom.DistLess(base.Tip.pos, connectedChunk.pos, idealLength * vultureCat.groundRetractionScale)// && UnderneathIsSolid()
                                )
                            {
                                ReleaseGrip();
                                vultureCat.hangingInTentacle = false;
                            }
                        }
                        if (playGrabSound)
                        {
                            room.PlaySound(SoundID.Vulture_Tentacle_Grab_Terrain, base.Tip.pos, 0.25f, 1f);
                            playGrabSound = false;
                        }
                    }
                    /*//允许伸翅膀
                    else if (!Custom.DistLess(connectedChunk.pos, vultureCat.wantPos, 5f) && 
                             OtherTentacle.attachedAtTip)
                    {
                        tChunks[tChunks.Length - 1].vel = Vector2.Lerp(tChunks[tChunks.Length - 1].vel, vultureCat.wantPos - tChunks[tChunks.Length - 1].pos, 0.05f);
                    }*/
                    else
                    {
                        playGrabSound = true;
                        FindGrabPos(ref scratchPath);
                        framesWithoutReaching++;
                        if ((float)framesWithoutReaching > 60f && !floatGrabDest.HasValue)
                        {/*
                            Vector2 down = Vector2.down * idealLength * (UnderneathIsSolid() ? 1f : 0f);
                            tChunks[tChunks.Length - 1].vel = Vector2.Lerp(tChunks[tChunks.Length - 1].vel, connectedChunk.pos + down - tChunks[tChunks.Length - 1].pos, 0.05f);*/
                            framesWithoutReaching = 0;
                            //SwitchMode(Mode.Fly);
                        }
                    }
                    if (OtherTentacle.mode == Mode.Fly)
                    {
                        otherTentacleIsFlying++;
                        if (!hasAnyGrip && ((otherTentacleIsFlying > 30 && room.aimap.getTerrainProximity(base.BasePos) >= 3) || otherTentacleIsFlying > 100))
                        {
                            SwitchMode(Mode.Fly);
                            vultureCat.InitiateFlight(player);
                            otherTentacleIsFlying = 0;
                        }
                    }
                    else
                    {
                        otherTentacleIsFlying = 0;
                    }
                }
                else if (mode == Mode.Fly)
                {
                    bool contact = false;
                    flyingMode += 0.05f;
                    for (int m = 0; m < tChunks.Length; m++)
                    {
                        tChunks[m].vel *= 0.95f;
                        tChunks[m].vel.x += tentacleDir * 0.6f;
                        bool shouldBeMirrored = side % 2 == 0;
                        Vector2 wantDir = Vector2.Lerp(Custom.PerpendicularVector(Custom.DirVec(player.bodyChunks[0].pos, player.bodyChunks[1].pos)) * (shouldBeMirrored ? (-1f) : 1f),
                                                       new Vector2(tentacleDir, 0f),
                                                       0.5f);
                        Vector2 wantPos = connectedChunk.pos + idealLength * Mathf.Pow(tChunks[m].tPos, Mathf.Sqrt(vultureCat.wingLength / 20f)) * wantDir;
                        Vector2 perp = Custom.PerpendicularVector((connectedChunk.pos - wantPos).normalized) * (shouldBeMirrored ? (-1f) : 1f);
                        float wave = Mathf.Sin((float)Math.PI * 2f * (vultureCat.wingFlap - tChunks[m].tPos * 0.5f));
                        float waveScale = Mathf.Lerp(Mathf.Pow(1 - tChunks[m].tPos, 1 - Mathf.Sqrt(vultureCat.wingLength / 20f)), 1f, 0.5f) * 
                                          vultureCat.wingLength * Mathf.Lerp(10f, 30f, vultureCat.wingFlapAmplitude);
                        waveScale *= player.input[0].y < 0 ? 0f : 1f;
                        waveScale *= 1f + player.mainBodyChunk.vel.magnitude / 10f;
                        wantPos += perp * waveScale * wave;
                        tChunks[m].vel += Vector2.ClampMagnitude(wantPos - tChunks[m].pos, 1.5f * vultureCat.wingLength) / (1.5f * vultureCat.wingLength) * 5f * Mathf.Lerp(0.2f, 1f, vultureCat.wingFlapAmplitude);
                        if (tChunks[m].contactPoint.x != 0 || tChunks[m].contactPoint.y != 0)
                        {
                            contact = true;
                        }
                    }
                    float num4 = 0.5f;
                    if (vultureCat.IsMiros)
                    {
                        num4 = 1.4f / (float)vultureCat.wings.Length;
                    }
                    player.bodyChunks[0].vel.y += vultureCat.wingLength / 20f * Mathf.Pow(num4 + num4 * Mathf.Sin((float)Math.PI * 2f * vultureCat.wingFlap), 2f) * 5.6f * Mathf.Lerp(0.5f, 1f, vultureCat.wingFlapAmplitude);
                    player.bodyChunks[0].vel.x += vultureCat.wingLength / 20f * (num4 + num4 * Mathf.Sin((float)Math.PI * 2f * vultureCat.wingFlap)) * -2.6f * tentacleDir * Mathf.Lerp(0.5f, 1f, vultureCat.wingFlapAmplitude);
                    if (OtherTentacle.stun > 0 && stun < 1)
                    {
                        for (int n = 0; n < player.bodyChunks.Length; n++)
                        {
                            player.bodyChunks[n].vel += vultureCat.wingLength / 20f * Custom.DirVec(base.Tip.pos, player.bodyChunks[n].pos) * Mathf.Pow(num4 + num4 * Mathf.Sin((float)Math.PI * 2f * vultureCat.wingFlap), 2f) * 0.4f * Mathf.Lerp(0.5f, 1f, vultureCat.wingFlapAmplitude);
                        }
                    }
                    if (contact)
                    {
                        framesOfHittingTerrain++;
                    }
                    else
                    {
                        framesOfHittingTerrain--;
                    }
                    framesOfHittingTerrain = Custom.IntClamp(framesOfHittingTerrain, 0, 30);/*
                    if (framesOfHittingTerrain >= 30)
                    {
                        framesOfHittingTerrain = 0;
                        SwitchMode(Mode.Climb);
                    }
                    else if (OtherTentacle.mode == Mode.Climb && OtherTentacle.attachedAtTip)
                    {
                        UpdateDesiredGrabPos();
                        FindGrabPos(ref scratchPath);
                        if (floatGrabDest.HasValue)
                        {
                            SwitchMode(Mode.Climb);
                        }
                    }*/
                }
            }
            wooshSound.volume = Custom.SCurve(Mathf.InverseLerp(0.4f, 18f, Vector2.Distance(base.Tip.pos - connectedChunk.pos, base.Tip.lastPos - connectedChunk.lastPos)), 0.6f) * flyingMode;
            wooshSound.pitch = Mathf.Lerp(0.3f, 1.7f, Mathf.InverseLerp(-20f, 20f, base.Tip.lastPos.y - base.Tip.pos.y - (connectedChunk.lastPos.y - connectedChunk.pos.y)));
            wooshSound.pos = Vector2.Lerp(connectedChunk.pos, base.Tip.pos, 0.7f);
            wooshSound.Update();
            if (debugViz)
            {
                grabGoalSprites[1].pos = desiredGrabPos;
            }
        }

        public void SwitchMode(Mode newMode)
        {
            mode = newMode;
            if (newMode == Mode.Fly)
            {
                if (vultureCat.IsMiros)
                {
                    ReleaseGrip();
                }
                floatGrabDest = null;
            }
        }

        public void ReleaseGrip()
        {
            if (OtherTentacle.grabDelay < 1)
            {
                grabDelay = 10;// 10;
            }
            floatGrabDest = null;
        }

        private void UpdateDesiredGrabPos()
        {
            if (vultureCat.hoverStill)
            {
                desiredGrabPos = player.mainBodyChunk.pos +
                                 (UnderneathIsSolid() ? 0f : 0f) * Vector2.down * idealLength +
                                 (UnderneathIsSolid() ? 0.7f : 0.7f) * new Vector2(tentacleDir, -0.8f).normalized * idealLength;
            }
            else
            {
                desiredGrabPos = player.mainBodyChunk.pos +
                                 (UnderneathIsSolid() ? 0f : 0f) * Vector2.down * idealLength +
                                 (UnderneathIsSolid() ? 0.7f : 0.7f) * (Vector2)Vector3.Slerp(vultureCat.moveDirection, new Vector2(tentacleDir, -0.8f).normalized, 0.3f) * idealLength;
            }
        }

        private void FindGrabPos(ref List<IntVector2> path)
        {
            if (grabDelay > 0)
            {
                grabDelay--;
                return;
            }
            IntVector2? intVector = ClosestSolid(room.GetTilePosition(desiredGrabPos), 8, 8f);
            if (intVector.HasValue)
            {
                IntVector2? intVector2 = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(room, base.BasePos, intVector.Value);
                if (!base.grabDest.HasValue || GripPointAttractiveness(intVector2.Value) > GripPointAttractiveness(base.grabDest.Value))
                {
                    Vector2 newGrabDest = Custom.RestrictInRect(base.FloatBase, FloatRect.MakeFromVector2(room.MiddleOfTile(intVector2.Value) - new Vector2(11f, 11f), room.MiddleOfTile(intVector2.Value) + new Vector2(11f, 11f)));
                    MoveGrabDest(newGrabDest, ref path);
                }
            }
            Vector2 pos = desiredGrabPos + Custom.DegToVec(UnityEngine.Random.value * 360f) * UnityEngine.Random.value * idealLength;
            int num = room.RayTraceTilesList(base.BasePos.x, base.BasePos.y, room.GetTilePosition(pos).x, room.GetTilePosition(pos).y, ref path);
            for (int i = 0; i < num && !room.GetTile(path[i]).Solid; i++)
            {
                if ((room.GetTile(path[i]).horizontalBeam || room.GetTile(path[i]).verticalBeam) && (!base.grabDest.HasValue || GripPointAttractiveness(path[i]) > GripPointAttractiveness(base.grabDest.Value)))
                {
                    MoveGrabDest(room.MiddleOfTile(path[i]), ref path);
                    break;
                }
            }
        }

        public float ReleaseScore()
        {
            if (mode != Mode.Climb)
            {
                return float.MinValue;
            }
            float num = Vector2.Distance(base.Tip.pos, desiredGrabPos);
            if (!floatGrabDest.HasValue)
            {
                num *= 2f;
            }
            return num;
        }

        private bool UnderneathIsSolid()
        {
            float num = Custom.AimFromOneVectorToAnother(player.bodyChunks[1].pos, player.bodyChunks[0].pos);
            bool result = player.bodyChunks[0].ContactPoint.y < 0 || player.bodyChunks[1].ContactPoint.y < 0 ||
                          player.room.GetTile(player.bodyChunks[0].pos + 20f * Mathf.Abs(Mathf.Sin(num)) * Vector2.down).Terrain == Room.Tile.TerrainType.Solid ||
                          player.room.GetTile(player.bodyChunks[0].pos + 20f * Mathf.Abs(Mathf.Sin(num)) * Vector2.down).Terrain == Room.Tile.TerrainType.Slope ||
                          player.room.GetTile(player.bodyChunks[0].pos + 20f * Mathf.Abs(Mathf.Sin(num)) * Vector2.down).Terrain == Room.Tile.TerrainType.Floor;
            return result;
        }

        private bool GripTerrain(int chunk)
        {
            for (int i = 0; i < 4; i++)
            {
                if (room.GetTile(room.GetTilePosition(tChunks[chunk].pos) + Custom.fourDirections[i]).Solid)
                {
                    tChunks[chunk].vel *= 0.25f;
                    tChunks[chunk].vel += Custom.fourDirections[i].ToVector2() * 0.8f;
                    if (tChunks[chunk].contactPoint.x == 0)
                    {
                        return tChunks[chunk].contactPoint.y != 0;
                    }
                    return true;
                }
            }
            if (room.GetTile(tChunks[chunk].pos).horizontalBeam)
            {
                tChunks[chunk].vel *= 0.25f;
                tChunks[chunk].vel.y += (room.MiddleOfTile(tChunks[chunk].pos).y - tChunks[chunk].pos.y) * 0.3f;
                return true;
            }
            if (room.GetTile(tChunks[chunk].pos).verticalBeam)
            {
                tChunks[chunk].vel *= 0.25f;
                tChunks[chunk].vel.x += (room.MiddleOfTile(tChunks[chunk].pos).x - tChunks[chunk].pos.x) * 0.3f;
                return true;
            }
            return false;
        }

        private float GripPointAttractiveness(IntVector2 pos)
        {
            if (room.GetTile(pos).Solid)
            {
                return 100f / room.GetTilePosition(desiredGrabPos).FloatDist(pos);
            }
            return 65f / room.GetTilePosition(desiredGrabPos).FloatDist(pos);
        }

        public float Support()
        {
            if (stun > 0)
            {
                return 0f;
            }
            if (mode == Mode.Climb)
            {
                return Mathf.Clamp(((!hasAnyGrip) ? 0f : (vultureCat.IsMiros ? 4f : 0.5f)) + (float)segmentsGrippingTerrain / (float)tChunks.Length, 0f, 1f);
            }
            if (mode == Mode.Fly)
            {
                if (!vultureCat.IsMiros)
                {
                    return 0.5f;
                }
                return 1.2f;
            }
            return 0f;
        }

        private IntVector2? ClosestSolid(IntVector2 goal, int maxDistance, float maxDistFromBase)
        {
            if (room.GetTile(goal).Solid)
            {
                return goal;
            }
            for (int i = 1; i <= maxDistance; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (room.GetTile(goal + Custom.eightDirections[j] * i).Solid && base.BasePos.FloatDist(goal + Custom.eightDirections[j] * i) < maxDistFromBase)
                    {
                        return goal + Custom.eightDirections[j] * i;
                    }
                }
            }
            return null;
        }

        public bool WingSpace()
        {
            for (int i = -1; i <= 1; i++)
            {
                if (!SharedPhysics.RayTraceTilesForTerrain(room, room.GetTilePosition(connectedChunk.pos), room.GetTilePosition(connectedChunk.pos + new Vector2(tentacleDir * idealLength, 50f * (float)i))))
                {
                    return false;
                }
            }
            return true;
        }

        public override IntVector2 GravityDirection()
        {
            if (!(UnityEngine.Random.value < 0.5f))
            {
                return new IntVector2(0, -1);
            }
            return new IntVector2((int)tentacleDir, -1);
        }

        public void Damage(Creature.DamageType type, float damage, float stunBonus)
        {
            damage /= 2.7f;
            stunBonus /= 1.2f;
            stun = Math.Max(stun, (int)(damage * 30f + stunBonus));
            (vultureCat.State as VultureCatState).wingHealth[index, side] -= damage;
        }
    }
    #endregion

    internal class VultureCatState : HealthState
    {
        VultureCat vultureCat;
        public float[,] wingHealth;

        public bool mask;

        public float[] randomValueForWingColor;
        private string header = "RANDOMVALUEFORWINGCOLOR";

        public VultureCatState(AbstractCreature creature, VultureCat vultureCat)
            : base(creature)
        {
            bool flag = ModManager.MSC && creature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.MirosVulture;
            wingHealth = new float[flag ? 2 : 1, 2];
            for (int i = 0; i < wingHealth.GetLength(0); i++)
                for (int j = 0; j < wingHealth.GetLength(1); j++)
                    wingHealth[i, j] = 1f;
            mask = !flag;
            this.vultureCat = vultureCat;
        }

        public override string ToString()
        {
            string text = HealthBaseSaveString() + (mask ? "" : "<cB>NOMASK");
            foreach (KeyValuePair<string, string> unrecognizedSaveString in unrecognizedSaveStrings)
            {
                text = text + "<cB>" + unrecognizedSaveString.Key + "<cC>" + unrecognizedSaveString.Value;
            }
            if (vultureCat.randomValueForWingColor != null)
            {
                randomValueForWingColor = vultureCat.randomValueForWingColor;
                string result = "<vcdA>" + header;
                for (int i = 0; i < randomValueForWingColor.Length; i++)
                {
                    result += "<vcdB>" + i.ToString() + "<randomValueForWingColor>" + randomValueForWingColor[i].ToString();
                }
                result += "<vcdA>";
                text += result;
            }
            return text;
        }

        public override void LoadFromString(string[] s)
        {
            base.LoadFromString(s);
            string rColor = "";
            for (int i = 0; i < s.Length; i++)
            {
                string text = Regex.Split(s[i], "<cC>")[0];
                if (text != null && text == "NOMASK")
                {
                    mask = false;
                }

                string[] array = Regex.Split(s[i], "<vcdA>");
                for (int m = 0; m < array.Length; m++)
                {
                    string[] array2 = Regex.Split(array[m], "<vcdB>");
                    if (array2.Length > 1 && array2[0] == header)
                    {
                        rColor = "<vcdA>" + header;
                        randomValueForWingColor = new float[array2.Length - 1];
                        for (int n = 1; n < array2.Length; n++)
                        {
                            string[] array3 = Regex.Split(array2[n], "<randomValueForWingColor>");
                            if (array3.Length > 1 && array3[0] != "" && array3[1] != "")
                            {
                                int header = int.Parse(array3[0]);
                                randomValueForWingColor[header] = float.Parse(array3[1]);
                                rColor += "<vcdB>" + header + "<randomValueForWingColor>" + randomValueForWingColor[header].ToString();
                            }
                        }
                    }
                }
            }
            unrecognizedSaveStrings.Remove("NOMASK"); 
            unrecognizedSaveStrings.Remove(rColor);
        }
    }
    
    #region 角矛
    internal class KingTusks
    {
        public class Tusk : SharedPhysics.IProjectileTracer
        {
            public class Mode : ExtEnum<Mode>
            {
                public static readonly Mode Attached = new Mode("Attached", register: true);

                public static readonly Mode Charging = new Mode("Charging", register: true);

                public static readonly Mode ShootingOut = new Mode("ShootingOut", register: true);

                public static readonly Mode StuckInCreature = new Mode("StuckInCreature", register: true);

                public static readonly Mode StuckInWall = new Mode("StuckInWall", register: true);

                public static readonly Mode Dangling = new Mode("Dangling", register: true);

                public static readonly Mode Retracting = new Mode("Retracting", register: true);

                public Mode(string value, bool register = false)
                    : base(value, register)
                {
                }
            }
            #region 字段
            public KingTusks owner;
            public int side;
            public static int TotalSprites = 3;
            private static int tuskSegs = 15;//15;
            public Vector2 lastZRot;
            public Vector2 zRot;
            public Rope rope;
            public Vector2[,] chunkPoints;
            public static float length = 30f;//30f;
            public static float maxWireLength = 500f;
            public static float shootRange = 550f;
            public static float minShootRange = 250f;
            public float attached;

            public Vector2[,] wire;
            private float wireLoose;
            private float lastWireLoose;
            private float currWireLength;
            private float wireExtraSlack;
            private float elasticity;

            public Mode mode;
            public int modeCounter;
            private int preparationTime;
            private int retractingTime;
            private float retractingSpeed;

            public Vector2? stuckInWallPos;
            public Vector2 shootDir;
            public Vector2 wantShootDir;
            private float stuck;

            private float laserAlpha;
            private float lastLaserAlpha;
            private float laserPower;

            private SharedPhysics.TerrainCollisionData scratchTerrainCollisionData;

            public BodyChunk impaleChunk;

            public Color armorColor;
            #endregion
            //火箭助推角矛
            private int shootCounter;
            private Creature focusCreature;
            public float InitSpeed => Custom.LerpMap(shootCounter, 20, 30, 0, 20);
            //
            #region 属性
            public Player player => owner.player;

            public Room room => owner.player.room;

            public BodyChunk head => owner.player.bodyChunks[1];

            public bool FullyAttached => attached == 1f;

            public bool StuckInAnything
            {
                get
                {
                    if (!(mode == Mode.StuckInCreature))
                    {
                        return mode == Mode.StuckInWall;
                    }
                    return true;
                }
            }

            public bool StuckOrShooting
            {
                get
                {
                    if (!StuckInAnything)
                    {
                        return mode == Mode.ShootingOut;
                    }
                    return true;
                }
            }

            public bool ReadyToShoot
            {
                get
                {
                    if (mode == Mode.Attached)
                    {
                        return laserPower == 1f;
                    }
                    return false;
                }
            }
            #endregion
            #region sprites序号
            public int FirstSprite(PlayerGraphics vGraphics)
            {
                if (side % 2 == 0 == (Mathf.Sign(owner.HeadRotVector.x) == Mathf.Sign(owner.HeadRotVector.y)))
                {
                    return owner.vultureCat.TuskSprite(1 + Mathf.FloorToInt(side / 2) * 2);
                }
                return owner.vultureCat.TuskSprite(0 + Mathf.FloorToInt(side / 2) * 2);
            }

            public int LaserSprite(PlayerGraphics vGraphics)
            {
                return FirstSprite(vGraphics);
            }

            public int TuskSprite(PlayerGraphics vGraphics)
            {
                return FirstSprite(vGraphics) + 1;
            }

            public int TuskDetailSprite(PlayerGraphics vGraphics)
            {
                return FirstSprite(vGraphics) + 2;
            }
            #endregion
            #region 角矛属性
            public float TuskBend(float f)
            {
                return Mathf.Sin(Mathf.Pow(f, 0.85f) * (float)Math.PI * 2f) * Mathf.Pow(1f - f, 2f);
            }

            public float TuskProfBend(float f)
            {
                return (0f - Mathf.Cos(Mathf.Pow(f, 0.85f) * (float)Math.PI * 2.5f)) * Mathf.Pow(1f - f, 3f);
            }

            public float TuskRad(float f, float profileFac)
            {
                return 0.5f + 2f * Mathf.Pow(Mathf.Clamp01(Mathf.Sin(Mathf.Pow(f, Mathf.Lerp(0.65f, 0.5f, profileFac)) * (float)Math.PI)), 1.2f - 0.3f * profileFac);
            }
            #endregion
            public Vector2 AimDir(float timeStacker)
            {
                if (mode == Mode.Charging || mode == Mode.ShootingOut)
                {
                    Vector2 inputDir = new Vector2(player.input[0].x, player.input[0].y).normalized;
                    wantShootDir = Vector3.Slerp(wantShootDir, inputDir, 0.02f);
                }
                else
                {
                    Vector2 vector = Custom.DirVec(Vector2.Lerp((player.graphicsModule as PlayerGraphics).head.lastPos, (player.graphicsModule as PlayerGraphics).head.pos, timeStacker), 
                                                   Vector2.Lerp(head.lastPos, head.pos, timeStacker));//Custom.DirVec(Vector2.Lerp(player.neck.tChunks[player.neck.tChunks.Length - 1].lastPos, player.neck.tChunks[player.neck.tChunks.Length - 1].pos, timeStacker), Vector2.Lerp(head.lastPos, head.pos, timeStacker));
                    float num = Mathf.InverseLerp(0f, 25f, (float)modeCounter + timeStacker);
                    if (owner.lastEyesHome > 0f || owner.eyesHomeIn > 0f)
                    {
                        Vector3 vector2 = Custom.DirVec(Vector2.Lerp(head.lastPos, head.pos, timeStacker), Vector2.Lerp(owner.lastPreyPos, owner.preyPos, timeStacker));
                        vector = Vector3.Slerp(vector, vector2, Mathf.Lerp(owner.lastEyesHome, owner.eyesHomeIn, timeStacker) * Mathf.Pow(Mathf.InverseLerp(0.2f, 0.85f - 0.2f * num, Vector2.Dot(vector, vector2)), 2f - 1.5f * num));
                    }
                    if (owner.lastEyesOut > 0f || owner.eyesOut > 0f)
                    {
                        vector += Custom.PerpendicularVector(vector) * Vector3.Slerp(Custom.DegToVec(owner.lastHeadRot + ((side % 2 == 0) ? (-90f) : 90f)), Custom.DegToVec(owner.headRot + ((side % 2 == 0) ? (-90f) : 90f)), timeStacker).x * Mathf.Lerp(owner.lastEyesOut, owner.eyesOut, timeStacker) * 0.5f;
                    }
                    wantShootDir = Vector2.Lerp(wantShootDir, vector, 0.02f);
                }
                return wantShootDir.normalized;
            }

            public Tusk(KingTusks owner, int side)
            {
                this.owner = owner;
                this.side = side;
                chunkPoints = new Vector2[2, 3];
                wire = new Vector2[20, 4];
                Reset(room);

                //shootRange = VultureShapedMutationBuff.Instance.RocketBoostTusks ? 700f : 550f;
                //maxWireLength = VultureShapedMutationBuff.Instance.RocketBoostTusks ? 650f : 500f;
            }

            public void Reset(Room newRoom)
            {
                attached = 1f;
                for (int i = 0; i < chunkPoints.GetLength(0); i++)
                {
                    chunkPoints[i, 0] = owner.player.bodyChunks[1].pos + Custom.RNV();
                    chunkPoints[i, 1] = chunkPoints[i, 0];
                    chunkPoints[i, 2] *= 0f;
                }
                if (rope != null && rope.visualizer != null)
                {
                    rope.visualizer.ClearSprites();
                }
                rope = null;
                for (int j = 0; j < wire.GetLength(0); j++)
                {
                    wire[j, 0] = head.pos + Custom.RNV() * UnityEngine.Random.value;
                    wire[j, 1] = wire[j, 0];
                    wire[j, 2] *= 0f;
                    wire[j, 3] *= 0f;
                }
                mode = Mode.Attached;
                modeCounter = 0;
                wireLoose = 0f;
                lastWireLoose = 0f;
                wireExtraSlack = 0f;
                elasticity = 0.9f;
                //角矛蓄力时间
                preparationTime = Mathf.FloorToInt(Custom.LerpMap(VultureShapedMutationBuffEntry.StackLayer, 3, 10, 10, 5));
                if (VultureShapedMutationBuff.Instance.AerialFirepower)
                {
                    preparationTime = Mathf.FloorToInt(preparationTime / 2f);
                }
                //绳索收回时间
                retractingTime = 80;
                if (VultureShapedMutationBuff.Instance.AerialFirepower)
                {
                    retractingTime = 0;
                }
                //绳索收回速度
                retractingSpeed = 1f / 90f;
                if (VultureShapedMutationBuff.Instance.AerialFirepower)
                {
                    retractingSpeed = 1f / 4f;
                }
            }

            public void SwitchMode(Mode newMode)
            {/*
                if (VultureShapedMutationBuff.Instance.RocketBoostTusks)
                {
                    if (newMode == KingTusks.Tusk.Mode.StuckInWall)
                    {
                        this.room.AddObject(new Explosion(this.room, null, this.stuckInWallPos.Value,
                            7, 150f, 6.2f, 0f, 10f, 0.25f, player, 0.7f, 0f, 1f));
                        var pos = this.stuckInWallPos.Value;
                        this.room.AddObject(new Explosion.ExplosionLight(pos, 100f, 1f, 7, Color.white));
                        this.room.AddObject(new Explosion.ExplosionLight(pos, 90f, 1f, 3, new Color(1f, 1f, 1f)));
                        this.room.AddObject(new ExplosionSpikes(this.room, pos, 10, 30f, 9f, 7f, 100f, Color.white));
                        this.room.AddObject(new ShockWave(pos, 130f, 0.025f, 5, false));
                        this.modeCounter = 30;
                        newMode = KingTusks.Tusk.Mode.Dangling;
                    }
                }*/
                if (!(mode == newMode))
                {
                    if (newMode != Mode.StuckInCreature)
                    {
                        impaleChunk = null;
                    }
                    modeCounter = 0;
                    mode = newMode;
                }/*
                if (VultureShapedMutationBuff.Instance.RocketBoostTusks)
                {
                    if (this.mode == KingTusks.Tusk.Mode.StuckInCreature)
                    {
                        var pos = this.impaleChunk?.pos ?? TuskPos(this);
                        this.room.AddObject(new Explosion(this.room, null, pos,
                            7, 150f, 6.2f, 0f, 10f, 0.25f, player, 0.3f, 0f, 1f));

                        this.room.AddObject(new Explosion.ExplosionLight(pos, 100f, 1f, 7, Color.white));
                        this.room.AddObject(new Explosion.ExplosionLight(pos, 90f, 1f, 3, new Color(1f, 1f, 1f)));
                        this.room.AddObject(new ExplosionSpikes(this.room, pos, 10, 30f, 9f, 7f, 100f, Color.white));
                        this.room.AddObject(new ShockWave(pos, 130f, 0.025f, 5, false));
                    }
                }*/
            }

            public void Shoot(Vector2 tuskHangPos)
            {
                SwitchMode(Mode.ShootingOut);
                room.PlaySound(SoundID.King_Vulture_Tusk_Shoot, head);
                shootDir = AimDir(1f);
                owner.noShootDelay = 20;
                stuck = 0f;
                attached = 0f;
                currWireLength = maxWireLength;
                head.vel -= shootDir * 25f;
                head.pos -= shootDir * 25f;
                head.lastPos -= shootDir * 25f;
                (player.graphicsModule as PlayerGraphics).head.pos -= shootDir * 35f;
                (player.graphicsModule as PlayerGraphics).head.lastPos -= shootDir * 35f;
                (player.graphicsModule as PlayerGraphics).head.vel -= shootDir * 35f;
                /*
                for (int i = 0; i < player.neck.tChunks.Length; i++)
                {
                    player.neck.tChunks[i].pos -= shootDir * 35f * Mathf.InverseLerp(0f, player.neck.tChunks.Length - 1, i);
                    player.neck.tChunks[i].lastPos -= shootDir * 35f * Mathf.InverseLerp(0f, player.neck.tChunks.Length - 1, i);
                    player.neck.tChunks[i].vel -= shootDir * 35f * Mathf.InverseLerp(0f, player.neck.tChunks.Length - 1, i);
                }*/
                player.bodyChunks[0].vel -= shootDir * 5f;
                wireExtraSlack = 1f;
                wireLoose = 1f;
                lastWireLoose = 1f;
                laserPower = 0f;
                laserAlpha = 0f;
                ShootUpdate(60f);
                if (rope != null && rope.visualizer != null)
                {
                    rope.visualizer.ClearSprites();
                }
                rope = new Rope(room, head.pos, chunkPoints[1, 0], 1f);
                for (int j = 0; j < wire.GetLength(0); j++)
                {
                    float num = Mathf.InverseLerp(0f, wire.GetLength(0), j);
                    Vector2 vector = Custom.RNV() * (1f - num);
                    wire[j, 1] = Vector2.Lerp(head.pos, chunkPoints[1, 0], num);
                    wire[j, 0] = Vector2.Lerp(head.pos, chunkPoints[1, 0], num) + vector * 80f * UnityEngine.Random.value;
                    wire[j, 2] = vector * 160f * UnityEngine.Random.value;
                }
                if (player.room.BeingViewed && !player.room.PointSubmerged(head.pos))
                {/*
                    if (owner.smoke == null)
                    {
                        owner.smoke = new Smoke.NewVultureSmoke(room, head.pos, owner.player);
                        room.AddObject(owner.smoke);
                    }
                    Vector2 a = Custom.DirVec(head.pos, tuskHangPos);
                    for (int k = 0; k < 8; k++)
                    {
                        float num2 = Mathf.InverseLerp(0f, 8f, k);
                        owner.smoke.pos = (head.pos + chunkPoints[0, 0] + chunkPoints[1, 0]) / 3f;
                        owner.smoke.EmitSmoke(Vector2.Lerp(a, -shootDir, num2) * Mathf.Lerp(15f, 60f, UnityEngine.Random.value * (1f - num2)) + Custom.RNV() * UnityEngine.Random.value * 60f, 1f);
                    }*/
                }
                if (VultureShapedMutationBuff.Instance.ActiveProtectionSysytem)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        player.room.AddObject(new ProtectionMissile(player.room, player.DangerPos, Custom.DegToVec(Custom.VecToDeg(shootDir) + i == 0 ? -60f : 60f), player));
                    }
                }
                if (VultureShapedMutationBuff.Instance.AerialFirepower)
                {
                    owner.noShootDelay = 3;
                    laserPower = 1f;
                }
            }

            public void ShootUpdate(float speed)
            {
                for (int i = 0; i < chunkPoints.GetLength(0); i++)
                {
                    chunkPoints[i, 1] = chunkPoints[i, 0];
                    chunkPoints[i, 2] *= 0f;
                }
                if (owner.smoke != null && modeCounter < 3)
                {
                    owner.smoke.pos = chunkPoints[1, 0];
                    owner.smoke.EmitSmoke(Custom.RNV() * UnityEngine.Random.value * 3f - shootDir * UnityEngine.Random.value * 3f, 1f);
                }
                float num = 20f;/*
                if (VultureShapedMutationBuff.Instance.RocketBoostTusks)
                    num = InitSpeed;*/
                Vector2 vector = chunkPoints[0, 0] + shootDir * num;
                Vector2 vector2 = chunkPoints[0, 0] + shootDir * (num + speed);
                FloatRect? floatRect = SharedPhysics.ExactTerrainRayTrace(room, vector, vector2);
                Vector2 vector3 = default(Vector2);
                if (floatRect.HasValue)
                {
                    vector3 = new Vector2(floatRect.Value.left, floatRect.Value.bottom);
                }
                Vector2 pos = vector2;
                SharedPhysics.CollisionResult collisionResult = SharedPhysics.TraceProjectileAgainstBodyChunks(this, room, vector, ref pos, 5f, 1, owner.player, hitAppendages: false);
                if (floatRect.HasValue && collisionResult.chunk != null)
                {
                    if (Vector2.Distance(vector, vector3) < Vector2.Distance(vector, collisionResult.collisionPoint))
                    {
                        collisionResult.chunk = null;
                    }
                    else
                    {
                        floatRect = null;
                    }
                }
                if (floatRect.HasValue)
                {
                    vector2 = vector3 - shootDir * num * 0.7f;
                    if (room.BeingViewed)
                    {
                        for (int j = 0; j < 6; j++)
                        {
                            if (UnityEngine.Random.value < Mathf.InverseLerp(0f, 0.5f, room.roomSettings.CeilingDrips))
                            {
                                room.AddObject(new WaterDrip(vector3, -shootDir * 8f + Custom.RNV() * 8f * UnityEngine.Random.value, waterColor: false));
                            }
                        }
                    }
                    if (modeCounter > 0 && Vector2.Dot(shootDir, new Vector2(floatRect.Value.right, floatRect.Value.top)) > Custom.LerpMap(modeCounter, 1f, 8f, 0.65f, 0.95f))
                    {
                        stuckInWallPos = vector2;
                        room.ScreenMovement(vector2, shootDir * 1.2f, 0.3f);
                        SwitchMode(Mode.StuckInWall);
                        room.PlaySound(SoundID.King_Vulture_Tusk_Stick_In_Terrain, vector2);
                        stuck = 1f;
                    }
                    else
                    {
                        room.ScreenMovement(vector2, shootDir * 0.75f, 0.25f);
                        if (floatRect.Value.right != 0f)
                        {
                            chunkPoints[0, 2].x = (Mathf.Abs(chunkPoints[0, 2].x) + 15f) * Mathf.Sign(floatRect.Value.right) * -1.5f;
                        }
                        if (floatRect.Value.top != 0f)
                        {
                            chunkPoints[0, 2].y = (Mathf.Abs(chunkPoints[0, 2].y) + 15f) * Mathf.Sign(floatRect.Value.top) * -1.5f;
                        }
                        Vector2 vector4 = Custom.RNV();
                        chunkPoints[0, 2] += vector4 * 10f;
                        chunkPoints[1, 2] -= vector4 * 10f;
                        SwitchMode(Mode.Dangling);
                        room.PlaySound(SoundID.King_Vulture_Tusk_Bounce_Off_Terrain, vector2);
                    }
                }
                else if (collisionResult.chunk != null)
                {
                    vector2 = collisionResult.collisionPoint - shootDir * num * 0.7f;
                    chunkPoints[0, 0] = vector2 - shootDir * num;
                    chunkPoints[1, 0] = vector2 - shootDir * (num + length);
                    if (room.BeingViewed)
                    {
                        for (int k = 0; k < 6; k++)
                        {
                            if (UnityEngine.Random.value < Mathf.InverseLerp(0f, 0.5f, room.roomSettings.CeilingDrips))
                            {
                                room.AddObject(new WaterDrip(collisionResult.collisionPoint, -shootDir * Mathf.Lerp(5f, 15f, UnityEngine.Random.value) + Custom.RNV() * UnityEngine.Random.value * 10f, waterColor: false));
                            }
                        }
                    }
                    SwitchMode(Mode.StuckInCreature);
                    room.PlaySound(SoundID.King_Vulture_Tusk_Impale_Creature, vector2);
                    impaleChunk = collisionResult.chunk;
                    impaleChunk.vel += shootDir * 12f / impaleChunk.mass;
                    impaleChunk.vel = Vector2.ClampMagnitude(impaleChunk.vel, 50f);
                    if (impaleChunk.owner is Creature)
                    {
                        (impaleChunk.owner as Creature).Violence(null, null, impaleChunk, null, Creature.DamageType.Stab, 1.5f, 0f);
                    }
                    shootDir = Vector3.Slerp(shootDir, Custom.DirVec(vector2, impaleChunk.pos), 0.4f);
                    if (impaleChunk.rotationChunk != null)
                    {
                        shootDir = Custom.RotateAroundOrigo(shootDir, 0f - Custom.AimFromOneVectorToAnother(impaleChunk.pos, impaleChunk.rotationChunk.pos));
                    }
                    if (impaleChunk.owner.graphicsModule != null)
                    {
                        impaleChunk.owner.graphicsModule.BringSpritesToFront();
                    }
                    return;
                }
                chunkPoints[0, 0] = vector2 - shootDir * num;
                chunkPoints[1, 0] = vector2 - shootDir * (num + length);
                if (room.PointSubmerged(chunkPoints[0, 0]))
                {
                    for (int l = 0; l < 8; l++)
                    {
                        Vector2 pos2 = Vector2.Lerp(vector, vector2, UnityEngine.Random.value);
                        if (room.PointSubmerged(pos2))
                        {
                            room.AddObject(new Bubble(pos2, shootDir * UnityEngine.Random.value * 30f + Custom.RNV() * UnityEngine.Random.value * 15f, bottomBubble: false, fakeWaterBubble: false));
                        }
                    }
                }
                elasticity = 0.2f;
                if (!(mode == Mode.ShootingOut))
                {
                    return;
                }
                float ropeLength = ((rope != null) ? rope.totalLength : Vector2.Distance(head.pos, chunkPoints[1, 0]));
                wireExtraSlack = Mathf.InverseLerp(shootRange * 0.8f, shootRange * 0.5f, ropeLength);
                if (wireExtraSlack < 1f)
                {
                    for (int m = 0; m < wire.GetLength(0); m++)
                    {
                        float num3 = Mathf.InverseLerp(0f, wire.GetLength(0), m);
                        wire[m, 2] += (Vector2.Lerp(head.pos, chunkPoints[1, 0], num3) - wire[m, 0]) * Mathf.Pow(1f - wireExtraSlack, 3f) / 5f;
                        wire[m, 0] += (Vector2.Lerp(head.pos, chunkPoints[1, 0], num3) - wire[m, 0]) * Mathf.Pow(1f - wireExtraSlack, 3f);
                        if (num3 > 0.6f)
                        {
                            wire[m, 2] = Vector2.Lerp(wire[m, 2], Custom.DirVec(wire[m, 0], head.pos) * 10f, Mathf.InverseLerp(0.6f, 1f, num3) * (1f - wireExtraSlack));
                        }
                    }
                }
                if (ropeLength > shootRange)
                {
                    SwitchMode(Mode.Dangling);
                    room.PlaySound(SoundID.King_Vulture_Tusk_Wire_End, vector2, Custom.LerpMap(ropeLength, shootRange, shootRange + 30f, 0.5f, 1f), 1f);
                    head.pos += Custom.DirVec(head.pos, chunkPoints[1, 0]) * 10f;
                    head.vel += Custom.DirVec(head.pos, chunkPoints[1, 0]) * 10f;
                    chunkPoints[0, 2] = shootDir * speed * 0.4f;
                    chunkPoints[1, 2] = shootDir * speed * 0.6f;
                    Vector2 vector5 = Custom.RNV();
                    chunkPoints[0, 0] += vector5 * 4f;
                    chunkPoints[0, 2] += vector5 * 6f;
                    chunkPoints[1, 0] -= vector5 * 4f;
                    chunkPoints[1, 2] -= vector5 * 6f;
                }
            }

            public void Update()
            {
                lastZRot = zRot;
                lastWireLoose = wireLoose;
                lastLaserAlpha = laserAlpha;
                zRot = Vector3.Slerp(zRot, Custom.DegToVec(owner.headRot + ((side % 2 == 0) ? (-90f) : 90f)), 0.9f * attached);
                Vector2 headToBody = Custom.DirVec((player.graphicsModule as PlayerGraphics).head.pos, player.bodyChunks[0].pos); //Custom.DirVec(player.neck.tChunks[player.neck.tChunks.Length - 1].pos, player.bodyChunks[1].pos);
                Vector2 perpBody = Custom.PerpendicularVector(headToBody);
                Vector2 rootPos = (player.graphicsModule as PlayerGraphics).head.pos + headToBody * 2f;//player.bodyChunks[1].pos + headToBody * -5f;
                rootPos += perpBody * zRot.x * (10f + 5f * Mathf.FloorToInt(side / 2f) * 2f);// 15f;
                rootPos += perpBody * zRot.y * ((side % 2 == 0) ? (-1f) : 1f) * (7f + 5f * Mathf.FloorToInt(side / 2f) * 2f);
                laserPower = Custom.LerpAndTick(laserPower, attached, 0.01f, 1f / 120f);
                bool isAnyTuskCharging = false;
                for (int i = 0; i < owner.tusks.Length; i++)
                {
                    if (i != side && (owner.tusks[i].mode == Mode.Charging || !player.Consious))
                        isAnyTuskCharging = true;
                }
                if (isAnyTuskCharging)
                {
                    laserAlpha = Mathf.Max(laserAlpha - 0.1f, 0f);
                }
                else if (UnityEngine.Random.value < 0.25f)
                {
                    laserAlpha = ((UnityEngine.Random.value < laserPower) ? Mathf.Lerp(laserAlpha, Mathf.Pow(laserPower, 0.25f), Mathf.Pow(UnityEngine.Random.value, 0.5f)) : (laserAlpha * UnityEngine.Random.value * UnityEngine.Random.value));
                }
                modeCounter++;
                if (mode != Mode.ShootingOut)
                {
                    wireExtraSlack = Mathf.Max(0f, wireExtraSlack - 1f / 30f);
                    elasticity = Mathf.Min(0.9f, elasticity + 0.025f);
                }
                if (mode == Mode.Attached)
                {
                    attached = 1f;
                }
                else if (mode == Mode.Charging)
                {
                    attached = Custom.LerpMap(modeCounter, 0f, 25f, 0.2f, 1f);
                    if (modeCounter >= preparationTime)//(owner.CloseQuarters ? 10 : 25)
                    {
                        if (modeCounter == preparationTime)
                        {
                            room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, head);
                            for (int j = 0; j < 6; j++)
                            {
                                if (UnityEngine.Random.value < Mathf.InverseLerp(0f, 0.5f, room.roomSettings.CeilingDrips))
                                {
                                    room.AddObject(new WaterDrip(rootPos, -shootDir * 8f + Custom.RNV() * 8f * UnityEngine.Random.value, waterColor: false));
                                }
                            }
                        }
                        if (player.Consious && (//owner.noShootDelay < 1 || //时间一到自动释放
                            !BuffInput.GetKey(BuffPlayerData.Instance.GetKeyBind(VultureShapedMutationBuffEntry.VultureShapedMutation))))
                        {
                            Shoot(rootPos);
                        }
                    }
                    if (modeCounter % 3 == 0)
                    {
                        room.PlaySound(SoundID.King_Vulture_Tusk_Aim_Beep, chunkPoints[0, 0], 1f, 2f);
                    }
                }
                else if (mode == Mode.ShootingOut)
                {
                    attached = 0f;
                    currWireLength = maxWireLength;
                    if (modeCounter > (room.PointSubmerged(chunkPoints[0, 0]) ? 6 : 10))
                    {
                        SwitchMode(Mode.Dangling);
                        room.PlaySound(SoundID.King_Vulture_Tusk_Wire_End, chunkPoints[0, 0], 0.4f, 1f);
                    }
                }
                else if (mode == Mode.Dangling)
                {
                    attached = 0f;
                    if (modeCounter > retractingTime)
                    {
                        SwitchMode(Mode.Retracting);
                    }
                }
                else if (mode == Mode.Retracting)
                {
                    if (currWireLength > 0f)
                    {
                        currWireLength = Mathf.Max(0f, currWireLength - maxWireLength * retractingSpeed);
                        attached = 0f;
                    }
                    else
                    {
                        float num = attached;
                        if (attached < 1f)
                        {
                            attached = Mathf.Min(1f, attached + 0.05f);
                        }
                        else
                        {
                            SwitchMode(Mode.Attached);
                        }
                        if (num < 0.5f && attached >= 0.5f)
                        {
                            room.PlaySound(SoundID.King_Vulture_Tusk_Reattach, chunkPoints[0, 0]);
                        }
                    }
                }
                else if (mode == Mode.StuckInCreature)
                {
                    attached = 0f;
                    if (modeCounter > 80)
                    {
                        currWireLength = Mathf.Max(100f, currWireLength - maxWireLength / 180f);
                    }
                    if (impaleChunk == null)
                    {
                        SwitchMode(Mode.Dangling);
                    }
                }
                else if (mode == Mode.StuckInWall)
                {
                    attached = 0f;
                    if (modeCounter > 240)
                    {
                        currWireLength = Mathf.Max(100f, currWireLength - maxWireLength / 180f);
                    }
                    if (!stuckInWallPos.HasValue || stuck <= 0f)
                    {
                        SwitchMode(Mode.Dangling);
                    }
                    else
                    {
                        for (int i = 0; i < chunkPoints.GetLength(0); i++)
                        {
                            chunkPoints[i, 1] = chunkPoints[i, 0];
                            chunkPoints[i, 2] *= 0f;
                        }
                        chunkPoints[0, 0] = stuckInWallPos.Value;
                        chunkPoints[1, 0] = stuckInWallPos.Value - shootDir * length;
                        if (rope != null && rope.totalLength >= currWireLength)
                        {
                            chunkPoints[1, 0] += Custom.DirVec(chunkPoints[1, 0], head.pos) * UnityEngine.Random.value * 10f * (1f - stuck);
                        }
                    }
                }
                Vector2 vector4 = headToBody;
                if (mode == Mode.Charging)
                {
                    vector4 = Vector3.Slerp(headToBody, AimDir(1f), Mathf.InverseLerp(0f, 25f, modeCounter));
                }
                if (!StuckOrShooting)
                {
                    Vector2 vector6;
                    for (int j = 0; j < chunkPoints.GetLength(0); j++)
                    {
                        chunkPoints[j, 1] = chunkPoints[j, 0];//lastPos
                        chunkPoints[j, 0] += chunkPoints[j, 2];//vel
                        if (room.PointSubmerged(chunkPoints[j, 0]))
                        {
                            chunkPoints[j, 2] *= 0.95f;
                            chunkPoints[j, 2].y += 0.1f;
                        }
                        else
                        {
                            chunkPoints[j, 2] *= 0.98f;
                            chunkPoints[j, 2].y -= 0.9f;
                        }
                        if (!FullyAttached && Custom.DistLess(chunkPoints[j, 0], chunkPoints[j, 1], 200f))
                        {
                            SharedPhysics.TerrainCollisionData cd = scratchTerrainCollisionData.Set(chunkPoints[j, 0], chunkPoints[j, 1], chunkPoints[j, 2], 2f, new IntVector2(0, 0), goThroughFloors: true);
                            cd = SharedPhysics.VerticalCollision(room, cd);
                            cd = SharedPhysics.HorizontalCollision(room, cd);
                            chunkPoints[j, 0] = cd.pos;
                            chunkPoints[j, 2] = cd.vel;
                            if ((float)cd.contactPoint.y != 0f)
                            {
                                chunkPoints[j, 2].x *= 0.5f;
                            }
                            if ((float)cd.contactPoint.x != 0f)
                            {
                                chunkPoints[j, 2].y *= 0.5f;
                            }
                        }
                        if (attached > 0f)
                        {
                            Vector2 vector5 = rootPos + vector4 * length * ((j == 0) ? 0.5f : (-0.5f));
                            float num2 = Mathf.Lerp(6f, 1f, attached);
                            if (!Custom.DistLess(chunkPoints[j, 0], vector5, num2))
                            {
                                vector6 = Custom.DirVec(chunkPoints[j, 0], vector5) * (Vector2.Distance(chunkPoints[j, 0], vector5) - num2);
                                chunkPoints[j, 0] += vector6;
                                chunkPoints[j, 2] += vector6;
                            }
                        }
                    }
                    vector6 = Custom.DirVec(chunkPoints[0, 0], chunkPoints[1, 0]) * (Vector2.Distance(chunkPoints[0, 0], chunkPoints[1, 0]) - length);
                    chunkPoints[0, 0] += vector6 / 2f;
                    chunkPoints[0, 2] += vector6 / 2f;
                    chunkPoints[1, 0] -= vector6 / 2f;
                    chunkPoints[1, 2] -= vector6 / 2f;
                }
                wireLoose = Custom.LerpAndTick(wireLoose, (attached > 0f) ? 0f : 1f, 0.07f, 1f / 30f);
                if (lastWireLoose == 0f && wireLoose == 0f)
                {
                    for (int k = 0; k < wire.GetLength(0); k++)
                    {
                        wire[k, 0] = head.pos + Custom.RNV();
                        wire[k, 1] = wire[k, 0];
                        wire[k, 0] *= 0f;
                    }
                }
                else
                {
                    float num3 = 1f;
                    if (rope != null)
                    {
                        num3 = rope.totalLength / (float)wire.GetLength(0) * 0.5f;
                    }
                    num3 *= wireLoose;
                    num3 += 10f * wireExtraSlack;
                    float num4 = Mathf.InverseLerp(currWireLength * 0.75f, currWireLength, (rope != null) ? rope.totalLength : Vector2.Distance(head.pos, chunkPoints[1, 0]));
                    num4 *= 1f - wireExtraSlack;
                    for (int l = 0; l < wire.GetLength(0); l++)
                    {
                        wire[l, 1] = wire[l, 0];
                        wire[l, 0] += wire[l, 2];
                        if (room.PointSubmerged(wire[l, 0]))
                        {
                            wire[l, 2] *= 0.7f;
                            wire[l, 2].y += 0.2f;
                        }
                        else
                        {
                            wire[l, 2] *= Mathf.Lerp(0.98f, 1f, wireExtraSlack);
                            wire[l, 2].y -= 0.9f * (1f - wireExtraSlack);
                        }
                        if (rope != null)
                        {
                            Vector2 vector7 = OnRopePos(Mathf.InverseLerp(0f, wire.GetLength(0) - 1, l));
                            wire[l, 2] += (vector7 - wire[l, 0]) * (1f - wireExtraSlack) / Mathf.Lerp(60f, 2f, num4);
                            wire[l, 0] += (vector7 - wire[l, 0]) * (1f - wireExtraSlack) / Mathf.Lerp(60f, 2f, num4);
                            wire[l, 0] = Vector2.Lerp(vector7, wire[l, 0], wireLoose);
                            if (wire[l, 3].x == 0f && wireLoose == 1f && Custom.DistLess(wire[l, 0], wire[l, 1], 500f))
                            {
                                SharedPhysics.TerrainCollisionData cd2 = scratchTerrainCollisionData.Set(wire[l, 0], wire[l, 1], wire[l, 2], 3f, new IntVector2(0, 0), goThroughFloors: true);
                                cd2 = SharedPhysics.VerticalCollision(room, cd2);
                                cd2 = SharedPhysics.HorizontalCollision(room, cd2);
                                wire[l, 0] = cd2.pos;
                                wire[l, 2] = cd2.vel;
                            }
                        }
                        wire[l, 3].x = 0f;
                    }
                    for (int m = 1; m < wire.GetLength(0); m++)
                    {
                        if (!Custom.DistLess(wire[m, 0], wire[m - 1, 0], num3))
                        {
                            Vector2 vector6 = Custom.DirVec(wire[m, 0], wire[m - 1, 0]) * (Vector2.Distance(wire[m, 0], wire[m - 1, 0]) - num3);
                            wire[m, 0] += vector6 / 2f;
                            wire[m, 2] += vector6 / 2f;
                            wire[m - 1, 0] -= vector6 / 2f;
                            wire[m - 1, 2] -= vector6 / 2f;
                        }
                    }
                    if (rope != null && wireLoose == 1f)
                    {
                        AlignWireToRopeSim();
                    }
                    Vector2 pos = (player.graphicsModule as PlayerGraphics).head.pos;//owner.player.neck.tChunks[owner.player.neck.tChunks.Length - 1].pos;
                    pos += perpBody * zRot.x * 15f;
                    pos += perpBody * zRot.y * ((side % 2 == 0) ? (-1f) : 1f) * 7f;
                    if (!Custom.DistLess(wire[0, 0], pos, num3))
                    {
                        Vector2 vector6 = Custom.DirVec(wire[0, 0], pos) * (Vector2.Distance(wire[0, 0], pos) - num3);
                        wire[0, 0] += vector6;
                        wire[0, 2] += vector6;
                    }
                    pos = WireAttachPos(1f);
                    if (!Custom.DistLess(wire[wire.GetLength(0) - 1, 0], pos, num3))
                    {
                        Vector2 vector6 = Custom.DirVec(wire[wire.GetLength(0) - 1, 0], pos) * (Vector2.Distance(wire[wire.GetLength(0) - 1, 0], pos) - num3);
                        wire[wire.GetLength(0) - 1, 0] += vector6;
                        wire[wire.GetLength(0) - 1, 2] += vector6;
                    }
                }
                if (mode == Mode.ShootingOut)
                {
                    ShootUpdate(Custom.LerpMap(modeCounter, 0f, 8f, 50f, 30f, 3f));
                }
                if (impaleChunk != null)
                {
                    if (!(impaleChunk.owner is Creature) || mode != Mode.StuckInCreature || (impaleChunk.owner as Creature).enteringShortCut.HasValue || (impaleChunk.owner as Creature).room != room)
                    {
                        impaleChunk = null;
                    }
                    else if (player.Consious && modeCounter > 20 && UnityEngine.Random.value < Custom.LerpMap(modeCounter, 20f, 80f, 0.0016666667f, 1f / 30f) && !owner.DoIWantToHoldCreature(impaleChunk.owner as Creature))
                    {
                        if (player.grasps[0] != null && player.grasps[0].grabbed == impaleChunk.owner)
                        {
                            currWireLength = 0f;
                            SwitchMode(Mode.Retracting);
                        }
                        else
                        {
                            SwitchMode(Mode.Dangling);
                        }
                        impaleChunk = null;
                    }
                    else
                    {
                        for (int n = 0; n < 2; n++)
                        {
                            chunkPoints[n, 1] = chunkPoints[n, 0];
                            chunkPoints[n, 2] *= 0f;
                        }
                        Vector2 vec = shootDir;
                        vec = ((impaleChunk.rotationChunk != null) ? Custom.RotateAroundOrigo(vec, Custom.AimFromOneVectorToAnother(impaleChunk.pos, impaleChunk.rotationChunk.pos)) : ((rope == null) ? Custom.DirVec(impaleChunk.pos, head.pos) : Custom.DirVec(impaleChunk.pos, rope.BConnect)));
                        chunkPoints[0, 0] = impaleChunk.pos - vec * impaleChunk.rad;
                        chunkPoints[1, 0] = impaleChunk.pos - vec * (impaleChunk.rad + length);
                        if (owner.vultureCat.behavior == VultureAI.Behavior.Hunt && 
                            player.grasps[0] == null && //player.AI.focusCreature != null && 
                            impaleChunk.owner is Creature// && (impaleChunk.owner as Creature).abstractCreature == player.AI.focusCreature.representedCreature
                            )
                        {
                            for (int num5 = 0; num5 < impaleChunk.owner.bodyChunks.Length; num5++)
                            {
                                if (Custom.DistLess(impaleChunk.owner.bodyChunks[num5].pos, player.bodyChunks[1].pos, impaleChunk.owner.bodyChunks[num5].rad + player.bodyChunks[1].rad))
                                {
                                    Custom.Log("grab impaled");
                                    player.Grab(impaleChunk.owner, 0, num5, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 1f, overrideEquallyDominant: true, pacifying: true);
                                    room.PlaySound(SoundID.Vulture_Grab_NPC, player.bodyChunks[1]);
                                    break;
                                }
                            }
                        }
                        if (UnityEngine.Random.value < 0.05f && impaleChunk.owner.grabbedBy.Count > 0)
                        {
                            for (int num6 = 0; num6 < impaleChunk.owner.grabbedBy.Count; num6++)
                            {
                                if (impaleChunk.owner.grabbedBy[num6].shareability != Creature.Grasp.Shareability.NonExclusive)
                                {
                                    SwitchMode(Mode.Dangling);
                                    impaleChunk = null;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (attached == 0f)
                {
                    Vector2 vector8 = ((impaleChunk != null) ? impaleChunk.pos : chunkPoints[1, 0]);
                    if (rope == null && room.VisualContact(head.pos, vector8))
                    {
                        rope = new Rope(room, head.pos, vector8, 1f);
                    }
                    if (rope != null)
                    {
                        rope.Update(head.pos, vector8);
                        if (rope.totalLength > currWireLength)
                        {
                            wireExtraSlack = Mathf.Max(0f, wireExtraSlack - 0.1f);
                            float num7 = stuck;
                            stuck -= Mathf.InverseLerp(30f, 90f, modeCounter) * UnityEngine.Random.value / Custom.LerpMap(rope.totalLength / currWireLength, 1f, 1.3f, 120f, 10f, 0.7f);
                            if (player.grasps[0] != null)
                            {
                                stuck -= 0.1f;
                            }
                            if (mode == Mode.StuckInWall && stuck <= 0f && num7 > 0f)
                            {
                                room.PlaySound(SoundID.King_Vulture_Tusk_Out_Of_Terrain, chunkPoints[0, 0], 1f, 1f);
                            }
                            float num8 = head.mass / (0.1f + head.mass);
                            float num9 = rope.totalLength - currWireLength;
                            if (mode == Mode.StuckInWall)
                            {
                                Vector2 vector6 = Custom.DirVec(head.pos, rope.AConnect) * num9;
                                head.pos += vector6 * elasticity;
                                head.vel += vector6 * elasticity;
                            }
                            else if (mode == Mode.StuckInCreature && impaleChunk != null)
                            {
                                num8 = head.mass / (impaleChunk.mass + head.mass);
                                Vector2 vector6 = Custom.DirVec(head.pos, rope.AConnect) * num9;
                                head.pos += vector6 * (1f - num8) * elasticity;
                                head.vel += vector6 * (1f - num8) * elasticity;
                                vector6 = Custom.DirVec(impaleChunk.pos, rope.BConnect) * num9;
                                impaleChunk.pos += vector6 * num8 * elasticity;
                                impaleChunk.vel += vector6 * num8 * elasticity;
                            }
                            else
                            {
                                Vector2 vector6 = Custom.DirVec(head.pos, rope.AConnect) * num9;
                                head.pos += vector6 * (1f - num8) * elasticity;
                                head.vel += vector6 * (1f - num8) * elasticity;
                                vector6 = Custom.DirVec(chunkPoints[1, 0], rope.BConnect) * num9;
                                chunkPoints[1, 0] += vector6 * num8 * elasticity;
                                chunkPoints[1, 2] += vector6 * num8 * elasticity;
                            }
                        }
                    }/*
                    if (StuckInAnything && !Custom.DistLess(head.pos, player.bodyChunks[0].pos, player.neck.idealLength * 0.75f))
                    {
                        Vector2 vector6 = Custom.DirVec(head.pos, player.bodyChunks[0].pos) * (Vector2.Distance(head.pos, player.bodyChunks[0].pos) - player.neck.idealLength * 0.75f);
                        float num10 = head.mass / (player.bodyChunks[0].mass + head.mass);
                        head.pos += vector6 * (1f - num10);
                        head.vel += vector6 * (1f - num10);
                        player.bodyChunks[0].pos -= vector6 * num10;
                        player.bodyChunks[0].vel -= vector6 * num10;
                    }*/
                    return;
                }
                if (rope != null)
                {
                    if (rope.visualizer != null)
                    {
                        rope.visualizer.ClearSprites();
                    }
                    rope = null;
                }
                for (int num11 = 0; num11 < wire.GetLength(0); num11++)
                {
                    wire[num11, 0] = head.pos + Custom.RNV();
                }
                if (VultureShapedMutationBuff.Instance.AerialFirepower)
                {
                    if (stuck > 0)
                    {
                        stuck = Mathf.Max(0, stuck - 0.1f);
                    }
                }
            }

            private Vector2 WireAttachPos(float timeStacker)
            {
                return Vector2.Lerp(chunkPoints[0, 1], chunkPoints[0, 0], timeStacker);
            }

            private void AlignWireToRopeSim()
            {
                if (rope.TotalPositions < 3)
                {
                    return;
                }
                float totalLength = rope.totalLength;
                float num = 0f;
                for (int i = 0; i < rope.TotalPositions; i++)
                {
                    if (i > 0)
                    {
                        num += Vector2.Distance(RopePos(i - 1), RopePos(i));
                    }
                    int num2 = Custom.IntClamp((int)(num / totalLength * (float)wire.GetLength(0)), 0, wire.GetLength(0) - 1);
                    wire[num2, 1] = wire[num2, 0];
                    wire[num2, 0] = RopePos(i);
                    wire[num2, 2] *= 0f;
                    wire[num2, 3].x = 1f;
                }
            }
            #region 绳子
            private Vector2 RopePos(int i)
            {
                if (i == rope.TotalPositions - 1)
                {
                    return WireAttachPos(1f);
                }
                return rope.GetPosition(i);
            }

            private float RopeFloatAtSegment(int segment)
            {
                float num = 0f;
                float num2 = 0f;
                for (int i = 0; i < rope.TotalPositions - 1; i++)
                {
                    if (i < segment)
                    {
                        num2 += Vector2.Distance(RopePos(i), RopePos(i + 1));
                    }
                    num += Vector2.Distance(RopePos(i), RopePos(i + 1));
                }
                return num2 / num;
            }

            public int RopePrevSegAtFloat(float fPos)
            {
                fPos *= rope.totalLength;
                float num = 0f;
                for (int i = 0; i < rope.TotalPositions - 1; i++)
                {
                    num += Vector2.Distance(RopePos(i), RopePos(i + 1));
                    if (num > fPos)
                    {
                        return i;
                    }
                }
                return rope.TotalPositions - 1;
            }

            public Vector2 OnRopePos(float fPos)
            {
                if (rope == null)
                {
                    return head.pos;
                }
                int num = RopePrevSegAtFloat(fPos);
                int num2 = Custom.IntClamp(num + 1, 0, rope.TotalPositions - 1);
                float t = Mathf.InverseLerp(RopeFloatAtSegment(num), RopeFloatAtSegment(num2), fPos);
                return Vector2.Lerp(RopePos(num), RopePos(num2), t);
            }
            #endregion
            #region 外观
            public void UpdateTuskColors(RoomCamera.SpriteLeaser sLeaser)
            {
                PlayerGraphics vultureGraphics = player.graphicsModule as PlayerGraphics;
                for (int i = 0; i < (sLeaser.sprites[TuskDetailSprite(vultureGraphics)] as TriangleMesh).verticeColors.Length; i++)
                {
                    float num = Mathf.InverseLerp(0f, (sLeaser.sprites[TuskSprite(vultureGraphics)] as TriangleMesh).verticeColors.Length - 1, i);
                    (sLeaser.sprites[TuskSprite(vultureGraphics)] as TriangleMesh).verticeColors[i] = 
                        Color.Lerp(Color.Lerp(armorColor, Color.white, Mathf.Pow(num, 2f)), owner.vultureCat.palette.blackColor, owner.vultureCat.darkness);
                    (sLeaser.sprites[TuskDetailSprite(vultureGraphics)] as TriangleMesh).verticeColors[i] = 
                        Color.Lerp(Color.Lerp(Color.Lerp(HSLColor.Lerp(owner.vultureCat.ColorA, owner.vultureCat.ColorB, num).rgb, owner.vultureCat.palette.blackColor, 0.65f - 0.4f * num), armorColor, Mathf.Pow(num, 2f)), owner.vultureCat.palette.blackColor, owner.vultureCat.darkness);
                }
            }

            public void InitiateSprites(PlayerGraphics vGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites[owner.vultureCat.NeckLumpSprite(side)] = new FSprite("Circle20");
                sLeaser.sprites[owner.vultureCat.NeckLumpSprite(side)].anchorY = 0f;
                sLeaser.sprites[LaserSprite(vGraphics)] = new CustomFSprite("Futile_White");
                sLeaser.sprites[LaserSprite(vGraphics)].shader = rCam.game.rainWorld.Shaders["HologramBehindTerrain"];
                sLeaser.sprites[TuskSprite(vGraphics)] = TriangleMesh.MakeLongMesh(tuskSegs, pointyTip: true, customColor: true);
                sLeaser.sprites[TuskDetailSprite(vGraphics)] = TriangleMesh.MakeLongMesh(tuskSegs, pointyTip: true, customColor: true);
                sLeaser.sprites[TuskDetailSprite(vGraphics)].shader = rCam.game.rainWorld.Shaders["KingTusk"];
                sLeaser.sprites[owner.vultureCat.TuskWireSprite(side)] = TriangleMesh.MakeLongMesh(wire.GetLength(0), pointyTip: false, customColor: true);
            }

            public void DrawSprites(PlayerGraphics vGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {/*
                if (vGraphics.shadowMode)
                {
                    camPos.y -= rCam.room.PixelHeight + 300f;
                }*/
                if (ModManager.MMF)
                {
                    UpdateTuskColors(sLeaser);
                }
                Vector2 bodyPos = Vector2.Lerp(player.bodyChunks[0].lastPos, player.bodyChunks[0].pos, timeStacker);
                Vector2 headToBody = Custom.DirVec(Vector2.Lerp((player.graphicsModule as PlayerGraphics).head.lastPos, (player.graphicsModule as PlayerGraphics).head.pos, timeStacker), 
                                                   Vector2.Lerp(player.bodyChunks[0].lastPos, player.bodyChunks[0].pos, timeStacker));//Custom.DirVec(Vector2.Lerp(player.neck.tChunks[player.neck.tChunks.Length - 1].lastPos, player.neck.tChunks[player.neck.tChunks.Length - 1].pos, timeStacker), Vector2.Lerp(player.bodyChunks[1].lastPos, player.bodyChunks[1].pos, timeStacker));
                Vector2 perpToBody = Custom.PerpendicularVector(headToBody);
                Vector2 nowZRot = Vector3.Slerp(lastZRot, zRot, timeStacker);
                float nowLaserAlpha = Mathf.Lerp(lastLaserAlpha, laserAlpha, timeStacker);
                Color color = Custom.HSL2RGB(owner.vultureCat.ColorB.hue, 1f, 0.5f);
                if (mode == Mode.Charging)
                {
                    nowLaserAlpha = ((modeCounter % 4 < 2) ? 1f : 0f);//((modeCounter % 6 < 3) ? 1f : 0f);
                    if (modeCounter % 2 == 0)
                        color = Color.Lerp(color, Color.white, UnityEngine.Random.value);
                    if (modeCounter >= preparationTime)
                    {
                        nowLaserAlpha = 1f;
                        color = Color.white;
                    }
                }
                float laserRootX = 7f;
                float laserRootY = -2f;//15f
                Vector2 laserRootPos = bodyPos + headToBody * laserRootY +
                                       perpToBody * laserRootX * Vector3.Slerp(Custom.DegToVec(owner.lastHeadRot + ((side % 2 == 0) ? (-90f) : 90f)), 
                                                                               Custom.DegToVec(owner.headRot + ((side % 2 == 0) ? (-90f) : 90f)), 
                                                                               timeStacker).x;
                Vector2 aimDir = AimDir(timeStacker);
                if (nowLaserAlpha <= 0f)
                {
                    sLeaser.sprites[LaserSprite(vGraphics)].isVisible = false;
                }
                else
                {
                    sLeaser.sprites[LaserSprite(vGraphics)].isVisible = true;
                    sLeaser.sprites[LaserSprite(vGraphics)].alpha = nowLaserAlpha;
                    Vector2 corner = Custom.RectCollision(laserRootPos, laserRootPos + aimDir * 100000f, rCam.room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);
                    IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(rCam.room, laserRootPos, corner);
                    if (intVector.HasValue)
                    {
                        corner = Custom.RectCollision(corner, laserRootPos, rCam.room.TileRect(intVector.Value)).GetCorner(FloatRect.CornerLabel.D);
                    }
                    (sLeaser.sprites[LaserSprite(vGraphics)] as CustomFSprite).verticeColors[0] = Custom.RGB2RGBA(color, nowLaserAlpha);
                    (sLeaser.sprites[LaserSprite(vGraphics)] as CustomFSprite).verticeColors[1] = Custom.RGB2RGBA(color, nowLaserAlpha);
                    (sLeaser.sprites[LaserSprite(vGraphics)] as CustomFSprite).verticeColors[2] = Custom.RGB2RGBA(color, Mathf.Pow(nowLaserAlpha, 2f) * ((mode == Mode.Charging) ? 1f : 0.5f));
                    (sLeaser.sprites[LaserSprite(vGraphics)] as CustomFSprite).verticeColors[3] = Custom.RGB2RGBA(color, Mathf.Pow(nowLaserAlpha, 2f) * ((mode == Mode.Charging) ? 1f : 0.5f));
                    (sLeaser.sprites[LaserSprite(vGraphics)] as CustomFSprite).MoveVertice(0, laserRootPos + aimDir * 2f + Custom.PerpendicularVector(aimDir) * 0.5f - camPos);
                    (sLeaser.sprites[LaserSprite(vGraphics)] as CustomFSprite).MoveVertice(1, laserRootPos + aimDir * 2f - Custom.PerpendicularVector(aimDir) * 0.5f - camPos);
                    (sLeaser.sprites[LaserSprite(vGraphics)] as CustomFSprite).MoveVertice(2, corner - Custom.PerpendicularVector(aimDir) * 0.5f - camPos);
                    (sLeaser.sprites[LaserSprite(vGraphics)] as CustomFSprite).MoveVertice(3, corner + Custom.PerpendicularVector(aimDir) * 0.5f - camPos);
                }
                Vector2 averageChunkPoints = (Vector2.Lerp(chunkPoints[0, 1], chunkPoints[0, 0], timeStacker) + Vector2.Lerp(chunkPoints[1, 1], chunkPoints[1, 0], timeStacker)) / 2f;
                Vector2 chunkPointsDir = Custom.DirVec(Vector2.Lerp(chunkPoints[1, 1], chunkPoints[1, 0], timeStacker), Vector2.Lerp(chunkPoints[0, 1], chunkPoints[0, 0], timeStacker));
                Vector2 perpChunkPoints = Custom.PerpendicularVector(chunkPointsDir);
                if (mode == Mode.Charging)
                {
                    averageChunkPoints += chunkPointsDir * Mathf.Lerp(-6f, 6f, UnityEngine.Random.value);
                }
                float neckLumpPosScaleX = 7f;
                float neckLumpPosScaleY = 7f + 3f * Mathf.FloorToInt(side / 2f) * 2f;//10f;
                Vector2 neckLumpPos = bodyPos - chunkPointsDir * neckLumpPosScaleX;
                Vector2 attachedPos = Vector2.Lerp(bodyPos, averageChunkPoints, Mathf.InverseLerp(0f, 0.25f, attached));
                sLeaser.sprites[owner.vultureCat.NeckLumpSprite(side)].x = neckLumpPos.x - camPos.x;
                sLeaser.sprites[owner.vultureCat.NeckLumpSprite(side)].y = neckLumpPos.y - camPos.y;
                sLeaser.sprites[owner.vultureCat.NeckLumpSprite(side)].scaleY = (Vector2.Distance(bodyPos - chunkPointsDir * neckLumpPosScaleY, attachedPos) + 4f) / 20f;//(Vector2.Distance(neckLumpPos, attachedPos) + 4f) / 20f;
                sLeaser.sprites[owner.vultureCat.NeckLumpSprite(side)].rotation = Custom.AimFromOneVectorToAnother(neckLumpPos, attachedPos);
                sLeaser.sprites[owner.vultureCat.NeckLumpSprite(side)].scaleX = 0.6f * neckLumpPosScaleX / 10f;

                //角矛
                float tuskRootPosScale = -17.5f;//-35f;
                float tuskRootPerpY = -7.5f;//-15f;
                float tuskRootLength = -15f;// -30f;
                float tuskTipLength = 30f;// 60f;
                float tuskPerpX = 10f;// 20f;
                float tuskPerpY = 5f;// 10f;
                Vector2 lastTuskSegPos = averageChunkPoints + chunkPointsDir * tuskRootPosScale + 
                                      perpChunkPoints * nowZRot.y * ((side % 2 == 0) ? (-1f) : 1f) * tuskRootPerpY;
                float lastTuskRad = 0f;
                for (int i = 0; i < tuskSegs; i++)
                {
                    float t = Mathf.InverseLerp(0f, tuskSegs - 1, i);
                    Vector2 tuskSegPos = averageChunkPoints + chunkPointsDir * Mathf.Lerp(tuskRootLength, tuskTipLength, t) + 
                                         TuskBend(t)     * perpChunkPoints * nowZRot.x * tuskPerpX + 
                                         TuskProfBend(t) * perpChunkPoints * nowZRot.y * tuskPerpY * ((side % 2 == 0) ? (-1f) : 1f);
                    Vector2 tuskDir = (tuskSegPos - lastTuskSegPos).normalized;
                    Vector2 perpTuskDir = Custom.PerpendicularVector(tuskDir);
                    float segDist = Vector2.Distance(tuskSegPos, lastTuskSegPos) / 5f;
                    float tuskRad = TuskRad(t, Mathf.Abs(nowZRot.y));
                    (sLeaser.sprites[TuskSprite(vGraphics)] as TriangleMesh).MoveVertice(i * 4, lastTuskSegPos - perpTuskDir * (tuskRad + lastTuskRad) * 0.5f + tuskDir * segDist - camPos);
                    (sLeaser.sprites[TuskSprite(vGraphics)] as TriangleMesh).MoveVertice(i * 4 + 1, lastTuskSegPos + perpTuskDir * (tuskRad + lastTuskRad) * 0.5f + tuskDir * segDist - camPos);
                    if (i == tuskSegs - 1)
                    {
                        (sLeaser.sprites[TuskSprite(vGraphics)] as TriangleMesh).MoveVertice(i * 4 + 2, tuskSegPos + tuskDir * segDist - camPos);
                    }
                    else
                    {
                        (sLeaser.sprites[TuskSprite(vGraphics)] as TriangleMesh).MoveVertice(i * 4 + 2, tuskSegPos - perpTuskDir * tuskRad - tuskDir * segDist - camPos);
                        (sLeaser.sprites[TuskSprite(vGraphics)] as TriangleMesh).MoveVertice(i * 4 + 3, tuskSegPos + perpTuskDir * tuskRad - tuskDir * segDist - camPos);
                    }
                    lastTuskRad = tuskRad;
                    lastTuskSegPos = tuskSegPos;
                }
                for (int j = 0; j < (sLeaser.sprites[TuskSprite(vGraphics)] as TriangleMesh).vertices.Length; j++)
                {
                    (sLeaser.sprites[TuskDetailSprite(vGraphics)] as TriangleMesh).MoveVertice(j, (sLeaser.sprites[TuskSprite(vGraphics)] as TriangleMesh).vertices[j]);
                }
                if (lastWireLoose > 0f || wireLoose > 0f)
                {
                    sLeaser.sprites[owner.vultureCat.TuskWireSprite(side)].isVisible = true;
                    float nowWireLoose = Mathf.Lerp(lastWireLoose, wireLoose, timeStacker);
                    float wireRootPosScale = 7f;// 14f;
                    Vector2 lastWirePos = bodyPos - headToBody * wireRootPosScale;
                    for (int k = 0; k < wire.GetLength(0); k++)
                    {
                        Vector2 wirePos = Vector2.Lerp(wire[k, 1], wire[k, 0], timeStacker);
                        if (nowWireLoose < 1f)
                        {
                            wirePos = Vector2.Lerp(Vector2.Lerp(bodyPos - headToBody * wireRootPosScale, averageChunkPoints + chunkPointsDir * 6f, Mathf.InverseLerp(0f, wire.GetLength(0) - 1, k)), 
                                                   wirePos, nowWireLoose);
                        }
                        if (k == wire.GetLength(0) - 1)
                        {
                            wirePos = WireAttachPos(timeStacker);
                        }
                        Vector2 wireDir = (wirePos - lastWirePos).normalized;
                        Vector2 perpWireDir = Custom.PerpendicularVector(wireDir);
                        float wireDist = Vector2.Distance(wirePos, lastWirePos) / 5f;
                        if (k == wire.GetLength(0) - 1)
                        {
                            wireDist = 0f;
                        }
                        (sLeaser.sprites[owner.vultureCat.TuskWireSprite(side)] as TriangleMesh).MoveVertice(k * 4, lastWirePos - perpWireDir + wireDir * wireDist - camPos);
                        (sLeaser.sprites[owner.vultureCat.TuskWireSprite(side)] as TriangleMesh).MoveVertice(k * 4 + 1, lastWirePos + perpWireDir + wireDir * wireDist - camPos);
                        (sLeaser.sprites[owner.vultureCat.TuskWireSprite(side)] as TriangleMesh).MoveVertice(k * 4 + 2, wirePos - perpWireDir - wireDir * wireDist - camPos);
                        (sLeaser.sprites[owner.vultureCat.TuskWireSprite(side)] as TriangleMesh).MoveVertice(k * 4 + 3, wirePos + perpWireDir - wireDir * wireDist - camPos);
                        lastWirePos = wirePos;
                    }
                }
                else
                {
                    sLeaser.sprites[owner.vultureCat.TuskWireSprite(side)].isVisible = false;
                }
            }

            public void AddToContainer(PlayerGraphics vGraphics, int spr, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                if (spr == LaserSprite(vGraphics))
                {
                    rCam.ReturnFContainer(ModManager.MMF ? "Midground" : "Foreground").AddChild(sLeaser.sprites[spr]);
                }
                else if (spr == owner.vultureCat.TuskWireSprite(side) || spr == TuskSprite(vGraphics) || spr == TuskDetailSprite(vGraphics))
                {
                    rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[spr]);
                    if(spr == owner.vultureCat.TuskWireSprite(side))
                        sLeaser.sprites[spr].MoveBehindOtherNode(sLeaser.sprites[3]);
                }
            }

            public void ApplyPalette(PlayerGraphics vGraphics, RoomPalette palette, Color armorColor, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                this.armorColor = armorColor;
                for (int i = 0; i < (sLeaser.sprites[TuskDetailSprite(vGraphics)] as TriangleMesh).verticeColors.Length; i++)
                {
                    float num = Mathf.InverseLerp(0f, (sLeaser.sprites[TuskSprite(vGraphics)] as TriangleMesh).verticeColors.Length - 1, i);
                    (sLeaser.sprites[TuskSprite(vGraphics)] as TriangleMesh).verticeColors[i] = Color.Lerp(armorColor, Color.white, Mathf.Pow(num, 2f));
                    (sLeaser.sprites[TuskDetailSprite(vGraphics)] as TriangleMesh).verticeColors[i] = Color.Lerp(Color.Lerp(HSLColor.Lerp(owner.vultureCat.ColorA, owner.vultureCat.ColorB, num).rgb, palette.blackColor, 0.65f - 0.4f * num), armorColor, Mathf.Pow(num, 2f));
                }
                (sLeaser.sprites[TuskDetailSprite(vGraphics)] as TriangleMesh).alpha = owner.patternDisplace;
                for (int j = 0; j < (sLeaser.sprites[owner.vultureCat.TuskWireSprite(side)] as TriangleMesh).verticeColors.Length; j++)
                {
                    (sLeaser.sprites[owner.vultureCat.TuskWireSprite(side)] as TriangleMesh).verticeColors[j] = Color.Lerp(palette.blackColor, palette.fogColor, 0.33f * Mathf.Sin(Mathf.InverseLerp(0f, (sLeaser.sprites[owner.vultureCat.TuskWireSprite(side)] as TriangleMesh).verticeColors.Length - 1, j) * (float)Math.PI));
                }
            }
            #endregion
            public bool HitThisObject(PhysicalObject obj)
            {
                if (obj != player)
                {
                    return obj is Creature;
                }
                return false;
            }

            public bool HitThisChunk(BodyChunk chunk)
            {
                return true;
            }

            //火箭助推角矛
            public static Vector2 TuskPos(Tusk tusk)
            {
                return (tusk.chunkPoints[0, 0] + tusk.chunkPoints[1, 0]) / 2f;
            }
        }

        private Player player;
        private VultureCat vultureCat;
        public Tusk[] tusks;
        private float headRot;
        private float lastHeadRot;
        public float eyesOut;
        public float lastEyesOut;
        public float eyesOutCycle;
        public float eyesHomeIn;
        public float lastEyesHome;
        private Vector2 preyPos;
        private Vector2 lastPreyPos;
        private Vector2 preyVelEstimate;
        private Smoke.NewVultureSmoke smoke;
        public int noShootDelay;
        public float patternDisplace;

        private Vector2 HeadRotVector => Custom.DegToVec(headRot);

        public bool ReadyToShoot
        {
            get
            {
                if (!tusks[0].ReadyToShoot)
                {
                    return tusks[1].ReadyToShoot;
                }
                return true;
            }
        }

        public bool AnyCreatureImpaled
        {
            get
            {
                if (tusks[0].impaleChunk == null)
                {
                    return tusks[1].impaleChunk != null;
                }
                return true;
            }
        }

        public bool CloseQuarters
        {
            get
            {
                if (player.room.aimap.getTerrainProximity(preyPos) != 1 || player.room.aimap.getTerrainProximity(player.bodyChunks[1].pos) >= 2)
                {
                    return player.room.aimap.getAItile(player.bodyChunks[1].pos).narrowSpace;
                }
                return true;
            }
        }

        public bool ThisCreatureImpaled(AbstractCreature crit)
        {
            for (int i = 0; i < tusks.Length; i++)
            {
                if (tusks[i].impaleChunk != null && tusks[i].impaleChunk.owner is Creature && (tusks[i].impaleChunk.owner as Creature).abstractCreature == crit)
                {
                    return true;
                }
            }
            return false;
        }

        internal KingTusks(Player player, VultureCat vultureCat)
        {
            this.player = player;
            this.vultureCat = vultureCat;
            this.tusks = new Tusk[vultureCat.TusksLength];
            for (int i = 0; i < tusks.Length; i++)
            {
                tusks[i] = new Tusk(this, i);
            }
        }

        public void NewRoom(Room newRoom)
        {
            for (int i = 0; i < tusks.Length; i++)
            {
                tusks[i].Reset(newRoom);
            }
            lastPreyPos = player.mainBodyChunk.pos;
            preyPos = player.mainBodyChunk.pos;
            smoke = null;
            preyVelEstimate *= 0f;
            noShootDelay = 220;
        }

        public void Update()
        {
            if (player.room == null)
            {
                return;
            }
            lastEyesOut = eyesOut;
            eyesOutCycle += 1f / 15f;
            eyesOut = (0.5f + 0.5f * Mathf.Sin(eyesOutCycle)) * (1f - eyesHomeIn);
            lastEyesHome = eyesHomeIn;
            lastPreyPos = preyPos;
            if (noShootDelay > 0)
            {
                noShootDelay--;
            }
            lastHeadRot = headRot;
            headRot = Custom.AimFromOneVectorToAnother((player.graphicsModule as PlayerGraphics).head.pos, player.bodyChunks[0].pos);// Custom.AimFromOneVectorToAnother(player.neck.tChunks[player.neck.tChunks.Length - 1].pos, player.bodyChunks[1].pos);
            headRot -= Custom.AimFromOneVectorToAnother(player.bodyChunks[0].pos, player.bodyChunks[1].pos);
            for (int i = 0; i < tusks.Length; i++)
            {
                tusks[i].Update();
            }
            if (smoke != null)
            {
                smoke.WindDrag(player.mainBodyChunk.pos, player.mainBodyChunk.vel, 30f);
                if (smoke.Dead || player.room != smoke.room)
                {
                    smoke = null;
                }
            }
            /*
            if (vultureCat.behavior == VultureAI.Behavior.Hunt && player.Consious)
            {
                if (player.AI.preyTracker.MostAttractivePrey != null && (player.AI.preyTracker.MostAttractivePrey.VisualContact || player.room.VisualContact(preyPos, player.bodyChunks[1].pos)))
                {
                    if (targetRep == player.AI.preyTracker.MostAttractivePrey)
                    {
                        eyesHomeIn = Mathf.Min(1f, eyesHomeIn + Mathf.InverseLerp(0.25f, 0.9f, Vector2.Dot(((player.graphicsModule as PlayerGraphics).head.pos - player.bodyChunks[0].pos).normalized, (player.bodyChunks[0].pos - preyPos).normalized)) / 40f);//Mathf.Min(1f, eyesHomeIn + Mathf.InverseLerp(0.25f, 0.9f, Vector2.Dot((player.neck.tChunks[player.neck.tChunks.Length - 1].pos - player.bodyChunks[1].pos).normalized, (player.bodyChunks[1].pos - preyPos).normalized)) / 40f);
                    }
                    else
                    {
                        eyesHomeIn = Mathf.Max(0f, eyesHomeIn - 0.2f);
                        if (eyesHomeIn == 0f && lastEyesHome == 0f)
                        {
                            targetRep = player.AI.preyTracker.MostAttractivePrey;
                        }
                    }
                }
                else
                {
                    eyesHomeIn = Mathf.Max(0f, eyesHomeIn - Custom.LerpMap((player.AI.preyTracker.MostAttractivePrey != null) ? player.AI.preyTracker.MostAttractivePrey.TicksSinceSeen : 120, 60f, 120f, 0f, 0.01f));
                }
            }
            else
            {
                eyesHomeIn = Custom.LerpAndTick(eyesHomeIn, 0f, 0.07f, 0.025f);
            }*/
            eyesHomeIn = Custom.LerpAndTick(eyesHomeIn, 0f, 0.07f, 0.025f);
            if (BuffInput.GetKey(BuffPlayerData.Instance.GetKeyBind(VultureShapedMutationBuffEntry.VultureShapedMutation)))
            {
                TryToShoot();
            }
            else
            {
                StopShoot();
            }
            VultureShapedMutationBuffEntry.UpdateSpeed = 1000;
            for (int i = 0; i < tusks.Length; i++)
            {
                if (tusks[i].mode == Tusk.Mode.Charging)
                {
                    //时缓
                    for (int j = 0; j < player.room.game.AlivePlayers.Count; j++)
                    {
                        if (player.room.game.AlivePlayers[j].realizedCreature != null &&
                             VultureShapedMutationBuffEntry.VultureCatFeatures.TryGetValue(player.room.game.AlivePlayers[j].realizedCreature as Player, out var vultureCat))
                        {
                            VultureShapedMutationBuffEntry.UpdateSpeed = 10;
                            break;
                        }
                    }
                }
            }
        }

        #region 外观
        public void InitiateSprites(PlayerGraphics vGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            for (int i = 0; i < tusks.Length; i++)
            {
                tusks[i].InitiateSprites(vGraphics, sLeaser, rCam);
            }
        }

        public void DrawSprites(PlayerGraphics vGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            for (int i = 0; i < tusks.Length; i++)
            {
                tusks[i].DrawSprites(vGraphics, sLeaser, rCam, timeStacker, camPos);
            }
        }

        public void ApplyPalette(PlayerGraphics vGraphics, RoomPalette palette, Color armorColor, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            for (int i = 0; i < tusks.Length; i++)
            {
                tusks[i].ApplyPalette(vGraphics, palette, armorColor, sLeaser, rCam);
            }
        }

        public void AddToContainer(PlayerGraphics vGraphics, int sprite, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            for (int i = 0; i < tusks.Length; i++)
            {
                tusks[i].AddToContainer(vGraphics, sprite, sLeaser, rCam, newContatiner);
            }
        }
        #endregion

        public bool DoIWantToHoldCreature(Creature creature)
        {
            if (player.grasps[0] != null)
            {
                return false;
            }
            if (vultureCat.behavior != VultureAI.Behavior.Hunt && vultureCat.behavior != VultureAI.Behavior.Idle)
            {
                return false;
            }
            return player.AI.DynamicRelationship(creature.abstractCreature).type == CreatureTemplate.Relationship.Type.Eats;
        }

        public void TryToShoot()
        {
            for (int i = 0; i < tusks.Length; i++)
            {
                if (tusks[i].mode == Tusk.Mode.Charging)
                {
                    //身体几乎不再移动
                    for (int j = 0; j < player.bodyChunks.Length; j++)
                    {
                        player.bodyChunks[j].vel *= 0.05f;
                    }
                    if (player.bodyMode == Player.BodyModeIndex.Stand)
                        player.bodyMode = Player.BodyModeIndex.Default;
                    else if (player.bodyMode == Player.BodyModeIndex.Swimming)
                        player.bodyMode = Player.BodyModeIndex.Default;
                    return;
                }
            }
            bool isAnyTuskReadyToShoot = false;
            for (int i = 0; i < tusks.Length; i++)
            {
                if (tusks[i].ReadyToShoot)
                {
                    isAnyTuskReadyToShoot = true;
                    break;
                }
            }
            if (isAnyTuskReadyToShoot)
            {
                int num = UnityEngine.Random.Range(0, tusks.Length);
                while (!tusks[num].ReadyToShoot)
                {
                    num = UnityEngine.Random.Range(0, tusks.Length);
                }
                if (tusks[num].ReadyToShoot)
                {
                    tusks[num].SwitchMode(Tusk.Mode.Charging);
                    player.room.PlaySound(SoundID.King_Vulture_Tusk_Aim, player.bodyChunks[1]);
                    if (!Custom.DistLess(player.bodyChunks[1].lastPos, player.bodyChunks[1].pos, 5f))
                    {
                        vultureCat.AirBrake(15);
                    }
                }
            }
        }

        public void StopShoot()
        {
            for (int i = 0; i < tusks.Length; i++)
            {
                if (tusks[i].mode == Tusk.Mode.Charging)
                {
                    player.room.PlaySound(SoundID.King_Vulture_Tusk_Cancel_Shot, tusks[i].chunkPoints[0, 0]);
                    tusks[i].SwitchMode(Tusk.Mode.Attached);
                    noShootDelay = Mathf.Max(noShootDelay, 10);
                    Custom.Log("cancel shot");
                }
            }
        }
    }
    #endregion
}
