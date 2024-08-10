using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Negative.SephirahMeltdown.Conditions;
using BuiltinBuffs.Positive;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuffUtils;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Negative.SephirahMeltdown
{
    internal class AyinBuffData : BuffData
    {
        public static readonly BuffID Ayin = new BuffID("Ayin",true);
        public override BuffID ID => Ayin;

        
    }

    internal class AyinBuff : Buff<AyinBuff, AyinBuffData>
    {
        public override BuffID ID => AyinBuffData.Ayin;

        public float Deg(float timeStacker) => Mathf.Lerp(lastDeg, deg, timeStacker);

        public AyinBuff()
        {
            forceEnableSub = false;
            TreeOfLightCondition.OnMoveToNextPart += TreeOfLightCondition_OnMoveToNextPart;
            if (BuffCustom.TryGetGame(out var game))
            {
                (game.Players[0].state as PlayerState).foodInStomach = 0;
                foreach(var layer in game.cameras[0].SpriteLayers)
                    ReplaceForContainer(layer);

                MusicEvent musicEvent = new MusicEvent
                {
                    fadeInTime = 1f,
                    roomsRange = -1,
                    cyclesRest = 0,
                    volume = 0.13f,
                    prio = 10f,
                    stopAtDeath = true,
                    stopAtGate = false,
                    loop = true,
                    maxThreatLevel = 10,
                    songName = $"BUFF_{AyinBuffData.Ayin.GetStaticData().AssetPath}/Ayin-1",

                };
                game.rainWorld.processManager.musicPlayer?.GameRequestsSong(musicEvent);
            }

            void ReplaceForContainer(FContainer container)
            {
                if (container == null) return;
                foreach (var node in container._childNodes)
                {
                    if (node is FFacetNode facet)
                    {
                        if (AyinHook.ReplacedShaders.Contains(facet.shader.name))
                            facet.shader = Custom.rainWorld.Shaders[$"SephirahMeltdownEntry.{facet.shader.name}Rotation"];
                    }
                    else if(node is FContainer child)
                        ReplaceForContainer(child);
                }
            }
        }

        private void TreeOfLightCondition_OnMoveToNextPart(int obj)
        {
            currentState = obj;
            if (BuffCustom.TryGetGame(out var game))
            {
                fromDeg = deg;
                toDeg = Custom.LerpMap(obj, 0, 5, 0, 180);

                if (obj == 5)
                {
                    MusicEvent musicEvent = new MusicEvent
                    {
                        fadeInTime = 1f,
                        roomsRange = -1,
                        cyclesRest = 0,
                        volume = 0.13f,
                        prio = 12f,
                        stopAtDeath = true,
                        stopAtGate = false,
                        loop = true,
                        maxThreatLevel = 10,
                        songName = $"BUFF_{AyinBuffData.Ayin.GetStaticData().AssetPath}/Ayin-2",

                    };
                    game.rainWorld.processManager.musicPlayer?.GameRequestsSong(musicEvent);
                }
                moveCounter = 0;
                if (obj == 6)
                {
                    _ = AyinPost.Instance;
                }
                else if(obj == 7)
                {
                    AyinPost.Instance.speed = 6;
                }
                else if (obj == 8)
                {
                    AyinPost.Instance.toHeight = 0.9f;
                    AyinPost.Instance.speed = 7;
                }
                else if (obj == 9)
                {
                    AyinPost.Instance.toHeight = 1.996f;
                    AyinPost.Instance.smoothHeight = 0.01f;
                    ParticleEmitter emitter = new ParticleEmitter(game.cameras[0].room);
                    var rect = new Rect(game.cameras[0].pos, Custom.rainWorld.screenSize);
                    rect.yMax += Custom.rainWorld.screenSize.y / 4;
                    rect.yMin = rect.yMax - Custom.rainWorld.screenSize.y/2;
                    emitter.ApplyParticleModule(new RectPositionModule(emitter, rect, 0));
                    emitter.ApplyParticleModule(new SetRandomRotation(emitter, 0, 360));
                    emitter.ApplyParticleSpawn(new RateSpawnerModule(emitter, 120,10));
                    emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White",
                        $"FlatLight",
                        game.cameras[0].SpriteLayerIndex["HUD"])));
                    emitter.ApplyParticleModule(new Gravity(emitter, 1.2f));
                    emitter.ApplyParticleModule(new SetRandomVelocity(emitter, Vector2.down * 10f, Vector2.down * 25f,
                        false));
                    emitter.ApplyParticleModule(new SetRandomColor(emitter, 32 / 360f, 60 / 360f, 1, 0.7f));
                    emitter.ApplyParticleModule(new SetRandomScale(emitter, 4,10));
                    emitter.ApplyParticleModule(new SetOriginalAlpha(emitter, 0));
                    emitter.ApplyParticleModule(new SetRandomLife(emitter, 40, 60));
                    emitter.ApplyParticleModule(new AlphaOverLife(emitter,
                        (p, time) => 0.6f * Mathf.InverseLerp(0, 0.13f, time) * Mathf.InverseLerp(1f, 0.5f, time)));
                    ParticleSystem.ApplyEmitterAndInit(emitter);
                    game.cameras[0].ScreenMovement(null, Vector2.down * 5, 1);
                }

                if (obj <= 5)
                {
                    ParticleEmitter emitter = new ParticleEmitter(game.cameras[0].room);
                    var rect = new Rect(game.cameras[0].pos, Custom.rainWorld.screenSize);
                    rect.yMin = rect.yMax - 150;
                    float texSize = Futile.atlasManager.GetElementWithName("Binah.Smoke").sourceSize.x;
                    emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 5, false));

                    emitter.ApplyParticleModule(new RectPositionModule(emitter, rect, 0));
                    emitter.ApplyParticleModule(new SetRandomRotation(emitter, 0, 360));
                    emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, 35));
                    emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Binah.Smoke",
                        $"{StormIsApproachingEntry.StormIsApproaching}.AdditiveDefault",
                        game.cameras[0].SpriteLayerIndex["HUD"])));
                    emitter.ApplyParticleModule(new Gravity(emitter, 1.2f));
                    emitter.ApplyParticleModule(new SetRandomVelocity(emitter, Vector2.down * 5f, Vector2.down * 15f,
                        false));
                    emitter.ApplyParticleModule(new SetRandomColor(emitter, 32 / 360f, 60 / 360f, 1, 0.3f));
                    emitter.ApplyParticleModule(new SetRandomScale(emitter, 70 / texSize, 200 / texSize));
                    emitter.ApplyParticleModule(new SetOriginalAlpha(emitter, 0));
                    emitter.ApplyParticleModule(new SetRandomLife(emitter, 20, 40));
                    emitter.ApplyParticleModule(new AlphaOverLife(emitter,
                        (p, time) => 0.6f * Mathf.InverseLerp(0, 0.13f, time) * Mathf.InverseLerp(1f, 0.5f, time)));
                    ParticleSystem.ApplyEmitterAndInit(emitter);
                    game.cameras[0].ScreenMovement(null, Vector2.down * 5, 1);

                }

            }
        }

        public override void Update(RainWorldGame game)
        {
            base.Update(game);
            if (moveCounter < 40)
                moveCounter++;
            lastDeg = deg;
            deg = Mathf.Lerp(fromDeg, toDeg, Helper.EaseOutElastic(moveCounter / 40f));


            if (currentState > 5 && currentState < 9)
            {
                if (counter % 100 == 0)
                {
                    ParticleEmitter emitter = new ParticleEmitter(game.cameras[0].room);
                    var rect = new Rect(game.cameras[0].pos, Custom.rainWorld.screenSize);
                    rect.yMin = rect.yMax - 150;
                    float texSize = Futile.atlasManager.GetElementWithName("Binah.Smoke").sourceSize.x;
                    emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 5, false));

                    emitter.ApplyParticleModule(new RectPositionModule(emitter, rect, 0));
                    emitter.ApplyParticleModule(new SetRandomRotation(emitter, 0, 360));
                    emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, Random.Range(15,25)));
                    emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Binah.Smoke",
                        $"{StormIsApproachingEntry.StormIsApproaching}.AdditiveDefault",
                        game.cameras[0].SpriteLayerIndex["HUD"])));
                    emitter.ApplyParticleModule(new Gravity(emitter, 1.2f));
                    emitter.ApplyParticleModule(new SetRandomVelocity(emitter, Vector2.down * 5f, Vector2.down * 15f,
                        false));
                    emitter.ApplyParticleModule(new SetRandomColor(emitter, 32 / 360f, 60 / 360f, 1, 0.3f));
                    emitter.ApplyParticleModule(new SetRandomScale(emitter, 70 / texSize, 200 / texSize));
                    emitter.ApplyParticleModule(new SetOriginalAlpha(emitter, 0));
                    emitter.ApplyParticleModule(new SetRandomLife(emitter, 20, 40));
                    emitter.ApplyParticleModule(new AlphaOverLife(emitter,
                        (p, time) => 0.6f * Mathf.InverseLerp(0, 0.13f, time) * Mathf.InverseLerp(1f, 0.5f, time)));
                    ParticleSystem.ApplyEmitterAndInit(emitter);
                    game.cameras[0].ScreenMovement(null, Vector2.down * 5, 1);
                }
                counter++;
                game.cameras[0].screenShake = 0.1f;
            }
        }

        private int moveCounter;
        private int counter;

        private float lastDeg;
        private float deg;
        private float fromDeg;
        private float toDeg;

        private int currentState;

        public override void Destroy()
        {
            base.Destroy();
            TreeOfLightCondition.OnMoveToNextPart -= TreeOfLightCondition_OnMoveToNextPart;
            Shader.SetGlobalFloat("Buff_screenRotate", 0);
            AyinPost.EndDestroy();
            if(BuffCustom.TryGetGame(out var game))
                game.rainWorld.processManager.musicPlayer?.GameRequestsSongStop(new StopMusicEvent() { fadeOutTime = 0.5f, prio = 100, songName = $"BUFF_{AyinBuffData.Ayin.GetStaticData().AssetPath}/Ayin-1", type = StopMusicEvent.Type.AllSongs });

        }

        public static bool forceEnableSub = false;
    }

    internal class AyinHook
    {
        public static void HookOn()
        {
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
            On.RegionGate.customKarmaGateRequirements += RegionGate_customKarmaGateRequirements;

            On.RainCycle.Update += RainCycle_Update;
            On.CreatureCommunities.LikeOfPlayer += CreatureCommunities_LikeOfPlayer;

            On.Player.SubtractFood += Player_SubtractFood;

            On.RainWorldGame.Update += RainWorldGame_Update;
            On.FFacetRenderLayer.ctor += FFacetRenderLayer_ctor;
            Shader.SetGlobalFloat("Buff_screenRotate", 0);


            counter = 0;
        }

        public static readonly HashSet<string> ReplacedShaders = new HashSet<string>()
            { "FlatLightBehindTerrain", "LevelSnowShader", "LightSource" ,"LevelColor"};

        private static void FFacetRenderLayer_ctor(On.FFacetRenderLayer.orig_ctor orig, FFacetRenderLayer self, FStage stage, FFacetType facetType, FAtlas atlas, FShader shader)
        {
            if (ReplacedShaders.Contains(shader.name))
                shader = Custom.rainWorld.Shaders[$"SephirahMeltdownEntry.{shader.name}Rotation"];
            orig(self,stage, facetType, atlas, shader);
        }

        private static int counter = 0;

        private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            
            counter++;
            if (counter == 40)
            {
                foreach (var room in self.world.activeRooms)
                    if (room.updateList.All(i => !(i is CreatureEnd)))
                        room.AddObject(new CreatureEnd(room));
                counter = 0;
            }

        }

        private static void Player_SubtractFood(On.Player.orig_SubtractFood orig, Player self, int sub)
        {
            if(AyinBuff.forceEnableSub)
                orig(self, sub);
        }

        private static float CreatureCommunities_LikeOfPlayer(On.CreatureCommunities.orig_LikeOfPlayer orig, CreatureCommunities self, CreatureCommunities.CommunityID commID, int region, int playerNumber)
        {
            var re = orig(self,commID, region, playerNumber);
            return Mathf.Max(re, 0.75f);
        }

        private static void RainCycle_Update(On.RainCycle.orig_Update orig, RainCycle self)
        {
            orig(self);
            self.timer = 500;
            self.preTimer = 0;
        }

        private static void RegionGate_customKarmaGateRequirements(On.RegionGate.orig_customKarmaGateRequirements orig, RegionGate self)
        {
            orig(self);
            self.karmaRequirements[0] = RegionGate.GateRequirement.OneKarma;
            self.karmaRequirements[1] = RegionGate.GateRequirement.OneKarma;
            self.unlocked = true;

        }

        private static void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            orig(self, timeStacker, timeSpeed);

            var localCenter = (self.followAbstractCreature?.realizedCreature?.DangerPos - self.pos) / Custom.rainWorld.screenSize ??
                              new Vector2(0.5F, 0.5f);
            var rect = Shader.GetGlobalVector(RainWorld.ShadPropSpriteRect);
            var border = Custom.LerpMap(Mathf.Abs(90 - AyinBuff.Instance.Deg(timeStacker)), 90,0, 0.5f, 0.25f);
            var xOffset = Mathf.Max(0, Mathf.Abs(localCenter.x - 0.5f) - border) * Mathf.Sign(localCenter.x - 0.5f);
            for (int i = 0; i < 11; i++)
            {
                self.SpriteLayers[i].rotation = 0;
                self.SpriteLayers[i].SetPosition(0, xOffset * self.sSize.y);
                self.SpriteLayers[i].RotateAroundPointRelative(self.sSize * new Vector2(0.5f,0.5f), AyinBuff.Instance.Deg(timeStacker));
            }


            (rect.x, rect.y, rect.z, rect.w) = (rect.x , rect.y  + xOffset * (rect.w - rect.y), rect.z , rect.w + xOffset * (rect.w - rect.y));
            Shader.SetGlobalVector(RainWorld.ShadPropSpriteRect, rect);

            Shader.SetGlobalFloat("Buff_screenRotate", AyinBuff.Instance.Deg(timeStacker) * Mathf.Deg2Rad);
            if(self.levelGraphic.shader != Custom.rainWorld.Shaders["SephirahMeltdownEntry.LevelColorRotation"])
                self.levelGraphic.shader = Custom.rainWorld.Shaders["SephirahMeltdownEntry.LevelColorRotation"];

           
                
         
        }
    }

    internal class CreatureEnd : UpdatableAndDeletable
    {
        public CreatureEnd(Room room)
        {
            this.room = room;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            foreach (var crit in room.abstractRoom.creatures.Select(i => i.realizedCreature))
            {
                if (crit == null || crit is Player || crit.inShortcut || crit.Template.smallCreature || crit.Template.type == CreatureTemplate.Type.Overseer) continue;
                crit.Stun(200);

            }
        }
    }

    internal class AyinPost : BuffPostEffect
    {
        protected AyinPost(int layer) : base(layer)
        {
            material = new Material(SephirahMeltdownEntry.AyinScreenEffect);
        }

        public float height = 0.7f;
        public float smoothHeight = 0.05f;
        public float inst = 500;
        public float pow = 10;
        public Color color = Color.clear;
        public Color toColor = new Color(1, 0.68f, 0.03f);
        private float offset;
        public float speed = 5;
        public float toHeight = 0.7f;

        public override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {


            material.SetFloat("offset",offset);
            material.SetFloat("p", pow);
            material.SetFloat("inst", inst);
            material.SetFloat("height", height);
            material.SetFloat("smoothHeight", smoothHeight);
            material.SetColor("color", color);

            Graphics.Blit(source,destination,material);
        }

        public override void Update()
        {
            base.Update();
            offset += -speed * Time.deltaTime;
            if (offset < -20000) offset += 20000;

            height = Mathf.Lerp(height, toHeight, Time.deltaTime * 40 * 0.03f);
            color = Color.Lerp( color, toColor, Time.deltaTime * 40 * 0.03f);

        }

        public static void EndDestroy()
        {
            instance?.Destroy();
            instance = null;
        }


        public static AyinPost Instance
        {
            get
            {
                if (instance == null)
                    BuffPostEffectManager.AddEffect(instance = new AyinPost(3));
                return instance;
            }
        }

        private static AyinPost instance;
    }
}
