using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Negative.SephirahMeltdown.Conditions;
using BuiltinBuffs.Positive;
using HUD;
using MoreSlugcats;
using Newtonsoft.Json;
using On.Menu.Remix;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;
using RandomBuffUtils;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RWCustom;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Negative.SephirahMeltdown
{
    internal class BinahBuffData : BuffData
    {
        public static readonly BuffID Binah = new BuffID(nameof(Binah), true);
        public override BuffID ID => Binah;

        [JsonProperty] 
        public float Health { get; set; } = 1;

        public const float MaxHealth = 30;

        [JsonProperty]
        public int CycleUse;

        public override void CycleEnd()
        {
            base.CycleEnd();
            CycleUse++;
        }
    }

    internal class BinahBuff : Buff<BinahBuff,BinahBuffData>, BuffHudPart.IOwnBuffHudPart
    {
        public override BuffID ID => BinahBuffData.Binah;
        public bool needSpawnChain = true;
        private MusicEvent turnOne;
        private MusicEvent turnTwo;

        public const int MaxTime = 200;

        public BinahBuff()
        {
            MyTimer = new DownCountBuffTimer(((timer, worldGame) =>
            {

                needSpawnChain = true;
            }), MaxTime);

            BinahGlobalManager.Init();
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var room in game.world.activeRooms)
                    room.AddObject(new BinahRoom(room, room.IsNeedBinahChain()));
            }

            foreach (var self in (BuffCustom.TryGetGame(out game) ? game.Players : new List<AbstractCreature>())
                .Select(i => i.realizedCreature as Player).Where(i => !(i is null)))
            {
                self.slugcatStats.corridorClimbSpeedFac *= Fac;
                self.slugcatStats.poleClimbSpeedFac *= Fac;
                self.slugcatStats.runspeedFac *= Fac;
                if(self.room != null)
                    self.room.AddObject(new BinahGuardBlackSmoke(self.room,self));
            }

            turnOne = new MusicEvent
            {
                fadeInTime = 1f,
                roomsRange = -1,
                cyclesRest = 0,
                prio = 9.9f,
                volume = 0.13f,
                stopAtDeath = false,
                stopAtGate = false,
                loop = true,
                oneSongPerCycle = false,
                maxThreatLevel = 10,
                songName = $"BUFF_{BinahBuffData.Binah.GetStaticData().AssetPath}/SephirahMissionSong",

            };
            game.rainWorld.processManager.musicPlayer.GameRequestsSong(turnOne);
        }


        private const float Fac = 0.85f;

        public override void Update(RainWorldGame game)
        {
            base.Update(game);
            BinahGlobalManager.GlobalUpdate(game);
            if (needSpawnChain)
                needSpawnChain = !BinahGlobalManager.SpawnNewChain(game);

            if (Data.Health < 0.75f && turnTwo == null)
            {
                turnTwo = new MusicEvent
                {
                    fadeInTime = 1f,
                    roomsRange = -1,
                    cyclesRest = 0,
                    volume = 0.2f,
                    prio = 12f,
                    stopAtDeath = true,
                    stopAtGate = false,
                    loop = true,
                    oneSongPerCycle = false,
                    maxThreatLevel = 10,
                    songName = $"BUFF_{BinahBuffData.Binah.GetStaticData().AssetPath}/Binah-Garion",

                };
                game.rainWorld.processManager.musicPlayer.GameRequestsSong(turnTwo);

            }

        }

        public override void Destroy()
        {
            base.Destroy();
            MyTimer.Reset();
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var self in game.Players
                    .Select(i => i.realizedCreature as Player).Where(i => !(i is null)))
                {
                    self.slugcatStats.corridorClimbSpeedFac /= Fac;
                    self.slugcatStats.poleClimbSpeedFac /= Fac;
                    self.slugcatStats.runspeedFac /= Fac;
                }

                if (BuffPoolManager.Instance.GameSetting.MissionId == SephirahMeltdownsMission.SephirahMeltdowns.value)
                {
                    turnOne = new MusicEvent
                    {
                        fadeInTime = 1f,
                        roomsRange = -1,
                        cyclesRest = 0,
                        prio = 30f,
                        volume = 0.2f,
                        stopAtDeath = true,
                        stopAtGate = false,
                        loop = true,
                        oneSongPerCycle = false,
                        songName = $"BUFF_{BinahBuffData.Binah.GetStaticData().AssetPath}/SephirahMissionSong",

                    };

                    //TODO:存在BUG
                    game.GetStorySession.saveState.deathPersistentSaveData.songsPlayRecords.RemoveAll(i =>
                        i.songName == $"BUFF_{BinahBuffData.Binah.GetStaticData().AssetPath}/SephirahMissionSong");
                    game.rainWorld.processManager.musicPlayer.GameRequestsSong(turnOne);
                }
                else
                {
                    game.rainWorld.processManager.musicPlayer.GameRequestsSongStop(new StopMusicEvent()
                    {
                        fadeOutTime = 1,
                        prio = 20,
                        songName = $"BUFF_{BinahBuffData.Binah.GetStaticData().AssetPath}/Binah-Garion",
                        type = StopMusicEvent.Type.AllSongs
                    });
                }
            }



            BinahGlobalManager.DestroyNoDie();
        }


        public BuffHudPart CreateHUDPart()
        {
            return new BinahPart();
        }
    }

    internal class HealthBar
    {
        public FContainer Container { get; private set; } = new FContainer();

        private readonly FSprite[] sprites;
        public HealthBar(string nameTex = "Binah.BarFront",float anchorY = 1)
        {
            sprites = new FSprite[4];
            sprites[0] = new FSprite("Binah.BarBack") {anchorY = anchorY };
            sprites[1] = new FSprite("Binah.Bar")
            { shader = Custom.rainWorld.Shaders["SephirahMeltdownEntry.Bar"], anchorY = anchorY };
            sprites[2] = new FSprite("Binah.BarMid") { anchorY = anchorY };
            sprites[3] = new FSprite(nameTex) { anchorY = anchorY };
            foreach (var sprite in sprites)
                Container.AddChild(sprite);
        }

        public float Health
        {
            get => Mathf.InverseLerp(0.17f, 0.72f, sprites[1].color.r);
            set => sprites[1].color = new Color(Mathf.Lerp(0.17f, 0.72f, value), 1, 1);
        }

        public void CleanSprites()
        {
            foreach (var sprite in sprites)
            {
                sprite.RemoveFromContainer();
            }
            Container.RemoveFromContainer();
        }
    }

    internal class BinahPart : BuffHudPart
    {
        private HealthBar bar;
        public override void InitSprites(HUD.HUD hud)
        {
            base.InitSprites(hud);
            if (bar != null)
                bar.CleanSprites();
            bar = new HealthBar();
            hud.fContainers[1].AddChild(bar.Container);
            bar.Container.SetPosition(Custom.rainWorld.screenSize.x / 2, Custom.rainWorld.screenSize.y - 30);
            bar.Container.scale = 0.33f;
        }

        private float alpha = 0, lastAlpha = 0;

        private Vector2 RoomPos(HUD.HUD hud, int roomIndex, Vector2? pos = null)
        {
            pos = pos ?? Vector2.zero;
            return hud.map.mapData.PositionOfRoom(roomIndex) / 3f + new Vector2(10f, 10f) + (pos.Value - new Vector2(hud.map.mapData.SizeOfRoom(roomIndex).x * 20f, 
                hud.map.mapData.SizeOfRoom(roomIndex).y * 20f) / 2f)/20f;
        }

        public override void Draw(HUD.HUD hud, float timeStacker)
        {
            base.Draw(hud, timeStacker);
            bar.Health = BinahBuff.Instance.Data.Health;
            bar.Container.alpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
            if (hud.owner is Player player && hud.map != null)
            {
                var rRoom = player.abstractCreature.world.game.cameras[0].room;
                var room = rRoom.abstractRoom.index;
                var roomPos = RoomPos(hud,room, player.abstractCreature.world.game.cameras[0].followAbstractCreature?.realizedCreature?.DangerPos ??
                                                rRoom.MiddleOfTile(player.abstractCreature.world.game.cameras[0].followAbstractCreature?.pos.Tile ??
                                                                   new IntVector2(rRoom.Width, rRoom.Height)));
                HashSet<int> useRoom = new HashSet<int>(){ room };
                var border = new Vector2(30, 30);
                var screenSize = Custom.rainWorld.screenSize / 2 - border;

                 while(arrows.Count < BinahGlobalManager.ChainRoomIndices.Count)
                    arrows.Add(new ChainArrow());
                for (int i =0;i< BinahGlobalManager.ChainRoomIndices.Count;i++)
                {
                    var curRoom = BinahGlobalManager.ChainRoomIndices[i];
                    if (useRoom.Contains(curRoom))
                    {
                        if (arrows[i].Container.container != null)
                            arrows[i].Container.RemoveFromContainer();
                        continue;
                    }

                    useRoom.Add(curRoom);

                    var dir = Custom.DirVec(roomPos , RoomPos(hud,curRoom));
                    Vector2 pos;
                    if (Mathf.Abs(dir.y / dir.x) > screenSize.y / screenSize.x)
                        pos = new Vector2(screenSize.y / Mathf.Abs(dir.y) * dir.x, Mathf.Sign(dir.y) * screenSize.y);
                    else
                        pos = new Vector2(Mathf.Sign(dir.x) * screenSize.x, screenSize.x / Mathf.Abs(dir.x) * dir.y);
                    pos += screenSize + border;
         

                    arrows[i].Container.SetPosition(pos);
                    arrows[i].Container.rotation = Custom.VecToDeg(dir);
                    arrows[i].SetCreature(BinahGlobalManager.ChainType[curRoom]);
                    if (arrows[i].Container.container == null)
                        hud.fContainers[1].AddChild(arrows[i].Container);

                }

                foreach (var arrow in arrows)
                    arrow.Container.alpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);

                for (int i = BinahGlobalManager.ChainRoomIndices.Count; i < arrows.Count; i++)
                {
                    if (arrows[i].Container.container != null)
                        arrows[i].Container.RemoveFromContainer();
                }
            }
        }

        private FLabel[] debugLabels;

        public override void Update(HUD.HUD hud)
        {
            base.Update(hud);
            lastAlpha = alpha;
            alpha = Mathf.Lerp(alpha,BinahGlobalManager.needDelete ? 0 : 1, 0.1f);
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            foreach (var arrow in arrows)
            {
                arrow.Container.RemoveAllChildren();
                arrow.Container.RemoveFromContainer();
            }
            arrows.Clear();
            bar.CleanSprites();
        }

        readonly List<ChainArrow> arrows = new List<ChainArrow>();

        internal class ChainArrow
        {
            public FContainer Container { get; private set; } = new FContainer();

            private FSprite[] sprites;

            public ChainArrow()
            {
                sprites = new FSprite[3];
                sprites[0] = new FSprite("Multiplayer_Arrow") {scale = 2,anchorY = 1, rotation = 180, color = RainWorld.GoldRGB*1.3f};
                sprites[1] = new FSprite("Futile_White")
                    { scale = 8, anchorY = 1, y = 30, shader = Custom.rainWorld.Shaders["FlatLight"], alpha = 0.35f };
                sprites[2] = new FSprite("Futile_White") { scale = 1.2f, y = -35f};
                Container.AddChild(sprites[0]);
                Container.AddChild(sprites[1]);
                Container.AddChild(sprites[2]);


            }

            public void SetCreature(CreatureTemplate.Type type)
            {
                var iconData = new IconSymbol.IconSymbolData(type, AbstractPhysicalObject.AbstractObjectType.Creature, 0);
                sprites[2].rotation = -Container.rotation;
                sprites[2].element = Futile.atlasManager.GetElementWithName(CreatureSymbol.SpriteNameOfCreature(iconData));
                sprites[2].color = sprites[1].color = CreatureSymbol.ColorOfCreature(iconData);
            }

        }

    }

    internal class BinahHook
    {
  

        public static void HookOn()
        {
            On.Room.Loaded += Room_Loaded;
            On.TempleGuard.ctor += TempleGuard_ctor;
            On.TempleGuardGraphics.AddToContainer += TempleGuardGraphics_AddToContainer;
            On.TempleGuardAI.ThrowOutScore += TempleGuardAI_ThrowOutScore;
            On.TempleGuardAI.Update += TempleGuardAI_Update;
            On.TempleGuard.Die += TempleGuard_Die;
            On.TempleGuard.Update += TempleGuard_Update;

            On.Creature.Violence += Creature_Violence;

            On.HUD.Map.Draw += Map_Draw;
            On.HUD.Map.ClearSprites += Map_ClearSprites;

            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;

            On.Player.NewRoom += Player_NewRoom;

            On.RegionGate.customKarmaGateRequirements += RegionGate_customKarmaGateRequirements;
            On.RegionGate.Update += RegionGate_Update;
            currentDarkness = 0;
        }

        private static void TempleGuard_Update(On.TempleGuard.orig_Update orig, TempleGuard self, bool eu)
        {
            if (Modules.TryGetValue(self, out var module))
            {
                self.stun = 0;
                self.blind = 0;
            }
            orig(self, eu);
        }

        private static void RegionGate_Update(On.RegionGate.orig_Update orig, RegionGate self, bool eu)
        {
            orig(self, eu);
            self.mode = RegionGate.Mode.Broken;
        }

        private static void RegionGate_customKarmaGateRequirements(On.RegionGate.orig_customKarmaGateRequirements orig, RegionGate self)
        {
            orig(self);
            self.karmaRequirements[0] = MoreSlugcatsEnums.GateRequirement.OELock;
            self.karmaRequirements[1] = MoreSlugcatsEnums.GateRequirement.OELock;
        }

        private static float currentDarkness;

        private static void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            orig(self, timeStacker, timeSpeed);
            currentDarkness = Mathf.Lerp(currentDarkness, BinahGlobalManager.needDelete ? 0 : BinahBuff.Instance.Data.Health > 0.25F ? 0.325f : 0.5F, 0.02f);
            Shader.SetGlobalFloat(RainWorld.ShadPropDarkness,
                Mathf.Lerp(Shader.GetGlobalFloat(RainWorld.ShadPropDarkness), 0, currentDarkness));
            if (BinahGlobalManager.needDelete && currentDarkness < 0.03f)
                BinahGlobalManager.Destroy();
        }

        private static void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            orig(self, newRoom);
            newRoom.AddObject(new BinahGuardBlackSmoke(newRoom,self));

        }

        private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (self is TempleGuard guard && guard.abstractCreature.ID.spawner == BinahBuffData.Binah.valueHash)
            {
                if (type == Creature.DamageType.Water || type == Creature.DamageType.Explosion)
                    return;
                BinahGlobalManager.HitBinah(damage);
                return;
            }
            orig(self,source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        private static void Map_ClearSprites(On.HUD.Map.orig_ClearSprites orig, Map self)
        {
            orig(self);
            BinahGlobalManager.ClearSprites();
        }

        private static void Map_Draw(On.HUD.Map.orig_Draw orig, HUD.Map self, float timeStacker)
        {
            orig(self, timeStacker);
            BinahGlobalManager.Draw(self,timeStacker);
        }

        private static void TempleGuard_Die(On.TempleGuard.orig_Die orig, TempleGuard self)
        {
            if (self.abstractCreature.ID.spawner == BinahBuffData.Binah.valueHash && !BinahGlobalManager.IsDead)
                return;
            orig(self);
        }
        private static void TempleGuardAI_Update(On.TempleGuardAI.orig_Update orig, TempleGuardAI self)
        {
            orig(self);
            if(Modules.TryGetValue(self.guard,out var module))
                module.AIUpdate(self);
        }
        private static float TempleGuardAI_ThrowOutScore(On.TempleGuardAI.orig_ThrowOutScore orig, TempleGuardAI self, Tracker.CreatureRepresentation crit)
        {
            if (self.creature.ID.spawner == BinahBuffData.Binah.valueHash)
                return 0;
            return orig(self,crit);
        }

        private static void TempleGuardGraphics_AddToContainer(On.TempleGuardGraphics.orig_AddToContainer orig, TempleGuardGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if(Modules.TryGetValue(self.guard,out var module))
                module.AddToContainer(self,sLeaser,rCam,newContatiner);
            else
                orig(self, sLeaser, rCam, newContatiner);
            
        }

        private static void TempleGuard_ctor(On.TempleGuard.orig_ctor orig, TempleGuard self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if(self.abstractCreature.ID.spawner == BinahBuffData.Binah.valueHash)
                Modules.Add(self,new BinahGuardModule(self));
        }


        private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            orig(self);
            self.AddObject(new BinahRoom(self,self.IsNeedBinahChain()));
        }

        public static readonly ConditionalWeakTable<TempleGuard, BinahGuardModule> Modules =
            new ConditionalWeakTable<TempleGuard, BinahGuardModule>();
    }


    internal class BinahGuardBlackSmoke : UpdatableAndDeletable
    {
        private readonly Player player;
        private readonly ParticleEmitter emitter;

        public BinahGuardBlackSmoke(Room room, Player player)
        {
            this.room = room;
            this.player = player;

            emitter = new ParticleEmitter(room);
            float texSize = Futile.atlasManager.GetElementWithName("Binah.Smoke2").sourceSize.x;
            emitter.ApplyParticleModule(new BindEmitterToPhysicalObject(emitter, player, 1, false));
            emitter.ApplyParticleSpawn(new RateSpawnerModule(emitter, 100, 5));
            emitter.ApplyParticleModule(new SetOriginalAlpha(emitter, 0));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Binah.Smoke2",
                $"{FakeCreatureBuffData.FakeCreatureID}.AlphaBehindTerrain")));
            emitter.ApplyParticleModule(new RectPositionModule(emitter,new Rect(-5,-5,5,0),0));
            emitter.ApplyParticleModule(new SetRandomScale(emitter,70/texSize,140/texSize));
            emitter.ApplyParticleModule(new SetRandomLife(emitter,3*40,5*40));
            emitter.ApplyParticleModule(new AlphaOverLife(emitter,((particle, time) => Mathf.InverseLerp(0,0.03f,time) * Mathf.InverseLerp(1f,0.5f,time) * 0.35f)));
            emitter.ApplyParticleModule(new SetConstColor(emitter,Color.black));
            emitter.ApplyParticleModule(new SetRandomRotation(emitter,0,360));

            ParticleSystem.ApplyEmitterAndInit(emitter);
        }

        public override void Update(bool eu)
        {
            if (slatedForDeletetion)
                return;
            if (room.abstractRoom != player.abstractCreature.Room || BinahGlobalManager.needDelete)
            {
                Destroy();
                return;
            }

            emitter.SpawnModule.maxParitcleCount = player.room == null ? 0 : 100;
            base.Update(eu);
        }

        public override void Destroy()
        {
            base.Destroy();
            emitter.Die();
        }
    }
    internal class BinahGuardRed : HudPart
    {
        private int counter = 0;
        public BinahGuardRed(HUD.HUD hud) : base(hud)
        {
            sprite = new FSprite("Futile_White")
            {
                alpha = 0, color = Color.red, anchorX = 0, anchorY = 0, width = Custom.rainWorld.screenSize.x,
                height = Custom.rainWorld.screenSize.y
            };
            hud.fContainers[1].AddChild(sprite);
        }

        public override void Update()
        {
            base.Update();
            counter++;
            if(counter == 80)
                ClearSprites();
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            sprite.alpha = Mathf.Pow(Mathf.InverseLerp(0, 30, counter + timeStacker), 0.5f) *
                           Mathf.Pow(Mathf.InverseLerp(80, 50, counter + timeStacker), 0.5f) *0.2f;
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            sprite.RemoveFromContainer();
            hud.parts.Remove(this);
        }

        private FSprite sprite;
    }

    internal class BinahGuardModule
    {
        private WeakReference<TempleGuard> critRef;

        public BinahGuardModule(TempleGuard crit)
        {
            critRef = new WeakReference<TempleGuard>(crit);
            crit.CollideWithTerrain = false;
            crit.CollideWithObjects = false;

            BinahGlobalManager.requestFairyAttack += BinahGlobalManager_OnRequestFairyAttack;
        }

        private void BinahGlobalManager_OnRequestFairyAttack()
        {
            if (!critRef.TryGetTarget(out var guard) || guard.room.PlayersInRoom.Count == 0)
                return;
            
            var forceDir = Vector2.zero;
            var centerPos = Vector2.Lerp(guard.bodyChunks[1].pos, guard.bodyChunks[2].pos, 0.5f);
            foreach (var p in guard.room.PlayersInRoom.Where(i => i != null && !i.dead))
            {
                guard.room.AddObject(new BinahFairy(guard.room, centerPos,
                    Custom.DirVec(centerPos, p.DangerPos)));
                forceDir += Custom.DirVec(p.DangerPos, centerPos);
            }

            foreach (var chunk in guard.bodyChunks)
                chunk.vel += forceDir.normalized * 5F;
        }

        private bool IsAttacking => AttackType != BinahAttackType.None;
        private int attackCounter = 0;

        private BinahAttackType AttackType
        {
            get => _attackType;
            set 
            {
                if(value == _attackType) return;
                if (critRef.TryGetTarget(out var target))
                {
                    if (value == BinahAttackType.None)
                        EndAttack(target);
                    else
                    {
                        BuffUtils.Log(BinahBuffData.Binah, $"Start Attack, Type:{value}, Room:{target.room.abstractRoom.name}");
                        PrepareAttack(target);
                    }

                    _attackType = value;
                }
            }
        }

        private BinahAttackType _attackType;

        private Player focusedPlayer;

        public void OnDestroy()
        {
            if(AttackType != BinahAttackType.None)
                AttackType = BinahAttackType.None;
        }

        public void AIUpdate(TempleGuardAI ai)
        {
            if(IsAttacking)
                ai.guard.telekinesis = Mathf.Min(ai.guard.telekinesis+0.1f,1f);
            if (!IsAttacking)
            {
                if (ai.guard.room.PlayersInRoom.Any(i => !i.dead))
                {
                    foreach (var type in BinahGlobalManager.MaxCd.Keys)
                    {
                        if (BinahGlobalManager.TryUseAttack(type))
                        {
                            AttackType = type;
                            focusedPlayer = ai.guard.room.PlayersInRoom.First(i => !i.dead);
                            switch (AttackType)
                            {
                                case BinahAttackType.Key:
                                    ai.guard.room.AddObject(new BinahKey(ai.guard.room,Vector2.Lerp(ai.guard.bodyChunks[1].pos, ai.guard.bodyChunks[2].pos, 0.5f), 
                                        focusedPlayer));
                                    break;
                                case BinahAttackType.Strike:
                                    foreach(var player in ai.guard.room.PlayersInRoom)
                                        ai.guard.room.AddObject(new BinahStrike(ai.guard.room, player.DangerPos));
                                    AttackType = BinahAttackType.None;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        }

                    }
                }
            }
            else
            {
                attackCounter++;
                switch (AttackType)
                {
                    case BinahAttackType.Key:

                        ai.telekinGetToPoint = focusedPlayer.DangerPos;
                        ai.telekinGetToDir = (Custom.DirVec(Vector2.Lerp(ai.guard.bodyChunks[1].pos, ai.guard.bodyChunks[2].pos, 0.5f), focusedPlayer.DangerPos) +
                                              (Mathf.InverseLerp(0, BinahKey.ReadyCount * 0.85f, attackCounter) *
                                               Mathf.InverseLerp(BinahKey.ReadyCount * 2f, BinahKey.ReadyCount * 1.25f,
                                                   attackCounter)) * Vector2.up * 3).normalized;
                        if (attackCounter == BinahKey.ReadyCount)
                        {
                            foreach (var chunk in ai.guard.bodyChunks)
                                chunk.vel += Custom.DirVec(focusedPlayer.DangerPos,
                                    Vector2.Lerp(ai.guard.bodyChunks[1].pos, ai.guard.bodyChunks[2].pos, 0.5f)) * 5F;

                        }
                        if (attackCounter > BinahKey.ReadyCount * 2f)
                            AttackType = BinahAttackType.None;
                        break;
                    case BinahAttackType.Fairy:
                        ai.telekinGetToPoint = focusedPlayer.DangerPos;
                        ai.telekinGetToDir =
                            Custom.DirVec(Vector2.Lerp(ai.guard.bodyChunks[1].pos, ai.guard.bodyChunks[2].pos, 0.5f),
                                focusedPlayer.DangerPos);

                        if (attackCounter / 80 == 3)
                        {
                            BinahGlobalManager.requestFairyAttack?.Invoke();
                            AttackType = BinahAttackType.None;
                        }
                        else if (attackCounter % 80 == 1)
                        {
                            BuffPostEffectManager.AddEffect(new Positive.SingleColorEffect(4, 1.7f, 1.25f, 0.75f,
                                Custom.hexToColor("080203"), Custom.hexToColor("FF496A"), 0.7f));
                            //ai.guard.room.game.cameras[0].hud.AddPart(new BinahGuardRed(ai.guard.room.game.cameras[0].hud));
                        }
                        break;
                    default:
                        AttackType = BinahAttackType.None;
                        break;
                }
            }

        }

        private void EndAttack(TempleGuard crit)
        {
            BuffUtils.Log(BinahBuffData.Binah,$"End Attack, Type:{AttackType}, Room:{crit.room.abstractRoom.name}");
            //crit.room.lockedShortcuts.Clear();
            BinahGlobalManager.EndAttack(AttackType);
        }

        private void PrepareAttack(TempleGuard crit)
        {
            attackCounter = 0;
            //crit.room.lockedShortcuts.AddRange(crit.room.shortcutsIndex);
        }

        public void AddToContainer(TempleGuardGraphics graphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            sLeaser.RemoveAllSpritesFromContainer();
            if (newContatiner == null)
                newContatiner = rCam.ReturnFContainer("Foreground");
            
            for (int i = 0; i < graphics.FirstHaloSprite; i++)
                newContatiner.AddChild(sLeaser.sprites[i]);
            
            for (int j = graphics.FirstHaloSprite; j < graphics.TotalSprites; j++)
                rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[j]);
            
            if (sLeaser.containers != null)
                foreach (FContainer node in sLeaser.containers)
                    newContatiner.AddChild(node);
                
            
        }
    }

    //地图UI
    internal static partial class BinahGlobalManager
    {
        public static event Action OnBinahDie;
        public static event Action OnBinahNewChain;


        public static Action requestFairyAttack;

        public static bool NeedFinalAttack
        {
            get => needFinalAttack && ChainRoomIndices.Count > 0 && !needDelete;
            set => needFinalAttack = value;
        }

        private static bool needFinalAttack;

        private static readonly List<FSprite> ChainSprites = new List<FSprite>();

        public static readonly CreatureTemplate.Type[] SpawnTypes = new[]
        {
            CreatureTemplate.Type.RedCentipede,
            CreatureTemplate.Type.RedLizard,
            CreatureTemplate.Type.CyanLizard,
        };

        public static void Draw(Map map, float timeStacker)
        {
            while (ChainSprites.Count < ChainRoomIndices.Count)
            {
                ChainSprites.Add(new FSprite("Futile_White"));
                map.container.AddChild(ChainSprites[ChainSprites.Count - 1]);
            }

            for (int i = 0; i < ChainRoomIndices.Count; i++)
            {

                ChainSprites[i].isVisible = true;
                ChainSprites[i].MoveToFront();
                ChainSprites[i].alpha =
                    map.Alpha(map.mapData.LayerOfRoom(ChainRoomIndices[i]), timeStacker, false);
                ChainSprites[i].SetPosition(map.RoomToMapPos(
                    map.mapData.SizeOfRoom(ChainRoomIndices[i]).ToVector2() * 10, ChainRoomIndices[i], timeStacker));
            }

            for (int i = ChainRoomIndices.Count; i < ChainSprites.Count; i++)
                ChainSprites[i].isVisible = false;
        }

        public static void ClearSprites()
        {
            foreach (var sprite in ChainSprites)
            {
                sprite.RemoveFromContainer();
            }
            ChainSprites.Clear();
        }
    }

    //锁链相关
    internal static partial class BinahGlobalManager
    {
        public static bool needDelete = false;

        public static readonly List<int> ChainRoomIndices = new List<int>();

        public static readonly Dictionary<int, CreatureTemplate.Type> ChainType = new Dictionary<int, CreatureTemplate.Type>();

        private const int SearchDepth = 3;

        private static int maxCount = 1;

        public static bool IsNeedBinahChain(this Room room)
        {
            return ChainRoomIndices.Contains(room.abstractRoom.index);
        }


        public static bool SpawnNewChain(RainWorldGame game)
        {
            NeedFinalAttack = BinahBuff.Instance.Data.Health < 0.25f;
            if (BinahBuff.Instance.Data.Health < 0.25f)
                game.cameras[0].virtualMicrophone.PlaySound(SephirahMeltdownEntry.BinahAtkFinalStart, 1, 0.2f, 1);
            
            localDamage = 0;
            if (game.AlivePlayers.Count == 0 || game.AlivePlayers[0].Room?.connections == null) return false;
            ChainRoomIndices.Clear();
            BuffPostEffectManager.AddEffect(new ShockEffect(0,1.3f,0.5f,0.7f,0,10,0.2f,Custom.RNV()*1.3F));

            List<int> allRooms = new List<int>();
            List<AbstractRoom> toUseRooms = new List<AbstractRoom> { game.AlivePlayers[0].Room };
            for (int currentDepth = 0; currentDepth < SearchDepth; currentDepth++)
            {
                var useRoom = toUseRooms.ToList();
                toUseRooms.Clear();
                foreach (var room in useRoom)
                {
                    if(room?.connections == null) continue;
                    foreach (var node in room.connections.Where(i => !allRooms.Contains(i)))
                    {
                        if (game.world.GetAbstractRoom(node) != null && !game.world.GetAbstractRoom(node).shelter && !game.world.GetAbstractRoom(node).gate)
                        {
                            allRooms.Add(node);
                            toUseRooms.Add(game.world.GetAbstractRoom(node));
                        }
                    }
                }
            }

            allRooms.Remove(game.AlivePlayers[0].pos.room);
            if (allRooms.Count == 0)
                allRooms.Add(game.AlivePlayers[0].pos.room);

            int spawnCount = Mathf.RoundToInt(maxCount = Random.Range(3, 5));
            bool allowMulti = spawnCount > toUseRooms.Count;
            for (int i = 0; i < spawnCount; i++)
            {
                int r = Random.Range(0, allRooms.Count);
                ChainRoomIndices.Add(allRooms[r]);
                if (!allowMulti)
                    allRooms.RemoveAt(r);
            }
            CheckChainCreateOrDelete();
            OnBinahNewChain?.Invoke();
            BuffUtils.Log(BinahBuffData.Binah, $"new chains count:{maxCount}");
            return true;
        }

        public static void CheckChainCreateOrDelete()
        {
            if (DisplayChains.Count > 0)
            {
                foreach (var chain in DisplayChains)
                    chain.TimeOutBreak();
                DisplayChains.Clear();
                DisplayChainPos.Clear();
                if (BuffCustom.TryGetGame(out var game))
                {
                    foreach (var player in game.Players.Select(i => i.realizedCreature))
                    {
                        if(player == null || player.dead || player.abstractCreature.Room?.realizedRoom == null)
                            continue;
                        var room = player.abstractCreature.Room?.realizedRoom;
                        for (int i = 0; i < 100; i++)
                        {
                            var tile = room.RandomPos();
                            if(room.GetTile(tile).Solid)
                                continue;
                            AbstractCreature abCrit = new AbstractCreature(room.world,
                                StaticWorld.GetCreatureTemplate(SpawnTypes[Random.Range(0, SpawnTypes.Length)]), null,
                                new WorldCoordinate(room.abstractRoom.index, room.GetTilePosition(tile).x,
                                    room.GetTilePosition(tile).y, -1), room.game.GetNewID());
                            abCrit.RealizeInRoom();
                            abCrit.voidCreature = true;
                            foreach (var chunk in abCrit.realizedCreature.bodyChunks)
                                chunk.HardSetPosition(tile);

                            room.AddObject(new BinahShowEffect(room, tile, 200, RainWorld.SaturatedGold));
                            break;
                        }
                    }
                }
            }
            ChainType.Clear();

            foreach (var index in ChainRoomIndices)
                if(!ChainType.ContainsKey(index))
                    ChainType.Add(index,SpawnTypes[Random.Range(0,SpawnTypes.Length)]);

        }
        public static readonly List<BinahRoom> DisplayChains = new List<BinahRoom>();

        public static readonly Dictionary<int, Vector2> DisplayPos =
            new Dictionary<int, Vector2>();


        public static readonly Dictionary<int, List<Vector2>> DisplayChainPos =
            new Dictionary<int, List<Vector2>>();
    }


    enum BinahAttackType
    {
        None,
        Key,
        Fairy,
        Strike
    }

    //AI及血量相关
    internal static partial class BinahGlobalManager
    {
        class RefInt
        {
            private RefInt(int val) => value = val;

            public int value;


            public static implicit operator int(RefInt b)
            {
                return b.value;
            }

            public static implicit operator RefInt(int b)
            {
                return new RefInt(b);
            }
        }

        public static float Resistance => NeedFinalAttack
            ? 0
            : (ChainRoomIndices.Count == 0 ? 1.2f : Custom.LerpMap(ChainRoomIndices.Count, maxCount, 1, 0.1f, 0.75f));


        public static bool IsDead => false;


        public static void Init()
        {
            needDelete = false;
            postEffect = null;
            ChainRoomIndices.Clear();
            DisplayChains.Clear();
            DisplayChainPos.Clear();
            DisplayPos.Clear();
            ChainType.Clear();
            cd.Clear();
            NeedFinalAttack = false;
            maxCount = 1;
            updateCounter = 0;
            foreach(var item in MaxCd)
                cd.Add(item.Key,Random.Range(0, MaxCd[item.Key].max)*40);
            localDamage = 0;
        }

        private static int updateCounter;

        private static float localDamage = 0;

        private static BinahPost postEffect;

        public static void GlobalUpdate(RainWorldGame game)
        {
            foreach (var item in cd)
                item.Value.value = item.Value - 1;

            if(BinahBuff.Instance.Data.Health < 0.25f && postEffect == null)
                BuffPostEffectManager.AddEffect(postEffect = new BinahPost(3));
            if (waitToDelete >= 0)
            {
                waitToDelete++;
                if(waitToDelete == 40)
                    needDelete = true;
            }

            updateCounter++;
            if (updateCounter % 160 == 0)
            {
                foreach (var room in game.world.abstractRooms)
                foreach (var abCrit in room.creatures)
                    if(Random.value < 0.05f)
                        abCrit?.abstractAI?.SetDestination((game.AlivePlayers.FirstOrDefault() ?? game.Players.First())
                            .pos);
            }
        }


        ///技能

        public static bool TryUseAttack(BinahAttackType attackType)
        {
            if (attackType == BinahAttackType.Strike && BinahBuff.Instance.Data.Health > 0.75f)
                return false;
            if (attackType == BinahAttackType.Key && BinahBuff.Instance.Data.Health < 0.25f)
                return false;
            var re = cd[attackType].value <= 0;
            if(re)
                cd[attackType].value = int.MaxValue; //等待攻击结束
            return re;
        }

        public static void EndAttack(BinahAttackType attackType)
        {
            cd[attackType].value = Random.Range(MaxCd[attackType].min, MaxCd[attackType].max) * 40;
        }

        public static readonly Dictionary<BinahAttackType,(int min,int max)> MaxCd = new Dictionary<BinahAttackType, (int min, int max)>()
        {
            { BinahAttackType.Key ,(30,45)},
            { BinahAttackType.Fairy,(20,30)},
            { BinahAttackType.Strike,(20,30)}
        };
        private static Dictionary<BinahAttackType,RefInt> cd = new Dictionary<BinahAttackType, RefInt>();
        

        public static void HitBinah(float damage)
        {
            if (waitToDelete >= 0)
                return;

            BinahBuff.Instance.Data.Health -= damage / BinahBuffData.MaxHealth * Resistance;
            localDamage += damage / BinahBuffData.MaxHealth * Resistance;
   
            BuffUtils.Log(BinahBuffData.Binah,$"Hit {damage} damage, current life: {BinahBuff.Instance.Data.Health}, resistance: {Resistance}");
            if (BinahBuff.Instance.Data.Health <= 0)
            {
                Die();
                return;
            }

            if (localDamage > 0.25f)
            {
                BuffUtils.Log(BinahBuffData.Binah, $"Hit 1/4 raw damage, switch to next part");
                BinahBuff.Instance.MyTimer.Reset();
                BinahBuff.Instance.needSpawnChain = true;

            }
        }

        private static int waitToDelete = -1;

        public static void Die()
        {
            //TODO:特效
            foreach (var chain in DisplayChains.ToList())
                chain.Break();
            DisplayChains.Clear();
            waitToDelete = 0;
            if(BuffCustom.TryGetGame(out var game))
                game.cameras[0].room.AddObject(new GhostHunch(game.cameras[0].room, null){goAt=10});
            postEffect?.Destroy();
        }


        public static void Destroy()
        {
            BuffPoolManager.Instance.RemoveBuffAndData(BinahBuffData.Binah);
            SlugcatStats.Name name;
            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
                name = game.StoryCharacter;
            else
                name = Custom.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat;

            if (BuffPoolManager.Instance.GameSetting.MissionId != SephirahMeltdownsMission.SephirahMeltdowns.value)
            {
                BuffPicker.GetNewBuffsOfType(name, 1, BuffType.Positive)[0].BuffID.CreateNewBuff();
                BuffPicker.GetNewBuffsOfType(name, 1, BuffType.Positive)[0].BuffID.CreateNewBuff();
                BuffPicker.GetNewBuffsOfType(name, 1, BuffType.Positive)[0].BuffID.CreateNewBuff();
            }

            postEffect?.Destroy();
            postEffect = null;
            OnBinahDie?.Invoke();
        }


        public static void DestroyNoDie()
        {
            foreach (var chain in DisplayChains.ToList())
                chain.Break();
            DisplayChains.Clear();
            postEffect?.Destroy();
            postEffect = null;
        }

        public static void DEBUG_ForceSetCd(BinahAttackType type, int cd)
        {
            BinahGlobalManager.cd[type] = cd;
        }

    }

    internal class BinahChain : VoidChain
    {
        public BinahChain(Room room, Vector2 spawnPosA, Vector2 spawnPosB) : base(room, spawnPosA, spawnPosB)
        {
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            foreach(var phys in room.physicalObjects)
            foreach(var weapon in phys.OfType<Weapon>())
                if (weapon.thrownBy is Player && weapon.mode == Weapon.Mode.Thrown)
                {
                    bool canHit = false;
                    for (int i = 0; i < segments.GetLength(0); i++)
                        if ((canHit = weapon.bodyChunks.Any(chunk =>
                                Custom.DistLess(chunk.pos, segments[i, 0], chunk.rad*1.5f+ weapon.firstChunk.vel.magnitude*0.8f)))==true)
                            break;
                    if (canHit)
                    {
                        Destroy();
                        weapon.ChangeMode(Weapon.Mode.Free);
                        weapon.SetRandomSpin();
                        break;
                    }
                }
        }
    }

    internal class RectPositionModule :EmitterModule, IParticleInitModule
    {
        private Rect rect;
        public float deg;
        public RectPositionModule(ParticleEmitter emitter ,Rect rect, float deg) : base(emitter)
        {
            this.deg = deg;
            this.rect = rect;
        }

        public void ApplyInit(Particle particle)
        {
            particle.HardSetPos(Random.Range(rect.xMin, rect.xMax) * Custom.DegToVec(deg + 90) +
                                Random.Range(rect.yMin, rect.yMax) * Custom.DegToVec(deg ) + emitter.pos);
            particle.moveType = Particle.MoveType.Global;
        }
    }

    internal class BindPositionModule : EmitterModule
    {
        public BindPositionModule(ParticleEmitter emitter, Action<ParticleEmitter> deg) : base(emitter)
        {
            this.deg = deg;
        }

        private Action<ParticleEmitter> deg;

        public override void Update()
        {
            base.Update();
            deg.Invoke(emitter);
        }
    }

    internal class BinahFairy : CosmeticSprite
    {
        private ParticleEmitter emitter;

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = Array.Empty<FSprite>();
        }

        public BinahFairy(Room room, Vector2 pos, Vector2 dir)
        {
            this.room = room;
            this.lastPos = this.pos = pos + dir * 25f;
            this.vel = dir * 60;
            emitter = new ParticleEmitter(room);
            emitter.ApplyParticleSpawn(new RateSpawnerModule(emitter,100,130));
            emitter.ApplyEmitterModule(new BindPositionModule(emitter,(particleEmitter =>
            {
                particleEmitter.pos = this.pos;
                particleEmitter.vel = this.vel;
            })));
            emitter.ApplyParticleModule(new AddElement(emitter,new Particle.SpriteInitParam("Binah.Fairy",$"{FakeCreatureBuffData.FakeCreatureID}.AlphaBehindTerrain")));
            emitter.ApplyParticleModule(new SetRandomLife(emitter,20, 40));
            emitter.ApplyParticleModule(new SetOriginalAlpha(emitter,0));
            emitter.ApplyParticleModule(new AlphaOverLife(emitter,(particle,time) => Mathf.InverseLerp(0,0.03f,time) * Mathf.InverseLerp(1f, 0.75f,time)));
            emitter.ApplyParticleModule(new RectPositionModule(emitter,new Rect(0,0,40,40),0));
            emitter.ApplyParticleModule(new SetRandomRotation(emitter,0,360));
            emitter.ApplyParticleModule(new SetRandomScale(emitter,new Vector2(0.5f,0.2f),new Vector2(1f,0.4f)));
            ParticleSystem.ApplyEmitterAndInit(emitter);
            if (room.BeingViewed)
                room.PlaySound(SephirahMeltdownEntry.BinahAtkFairy, pos, 0.2f, 1f);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            foreach (var d in Custom.eightDirectionsAndZero)
            {
                if (room.GetTile(d.ToVector2() * 20 + pos).Solid)
                {
                    dieCounter = 0;
                    hitPos = pos;
                    break;
                }
            }

       

            if (dieCounter < 0)
            {
                foreach (var crit in room.abstractRoom.creatures.Select(i => i.realizedCreature))
                {
                    if (crit == null || crit.dead || critSet.Contains(crit))
                        continue;
                    if (crit.bodyChunks.Any(i => Custom.DistLess(i.pos, pos, 50)))
                    {
                        crit.Stun(360);
                        room.AddObject(new CreatureSpasmer(crit,false,320));
                        critSet.Add(crit);
                    }
                }
            }
            else
            {
                dieCounter++;
                if(dieCounter == 4 || !Custom.DistLess(hitPos, pos, 50))
                    Destroy();
            }
        }
        private HashSet<Creature> critSet = new HashSet<Creature>();
        private int dieCounter = -1;
        private Vector2 hitPos;
        public override void Destroy()
        {
            base.Destroy();
            emitter.Die();
        }
    }

    internal class BinahFinalAttack : CosmeticSprite
    {
        private TempleGuard guard;

        private float time;
        private float lastTime;
        private int explodeCounter = -1;
        private float offset;
        private float vel;

        private int dieCounter = -1;

        private readonly ParticleEmitter[] emitters = new ParticleEmitter[8];


        public BinahFinalAttack(Room room, TempleGuard guard)
        {
            this.room = room;
            this.guard = guard;
            pos = lastPos = Vector2.Lerp(guard.bodyChunks[1].pos, guard.bodyChunks[2].pos, 0.5f);
            lastTime = time = (1-BinahBuff.Instance.MyTimer.frames / (BinahBuff.MaxTime * 40f)) * 9;
            for(int i =1;i<=(int)time;i++)
                emitters[(int)time-1] = CreatureEmitter(360 / 8f * (i-1) - 90, true);
      

        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = new FSprite[8];
            for (int i = 0; i < 8; i++)
                sLeaser.sprites[i] = new FSprite("Binah.Key")
                {
                    anchorX = -0.2f, width = BinahKey.Width, height = BinahKey.Width / 25 * 6, isVisible = false,
                    rotation = 360 / 8f * i - 90
                };
            AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("HUD"));

        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            for (int i = 0; i < 8; i++)
            {
                sLeaser.sprites[i].SetPosition(Custom.DegToVec(360 / 8f * i) *offset + pos - camPos);
                sLeaser.sprites[i].isVisible = time > (i+1) || explodeCounter >= 0;
                sLeaser.sprites[i].alpha = Mathf.Pow(Mathf.Clamp01((Mathf.Lerp(lastTime, time, timeStacker) - (i+1))*5), 0.4f) *
                                           Custom.LerpMap(dieCounter + timeStacker, 0, 40, 1, 0, 2f);
            }
        }

        public void Die()
        {
            dieCounter = 0;
        }

        public override void Destroy()
        {
            base.Destroy();
            foreach(var emitter in emitters)
                emitter?.Die();
        }


        public override void Update(bool eu)
        {
            base.Update(eu);
            this.pos = Vector2.Lerp(guard.bodyChunks[1].pos, guard.bodyChunks[2].pos, 0.5f);
            if (explodeCounter >= 0)
            {
                vel = Mathf.Lerp(vel, 40, 0.1f);
                offset += vel;
                for (int i = 0; i < 8; i++)
                {
                    var center = pos + Custom.DegToVec(360 / 8f * i + 90) * (BinahKey.Width - BinahKey.Width / 25 * 3 + offset);
                    var rad = BinahKey.Width / 25 * 3;
                    foreach (var crit in room.abstractRoom.creatures.Select(c => c.realizedCreature))
                    {
                        if (crit == null || crit.dead || crit.inShortcut)
                            continue;
                        if (crit.bodyChunks.Any(c => Custom.DistLess(c.pos, center, rad)))
                        {
                            room.AddObject(new CreatureSpasmer(crit,true,200));
                            crit.Stun(300);
                            crit.Die();
                        }
                    }
                }

                if (explodeCounter == 120)
                    Destroy();
                return;
            }

            if (dieCounter >= 0)
            {
                dieCounter++;
                if(dieCounter == 40)
                    Destroy();
                return;
            }
            lastTime = time;
            time = (1 - BinahBuff.Instance.MyTimer.frames / (BinahBuff.MaxTime * 40f)) * 9;

            if (time < lastTime || (int)time - 1 == emitters.Length)//时间
            {
                explodeCounter = 0;
                if (room.BeingViewed)
                    room.PlaySound(SephirahMeltdownEntry.BinahAtkFinalEnd, pos, 0.2f, 1f);
            }
            else if ((int)time != 0 && emitters[(int)time - 1] == null)
            {
                emitters[(int)time - 1] = CreatureEmitter(360 / 8f * ((int)time - 1) - 90, false);
                if (room.BeingViewed)
                    room.PlaySound(SephirahMeltdownEntry.BinahAtkFinalMake, pos, 0.3f, 1f);
            }
        }

        private ParticleEmitter CreatureEmitter(float deg, bool needBurst)
        {
            var emitter = new ParticleEmitter(room);
            emitter.ApplyEmitterModule(new BindPositionModule(emitter, (emit) =>
            {
                emit.pos = this.pos;
            }));
            float texSize = Futile.atlasManager.GetElementWithName("Binah.Smoke").sourceSize.x;

            if (needBurst)
                emitter.ApplyParticleModule(new BurstSpawnerModule(emitter, 30));
            emitter.ApplyParticleModule(new BindPositionModule(emitter, (emit) =>
            {
                emit.pos = pos + offset * Custom.DegToVec(deg+90);
                emit.lastPos = lastPos;
            }));
            emitter.ApplyParticleModule(new RateSpawnerModule(emitter, 120, 20));
            emitter.ApplyParticleModule(new SetOriginalAlpha(emitter, 0));
            emitter.ApplyParticleModule(new RectPositionModule(emitter, new Rect(BinahKey.Width * 0.2f, -(BinahKey.Width / 25 * 6) / 3, 
                BinahKey.Width * 1.15f, (BinahKey.Width / 25 * 6) * 2 / 3),deg));
            emitter.ApplyParticleModule(new SetRandomScale(emitter, 70 / texSize, 120 / texSize));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 2 * 40, 4 * 40));
            emitter.ApplyParticleModule(new SetRandomRotation(emitter, 0, 360));
            emitter.ApplyParticleModule(new SetConstVelociy(emitter, Vector2.zero));
            emitter.ApplyParticleModule(new AlphaOverLife(emitter, ((particle, time) => Mathf.InverseLerp(0, 0.1f, time))));
            emitter.ApplyParticleModule(new ColorOverLife(emitter, ((particle, time) => BinahKey.KeySmokeColor * Mathf.Lerp(0.5f, 0, time))));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Binah.Smoke", $"{StormIsApproachingEntry.StormIsApproaching}.AdditiveDefault")));
            emitter.pos = emitter.lastPos = pos;
            ParticleSystem.ApplyEmitterAndInit(emitter);
            return emitter;
        }
    }

    internal class BinahKey : CosmeticSprite
    {
        internal class BinahRing : CosmeticSprite
        {
            private readonly Vector2 dir;

            private static readonly float[] InitHeight = new[] { 100f, 150f, 250f };
            private static readonly int[] Delay = new[] { 0, 2, 5 };

            private const float WidthFac = 0.2f;
            private int counter;


            public BinahRing(Room room, Vector2 pos, Vector2 dir)
            {
                this.room = room;

                this.lastPos = this.pos = pos + dir * BinahKey.Width * 0.15f;
                this.dir = Vector2.Perpendicular(dir);
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                counter++;
                if (counter > 40)
                {
                    Destroy();
                }
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                base.InitiateSprites(sLeaser, rCam);
                sLeaser.sprites = new FSprite[3];
                for (int i = 0; i < 3; i++)
                    sLeaser.sprites[i] = new FSprite("Futile_White")
                    {
                        isVisible = false,
                        color = Color.yellow,
                        height = InitHeight[i],
                        width = InitHeight[i] * WidthFac,
                        shader = rCam.game.rainWorld.Shaders[$"SephirahMeltdownEntry.BinahWave"],
                        rotation = Custom.VecToDeg(dir)
                    };
                AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("HUD"));
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                if (slatedForDeletetion)
                    return;

                for (int i = 0; i < 3; i++)
                {
                    float size = Custom.LerpMap(counter + timeStacker - Delay[i], 0, 15, 0, 1f, 0.33f);
                    float alpha = Custom.LerpMap(counter + timeStacker - Delay[i], 3, 0, 1, 0f) *
                                  Custom.LerpMap(counter + timeStacker - Delay[i], 10, 15, 1, 0f);
                    sLeaser.sprites[i].SetPosition(pos - camPos);
                    sLeaser.sprites[i].color = (Color.yellow * alpha).CloneWithNewAlpha(1);
                    sLeaser.sprites[i].alpha = size;
                    sLeaser.sprites[i].isVisible = true;
                }
            }
        }

        private Vector2 dir;

        private int counter = 0;

        public const int ReadyCount = 80;

        public const float Width = 250;

        private const int MaxCount = ReadyCount + 160;

        public static readonly Color KeySmokeColor = Custom.hexToColor("D892FF");

        private readonly ParticleEmitter emitter;
        private Player focusPlayer;


        public BinahKey(Room room,Vector2 pos, Player focusPlayer)
        {
            this.focusPlayer = focusPlayer;
            this.room = room;
            this.dir = Custom.DirVec(pos,focusPlayer.DangerPos);
            lastPos = this.pos = pos + dir * 20;
            float texSize = Futile.atlasManager.GetElementWithName("Binah.Smoke").sourceSize.x;
            emitter= new ParticleEmitter(room);
            emitter.ApplyEmitterModule(new BindPositionModule(emitter, (emit) =>
            {
                emit.pos = this.pos;
            }));
            emitter.ApplyParticleModule(new RateSpawnerModule(emitter,120,30));
            emitter.ApplyParticleModule(new SetOriginalAlpha(emitter, 0));
            emitter.ApplyParticleModule(new RectPositionModule(emitter, new Rect(Width * 0.15f, -(Width / 25 * 6) /3, Width * 1.15f,(Width/25*6)*2/3),Custom.VecToDeg(Vector2.Perpendicular(dir))));
            emitter.ApplyParticleModule(new SetRandomScale(emitter, 70 / texSize, 120 / texSize));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 2 * 40, 4 * 40));
            emitter.ApplyParticleModule(new SetRandomRotation(emitter, 0, 360));
            emitter.ApplyParticleModule(new SetConstVelociy(emitter, Vector2.zero));
            emitter.ApplyParticleModule(new AlphaOverLife(emitter,((particle, time) => Mathf.InverseLerp(0, 0.1f, time))));
            emitter.ApplyParticleModule(new ColorOverLife(emitter,((particle, time) => KeySmokeColor * Mathf.Lerp(1f,0,time) )));
            emitter.ApplyParticleModule(new AddElement(emitter,new Particle.SpriteInitParam("Binah.Smoke",$"{StormIsApproachingEntry.StormIsApproaching}.AdditiveDefault")));
            emitter.pos = emitter.lastPos = pos;
            ParticleSystem.ApplyEmitterAndInit(emitter);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("Binah.Key") { anchorX = -0.15f, width = Width, height = Width / 25 * 6 , isVisible = false};
            AddToContainer(sLeaser,rCam,rCam.ReturnFContainer("HUD"));
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            counter++;
            if (counter < ReadyCount * 0.8f && focusPlayer.room == room)
            {
                dir = Vector2.Lerp(dir, Custom.DirVec(pos, focusPlayer.DangerPos), 0.05f);
                emitter.PInitModules.OfType<RectPositionModule>().First().deg =
                    Custom.VecToDeg(Vector2.Perpendicular(dir));
            }

            if (counter == ReadyCount - 2)
            {
                room.AddObject(new BinahRing(room, pos, dir));
                if (room.BeingViewed)
                    room.PlaySound(SephirahMeltdownEntry.BinahAtkStone, pos, 0.2f, 1);
            }

            if (counter >= ReadyCount)
            {
                vel = Vector2.Lerp(vel, dir*35, 0.1f);
                //伤害判断
                var center = pos + dir * (Width - Width / 25 * 3);
                var rad = Width / 25 * 3;
                foreach (var crit in room.abstractRoom.creatures.Select(i => i.realizedCreature))
                {
                    if(crit == null || crit.dead)
                        continue;
                    if(crit.bodyChunks.Any(i => Custom.DistLess(i.pos, center, rad)))
                        crit.Stun(150);
                }

                if (!hasTriggerCreature)
                {
                    foreach (var dir in Custom.eightDirectionsAndZero.Select(i => i.ToVector2()*20))
                    {
                        if (room.GetTile(dir + center).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
                        {
                            var index=  room.shortcutsIndex.IndexfOf(room.GetTilePosition(dir + center));
                            if (index != -1 && room.shortcuts[index].shortCutType != ShortcutData.Type.DeadEnd)
                            {
                                var crit = FakeCreatureEntry.templates[
                                    Random.Range(0, FakeCreatureEntry.templates.Length)];
                                if (crit.type == CreatureTemplate.Type.RedCentipede)
                                    crit = StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.RedLizard);
                                AbstractCreature acreature = new AbstractCreature(room.world,
                                    crit,
                                    null, new WorldCoordinate(room.abstractRoom.index,0,0,index), room.world.game.GetNewID());
                             
                                if (room.shortcuts[index].destinationCoord.room != -1 && room.shortcuts[index].destNode != -1)
                                {
                                    acreature.Realize();
                                    var creature = acreature.realizedCreature;
                                    creature.inShortcut = true;
                                    room.world.game.shortcuts.CreatureEnterFromAbstractRoom(creature,
                                        room.world.GetAbstractRoom(room.shortcuts[index].destinationCoord.room),
                                        room.shortcuts[index].destNode);
                                }
                                else
                                {
                                    acreature.RealizeInRoom();
                                    var creature = acreature.realizedCreature;
                                    foreach (var chunk in creature.bodyChunks)
                                        chunk.HardSetPosition(dir+center);
                                    creature.Stun(50);
                                }
                                room.AddObject(new BinahShowEffect(room, dir + center, 200, RainWorld.SaturatedGold));
                                BuffUtils.Log(BinahBuffData.Binah,$"Spawn creature cause by key attack, Type:{acreature.creatureTemplate.type}, pos:{dir+center}, shortCut:{room.shortcuts[index].shortCutType}");
                                hasTriggerCreature = true;
                                break;
                            }
                        }
                    }
                }

            }
            if(counter >= MaxCount)
                Destroy();

        }

        private bool hasTriggerCreature;

        public override void Destroy()
        {
            base.Destroy();
            emitter.Die();
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            if (slatedForDeletetion)
                return;
            sLeaser.sprites[0].alpha = Mathf.InverseLerp(0, ReadyCount*0.6f, counter + timeStacker);
            sLeaser.sprites[0].SetPosition(Vector2.Lerp(lastPos, pos, timeStacker) - camPos);
            sLeaser.sprites[0].isVisible = true;
            sLeaser.sprites[0].rotation = Custom.VecToDeg(Vector2.Perpendicular(dir));
        }
    }



    internal class BinahShowEffect : CosmeticSprite
    {
        private readonly float rad;
        private int counter;
        private readonly Color color;
        private ParticleEmitter emitter;

        public BinahShowEffect(Room room, Vector2 pos, float rad, Color color)
        {
            this.rad = rad/20;
            this.room = room;
            this.pos = pos;
            this.color = color;
            room.AddObject(new ShockWave(pos, rad*1.2F, 0.01f, 20, true)); 
            emitter = new ParticleEmitter(room);
            emitter.pos = emitter.lastPos = pos;
            emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter,Random.Range(20,30)));

            emitter.ApplyParticleModule(new AddElement(emitter,
                new Particle.SpriteInitParam("Circle20",null,8,1,0.03f, RainWorld.SaturatedGold*2)));
            emitter.ApplyParticleModule(new AddElement(emitter,
                new Particle.SpriteInitParam("Futile_White", "LightSource",8,0.5f)));
            emitter.ApplyParticleModule(new SetRandomPos(emitter, rad*0.3f));
            emitter.ApplyParticleModule(new SetMoveType(emitter,Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetOriginalAlpha(emitter,0));
            emitter.OnParticleInitEvent += (particle) =>
            {
                particle.vel = Custom.RNV() * Random.Range(13,20);
            };
            emitter.ApplyParticleModule(new SetRandomLife(emitter,30,60));
            emitter.ApplyParticleModule(new SetConstColor(emitter,RainWorld.GoldRGB*1.5f));
            emitter.ApplyParticleModule(new SetRandomScale(emitter,5,10));
            emitter.ApplyParticleModule(new AlphaOverLife(emitter, (particle, time) =>
            {
                particle.vel = (particle.vel + Custom.RNV() * 1).normalized * particle.vel.magnitude;
                particle.vel *= 0.93f;
                return Mathf.Pow(1-time, 2f);
            }));
            ParticleSystem.ApplyEmitterAndInit(emitter);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = new FSprite("Futile_White") { shader = rCam.game.rainWorld.Shaders["LightSource"],color = color };
            sLeaser.sprites[1] = new FSprite("Futile_White") { shader = rCam.game.rainWorld.Shaders["FlatLight"],color=color*2 };
            AddToContainer(sLeaser,rCam,rCam.ReturnFContainer("HUD"));
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            counter++;
            if (counter == 41)
            {
                Destroy();
                emitter.Die();
            }

        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            if (slatedForDeletetion)
                return;

            sLeaser.sprites[0].scale = Custom.LerpMap(counter + timeStacker, 0, 40, 0, 1, 0.33f) * rad;
            sLeaser.sprites[1].scale = Custom.LerpMap(counter + timeStacker, 0, 40, 0, 1, 0.33f) * rad * 1.3f;
            sLeaser.sprites[0].alpha = sLeaser.sprites[1].alpha =
                Custom.LerpMap(counter + timeStacker, 0, 13, 0, 1, 0.33f) *
                Custom.LerpMap(counter + timeStacker, 40, 20, 0, 1, 1);
            sLeaser.sprites[0].SetPosition(pos - camPos);
            sLeaser.sprites[1].SetPosition(pos - camPos);


        }
    }

    internal class BinahRoom : UpdatableAndDeletable
    {
        private Vector2 chainPos = new Vector2(-1,-1);
        private bool canSpawn = false;
        private readonly int index;
        private readonly bool needChain;

        private BinahChain chain;
        private Vector2 pos;

        private TempleGuard guard;
        private float chainsActiveTime;
        private int totTime;
        private bool isInit;

        private BinahFinalAttack finalAttack;

        public BinahRoom(Room room, bool needChain)
        {
            this.room = room;
            this.needChain = needChain;
            index = BinahGlobalManager.DisplayChains.Count(i => i.room == room);
            BinahGlobalManager.OnBinahNewChain += BinahGlobalManager_OnBinahNewChain;
        }

        private void BinahGlobalManager_OnBinahNewChain()
        {
            finalAttack = null;
        }

        private int initState = 0;
        private void Init()
        {
            initState++;
            if (initState < 10)
            {
                if (BinahGlobalManager.DisplayPos.TryGetValue(room.abstractRoom.index, out var value))
                {
                    pos = value.x < 0 ? room.MiddleOfTile(room.shortcutsIndex.FirstOrDefault()) : value;
                    canSpawn = value.x > 0;
                    initState = 10;
                    if (canSpawn) 
                        SpawnGuard();
                }
                else
                {
                    for (int i = 0; i < 30; i++)
                    {
                        var tile = room.RandomTile();
                        if (room.GetTile(tile).Solid || room.aimap.getTerrainProximity(tile) == -1)
                            continue;
                        while ((room.aimap.getTerrainProximity(tile) < 7 || room.aimap.getAItile(tile).narrowSpace) &&
                               tile.y < room.Height)
                            tile.y++;

                        if (room.aimap.getTerrainProximity(tile) < 7 || room.aimap.getAItile(tile).narrowSpace)
                            continue;
                        pos = room.MiddleOfTile(tile);
                        canSpawn = true;
                        break;
                    }


                    if (canSpawn)
                    {
                        SpawnGuard();
                        initState = 10;
                        BinahGlobalManager.DisplayPos.Add(room.abstractRoom.index, canSpawn ? pos : new Vector2(-1, -1));
                    }
                }
            }
            else
            {
                if (!canSpawn && initState == 10)
                {
                    BuffUtils.Log(BinahBuffData.Binah,
                        $"Can't find pos to place template guard at {room.abstractRoom.name}");
                    pos = room.RandomPos();
                    while (!room.GetTile(pos).Solid)
                        pos = room.RandomPos();
                }

                if (initState == 20 && needChain)
                {
                    if (chainPos.x <= 0)
                    {
                        chainPos = room.MiddleOfTile(canSpawn
                            ? room.shortcutsIndex.FirstOrDefault()
                            : (room.shortcutsIndex.Length > 1 ? room.shortcutsIndex[1] : room.RandomTile()));
                        BuffUtils.Log(BinahBuffData.Binah,
                            $"Can't find pos to place chain at {room.abstractRoom.name}");

                        if (!BinahGlobalManager.DisplayChainPos.ContainsKey(room.abstractRoom.index))
                            BinahGlobalManager.DisplayChainPos.Add(room.abstractRoom.index, new List<Vector2>());
                        BinahGlobalManager.DisplayChainPos[room.abstractRoom.index].Add(chainPos);
                        BinahGlobalManager.DisplayChains.Add(this);
                        room.AddObject(chain = new BinahChain(room, pos, chainPos));
                        isInit = true;
                        return;
                    }
                }
                if (needChain)
                {
                    if (BinahGlobalManager.DisplayChainPos.TryGetValue(room.abstractRoom.index, out var chainList) && chainList.Count > index)
                    {
                        chainPos = chainList[index];
                        BinahGlobalManager.DisplayChains.Add(this);
                        room.AddObject(chain = new BinahChain(room, pos, chainPos));
                        isInit = true;
                    }
                    else
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            var x = room.RandomTile().x;
                            var y = 0;
                            while (y < room.Height - 1)
                            {
                                if (Custom.DistLess(pos, room.MiddleOfTile(x, y), 250))
                                {
                                    y++;
                                    continue;
                                }
                                if (
                                    (room.GetTile(x, y).Solid || room.GetTile(x, y).Terrain == Room.Tile.TerrainType.Floor || room.GetTile(x, y).Terrain == Room.Tile.TerrainType.Slope)
                                    && room.GetTile(x, y + 1).Terrain == Room.Tile.TerrainType.Air)
                                {
                                    chainPos = room.MiddleOfTile(x, y);
                                    break;
                                }
                                if ((room.GetTile(x, y + 1).Solid || room.GetTile(x, y + 1).Terrain == Room.Tile.TerrainType.Floor ||
                                          room.GetTile(x, y + 1).Terrain == Room.Tile.TerrainType.Slope) && room.GetTile(x, y).Terrain == Room.Tile.TerrainType.Air)
                                {
                                    chainPos = room.MiddleOfTile(x, y + 1);
                                    break;
                                }
                                y++;
                            }

                            if (chainPos.x >= 0)
                            {
                                if (!BinahGlobalManager.DisplayChainPos.ContainsKey(room.abstractRoom.index))
                                    BinahGlobalManager.DisplayChainPos.Add(room.abstractRoom.index, new List<Vector2>());
                                BinahGlobalManager.DisplayChainPos[room.abstractRoom.index].Add(chainPos);
                                BinahGlobalManager.DisplayChains.Add(this);
                                room.AddObject(chain = new BinahChain(room, pos, chainPos));
                                isInit = true;
                                return;
                            }
                        }

                 
    
                    }
                }
                else
                {
                    isInit = true;
                }

            }
            
        }

        private void SpawnGuard()
        {
            guard = room.abstractRoom.creatures.FirstOrDefault(i =>
                i.creatureTemplate.type == CreatureTemplate.Type.TempleGuard &&
                i.ID.spawner == BinahBuffData.Binah.valueHash && i.realizedCreature != null)?.realizedCreature as TempleGuard;
            if (guard != null)
                return;
            var tile = room.GetTilePosition(pos);
            AbstractCreature crit = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.TempleGuard),
                null, new WorldCoordinate(room.abstractRoom.index, tile.x, tile.y, 0), room.world.game.GetNewID(BinahBuffData.Binah.valueHash))
            {
                lavaImmune = true,
                destroyOnAbstraction = true
            };
            crit.RealizeInRoom();
            guard = crit.realizedCreature as TempleGuard;

        }

        public void Die()
        {
            if (guard != null)
            {
                room.AddObject(new GhostEffect(guard.graphicsModule, 40, 1, 0));
                guard.Destroy();
                guard = null;
            }

            if (finalAttack != null)
            {
                finalAttack.Die();
                finalAttack = null;
            }

            Destroy();
        }


        public override void Update(bool eu)
        {
            base.Update(eu);

            if (!isInit)
            {
                if (!room.readyForAI)
                    return;
                Init();
            }

            if (BinahGlobalManager.needDelete)
            {
                Die();
            }

            if (BinahGlobalManager.NeedFinalAttack && finalAttack == null && canSpawn)
                room.AddObject(finalAttack = new BinahFinalAttack(room, guard));
            else if (!BinahGlobalManager.NeedFinalAttack && finalAttack != null)
            {
                finalAttack.Die();
                finalAttack = null;
            }


            totTime++;
            chainsActiveTime += 1f;
            if (chain != null)
            {
                if (chain.slatedForDeletetion)
                {
                    chain = null;
                    Break();
                }
                else
                {
                    if (guard != null)
                        chain.stuckPosA = pos = guard.firstChunk.pos;
                    chainsActiveTime += 1f;
                    chain.proximityAlpha = 1;
                    chain.colorFlash =
                        Mathf.Sin(Mathf.PI * ((float)totTime / 400f) - Mathf.PI) * 0.25f + 0.25f +
                        0.5f * Mathf.Min(1f, chainsActiveTime / 80f);
                }
             
            }

            
        }
        public void TimeOutBreak()
        {
            room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Chain_Break, 0f, 1f, Random.value * 0.5f + 0.8f);
            chain.Destroy();
            chain = null;
            AbstractCreature crit = new AbstractCreature(room.world,
                StaticWorld.GetCreatureTemplate(BinahGlobalManager.ChainType[room.abstractRoom.index]), null,
                new WorldCoordinate(room.abstractRoom.index, room.GetTilePosition(chainPos).x,
                    room.GetTilePosition(chainPos).y, -1), room.game.GetNewID());
            crit.lavaImmune = true;
            crit.voidCreature = true;
            crit.RealizeInRoom();
            if(room.PlayersInRoom.Count != 0)
                room.AddObject(new BinahShowEffect(room, chainPos, 200, RainWorld.SaturatedGold));
            BuffUtils.Log(BinahBuffData.Binah, $"Time out break at:{room.abstractRoom.name}, spawn:{crit.creatureTemplate.type}");
        }

        public void Break()
        {
            room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Chain_Break, 0f, 1f, Random.value * 0.5f + 0.8f);
            BinahGlobalManager.DisplayChains.Remove(this);
            BinahGlobalManager.ChainRoomIndices.Remove(room.abstractRoom.index);
            BuffUtils.Log(BinahBuffData.Binah, $"Break at:{room.abstractRoom.name}");

        }


        public override void Destroy()
        {
            base.Destroy();
            chain?.Destroy();
            BinahGlobalManager.DisplayChains.Remove(this);
            if(guard != null && BinahHook.Modules.TryGetValue(guard,out var module))
                module.OnDestroy();
        }
    }

    internal class BinahStrike : CosmeticSprite
    {
        private const int WaitCounter = 80;
        private const int OutCounter = 6;

        private int counter = 0;
        public BinahStrike(Room room, Vector2 pos)
        {
            this.room = room;
            this.lastPos = this.pos = pos - new Vector2(0, 15);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
    
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = new FSprite("Binah.StrikeFog"){height = 25,width = 150,alpha = 0};
            sLeaser.sprites[1] = new FSprite("Binah.Strike") { height = 0, width = 150, anchorY = 0 };
            AddToContainer(sLeaser,rCam,rCam.ReturnFContainer("Water"));
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            if (slatedForDeletetion)
                return;
            var alpha = Custom.LerpMap(counter + timeStacker, WaitCounter, WaitCounter + OutCounter, 0, 1,0.33f) *
                        Custom.LerpMap(counter + timeStacker, WaitCounter + OutCounter, WaitCounter + OutCounter * 2, 1,
                            0,2f);
            sLeaser.sprites[0].SetPosition(pos - camPos + Vector2.down*5* alpha);
            sLeaser.sprites[1].SetPosition(pos - camPos);
            sLeaser.sprites[1].height = alpha * 200;
            sLeaser.sprites[0].alpha =
                Mathf.Lerp(sLeaser.sprites[0].alpha, counter > WaitCounter + OutCounter ? 0 : 1, 0.05f);

        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            counter++;
            if(counter == WaitCounter-10 && room.BeingViewed)
                room.PlaySound(SephirahMeltdownEntry.BinahAtkStrike, pos, 0.2f, 1);

            if (Math.Abs(counter - (WaitCounter + OutCounter * 0.5f)) < 2)
            {
                foreach (var crit in room.abstractRoom.creatures.Select(i => i.realizedCreature))
                {
                    if(crit== null || crit.dead || crit.inShortcut) continue;

                    if (crit.bodyChunks.Any(i => Custom.DistLess(i.pos, pos + new Vector2(0, 50), 80)))
                    {
                        crit.Violence(crit.mainBodyChunk, null, crit.mainBodyChunk, null, Creature.DamageType.Blunt, 2,
                            80);
                    }
                }
            }

            if (counter == WaitCounter + 2 * OutCounter)
            {
                Destroy();
            }
        }
    }

    class BinahPost : BuffPostEffect
    {
        
        public BinahPost(int layer) : base(layer)
        {
            material = new Material(SephirahMeltdownEntry.BinahScreenEffect);
            material.SetTexture("_EffectTex",SephirahMeltdownEntry.BinahScreenEffectTexture);
            toRotation[0] = rotation[0] = Random.value + 200;
            toRotation[1] = rotation[1] = Random.value + 200;
            waitCounter[0] = RandomValue;
            waitCounter[1] = RandomValue;

        }

        private float RandomValue => Random.Range(2, 5);

        public override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            material.SetColor("_EffectSV1",new Color(0.7f, 0.5f, 0.128F, 0.8f));
            material.SetColor("_EffectSV2", new Color(0.8f, 0.8f,0.022f,0.8f));
            material.SetColor("SnowColor",new Color(1,1,1,snowAlpha));
            Graphics.Blit(source,destination,material);
        }

        public override void Update()
        {
            base.Update();
            snowAlpha = Mathf.Lerp(snowAlpha, waitingForDelete ? 0 :0.3f, (waitingForDelete ? 0.05f : 0.01f) * Time.deltaTime * BuffCustom.TimeSpeed * 40f);
            if (snowAlpha < 0.01f && waitingForDelete)
            {
                needDeletion = true;
                return;
            }
            //for (int i = 0; i < 2; i++)
            //{
            //    rotation[i] = Mathf.Lerp(rotation[i], toRotation[i], 0.02f * Time.deltaTime * BuffCustom.TimeSpeed * 40f);
            //    if ((waitCounter[i] -= Time.deltaTime * BuffCustom.TimeSpeed) < 0)
            //    {
            //        waitCounter[i] = RandomValue;
            //        toRotation[i] += Random.Range(-0.5f, 0.5f);
            //    }
                
            //}

        }

        public override void Destroy()
        {
            waitingForDelete = true;
        }

        private bool waitingForDelete;
        private float snowAlpha;

        private readonly float[] rotation = new float[2];
        private readonly float[] toRotation = new float[2];


        private readonly float[] waitCounter = new float[2];
    }
}
