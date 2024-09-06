using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuffUtils.ObjectExtend;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive
{
    internal class OrbitalRailcannonStrikeEntry : IBuffEntry
    {
        public static readonly BuffID OrbitalRailcannonStrike = new BuffID(nameof(OrbitalRailcannonStrike), true);

        public static readonly SoundID OrbitalStrike = new SoundID(nameof(OrbitalStrike), true);


        public void OnEnable()
        {
            BuffRegister.RegisterBuff<OrbitalRailcannonStrikeBuff,OrbitalRailcannonStrikeBuffData>(OrbitalRailcannonStrike);
        }

        public static void LoadAssets()
        {
            BuffSounds.LoadSound(OrbitalStrike,OrbitalRailcannonStrike.GetStaticData().AssetPath,new BuffSoundGroupData(),new BuffSoundData("orbitalStrike"));
            var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath(OrbitalRailcannonStrike.GetStaticData().AssetPath +
                                                                  Path.DirectorySeparatorChar + "railstrike"));
            screenTex = bundle.LoadAsset<Texture2D>("T_FX_Tile_0021");
            addiveTex = bundle.LoadAsset<Texture2D>("T_FX_Tile_0011");
            Custom.rainWorld.Shaders.Add($"{OrbitalRailcannonStrike}.TopLeaser", FShader.CreateShader($"{OrbitalRailcannonStrike}.TopLeaser", bundle.LoadAsset<Shader>("TopLeaser")));
            Custom.rainWorld.Shaders.Add($"{OrbitalRailcannonStrike}.LowLeaser", FShader.CreateShader($"{OrbitalRailcannonStrike}.LowLeaser", bundle.LoadAsset<Shader>("LowLeaser")));
            Custom.rainWorld.Shaders.Add($"{OrbitalRailcannonStrike}.LeaserLight", FShader.CreateShader($"{OrbitalRailcannonStrike}.LeaserLight", bundle.LoadAsset<Shader>("LeaserLight")));
            radialBlur = bundle.LoadAsset<Shader>("RadialBlur");
        }



        public static void HookOn()
        {
            On.Player.Grabability += Player_Grabability;
        }

        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            if (obj is RailStrike)
                return Player.ObjectGrabability.OneHand;
            return orig(self, obj);
        }

        public static Texture2D screenTex;
        public static Texture2D addiveTex;

        public static Shader radialBlur;

    }

    internal class OrbitalRailcannonStrikeBuff : Buff<OrbitalRailcannonStrikeBuff, OrbitalRailcannonStrikeBuffData>
    {
        public override BuffID ID => OrbitalRailcannonStrikeEntry.OrbitalRailcannonStrike;

        public OrbitalRailcannonStrikeBuff()
        {
            MyTimer = new DownCountBuffTimer((timer, game) =>
            {
                canTrigger = true;

            }, 240,autoReset:false);
            MyTimer.Paused = true;
        }

        private bool canTrigger = true;
        public override bool Triggerable => canTrigger;

        public override bool Trigger(RainWorldGame game)
        {
            var player = game.cameras[0].followAbstractCreature.realizedCreature as Player;
            if (player == null && game.AlivePlayers.Count != 0)
                player = game.AlivePlayers.FirstOrDefault(i => i.realizedCreature is Player && i.realizedCreature.room != null)?.realizedCreature as Player;
            if (player == null)
                player = game.Players.FirstOrDefault(i => i.realizedCreature is Player && i.realizedCreature.room != null)?.realizedCreature as Player;
            if (player == null)
                return false;


            AbstractRailStrike rail = new AbstractRailStrike(player.abstractCreature.world,
                AbstractRailStrike.RailStrike, null, player.abstractCreature.pos, game.GetNewID());
            player.room.abstractRoom.AddEntity(rail);
            railStrikes.Add(rail);
            rail.RealizeInRoom();
            if (player.FreeHand() != -1 && !player.dead)
                player.SlugcatGrab(rail.realizedObject, player.FreeHand());
            else
                rail.realizedObject.firstChunk.HardSetPosition(player.DangerPos);
            canTrigger = false;
            MyTimer.Reset();
            MyTimer.Paused = false;
            return false;
        }

        public override void Destroy()
        {
            base.Destroy();
            railStrikes.ForEach(i =>
            {
                if (i.realizedObject != null)
                    i.realizedObject.Destroy();
                else
                    i.Destroy();
            });
            railStrikes.Clear();
        }

        private readonly List<AbstractRailStrike> railStrikes = new List<AbstractRailStrike>();
    }

    internal class OrbitalRailcannonStrikeBuffData : BuffData
    {
        public override BuffID ID => OrbitalRailcannonStrikeEntry.OrbitalRailcannonStrike;
    }


    internal class AbstractRailStrike : AbstractPhysicalObject
    {
        public static readonly AbstractObjectType RailStrike = new AbstractObjectType(nameof(RailStrike), true);
        public AbstractRailStrike(World world, AbstractObjectType type, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID) : base(world, type, realizedObject, pos, ID)
        {
        }

        public override void Realize()
        {
            base.Realize();
            if (realizedObject == null)
                realizedObject = new RailStrike(this, world);
            
        }
    }

    internal class RailStrike : Weapon
    {
        private bool hasThrow = false;

        public RailStrike(AbstractPhysicalObject abstractPhysicalObject, World world) : base(abstractPhysicalObject, world)
        {
            base.bodyChunks = new BodyChunk[1];
            base.bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0f, 0f), 5f, 0.07f);
            this.bodyChunkConnections = Array.Empty<BodyChunkConnection>();
            base.airFriction = 0.999f;
            base.gravity = 0.9f;
            this.bounce = 0.4f;
            this.surfaceFriction = 0.4f;
            this.collisionLayer = 2;
            base.waterFriction = 0.98f;
            base.buoyancy = 0.4f;
            base.firstChunk.loudness = 9f;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            counter++;
            lastColor = color;
            lastAlpha = alpha;
            alpha = Mathf.Lerp(alpha, hasThrow ? 0 : 1, 0.1f);
            color = Color.Lerp(color, hasThrow ? Color.gray : AlarmColor, 0.1f);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[7];
            for (int i = 0; i < 6; i++)
                sLeaser.sprites[i] = new FSprite("Futile_White");

            for (int i = 1; i < 5; i++)
            {
                sLeaser.sprites[i].width = SideWidth;
                sLeaser.sprites[i].height = SideWidth;
            }

            sLeaser.sprites[0].width = CenterWidth;
            sLeaser.sprites[0].height = CenterWidth;
            sLeaser.sprites[0].color = Color.Lerp(Color.gray,Color.white,0.5f);

            sLeaser.sprites[6] = new FSprite("Circle20");
            sLeaser.sprites[6].color = AlarmColor;
            sLeaser.sprites[6].scale = 0.4f;
            sLeaser.sprites[5].shader = rCam.game.rainWorld.Shaders["FlatLightBehindTerrain"];
            sLeaser.sprites[5].color = AlarmColor;
            sLeaser.sprites[5].scale = 5;
            lastColor = color = AlarmColor;
            AddToContainer(sLeaser,rCam,null);
        }

        public static readonly Color AlarmColor = Custom.hexToColor("FF7F27");

        public const float Width = 7;
        public const float CenterWidth = 10;

        public const float SideWidth = 6;

        private Color color;
        private Color lastColor;

        private int counter;

        private float lastAlpha;
        private float alpha;


        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (slatedForDeletetion)
            {
                sLeaser.CleanSpritesAndRemove();
                return;
            }
            var rot = Vector2.Lerp(lastRotation, rotation, timeStacker);
            var hRot = Vector2.Perpendicular(rot);
            var degRot = Custom.AimFromOneVectorToAnother(Vector2.zero, rot);
            var centerPos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
            for (int i = 0; i < 5; i++)
            {
                sLeaser.sprites[i].rotation = degRot + (i != 0 ? 45 : 0);
                sLeaser.sprites[i].SetPosition(centerPos - camPos + Custom.fourDirectionsAndZero[i].x * Width * rot + Custom.fourDirectionsAndZero[i].y * Width * hRot);
            }

            sLeaser.sprites[5].color = Color.Lerp(lastColor, color, timeStacker);
            sLeaser.sprites[5].SetPosition(centerPos - camPos);
            sLeaser.sprites[5].alpha = Mathf.Lerp(lastAlpha, alpha, timeStacker) * (Mathf.Sin((counter+timeStacker)/40 * Mathf.PI) + 1)/2f;
            sLeaser.sprites[6].color = Color.Lerp(lastColor, color, timeStacker);
            sLeaser.sprites[6].SetPosition(centerPos - camPos);

        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
                newContatiner = rCam.ReturnFContainer("Items");
            for (int num = 0; num < sLeaser.sprites.Length; num++)
            {
                if(num==5)
                    continue;
                sLeaser.sprites[num].RemoveFromContainer();
                newContatiner.AddChild(sLeaser.sprites[num]);
            }
            sLeaser.sprites[5].RemoveFromContainer();
            rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[5]);

        }

        public override void ChangeMode(Mode newMode)
        {
            var sourcePos = firstChunk.pos + Vector2.up * 20 * 100;
            if (newMode == Mode.Free && mode == Mode.Thrown && hasThrow == false && 
                room.VisualContact(firstChunk.pos, sourcePos))
            {
                hasThrow = true;
                BuffUtils.Log(OrbitalRailcannonStrikeEntry.OrbitalRailcannonStrike,"calling rail strike");
                room.AddObject(new RailStrikeEffect(sourcePos, room, this));
            }
            base.ChangeMode(newMode);
       
        }
    }


    public class RailStrikeEffect : CosmeticSprite
    {

        private PhysicalObject targetObject;
        private Weapon sourceObject;

        private int cdCounter = MaxCd;

        const int MaxCd = 5 * 20;

        private Vector2 aimPos;
        private Vector2 lastAimPos;

        private float alpha = 0;
        private float lastAlpha = 0;
        private float lastVal = 0;


        private bool isDirty;
        private RoomSettings.RoomEffect darkness;
        private bool needDeleteDarkness;

        public RailStrikeEffect(Vector2 sourcePos, Room room, Weapon source)
        {

            pos = sourcePos;
            sourceObject = source;
            this.room = room;
            targetObject = source;
            float maxValue = float.MinValue;
            int canHitThisCreature = 0;
            darkness = room.roomSettings.effects.FirstOrDefault(i => i.type == RoomSettings.RoomEffect.Type.Darkness);
            if (darkness == null)
            {
                needDeleteDarkness = true;
                darkness = new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Darkness, 0, false);
                room.roomSettings.effects.Add(darkness);
            }

            lastVal = darkness.amount;

            aimPos = lastAimPos = source.firstChunk.pos;
            foreach (var crit in room.abstractRoom.creatures.Select(i => i.realizedCreature))
            {
                if (crit.Template.dangerousToPlayer == 0) continue;
                var canHit = room.VisualContact(pos, crit.DangerPos) ? 1 : 0;
                if (canHit*1000 + crit.Template.dangerousToPlayer > canHitThisCreature *1000 + maxValue)
                {
                    maxValue = crit.Template.dangerousToPlayer;
                    canHitThisCreature = canHit;
                    targetObject = crit;
                }
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[3];
  

            sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(1, false, false);
            sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders[$"{OrbitalRailcannonStrikeEntry.OrbitalRailcannonStrike}.LowLeaser"];
            sLeaser.sprites[0].alpha = 0;
            sLeaser.sprites[1] = TriangleMesh.MakeLongMesh(1, false, false);
            sLeaser.sprites[1].shader = rCam.game.rainWorld.Shaders[$"{OrbitalRailcannonStrikeEntry.OrbitalRailcannonStrike}.TopLeaser"];
            sLeaser.sprites[1].alpha = 0;

            sLeaser.sprites[2] = TriangleMesh.MakeLongMesh(1, false, false);
            sLeaser.sprites[2].shader = rCam.game.rainWorld.Shaders[$"{OrbitalRailcannonStrikeEntry.OrbitalRailcannonStrike}.LeaserLight"];
            sLeaser.sprites[2].color = Custom.hexToColor("FF2200");
            sLeaser.sprites[2].alpha = 0;


            isDirty = true;
            AddToContainer(sLeaser, rCam, null);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

            if (slatedForDeletetion)
            {
                sLeaser.RemoveAllSpritesFromContainer();
                return;
            }


            var startLerp = Mathf.Pow(Mathf.InverseLerp(20, 40, MaxCd - (cdCounter - timeStacker)), 0.3f) *
                            Mathf.Pow( Mathf.InverseLerp(-40,0, cdCounter-timeStacker), 0.3f);
            if (isDirty)
            {
                bool hasSet = true;
                for (int i = 0; i < 2; i++)
                {
                    if (sLeaser.sprites[i]?._renderLayer?._material is Material mat)
                    {
                        mat.SetTexture("_ScreenTex", OrbitalRailcannonStrikeEntry.screenTex);
                        mat.SetTexture("_AddiveTex", OrbitalRailcannonStrikeEntry.addiveTex);
                    }
                    else
                        hasSet = false;
                    
                }
                isDirty = !hasSet;
            }


            for (int i = 0; i < 2; i++)
            {
                var sprite = sLeaser.sprites[i];
             
                sprite.color = new Color(Mathf.Lerp((i == 0) ? 1 : 3, (i == 0) ? 3 : 12, startLerp), 
                    (i == 0 ? 0.45f : 0.3f) * Mathf.Lerp(3, 0.6f, startLerp), 
                    (i == 0 ? 0.45f : 0.3f) * Mathf.Lerp(3, 0.6f, startLerp), 
                    Mathf.Lerp(lastAlpha, alpha, timeStacker));
            }
            foreach (var sprite in sLeaser.sprites)
                sprite.alpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);


            var toPos = Vector2.Lerp(lastAimPos, aimPos, timeStacker);
            var re = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(room, pos, toPos);
            var dir = Custom.DirVec(pos, toPos);
            var hDir = Vector2.Perpendicular(dir);

            toPos = re == null ? toPos : pos+Custom.Dist(pos,room.MiddleOfTile(re.Value))*dir;
      
            SetPoint(sLeaser.sprites[0], 100);
            SetPoint(sLeaser.sprites[1], 25);
            SetPoint(sLeaser.sprites[2], 150, 40);
         
            void SetPoint(FSprite sprite, float width,float exLength = 0)
            {
                var mesh = sprite as TriangleMesh;
                mesh.MoveVertice(0, pos + width * hDir - camPos);
                mesh.MoveVertice(1, pos - width * hDir - camPos);
                mesh.MoveVertice(2, toPos + exLength *dir + width * hDir - camPos);
                mesh.MoveVertice(3, toPos + exLength * dir- width * hDir - camPos);
            }
        }


        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if(newContatiner == null)
                newContatiner = rCam.ReturnFContainer("HUD");
            for(int i =0;i<2;i++)
                newContatiner.AddChild(sLeaser.sprites[i]);
            rCam.ReturnFContainer("ForegroundLights").AddChild(sLeaser.sprites[2]);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if(MaxCd-cdCounter == 16)
                room.PlaySound(OrbitalRailcannonStrikeEntry.OrbitalStrike, sourceObject.firstChunk);
            lastAimPos = aimPos;
            lastAlpha = alpha;
            darkness.amount = Custom.LerpMap(MaxCd - cdCounter, 0, 40, lastVal, Mathf.Min(lastVal + 0.5f, 0.7f), 0.5f) *
                              Custom.LerpMap(cdCounter, -40, 0, 0, 1f, 0.5f);
            if (targetObject.room != room)
                targetObject = sourceObject;
            alpha = Mathf.Lerp(alpha, cdCounter > 0 && (MaxCd-cdCounter > 20) ? 1 : 0, 0.05f);
            aimPos = Vector2.Lerp(aimPos, targetObject.firstChunk.pos, 0.1f * Mathf.Lerp(0.3f,1f,alpha));
            cdCounter--;

            if (cdCounter == 0) Explode(); 
      
            if (cdCounter == -40) Destroy();
        }

        public void Explode()
        {
            Creature hitCreature = null;
            if (targetObject is Creature creature &&
                room.VisualContact(pos,creature.mainBodyChunk.pos))
                hitCreature = creature;
            var toPos = aimPos;
            var tile = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(room, pos, toPos);
            var dir = Custom.DirVec(pos, toPos);
            toPos = tile == null ? toPos : pos + Custom.Dist(pos, room.MiddleOfTile(tile.Value)) * dir;

            room.AddObject(new RailStrikeExplode(room,hitCreature, toPos,dir,sourceObject));
         
            darkness.amount = lastVal;
            if (needDeleteDarkness)
                room.roomSettings.RemoveEffect(RoomSettings.RoomEffect.Type.Darkness);
        }
    }

    public class RailStrikeExplode : UpdatableAndDeletable
    {
        private Vector2 pos;
        private int counter = 10;
        private ParticleEmitter emitter;
        public RailStrikeExplode(Room room, Creature hitCreature, Vector2 damagePos, Vector2 dir, Weapon sourceObject)
        {
            this.room = room;
            if (hitCreature != null)
                hitCreature.Die();
            pos = damagePos;
            var deg = Custom.VecToDeg(-dir);
        
            room.AddObject(new Explosion.ExplosionLight(pos, 450, 1, 20, Color.white));
            room.AddObject(new Explosion(room, sourceObject, pos, 40,400, 6.2f, 70F, 400f, 0.25f, sourceObject.thrownBy, 
                1.4f, 160f, 1f));
            room.AddObject(new ShockWave(pos, 450, 0.5f, 8, true));

            emitter = new ParticleEmitter(room);
            emitter.pos = emitter.lastPos = pos;
            emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter,Random.Range(15,35)));
            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Pebble1", string.Empty)));

            emitter.ApplyParticleModule(new SetRandomPos(emitter, 40));
            emitter.ApplyParticleModule(new SetRandomVelocity(emitter, Custom.DegToVec(-45 + deg)*20,Custom.DegToVec(45 + deg)*40));
            emitter.ApplyParticleModule(new VelocityOverLife(emitter, (particle, time) 
                => particle.vel*0.99f + Vector2.down /4f * emitter.room.gravity));
            emitter.ApplyParticleModule(new RotationOverLife(emitter, (particle, time) =>
                particle.rotation + Mathf.Sign(particle.vel.x) * Custom.LerpMap(particle.vel.magnitude, 5, 20, 10, 80) / 40f * (1 - time)));
            emitter.ApplyParticleModule(new SetRandomScale(emitter, 1f, 0.15f));
            emitter.ApplyParticleModule(new SetRandomRotation(emitter, 0, 360));
            emitter.ApplyParticleModule(new SetConstColor(emitter, new Color(0.01f, 0.01f, 0.01f)));
            emitter.ApplyParticleModule(new SimpleParticlePhysic(emitter,true,false));
            emitter.ApplyParticleModule(new AlphaOverLife(emitter, ((particle, time) =>
                particle.alpha = Mathf.Pow(1 - time, 0.5f))));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 80, 160));
            emitter.ApplyParticleModule(new Gravity(emitter, 0.9f));

            for (int i = 0; i < 35; i++)
            {
                room.AddObject(new Explosion.FlashingSmoke(pos + Custom.RNV() * 60f * Random.value,
                    Custom.RNV() * Mathf.Lerp(4f, 20f, Mathf.Pow(Random.value, 2f)), 
                    1.2f + 0.05f * Random.value, new Color(1f, 1f, 1f), Color.white, Random.Range(7, 22)));
            }


            ParticleSystem.ApplyEmitterAndInit(emitter);

            if (room.game.cameras[0].room == room)
                BuffPostEffectManager.AddEffect(new BurstRadialBlurEffect(0, 0.27f, 0.05f, 0.15f, (pos - room.game.cameras[0].pos) / Custom.rainWorld.screenSize));
            

            room.game.cameras[0].ScreenMovement(damagePos,Vector2.up*40,2);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            counter--;
            if (counter == 0)
                Destroy();
            
        }

        public override void Destroy()
        {
            emitter.Die();
        }
    }

    public class BurstRadialBlurEffect : BuffPostEffectLimitTime
    {
        private float blurFactor;
        private Vector2 blurCenter;
        private float lerpFactor;


        private int downSampleFactor;

        public BurstRadialBlurEffect(int layer, float duringTime, float enterTime, float fadeTime, Vector2? blurCenter = null, float blurFactor = 0.1f, float lerpFactor = 0.8f, int downSampleFactor = 2) 
            : base(layer, duringTime,enterTime,fadeTime)
        {
            if (!blurCenter.HasValue) blurCenter = new Vector2(0.5f, 0.5f);
            this.lerpFactor = lerpFactor;
            this.blurFactor = blurFactor;
            this.blurCenter = blurCenter.Value;
            this.downSampleFactor = downSampleFactor;
            material = new Material(OrbitalRailcannonStrikeEntry.radialBlur);
        }

        protected override float LerpAlpha => Mathf.Pow(base.LerpAlpha, 0.33f);

        public override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            base.OnRenderImage(source, destination);
            RenderTexture rt1 = RenderTexture.GetTemporary(source.width >> downSampleFactor, source.height >> downSampleFactor, 0, source.format);
            RenderTexture rt2 = RenderTexture.GetTemporary(source.width >> downSampleFactor, source.height >> downSampleFactor, 0, source.format);
            Graphics.Blit(source, rt1);
            Graphics.Blit(rt1, rt2, material, 0);

            material.SetTexture("_BlurTex", rt2);
            material.SetFloat("_BlurFactor", blurFactor * LerpAlpha);
            material.SetFloat("_LerpFactor", lerpFactor * LerpAlpha);
            material.SetVector("_BlurCenter", blurCenter);
            Graphics.Blit(source, destination, material, 1);
            RenderTexture.ReleaseTemporary(rt1);
            RenderTexture.ReleaseTemporary(rt2);
        }
    }
}
