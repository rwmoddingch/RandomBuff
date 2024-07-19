using BuiltinBuffs.Duality;
using BuiltinBuffs.Negative;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuffUtils.FutileExtend;
using RandomBuffUtils.ObjectExtend;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RWCustom;
using Smoke;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using static BuiltinBuffs.Positive.FlameThrower;
using static RandomBuffUtils.FutileExtend.FMesh;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive
{

    internal class FlameThrowerBuff : IgnitionPointBaseBuff<FlameThrowerBuff, FlameThrowerBuffData>
    {
        public override BuffID ID => FlameThrowerBuffEntry.flameThrowerBuffID;


        public override bool Triggerable => true;

        public override bool Trigger(RainWorldGame game)
        {
            var room = game.cameras[0].room;

            foreach(var abRoom in game.cameras[0].room.world.abstractRooms)
            {
                foreach(var obj in abRoom.entities)
                {
                    if((obj is AbstractPhysicalObject abObj && abObj.type == AbstractFlameThrower.flameThrowerType))
                    {
                        if (abObj.realizedObject != null)
                            abObj.realizedObject.Destroy();
                        abObj.Destroy();
                    }
                }
            }

            var newFlame = new AbstractFlameThrower(game.world, null, game.Players[0].pos, game.GetNewID());
            room.abstractRoom.AddEntity(newFlame);
            newFlame.RealizeInRoom();

            return false;
        }
    }

    internal class FlameThrowerBuffData : BuffData
    {
        public override BuffID ID => FlameThrowerBuffEntry.flameThrowerBuffID;
    }

    internal class FlameThrowerBuffEntry : IBuffEntry
    {
        public static BuffID flameThrowerBuffID = new BuffID("FlameThrower", true);

        public static Mesh3DAsset flameThrowerMesh;
        public static Mesh3DAsset flameThrowerTubeMesh;
        public static string flameThrowerTexture;
        public static string flameVFX0;
        public static string flameVFX1;
        public static Texture2D flameThrowerTex;

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<FlameThrowerBuff, FlameThrowerBuffData, FlameThrowerBuffEntry>(flameThrowerBuffID);
        }

        public static void LoadAssets()
        {
            flameThrowerMesh = MeshManager.LoadMesh("flameThrower", AssetManager.ResolveFilePath(flameThrowerBuffID.GetStaticData().AssetPath + Path.DirectorySeparatorChar + "flameThrower.obj"));
            flameThrowerTubeMesh = MeshManager.LoadMesh("flameThrowerTube", AssetManager.ResolveFilePath(flameThrowerBuffID.GetStaticData().AssetPath + Path.DirectorySeparatorChar + "flameThrowerTube.obj"));

            flameThrowerTexture = Futile.atlasManager.LoadImage(flameThrowerBuffID.GetStaticData().AssetPath + Path.DirectorySeparatorChar + "flameThrowerTexture").elements[0].name;

            flameVFX0 = Futile.atlasManager.LoadImage(flameThrowerBuffID.GetStaticData().AssetPath + Path.DirectorySeparatorChar + "flameVFX0").elements[0].name;

            flameVFX1 = Futile.atlasManager.LoadImage(flameThrowerBuffID.GetStaticData().AssetPath + Path.DirectorySeparatorChar + "flameVFX1").elements[0].name;
        }

        public static void HookOn()
        {
            On.Player.Grabability += Player_Grabability;
        }

        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            if (obj is FlameThrower)
                return Player.ObjectGrabability.TwoHands;
            return orig(self, obj);
        }
    }

    [BuffAbstractPhysicalObject]
    public class AbstractFlameThrower : AbstractPhysicalObject
    {
        public static AbstractObjectType flameThrowerType = new AbstractObjectType("FlameThrower", true);

        public AbstractFlameThrower(World world, AbstractObjectType type, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID) : base(world, type, realizedObject, pos, ID)
        {
        }

        public AbstractFlameThrower(World world, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID) : this(world, flameThrowerType, realizedObject, pos, ID)
        {
        }

        public override void Realize()
        {
            base.Realize();
            realizedObject = new FlameThrower(this, world);
        }
    }

    public class FlameThrower : PlayerCarryableItem, IDrawable, IHeatingCreature
    {
        static float flameDistance = 45f;
        static int throwFlameCoolDown = 6;
        static float maxHeat = 40 * 4f;
        static float heatInfectRange = 160f;
        static float heatInfectHeight = 40f;

        Vector3 currentRotation3D;
        Vector3 lastRotation3D;

        Vector2 flamePosDelta;
        Vector2 dir = new Vector2(1f, 0f);

        bool throwFlame;
        int throwFlameCounter;

        float currentHeat;
        bool overHeat;

        float holdRotationAbs;

        ParticleEmitter flameEmitter;
        SteamSmoke overHeatSteam;

        public FlameThrower(AbstractPhysicalObject abstractPhysicalObject, World world) : base(abstractPhysicalObject)
        {
            bodyChunks = new BodyChunk[1];
            bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0f, 0f), 8.5f, 0.3f);
            bodyChunkConnections = new PhysicalObject.BodyChunkConnection[0];
            airFriction = 0.9f;
            gravity = 0.9f;
            bounce = 0.4f;
            surfaceFriction = 0.4f;
            collisionLayer = 1;
            waterFriction = 0.98f;
            buoyancy = 0.4f;

            currentRotation3D = lastRotation3D = new Vector3(180 + 70f, 0f, 0f);
            BuffUtils.Log("FlameThrower", $"Init at {abstractPhysicalObject.pos}");

            BuffUtils.Log("FlameThrower", Camera.main.depthTextureMode.ToString());
            Camera.main.depthTextureMode = DepthTextureMode.Depth;
        }

        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);
            base.firstChunk.HardSetPosition(placeRoom.MiddleOfTile(this.abstractPhysicalObject.pos));
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            lastRotation3D = currentRotation3D;
            throwFlame = false;
            Player player = null;
            if (grabbedBy != null && grabbedBy.Count > 0 && grabbedBy[0].grabber != null && grabbedBy[0].grabber is Player)
            {
                player = grabbedBy[0].grabber as Player;
                if(room.gravity > 0)
                {
                    if (player.input[0].x != 0)
                    {
                        dir = new Vector2(player.input[0].x, 0f);
                    }
                }
                else
                {
                    dir = player.input[0].analogueDir;
                }


                int pckpCount = 0;
                for (int i = 0; i < 8; i++)
                {
                    if (player.input[i].pckp)
                        pckpCount++;
                }

                if (pckpCount >= 6)
                {
                    throwFlame = true;
                }
            }

            //旋转控制
            if(room.gravity > 0)
            {
                currentRotation3D = Vector3.Lerp(currentRotation3D, new Vector3(180 + 70f * dir.x, 0f, 0f), 0.15f);
                //持枪倾斜
                if (!throwFlame && player != null)
                {
                    float vel = (firstChunk.pos - firstChunk.lastPos).magnitude / 20f;
                    holdRotationAbs = Mathf.Clamp(vel * 10, 0f, 20f);
                }
                else
                {
                    holdRotationAbs = 0f;
                }
                currentRotation3D = new Vector3(currentRotation3D.x, currentRotation3D.y + holdRotationAbs * dir.x, currentRotation3D.z);

                float radAngel = dir.x < 0 ? Mathf.PI - Mathf.Deg2Rad * Mathf.Abs(currentRotation3D.y) : Mathf.Deg2Rad * Mathf.Abs(currentRotation3D.y);
                //float radAngel = dir < 0 ? 180f - holdRotationAbs : holdRotationAbs;
                flamePosDelta = Vector2.Lerp(flamePosDelta, new Vector2(Mathf.Cos(radAngel), Mathf.Sin(radAngel)) * flameDistance, 0.15f);
            }
            else
            {
                currentRotation3D = Vector3.Lerp(currentRotation3D, new Vector3(180 + 70f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 0f), 0.15f);
                flamePosDelta = Vector2.Lerp(flamePosDelta, dir * flameDistance, 0.15f);
            }


            if (overHeat)//过热机制
            {
                throwFlame = false;
                currentHeat -= 0.5f;
                if (currentHeat <= 0f)
                    overHeat = false;
            }
            else
            {
                if (throwFlame)
                {
                    currentHeat += 1;
                    if (currentHeat >= maxHeat)
                    {
                        overHeat = true;
                    }
                }
                else if (currentHeat > 0f)
                    currentHeat -= 4;
            }


            //特效部分
            //枪口点火器火焰
            var fire = new HolyFire.HolyFireSprite(firstChunk.pos + Custom.RNV() * UnityEngine.Random.value * 3f + flamePosDelta);
            if (!throwFlame)
                fire.life = fire.lastLife = 0.5f;
            room.AddObject(fire);

            if (throwFlameCounter > 0)
                throwFlameCounter--;

            //火焰粒子系统
            if (throwFlame && flameEmitter == null)
            {
                CreateFlameEmitter();
            }
            else if (!throwFlame && flameEmitter != null)
            {
                flameEmitter.Die();
                flameEmitter = null;
            }
            if (flameEmitter != null)
                flameEmitter.pos = flamePosDelta + firstChunk.pos;

            //过热蒸汽
            if (overHeat)
            {
                if (overHeatSteam == null && room != null)
                {
                    overHeatSteam = new SteamSmoke(room);
                    room.AddObject(overHeatSteam);
                }
                else if (overHeatSteam.room != room)
                {
                    overHeatSteam.Destroy();
                    overHeatSteam = null;
                }
                else
                {
                    if(Random.value < 0.3f)
                    {
                        Vector2 pos = firstChunk.pos + flamePosDelta * Random.value;
                        overHeatSteam.EmitSmoke(pos, Vector2.up * 5f + Custom.RNV(), new FloatRect(pos.x - 1f, pos.y - 5f, pos.x + 1f, pos.y + 5f), currentHeat * 0.3f / maxHeat);
                    }
                }

                if(Random.value < currentHeat / maxHeat)
                    room.PlaySound(SoundID.Gate_Electric_Steam_Puff, firstChunk, false, (currentHeat / maxHeat) * 0.5f, 1f + (Random.value * 0.1f - 0.05f));
            }
            else
            {
                if(overHeatSteam != null)
                {
                    overHeatSteam.Destroy();
                    overHeatSteam = null;
                }
            }

            //额外作用力
            if (throwFlame && player != null)
            {
                player.firstChunk.vel += -dir * 0.1f;
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (FlameThrowerBuffEntry.flameThrowerMesh == null)
            {
                FlameThrowerBuffEntry.LoadAssets();
            }

            sLeaser.sprites = new FSprite[3];
            sLeaser.sprites[1] = new FMesh(FlameThrowerBuffEntry.flameThrowerMesh, FlameThrowerBuffEntry.flameThrowerTexture)
            {
                shader = rCam.game.rainWorld.Shaders["UniformSimpleLighting"]
            };

            sLeaser.sprites[0] = new FMesh(FlameThrowerBuffEntry.flameThrowerTubeMesh, "Futile_White")
            {
                shader = rCam.game.rainWorld.Shaders["UniformSimpleLighting"]
            };
            sLeaser.sprites[2] = new FSprite(FlameThrowerBuffEntry.flameVFX1) { shader = rCam.game.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"] , scaleX = 2f, scaleY = 0.5f, color = Color.red};

            AddToContainer(sLeaser, rCam, null);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Items");
            }

            for (int num = sLeaser.sprites.Length - 2; num >= 0; num--)
            {
                sLeaser.sprites[num].RemoveFromContainer();
                newContatiner.AddChild(sLeaser.sprites[num]);
            }
            sLeaser.sprites[2].RemoveFromContainer();
            rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[2]);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
        }


        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector3 smoothRotation = Vector3.Lerp(lastRotation3D, currentRotation3D, timeStacker);
            Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker) - camPos;
            sLeaser.sprites[0].SetPosition(pos);
            sLeaser.sprites[1].SetPosition(pos);

            var fmesh = sLeaser.sprites[1] as FMesh;
            fmesh.rotation3D = smoothRotation;
            fmesh.Scale3D = Vector3.one * 1.5f;

            var fmesh2 = sLeaser.sprites[0] as FMesh;
            fmesh2.rotation3D = smoothRotation;
            fmesh2.Scale3D = Vector3.one * 1.5f;
            fmesh2.color = CaculateTubeCol(currentHeat / maxHeat);

            sLeaser.sprites[2].SetPosition(pos + flamePosDelta / 2f);
            sLeaser.sprites[2].rotation = Custom.VecToDeg(flamePosDelta) + 90f;
            sLeaser.sprites[2].alpha = currentHeat / maxHeat;
        }

        Color CaculateTubeCol(float l)
        {
            if (l < 0.1f)
                return RoomFlame.flameCol_1;
            else if (l < 0.7f)
                return Color.Lerp(RoomFlame.flameCol_1, RoomFlame.flameCol_3, (l - 0.1f) / 0.6f);
            else
                return Color.Lerp(RoomFlame.flameCol_3, RoomFlame.flameCol_4, (l - 0.7f) / 0.3f);
        }

        public void CreateFlameEmitter()
        {
            var emitter = new ParticleEmitter(room);
            emitter.ApplyParticleSpawn(new RateSpawnerModule(emitter, 200, 80));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", FakeCreatureEntry.Turbulent.name, 8, 0.1f, 4f, new Color(0.3f, 0.5f, 10f))));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight", 8, alpha: 0.05f, scale: 6f, constCol: Color.black)));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam(FlameThrowerBuffEntry.flameVFX1, "StormIsApproaching.AdditiveDefault", 8, alpha: 0.1f, scale: 2f)));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam(FlameThrowerBuffEntry.flameVFX0, "StormIsApproaching.AdditiveDefault", 8)));

            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 10, 50));
            emitter.ApplyParticleModule(new SetRandomPos(emitter, 2f));
            emitter.ApplyParticleModule(new SetRandomRotation(emitter, 0f, 360f));

            emitter.ApplyParticleModule(new FlameVelociy(emitter, this));

            emitter.ApplyParticleModule(new ColorOverLife(emitter, (p, l) =>
            {
                if (l < 0.1f)
                    return Color.Lerp(RoomFlame.flameCol_0, RoomFlame.flameCol_1, l * 10f);
                else if (l < 0.25f)
                    return Color.Lerp(RoomFlame.flameCol_1, RoomFlame.flameCol_2, (l - 0.1f) / 0.15f);
                else if (l < 0.4f)
                    return Color.Lerp(RoomFlame.flameCol_2, RoomFlame.flameCol_3, (l - 0.25f) / 0.15f);
                else if (l < 0.8f)
                    return Color.Lerp(RoomFlame.flameCol_3, RoomFlame.flameCol_4, (l - 0.4f) / 0.4f);
                else
                    return Color.Lerp(RoomFlame.flameCol_4, RoomFlame.flameCol_5, (l - 0.8f) / 0.2f);
            }));

            emitter.ApplyParticleModule(new ScaleOverLife(emitter, (p, l) =>
            {
                if (l < 0.8f)
                    return Mathf.Lerp(0.25f, 2f, l / 0.8f);
                else
                    return Mathf.Lerp(2f, 4f, (l - 0.8f) / 2f);
            }));

            emitter.ApplyParticleModule(new AlphaOverLife(emitter, (p, l) =>
            {
                //if (l < 0.5f)
                //    return Mathf.Lerp(0f, 1f, l * 2f);
                if (l >= 0.9f)
                    return Mathf.Lerp(1f, 0f, (l - 0.9f) / 0.1f);
                return 1f;
            }));

            emitter.ApplyParticleModule(new VelocityOverLife(emitter, (p, l) =>
            {
                if (l < 0.8f)
                    return p.vel + Vector2.down * p.emitter.room.gravity * 0.125f;
                else
                    return p.vel + Vector2.up * p.emitter.room.gravity * 0.5f;
            }));
            emitter.ApplyParticleModule(new SimpleParticlePhysic(emitter, true, false, 0.1f)
            {
                OnTerrainCollide = (p) =>
                {
                    if (throwFlameCounter == 0)
                    {
                        //emitter.room.AddObject(new Napalm(emitter.room, 40 * 10, 30f, 4f, p.pos, p.vel * 0.5f));
                        RoomFlame.GetRoomFlame(room).ApplyFire(p.pos, 40f);
                        throwFlameCounter = throwFlameCoolDown;
                    }
                }
            });


            ParticleSystem.ApplyEmitterAndInit(emitter);
            flameEmitter = emitter;
        }


        public float GetHeat(UpdatableAndDeletable updatableAndDeletable, Vector2 pos)
        {
            if (!throwFlame)
                return 0f;

            float distance = Vector2.Distance(pos, flamePosDelta + firstChunk.pos);
            if (distance > heatInfectRange)
                return 0f;

            Vector2 posDir = (pos- (flamePosDelta + firstChunk.pos)).normalized;
            float cos = Vector2.Dot(posDir, dir.normalized);
            if (cos <= 0f)
                return 0f;

            float height = Mathf.Sin(Mathf.Acos(cos)) * distance;
            if (height < heatInfectHeight)
                return 1f / 40f;

            return 0f;
        }

        public class FlameVelociy : EmitterModule, IParticleInitModule
        {
            FlameThrower flameThrower;
            public FlameVelociy(ParticleEmitter emitter, FlameThrower flameThrower) : base(emitter)
            {
                this.flameThrower = flameThrower;
            }

            public void ApplyInit(Particle particle)
            {
                particle.SetVel(10f * flameThrower.dir + Custom.DegToVec(particle.randomParam1 * 360f) * 0.3f);
            }
        }

        public class FlameNaplamSpawner : EmitterModule, IParticleUpdateModule
        {
            FlameThrower flameThrower;
            public FlameNaplamSpawner(ParticleEmitter emitter, FlameThrower flameThrower) : base(emitter)
            {
                this.flameThrower = flameThrower;
            }

            public void ApplyUpdate(Particle particle)
            {
                if (particle.randomParam1 < 0.6f)
                    return;
                if (particle.LifeParam < 0.8f && particle.LifeParam < particle.randomParam2 && flameThrower.throwFlameCounter == 0)
                {
                    emitter.room.AddObject(new Napalm(emitter.room, 40 * 10, 30f, 4f, particle.pos, particle.vel * 0.5f));
                    flameThrower.throwFlameCounter = FlameThrower.throwFlameCoolDown;
                }
            }
        }

    }
}
