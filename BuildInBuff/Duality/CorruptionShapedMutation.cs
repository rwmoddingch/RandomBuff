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
using System.Reflection;
using MonoMod.RuntimeDetour;
using RandomBuff.Core.SaveData.BuffConfig;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.ConstrainedExecution;
using static DaddyGraphics;

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

        public static int speedLevel;

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


        public BlindWaveEffectManager blindWaveEffectManager;
        FSprite blindWaveTex;
        bool containerAdded;


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
            speedLevel = SpeedLevel;

            if (blindWaveEffectManager == null && !CorruptionShapedMutationBuff.Instance.Data.HavingVisionKey)
            {
                blindWaveTex = new FSprite("pixel")
                {
                    shader = Custom.rainWorld.Shaders["BlindWave"],
                    scale = 1600f,
                    anchorX = 0f,
                    anchorY = 0f,
                };
                blindWaveEffectManager = new BlindWaveEffectManager();
            }

            if (blindWaveEffectManager != null)
            {
                if (!containerAdded)
                {
                    game.cameras[0].ReturnFContainer("Bloom").AddChild(blindWaveTex);
                    containerAdded = true;
                    blindWaveEffectManager.EffectSprite = blindWaveTex;
                }

                if (game.cameras != null && game.cameras[0] != null && game.cameras[0].room != null)
                {
                    if (!blindWaveTex._isOnStage)
                        containerAdded = false;
                    var tile = game.cameras[0].room.GetTilePosition(new Vector2(Futile.mousePosition.x, Futile.mousePosition.y) + game.cameras[0].pos);
                    //test.SetPosition(game.cameras[0].room.MiddleOfTile(tile) - game.cameras[0].pos);
                    blindWaveTex.MoveToFront();
                }

                blindWaveEffectManager.Update(game);
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            if (blindWaveEffectManager != null)
            {
                blindWaveTex.RemoveFromContainer();
                blindWaveEffectManager.Destroy();
            }
        }
    }

    #region BlindWaveManager
    internal class BlindWaveEffectManager
    {
        Vector4[] staticPos = new Vector4[40];
        Vector4[] soundWaveInfos = new Vector4[50];

        int legalStaticCount;
        public int legalSoundCount;

        public List<SoundObject> activeWaveObjs = new List<SoundObject>();
        List<SoundObject> wavesToAdd = new List<SoundObject>();
        List<Func<SoundObject>> soundsToAdd = new List<Func<SoundObject>>();

        public FSprite EffectSprite;
        int mapRevealWaveCD;
        int mapRevealCounter;

        Dictionary<SoundID, int> rateLimit = new Dictionary<SoundID, int>();

        public void Update(RainWorldGame game)
        {
            foreach (var obj in soundsToAdd)
            {
                activeWaveObjs.Add(obj.Invoke());
            }
            soundsToAdd.Clear();

            foreach (var obj in wavesToAdd)
            {
                activeWaveObjs.Add(obj);
            }
            wavesToAdd.Clear();

            bool anyPlayerHoldMap = false;
            foreach (var player in game.Players)
            {
                if (player.realizedCreature != null && player.realizedCreature.room != null)
                {
                    if ((player.realizedCreature as Player).RevealMap)
                    {
                        anyPlayerHoldMap = true;
                    }
                }
            }
            if (anyPlayerHoldMap && mapRevealCounter < 30)
                mapRevealCounter += ((ModManager.MMF && MMF.cfgFastMapReveal.Value) ? 2 : 1);
            else if (!anyPlayerHoldMap && mapRevealCounter > 0)
                mapRevealCounter--;

            if (mapRevealWaveCD > 0)
                mapRevealWaveCD--;

            if (mapRevealCounter >= 30)
            {
                if (mapRevealWaveCD == 0)
                {
                    activeWaveObjs.Add(new MapRevealSoundObject());
                    mapRevealWaveCD = 120;
                }
            }


            for (int i = activeWaveObjs.Count - 1; i >= 0; i--)
            {
                SoundObject waveObj = activeWaveObjs[i];
                if (waveObj.slateForDeletion)
                {
                    activeWaveObjs.RemoveAt(i);
                    continue;
                }
                waveObj.Update(game);
            }

            foreach (var key in rateLimit.Keys.ToArray())
            {
                if (rateLimit[key] > 0)
                    rateLimit[key]--;
            }
        }

        public void RawUpdate(RainWorldGame game, float timeStacker)
        {
            if (EffectSprite == null || EffectSprite._renderLayer == null || EffectSprite._renderLayer._material == null)
                return;

            Vector2 camPos = Vector2.Lerp(game.cameras[0].lastPos, game.cameras[0].pos, timeStacker);

            legalStaticCount = 0;

            foreach (var player in game.Players)
            {
                //身体周围
                if (player.realizedCreature != null && player.realizedCreature.room != null && !player.realizedCreature.dead)
                {
                    staticPos[legalStaticCount] = Vector2.Lerp(player.realizedCreature.mainBodyChunk.lastPos, player.realizedCreature.mainBodyChunk.pos, timeStacker) - camPos;
                    legalStaticCount++;
                    if (legalStaticCount == 40)
                        break;
                    //触手周围
                    if (CorruptionShapedMutationBuffEntry.CorruptionCatFeatures.TryGetValue(player.realizedCreature as Player, out var corruption))
                    {
                        bool notOnePlayer = false;
                        foreach (var otherPlayer in game.Players)
                        {
                            if (otherPlayer != player && otherPlayer.Room == player.Room)
                            {
                                notOnePlayer = true;
                            }
                        }
                        for (int i = 0; i < corruption.tentacles.Length; i++)
                        {
                            if (notOnePlayer)
                            {
                                bool skip = false;
                                foreach (var sp in staticPos)
                                {
                                    if (Custom.Dist(sp, corruption.tentacles[i].Tip.pos) < 40f)
                                    {
                                        skip = true;
                                        break;
                                    }
                                }
                                if (skip)
                                    continue;
                            }
                            staticPos[legalStaticCount] = Vector2.Lerp(corruption.tentacles[i].Tip.lastPos, corruption.tentacles[i].Tip.pos, timeStacker) - camPos;
                            legalStaticCount++;
                            if (legalStaticCount == 40)
                                break;
                        }
                    }
                }
            }

            if (game.cameras[0].room != null && legalStaticCount < 40)
            {
                foreach (var shortcut in game.cameras[0].room.shortcuts)
                {
                    if (shortcut.shortCutType == ShortcutData.Type.RoomExit || shortcut.shortCutType == ShortcutData.Type.RegionTransportation || shortcut.shortCutType == ShortcutData.Type.Normal)
                    {
                        staticPos[legalStaticCount] = game.cameras[0].room.MiddleOfTile(shortcut.StartTile) - camPos;
                        legalStaticCount++;
                    }
                    if (legalStaticCount == 40)
                        break;
                }
            }

            EffectSprite._renderLayer._material.SetInt("legalStaticCount", legalStaticCount);
            EffectSprite._renderLayer._material.SetVectorArray("staticCenter", staticPos);

            legalSoundCount = 0;

            foreach (var waveObj in activeWaveObjs)
            {
                if (waveObj.lastStrength <= 0 && waveObj.strength <= 0)
                    continue;

                Vector2 pos = Vector2.Lerp(waveObj.lastPos, waveObj.pos, timeStacker) - (waveObj.effectByCamPos ? camPos : Vector2.zero);
                float rad = waveObj.GetSmoothRad(timeStacker);
                float strength = Mathf.Clamp01(Mathf.Lerp(waveObj.lastStrength, waveObj.strength, timeStacker));
                soundWaveInfos[legalSoundCount].x = pos.x;
                soundWaveInfos[legalSoundCount].y = pos.y;
                soundWaveInfos[legalSoundCount].z = rad;
                soundWaveInfos[legalSoundCount].w = strength;
                legalSoundCount++;

                if (legalSoundCount >= soundWaveInfos.Length) break;
            }

            EffectSprite._renderLayer._material.SetInt("legalWaveInfoCount", legalSoundCount);
            EffectSprite._renderLayer._material.SetVectorArray("waveInfos", soundWaveInfos);
        }

        public void PositonedSoundPlayed(SoundID soundID, VirtualMicrophone.PositionedSound trackSound)
        {
            if (!rateLimit.ContainsKey(soundID))
                rateLimit.Add(soundID, 0);
            if (rateLimit[soundID] > 0)
                return;
            wavesToAdd.Add(new PositionedSoundObjectTracker(trackSound.pos, trackSound));
            //BuffUtils.Log($"WaveObject", $"New sound : {trackSound.initVol}");
            rateLimit[soundID] = 20;
        }

        public void DisembodiedLoopSoundPlayed(SoundID soundID, VirtualMicrophone.DisembodiedLoop disembodiedLoop)
        {
            if (!rateLimit.ContainsKey(soundID))
                rateLimit.Add(soundID, 0);
            if (rateLimit[soundID] > 0)
                return;
            wavesToAdd.Add(new DisembodiedLoopSoundObjectTracker(disembodiedLoop));
            //BuffUtils.Log($"WaveObject", $"New sound : {trackSound.initVol}");
            rateLimit[soundID] = 20;
        }

        public void AmbietnSoundPlayed(AmbientSoundPlayer ambientSoundPlayer)
        {
            wavesToAdd.Add(new AmbientSoundObjectTracker(ambientSoundPlayer));
        }

        public void RoomSwitch()
        {
            foreach (var obj in activeWaveObjs)
            {
                obj.Destroy();
            }
            activeWaveObjs.Clear();
        }

        public void Destroy()
        {
            foreach(var activeWaveObj in activeWaveObjs)
            {
                activeWaveObj.Destroy();
            }
            EffectSprite = null;
        }

        public class SoundObject
        {
            public Vector2 pos, lastPos;
            public float rad, lastRad, maxRad;
            public float strength, lastStrength;
            public bool slateForDeletion;
            public bool effectByCamPos = true;

            public virtual void Update(RainWorldGame game)
            {

            }

            public virtual void Destroy()
            {
                slateForDeletion = true;
            }

            public virtual float GetSmoothRad(float timeStacker)
            {
                float r = Mathf.Lerp(lastRad, rad, timeStacker);
                r = Helper.LerpEase(r / maxRad) * maxRad;
                return r;
            }
        }

        public class PositionedSoundObjectTracker : SoundObject
        {
            public float sDecrease;
            public float radIncrease;
            public WeakReference<VirtualMicrophone.PositionedSound> trackedSound;

            public PositionedSoundObjectTracker(Vector2 pos, VirtualMicrophone.PositionedSound trackedSound)
            {
                this.pos = this.lastPos = pos;
                sDecrease = 1 / 80f;
                this.maxRad = 20f;
                lastRad = rad = 1f;//防止除以0发生意外，该计算位于shader内
                radIncrease = maxRad * sDecrease;
                this.strength = this.lastStrength = 1f;
                this.trackedSound = new WeakReference<VirtualMicrophone.PositionedSound>(trackedSound);
            }

            void UpdateRadInfo(VirtualMicrophone.PositionedSound trackedSound)
            {
                maxRad = Mathf.Max(maxRad, Mathf.Clamp(trackedSound.volume * 100f, 20f, 1000f));
                radIncrease = maxRad * sDecrease;
            }

            public override void Update(RainWorldGame game)
            {
                if (slateForDeletion)
                    return;

                if (lastStrength <= 0 && strength <= 0)
                {
                    Destroy();
                }

                lastStrength = strength;
                strength -= sDecrease;

                lastRad = rad;
                rad += radIncrease;

                lastPos = pos;
                if (trackedSound.TryGetTarget(out var positionedSound))
                {
                    pos = positionedSound.pos;
                    lastPos = positionedSound.lastPos;
                    UpdateRadInfo(positionedSound);
                    if (positionedSound.slatedForDeletion)
                        trackedSound.SetTarget(null);
                }
            }


            public override void Destroy()
            {
                base.Destroy();
                trackedSound.SetTarget(null);
            }
        }

        public class DisembodiedLoopSoundObjectTracker : SoundObject
        {
            public WeakReference<VirtualMicrophone.DisembodiedLoop> trackedSound;

            int life, lastLife, initLife;


            public DisembodiedLoopSoundObjectTracker(VirtualMicrophone.DisembodiedLoop disembodiedLoop)
            {
                trackedSound = new WeakReference<VirtualMicrophone.DisembodiedLoop>(disembodiedLoop);
                effectByCamPos = false;
                lastPos = pos = new Vector2(Custom.rainWorld.options.ScreenSize.x / 2f * disembodiedLoop.controller.pan + Custom.rainWorld.options.ScreenSize.x / 2f, Custom.rainWorld.options.ScreenSize.y / 2f);
                initLife = life = lastLife = 160;
                lastRad = rad = 1f;
            }

            public override void Update(RainWorldGame game)
            {
                if (slateForDeletion)
                    return;

                lastPos = pos;
                lastLife = life;
                lastStrength = strength;
                lastRad = rad;

                if (life > 0)
                    life--;
                if (life == 0 && lastLife == 0)
                {
                    Destroy();
                }

                strength = Mathf.Sin(Mathf.PI * life / (float)initLife) * 0.2f;
                rad += 400f / 160f;


                if (trackedSound.TryGetTarget(out var target))
                {
                    if (target.slatedForDeletion || target.controller == null)
                    {
                        trackedSound.SetTarget(null);
                        return;
                    }
                    if (life == 0 && target.loop && target.allowPlay)
                    {
                        life = lastLife = 160;
                    }
                    pos = new Vector2(Custom.rainWorld.options.ScreenSize.x / 2f * target.controller.pan + Custom.rainWorld.options.ScreenSize.x / 2f, Custom.rainWorld.options.ScreenSize.y / 2f);
                }
            }

            public override float GetSmoothRad(float timeStacker)
            {
                return Mathf.Lerp(lastRad, rad, timeStacker);
            }
        }

        public class MapRevealSoundObject : SoundObject
        {
            int life, lastLife, initLife;

            public MapRevealSoundObject()
            {
                effectByCamPos = false;
                lastPos = pos = new Vector2(Custom.rainWorld.options.ScreenSize.x / 2f, Custom.rainWorld.options.ScreenSize.y / 2f);
                lastRad = rad = 1f;
                initLife = life = lastLife = 160;
            }

            public override void Update(RainWorldGame game)
            {
                base.Update(game);


                lastPos = pos;
                lastLife = life;
                lastStrength = strength;
                lastRad = rad;

                if (life > 0)
                    life--;
                if (life == 0 && lastLife == 0)
                    Destroy();

                strength = life / (float)initLife;
                rad += 800f / 160f;
            }

            public override float GetSmoothRad(float timeStacker)
            {
                return Mathf.Lerp(lastRad, rad, timeStacker);
            }
        }

        public class AmbientSoundObjectTracker : SoundObject
        {
            WeakReference<AmbientSoundPlayer> targetPlayer;

            float lifeParam, soundVol;

            public AmbientSoundObjectTracker(AmbientSoundPlayer ambientSoundPlayer)
            {
                targetPlayer = new WeakReference<AmbientSoundPlayer>(ambientSoundPlayer);
                rad = lastRad = 1f;
                maxRad = (ambientSoundPlayer.aSound as SpotSound).rad;
                soundVol = ambientSoundPlayer.aSound.volume;
                pos = lastPos = (ambientSoundPlayer.aSound as SpotSound).pos;
                lifeParam = Random.value;
            }

            public override void Update(RainWorldGame game)
            {
                base.Update(game);
                if (slateForDeletion)
                    return;

                bool delete = false;
                if (targetPlayer.TryGetTarget(out var player))
                {
                    if (player.slatedForDeletion)
                        Destroy();
                }
                else
                    delete = true;
                lifeParam += 1 / 160f;
                if (lifeParam > 1f)
                {
                    lifeParam--;
                    if (delete)
                        Destroy();
                }

                lastRad = rad;
                rad = Mathf.Lerp(1f, maxRad, lifeParam);
                lastStrength = strength;
                strength = Mathf.Sin(Mathf.PI * lifeParam) * soundVol;
            }

            public override float GetSmoothRad(float timeStacker)
            {
                return Mathf.Lerp(lastRad, rad, timeStacker);
            }
        }

    }
    #endregion

    internal class CorruptionShapedMutationBuffData : BuffData
    {
        public override BuffID ID => CorruptionShapedMutationBuffEntry.CorruptionShapedMutation;

        [CustomBuffConfigInfo("HavingVision", "")]
        [CustomBuffConfigTwoValue(false, true)]
        public bool HavingVisionKey { get; }

        public override void Stack()
        {
            base.Stack();
            CorruptionShapedMutationBuffEntry.EstablishRelationship();
        }
    }

    internal class CorruptionShapedMutationBuffEntry : IBuffEntry
    {
        public static BuffID CorruptionShapedMutation = new BuffID("CorruptionShapedMutation", true);

        public static ConditionalWeakTable<Player, CorruptionCat> CorruptionCatFeatures = new ConditionalWeakTable<Player, CorruptionCat>();

        public delegate CreatureTemplate.Relationship orig_IUseARelationshipTracker_UpdateDynamicRelationship(ArtificialIntelligence self, RelationshipTracker.DynamicRelationship dRelation);
        public delegate float orig_PhysicalObject_TotalMass(PhysicalObject self);

        public static int StackLayer => CorruptionShapedMutation.GetBuffData()?.StackLayer ?? 0;

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<CorruptionShapedMutationBuff, CorruptionShapedMutationBuffData, CorruptionShapedMutationBuffEntry>(CorruptionShapedMutation);
        }

        public static void LoadAssets()
        {
            var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("buffassets/assetBundles/builtinbundle"));
            Custom.rainWorld.Shaders.Add("BlindWave", FShader.CreateShader($"BlindWave", bundle.LoadAsset<Shader>("blindwave")));
            bundle.Unload(false);
        }

        public static void HookOn()
        {
            IL.Player.MovementUpdate += Player_MovementUpdateIL;
            IL.DaddyCorruption.Bulb.Update += DaddyCorruption_Bulb_UpdateIL;

            On.Player.CanEatMeat += Player_CanEatMeat;
            On.SlugcatStats.NourishmentOfObjectEaten += SlugcatStats_NourishmentOfObjectEaten;
            On.MoreSlugcats.SlugNPCAI.TheoreticallyEatMeat += SlugNPCAI_TheoreticallyEatMeat;
            On.Creature.Violence += Creature_Violence;

            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.Player.Collide += Player_Collide;
            On.Player.Grabability += Player_Grabability;
            On.Player.MovementUpdate += Player_MovementUpdate;
            On.Player.FreeHand += Player_FreeHand;
            On.Player.Jump += Player_Jump;
            On.Player.NewRoom += Player_NewRoom;
            On.Player.DeathByBiteMultiplier += Player_DeathByBiteMultiplier;

            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.Reset += PlayerGraphics_Reset;
            On.PlayerGraphics.ctor += PlayerGraphics_ctor;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;

            On.OracleBehavior.Update += OracleBehavior_Update;
            /*
            Hook hook = new Hook(typeof(PhysicalObject).GetProperty("TotalMass", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(), 
                                 typeof(CorruptionShapedMutationBuffEntry).GetMethod("PhysicalObject_get_TotalMass", BindingFlags.Static | BindingFlags.NonPublic));
            */
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach(var type in ass.SafeGetTypes())
                    {
                        if (type.BaseType == typeof(ArtificialIntelligence) &&
                            type.GetInterface("IUseARelationshipTracker") != null &&
                            type.GetMethod("IUseARelationshipTracker.UpdateDynamicRelationship", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.DeclaringType == type)
                        {
                            //if (BanAi.Contains(type)) continue;

                            _ = new Hook(type.GetMethod("IUseARelationshipTracker.UpdateDynamicRelationship", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
                                typeof(CorruptionShapedMutationBuffEntry).GetMethod(nameof(IUseARelationshipTracker_UpdateDynamicRelationship), BindingFlags.Static | BindingFlags.NonPublic));
                        }
                    }
                }
                catch (Exception ex)
                {
                    BuffPlugin.LogError(ex);
                }
            }

            //视觉效果
            On.VirtualMicrophone.SoundObject.Play += SoundObject_Play;
            On.AmbientSoundPlayer.TryInitiation += AmbientSoundPlayer_TryInitiation;
            On.RainWorldGame.GrafUpdate += RainWorldGame_GrafUpdate;
        }
        
        public static void LongLifeCycleHookOn()
        {
            On.SlugcatStats.SlugcatFoodMeter += SlugcatStats_SlugcatFoodMeter;
            //On.StaticWorld.InitStaticWorld += StaticWorld_InitStaticWorld;
        }
        #region BlindWaveRelated
        private static void RainWorldGame_GrafUpdate(On.RainWorldGame.orig_GrafUpdate orig, RainWorldGame self, float timeStacker)
        {
            orig.Invoke(self, timeStacker);
            if (!CorruptionShapedMutationBuff.Instance.Data.HavingVisionKey &&
                CorruptionShapedMutationBuff.Instance.blindWaveEffectManager != null)
                CorruptionShapedMutationBuff.Instance.blindWaveEffectManager.RawUpdate(self, timeStacker);
        }

        private static void AmbientSoundPlayer_TryInitiation(On.AmbientSoundPlayer.orig_TryInitiation orig, AmbientSoundPlayer self)
        {
            orig.Invoke(self);
            if (!CorruptionShapedMutationBuff.Instance.Data.HavingVisionKey &&
                CorruptionShapedMutationBuff.Instance.blindWaveEffectManager != null && 
                self.initiated && self.aSound.type == AmbientSound.Type.Spot)
            {
                CorruptionShapedMutationBuff.Instance.blindWaveEffectManager.AmbietnSoundPlayed(self);
            }
        }

        private static void SoundObject_Play(On.VirtualMicrophone.SoundObject.orig_Play orig, VirtualMicrophone.SoundObject self)
        {
            orig.Invoke(self);
            if (!CorruptionShapedMutationBuff.Instance.Data.HavingVisionKey &&
                CorruptionShapedMutationBuff.Instance.blindWaveEffectManager != null &&
                self is VirtualMicrophone.PositionedSound positionedSound)
            {
                CorruptionShapedMutationBuff.Instance.blindWaveEffectManager.PositonedSoundPlayed(self.soundData.soundID, positionedSound);
            }
            //else if(self is VirtualMicrophone.DisembodiedLoop disembodiedLoop)
            //{
            //    UltraCoinsBuff.Instance.blindWaveEffectManager.DisembodiedLoopSoundPlayed(self.soundData.soundID, disembodiedLoop);
            //}
        }
        #endregion
        #region 迭代器相关
        public static void OracleBehavior_Update(On.OracleBehavior.orig_Update orig, OracleBehavior self, bool eu)
        {
            orig(self, eu);
            if (self.oracle != null && self.oracle.room != null)
            {
                for (int i = 0; i < self.oracle.room.abstractRoom.creatures.Count; i++)
                {
                    if (self.oracle.room.abstractRoom.creatures[i].realizedCreature != null &&
                        self.oracle.room.abstractRoom.creatures[i].realizedCreature is Player player &&
                        self.player == player)
                    {
                        if (self.oracle.room.game.cameras[0].hud.dialogBox != null &&
                            self.oracle.room.game.cameras[0].hud.dialogBox.messages != null)
                            self.oracle.room.game.cameras[0].hud.dialogBox.messages.Clear();
                        //如果迭代器是跪坐的,则拒绝说话
                        if ((self.oracle.room.world != null && 
                            (self.oracle.room.world.region.name == "SL" || 
                             self.oracle.room.world.region.name == "RM" ||
                             self.oracle.room.world.region.name == "CL")) || 
                            (self.oracle.IsTileSolid(0, 0, -1) || (self.oracle.bodyChunks.Length > 1 && self.oracle.IsTileSolid(1, 0, -1)) ||
                             self.oracle.bodyChunks[0].ContactPoint.y == -1 || (self.oracle.bodyChunks.Length > 1 && self.oracle.bodyChunks[1].ContactPoint.y == -1)))
                        {
                            if (CorruptionCatFeatures.TryGetValue(player, out var corruption))
                            {
                                corruption.killFac = 0f;
                            }
                        }
                        //如果迭代器是飘着的，则拒绝说话，并杀猫
                        else
                        {
                            if (CorruptionCatFeatures.TryGetValue(player, out var corruption))
                            {
                                corruption.KillPlayerUpdate(self.oracle);
                            }
                        }
                    }
                }
            }
        }
        #endregion
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
                BuffUtils.LogError(CorruptionShapedMutation, "IL HOOK FAILED");
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

        //食量增大（需求+6，存储-3）
        private static IntVector2 SlugcatStats_SlugcatFoodMeter(On.SlugcatStats.orig_SlugcatFoodMeter orig, SlugcatStats.Name slugcat)
        {
            IntVector2 origFoodRequirement = orig(slugcat);
            int newHibernateRequirement = origFoodRequirement.y + (4 + 2 * StackLayer);
            int newTotalFoodRequirement = origFoodRequirement.x + (2 + 1 * StackLayer);

            if (newHibernateRequirement > newTotalFoodRequirement)
            {
                newHibernateRequirement += newHibernateRequirement - newTotalFoodRequirement;
                newTotalFoodRequirement = newHibernateRequirement;
            }

            return new IntVector2(newTotalFoodRequirement, newHibernateRequirement);
        }

        //无法抓取
        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            Player.ObjectGrabability result = orig(self, obj);

            if (CorruptionCatFeatures.TryGetValue(self, out var corruption))
            {
                result = corruption.Grabability(result, obj);
            }

            return result;
        }

        //不可拾取
        private static int Player_FreeHand(On.Player.orig_FreeHand orig, Player self)
        {
            int result = orig(self);
            if (CorruptionCatFeatures.TryGetValue(self, out var corruption))
                result = -1;
            return result;
        }

        //蜥蜴咬伤必定不死
        private static float Player_DeathByBiteMultiplier(On.Player.orig_DeathByBiteMultiplier orig, Player self)
        {
            float result = orig(self);
            if (CorruptionCatFeatures.TryGetValue(self, out var corruption))
                result = 0f;
            return result;
        }

        //紫菇之前爆炸抗性减弱，并增加了生命值设定
        private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, UnityEngine.Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage,
            Creature.DamageType type, float damage, float stunBonus)
        {
            float origDamage = damage;
            if (self is Player && type == Creature.DamageType.Explosion &&
                CorruptionCatFeatures.TryGetValue(self as Player, out _) &&
                CorruptionCat.Type !=  DLCSharedEnums.CreatureTemplateType.TerrorLongLegs)
            {
                damage *= 0f;
                stunBonus *= 3f;
            }
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
            if (self is Player && CorruptionCatFeatures.TryGetValue(self as Player, out var corruption))
            {
                damage = origDamage;
                float num = damage / self.Template.baseDamageResistance;
                if (type.Index != -1)
                {
                    if (self.Template.damageRestistances[type.Index, 0] > 0f)
                    {
                        num /= self.Template.damageRestistances[type.Index, 0];
                    }
                }
                if (ModManager.MSC)
                {
                    if (self.room != null && self.room.world.game.IsArenaSession && self.room.world.game.GetArenaGameSession.chMeta != null && self.room.world.game.GetArenaGameSession.chMeta.resistMultiplier > 0f && !(self is Player))
                    {
                        num /= self.room.world.game.GetArenaGameSession.chMeta.resistMultiplier;
                    }
                    if (self.room != null && self.room.world.game.IsArenaSession && self.room.world.game.GetArenaGameSession.chMeta != null && self.room.world.game.GetArenaGameSession.chMeta.invincibleCreatures && !(self is Player))
                    {
                        num = 0f;
                    }
                }
                (corruption.state as HealthState).health -= num;
                if (StaticWorld.GetCreatureTemplate(CorruptionCat.Type).quickDeath && 
                    (Random.value < -(corruption.state as HealthState).health || 
                    (corruption.state as HealthState).health < -1f || 
                    ((corruption.state as HealthState).health < 0f && Random.value < 0.33f)))
                {
                    self.Die();
                }
                BuffPlugin.Log($"[CorruptionShapedMutation] Left Health: {(corruption.state as HealthState).health * self.Template.baseDamageResistance}");
            }
        }

        //玩家体重会计入核心重量
        private static float PhysicalObject_get_TotalMass(CorruptionShapedMutationBuffEntry.orig_PhysicalObject_TotalMass orig, PhysicalObject self)
        {
            float result = orig(self);
            if (self is Player && CorruptionCatFeatures.TryGetValue(self as Player, out var corruption))
                result += corruption.CoreTotalMass;
            return result;
        }
        #endregion
        #region 生物关系
        //修改生物关系（棕色长腿菌、猎手长腿菌不再攻击玩家，其他生物对蛞蝓猫的生物关系变成对长腿菌的生物关系）
        public static void EstablishRelationship()
        {
            StaticWorld.EstablishRelationship(CreatureTemplate.Type.BrotherLongLegs, CreatureTemplate.Type.Slugcat, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f));
            StaticWorld.EstablishRelationship(MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy, CreatureTemplate.Type.Slugcat, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f));
            //其他生物对蛞蝓猫的生物关系变成对棕色长腿菌的生物关系
            CreatureTemplate daddy = StaticWorld.GetCreatureTemplate(CorruptionCat.Type);
            CreatureTemplate slug = StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat);
            CreatureTemplate slugpup = StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC);
            if (daddy == null || slug == null || slug.type.Index == -1)
            {
                return;
            }
            foreach (CreatureTemplate other in StaticWorld.creatureTemplates)
            {
                if (other != null)
                {
                    //已经是蛞蝓猫的食物则不修改
                    if (slugpup.relationships[other.type.Index].type != CreatureTemplate.Relationship.Type.Eats)
                    {
                        StaticWorld.EstablishRelationship(other.type, slug.type, other.relationships[daddy.type.Index]);
                        StaticWorld.EstablishRelationship(slug.type, other.type, daddy.relationships[other.type.Index]);

                        StaticWorld.EstablishRelationship(other.type, slugpup.type, other.relationships[daddy.type.Index]);
                        StaticWorld.EstablishRelationship(slugpup.type, other.type, daddy.relationships[other.type.Index]);
                    }
                    else
                    {
                        StaticWorld.EstablishRelationship(other.type, slug.type, other.relationships[slugpup.type.Index]);
                        StaticWorld.EstablishRelationship(slug.type, other.type, slugpup.relationships[other.type.Index]);
                    }
                }
            }
        }

        private static CreatureTemplate.Relationship IUseARelationshipTracker_UpdateDynamicRelationship(orig_IUseARelationshipTracker_UpdateDynamicRelationship orig,ArtificialIntelligence self, RelationshipTracker.DynamicRelationship dRelation)
        {
            CreatureTemplate.Relationship result = orig(self, dRelation);
            if (self.creature != null)
            {
                CreatureTemplate slug = StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat);
                result = self.creature.creatureTemplate.relationships[slug.index];
                //BuffPlugin.Log($"[CorruptionShapedMutation]The Dynamic Relationship between {self.creature.creatureTemplate.type.ToString()} and {slug.type.ToString()}: {result.ToString()}");
            }
            return result;
        }

        //免疫香菇墙
        private static void DaddyCorruption_Bulb_UpdateIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILCursor find = new ILCursor(il);
            ILLabel pos = null;
            int local = 9;
            //找到原方法结束的地方
            if (find.TryGotoNext(MoveType.After,
                                 (i) => i.MatchLdfld<DaddyCorruption.Bulb> ("eatChunk"),
                                 (i) => i.Match(OpCodes.Brfalse),
                                 (i) => i.MatchLdloc(out local)))
            {
                find.Emit(OpCodes.Stloc_S, (byte)local);
                pos = find.MarkLabel();
                find.Emit(OpCodes.Ldloc_S, (byte)local);
            }
            else
                BuffUtils.LogError(CorruptionShapedMutation, "IL HOOK FAILED (pos)");
            if (c.TryGotoNext(MoveType.After,
                              (i) => i.MatchIsinst("DaddyLongLegs"),
                              (i) => i.Match(OpCodes.Brtrue)))
            {
                if (pos != null)
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldloc_S, (byte)local);
                    c.EmitDelegate<Func<DaddyCorruption.Bulb, int, bool>>((self, i) =>
                    {
                        return self.owner.room.abstractRoom.creatures[i].realizedCreature != null &&
                               self.owner.room.abstractRoom.creatures[i].realizedCreature is Player player &&
                               CorruptionCatFeatures.TryGetValue(player, out _);
                    });
                    c.Emit(OpCodes.Brtrue, pos);
                }
            }
            else
                BuffUtils.LogError(CorruptionShapedMutation, "IL HOOK FAILED (c)");
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
                corruption.Collide(otherObject, self.bodyChunks[myChunk], otherObject.bodyChunks[otherChunk]);
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
                corruption.MovementUpdate(eu);
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
                corruption.graphics.ctor(self, ow);
            }
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            if (CorruptionCatFeatures.TryGetValue(self.player, out var corruption))
                corruption.graphics.GraphicsUpdate();
            orig(self);
        }

        private static void PlayerGraphics_Reset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig(self);
            if (CorruptionCatFeatures.TryGetValue(self.player, out var corruption))
                corruption.graphics.Reset(self);
        }
        #endregion
    }

    internal class CorruptionCat
    {
        WeakReference<Player> ownerRef;
        public BodyChunk[] coreChunks;
        public PhysicalObject.BodyChunkConnection[] coreChunkConnections;
        public float killFac;
        public DaddyState state;

        public bool SizeClass
        {
            get
            {
                return CorruptionShapedMutationBuffEntry.StackLayer >= 2;
                //return (ModManager.MSC && (base.Template.type == DLCSharedEnums.CreatureTemplateType.TerrorLongLegs || this.world.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Spear || this.world.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer)) || base.Template.type == CreatureTemplate.Type.DaddyLongLegs;
            }
        }

        public float CoreTotalMass
        {
            get
            {
                float mass = 0f;
                for (int i = 0; i < this.coreChunks.Length; i++)
                {
                    mass += this.coreChunks[i].mass;
                }
                return mass;
            }
        }

        public Vector2 LastMiddleOfBody
        {
            get
            {
                if (!ownerRef.TryGetTarget(out var player))
                    return Vector2.zero;
                Vector2 vector = Vector2.zero;
                for (int i = 0; i < player.bodyChunks.Length; i++)
                {
                    vector += player.bodyChunks[i].lastPos * player.bodyChunks[i].mass;
                }/*
                for (int i = 0; i < this.coreChunks.Length; i++)
                {
                    vector += this.coreChunks[i].lastPos * this.coreChunks[i].mass;
                }*/
                return vector / player.TotalMass;
            }
        }

        public Vector2 MiddleOfBody
        {
            get
            {
                if (!ownerRef.TryGetTarget(out var player))
                    return Vector2.zero;
                Vector2 vector = Vector2.zero;
                for (int i = 0; i < player.bodyChunks.Length; i++)
                {
                    vector += player.bodyChunks[i].pos * player.bodyChunks[i].mass;
                }/*
                for (int i = 0; i < this.coreChunks.Length; i++)
                {
                    vector += this.coreChunks[i].pos * this.coreChunks[i].mass;
                }*/
                return vector / player.TotalMass;
            }
        }

        public static CreatureTemplate.Type Type
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
                        return DLCSharedEnums.CreatureTemplateType.TerrorLongLegs;
                    default:
                        return DLCSharedEnums.CreatureTemplateType.TerrorLongLegs;
                }
            }
        }

        #region 行动
        private float moveSpeed;
        private Vector2 bodyWantPos;
        private Vector2 wantPos;
        public Vector2 moveDirection;
        public int stuckCounter;
        public int TotalGrip
        {
            get
            {
                int num = 0;
                for (int n = 0; n < this.tentacles.GetLength(0); n++)
                {
                    if (this.tentacles[n].atGrabDest || this.tentacles[n].Tip.contactPoint.x != 0 || this.tentacles[n].Tip.contactPoint.y != 0)
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
                    if ((this.tentacles[n].atGrabDest || this.tentacles[n].Tip.contactPoint.x != 0 || this.tentacles[n].Tip.contactPoint.y != 0) && !this.tentacles[n].OppositeDir)
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
        public int ChoosedTentaclesButNotControlCount
        {
            get
            {
                int num = 0;
                for (int n = 0; n < this.tentacles.GetLength(0); n++)
                {
                    if (this.tentacles[n].chooseToMoveByPlayerButNotControl)
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
        public List<CorruptionCat.EatObject> eatObjects;

        //负重情况
        private float MassFac
        {
            get
            {
                if (!ownerRef.TryGetTarget(out var player))
                    return 1f;

                float graspMass = 0f;
                if (this.tentacles != null)
                {
                    for (int i = 0; i < this.tentacles.Length; i++)
                    {
                        if (this.tentacles[i].grabChunk != null)
                            graspMass += this.tentacles[i].grabChunk.owner.TotalMass;
                    }
                }

                float result = (player.TotalMass + graspMass) / player.slugcatStats.runspeedFac;
                return result;
            }
        }

        private bool MassFacCondition //成立时导致玩家走不动，下落
        {
            get
            {
                if (!ownerRef.TryGetTarget(out var player))
                    return false;
                return MassFac >= 8f + 4f * CorruptionShapedMutationBuff.corruptionLevel + 2f * CorruptionShapedMutationBuffEntry.StackLayer;
            }
        }

        public float DefaultMoveSpeed
        {
            get
            {
                return 2f * (4f + 2f * CorruptionShapedMutationBuff.speedLevel + 
                            1f * CorruptionShapedMutationBuff.corruptionLevel + 
                            1f * CorruptionShapedMutationBuffEntry.StackLayer);
            }
        }

        public float MostDigestedEatObject
        {
            get
            {
                float num = 0f;
                for (int i = 0; i < this.eatObjects.Count; i++)
                {
                    num = Mathf.Max(num, this.eatObjects[i].progression);
                }
                return num;
            }
        }
        #endregion
        #region 操作
        public bool HaveDirInput => ownerRef.TryGetTarget(out var player) && (player.input[0].x != 0 || player.input[0].y != 0);
        public bool WantToMoveBody => ownerRef.TryGetTarget(out var player) && !player.input[0].jmp && this.EnoughGripToMove();
        public bool WantToHunt => ownerRef.TryGetTarget(out var player) && (player.input[0].thrw || player.input[0].pckp);
        public bool WantToAutoHunt => ownerRef.TryGetTarget(out var player) && player.input[0].thrw;
        #endregion
        #region 外观
        public CorruptionCatGraphics graphics;
        public CorruptionCatTentacle[] tentacles;
        public int totalLegSprites;
        public Color effectColor;
        public Color eyeColor;
        public Color EffectColor => Color.blue;
        public int TentaclesCount => Mathf.RoundToInt(4 + 1 * CorruptionShapedMutationBuff.corruptionLevel + 2 * CorruptionShapedMutationBuffEntry.StackLayer *
            (ownerRef.TryGetTarget(out var player) && player.isSlugpup ? 0.75f : 1f));
        public float TentaclesLength => (150f + 50f * CorruptionShapedMutationBuff.corruptionLevel + 100f * CorruptionShapedMutationBuffEntry.StackLayer) *
            (ownerRef.TryGetTarget(out var player) && player.isSlugpup ? 0.5f : 1f);
        #endregion

        public CorruptionCat(Player player)
        {
            this.ownerRef = new WeakReference<Player>(player);
            this.state = new DaddyState(this, player.abstractCreature);
            var oldChunks = player.bodyChunks;
            //核心
            this.coreChunks = new BodyChunk[4];
            for (int i = 0; i < this.coreChunks.Length; i++)
            {
                this.coreChunks[i] = new BodyChunk(player, player.bodyChunks.Length + i, player.bodyChunks[0].pos + 6f * Custom.RNV(), i < 2 ? 6f : 4.5f, 0.45f);
                //this.coreChunks[i].collideWithTerrain = false;//注意，如果为true，则需要Player.TerrainImpact会报错，可以改但来不及了，所以这里先不管地形碰撞了
            }
            this.coreChunkConnections = new PhysicalObject.BodyChunkConnection[4 * this.coreChunks.Length + 2];
            for (int i = 0; i < this.coreChunks.Length; i++)
            {
                this.coreChunkConnections[i] = new PhysicalObject.BodyChunkConnection(this.coreChunks[i], player.bodyChunks[0], 
                    Mathf.Lerp(6f, 22f, (float)i / this.coreChunks.Length), PhysicalObject.BodyChunkConnection.Type.Pull, 1f, 0.5f);
                this.coreChunkConnections[i + 1 * this.coreChunks.Length] = new PhysicalObject.BodyChunkConnection(this.coreChunks[i], player.bodyChunks[0],
                    Mathf.Lerp(3f, 11f, (float)i / this.coreChunks.Length), PhysicalObject.BodyChunkConnection.Type.Push, 1f, 0.5f);
                this.coreChunkConnections[i + 2 * this.coreChunks.Length] = new PhysicalObject.BodyChunkConnection(this.coreChunks[i], player.bodyChunks[1], 
                    Mathf.Lerp(18f, 6f, (float)i / this.coreChunks.Length), PhysicalObject.BodyChunkConnection.Type.Pull, 1f, 0.5f);
                this.coreChunkConnections[i + 3* this.coreChunks.Length] = new PhysicalObject.BodyChunkConnection(this.coreChunks[i], player.bodyChunks[1],
                    Mathf.Lerp(9f, 3f, (float)i / this.coreChunks.Length), PhysicalObject.BodyChunkConnection.Type.Push, 1f, 0.5f);
            }
            this.coreChunkConnections[this.coreChunkConnections.Length - 2] = new PhysicalObject.BodyChunkConnection(this.coreChunks[0], this.coreChunks[1],
                    10f, PhysicalObject.BodyChunkConnection.Type.Push, 1f, 0.5f);
            this.coreChunkConnections[this.coreChunkConnections.Length - 1] = new PhysicalObject.BodyChunkConnection(this.coreChunks[2], this.coreChunks[3],
                    10f, PhysicalObject.BodyChunkConnection.Type.Push, 1f, 0.5f);
            //新身体
            player.bodyChunks = new BodyChunk[oldChunks.Length + this.coreChunks.Length];
            for(int i = 0; i < oldChunks.Length; i++)
                player.bodyChunks[i] = oldChunks[i];
            for(int i = oldChunks.Length; i < player.bodyChunks.Length; i++)
                player.bodyChunks[i] = this.coreChunks[i - oldChunks.Length];
            //触手
            this.tentacles = new CorruptionCatTentacle[this.TentaclesCount];
            for (int i = 0; i < Mathf.Min(player.bodyChunks.Length, this.tentacles.Length); i++)
                this.tentacles[i] = new CorruptionCatTentacle(player, this, player.bodyChunks[i], this.TentaclesLength, i, Custom.DegToVec(i * 60f + 30f));
            if (this.tentacles.Length > player.bodyChunks.Length)
                for (int i = player.bodyChunks.Length; i < this.tentacles.Length; i++)
                    this.tentacles[i] = new CorruptionCatTentacle(player, this, player.bodyChunks[1], this.TentaclesLength, i, Custom.DegToVec(i * 60f + 30f));/*
            if (this.tentacles.Length > player.bodyChunks.Length)
                for (int i = player.bodyChunks.Length; i < Mathf.Min(player.bodyChunks.Length + this.coreChunks.Length, this.tentacles.Length); i++)
                    this.tentacles[i] = new CorruptionCatTentacle(player, this, this.coreChunks[i - player.bodyChunks.Length], this.TentaclesLength, i, Custom.DegToVec(i * 60f + 30f));
            if (this.tentacles.Length > player.bodyChunks.Length + this.coreChunks.Length)
                for (int i = player.bodyChunks.Length + this.coreChunks.Length; i < this.tentacles.Length; i++)
                    this.tentacles[i] = new CorruptionCatTentacle(player, this, player.bodyChunks[1], this.TentaclesLength, i, Custom.DegToVec(i * 60f + 30f));*/

            this.moveSpeed = DefaultMoveSpeed;
            this.eatObjects = new List<CorruptionCat.EatObject>();
            this.graphics = new CorruptionCatGraphics(player, this);
            this.wantPos = player.mainBodyChunk.pos;
            this.bodyWantPos = player.mainBodyChunk.pos;
            //免疫香菇触手
            player.abstractCreature.tentacleImmune = true;
            //立即致死伤害
            player.Template.instantDeathDamageLimit = this.state.health;

            this.NewRoom(player.room);
        }

        //进行更新
        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (player.room == null)
                return;
            player.playerState.permanentDamageTracking = 0f;/*
            for (int i = 0; i < this.coreChunks.Length; i++)
            {
                this.coreChunks[i].Update();
            }*/
            for (int j = 0; j < this.coreChunkConnections.Length; j++)
            {
                this.coreChunkConnections[j].Update();
            }
            //即时变更触手状态
            if (this.tentacles.Length != this.TentaclesCount && player.room != null)
            {
                Array.Resize(ref this.tentacles, this.TentaclesCount);
                for (int i = 0; i < Mathf.Min(player.bodyChunks.Length, this.tentacles.Length); i++)
                    this.tentacles[i] = new CorruptionCatTentacle(player, this, player.bodyChunks[i], this.TentaclesLength, i, Custom.DegToVec(i * 60f + 30f));
                if (this.tentacles.Length > player.bodyChunks.Length)
                    for (int i = player.bodyChunks.Length; i < this.tentacles.Length; i++)
                        this.tentacles[i] = new CorruptionCatTentacle(player, this, player.bodyChunks[1], this.TentaclesLength, i, Custom.DegToVec(i * 60f + 30f));/*
                if (this.tentacles.Length > player.bodyChunks.Length)
                    for (int i = player.bodyChunks.Length; i < Mathf.Min(player.bodyChunks.Length + this.coreChunks.Length, this.tentacles.Length); i++)
                        this.tentacles[i] = new CorruptionCatTentacle(player, this, this.coreChunks[i - player.bodyChunks.Length], this.TentaclesLength, i, Custom.DegToVec(i * 60f + 30f));
                if (this.tentacles.Length > player.bodyChunks.Length + this.coreChunks.Length)
                    for (int i = player.bodyChunks.Length + this.coreChunks.Length; i < this.tentacles.Length; i++)
                        this.tentacles[i] = new CorruptionCatTentacle(player, this, player.bodyChunks[1], this.TentaclesLength, i, Custom.DegToVec(i * 60f + 30f));*/

                foreach (var tentacle in tentacles)
                    tentacle.NewRoom(player.room);
                this.graphics.legGraphics = new CorruptionCatLegGraphics[this.tentacles.Length];
                if (BuffCustom.TryGetGame(out var game))
                    this.graphics.ResetSprites(game.cameras[0].spriteLeasers.
                        First(i => i.drawableObject == player.graphicsModule), game.cameras[0]);


                this.state.tentacleHealth = new float[this.TentaclesCount];
                for (int i = 0; i < this.state.tentacleHealth.Length; i++)
                {
                    this.state.tentacleHealth[i] = 1f;
                }
            }
            
            for (int i = 0; i < this.tentacles.Length; i++)
            {
                if (this.tentacles[i].grabChunk == null)
                    continue;
                for(int j = 0; j < this.coreChunks.Length; j++)
                {
                    if(Custom.Dist(this.tentacles[i].grabChunk.pos, this.coreChunks[j].pos) < this.tentacles[i].grabChunk.rad + this.coreChunks[j].rad)
                        this.Collide(this.tentacles[i].grabChunk.owner, this.coreChunks[j], this.tentacles[i].grabChunk);
                }
            }

            //颜色
            this.effectColor = this.graphics.EffectColorA;
            this.eyeColor = this.graphics.EffectColorB;
            //始终闭眼
            player.Blink(5);
            //无法站立
            player.standing = false;
            //无法投掷
            player.slugcatStats.throwingSkill = -1;//Mathf.Max(-1, origThrowingSkill - VultureShapedMutationBuffEntry.StackLayer + 1);
            //无法拾取任何东西
            for (int i = 0; i < player.grasps.Length; i++)
                if (player.grasps[i] != null)
                    player.ReleaseGrasp(i);

            if (player.graphicsModule != null && player.Consious && //(player.room.aimap == null ||
                !EnoughGripToMove())//(!player.room.aimap.TileAccessibleToCreature(player.mainBodyChunk.pos, StaticWorld.GetCreatureTemplate(this.Type)) &&
                // !player.room.aimap.TileAccessibleToCreature(player.bodyChunks[1].pos, StaticWorld.GetCreatureTemplate(this.Type)))))
            {
                for (int l = 0; l < this.tentacles.GetLength(0); l++)
                {
                    if (this.tentacles[l].atGrabDest)// &&
                    //!Custom.DistLess(player.mainBodyChunk.pos, this.tentacles[l].Tip.pos, this.tentacles[l].idealLength * 0.7f)
                    {
                        Vector2 a = Custom.DirVec(player.mainBodyChunk.pos, this.tentacles[l].Tip.pos) *
                            Mathf.Pow(Custom.LerpMap(Mathf.Pow((Vector2.Distance(player.mainBodyChunk.pos, this.tentacles[l].Tip.pos) / this.tentacles[l].idealLength), 2f),
                                                     0.65f, 1f,
                                                     0f, 1.5f), 3f);
                        player.mainBodyChunk.pos += a * 0.8f * (player.room.gravity - player.buoyancy * player.Submersion) * 2f;
                        player.mainBodyChunk.vel += a * 0.8f * (player.room.gravity - player.buoyancy * player.Submersion) * 2f;
                    }
                }
            }
            for (int m = 0; m < this.tentacles.Length; m++)
            {
                //触手回血
                if (ModManager.MSC)
                {
                    if ((this.state as DaddyState).tentacleHealth[m] < 1f)
                    {
                        if (CorruptionCat.Type == DLCSharedEnums.CreatureTemplateType.TerrorLongLegs)//base.abstractCreature.superSizeMe || this.isHD
                        {
                            (this.state as DaddyState).tentacleHealth[m] += 0.0012f;
                        }
                        else if (this.SizeClass)
                        {
                            (this.state as DaddyState).tentacleHealth[m] += 0.0003f;
                        }
                    }
                    if ((this.state as DaddyState).tentacleHealth[m] > 1f)
                    {
                        (this.state as DaddyState).tentacleHealth[m] = 1f;
                    }
                }
                this.tentacles[m].Update();
                this.tentacles[m].retractFac = this.squeezeFac;
            }

            if (this.digestingCounter > 0)
            {
                this.digestingCounter--;
                if (this.digestingCounter > 30)
                {
                    this.eyesClosed = Math.Max(10, this.eyesClosed);
                }
                player.stun = Math.Max(10, player.stun);
            }
            if (this.eyesClosed > 0)
            {
                this.eyesClosed--;
            }
            this.Eat();
            if (player.Consious)
            {
                this.Act(this.TotalGrip);
                if (player.room != null && player.room.BackgroundNoise > 0.35f)
                {
                    this.eyesClosed = Math.Max(this.eyesClosed, 15 + (int)Custom.LerpMap(player.room.BackgroundNoise, 0.35f, 1f, 15f, 100f));
                    return;
                }
            }
            else
            {/*
                if (this.HDmode && !base.dead && !base.Consious && this.AI.preyTracker != null)
                {
                    this.AI.preyTracker.ForgetAllPrey();
                }*/
                this.eyesClosed = Math.Max(this.eyesClosed, 15);
            }

            player.bodyMode = Player.BodyModeIndex.Default;
            player.animation = Player.AnimationIndex.None;
        }

        public void Collide(PhysicalObject otherObject, BodyChunk myChunk, BodyChunk otherChunk)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            bool wantEatCreatue = otherObject is Creature creature && 
                                  creature != null &&
                                  this.DynamicRelationship(creature.abstractCreature).type == CreatureTemplate.Relationship.Type.Eats;
            bool wantEatObject = CanEatObjectType(otherObject) && otherObject != null;
            if ((wantEatCreatue || wantEatObject) &&
                this.CheckDaddyConsumption(otherObject))
            {
                bool flag = false;
                if (!this.SizeClass && this.digestingCounter > 0)
                {
                    return;
                }
                int num = 0;
                while (num < this.tentacles.Length && !flag)
                {
                    if (this.tentacles[num].grabChunk != null && this.tentacles[num].grabChunk.owner == otherObject)
                    {
                        flag = true;
                    }
                    num++;
                }
                int num2 = 0;
                while (num2 < this.eatObjects.Count && flag)
                {
                    if (this.eatObjects[num2].chunk.owner == otherObject)
                    {
                        flag = false;
                    }
                    num2++;
                }
                if (flag && this.WantToHunt)
                {
                    /*将猎物图层置于玩家图层中
                    if (player.graphicsModule != null)
                    {
                        if (otherObject is IDrawable)
                        {
                            player.graphicsModule.AddObjectToInternalContainer(otherObject as IDrawable, 0);
                        }
                        else if (otherObject.graphicsModule != null)
                        {
                            player.graphicsModule.AddObjectToInternalContainer(otherObject.graphicsModule, 0);
                        }
                    }*/
                    this.eatObjects.Add(new CorruptionCat.EatObject(otherChunk, 
                                        Vector2.Distance(this.MiddleOfBody, otherChunk.pos)));
                    player.room.PlaySound(this.SizeClass ? SoundID.Daddy_Digestion_Init : SoundID.Bro_Digestion_Init, myChunk);
                }
            }
        }

        public void Eat()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            Vector2 middleOfBody = this.MiddleOfBody;
            for (int i = this.eatObjects.Count - 1; i >= 0; i--)
            {
                if (this.eatObjects[i].chunk != null &&
                    this.eatObjects[i].chunk.owner != null &&
                    this.eatObjects[i].chunk.owner.slatedForDeletetion)
                {
                    //删除猎物
                    this.eatObjects.RemoveAt(i);
                    continue;
                }
                if (this.eatObjects[i].chunk.owner.room == null ||
                    player.room == null ||
                    this.eatObjects[i].chunk.owner.room != player.room)
                {
                    this.eatObjects.RemoveAt(i);
                    continue;
                }
                /*
                if (this.eatObjects[i].chunk.owner is Creature)
                    (this.eatObjects[i].chunk.owner as Creature).SetKillTag(player.abstractCreature);*/
                if (this.eatObjects[i].progression > 1f )
                {
                    if (this.eatObjects[i].chunk.owner is Creature)
                    {
                        //击杀生物计算
                        //(this.eatObjects[i].chunk.owner as Creature).SetKillTag(player.abstractCreature);
                        if (!this.SizeClass)
                        {
                            this.digestingCounter = (int)Custom.LerpMap((this.eatObjects[i].chunk.owner as Creature).Template.bodySize, 0.2f, 5f, 30f, 300f);
                            //this.digestingCounter = (int)Custom.LerpMap((this.eatObjects[i].chunk.owner as Creature).Template.bodySize, 0.2f, 5f, 50f, 1100f);
                        }
                        //this.AI.tracker.ForgetCreature((this.eatObjects[i].chunk.owner as Creature).abstractCreature);
                        Player otherPlayer = this.eatObjects[i].chunk.owner as Player;
                        if (otherPlayer != null)
                        {
                            otherPlayer.PermaDie();
                        }
                    }
                    //添加食物
                    if (this.eatObjects[i].chunk.owner is IPlayerEdible)
                    {
                        int j;
                        for (j = SlugcatStats.NourishmentOfObjectEaten(SlugcatStats.Name.Red, this.eatObjects[i].chunk.owner as IPlayerEdible); j >= 4; j -= 4)
                        {
                            player.AddFood(1);
                        }
                        while (j > 0)
                        {
                            player.AddQuarterFood();
                            j--;
                        }
                    }
                    else if (this.eatObjects[i].chunk.owner is Creature && (this.eatObjects[i].chunk.owner as Creature).Template.meatPoints > 0)
                        player.AddFood((this.eatObjects[i].chunk.owner as Creature).Template.meatPoints);
                    else if (this.eatObjects[i].chunk.owner is Oracle)
                        player.AddFood(player.MaxFoodInStomach);
                    else if (this.eatObjects[i].chunk.owner is NSHSwarmer)
                        player.AddQuarterFood();
                    else if (this.eatObjects[i].chunk.owner is PhysicalObject)
                        for (int k = 0; k < 4f * (this.eatObjects[i].chunk.owner as PhysicalObject).TotalMass; k++)
                            player.AddQuarterFood();
                    //删除猎物
                    this.eatObjects[i].chunk.owner.Destroy();
                    this.eatObjects.RemoveAt(i);
                }
                else
                {
                    this.eyesClosed = Math.Max(this.eyesClosed, 15);
                    if (this.eatObjects[i].chunk.owner.collisionLayer != 0)
                    {
                        this.eatObjects[i].chunk.owner.ChangeCollisionLayer(0);
                    }
                    if (ModManager.MMF && this.eatObjects[i].chunk.owner is Creature)
                    {
                        (this.eatObjects[i].chunk.owner as Creature).enteringShortCut = null;
                    }
                    float progression = this.eatObjects[i].progression;
                    this.eatObjects[i].progression += 0.0125f;
                    if (progression <= 0.5f && this.eatObjects[i].progression > 0.5f)
                    {
                        if (this.eatObjects[i].chunk.owner is Creature)
                        {
                            (this.eatObjects[i].chunk.owner as Creature).SetKillTag(player.abstractCreature);
                            (this.eatObjects[i].chunk.owner as Creature).Violence(player.mainBodyChunk, new Vector2?(Vector2.zero), this.eatObjects[i].chunk, null, Creature.DamageType.Bite, 100f, 0f);
                            (this.eatObjects[i].chunk.owner as Creature).Die();
                        }
                        if (this.eatObjects[i].chunk.owner is Oracle)
                            (this.eatObjects[i].chunk.owner as Oracle).stun = 200;
                        for (int j = 0; j < this.eatObjects[i].chunk.owner.bodyChunkConnections.Length; j++)
                        {
                            this.eatObjects[i].chunk.owner.bodyChunkConnections[j].type = PhysicalObject.BodyChunkConnection.Type.Pull;
                        }
                    }
                    float num = this.eatObjects[i].distance * (1f - this.eatObjects[i].progression);
                    this.eatObjects[i].chunk.vel *= 0f;
                    this.eatObjects[i].chunk.MoveFromOutsideMyUpdate(true, middleOfBody + Custom.DirVec(middleOfBody, this.eatObjects[i].chunk.pos) * num);
                    for (int k = 0; k < this.eatObjects[i].chunk.owner.bodyChunks.Length; k++)
                    {
                        this.eatObjects[i].chunk.owner.bodyChunks[k].vel *= 1f - this.eatObjects[i].progression;
                        this.eatObjects[i].chunk.owner.bodyChunks[k].MoveFromOutsideMyUpdate(true, Vector2.Lerp(this.eatObjects[i].chunk.owner.bodyChunks[k].pos, middleOfBody + Custom.DirVec(middleOfBody, this.eatObjects[i].chunk.owner.bodyChunks[k].pos) * num, this.eatObjects[i].progression));
                    }
                    if (this.eatObjects[i].chunk.owner.graphicsModule != null && this.eatObjects[i].chunk.owner.graphicsModule.bodyParts != null)
                    {
                        for (int l = 0; l < this.eatObjects[i].chunk.owner.graphicsModule.bodyParts.Length; l++)
                        {
                            this.eatObjects[i].chunk.owner.graphicsModule.bodyParts[l].vel *= 1f - this.eatObjects[i].progression;
                            this.eatObjects[i].chunk.owner.graphicsModule.bodyParts[l].pos = Vector2.Lerp(this.eatObjects[i].chunk.owner.graphicsModule.bodyParts[l].pos, middleOfBody, this.eatObjects[i].progression);
                        }
                    }
                }
            }
        }

        public bool CanEatObjectType(PhysicalObject obj)
        {
            bool result = obj is Oracle || //(obj is Oracle && BuffPoolManager.Instance.GameSetting.MissionId == "DevouringMysteries") ||
                          obj is NSHSwarmer ||
                          obj is OracleSwarmer;
            return result;
        }

        //调整姿势
        public void MovementUpdate(bool eu)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (!player.Consious) return;

            if (player.bodyMode != Player.BodyModeIndex.Swimming &&
                player.bodyMode != Player.BodyModeIndex.ZeroG &&
                player.animation != Player.AnimationIndex.DeepSwim &&
                player.animation != Player.AnimationIndex.SurfaceSwim &&
                player.animation != Player.AnimationIndex.ZeroGSwim)
            {
                player.bodyMode = Player.BodyModeIndex.Default;
                player.animation = Player.AnimationIndex.None;
            }
            //尝试粗暴解决低重力环境下菌形猫在水中下沉的问题
            if (player.Submersion > 0.5f)
            {
                if (player.input[0].y >= 0)
                {
                    for (int i = 0; i < player.bodyChunks.Length; i++)
                        player.bodyChunks[i].vel.y += 1.2f * (player.input[0].y > 0f ? 1.85f : 1f) * (1f - player.room.gravity);
                }
                else
                {
                    for (int i = 0; i < player.bodyChunks.Length; i++)
                        player.bodyChunks[i].vel.y += 0.5f * (1f - player.room.gravity);
                }
            }
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
                else if (player.abstractCreature.creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.TerrorLongLegs)
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
                if (WantToMoveBody)
                {
                    if (HaveDirInput)
                    {
                        bool grabSomething = false;
                        for (int j = 0; j < this.tentacles.Length; j++)
                        {
                            if (this.tentacles[j].grabChunk != null && this.tentacles[j].grabChunk.owner is Creature)
                            {
                                grabSomething = true;
                                break;
                            }
                        }/*
                        //?，或抓住了物品
                        if (WantToHunt || grabSomething)//!player.input[0].pckp || grabSomething
                        {
                            vector = new Vector2?(player.mainBodyChunk.pos + new Vector2((float)player.input[0].x, (float)player.input[0].y) * 40f);
                            //movementConnection = new MovementConnection(type, player.room.GetWorldCoordinate(player.mainBodyChunk.pos), player.room.GetWorldCoordinate(vector.Value), 2);
                        }
                        else
                        {
                            this.moving = false;
                        }*/
                    }
                    else
                    {
                        this.moving = false;
                    }
                    //松开触手抓握，(或需要移动)
                    if ((!player.input[0].thrw && !player.input[0].pckp && player.input[1].thrw) || (!player.input[0].pckp && !player.input[0].thrw && player.input[1].pckp))// || player.input[0].jmp
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
                if (this.WantToAutoHunt)
                {
                    this.unconditionalSupport = 0f;
                    for (int l = 0; l < this.tentacles.Length; l++)
                    {
                        if (this.EnoughGripToMove() && this.tentacles[l].grabChunk == null)
                        {
                            this.tentacles[l].neededForLocomotion = false;
                            this.tentacles[l].atGrabDest = false;
                            this.tentacles[l].SwitchTask(CorruptionCatTentacle.Task.Hunt);
                        }
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
                    if (this.tentacles[num9].atGrabDest && 
                        this.tentacles[num9].huntObj == null && this.tentacles[num9].ReleaseScore() > num7)
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
            float speedChangeFac = 0f;
            float num11 = 0f;
            for (int num12 = 0; num12 < this.tentacles.Length; num12++)
            {
                float grip = Mathf.Pow(this.tentacles[num12].chunksGripping, 0.5f);
                if (this.tentacles[num12].atGrabDest && this.tentacles[num12].grabDest != null)
                {
                    num11 += Mathf.Pow(Mathf.InverseLerp(Custom.LerpMap((float)this.stuckCounter, 0f, 100f, -0.1f, -1f),
                                                         0.85f,
                                                         Vector2.Dot((this.tentacles[num12].floatGrabDest.Value - player.mainBodyChunk.pos).normalized, this.moveDirection)),
                                       0.8f) /
                            (float)this.tentacles.Length;
                    grip = Mathf.Lerp(grip, 1f, 0.75f);
                }
                speedChangeFac += grip / (float)this.tentacles.Length;
            }
            num11 = Mathf.Pow(num11 * speedChangeFac, Custom.LerpMap((float)this.stuckCounter, 100f, 200f, 0.8f, 0.1f));
            speedChangeFac = Mathf.Pow(speedChangeFac, 0.3f);
            num11 = Mathf.Max(num11, this.squeezeFac);
            speedChangeFac = Mathf.Max(speedChangeFac, this.squeezeFac);
            speedChangeFac = Mathf.Max(speedChangeFac, this.unconditionalSupport);
            num11 = Mathf.Max(num11, this.unconditionalSupport);
            float allNeededForLocomotionFac = 0f;
            for (int num15 = 0; num15 < this.tentacles.Length; num15++)
            {
                if (this.tentacles[num15].neededForLocomotion)
                {
                    allNeededForLocomotionFac += 1f / (float)this.tentacles.Length;
                }
            }
            //速度变化 < 未参与移动的触手比例，即速度变化很大，但负责移动的触手较少时执行
            if (speedChangeFac < 1f - allNeededForLocomotionFac)
            {
                float num16 = float.MinValue;
                int num17 = Random.Range(0, this.tentacles.Length);
                for (int num18 = 0; num18 < this.tentacles.Length; num18++)
                {
                    if (!this.tentacles[num18].neededForLocomotion)
                    {
                        float num19 = 1000f / Mathf.Lerp(this.tentacles[num18].idealLength * 
                            (float)player.room.aimap.getTerrainProximity(this.tentacles[num18].Tip.pos), 200f, 0.8f);
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
            //如果速度变化太快，则随机选择触手不参与移动（也许是移动卡住的原因？暂不确定）
            else if (speedChangeFac > 0.85f)
            {
                this.tentacles[Random.Range(0, this.tentacles.Length)].neededForLocomotion = false;
            }

            for (int num20 = 0; num20 < player.bodyChunks.Length; num20++)
            {
                player.bodyChunks[num20].vel *= Mathf.Lerp(1f, Mathf.Lerp(0.95f, 0.8f, this.squeezeFac), speedChangeFac);
                player.bodyChunks[num20].vel.y = player.bodyChunks[num20].vel.y + (player.gravity - player.buoyancy * player.bodyChunks[num20].submersion) * speedChangeFac * num3;
            }/*
            for (int num20 = 0; num20 < this.coreChunks.Length; num20++)
            {
                this.coreChunks[num20].vel *= Mathf.Lerp(1f, Mathf.Lerp(0.95f, 0.8f, this.squeezeFac), speedChangeFac);
                this.coreChunks[num20].vel.y = this.coreChunks[num20].vel.y + (player.gravity - player.buoyancy * this.coreChunks[num20].submersion) * speedChangeFac * num3;
            }*/

            MovementConnection movementConnection2 = default(MovementConnection);
            if (vector != null && 
                Custom.ManhattanDistance(player.abstractCreature.pos, Custom.MakeWorldCoordinate(Room.StaticGetTilePosition(vector.Value), player.abstractCreature.Room.index)) < num2)
            {
                for (int num22 = 0; num22 < player.bodyChunks.Length; num22++)
                {
                    player.bodyChunks[num22].vel += Vector2.ClampMagnitude(player.room.MiddleOfTile(vector.Value) - player.bodyChunks[0].pos, 30f) / 30f * num * num11;
                }/*
                for (int num22 = 0; num22 < this.coreChunks.Length; num22++)
                {
                    this.coreChunks[num22].vel += Vector2.ClampMagnitude(player.room.MiddleOfTile(vector.Value) - this.coreChunks[0].pos, 30f) / 30f * num * num11;
                }*/
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
            this.moving = WantToMoveBody && HaveDirInput;
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
            //BuffPlugin.Log("EnoughGripToMove(): " + EnoughGripToMove());
            this.wantPos = player.bodyChunks[0].lastPos + moveSpeed * new Vector2(player.input[0].x, player.input[0].y);
            this.moveDirection = (wantPos - player.bodyChunks[0].lastPos).normalized;

            float massSpeedFac = 1f;
            if (player.aerobicLevel >= 0.2f && MassFacCondition)
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
            //随机速度
            //player.bodyChunks[0].vel += Custom.RNV() * Random.value * 0.5f;
            //player.bodyChunks[1].vel *= 0.9f;

            for (int n = 0; n < this.tentacles.GetLength(0); n++)
            {
                if (this.tentacles[n].atGrabDest)
                    this.tentacles[n].chooseToMoveByPlayer = false;

                if (this.tentacles[n].chooseToMoveByPlayer &&
                    (!this.HaveDirInput ||
                     (!player.input[0].jmp &&
                      ((this.tentacles[n].grabDest != null &&
                        Custom.Dist(this.tentacles[n].Tip.pos, this.tentacles[n].connectedChunk.pos) > this.tentacles[n].idealLength / 3f &&
                        (this.ChoosedTentaclesCount - this.ChoosedTentaclesButNotControlCount) >= 1) ||
                       this.tentacles[n].chooseToMoveCount >= 150))
                     ))
                    this.tentacles[n].chooseToMoveByPlayerButNotControl = true;

                if (!this.tentacles[n].chooseToMoveByPlayer)
                    this.tentacles[n].chooseToMoveByPlayerButNotControl = false;
            }

            float releaseScore = float.MinValue;
            float secondReleaseScore = float.MinValue;
            int moveIndex = -1;
            int secondMoveIndex = -1;
            for (int num9 = 0; num9 < this.tentacles.Length; num9++)
            {
                if (this.tentacles[num9].huntObj == null &&
                    this.tentacles[num9].ReleaseScoreForAngle() > secondReleaseScore)
                {
                    secondReleaseScore = this.tentacles[num9].ReleaseScoreForAngle();
                    secondMoveIndex = num9;
                }
                if (this.tentacles[num9].atGrabDest && this.tentacles[num9].huntObj == null &&
                    this.tentacles[num9].ReleaseScore() > releaseScore)
                {
                    releaseScore = this.tentacles[num9].ReleaseScore();
                    moveIndex = num9;
                    //secondMoveIndex = moveIndex;
                }
            }
            if (this.ChoosedTentaclesCount < 2 && this.OppositeDirCount >= 1)
            {
                int n = secondMoveIndex > -1 ? secondMoveIndex : Random.Range(0, this.tentacles.GetLength(0));
                this.tentacles[n].chooseToMoveByPlayer = true;
                this.tentacles[n].atGrabDest = false;
            }
            if (moveIndex > -1 &&
                !this.tentacles[moveIndex].atGrabDest && this.tentacles[moveIndex].OppositeDir &&
                this.ChoosedTentaclesCount < 2 && this.OppositeDirCount >= 1)// && this.totalGrip >= 1
            {
                this.tentacles[moveIndex].chooseToMoveByPlayer = true;
                this.tentacles[moveIndex].atGrabDest = false;
            }

            //按住跳跃键时，可以按方向键使核心移动
            if (WantToMoveBody)
            {
                bodyWantPos = wantPos;
                player.bodyChunks[0].vel *= Custom.LerpMap(player.bodyChunks[0].vel.magnitude, 1f, 6f, 0.99f, 0.9f);
                player.bodyChunks[0].vel += Mathf.Clamp01((float)(this.TotalGrip + 2f * this.MoveDirGrip) / 2f) *
                    Vector2.ClampMagnitude(wantPos - player.bodyChunks[0].pos, moveSpeed) / moveSpeed * 3f;
                //在管道附近额外助推
                foreach (var shortcut in player.room.shortcuts)
                {
                    if (Vector2.Dot(player.bodyChunks[0].vel, this.moveDirection) < 0 &&
                        Custom.Dist(player.DangerPos, player.room.MiddleOfTile(shortcut.StartTile)) < 20f)
                    {
                        for (int i = 0; i < player.room.updateList.Count; i++)
                        {
                            if (player.room.updateList[i] is ShortcutHelper)
                            {
                                ShortcutHelper sh = player.room.updateList[i] as ShortcutHelper;
                                foreach (var push in sh.pushers)
                                {
                                    if (push.shortCutPos != shortcut.StartTile)
                                        continue;
                                    //身体卡在了助推点下方)
                                    if (Vector2.Dot(push.pushPos - player.bodyChunks[0].pos, this.moveDirection) > 0)
                                        if (shortcut.shortCutType == ShortcutData.Type.RoomExit || shortcut.shortCutType == ShortcutData.Type.RegionTransportation || shortcut.shortCutType == ShortcutData.Type.Normal)
                                        {
                                            player.bodyChunks[0].vel *= 0f;
                                            player.bodyChunks[0].pos = push.pushPos + 10f * (push.pushPos - player.bodyChunks[0].pos).normalized;
                                        }
                                    break;
                                }
                                break;
                            }
                        }
                    }
                }
            }
            //不按跳跃键时，核心位置不移动
            else
            {
                player.bodyChunks[0].vel *= Custom.LerpMap(player.bodyChunks[0].vel.magnitude, 1f, 6f, 0.99f, 0.9f);
                player.bodyChunks[0].vel += Mathf.Clamp01((float)(this.TotalGrip + 2f * this.MoveDirGrip) / 2f) *
                    Vector2.ClampMagnitude(bodyWantPos - player.bodyChunks[0].pos, moveSpeed) / moveSpeed * 3f;
            }
            if (Custom.Dist(bodyWantPos, wantPos) >= moveSpeed * 2.5f)
                bodyWantPos = wantPos;
        }

        private bool EnoughGripToMove()
        {
            bool result = false;
            if (!ownerRef.TryGetTarget(out var player))
                return false;
            if (this.tentacles == null)
                return false;
            //BuffPlugin.Log("this.totalGrip + 2f * moveDirGrip: " + (this.TotalGrip + 2f * MoveDirGrip));
            if ((this.TotalGrip > this.tentacles.Length / 2 * (player.room.gravity - player.buoyancy * player.Submersion) ||
                 this.TotalGrip + 2f * this.MoveDirGrip >= 5f * (player.room.gravity - player.buoyancy * player.Submersion)) &&
                 this.TotalGrip + 2f * this.MoveDirGrip >= 4f * (player.room.gravity - player.buoyancy * player.Submersion))
                result = true;
            if (this.TotalGrip > 0 && player.room.aimap != null &&
                player.room.aimap.getAItile(player.bodyChunks[0].pos).narrowSpace)
                result = true;

            foreach (var shortcut in player.room.shortcuts)
            {
                if (Custom.Dist(player.DangerPos, player.room.MiddleOfTile(shortcut.StartTile)) < 40f)
                    if (shortcut.shortCutType == ShortcutData.Type.RoomExit || shortcut.shortCutType == ShortcutData.Type.RegionTransportation || shortcut.shortCutType == ShortcutData.Type.Normal)
                        result = true;
            }
            /*
            for (int i = 0; i < player.bodyChunks.Length; i++)
            {
                if ((player.IsTileSolid(i, 1, 0) && player.IsTileSolid(i, -1, 0)) ||
                    (player.IsTileSolid(i, 0, 1) && player.IsTileSolid(i, 0, -1)) ||
                    (player.IsTileSolid(i, 1, 1) && player.IsTileSolid(i, -1, -1)) ||
                    (player.IsTileSolid(i, -1, 1) && player.IsTileSolid(i, 1, -1)))
                    result = true;
            }*/
            return result;
        }

        public MovementConnection CheckTentaclesForAccessibleTerrain()
        {
            if (!ownerRef.TryGetTarget(out var player) || player.room == null)
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
                    if ((EnoughGripToMove() || 
                         (player.room.aimap != null && 
                          player.room.aimap.TileAccessibleToCreature(this.tentacles[i].tChunks[j].pos, StaticWorld.GetCreatureTemplate(CorruptionCat.Type)))) &&//player.room.aimap.TileAccessibleToCreature(this.tentacles[i].tChunks[j].pos, StaticWorld.GetCreatureTemplate(this.Type) && 
                        Custom.DistLess(this.tentacles[i].tChunks[j].pos, vector, num))
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

        public bool CheckDaddyConsumption(PhysicalObject otherObject)
        {
            bool result = false;
            if (otherObject != null)
            {
                if (otherObject is DaddyLongLegs)
                {
                    if (!(otherObject as DaddyLongLegs).SizeClass && this.SizeClass)
                    {
                        result = true;
                    }
                    if (ModManager.MSC && (otherObject as DaddyLongLegs).Template.type != DLCSharedEnums.CreatureTemplateType.TerrorLongLegs &&
                        CorruptionCat.Type == DLCSharedEnums.CreatureTemplateType.TerrorLongLegs)
                    {
                        result = true;
                    }
                }
                else
                {
                    result = (this.SizeClass || otherObject.TotalMass < 5f);
                }
            }
            return result;
        }

        public void NewRoom(Room newRoom)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;/*
            for (int i = 0; i < this.coreChunks.Length; i++)
                this.coreChunks[i].HardSetPosition(player.bodyChunks[0].pos + 6f * Custom.RNV());*/
            foreach (var tentacle in tentacles)
            {
                tentacle.NewRoom(newRoom);
            }
            this.pastPositions = new List<IntVector2>();
            this.wantPos = player.mainBodyChunk.pos;
            this.bodyWantPos = this.wantPos;
        }

        #region 拿东西

        //不可拾取
        public Player.ObjectGrabability Grabability(Player.ObjectGrabability result, PhysicalObject obj)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return result;

            result = Player.ObjectGrabability.CantGrab;

            return result;
        }
        #endregion

        public void KillPlayerUpdate(Oracle oracle)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if ((!player.dead || killFac > 0.5f) && player.room == oracle.room)
            {
                this.killFac += 0.0125f;
                if (this.killFac >= 1f)
                {
                    player.mainBodyChunk.vel += Custom.RNV() * 12f;
                    for (int k = 0; k < 20; k++)
                    {
                        oracle.room.AddObject(new Spark(player.mainBodyChunk.pos, Custom.RNV() * Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                    }
                    player.Die();
                    this.killFac = 0f;
                    return;
                }
            }
        }

        public CreatureTemplate.Relationship DynamicRelationship(AbstractCreature absCrit)//Tracker.CreatureRepresentation rep, 
        {
            if (!ownerRef.TryGetTarget(out var player))
                return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f);

            CreatureTemplate other = StaticWorld.GetCreatureTemplate(absCrit.creatureTemplate.type);
            CreatureTemplate slugpup = StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC);
            
            if (slugpup.relationships[other.type.Index].type == CreatureTemplate.Relationship.Type.Eats)
                return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, slugpup.relationships[other.type.Index].intensity);
            /*
		    if (rep == null)
		    {
			    rep = this.tracker.RepresentationForCreature(absCrit, false);
		    }
		    if (rep == null)
		    {
			    return this.StaticRelationship(absCrit);
		    }
		    if (rep.dynamicRelationship != null)
		    {
			    return rep.dynamicRelationship.currentRelationship;
		    }
		    return this.StaticRelationship(rep.representedCreature);*/
            return StaticWorld.GetCreatureTemplate(CorruptionCat.Type).CreatureRelationship(absCrit.creatureTemplate);
        }

        public class DaddyState : HealthState
        {
            public CorruptionCat owner;
            public float[] tentacleHealth;

            public DaddyState(CorruptionCat owner, AbstractCreature creature) : base(creature)
            {
                this.owner = owner;
                this.tentacleHealth = new float[owner.TentaclesCount];
                for (int i = 0; i < this.tentacleHealth.Length; i++)
                {
                    this.tentacleHealth[i] = 1f;
                }
                creature.creatureTemplate.baseDamageResistance = StaticWorld.GetCreatureTemplate(CorruptionCat.Type).baseDamageResistance / 10f;
                //creature.creatureTemplate.baseStunResistance = StaticWorld.GetCreatureTemplate(CorruptionCat.Type).baseStunResistance / 2f;
                creature.creatureTemplate.damageRestistances[(int)Creature.DamageType.Explosion, 0] = StaticWorld.GetCreatureTemplate(CorruptionCat.Type).damageRestistances[(int)Creature.DamageType.Explosion, 0];
            }
        }

        public class EatObject
        {
            public EatObject(BodyChunk chunk, float distance)
            {
                this.chunk = chunk;
                this.distance = distance;
                this.progression = 0f;
            }

            public BodyChunk chunk;
            public float distance;
            public float progression;
        }
    }

    internal class CorruptionCatGraphics
    {
        public Player player;
        public CorruptionCat corruptionCat;

        #region 外观
        public CorruptionCatLegGraphics[] legGraphics;
        public Color blackColor;
        public Color EffectColorA
        {
            get
            {
                Color color = PlayerGraphics.DefaultSlugcatColor(player.SlugCatClass);
                if (player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Slugpup)
                    color = player.ShortCutColor();
                else if (player.abstractCreature.world != null && player.playerState != null)
                    color = PlayerGraphics.JollyBodyColorMenu(player.SlugCatClass,
                                                              player.abstractCreature.world.game.rainWorld.options.jollyPlayerOptionsArray[player.playerState.playerNumber].playerClass);
                if (player.room != null && player.room.game.cameras[0].spriteLeasers != null)
                {
                    foreach (var spriteLeaser in player.room.game.cameras[0].spriteLeasers)
                    {
                        if (spriteLeaser.drawableObject == player.graphicsModule && spriteLeaser.sprites != null)
                        {
                            color = spriteLeaser.sprites[0].color;
                            break;
                        }
                    }
                }
                return color;
            }
        }
        public Color EffectColorB
        {
            get
            {
                Color color = EffectColorA;
                if (player.isSlugpup)
                {
                    Vector3 hsl = Custom.RGB2HSL(color);
                    if (hsl.z > 0.5f)
                        hsl.z -= 0.2f;
                    else
                        hsl.z += 0.2f;
                    color = Custom.HSL2RGB(hsl.x, hsl.y, hsl.z);
                }
                else
                {
                    if (player.abstractCreature.world != null && player.playerState != null)
                        color = PlayerGraphics.JollyUniqueColorMenu(player.SlugCatClass,
                                                                    player.abstractCreature.world.game.rainWorld.options.jollyPlayerOptionsArray[player.playerState.playerNumber].playerClass,
                                                                    player.playerState.playerNumber);
                    if (color == EffectColorA)
                    {
                        Vector3 hsl = Custom.RGB2HSL(color);
                        if (hsl.z > 0.5f)
                            hsl.z -= 0.2f;
                        else
                            hsl.z += 0.2f;
                        color = Custom.HSL2RGB(hsl.x, hsl.y, hsl.z);
                    }
                }
                return color;
            }
        }
        /*
        public Color EffectColorB => PlayerGraphics.JollyUniqueColorMenu(player.SlugCatClass,
                                                        player.abstractCreature.world.game.rainWorld.options.jollyPlayerOptionsArray[player.playerState.playerNumber].playerClass,
                                                        player.playerState.playerNumber);*/

        public bool SizeClass => this.corruptionCat.SizeClass;
        #endregion

        #region 序号
        public int startSprite;
        public int totalLegSprites;
        public int totalCoreSprites;
        public int totalDanglers;
        public int totalDeadLegSprites;
        public int TotalSprites
        {
            get
            {
                return this.totalLegSprites + this.totalDeadLegSprites + this.totalDanglers + this.corruptionCat.coreChunks.Length * 4;
                //return this.totalLegSprites + this.totalDeadLegSprites + this.totalDanglers + this.player.bodyChunks.Length * (this.daddy.HDmode ? 4 : 3) + (this.daddy.HDmode ? this.dummy.numberOfSprites : 0);
            }
        }

        public int BodySprite(int chunk)
        {
            return this.startSprite + this.totalLegSprites + this.totalDeadLegSprites + this.totalDanglers + chunk;
        }
        public int DanglerSprite(int dangler)
        {
            return this.startSprite + dangler;
        }
        public int DeadLegSprite(int leg)
        {
            return this.startSprite + this.totalDanglers + this.totalLegSprites + leg;
        }
        public int DummySprite()
        {
            return this.startSprite + this.totalLegSprites + this.totalDeadLegSprites + this.totalDanglers + this.corruptionCat.coreChunks.Length * 4;
        }
        public int EyeSprite(int eye, int part)
        {
            return this.startSprite + this.totalLegSprites + this.totalDeadLegSprites + this.totalDanglers + this.corruptionCat.coreChunks.Length + eye * 3 + part;
            //return this.totalLegSprites + this.totalDeadLegSprites + this.totalDanglers + this.player.bodyChunks.Length + eye * (this.daddy.HDmode ? 3 : 2) + part;
        }
        #endregion

        public CorruptionCatGraphics.Eye[] eyes;
        public float[,] chunksRotats;
        public int feelSomethingReactionDelay; 
        public float digesting;

        public List<IndicatorSymbol> indicators;

        public CorruptionCatGraphics(Player player, CorruptionCat corruptionCat)
        {
            this.player = player;
            this.corruptionCat = corruptionCat;
            this.totalLegSprites = 0;
            this.legGraphics = new CorruptionCatLegGraphics[corruptionCat.tentacles.Length];
            this.indicators = new List<IndicatorSymbol>();

            this.chunksRotats = new float[this.corruptionCat.coreChunks.Length, 2];
            this.eyes = new CorruptionCatGraphics.Eye[this.corruptionCat.coreChunks.Length];
            for (int m = 0; m < this.corruptionCat.coreChunks.Length; m++)
            {
                this.chunksRotats[m, 0] = Random.value * 360f;
                this.chunksRotats[m, 1] = Random.value;
                this.eyes[m] = new CorruptionCatGraphics.Eye(this, m);
            }
        }

        #region 外观
        public void ctor(PlayerGraphics self, PhysicalObject ow)
        {
            if (self.internalContainerObjects == null)
                self.internalContainerObjects = new List<GraphicsModule.ObjectHeldInInternalContainer>();
            if (self.drawPositions.GetLength(0) != player.bodyChunks.Length)
            {
                self.drawPositions = new Vector2[player.bodyChunks.Length, 2];
                for (int m = 0; m < player.bodyChunks.Length; m++)
                {
                    self.drawPositions[m, 0] = player.bodyChunks[m].pos;
                    self.drawPositions[m, 1] = player.bodyChunks[m].lastPos;
                }
            }
            this.startSprite = 0;
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            if (self.internalContainerObjects == null)
                self.internalContainerObjects = new List<GraphicsModule.ObjectHeldInInternalContainer>();
            if (self.drawPositions.GetLength(0) != player.bodyChunks.Length)
            {
                self.drawPositions = new Vector2[player.bodyChunks.Length, 2];
                for (int m = 0; m < player.bodyChunks.Length; m++)
                {
                    self.drawPositions[m, 0] = player.bodyChunks[m].pos;
                    self.drawPositions[m, 1] = player.bodyChunks[m].lastPos;
                }
            }
            this.startSprite = 0;
            this.ResetSprites(sLeaser, rCam);
        }

        public void ResetSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            if (this.startSprite != 0)
            {
                for (int i = this.startSprite; i < this.startSprite + this.TotalSprites; i++)
                {
                    sLeaser.sprites[i].isVisible = false;
                }/*
                for (int i = this.startSprite + this.TotalSprites - 1; i >= this.startSprite; i--)
                {
                    sLeaser.sprites[i].RemoveFromContainer();
                    sLeaser.sprites[i] = null;
                }
                for (int i = this.startSprite; i < sLeaser.sprites.Length - this.TotalSprites; i++)
                {
                    sLeaser.sprites[i] = sLeaser.sprites[i + this.TotalSprites];
                }
                Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length - this.TotalSprites);*/
            }

            this.startSprite = sLeaser.sprites.Length;
            this.totalLegSprites = 0;
            for (int i = 0; i < this.legGraphics.Length; i++)
            {
                this.legGraphics[i] = new CorruptionCatLegGraphics(this, i, this.startSprite + this.totalLegSprites);// + 12
                this.totalLegSprites += this.legGraphics[i].sprites;
            }
            this.totalCoreSprites = 4 * this.corruptionCat.coreChunks.Length;
            //this.totalCoreSprites = (this.daddy.HDmode ? 4 : 3) * this.corruptionCat.coreChunks.Length;
            Array.Resize(ref sLeaser.sprites, this.startSprite + this.TotalSprites);
            for (int i = 0; i < this.corruptionCat.coreChunks.Length; i++)
            {
                sLeaser.sprites[this.BodySprite(i)] = new FSprite("Futile_White", true);
                sLeaser.sprites[this.BodySprite(i)].scale = (this.corruptionCat.coreChunks[i].rad * 1.1f + 2f) / 8f;//(this.corruptionCat.coreChunks[i].rad * 1.1f + 2f) / 8f;
                sLeaser.sprites[this.BodySprite(i)].shader = rCam.room.game.rainWorld.Shaders["JaggedCircle"];
                sLeaser.sprites[this.BodySprite(i)].alpha = 0.25f;
                sLeaser.sprites[this.EyeSprite(i, 0)] = this.MakeSlitMesh();
                sLeaser.sprites[this.EyeSprite(i, 1)] = this.MakeSlitMesh();
                if (true)//this.daddy.HDmode
                {
                    sLeaser.sprites[this.EyeSprite(i, 2)] = new FSprite("CorruptGrad", true);
                    sLeaser.sprites[this.EyeSprite(i, 2)].scale = 0.0625f * this.corruptionCat.coreChunks[i].rad * 2f; //0.0625f * this.corruptionCat.coreChunks[i].rad * 2f;
                }
            }
            foreach (var tentacle in this.legGraphics)
            {
                tentacle.InitiateSprites(sLeaser, rCam);
            }/*
            for (int k = 0; k < this.deadLegs.Length; k++)
            {
                this.deadLegs[k].InitiateSprites(sLeaser, rCam);
            }
            for (int l = 0; l < this.danglers.Length; l++)
            {
                this.danglers[l].InitiateSprites(sLeaser, rCam);
            }
            if (this.daddy.HDmode)
            {
                this.dummy.InitiateSprites(sLeaser, rCam);
            }*/
            this.AddToContainer(sLeaser, rCam, null);
            self.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            this.blackColor = palette.blackColor;
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
            if (startSprite >= 1 && sLeaser.sprites.Length >= startSprite + TotalSprites)
            {
                var foregroundContainer = rCam.ReturnFContainer("Foreground");
                var midgroundContainer = newContatiner != null ? newContatiner : rCam.ReturnFContainer("Midground");

                for (int i = 0; i < totalLegSprites; i++)
                {
                    var sprite = sLeaser.sprites[startSprite + this.totalDanglers + i];
                    sprite.RemoveFromContainer();
                    midgroundContainer.AddChild(sprite);
                    //触手移到身体后方
                    sprite.MoveBehindOtherNode(sLeaser.sprites[0]);
                }
                for (int i = 0; i < totalCoreSprites; i++)
                {
                    var sprite = sLeaser.sprites[this.startSprite + this.totalLegSprites + this.totalDeadLegSprites + this.totalDanglers + i];
                    sprite.RemoveFromContainer();
                    midgroundContainer.AddChild(sprite);
                    //一半核心移到身体后方
                    if (i < this.corruptionCat.coreChunks.Length * 4 / 2)
                        sprite.MoveBehindOtherNode(sLeaser.sprites[0]);
                }
            }
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (player.graphicsModule == null || sLeaser == null || player.room == null)
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            //隐藏四肢
            if (sLeaser.sprites.Length >= 9)
                for (int i = 4; i <= 8; i++)
                    sLeaser.sprites[i].isVisible = false;
            //身体
            Vector2 vector = Vector2.Lerp(this.corruptionCat.LastMiddleOfBody, this.corruptionCat.MiddleOfBody, timeStacker);
            for (int j = 0; j < this.corruptionCat.coreChunks.Length; j++)
            {
                Vector2 vector2 = Vector2.Lerp(this.corruptionCat.coreChunks[j].lastPos, this.corruptionCat.coreChunks[j].pos, timeStacker) + Custom.RNV() * this.digesting * 4f * Random.value;
                if (false)//this.player.HDmode && j < 2
                {
                    sLeaser.sprites[this.BodySprite(j)].isVisible = false;
                }
                sLeaser.sprites[this.BodySprite(j)].x = vector2.x - camPos.x;
                sLeaser.sprites[this.BodySprite(j)].y = vector2.y - camPos.y;
                sLeaser.sprites[this.BodySprite(j)].rotation = Custom.AimFromOneVectorToAnother(vector2, vector) + this.chunksRotats[j, 0];
                if (true)//this.player.HDmode
                {
                    sLeaser.sprites[this.EyeSprite(j, 2)].color = Color.Lerp(this.eyes[j].renderColor, Color.black, 0.4f);
                    sLeaser.sprites[this.EyeSprite(j, 2)].x = vector2.x - camPos.x;
                    sLeaser.sprites[this.EyeSprite(j, 2)].y = vector2.y - camPos.y;
                }
                this.RenderSlits(j, vector2, vector, Custom.AimFromOneVectorToAnother(vector2, vector) + this.chunksRotats[j, 0], sLeaser, rCam, timeStacker, camPos);
            }
            //触手
            foreach (var tentacle in legGraphics)
            {
                tentacle.DrawSprite(sLeaser, rCam, timeStacker, camPos);
            }
        }

        public void GraphicsUpdate()
        {
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            //由于此段的存在，GraphicsUpdate在hook中应该位于orig之前
            if ((player.graphicsModule as PlayerGraphics).drawPositions.GetLength(0) != player.bodyChunks.Length)
            {
                (player.graphicsModule as PlayerGraphics).drawPositions = new Vector2[player.bodyChunks.Length, 2];
                for (int m = 0; m < player.bodyChunks.Length; m++)
                {
                    (player.graphicsModule as PlayerGraphics).drawPositions[m, 0] = player.bodyChunks[m].pos;
                    (player.graphicsModule as PlayerGraphics).drawPositions[m, 1] = player.bodyChunks[m].lastPos;
                }
            }

            this.IndicatorSymbolUpdate();
            foreach (var tentacle in legGraphics)
            {
                tentacle.Update();
            }
            if (this.eyes.Length != this.corruptionCat.coreChunks.Length)
            {
                this.chunksRotats = new float[this.corruptionCat.coreChunks.Length, 2];
                this.eyes = new CorruptionCatGraphics.Eye[this.corruptionCat.coreChunks.Length];
                for (int m = 0; m < this.corruptionCat.coreChunks.Length; m++)
                {
                    this.chunksRotats[m, 0] = Random.value * 360f;
                    this.chunksRotats[m, 1] = Random.value;
                    this.eyes[m] = new CorruptionCatGraphics.Eye(this, m);
                }
            }
            for (int l = 0; l < this.corruptionCat.coreChunks.Length; l++)
            {
                this.eyes[l].Update();
            }
            if (this.feelSomethingReactionDelay > 0)
            {
                this.feelSomethingReactionDelay--;
            }
            this.digesting = Mathf.Lerp(this.digesting, Mathf.Clamp(Mathf.Pow(this.corruptionCat.MostDigestedEatObject, 0.5f), 0f, 1f), 0.1f);
        }

        public void Reset(PlayerGraphics self)
        {
            //防止拉丝
            foreach (var tentacle in this.corruptionCat.tentacles)
            {
                tentacle.Reset(tentacle.connectedChunk.pos);
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
                float num3 = Vector2.Dot(Custom.DirVec(middleOfBody, this.corruptionCat.coreChunks[i].pos), Custom.DirVec(middleOfBody, feelPos));
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

        public TriangleMesh MakeSlitMesh()
        {
            TriangleMesh.Triangle[] array = new TriangleMesh.Triangle[8];
            for (int i = 0; i < 8; i++)
            {
                array[i] = new TriangleMesh.Triangle(i, i + 1, i + 2);
            }
            return new TriangleMesh("Futile_White", array, false, false);
        }

        public void RenderSlits(int chunk, Vector2 pos, Vector2 middleOfBody, float rotation, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (false)//this.player.HDmode && chunk < 2
            {
                sLeaser.sprites[this.EyeSprite(chunk, 0)].isVisible = false;
                sLeaser.sprites[this.EyeSprite(chunk, 1)].isVisible = false;
                sLeaser.sprites[this.EyeSprite(chunk, 2)].isVisible = false;
                return;
            }
            float rad = this.corruptionCat.coreChunks[chunk].rad * 1.8f;//this.corruptionCat.coreChunks[chunk].rad * 2f;
            float num = Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(this.eyes[chunk].lastClosed, this.eyes[chunk].closed, timeStacker)), 0.6f);
            float num2 = (this.SizeClass ? 1f : 0.8f) * (1f - num);
            Vector2 vector = Vector2.Lerp(this.eyes[chunk].lastDir, this.eyes[chunk].dir, timeStacker);
            float num3 = Mathf.Lerp(this.eyes[chunk].lastFocus, this.eyes[chunk].focus, timeStacker) * Mathf.Pow(Mathf.InverseLerp(-1f, 1f, Vector2.Dot(Custom.DirVec(middleOfBody, pos), vector.normalized)), 0.7f);
            num3 = Mathf.Max(num3, num);
            float num4 = Mathf.InverseLerp(0f, Mathf.Lerp(30f, 50f, this.chunksRotats[chunk, 1]), Vector2.Distance(middleOfBody, pos + Custom.DirVec(middleOfBody, pos) * rad)) * 0.9f;
            num4 = Mathf.Lerp(num4, 1f, 0.5f * num3);
            Vector2 vector2 = Vector2.Lerp(Custom.DirVec(middleOfBody, pos) * num4, vector, vector.magnitude * 0.5f);
            this.eyes[chunk].centerRenderPos = pos + vector2 * rad;
            this.eyes[chunk].renderColor = Color.Lerp(this.corruptionCat.eyeColor, new Color(1f, 1f, 1f), Mathf.Lerp(Random.value * this.eyes[chunk].light, 1f, num));
            if (num > 0f)
            {
                this.eyes[chunk].renderColor = Color.Lerp(this.eyes[chunk].renderColor, this.blackColor, num);
            }
            this.eyes[chunk].renderColor = Color.Lerp(this.eyes[chunk].renderColor, Color.white, this.eyes[chunk].flash);
            sLeaser.sprites[this.EyeSprite(chunk, 0)].color = this.eyes[chunk].renderColor;
            sLeaser.sprites[this.EyeSprite(chunk, 1)].color = this.eyes[chunk].renderColor;
            for (int i = 0; i < 2; i++)
            {
                Vector2 vector3 = Custom.DegToVec(rotation + 90f * (float)i);
                Vector2 vector4 = Custom.PerpendicularVector(vector3);
                (sLeaser.sprites[this.EyeSprite(chunk, i)] as TriangleMesh).MoveVertice(0, pos + this.BulgeVertex(vector3 * rad * 0.9f * Mathf.Lerp(1f, 0.6f, num3), vector2, rad) - camPos);
                (sLeaser.sprites[this.EyeSprite(chunk, i)] as TriangleMesh).MoveVertice(9, pos + this.BulgeVertex(vector3 * -rad * 0.9f * Mathf.Lerp(1f, 0.6f, num3), vector2, rad) - camPos);
                for (int j = 1; j < 5; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        float num5 = rad * ((j < 3) ? 0.7f : 0.25f) * ((k == 0) ? 1f : -1f) * Mathf.Lerp(1f, 0.6f, num3);
                        int num6 = (k == 0) ? j : (9 - j);
                        float num7 = num2 * ((j < 3) ? 0.5f : 1f) * ((num6 % 2 == 0) ? 1f : -1f) * Mathf.Lerp(1f, 2.5f, num3);
                        (sLeaser.sprites[this.EyeSprite(chunk, i)] as TriangleMesh).MoveVertice(num6, pos + this.BulgeVertex(vector3 * num5 + vector4 * num7, vector2, rad) - camPos);
                    }
                }
            }
        }
        
        public Vector2 BulgeVertex(Vector2 v, Vector2 dir, float rad)
        {
            return Vector2.Lerp(v, Vector2.ClampMagnitude(v + dir * rad, rad), dir.magnitude);
        }

        public void IndicatorSymbolUpdate()
        {
            //玩家房间为空，或失明特效为空
            if (this.player.room == null || CorruptionShapedMutationBuff.Instance.blindWaveEffectManager == null)
            {
                foreach (var indicator in this.indicators)
                {
                    indicator.Destroy();
                    this.player.room.RemoveObject(indicator);
                }
                this.indicators.Clear();
                return;
            }
            //生物离开房间
            for (int i = this.indicators.Count - 1; i >= 0; i--)
            {
                if (this.indicators[i].creature == null ||
                    this.indicators[i].creature.room == null ||
                    this.indicators[i].creature.room != this.player.room ||
                    this.indicators[i].alpha <= 0)
                {
                    this.indicators[i].Destroy();
                    this.player.room.RemoveObject(this.indicators[i]);
                    this.indicators.Remove(this.indicators[i]);
                }
            }
            Vector2 roomCenter = new Vector2(Custom.rainWorld.options.ScreenSize.x / 2f, Custom.rainWorld.options.ScreenSize.y / 2f);
            Vector2 camPos = player.room.game.cameras[0].pos;
            foreach (var abscreature in this.player.room.abstractRoom.creatures)
            {
                if (abscreature.realizedCreature == null || abscreature.realizedCreature == this.player)
                    continue;

                var creature = abscreature.realizedCreature;
                bool shouldGoToNext = false;
                foreach (var indicator in this.indicators)
                    if (indicator.creature == creature)
                    {
                        shouldGoToNext = true;
                        break;
                    }
                if (shouldGoToNext)
                    continue;

                bool inScreen = creature.DangerPos.x - camPos.x > roomCenter.x - Custom.rainWorld.options.ScreenSize.x / 2f &&
                                creature.DangerPos.x - camPos.x < roomCenter.x + Custom.rainWorld.options.ScreenSize.x / 2f &&
                                creature.DangerPos.y - camPos.y > roomCenter.y - Custom.rainWorld.options.ScreenSize.y / 2f &&
                                creature.DangerPos.y - camPos.y < roomCenter.y + Custom.rainWorld.options.ScreenSize.y / 2f;
                if (inScreen)
                    continue;

                for (int i = 0; i < CorruptionShapedMutationBuff.Instance.blindWaveEffectManager.activeWaveObjs.Count; i++)
                {
                    bool inSoundRange = false;
                    for (int j = 0; j < creature.bodyChunks.Length; j++)
                    {
                        inSoundRange = Custom.Dist(CorruptionShapedMutationBuff.Instance.blindWaveEffectManager.activeWaveObjs[i].pos,
                                                        creature.bodyChunks[j].pos) <
                                                        CorruptionShapedMutationBuff.Instance.blindWaveEffectManager.activeWaveObjs[i].rad + creature.bodyChunks[j].rad;
                        if (inSoundRange)
                            break;
                    }

                    if (inSoundRange)
                    {
                        var indicator = new IndicatorSymbol(this.player, creature, this.player.room);
                        this.indicators.Add(indicator);
                        this.player.room.AddObject(indicator);
                        BuffPlugin.Log($"[CorruptionShapedMutation] Add indicators, now count: {this.indicators.Count}");
                        break;
                    }
                }
            }
        }

        public class Eye : DaddyBubbleOwner
        {
            public BodyChunk chunk
            {
                get
                {
                    return this.owner.corruptionCat.coreChunks[this.index];
                }
            }

            public Color GetEyeColor()
            {
                return renderColor;
            }

            public Eye(CorruptionCatGraphics owner, int index)
            {
                this.index = index;
                this.owner = owner;
                this.dir = new Vector2(0f, 0f);
                this.lastDir = new Vector2(0f, 0f);
                this.centerRenderPos = owner.corruptionCat.coreChunks[index].pos;
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
        public int chooseToMoveCount;

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
        public PhysicalObject huntObj;
        //public Tracker.CreatureRepresentation huntObj;
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
        public bool changeIdealGrabPos;

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
            //BuffPlugin.Log($"[CorruptionShapedMutation] this.task[{this.tentacleNumber}]: " + (this.task == null ? "null" : this.task.ToString()));
            //BuffPlugin.Log($"[CorruptionShapedMutation] huntObj[{this.tentacleNumber}]: "+ (this.huntObj == null ? "null" : this.huntObj.abstractPhysicalObject.type.ToString()));
            //BuffPlugin.Log($"[CorruptionShapedMutation] grabChunk[{this.tentacleNumber}]: " + (this.grabChunk == null ? "null" : this.grabChunk.owner.abstractPhysicalObject.type.ToString()));
            base.Update();
            
            if (chooseToMoveByPlayer)
                chooseToMoveCount++;
            else
                chooseToMoveCount = 0;
            //抓取小生物时，小生物会眩晕
            if (this.grabChunk != null && this.grabChunk.owner is Creature creature && creature.Template.smallCreature)
                creature.Stun(20);
            //一些应松手的情况
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
            }
            // 触手受击眩晕
            if (Mathf.Pow(Random.value, 0.35f) > (this.corruptionCat.state as CorruptionCat.DaddyState).tentacleHealth[this.tentacleNumber])
            {
                this.stun = Math.Max(this.stun, (int)Mathf.Lerp(-4f, 14f, 
                    Mathf.Pow(Random.value, 0.5f + 20f * Mathf.Max(0f, (this.corruptionCat.state as CorruptionCat.DaddyState).tentacleHealth[this.tentacleNumber]))));
            }
            if (this.grabChunk != null)
            {
                float num = Vector2.Distance(base.Tip.pos, this.grabChunk.pos);
                float num2 = (base.Tip.rad + ClampRad(this.grabChunk.rad)) / 4f;
                Vector2 vector = Custom.DirVec(base.Tip.pos, this.grabChunk.pos);
                float num3 = ClampMass(this.grabChunk.mass) / (ClampMass(this.grabChunk.mass) + 0.01f);
                float num4 = 1f;
                //this.grabChunk.vel *= 0.9f; // 试图防止物体乱飞
                base.Tip.pos += vector * (num - num2) * num3 * num4;// * SpeedFac;
                base.Tip.vel += vector * (num - num2) * num3 * num4;// * SpeedFac;
                this.grabChunk.pos -= vector * (num - num2) * (1f - num3) * num4;// * SpeedFac;
                this.grabChunk.vel -= vector * (num - num2) * (1f - num3) * num4;// * SpeedFac;
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
            Vector2 bodyPos = this.player.firstChunk.pos;
            for (int k = 1; k < this.player.bodyChunks.Length; k++)
            {
                bodyPos += this.player.bodyChunks[k].pos;
            }
            bodyPos /= (float)this.player.bodyChunks.Length;
            this.awayFromBodyRotation = Custom.AimFromOneVectorToAnother(bodyPos, this.connectedChunk.pos);
            this.chunksGripping = 0f;
            //TODO：寻找猎物
            if (!this.neededForLocomotion)
            {
                bool flag = this.corruptionCat.WantToHunt;
                //bool flag = !this.player.safariControlled || (this.player.inputWithDiagonals != null && this.player.inputWithDiagonals.Value.pckp);
                if (this.task != CorruptionCatTentacle.Task.Grabbing && flag)
                {
                    this.LookForCreaturesToHunt();
                    if (this.huntObj == null && this.checkSound == null)
                    {
                        this.LookForSoundsToExamine();
                    }
                }
            }
            else if (this.task != CorruptionCatTentacle.Task.Locomotion)
            {
                this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
            }
            if (this.task == CorruptionCatTentacle.Task.Hunt && (this.huntObj == null || this.huntObj.slatedForDeletetion))
            {
                this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
            }
            else if (this.task != CorruptionCatTentacle.Task.Hunt && this.huntObj != null)
            {
                this.huntObj = null;
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
                if (!this.corruptionCat.WantToHunt)
                    this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
                this.Hunt(ref this.scratchPath);
            }
            else if (this.task == CorruptionCatTentacle.Task.ExamineSound)
            {
                this.ExamineSound(ref this.scratchPath);
            }
            else if (this.task == CorruptionCatTentacle.Task.Grabbing)
            {
                base.MoveGrabDest(bodyPos + Custom.DirVec(bodyPos, this.grabChunk.pos) * 20f, ref this.scratchPath);
                Vector2 p = bodyPos;
                bool flag2 = this.room.VisualContact(this.grabChunk.pos, bodyPos);
                if (!this.corruptionCat.WantToHunt)
                    this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
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
                    if ((this.tChunks[l].phase > -1f || this.room.GetTile(this.tChunks[l].pos).Solid))// && this.grabChunk != null && this.grabChunk.owner is Creature)
                    {
                        this.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Release_Creature, this.grabChunk.pos);
                        this.grabChunk = null;
                        this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
                        break;
                    }
                }
                if (this.task == CorruptionCatTentacle.Task.Grabbing)
                {
                    this.grabChunk.vel += (Vector2)Vector3.Slerp(Custom.DirVec(this.grabChunk.pos, p), 
                                                                 Custom.DirVec(base.Tip.pos, this.tChunks[this.tChunks.Length - 2].pos), 
                                                                 0.5f) *
                        Custom.LerpMap((float)this.grabPath.Count, 3f, 18f, 0.65f, 0.25f) / ClampMass(this.grabChunk.mass) * SpeedFac;

                    //this.grabChunk.vel += (Vector2)Vector3.Slerp(Custom.DirVec(this.grabChunk.pos, p), Custom.DirVec(base.Tip.pos, this.tChunks[this.tChunks.Length - 2].pos), 0.5f) * 
                    //    Custom.LerpMap((float)this.grabPath.Count, 3f, 18f, 0.65f, 0.25f) * (this.corruptionCat.SizeClass ? 1f : 0.45f) / this.grabChunk.mass;
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
                //限位
                if (Custom.Dist(this.tChunks[m].pos, this.connectedChunk.pos) > num5 * this.idealLength * 1.25f)
                {
                    this.tChunks[m].pos = this.connectedChunk.pos + 
                        Custom.DegToVec(Custom.AimFromOneVectorToAnother(this.connectedChunk.pos, this.tChunks[m].pos)) * num5 * this.idealLength * 1.25f;
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
            if (this.corruptionCat.WantToMoveBody && this.corruptionCat.HaveDirInput)
            {
                t = Mathf.Clamp01(Mathf.Pow(t, 0.4f));
                if (this.OppositeDir)
                    t = Mathf.Clamp01(Mathf.Pow(t, 0.2f));
            }
            if (this.chooseToMoveByPlayer && !this.chooseToMoveByPlayerButNotControl &&
                !this.corruptionCat.WantToMoveBody && this.corruptionCat.HaveDirInput)
                t = 1f; 
            Vector2 moveDirection = (this.chooseToMoveByPlayer || this.corruptionCat.WantToMoveBody) ? this.corruptionCat.moveDirection : this.tentacleDir;
            this.idealGrabPos = base.FloatBase + (Vector2)Vector3.Slerp(this.tentacleDir, moveDirection, t) * this.idealLength * 0.7f;

            if (!this.atGrabDest && (this.foundNoGrabPos > 40 || this.changeIdealGrabPos) &&
                (this.Tip.contactPoint.x != 0 || this.Tip.contactPoint.y != 0))
            {
                float num2 = Custom.AimFromOneVectorToAnother(this.player.mainBodyChunk.pos, this.idealGrabPos);
                float num3 = Custom.AimFromOneVectorToAnother(this.player.mainBodyChunk.pos, this.Tip.pos);
                if (Mathf.Abs(Mathf.DeltaAngle(num2, num3)) < 22.5f)
                {
                    this.changeIdealGrabPos = true;
                    this.idealGrabPos = this.Tip.pos;
                }
                else
                {
                    this.changeIdealGrabPos = false;
                }
            }

            Vector2 actualGrabPos = base.FloatBase +
                (Vector2)Vector3.Slerp((Vector2)Vector3.Slerp(this.tentacleDir, moveDirection, t), Custom.RNV(), Mathf.InverseLerp(20f, 200f, (float)this.foundNoGrabPos)) *
                this.idealLength * Custom.LerpMap((float)Math.Max(this.foundNoGrabPos, this.corruptionCat.stuckCounter), 20f, 200f, 0.7f, 1.2f);
            int i;
            for (i = SharedPhysics.RayTracedTilesArray(base.FloatBase, actualGrabPos, this._cachedRays1); 
                 i >= this._cachedRays1.Length; 
                 i = SharedPhysics.RayTracedTilesArray(base.FloatBase, actualGrabPos, this._cachedRays1))
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
                    this.ConsiderGrabPos(Custom.RestrictInRect(actualGrabPos, this.room.TileRect(this._cachedRays1[j]).Shrink(1f)), this.idealGrabPos);
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
                bool orig = this.backtrackFrom == -1 || this.backtrackFrom > k;
                bool notControl = !(this.chooseToMoveByPlayer && !this.chooseToMoveByPlayerButNotControl);
                if (notControl && orig)
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
            if (base.grabDest == null || !this.atGrabDest)
            {
                this.UpdateClimbGrabPos(ref path);
            }
            if (this.chooseToMoveByPlayer && !this.chooseToMoveByPlayerButNotControl)
            {
                this.neededForLocomotion = true;
                this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
                for (int k = 0; k < this.tChunks.Length; k++)
                {
                    this.tChunks[k].vel += Vector2.ClampMagnitude(actualGrabPos - this.tChunks[k].pos, 20f) / 20f * 1.2f * 1.5f * SpeedFac;
                    if (k == this.tChunks.Length - 1)
                    {
                        this.tChunks[k].vel += Vector2.ClampMagnitude(actualGrabPos - this.tChunks[k].pos, 20f) / 20f * 3f * 1.5f * SpeedFac;
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
            Vector2 actualGrabPos = base.FloatBase + Vector3.Slerp(dir, Custom.RNV(), Mathf.InverseLerp(20f, 200f, (float)this.foundNoGrabPos)).ToVector2InPoints() * this.idealLength * Custom.LerpMap((float)Math.Max(this.foundNoGrabPos, 0), 20f, 200f, 0.7f, 1.2f);
            List<IntVector2> list = new List<IntVector2>();
            SharedPhysics.RayTracedTilesArray(base.FloatBase, actualGrabPos, list);
            bool flag = false;
            for (int i = 0; i < list.Count - 1; i++)
            {
                if (this.room.GetTile(list[i + 1]).Solid)
                {
                    ConsiderGrabPos(Custom.RestrictInRect(actualGrabPos, this.room.TileRect(list[i]).Shrink(1f)), this.idealGrabPos);
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
            //if (this.floatGrabDest != null && !Custom.DistLess(chunk.pos, this.floatGrabDest.Value, this.idealLength * 0.3f))
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

        public float ReleaseScoreForAngle()
        {
            float num = float.MaxValue;
            for (int i = this.tChunks.Length / 2; i < this.tChunks.Length; i++)
            {
                if (Custom.DistLess(this.tChunks[i].pos, this.idealGrabPos, num))
                {
                    num = Vector2.Distance(this.tChunks[i].pos, this.idealGrabPos);
                }
            }
            num *= this.atGrabDest ? 1f : 1.2f;
            num *= Custom.LerpMap(Vector2.Dot(this.corruptionCat.moveDirection, this.Tip.pos - connectedChunk.pos), -1f, 1f, 2f, 1f);
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
            if (this.corruptionCat.WantToAutoHunt && this.huntObj != null && this.task == Task.Hunt)
                return;
            if (this.corruptionCat.TotalGrip <= 2 && this.atGrabDest)
                return;
            base.MoveGrabDest(this.preliminaryGrabDest, ref path);
        }

        #region 捕猎
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
                    if (this.player.abstractCreature != this.player.room.abstractRoom.creatures[i] && 
                        this.player.room.abstractRoom.creatures[i].realizedCreature != null &&
                        this.corruptionCat.DynamicRelationship(this.player.room.abstractRoom.creatures[i]).type == CreatureTemplate.Relationship.Type.Eats)
                    {
                        float num3 = Custom.AimFromOneVectorToAnother(this.player.mainBodyChunk.pos, this.player.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos);
                        float num4 = Custom.Dist(this.player.mainBodyChunk.pos, this.player.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos);
                        if ((Mathf.Abs(Mathf.DeltaAngle(num2, num3)) < 22.5f || this.huntDirection == Vector2.zero || creature == null) && num4 < num)
                        {
                            num = num4;
                            creature = this.player.room.abstractRoom.creatures[i].realizedCreature;
                        }
                    }
                }
                if (creature != null)
                {
                    this.huntObj = creature;
                    //creatureRepresentation = this.player.AI.tracker.RepresentationForCreature(creature.abstractCreature, true);
                }
                else if (this.player.room.physicalObjects != null)
                {
                    PhysicalObject obj = null;
                    num = float.MaxValue;
                    num2 = Custom.VecToDeg(this.huntDirection);
                    for (int i = 0; i < this.player.room.physicalObjects.Length; i++)
                    {
                        if (this.room.physicalObjects[i] == null)
                            continue;
                        for (int j = 0; j < this.room.physicalObjects[i].Count; j++)
                        {
                            //判断物品是否应该被抓
                            if (this.room.physicalObjects[i][j] != null && 
                                !(this.room.physicalObjects[i][j] is Creature) &&
                                this.corruptionCat.CanEatObjectType(this.room.physicalObjects[i][j]))
                            {
                                float num3 = Custom.AimFromOneVectorToAnother(this.player.mainBodyChunk.pos, this.room.physicalObjects[i][j].bodyChunks[0].pos);
                                float num4 = Custom.Dist(this.player.mainBodyChunk.pos, this.room.physicalObjects[i][j].bodyChunks[0].pos);
                                if ((Mathf.Abs(Mathf.DeltaAngle(num2, num3)) < 22.5f || this.huntDirection == Vector2.zero || obj == null) && num4 < num)
                                {
                                    num = num4;
                                    obj = this.room.physicalObjects[i][j];
                                }
                            }
                        }
                    }
                    if (obj != null)
                    {
                        this.huntObj = obj;
                    }
                }
            }
            if (this.huntObj != null)
            {
                return;
            }
            for (int j = 0; j < this.corruptionCat.tentacles.Length; j++)
            {
                if (this.corruptionCat.tentacles[j].huntObj == this.huntObj)
                {
                    return;
                }
            }
            if (this.IsObjCaughtEnough(this.huntObj.abstractPhysicalObject))
            {
                return;
            }
            if (this.huntObj.abstractPhysicalObject.pos.room != this.player.abstractCreature.pos.room)
            {
                return;
            }
            if (Vector2.Distance(this.room.MiddleOfTile(this.huntObj.abstractPhysicalObject.pos), base.FloatBase) > this.idealLength + 40f)
            {
                return;
            }
            if (this.checkSound != null)
            {
                this.checkSound.Destroy();
                this.checkSound = null;
            }
            //this.huntObj = creatureRepresentation;
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
                        if (this.corruptionCat.tentacles[num3].checkSound == this.player.AI.noiseTracker.sources[i] || (this.player.AI.noiseTracker.sources[i].creatureRep != null && this.corruptionCat.tentacles[num3].huntObj == this.player.AI.noiseTracker.sources[i].creatureRep))
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

        public bool IsObjCaughtEnough(AbstractPhysicalObject abobj)
        {
            int num = 0;
            for (int i = 0; i < this.corruptionCat.tentacles.Length; i++)
            {
                if (this.corruptionCat.tentacles[i].grabChunk != null &&
                    this.corruptionCat.tentacles[i].grabChunk.owner.abstractPhysicalObject == abobj)
                {
                    num++;
                }
            }
            //生物需要检查体型
            if (abobj is AbstractCreature crit)
            {
                return (float)num >= Mathf.Max(1f, crit.creatureTemplate.bodySize * (this.corruptionCat.SizeClass ? 1.5f : 2.5f));
            }
            //非生物需要检查重量
            return (float)num >= Mathf.Max(1f, abobj.realizedObject.TotalMass * 0.2f * (this.corruptionCat.SizeClass ? 1.5f : 2.5f));
        }

        public void Hunt(ref List<IntVector2> path)
        {
            if (this.huntObj.abstractPhysicalObject.pos.room != this.player.abstractCreature.pos.room || this.huntObj.slatedForDeletetion)
            {
                this.huntObj = null;
                this.SwitchTask(CorruptionCatTentacle.Task.Locomotion);
                return;
            }
            if (VisualContact(huntObj))
            {
                if (huntObj is Creature)
                    base.MoveGrabDest((this.huntObj as Creature).mainBodyChunk.pos, ref path);
                else
                    base.MoveGrabDest(this.huntObj.bodyChunks[0].pos, ref path);
            }
            else
            {
                if (this.huntObj.abstractPhysicalObject.pos.TileDefined)
                {
                    base.MoveGrabDest(this.room.MiddleOfTile(this.huntObj.abstractPhysicalObject.pos), ref path);
                }/*
                for (int i = 0; i < this.tChunks.Length; i++)
                {
                    if (this.huntObj is Tracker.ElaborateCreatureRepresentation)
                    {
                        for (int j = 0; j < (this.huntObj as Tracker.ElaborateCreatureRepresentation).ghosts.Count; j++)
                        {
                            if (this.room.GetTilePosition(this.tChunks[i].pos) == (this.huntObj as Tracker.ElaborateCreatureRepresentation).ghosts[j].coord.Tile)
                            {
                                (this.huntObj as Tracker.ElaborateCreatureRepresentation).ghosts[j].Push();
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
                    if (this.corruptionCat.tentacles[k].task == CorruptionCatTentacle.Task.Locomotion && 
                        !this.corruptionCat.tentacles[k].neededForLocomotion && 
                        (this.corruptionCat.tentacles[k].idealLength > this.idealLength || this.neededForLocomotion) && 
                        !this.corruptionCat.tentacles[k].atGrabDest && 
                        Mathf.Abs(this.corruptionCat.tentacles[k].idealLength - (float)this.grabPath.Count * 20f) < num)
                    {
                        num = Mathf.Abs(this.corruptionCat.tentacles[k].idealLength - (float)this.grabPath.Count * 20f);
                        num2 = k;
                    }
                }
                if (num2 > -1)
                {
                    this.corruptionCat.tentacles[num2].huntObj = this.huntObj;
                    this.corruptionCat.tentacles[num2].task = CorruptionCatTentacle.Task.Hunt;
                    this.huntObj = null;
                    this.UpdateClimbGrabPos(ref path);
                    return;
                }
            }
            if (Vector2.Distance(this.room.MiddleOfTile(this.huntObj.abstractPhysicalObject.pos), base.FloatBase) > this.idealLength * 1.5f)
            {
                this.huntObj = null;
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
        #endregion

        public void SwitchTask(Task newTask)
        {
            task = newTask;
        }

        public void Touch()
        {
            bool flag = false;
            bool wantTouch = this.corruptionCat.WantToHunt;//this.player.inputWithDiagonals != null && this.player.inputWithDiagonals.Value.pckp;
            for (int i = 0; i < this.room.abstractRoom.creatures.Count; i++)
            {
                //判断生物是否应该被抓
                if (this.room.abstractRoom.creatures[i].realizedCreature != null &&
                    !this.room.abstractRoom.creatures[i].realizedCreature.inShortcut && //不在管道
                    this.room.abstractRoom.creatures[i].realizedCreature != this.player && //不是玩家自己
                    !this.room.abstractRoom.creatures[i].tentacleImmune && //不免疫触手
                    wantTouch)
                {
                    Creature realizedCreature = this.room.abstractRoom.creatures[i].realizedCreature;
                    for (int j = 0; j < this.tChunks.Length; j++)
                    {
                        int m = 0;
                        while (m < realizedCreature.bodyChunks.Length)
                        {
                            //如果触手和生物的距离足够近
                            if (Custom.DistLess(this.tChunks[j].pos, realizedCreature.bodyChunks[m].pos, this.tChunks[j].rad + ClampRad(realizedCreature.bodyChunks[m].rad)))
                            {
                                /* 外观反应
                                if (this.corruptionCat.eyesClosed < 1 || Random.value < 0.05f)
                                {
                                    this.player.AI.tracker.SeeCreature(realizedCreature.abstractCreature);
                                    if (this.corruptionCat.graphics != null)
                                    {
                                        Creature creatureRep = realizedCreature;
                                        (this.corruptionCat.graphics as CorruptionCatGraphics).FeelSomethingWithTentacle(creatureRep, this.tChunks[j].pos)
                                    }
                                }*/
                                //生物意识到玩家的存在
                                if (realizedCreature.abstractCreature.creatureTemplate.AI &&
                                    realizedCreature.abstractCreature.abstractAI.RealAI != null &&
                                    realizedCreature.abstractCreature.abstractAI.RealAI.tracker != null)
                                {
                                    realizedCreature.abstractCreature.abstractAI.RealAI.tracker.SeeCreature(this.player.abstractCreature);
                                }
                                    this.CollideWithObject(j, realizedCreature.bodyChunks[m]);
                                if (!this.neededForLocomotion && //触手不需要移动
                                    realizedCreature.newToRoomInvinsibility < 1 && //生物刚到房间的不可见性 < 1
                                    this.grabChunk == null && //触手还没有抓住东西
                                    j == this.tChunks.Length - 1 && //这一节触手是触手尖端
                                    (this.corruptionCat.SizeClass || this.corruptionCat.digestingCounter < 1) && //如果腐化足够大，或者消化时间结束
                                    (this.corruptionCat.eyesClosed < 1 || Random.value < (this.corruptionCat.SizeClass ? 0.5f : 0.15f)) &&
                                    (this.task == CorruptionCatTentacle.Task.Hunt || !this.IsObjCaughtEnough(realizedCreature.abstractCreature)))//触手在捕猎状态，或生物没有被充分抓住
                                {
                                    flag = true;
                                    if (Vector2.Distance(this.tChunks[j].vel, realizedCreature.bodyChunks[m].vel) >= Mathf.Lerp(1f, 8f, this.sticky))
                                    {
                                        break;
                                    }
                                    bool canEat = false;
                                    if (realizedCreature != null &&
                                        this.corruptionCat.DynamicRelationship(realizedCreature.abstractCreature).type == CreatureTemplate.Relationship.Type.Eats)
                                    {
                                        canEat = true;
                                    }
                                    int num = 0;
                                    while (num < this.tChunks.Length && canEat)
                                    {
                                        if (this.tChunks[num].phase > -1f || this.room.GetTile(this.tChunks[num].pos).Solid)
                                        {
                                            canEat = false;
                                        }
                                        num++;
                                    }
                                    if (canEat)
                                    {
                                        this.grabChunk = realizedCreature.bodyChunks[m];
                                        this.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Grab_Creature, this.tChunks[j].pos, 1f, 1f);
                                        this.SwitchTask(CorruptionCatTentacle.Task.Grabbing);
                                        return;
                                    }
                                    break;
                                }
                                else
                                {
                                    if (this.neededForLocomotion || (!(this.task == CorruptionCatTentacle.Task.Locomotion) && !(this.task == CorruptionCatTentacle.Task.ExamineSound)) || this.IsObjCaughtEnough(realizedCreature.abstractCreature))
                                    {
                                        break;
                                    }
                                    Creature creatureRepresentation = realizedCreature;
                                    if (creatureRepresentation == null ||
                                        !(this.corruptionCat.DynamicRelationship(creatureRepresentation.abstractCreature).type == CreatureTemplate.Relationship.Type.Eats))
                                    {
                                        break;
                                    }
                                    bool flag4 = false;
                                    int num2 = 0;
                                    while (num2 < this.corruptionCat.tentacles.Length && !flag4)
                                    {
                                        if (this.corruptionCat.tentacles[num2].huntObj == creatureRepresentation)
                                        {
                                            flag4 = true;
                                        }
                                        num2++;
                                    }
                                    if (!flag4)
                                    {
                                        this.huntObj = creatureRepresentation;
                                        if (this.checkSound != null)
                                        {
                                            this.checkSound.Destroy();
                                            this.checkSound = null;
                                        }
                                        this.SwitchTask(CorruptionCatTentacle.Task.Hunt);
                                        break;
                                    }
                                    break;
                                }
                            }
                            else
                            {
                                m++;
                            }
                        }
                    }
                }
            }
            if (this.room.physicalObjects != null && this.grabChunk == null)
            {
                for (int i = 0; i < this.room.physicalObjects.Length; i++)
                {
                    if (this.room.physicalObjects[i] == null)
                        continue;
                    for (int j = 0; j < this.room.physicalObjects[i].Count; j++)
                    {
                        //判断物品是否应该被抓
                        if (this.room.physicalObjects[i][j] != null &&
                            !(this.room.physicalObjects[i][j] is Creature) &&
                            wantTouch)
                        {
                            PhysicalObject obj = this.room.physicalObjects[i][j];
                            for (int k = 0; k < this.tChunks.Length; k++)
                            {
                                int m = 0;
                                while (m < obj.bodyChunks.Length)
                                {
                                    //如果触手和物品的距离足够近
                                    if (Custom.DistLess(this.tChunks[k].pos, obj.bodyChunks[m].pos, this.tChunks[k].rad + ClampRad(obj.bodyChunks[m].rad)))
                                    {
                                        /* 外观反应
                                        if (this.corruptionCat.eyesClosed < 1 || Random.value < 0.05f)
                                        {
                                            this.player.AI.tracker.SeeCreature(obj.abstractCreature);
                                            if (this.corruptionCat.graphics != null)
                                            {
                                                Creature creatureRep = obj;
                                                (this.corruptionCat.graphics as CorruptionCatGraphics).FeelSomethingWithTentacle(creatureRep, this.tChunks[k].pos)
                                            }
                                        }*/
                                        if (Custom.DistLess(this.tChunks[k].pos, obj.bodyChunks[m].pos, this.tChunks[k].rad + obj.bodyChunks[m].rad))
                                            this.CollideWithObject(k, obj.bodyChunks[m]);
                                        if (!this.neededForLocomotion && //触手不需要移动
                                            this.grabChunk == null && //触手还没有抓住东西
                                            k == this.tChunks.Length - 1 && //这一节触手是触手尖端
                                            (this.corruptionCat.SizeClass || this.corruptionCat.digestingCounter < 1) && //如果腐化足够大，或者消化时间结束
                                            (this.corruptionCat.eyesClosed < 1 || Random.value < (this.corruptionCat.SizeClass ? 0.5f : 0.15f)) &&
                                            (this.task == CorruptionCatTentacle.Task.Hunt || !this.IsObjCaughtEnough(obj.abstractPhysicalObject)))//触手在捕猎状态，或生物没有被充分抓住
                                        {
                                            flag = true;
                                            if (Vector2.Distance(this.tChunks[k].vel, obj.bodyChunks[m].vel) >= Mathf.Lerp(1f, 8f, this.sticky))
                                            {
                                                break;
                                            }
                                            bool canEat = false;
                                            if (obj != null && this.corruptionCat.CanEatObjectType(obj))
                                            {
                                                canEat = true;
                                            }
                                            int num = 0;
                                            while (num < this.tChunks.Length && canEat)
                                            {
                                                if (this.tChunks[num].phase > -1f || this.room.GetTile(this.tChunks[num].pos).Solid)
                                                {
                                                    canEat = false;
                                                }
                                                num++;
                                            }
                                            if (canEat)
                                            {
                                                this.grabChunk = obj.bodyChunks[m];
                                                this.room.PlaySound(SoundID.Daddy_And_Bro_Tentacle_Grab_Creature, this.tChunks[k].pos, 1f, 1f);
                                                this.SwitchTask(CorruptionCatTentacle.Task.Grabbing);
                                                return;
                                            }
                                            break;
                                        }
                                        else
                                        {
                                            if (this.neededForLocomotion || 
                                                (!(this.task == CorruptionCatTentacle.Task.Locomotion) && 
                                                 !(this.task == CorruptionCatTentacle.Task.ExamineSound)) || 
                                                this.IsObjCaughtEnough(obj.abstractPhysicalObject))
                                            {
                                                break;
                                            }
                                            PhysicalObject creatureRepresentation = obj;
                                            if (creatureRepresentation == null ||
                                                !this.corruptionCat.CanEatObjectType(obj))
                                            {
                                                break;
                                            }
                                            bool flag4 = false;
                                            int num2 = 0;
                                            while (num2 < this.corruptionCat.tentacles.Length && !flag4)
                                            {
                                                if (this.corruptionCat.tentacles[num2].huntObj == creatureRepresentation)
                                                {
                                                    flag4 = true;
                                                }
                                                num2++;
                                            }
                                            if (!flag4)
                                            {
                                                this.huntObj = creatureRepresentation;
                                                if (this.checkSound != null)
                                                {
                                                    this.checkSound.Destroy();
                                                    this.checkSound = null;
                                                }
                                                this.SwitchTask(CorruptionCatTentacle.Task.Hunt);
                                                break;
                                            }
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        m++;
                                    }
                                }
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

        public void CollideWithObject(int tChunk, BodyChunk objChunk)
        {
            if (this.backtrackFrom > -1 && this.backtrackFrom <= tChunk)
            {
                return;
            }
            float num = Vector2.Distance(this.tChunks[tChunk].pos, objChunk.pos);
            float num2 = (this.tChunks[tChunk].rad + ClampRad(objChunk.rad)) / 4f;
            Vector2 vector = Custom.DirVec(this.tChunks[tChunk].pos, objChunk.pos);
            float num3 = ClampMass(objChunk.mass) / (ClampMass(objChunk.mass) + 0.01f);
            float num4 = 0.8f;
            this.tChunks[tChunk].pos += vector * (num - num2) * num3 * num4;// * SpeedFac;
            this.tChunks[tChunk].vel += vector * (num - num2) * num3 * num4;// * SpeedFac;
            objChunk.pos -= vector * (num - num2) * (1f - num3) * num4;// * SpeedFac;
            objChunk.vel -= vector * (num - num2) * (1f - num3) * num4;// * SpeedFac;
        }

        public static bool VisualContact(PhysicalObject huntObj)
        {
            //有视力时直接为true
            if (CorruptionShapedMutationBuff.Instance.blindWaveEffectManager == null)
                return true;
            //无视力则检查是否在声波中
            for (int i = 0; i < CorruptionShapedMutationBuff.Instance.blindWaveEffectManager.activeWaveObjs.Count; i++)
            {
                for (int j = 0; j < huntObj.bodyChunks.Length; j++)
                {
                    bool inSoundRange = Custom.Dist(CorruptionShapedMutationBuff.Instance.blindWaveEffectManager.activeWaveObjs[i].pos,
                                                    huntObj.bodyChunks[j].pos) <
                                                    CorruptionShapedMutationBuff.Instance.blindWaveEffectManager.activeWaveObjs[i].rad + huntObj.bodyChunks[j].rad;
                    if (inSoundRange)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static float ClampMass(float mass)
        {
            return Mathf.Max(mass, 2f);
        }

        public static float ClampRad(float rad)
        {
            return Mathf.Max(rad, 5f);
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

    internal class IndicatorSymbol : CosmeticSprite
    {
        public Player owner;
        public Creature creature;
        Vector2 dir;
        public float alpha;
        public float distFac;
        Color effectColor => Color.blue;

        public IndicatorSymbol(Player owner, Creature creature, Room room)
        {
            this.owner = owner;
            this.creature = creature;
            this.room = room;
            this.dir = Vector2.zero;
            this.alpha = 1.0f;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            Vector2 camPos = this.room.game.cameras[0].pos;
            Vector2 roomCenter = camPos + new Vector2(Custom.rainWorld.options.ScreenSize.x / 2f, Custom.rainWorld.options.ScreenSize.y / 2f);
            Vector2 aim = this.creature.DangerPos - roomCenter;
            bool inScreen = creature.DangerPos.x > roomCenter.x - Custom.rainWorld.options.ScreenSize.x / 2f &&
                            creature.DangerPos.x < roomCenter.x + Custom.rainWorld.options.ScreenSize.x / 2f &&
                            creature.DangerPos.y > roomCenter.y - Custom.rainWorld.options.ScreenSize.y / 2f &&
                            creature.DangerPos.y < roomCenter.y + Custom.rainWorld.options.ScreenSize.y / 2f;
            this.lastPos = this.pos;
            this.dir = Custom.DirVec(roomCenter, creature.DangerPos);
            if (!inScreen)
                if (Mathf.Abs(this.dir.x) > Mathf.Abs(this.dir.y))
                    this.pos = roomCenter + this.dir * (Custom.LerpMap(Custom.rainWorld.options.ScreenSize.x / 2f, 0f, Mathf.Abs(aim.x), 0f, aim.magnitude) - 50f);
                else
                    this.pos = roomCenter + this.dir * (Custom.LerpMap(Custom.rainWorld.options.ScreenSize.y / 2f, 0f, Mathf.Abs(aim.y), 0f, aim.magnitude) - 50f);

            this.distFac = Mathf.Clamp01(Mathf.Pow(100f / (this.creature.DangerPos - this.pos).magnitude, 0.4f));
            if (CorruptionCatTentacle.VisualContact(this.creature) && !inScreen)
                this.alpha = Mathf.Clamp01(this.alpha + 0.025f);
            else
                this.alpha = Mathf.Clamp01(this.alpha - 0.025f);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = new FSprite(CreatureSymbol.SpriteNameOfCreature(CreatureSymbol.SymbolDataFromCreature(creature.abstractCreature)));
            //sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["GateHologram"];
            sLeaser.sprites[1] = new FSprite("Multiplayer_Arrow");
            sLeaser.sprites[1].scale = 0.8f;
            //sLeaser.sprites[1].shader = rCam.game.rainWorld.Shaders["GateHologram"];
            base.InitiateSprites(sLeaser, rCam);
            this.AddToContainer(sLeaser, rCam, null);
            this.ApplyPalette(sLeaser, rCam, this.room.game.cameras[0].currentPalette);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

            sLeaser.sprites[0].x = Mathf.Lerp(this.pos.x, this.lastPos.x, timeStacker) - camPos.x;
            sLeaser.sprites[0].y = Mathf.Lerp(this.pos.y, this.lastPos.y, timeStacker) - camPos.y;
            sLeaser.sprites[0].alpha = this.alpha * this.distFac;
            //sLeaser.sprites[0].color = Color.Lerp(this.effectColor, CreatureSymbol.ColorOfCreature(CreatureSymbol.SymbolDataFromCreature(creature.abstractCreature)), this.distFac);

            sLeaser.sprites[1].x = sLeaser.sprites[0].x + 25f * dir.x;
            sLeaser.sprites[1].y = sLeaser.sprites[0].y + 25f * dir.y;
            sLeaser.sprites[1].alpha = sLeaser.sprites[0].alpha;
            sLeaser.sprites[1].color = sLeaser.sprites[0].color;

            sLeaser.sprites[1].rotation = Custom.VecToDeg(dir) - 180f;

            if (base.slatedForDeletetion || this.room != rCam.room || owner.room == null)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);

            for (int i = 0; i < sLeaser.sprites.Length; i++)
                sLeaser.sprites[i].color = CreatureSymbol.ColorOfCreature(CreatureSymbol.SymbolDataFromCreature(creature.abstractCreature));
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].RemoveFromContainer();
                rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[i]);
            }
        }
    }
}
