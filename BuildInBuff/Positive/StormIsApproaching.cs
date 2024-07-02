using MoreSlugcats;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuffUtils.FutileExtend;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RWCustom;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive
{
    internal class StormIsApproachingEntry : IBuffEntry
    {
        public static readonly Color Sword1 = Custom.hexToColor("77D2FF");
        public static readonly Color Sword2 = Custom.hexToColor("8377FF");

        public static readonly BuffID StormIsApproaching = new BuffID(nameof(StormIsApproaching), true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<StormIsApproachingBuff, StormIsApproachingBuffData, StormIsApproachingEntry>(
                StormIsApproaching);
        }


        public static void HookOn()
        {
            On.RainWorldGame.GrafUpdate += RainWorldGame_GrafUpdate;
        }

        private static void RainWorldGame_GrafUpdate(On.RainWorldGame.orig_GrafUpdate orig, RainWorldGame self, float timeStacker)
        {
            orig(self,timeStacker);

            if (!self.paused || self.pauseMenu != null)
                return;

            var cf = self.cameras[0].virtualMicrophone;
            foreach (var sound in  cf.soundObjects.Where(i => i.soundData.soundID.value.Contains("StormIsApproaching")))
            {
                sound.Update(timeStacker,self.framesPerSecond/40f);
            }
        }

        public static void LoadAssets()
        {
            var assetPath = StormIsApproaching.GetStaticData().AssetPath;
            var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath(assetPath + Path.DirectorySeparatorChar + "burythelight"));
            CutScreen = bundle.LoadAsset<Shader>("CutScreen");
            StartShock = bundle.LoadAsset<Shader>("StartShock");
            HueSeparation = bundle.LoadAsset<Shader>("HueSeparation");
            SingleColor = bundle.LoadAsset<Shader>("SingleColor");
            AddShader(bundle, "SwordLeaser", "SwordLeaser");
            AddShader(bundle, "AdditiveDefault", "AdditiveDefault");
            AddShader(bundle, "SwordRing", "SwordRing");
            //AddShader(bundle, "LevelColorRotation", "LevelColorRotation");

            MeshManager.LoadMesh("RawSword", assetPath + Path.DirectorySeparatorChar + "CurveAbsorb.OBJ");
            //MeshManager.LoadMesh("T", assetPath + Path.DirectorySeparatorChar + "flameThrower.obj");
            //Futile.atlasManager.LoadImage(assetPath + Path.DirectorySeparatorChar + "flameThrowerTexture");

            Futile.atlasManager.LoadImage(assetPath + Path.DirectorySeparatorChar + "StartFlare");
            BuffSounds.LoadSound(StartSound1, assetPath, new BuffSoundGroupData(), new BuffSoundData("Efect03"));
            BuffSounds.LoadSound(StartSound2, assetPath, new BuffSoundGroupData(), new BuffSoundData("pl030_new_drive_s02"));
            BuffSounds.LoadSound(EndSound1, assetPath, new BuffSoundGroupData(), new BuffSoundData("hyakuretu_end"));
            BuffSounds.LoadSound(EndSound2, assetPath, new BuffSoundGroupData(), new BuffSoundData("Ice_Hit_Break"));


        }

        public static readonly SoundID StartSound1 = new SoundID($"{StormIsApproaching}.StartSound1",true);
        public static readonly SoundID StartSound2 = new SoundID($"{StormIsApproaching}.StartSound2", true);
        public static readonly SoundID EndSound1 = new SoundID($"{StormIsApproaching}.EndSound1", true);
        public static readonly SoundID EndSound2 = new SoundID($"{StormIsApproaching}.EndSound2", true);




        private static void AddShader(AssetBundle bundle, string key, string name)
        {
            Custom.rainWorld.Shaders.Add($"{StormIsApproaching}.{key}",FShader.CreateShader($"{StormIsApproaching}.{key}",bundle.LoadAsset<Shader>(name)));
        }

        public static Shader CutScreen;
        public static Shader StartShock;
        public static Shader HueSeparation;
        public static Shader SingleColor;

    }

    internal class StormIsApproachingBuff : Buff<StormIsApproachingBuff, StormIsApproachingBuffData>
    {
        public override BuffID ID => StormIsApproachingEntry.StormIsApproaching;

        private bool hasUse = false;

        public override bool Triggerable => !hasUse;

        public override bool Trigger(RainWorldGame game)
        {
            var player = game.cameras[0].followAbstractCreature.realizedCreature as Player;
            if (player == null && game.AlivePlayers.Count != 0)
                player = game.AlivePlayers.FirstOrDefault(i => i.realizedCreature is Player && i.realizedCreature.room != null)?.realizedCreature as Player;
            if (player == null)
                player = game.Players.FirstOrDefault(i => i.realizedCreature is Player && i.realizedCreature.room != null)?.realizedCreature as Player;
            if (player == null)
                return false;

            hasUse = true;

            player.room.AddObject(new JudgmentCut(player.room, player));

            return false;
        }
    }
    internal class StormIsApproachingBuffData : CountableBuffData
    {
        public override BuffID ID => StormIsApproachingEntry.StormIsApproaching;
        public override int MaxCycleCount => 3;
    }

    internal class JudgmentCutPartBase : CosmeticSprite
    {
        protected int counter;
        protected int lifeCounter;

        protected JudgmentCutPartBase(Room room, int life)
        {
            this.room = room;
            this.lifeCounter = life;
        }


        public override void PausedUpdate()
        {
            base.PausedUpdate();
            if(room.game.pauseMenu == null && room.game.paused)
                Update(false);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            counter++;
            if(counter == lifeCounter)
                Destroy();
        }

        protected float TimeValue(int enter, int exit, int maxCounter, float timeStacker,float pow = 0.33f)
        {
            return Mathf.Pow(Mathf.InverseLerp(0, enter, counter + timeStacker) * Mathf.InverseLerp(0, exit, maxCounter - counter - timeStacker),pow);
        }
    }

    internal class JudgmentCutStartFlash : JudgmentCutPartBase
    {
        private const int maxCountFlash = 6;
        private const int maxCountFlashLeaser = 8;



        public JudgmentCutStartFlash(Room room, Vector2 pos) : base(room, maxCountFlashLeaser)
        {
            this.pos = pos;
        }


        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = new FSprite[StartLight + Trail];
            sLeaser.sprites[0] = new FSprite(StormIsApproachingEntry.StormIsApproaching.GetStaticData().AssetPath + Path.DirectorySeparatorChar + "StartFlare")
            {
                shader = rCam.game.rainWorld.Shaders[$"{StormIsApproachingEntry.StormIsApproaching}.AdditiveDefault"],
                width = 600,
                height = 200
            };
            sLeaser.sprites[1] = new FSprite
            {
                shader = rCam.game.rainWorld.Shaders["FlatLight"],
                width = 700,
                height = 700
            };

            //todo size
            AddToContainer(sLeaser, rCam, null);
        }

        public override void PausedDrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            if (slatedForDeletetion)
            {
                sLeaser.RemoveAllSpritesFromContainer();
                return;
            }
            sLeaser.sprites[0].SetPosition(pos - camPos);
            sLeaser.sprites[1].SetPosition(pos - camPos);

            sLeaser.sprites[0].alpha = TimeValue(3, 2, maxCountFlashLeaser, timeStacker);
            sLeaser.sprites[1].alpha = TimeValue(2, 2, maxCountFlash, timeStacker);

        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            var hud = rCam.ReturnFContainer("HUD");
            hud.AddChild(sLeaser.sprites[0]);
            hud.AddChild(sLeaser.sprites[1]);
        }



        private int TrailIndex(int index)
        {
            return StartLight + index;
        }
        private const int StartLight = 2;
        private const int Trail = 0;

    }

    internal class JudgmentGhostPeriodicEmitter : GhostPeriodicEmitter
    {
        private Vector2 origPos;
        private Vector2 origPosEnd;
        private Vector2 dir;
        private bool isZ = false;
        private float speed = 1000;
        private Player player;

        private int counter = 2;


        public JudgmentGhostPeriodicEmitter(Player player, Room room, int emitterLife = -1, int emitCounter = 10, int ghostLife = 40, float ghostAlpha = 0.3f, float ghostVelMulti = 0)
            : base(player.graphicsModule, room, emitterLife, emitCounter, ghostLife, ghostAlpha, ghostVelMulti)
        {
            this.player = player;
            origPos = player.DangerPos;
            origPosEnd = player.bodyChunks[1].pos;
            dir = new Vector2(player.flipDirection, 0);

            if (player.bodyMode == Player.BodyModeIndex.ZeroG)
            {
                dir = Custom.DirVec(player.bodyChunks[1].pos, player.bodyChunks[0].pos);
                isZ = true;
            }

            player.bodyChunks[0].HardSetPosition(origPos + dir * counter * speed / 40f);
            player.bodyChunks[1].HardSetPosition(origPosEnd + dir * counter * speed / 40f);

        }

        public override void Update(bool eu)
        {
            counter++;
            player.bodyChunks[0].HardSetPosition(origPos + dir * counter * speed / 40f);
            player.bodyChunks[1].HardSetPosition(origPosEnd + dir * counter * speed / 40f);
            base.Update(eu);

        }
    }


    internal class JudgmentCutMoveTrail : UpdatableAndDeletable
    {
        private int counter = 0;
        private Player player;
        private Vector2 origPos;
        private Vector2 dir;

        private GhostEmitter emitter;
        public JudgmentCutMoveTrail(Room room, Player player)
        {
            this.player = player;
            origPos = player.DangerPos;
            this.room = null;
            dir = new Vector2(player.flipDirection, 0);
            if (player.bodyMode == Player.BodyModeIndex.ZeroG)
                dir = Custom.DirVec(player.bodyChunks[1].pos, player.bodyChunks[0].pos);
            emitter = new JudgmentGhostPeriodicEmitter(player, room, 8, 1, 20, 0.4f);
            room.AddObject(emitter);
    
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            counter++;
            if (counter == 8)
            {
                room.AddObject(new LightningBolt(origPos - dir*15, player.DangerPos + dir *20, 
                    0, 2, 0.75f, 0.5f, 200f / 360f, true)
                { intensity = 1f });
                player.SuperHardSetPosition(new Vector2(100, 100000));
                emitter.Destroy();
                Destroy();

            }
        }

    }
    internal class JudgmentCutPlayerUpdate : JudgmentCutPartBase
    {
        private Player player;
        
        public JudgmentCutPlayerUpdate(Room room, Player player) : base(room,3)
        {
            this.player = player;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = Array.Empty<FSprite>();
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if(!room.game.paused)
                Destroy();
        }

        public override void PausedDrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.PausedDrawSprites(sLeaser, rCam, timeStacker, camPos);
            var re = rCam.spriteLeasers.FirstOrDefault(i => i.drawableObject == player.graphicsModule);
            if (re != null)
            {
                re.Update(1,rCam,camPos);
            }
        }
    }

    internal class JudgmentCutLineSword : JudgmentCutPartBase
    {
        public JudgmentCutLineSword(Room room, Vector2 pos) : base(room, (int)(5 * 40))
        {
            this.pos = pos;
            parDatas = new ParData[Random.Range(13, 25)];
            for (int i = 0; i < parDatas.Length; i++)
            {
                parDatas[i] = new ParData()
                {
                    life = Random.Range(-0.2f, 0f),
                    maxLife = Random.Range(0.355f, 0.7f),
                    offset = Custom.RNV() * Mathf.Pow(Random.value,0.2f) * 450,
                };
                parDatas[i].lastLife = parDatas[i].life;
            }
        }

        private ParData[] parDatas;

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = new FSprite[parDatas.Length];
            for (int i = 0; i < parDatas.Length; i++)
            {
                sLeaser.sprites[i] = new FSprite("Futile_White")
                {
                    shader = rCam.game.rainWorld.Shaders[$"{StormIsApproachingEntry.StormIsApproaching}.SwordLeaser"],
                    width = 50,
                    height = 0,
                    rotation = Random.Range(0,360),
                    color = Color.Lerp(StormIsApproachingEntry.Sword1, StormIsApproachingEntry.Sword2, Random.value)
                };
            }
            AddToContainer(sLeaser,rCam,null);
        }

        public override void PausedDrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            for (int i = 0; i < parDatas.Length; i++)
            {
                var life = Mathf.Lerp(parDatas[i].lastLife, parDatas[i].life, timeStacker);
                sLeaser.sprites[i].SetPosition(parDatas[i].offset + pos - camPos);
                sLeaser.sprites[i].height = Mathf.InverseLerp(0, 0.15f, 1 - life) * Mathf.InverseLerp(0, 0.24f, life) * 1000;
                sLeaser.sprites[i].alpha = Mathf.InverseLerp(0, 0.1f, 1 - life) * Mathf.InverseLerp(0, 0.12f, life);
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            foreach (var data in parDatas)
            {
                data.lastLife = data.life;
                data.life += 1 / 40f / data.maxLife;
            }
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner = rCam.ReturnFContainer("HUD");
            base.AddToContainer(sLeaser, rCam, newContatiner);
        }

        internal class ParData
        {
            public float lastLife;
            public float life;

            public float maxLife;

            public Vector2 offset;
        }
    }

    internal class JudgmentCutCircleSword : JudgmentCutPartBase
    {
        private Vector3 GetAxis(Vector3 rotation)
        {
            var v = Vector3.right;
            v = RotateRound(v, Vector3.up, rotation.x);
            v = RotateRound(v, Vector3.forward, rotation.y);
            v = RotateRound(v, Vector3.right, rotation.z);
            return v;

        }
        Vector3 RotateRound(Vector3 position, Vector3 axis, float angle)
        {
            return Quaternion.AngleAxis(angle, axis) * (position);
        }

        private Vector3 RotateX(Vector3 rotation,Vector3 axis,float deg)
        {
            return rotation +(Quaternion.AngleAxis(deg, axis)).eulerAngles;
        }
        public JudgmentCutCircleSword(Room room, Vector2 pos) : base(room, (int)(3f * 40))
        {
            this.pos = pos;

            parDatas = new MeshParData[Random.Range(5, 10)];
            for (int i = 0; i < parDatas.Length; i++)
            {
                parDatas[i] = new MeshParData()
                {
                    maxLife = Random.Range(0.1f, 0.25f),
                    life = Random.Range(-0.5f, 0f),
                    rotateSpeed = Random.Range(20f, 30f),
                    rotation = (new Vector3(Random.value - 0.5f, Random.value - 0.5f, Random.value - 0.5f))*360
                };
                parDatas[i].lastRotation = parDatas[i].rotation;
                parDatas[i].lastLife = parDatas[i].life;
                parDatas[i].axis = GetAxis(parDatas[i].rotation);
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[parDatas.Length];
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i] = new FMesh("RawSword", "Futile_White", false)
                {
                    shader = rCam.game.rainWorld.Shaders[$"{StormIsApproachingEntry.StormIsApproaching}.SwordRing"],
                    scale = Random.Range(0.8f,5f),
                    rotation3D = parDatas[i].rotation,
                    alpha = 0,
                    color = Color.Lerp(StormIsApproachingEntry.Sword1, StormIsApproachingEntry.Sword2, Random.value)

                };
            }
            AddToContainer(sLeaser,rCam,null);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner = rCam.ReturnFContainer("HUD");
            foreach(var sprite in sLeaser.sprites)
                newContatiner.AddChild(sprite);
        }

        public override void PausedDrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {

                var life = Mathf.Lerp(parDatas[i].lastLife, parDatas[i].life, timeStacker);
                sLeaser.sprites[i].SetPosition(pos - camPos);
                var mesh = sLeaser.sprites[i] as FMesh;
                mesh.rotation3D = Vector3.Lerp(parDatas[i].lastRotation, parDatas[i].rotation, timeStacker);
                mesh.alpha = Mathf.InverseLerp(0, 0.2f, 1 - life) * Mathf.InverseLerp(0, 0.2f, life);
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            foreach (var data in parDatas)
            {
                data.lastLife = data.life;
                data.lastRotation = data.rotation;
                data.rotation = RotateX(data.rotation, GetAxis(data.rotation), data.rotateSpeed / 40);
                data.life += 1 / 40f / data.maxLife;
            }
        }

        private MeshParData[] parDatas;

        internal class MeshParData
        {
            public Vector3 rotation;
            public Vector3 lastRotation;
            public Vector3 axis;
            public float rotateSpeed;

            public float lastLife;
            public float life;

            public float maxLife;
        }
    }
    internal class JudgmentCut : UpdatableAndDeletable
    {
        private Player player;
        private Vector2 pos;
        private Vector2 endPos;
        private int counter;

        public JudgmentCut(Room room, Player player)
        {

            this.room = room;
            this.player = player;
            this.pos = player.DangerPos;
            endPos = player.bodyChunks[1].pos;
            room.AddObject(new JudgmentCutStartFlash(room, pos));
            room.AddObject(new JudgmentCutMoveTrail(room, player));
            room.game.cameras[0].virtualMicrophone.PlaySound(StormIsApproachingEntry.StartSound1, 0, 0.9f, 1);
            room.game.cameras[0].virtualMicrophone.PlaySound(StormIsApproachingEntry.StartSound2, 0, 0.9f, 1);

        }
        private int delay = 2;
        private int cut = 10;
        private float duringTime = 2.5f;
        private ParticleEmitter emitter;

        public override void PausedUpdate()
        {
            base.PausedUpdate();
            if (room.game.pauseMenu != null)
                return;

            counter++;
            if (counter == 1)
            {
                var deg = Custom.VecToDeg(Vector2.up);
                BuffPostEffectManager.AddEffect(new ShockEffect(2, 0.3f, 0.1f, 0.5f, 0, 5f, 0.3f,
                    (pos - room.game.cameras[0].pos) / Custom.rainWorld.screenSize));
                BuffPostEffectManager.AddEffect(new BurstRadialBlurEffect(2, 0.3f, 0.1f, 0.05f,
                    (pos - room.game.cameras[0].pos) / Custom.rainWorld.screenSize, 0.07f));
                BuffPostEffectManager.AddEffect(new HueEffect(2, 0.3f, 0.1f, 0.2f, 0.8f, 0.2f));
                emitter = new ParticleEmitter(room);
                emitter.pos = emitter.lastPos = pos;
                emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, Random.Range(35, 70)));
                emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
                emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Pebble1", string.Empty)));

                emitter.ApplyParticleModule(new SetRandomPos(emitter, 700));
                emitter.ApplyParticleModule(new SetRandomVelocity(emitter, Custom.DegToVec(-45 + deg) * 5, Custom.DegToVec(45 + deg) * 0.01f));
                emitter.ApplyParticleModule(new VelocityOverLife(emitter, (particle, time)
                    => particle.vel * 0.99f + Vector2.down / 4f * emitter.room.gravity));
                emitter.ApplyParticleModule(new RotationOverLife(emitter, (particle, time) =>
                    particle.rotation + Mathf.Sign(particle.vel.x) * Custom.LerpMap(particle.vel.magnitude, 5, 20, 10, 80) / 40f * (1 - time)));
                emitter.ApplyParticleModule(new SetRandomScale(emitter, 1f, 0.15f));
                emitter.ApplyParticleModule(new SetRandomRotation(emitter, 0, 360));
                emitter.ApplyParticleModule(new SetConstColor(emitter, new Color(0.01f, 0.01f, 0.01f)));
                emitter.ApplyParticleModule(new SimpleParticlePhysic(emitter, true, false));
                emitter.ApplyParticleModule(new AlphaOverLife(emitter, ((particle, time) =>
                    particle.alpha = Mathf.InverseLerp(0.1f,0f,0.2f- time) * Mathf.Pow(1 - time, 0.5f))));
                emitter.ApplyParticleModule(new SetRandomLife(emitter, 80, 160));
                emitter.ApplyParticleModule(new Gravity(emitter, 0.9f));
                ParticleSystem.ApplyEmitterAndInit(emitter);
            }
            else if (counter == 8)
            {
                room.AddObject(new JudgmentCutCircleSword(room, pos));
                room.AddObject(new JudgmentCutLineSword(room, pos));
            }
            else if (counter == 8 + delay)
            {
                BuffPostEffectManager.AddEffect(new SingleColorEffect(1, duringTime, 0.5f, 0.5f,
                    Custom.hexToColor("070B0C"), Custom.hexToColor("E3F4FF"), 1f));
                room.game.paused = true;

            }
            else if (counter == 8 + delay + cut)
                BuffPostEffectManager.AddEffect(new CutEffect(0, duringTime - cut / 40f,0.3f,0.03f));
            else if (counter == 8 + delay + cut + 10)
            {
                player.SuperHardSetPosition(pos);
                room.AddObject(new JudgmentCutPlayerUpdate(room,player));
                player.bodyChunks[1].HardSetPosition(endPos);
            }
            else if (counter == 8 + delay + (int)((duringTime-0.3f) * 40))
            {
                BuffPostEffectManager.AddEffect(new SingleColorEffect(1, 0.2f, 0.001f, 0.1f,
                    Custom.hexToColor("000000"), Custom.hexToColor("C7E6FF"), -3f));
                BuffPostEffectManager.AddEffect(new BurstRadialBlurEffect(2, 0.2f, 0.001f, 0.1f,
                    (pos - room.game.cameras[0].pos) / Custom.rainWorld.screenSize, 0.1f));
                BuffPostEffectManager.AddEffect(new HueEffect(2, 0.3f, 0.001f, 0.2f, 1f, 0.2f));
                room.game.cameras[0].ScreenMovement(pos,Custom.RNV()*40,30);
                room.game.cameras[0].virtualMicrophone.PlaySound(StormIsApproachingEntry.EndSound1, 0, 0.9f, 1);
                room.game.cameras[0].virtualMicrophone.PlaySound(StormIsApproachingEntry.EndSound2, 0, 0.9f, 1);


            }
            else if (counter == 8 + delay + (int)((duringTime + 0.15f - 0.3f) * 40))
            {
                room.game.paused = false;
                foreach (var crit in room.abstractRoom.creatures.Where(i => i.creatureTemplate.dangerousToPlayer > 0)
                             .Select(i => i.realizedCreature))
                {
                    crit.Die();
                    var vel = Custom.RNV() * Random.Range(20, 35);
                    foreach (var chunk in crit.bodyChunks)
                        chunk.vel += vel;
                }
                emitter.Die();
            }

        }
    }


    /// <summary>
    /// Effect
    /// </summary>

    internal class ShockEffect : BuffPostEffectLimitTime
    {
        public ShockEffect(int layer, float duringTime, float enterTime, float fadeTime,float start,float end, float maxInst, Vector2 center) : base(layer, duringTime, enterTime, fadeTime)
        {
            this.start = start;
            this.end = end;
            pos = center;
            this.maxInst = maxInst;
            material = new Material(StormIsApproachingEntry.StartShock);
        }

        protected override float LerpAlpha => Mathf.Pow(base.LerpAlpha, 0.33f);

        public override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            base.OnRenderImage(source, destination);
            material.SetFloat("start", start);
            material.SetFloat("end", end);
            material.SetFloat("inst", maxInst * LerpAlpha);
            material.SetVector("pos",pos);
            Graphics.Blit(source, destination, material);
        }

        private float start;
        private float end;
        private float maxInst;
        private Vector4 pos;


    }
    internal class HueEffect : BuffPostEffectLimitTime
    {
        public HueEffect(int layer, float duringTime, float enterTime, float fadeTime, float maxLerp,  float maxInst) : base(layer, duringTime, enterTime, fadeTime)
        {
            this.maxLerp = maxLerp;
            this.maxInst = maxInst;
            material = new Material(StormIsApproachingEntry.HueSeparation);
        }

        protected override float LerpAlpha => Mathf.Pow(base.LerpAlpha, 0.33f);

        public override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            base.OnRenderImage(source, destination);
            material.SetFloat("_Lerp", maxLerp * LerpAlpha);
            material.SetFloat("_Spit", maxInst * LerpAlpha);

            Graphics.Blit(source, destination, material);
        }

        private float maxLerp;
        private float maxInst;


    }
    internal class SingleColorEffect : BuffPostEffectLimitTime
    {
        public SingleColorEffect(int layer, float duringTime, float enterTime, float fadeTime, Color start, Color end, float maxInst) : base(layer, duringTime, enterTime, fadeTime)
        {
            this.start = start;
            this.end = end;
            this.maxInst = maxInst;
            material = new Material(StormIsApproachingEntry.SingleColor);

        }

        protected override float LerpAlpha => Mathf.Pow(base.LerpAlpha, 0.33f);

        public override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            base.OnRenderImage(source, destination);
            material.SetColor("singleColorStart", start);
            material.SetColor("singleColorEnd", end);
            material.SetFloat("lerpValue", maxInst * LerpAlpha);

            Graphics.Blit(source, destination, material);
        }

        private Color start;
        private Color end;
        private float maxInst;


    }

    internal class CutEffect : BuffPostEffectLimitTime
    {

        private Vector4[] lineParams = new[]
        {
            new Vector4(-0.3f, 1, 0.7f, 0.1f),
            new Vector4(0.6f, 1, 0.4f, -0.1f),
            new Vector4(4.6f, 1, -3f, 0.1f),
            new Vector4(-1.8f, 1, 0.8f, -0.05f)
        };
        protected override float LerpAlpha => Mathf.Pow(Mathf.InverseLerp(0, enterTime, 1 - lifeTime), 2f);


        private Vector4 GetRandomLine()
        {
            var posX = new Vector2(Random.value, Random.Range(0, 2));
            var posY = new Vector2(Random.Range(0, 2), Random.value);
            var k = (posX.y - posY.y) / (posX.x - posY.x);
            return new Vector4(k, 1,posX.y - k * posX.x,0);
        }

        public CutEffect(int layer, float duringTime, float enterTime,float fadeTime,int count = 7) : base(layer, duringTime, enterTime, fadeTime)
        {
            material = new Material(StormIsApproachingEntry.CutScreen);
            Array.Resize(ref lineParams, count);
            for (int i = 4; i < count; i++)
            {
                lineParams[i] = GetRandomLine();
                lineParams[i].w = Random.Range(-0.1f, 0.1f);
            }


        }

        public override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            base.OnRenderImage(source, destination);
            material.SetVectorArray("lineParams", lineParams);
            material.SetFloat("length", lineParams.Length * LerpAlpha);
            material.SetFloat("inst", 1);

            Graphics.Blit(source, destination, material);
        }
    }
}
