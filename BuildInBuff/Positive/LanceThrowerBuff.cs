using Mono.Cecil.Cil;
using MonoMod.Cil;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RandomBuffUtils.ParticleSystem;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static BuiltinBuffs.Positive.LanceThrowerBuff;
using static BuiltinBuffs.Positive.StagnantForcefieldPlayerModule;
using static RandomBuffUtils.PlayerUtils;
using System.IO;
using System.Runtime.CompilerServices;
using RandomBuff.Core.SaveData.BuffConfig;
using RandomBuff.Core.SaveData;

namespace BuiltinBuffs.Positive
{
    internal class LanceThrowerBuff : Buff<LanceThrowerBuff, LanceThrowerBuffData>, IOWnPlayerUtilsPart
    {
        public override bool Triggerable => false;
        public LanceThrowerBuff()
        {
            AddPart(this);
        }

        public override void Destroy()
        {
            RemovePart(this);
        }

        public PlayerModulePart InitPart(PlayerModule module)
        {
            module.PlayerRef.TryGetTarget(out var player);
            BuffUtils.Log("LanceThrower", $"{player.playerState.playerNumber} - {LanceThrowerBuff.Instance.Data[player.playerState.playerNumber]}");
            return new LanceModule();
        }

        public PlayerModuleGraphicPart InitGraphicPart(PlayerModule module)
        {
            return null;
        }

        public class LanceModule : PlayerModulePart
        {
            public static int lanceRequirement = 60;
            public static int intoLanceMode = 15;
            public int lastLanceCounter;
            public int lanceCounter;
            public bool LanceReady => lanceCounter >= lanceRequirement;
            public bool LanceMode => lanceCounter > intoLanceMode;
            public bool LastLanceMode => lastLanceCounter > intoLanceMode;

            LanceEffect effect;
            public int lanceHand = 0;

            public override void Update(Player player, bool eu)
            {
                base.Update(player, eu);

                lastLanceCounter = lanceCounter;

                bool anyHandHoldSpear = false;

                if (player.grasps != null)
                {
                    for (int i = 0; i < player.grasps.Length; i++)
                    {
                        if (player.grasps[i] == null)
                            continue;
                        if (player.grasps[i].grabbed == null)
                            continue;
                        if (!(player.grasps[i].grabbed is Spear))
                            continue;
                        lanceHand = i;
                        anyHandHoldSpear = true;
                        break;
                    }
                }

                if (!anyHandHoldSpear)
                {
                    lanceCounter = 0;
                }
                else
                {
                    bool hold = LanceThrowerBuff.Instance.Data.UseThrowKey ? player.input[0].thrw : GetInput(player);
                    if ((player.bodyMode == Player.BodyModeIndex.Crawl || player.bodyMode == Player.BodyModeIndex.Stand || player.bodyMode == Player.BodyModeIndex.Default) && player.lowerBodyFramesOnGround > 0)
                    {
                        if (hold)
                        {
                            if (lanceCounter < lanceRequirement)
                                lanceCounter++;
                        }
                        else
                        {
                            if (LanceReady)
                            {
                                player.ThrowObject(lanceHand, false);
                            }
                        }
                    }
                    else if (lanceCounter > 0)
                    {
                        lanceCounter -= 5;

                        if (lanceCounter < 0)
                            lanceCounter = 0;
                    }
                }

                if (!LastLanceMode && LanceMode)
                {
                    player.slugcatStats.Modify(Multiply, "runspeedFac", 0.4f, this);
                }
                if (LastLanceMode && !LanceMode)
                {
                    player.slugcatStats.Undo(this);
                }

                if (lanceCounter > 0)
                {
                    if (effect == null && player.room != null)
                    {
                        player.room.AddObject(effect = new LanceEffect(player.room, player.grasps[lanceHand].grabbed.firstChunk.pos));
                    }
                    else if (effect != null)
                    {
                        if (player.room != null && player.grasps != null && player.grasps[lanceHand] != null)
                        {
                            effect.pos = player.grasps[lanceHand].grabbed.firstChunk.pos;
                            effect.holdStrength = (lanceCounter - intoLanceMode) / (float)(lanceRequirement - intoLanceMode);
                            effect.lanceReady = LanceReady;

                            if (lastLanceCounter != lanceCounter && LanceReady)
                            {
                                effect.Burst();
                            }
                        }
                        else
                        {
                            effect.Destroy();
                            effect = null;
                            return;
                        }


                        if (effect.room != player.room)
                        {
                            effect.Destroy();
                            effect = null;
                        }
                    }
                }
                else
                {
                    if (effect != null)
                    {
                        effect.Destroy();
                        effect = null;
                    }
                }
            }

            public bool GetInput(Player player)
            {
                if (LanceThrowerBuff.Instance.Data[player.playerState.playerNumber] != KeyCode.None)
                    return Input.GetKey(LanceThrowerBuff.Instance.Data[player.playerState.playerNumber]);
                if (BuffPlayerData.Instance.GetKeyBind(LanceThrowerBuffEntry.lanceThrowerBuffID) != KeyCode.None.ToString())
                    return BuffInput.GetKey(BuffPlayerData.Instance.GetKeyBind(LanceThrowerBuffEntry.lanceThrowerBuffID));
                return false;
            }

            public void LanceThrow(Player player, Spear spear)
            {
                if (effect != null)
                {
                    effect.LanceThrow(spear);
                    effect = null;
                }
                lanceCounter = 0;
                player.slugcatStats.Undo(this);
            }

            public override void Destroy()
            {
                if (effect != null)
                {
                    effect.Destroy();
                    effect = null;
                }
                UndoAll(this);
                base.Destroy();
            }

            public void LanceThrow()
            {
                lanceCounter = 0;
            }
        }

        public override BuffID ID => LanceThrowerBuffEntry.lanceThrowerBuffID;
    }

    internal class LanceThrowerBuffData : BuffData
    {
        public override BuffID ID => LanceThrowerBuffEntry.lanceThrowerBuffID;

        [CustomBuffConfigInfo("UseThrow","")]
        [CustomBuffConfigTwoValue(true, false)]
        public bool UseThrowKey { get; set; }

        [CustomBuffConfigInfo("Player1", "bind key for player 1")]
        [CustomBuffConfigEnum(typeof(KeyCode), "None")]
        public KeyCode Player1 { get; set; }

        [CustomBuffConfigInfo("Player2", "bind key for player 2")]
        [CustomBuffConfigEnum(typeof(KeyCode), "None")]
        public KeyCode Player2 { get; set; }

        [CustomBuffConfigInfo("Player3", "bind key for player 3")]
        [CustomBuffConfigEnum(typeof(KeyCode), "None")]
        public KeyCode Player3 { get; set; }

        [CustomBuffConfigInfo("Player4", "bind key for player 4")]
        [CustomBuffConfigEnum(typeof(KeyCode), "None")]
        public KeyCode Player4 { get; set; }

        public KeyCode this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Player1;
                    case 1: return Player2;
                    case 2: return Player3;
                    case 3: return Player4;
                    default:
                        return KeyCode.None;
                }
            }
        }
    }

    internal class LanceThrowerBuffEntry : IBuffEntry
    {
        public static BuffID lanceThrowerBuffID = new BuffID("LanceThrower", true);
        public static string lanceThrowerVFX0;

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<LanceThrowerBuff, LanceThrowerBuffData, LanceThrowerBuffEntry>(lanceThrowerBuffID);
        }

        public static void HookOn()
        {
            IL.Player.GrabUpdate += Player_GrabUpdate;
            IL.Lizard.Violence += Lizard_Violence;
            IL.Lizard.SpearStick += Lizard_SpearStick;
            On.SlugcatHand.Update += SlugcatHand_Update;
            On.Player.GraphicsModuleUpdated += Player_GraphicsModuleUpdated;
            On.Player.ThrownSpear += Player_ThrownSpear;
            On.Spear.HitSomething += Spear_HitSomething;
            On.Spear.LodgeInCreature_CollisionResult_bool_bool += Spear_LodgeInCreature_CollisionResult_bool_bool;
        }

        private static void Lizard_SpearStick(ILContext il)
        {
            ILCursor c1 = new ILCursor(il);

            if (c1.TryGotoNext(MoveType.After,
                (i) => i.MatchLdarg(0),
                (i) => i.Match(OpCodes.Ldarg_S),
                (i) => i.MatchCall<Lizard>("HitHeadShield")))
            {
                c1.Emit(OpCodes.Ldarg_1);
                c1.EmitDelegate<Func<bool, Weapon, bool>>((orig, weapon) =>
                {
                    bool result = orig;
                    if (orig && weapon is Spear spear && LanceEffect.spear2EffectMapper.TryGetValue(spear, out var effect))
                    {
                        effect.Burst(-spear.firstChunk.vel, false);
                        result = false;
                    }

                    return result;
                });
            }
            else
                BuffUtils.Log("LanceThrower", "Lizard_SpearStick c1 failed");
        }

        private static void Lizard_Violence(ILContext il)
        {
            ILCursor c1 = new ILCursor(il);

            if (c1.TryGotoNext(MoveType.After,
                (i) => i.MatchLdarg(0),
                (i) => i.Match(OpCodes.Ldarga_S),
                (i) => i.Match(OpCodes.Call),
                (i) => i.MatchCall<Lizard>("HitHeadShield")))
            {
                c1.Emit(OpCodes.Ldarg_1);
                c1.EmitDelegate<Func<bool, BodyChunk, bool>>((orig, source) =>
                {
                    bool result = orig;
                    if (result && source != null && source.owner is Spear spear && LanceEffect.spear2EffectMapper.TryGetValue(spear, out var effect))
                    {
                        effect.Burst(-spear.firstChunk.vel, false);
                    }

                    return result;
                });
            }
            else
                BuffUtils.Log("LanceThrower", "Lizard_Violence c1 failed");

            if (c1.TryGotoNext(MoveType.After,
                (i) => i.MatchLdloc(0),
                (i) => i.MatchLdcR4(0.1f),
                (i) => i.MatchMul(),
                (i) => i.MatchStloc(0)))
            {
                c1.Emit(OpCodes.Ldloc_0);
                c1.Emit(OpCodes.Ldarg_1);
                c1.EmitDelegate<Func<float, BodyChunk, float>>((orig, source) =>
                {
                    float result = orig;
                    if (source != null && source.owner is Spear spear && LanceEffect.spear2EffectMapper.TryGetValue(spear, out var effect))
                    {
                        result *= 3f;
                    }

                    return result;
                });
                c1.Emit(OpCodes.Stloc_0);
                return;
            }
            else
                BuffUtils.Log("LanceThrower", "Lizard_Violence c1 2 failed");
        }

        private static void Spear_LodgeInCreature_CollisionResult_bool_bool(On.Spear.orig_LodgeInCreature_CollisionResult_bool_bool orig, Spear self, SharedPhysics.CollisionResult result, bool eu, bool isJellyFish)
        {
            if (LanceEffect.spear2EffectMapper.TryGetValue(self, out _) && result.onAppendagePos != null)
            {
                return;
            }
            orig.Invoke(self, result, eu, isJellyFish);
        }

        private static bool Spear_HitSomething(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
        {
            bool re = orig.Invoke(self, result, eu);
            if (re)
            {
                foreach (var effect in self.room.updateList.OfType<LanceEffect>().ToArray())
                {
                    if (effect.bindSpear == self)
                    {
                        effect.Burst(-self.firstChunk.vel, false);
                        if (result.onAppendagePos != null)
                            re = false;
                    }
                }
            }
            return re;
        }

        private static void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig.Invoke(self, spear);
            if (TryGetModulePart<LanceModule>(self, LanceThrowerBuff.Instance, out var module) && module.LanceReady)
            {
                module.LanceThrow(self, spear);
                spear.firstChunk.vel += spear.throwDir.ToVector2() * 80f;
                spear.spearDamageBonus *= 3.5f;
            }
        }

        public static void LoadAssets()
        {
            lanceThrowerVFX0 = Futile.atlasManager.LoadImage(lanceThrowerBuffID.GetStaticData().AssetPath + Path.DirectorySeparatorChar + "lancethrowerspark").elements[0].name;
        }

        private static void Player_GraphicsModuleUpdated(On.Player.orig_GraphicsModuleUpdated orig, Player self, bool actuallyViewed, bool eu)
        {
            orig.Invoke(self, actuallyViewed, eu);

            if (!actuallyViewed)
                return;
            if (!(self.bodyMode == Player.BodyModeIndex.Stand))
                return;
            if (self.grasps == null)
                return;
            for (int i = 0; i < self.grasps.Length; i++)
            {
                if (self.grasps[i] == null)
                    continue;
                if (self.grasps[i].grabbed == null)
                    continue;
                if (!(self.grasps[i].grabbed is Spear spear))
                    continue;

                if (TryGetModulePart<LanceModule>(self, LanceThrowerBuff.Instance, out var module) && module.LanceMode && module.lanceHand == i)
                {
                    spear.setRotation = Custom.DegToVec((self.graphicsModule as PlayerGraphics).spearDir > 0 ? 90f : -90f);
                    spear.rotationSpeed = 0f;
                }
            }
        }

        private static void SlugcatHand_Update(On.SlugcatHand.orig_Update orig, SlugcatHand self)
        {
            orig.Invoke(self);

            var player = self.owner.owner as Player;
            var graphic = player.graphicsModule as PlayerGraphics;

            if (player.grasps[self.limbNumber] == null)
                return;

            if (player.grasps[self.limbNumber].grabbed == null)
                return;

            if (!(player.grasps[self.limbNumber].grabbed is Spear spear))
                return;

            if (TryGetModulePart<LanceModule>(self.owner.owner as Player, LanceThrowerBuff.Instance, out var module))
            {
                if (module.LanceMode && module.lanceHand == self.limbNumber)
                {
                    self.mode = Limb.Mode.HuntRelativePosition;

                    self.relativeHuntPos.x = -20f + 40f * self.limbNumber;
                    self.relativeHuntPos.y = 12f;

                    self.relativeHuntPos.x = self.relativeHuntPos.x * (1f - Mathf.Sin((self.owner.owner as Player).switchHandsProcess * 3.1415927f));

                    Vector2 b = Custom.DegToVec(180f + (self.limbNumber == 0 ? -1f : 1f) * 8f + (self.owner.owner as Player).input[0].x * 4f) * 12f;
                    b.y += Mathf.Sin(player.animationFrame / 6f * 2f * 3.1415927f) * 2f;
                    b.x -= Mathf.Cos((player.animationFrame + (player.leftFoot ? 0 : 6)) / 12f * 2f * 3.1415927f) * 4f * player.input[0].x;
                    b.x += (self.owner.owner as Player).input[0].x * 2f;
                    self.relativeHuntPos = Vector2.Lerp(self.relativeHuntPos, b, Mathf.Abs((self.owner as PlayerGraphics).spearDir));
                    spear.ChangeOverlap(graphic.spearDir > -0.4f && self.limbNumber == 0 || graphic.spearDir < 0.4f && self.limbNumber == 1);
                }
            }
        }

        static bool throw0;
        private static void Player_GrabUpdate(ILContext il)
        {
            ILCursor c1 = new ILCursor(il);

            if (c1.TryGotoNext(MoveType.After,
                (i) => i.MatchLdarg(0),
                (i) => i.MatchCall<Player>("get_input"),
                (i) => i.MatchLdcI4(1),
                (i) => i.MatchLdelema<Player.InputPackage>(),
                (i) => i.MatchLdfld<Player.InputPackage>("thrw"),
                (i) => i.MatchBrtrue(out _)
                ))
            {
            }

            if (c1.TryGotoPrev(MoveType.After,
                (i) => i.MatchLdarg(0),
                (i) => i.MatchCall<Player>("get_input"),
                (i) => i.MatchLdcI4(0),
                (i) => i.MatchLdelema<Player.InputPackage>(),
                (i) => i.MatchLdfld<Player.InputPackage>("thrw"),
                (i) => i.MatchBrfalse(out _)
                ))
            {
                c1.Index--;
                c1.Emit(OpCodes.Ldarg_0);
                c1.EmitDelegate<Func<bool, Player, bool>>((orig, self) =>
                {
                    //if (PlayerUtils.TryGetModulePart<LanceModule>(self, LanceThrowerBuff.Instance, out var module))
                    //{
                    //    module.TryLanceStamina(self);
                    //}

                    return LanceThrowerBuff.Instance.Data.UseThrowKey ? true : orig;
                });
            }
            else
            {
                BuffUtils.Log("LanceThrower", "c1 failed 1");
                return;
            }

            if (c1.TryGotoNext(MoveType.After,
                (i) => i.MatchLdarg(0),
                (i) => i.MatchCall<Player>("get_input"),
                (i) => i.MatchLdcI4(1),
                (i) => i.MatchLdelema<Player.InputPackage>(),
                (i) => i.MatchLdfld<Player.InputPackage>("thrw"),
                (i) => i.MatchBrtrue(out _)
                ))
            {
                c1.Index--;
                c1.Emit(OpCodes.Ldarg_0);
                c1.EmitDelegate<Func<bool, Player, bool>>((orig, self) =>
                {
                    return LanceThrowerBuff.Instance.Data.UseThrowKey ? !(!self.input[0].thrw && self.input[1].thrw) : orig;
                });
            }
            else
            {
                BuffUtils.Log("LanceThrower", "c1 failed 2");
                return;
            }
        }
    }

    internal class LanceEffect : CosmeticSprite
    {
        public static ConditionalWeakTable<Spear, LanceEffect> spear2EffectMapper = new ConditionalWeakTable<Spear, LanceEffect>();

        static Color gold = Custom.hexToColor("FFC400");
        static Color goldAlphaZero = gold.CloneWithNewAlpha(0f);
        static float rad = 60f;
        static int tailPosCount = 10;

        public float lastHoldStrength;
        public float holdStrength;
        public bool burst;
        public bool lanceReady;

        float lastBurstParam;
        float burstParam;

        float lastAlpha = 1f;
        float alpha = 1f;

        int fadeCounter;

        public Spear bindSpear;

        List<Vector2> tailPosList = new List<Vector2>();
        public LanceEffect(Room room, Vector2 pos)
        {
            this.room = room;
            this.pos = lastPos = pos;

            for (int i = 0; i < tailPosCount; i++)
                tailPosList.Add(pos);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            lastPos = pos;
            lastBurstParam = burstParam;
            lastAlpha = alpha;
            burstParam = Mathf.Lerp(burstParam, 0f, 0.15f);
            lastHoldStrength = holdStrength;

            tailPosList.Insert(0, pos);
            if (tailPosList.Count > tailPosCount)
                tailPosList.RemoveAt(tailPosCount);

            if (bindSpear != null)
            {
                pos = bindSpear.firstChunk.pos;
                if (bindSpear.mode != Weapon.Mode.Thrown)
                    OnSpearStoped();
                if (bindSpear.slatedForDeletetion)
                    Destroy();
            }

            if (fadeCounter > 0)
            {
                fadeCounter--;
                alpha = fadeCounter / 40f;
                if (fadeCounter == 0)
                    Destroy();
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[3];
            sLeaser.sprites[0] = new FSprite(FlameThrowerBuffEntry.flameVFX1)
            {
                shader = rCam.game.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"],
                color = gold,
                scale = 1f,
                alpha = 1f
            };
            sLeaser.sprites[1] = new FSprite(LanceThrowerBuffEntry.lanceThrowerVFX0)
            {
                shader = rCam.game.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"],
                color = gold,
                alpha = 0f,
                scale = 0f,
            };
            sLeaser.sprites[2] = new FSprite(FlameThrowerBuffEntry.flameVFX1)
            {
                shader = rCam.game.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"],
                color = gold,
                scale = 0f,
                alpha = 0f
            };

            sLeaser.sprites[2] = TriangleMesh.MakeLongMesh(tailPosCount - 1, false, true, FlameThrowerBuffEntry.flameVFX1);
            sLeaser.sprites[2].shader = rCam.game.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"];

            AddToContainer(sLeaser, rCam, null);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Bloom");
            }

            foreach (FSprite fSprite in sLeaser.sprites)
            {
                fSprite.RemoveFromContainer();
                newContatiner.AddChild(fSprite);
            }
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            if (slatedForDeletetion || sLeaser.deleteMeNextFrame)
                return;

            float smoothStrength = Mathf.Lerp(lastHoldStrength, holdStrength, timeStacker);
            float smoothBurst = Mathf.Lerp(lastBurstParam, burstParam, timeStacker);
            float smoothAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);

            sLeaser.sprites[0].SetPosition(smoothPos - camPos);
            sLeaser.sprites[0].scale = Mathf.Lerp(6f, 1f, smoothStrength + smoothBurst * 1f);
            sLeaser.sprites[0].alpha = smoothStrength * smoothAlpha;

            sLeaser.sprites[1].SetPosition(smoothPos - camPos);
            sLeaser.sprites[1].scale = Mathf.Sin(smoothBurst * Mathf.PI) * 1.5f;
            sLeaser.sprites[1].alpha = smoothBurst * smoothAlpha;

            sLeaser.sprites[2].isVisible = bindSpear != null;

            if (!sLeaser.sprites[2].isVisible)
                return;

            var triangleMesh = sLeaser.sprites[2] as TriangleMesh;
            Vector2 vector = smoothPos;
            for (int i = 0; i < tailPosCount - 1; i++)
            {
                float width = 2.5f * (1f - i / (float)tailPosCount);

                Vector2 smoothPos1 = GetSmoothPos(i, timeStacker);
                Vector2 smoothPos2 = GetSmoothPos(i + 1, timeStacker);
                Vector2 v2 = (vector - smoothPos1).normalized;
                Vector2 v3 = Custom.PerpendicularVector(v2);
                v2 *= Vector2.Distance(vector, smoothPos2) / 5f;
                triangleMesh.MoveVertice(i * 4, smoothPos - v3 * width - v2 - camPos);
                triangleMesh.MoveVertice(i * 4 + 1, vector + v3 * width - v2 - camPos);
                triangleMesh.MoveVertice(i * 4 + 2, smoothPos1 - v3 * width + v2 - camPos);
                triangleMesh.MoveVertice(i * 4 + 3, smoothPos1 + v3 * width + v2 - camPos);

                for (int j = 0; j < 4; j++)
                {
                    triangleMesh.verticeColors[i * 4 + j] = Color.Lerp(gold, goldAlphaZero, i / (float)tailPosCount);
                    triangleMesh.verticeColors[i * 4 + j].a *= smoothAlpha;
                }

                vector = smoothPos1;
            }
        }


        public void OnSpearStoped()
        {
            if (fadeCounter == 0)
                fadeCounter = 40;
        }

        public override void Destroy()
        {
            if (bindSpear != null)
            {
                spear2EffectMapper.Remove(bindSpear);
                bindSpear = null;
            }

            base.Destroy();
        }

        public void Burst(Vector2? movement = null, bool resetTail = true)
        {
            burstParam = 1f;
            CreateSparkleEmitter(room, movement ?? Vector2.zero);

            if (resetTail)
            {
                tailPosList.Clear();
                for (int i = 0; i < tailPosCount; i++)
                    tailPosList.Add(pos);
            }
        }

        public void LanceThrow(Spear spear)
        {
            bindSpear = spear;
            spear2EffectMapper.Add(bindSpear, this);
        }

        Vector2 GetSmoothPos(int i, float timeStacker)
        {
            return Vector2.Lerp(GetPos(i + 1), GetPos(i), timeStacker);
        }

        Vector2 GetPos(int i)
        {
            return tailPosList[Custom.IntClamp(i, 0, tailPosList.Count - 1)];
        }

        public void CreateSparkleEmitter(Room room, Vector2 movement)
        {
            this.room = room;
            room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, pos, 1f, 2f);

            var emitter = new ParticleEmitter(room);
            emitter.pos = emitter.lastPos = pos;

            emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 5, false));
            emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, 10));

            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight", alpha: 0.5f)));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("pixel", "", constCol: Color.white)));
            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 40, 50));
            emitter.ApplyParticleModule(new SetConstColor(emitter, gold));
            emitter.ApplyParticleModule(new SetRandomScale(emitter, 2f, 2.5f));
            emitter.ApplyParticleModule(new SetAlpha(emitter, 1f));
            emitter.ApplyParticleModule(new SetRandomPos(emitter, 0f));

            emitter.ApplyParticleModule(new PositionOverLife(emitter,
                (p, l) =>
                {
                    Vector2 dir = Custom.DegToVec(p.randomParam1 * 360f);
                    float radParam = p.randomParam2;
                    return (dir * StagnantForcefieldBuff.rad * radParam + movement) * Mathf.Min(1f, Helper.LerpEase(l)) + p.emitter.pos;
                }));

            emitter.ApplyParticleModule(new AlphaOverLife(emitter,
                (p, l) =>
                {
                    return 1f - Helper.LerpEase(l);
                }));
            //emitter.ApplyParticleModule(new StagnantForceFieldBlink(emitter));

            ParticleSystem.ApplyEmitterAndInit(emitter);
        }
    }
}
