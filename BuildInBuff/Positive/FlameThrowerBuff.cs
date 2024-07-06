using BuiltinBuffs.Duality;
using RandomBuffUtils.FutileExtend;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils;
using RandomBuffUtils.ObjectExtend;
using RandomBuff.Core.Entry;
using static RandomBuffUtils.FutileExtend.FMesh;
using UnityEngine;
using RandomBuff.Core.Buff;
using System.IO;
using RandomBuff;
using RWCustom;
using Smoke;
using System;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static BuiltinBuffs.Positive.FlameThrower;
using static TriangleMesh;
using System.Linq;

namespace BuiltinBuffs.Positive
{

    internal class FlameThrowerBuff : Buff<FlameThrowerBuff, FlameThrowerBuffData>
    {
        public override BuffID ID => FlameThrowerBuffEntry.flameThrowerBuffID;


        public override bool Triggerable => true;

        public override bool Trigger(RainWorldGame game)
        {
            var room = game.cameras[0].room;
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
                return Player.ObjectGrabability.BigOneHand;
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

    public class FlameThrower : PlayerCarryableItem, IDrawable
    {
        static float flameDistance = 45f;
        static int throwFlameCoolDown = 6;
        static float maxHeat = 40 * 4f;

        Vector3 currentRotation3D;
        Vector3 lastRotation3D;

        Vector2 flamePosDelta;
        int dir = 1;

        bool throwFlame;
        int throwFlameCounter;

        float currentHeat;
        bool overHeat;

        ParticleEmitter flameEmitter;
        SteamSmoke overHeaetSteam;

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
            if (grabbedBy != null && grabbedBy.Count > 0 && grabbedBy[0].grabber != null && grabbedBy[0].grabber is Player player)
            {
                if (player.input[0].x != 0)
                {
                    dir = player.input[0].x;
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
                    if (currentHeat > maxHeat)
                    {
                        overHeat = true;
                        room.PlaySound(SoundID.Gate_Electric_Steam_Puff, firstChunk, false, 0.5f, 1f);
                    }
                }
                else if (currentHeat > 0f)
                    currentHeat -= 4;
            }

            currentRotation3D = Vector3.Lerp(currentRotation3D, new Vector3(180 + 70f * dir, 0f, 0f), 0.15f);
            flamePosDelta = Vector2.Lerp(flamePosDelta, new Vector2(flameDistance * dir, 0f), 0.15f);

            //特效部分
            //枪口点火器火焰
            var fire = new HolyFire.HolyFireSprite(firstChunk.pos + Custom.RNV() * UnityEngine.Random.value * 3f + flamePosDelta);
            if (!throwFlame)
                fire.life = fire.lastLife = 0.5f;
            room.AddObject(fire);

            if (throwFlameCounter > 0)
                throwFlameCounter--;

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

            if (overHeat)
            {
                if (overHeaetSteam == null)
                {
                    overHeaetSteam = new SteamSmoke(room);
                    room.AddObject(overHeaetSteam);
                }
                else if (overHeaetSteam.room != room)
                {
                    overHeaetSteam.Destroy();
                    overHeaetSteam = null;
                }
                else
                {
                    Vector2 pos = firstChunk.pos + flamePosDelta * Random.value;
                    overHeaetSteam.EmitSmoke(pos, Vector2.up * 5f + Custom.RNV(), new FloatRect(pos.x - 5f, pos.y - 5f, pos.x + 5f, pos.y + 5f), currentHeat * 0.3f / maxHeat);
                }
            }
            else
            {
                if(overHeaetSteam != null)
                {
                    overHeaetSteam.Destroy();
                    overHeaetSteam = null;
                }
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (FlameThrowerBuffEntry.flameThrowerMesh == null)
            {
                FlameThrowerBuffEntry.LoadAssets();
            }

            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[1] = new FMesh(FlameThrowerBuffEntry.flameThrowerMesh, FlameThrowerBuffEntry.flameThrowerTexture, false)
            {
                shader = rCam.game.rainWorld.Shaders["UniformSimpleLighting"]
            };

            sLeaser.sprites[0] = new FMesh(FlameThrowerBuffEntry.flameThrowerTubeMesh, "Futile_White", false)
            {
                shader = rCam.game.rainWorld.Shaders["UniformSimpleLighting"]
            };

            AddToContainer(sLeaser, rCam, null);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Items");
            }

            for (int num = sLeaser.sprites.Length - 1; num >= 0; num--)
            {
                sLeaser.sprites[num].RemoveFromContainer();
                newContatiner.AddChild(sLeaser.sprites[num]);
            }
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
        }

        Color CaculateTubeCol(float l)
        {
            if (l < 0.1f)
                return flameCol_1;
            else if (l < 0.7f)
                return Color.Lerp(flameCol_1, flameCol_3, (l - 0.1f) / 0.6f);
            else
                return Color.Lerp(flameCol_3, flameCol_4, (l - 0.7f) / 0.3f);
        }

        public static Color flameCol_0 = Color.blue * 0.1f + Color.red * 1f + Color.white * 0.8f;
        public static Color flameCol_1 = Color.white;
        public static Color flameCol_2 = Color.white * 0.5f + Color.yellow * 0.5f;
        public static Color flameCol_3 = Color.yellow * 0.6f + Color.red * 0.4f;
        public static Color flameCol_4 = Color.red * 0.8f + Color.yellow * 0.2f;
        public static Color flameCol_5 = Color.black;
        public void CreateFlameEmitter()
        {
            var emitter = new ParticleEmitter(room);
            emitter.ApplyParticleSpawn(new RateSpawnerModule(emitter, 200, 80));

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
                    return Color.Lerp(flameCol_0, flameCol_1, l * 10f);
                else if (l < 0.25f)
                    return Color.Lerp(flameCol_1, flameCol_2, (l - 0.1f) / 0.15f);
                else if (l < 0.4f)
                    return Color.Lerp(flameCol_2, flameCol_3, (l - 0.25f) / 0.15f);
                else if (l < 0.8f)
                    return Color.Lerp(flameCol_3, flameCol_4, (l - 0.4f) / 0.4f);
                else
                    return Color.Lerp(flameCol_4, flameCol_5, (l - 0.8f) / 0.2f);
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
                if (l < 0.5f)
                    return Mathf.Lerp(0f, 1f, l * 2f);
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
                        GroundFlame.GetGroundFlame(room).ApplyFire(p.pos, 40f);
                        throwFlameCounter = throwFlameCoolDown;
                    }
                }
            });
            //emitter.ApplyParticleModule(new FlameNaplamSpawner(emitter, this));


            ParticleSystem.ApplyEmitterAndInit(emitter);
            flameEmitter = emitter;
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
                particle.SetVel(new Vector2(10f * flameThrower.dir, 0f) + Custom.DegToVec(particle.randomParam1 * 360f) * 0.3f);
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

    public class GroundFlame : UpdatableAndDeletable, IHeatingCreature
    {
        static ConditionalWeakTable<Room, GroundFlame> flameMapper = new ConditionalWeakTable<Room, GroundFlame>();

        float[,] fireIntensities;
        float fireIntensityDecrease;
        float heat;

        int totalBurningTile;

        List<IntVector2> burningTiles = new List<IntVector2>();
        List<FLabel> labels = new List<FLabel>();
        FLabel particleMoniter;

        ParticleEmitter fireEmitter;
        ParticleEmitter sparkleEmitter;

        float stacker;
        float rate;
        float baseRate = 1f;

        public GroundFlame(Room room, float heat, int maxFireLife = 400)
        {
            this.room = room;
            this.heat = heat;
            fireIntensities = new float[room.Width,room.Height];
            fireIntensityDecrease = 1f / maxFireLife;

            fireEmitter = CreateFireEmitter(); 
            sparkleEmitter = CreateSparkleEmitter();
            particleMoniter = new FLabel(Custom.GetFont(), "")
            {
                x = 100f,
                y = 100f,
                alignment = FLabelAlignment.Left,
                anchorX = 0f,
                anchorY = 0f,
            };
         
            Futile.stage.AddChild(particleMoniter);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (slatedForDeletetion)
                return;

            totalBurningTile = 0;
            for(int x = 0;x < fireIntensities.GetLength(0); x++)
            {
                for(int y = 0;y < fireIntensities.GetLength(1); y++)
                {
                    if (fireIntensities[x, y] > 0f)
                    {
                        totalBurningTile++;
                        fireIntensities[x, y] = Mathf.Clamp01(fireIntensities[x, y] - fireIntensityDecrease);
                        if (fireIntensities[x, y] == 0)
                        {
                            int index = burningTiles.IndexOf(new IntVector2(x, y));
                            var label = labels[index];
                            label.RemoveFromContainer();
                            labels.RemoveAt(index);
                            burningTiles.RemoveAt(index);
                        }
                    }
                }
            }
            if (totalBurningTile == 0)
                Destroy();

            rate = baseRate * totalBurningTile;
            stacker += rate;
            while (stacker > 0 && totalBurningTile > 0)
            {
                stacker--;
                var pos = burningTiles[Random.Range(0, burningTiles.Count)];
                var middlePos = room.MiddleOfTile(pos);
                //if(Random.value < fireIntensities[pos.x, pos.y])
                //    room.AddObject(new HolyFire.HolyFireSprite(middlePos + Custom.DegToVec(360f * Random.value) * 10f * Random.value));
            }

            for(int i = 0;i < burningTiles.Count; i++)
            {
                labels[i].SetPosition(room.MiddleOfTile(burningTiles[i]) - room.game.cameras[0].pos);
                labels[i].text = string.Format("{0:F1}", fireIntensities[burningTiles[i].x, burningTiles[i].y]);
            }

            //particleMoniter.text = $"flame:{fireEmitter.Particles.Count}/{(fireEmitter.SpawnModule as GroundFireSpawner).maxParitcleCount}\n{totalBurningTile}";
        }

        public override void Destroy()
        {
            base.Destroy();
            sparkleEmitter?.Die();
            fireEmitter?.Die();
            particleMoniter.RemoveFromContainer();
        }

        public void ApplyFire(Vector2 pos, float rad)
        {
            int left = Mathf.Clamp(room.GetTilePosition(pos + new Vector2(-rad, 0f)).x, 0, room.Width - 1);
            int right = Mathf.Clamp(room.GetTilePosition(pos + new Vector2(rad, 0f)).x, 0, room.Width - 1);
            int up = Mathf.Clamp(room.GetTilePosition(pos + new Vector2(0f, rad)).y, 0, room.Height - 1);
            int down = Mathf.Clamp(room.GetTilePosition(pos + new Vector2(0f, -rad)).y, 0, room.Height - 1);

            for(int x = left; x <= right; x++)
            {
                for(int y = down; y <= up; y++)
                {
                    if (room.GetTile(x, y).Solid)
                        continue;

                    if(Vector2.Distance(room.MiddleOfTile(x, y), pos) < rad)
                    {
                        if (fireIntensities[x, y] == 0f)
                        {
                            burningTiles.Add(new IntVector2(x, y));
                            labels.Add(new FLabel(Custom.GetFont(), "0"));
                            Futile.stage.AddChild(labels.Last());
                        }
                        fireIntensities[x, y] = 1f;
                    }
                }
            }
        }

        public float GetHeat(Vector2 pos)
        {
            var tile = room.GetTilePosition(pos);
            if (tile.x < 0 || tile.x >= room.Width || tile.y < 0 || tile.y >= room.Height)
                return 0f;

            if (fireIntensities[tile.x, tile.y] > 0f)
                return heat/40f;
            return 0f;
        }

        public ParticleEmitter CreateSparkleEmitter()
        {
            var emitter = new ParticleEmitter(room);
            emitter.ApplyParticleSpawn(new GroundFireSpawner(emitter, this, 1, 200));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "")));
            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 40, 60));
            emitter.ApplyParticleModule(new DispatchRandomFirePos(emitter, this));
            emitter.ApplyParticleModule(new SetConstVelociy(emitter, Vector2.up * 2f));
            emitter.ApplyParticleModule(new ColorOverLife(emitter, (p, l) =>
            {
                if (l < 0.5f)
                    return Color.Lerp(Color.white, Color.yellow, l * 2f);
                else
                    return Color.Lerp(Color.yellow, Color.red, (l - 0.5f) * 2f);
            }));

            emitter.ApplyParticleModule(new AlphaOverLife(emitter, (p, l) =>
            {
                if (l < 0.2f)
                    return l * 5f;
                else if (l > 0.5f)
                    return (1f - l) * 2f;
                else
                    return 1f;
            }));

            emitter.ApplyParticleModule(new VelocityOverLife(emitter, (p, l) =>
            {
                Vector2 vel = p.vel;
                vel += Custom.RNV() * 0.2f;
                return vel;
            }));

            emitter.ApplyParticleModule(new TrailDrawer(emitter, 0, 5)
            {
                gradient = (p, i, max) => p.color,
                alpha = (p, i, max) => p.alpha,
                width = (p, i, max) => 1f
            });
            ParticleSystem.ApplyEmitterAndInit(emitter);

            return emitter;
        }

        public ParticleEmitter CreateFireEmitter()
        {
            var emitter = new ParticleEmitter(room);
            emitter.ApplyParticleSpawn(new GroundFireSpawner(emitter, this, 4, 800));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam(FlameThrowerBuffEntry.flameVFX1, "StormIsApproaching.AdditiveDefault")));

            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 40, 80));
            emitter.ApplyParticleModule(new DispatchRandomFirePos(emitter, this));
            emitter.ApplyParticleModule(new SetConstVelociy(emitter, Vector2.up * 1f));
            emitter.ApplyParticleModule(new ColorOverLife(emitter, (p, l) =>
            {
                if (l < 0.5f)
                    return Color.Lerp(Color.white, Color.yellow, l * 2f);
                else
                    return Color.Lerp(Color.yellow, Color.red, (l - 0.5f) * 2f);
            }));


            emitter.ApplyParticleModule(new ScaleOverLife(emitter, (p, l) =>
            {
                return 4f;
            }));
            emitter.ApplyParticleModule(new DefaultDrawer(emitter, new int[1] { 0 }));
            ParticleSystem.ApplyEmitterAndInit(emitter);

            return emitter;
            //fireEmitter.ApplyParticleModule(new VelocityOverLife(fireEmitter, (p, l) =>
            //{
            //    return p.vel + Custom.RNV() * 0.1f;
            //}));

            //fireEmitter.ApplyParticleModule(new ColorOverLife(fireEmitter, (p, l) =>
            //{
            //    if (l < 0.1f)
            //        return Color.Lerp(flameCol_0, flameCol_1, l * 10f) * 0.35f;
            //    else if (l < 0.25f)
            //        return Color.Lerp(flameCol_1, flameCol_2, (l - 0.1f) / 0.15f) * 0.35f;
            //    else if (l < 0.4f)
            //        return Color.Lerp(flameCol_2, flameCol_3, (l - 0.25f) / 0.15f) * 0.3f;
            //    else if (l < 0.8f)
            //        return Color.Lerp(flameCol_3, flameCol_4, (l - 0.4f) / 0.4f) * 0.3f;
            //    else
            //        return Color.Lerp(flameCol_4, flameCol_5, (l - 0.8f) / 0.2f) * 0.3f;
            //}));

        }

        class GroundFireSpawner : SpawnModule
        {
            GroundFlame groundFlame;
            float stacker;
            float rate;
            float baseRate;

            public GroundFireSpawner(ParticleEmitter emitter, GroundFlame groundFlame, float baseRatePerSec, int maxParticleCount) : base(emitter, maxParticleCount)
            {
                this.groundFlame = groundFlame;
                baseRate = baseRatePerSec / 40f;
            }

            public override void Update()
            {
                base.Update();
                rate = baseRate * groundFlame.totalBurningTile;
                stacker += rate;
                while(stacker > 0)
                {
                    stacker--;
                    if (emitter.Particles.Count < maxParitcleCount)
                    {
                        emitter.SpawnParticle();
                    }
                }
            }
        }

        class DispatchRandomFirePos : EmitterModule, IParticleInitModule
        {
            GroundFlame groundFlame;
            public DispatchRandomFirePos(ParticleEmitter emitter, GroundFlame groundFlame) : base(emitter)
            {
                this.groundFlame = groundFlame;
            }

            public void ApplyInit(Particle particle)
            {
                var pos = groundFlame.burningTiles[(int)(particle.randomParam1 * (groundFlame.burningTiles.Count - 1))];
                var middlePos = groundFlame.room.MiddleOfTile(pos);
                particle.HardSetPos(middlePos + Custom.DegToVec(360f * particle.randomParam2) * 10f * particle.randomParam3);
            }
        }
   
        public static GroundFlame GetGroundFlame(Room room, float heat = 4f, int maxFireLife = 400)
        {
            if(flameMapper.TryGetValue(room, out var groundFlame) && !groundFlame.slatedForDeletetion)
            {
                return groundFlame;
            }
            else if(groundFlame != null)
                flameMapper.Remove(room);

            groundFlame = new GroundFlame(room, heat, maxFireLife);
            room.AddObject(groundFlame);
            flameMapper.Add(room, groundFlame);
            return groundFlame;
        }

        public float GetHeat(UpdatableAndDeletable updatableAndDeletable, Vector2 pos)
        {
            //TODO :接口
            return 0;
            //throw new NotImplementedException();
        }
    }
}
