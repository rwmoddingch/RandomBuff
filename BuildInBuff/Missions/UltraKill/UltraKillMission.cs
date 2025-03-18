using BuiltinBuffs.Positive;
using MoreSlugcats;
using Music;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuff.Render.UI;
using RandomBuffUtils;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Missions.UltraKill
{
    internal class UltraKillMission : Mission, IMissionEntry
    {
        public static readonly MissionID UltraKillMissionID = new MissionID(nameof(UltraKillMissionID), true);
        public override MissionID ID => UltraKillMissionID;

        public override SlugcatStats.Name BindSlug => null;

        public override Color TextCol => Color.red;

        public override string MissionName => BuffResourceString.Get("Mission_Display_UltraKill", true);

        public UltraKillMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    //new DeathCondition(){deathCount = 10},
                    new UltraKillCondition()
                },

                gachaTemplate = new NormalGachaTemplate()
                {
                    ForceStartPos = "BUFF_MISSION0",
                    PocketPackMultiply = 0,
                }
            };
            startBuffSet.Add(UltraCoinsBuffEntry.ultraCoinsBuffID);
            startBuffSet.Add(IntenseSituationBuffEntry.IntenseSituation);
        }

        public void RegisterMission()
        {
            UltraKillWave.InitWaveInfos();
            MissionRegister.RegisterMission(UltraKillMissionID, new UltraKillMission());
            BuffRegister.RegisterCondition<UltraKillCondition>(UltraKillCondition.ultraKillConditionID, "UltraKill", true);
        }
    }

    internal class UltraKillCondition : Condition
    {
        public static ConditionID ultraKillConditionID = new ConditionID("UltraKillCondition", true);
        public override ConditionID ID => ultraKillConditionID;

        public override int Exp => 100;

        UltraKillManager manager;

        public override string DisplayName(InGameTranslator translator)
        {
            return BuffResourceString.Get("DisplayName_UltraKill", true);
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"";
        }

        public override void EnterGame(RainWorldGame game)
        {
            base.EnterGame(game);
            On.Room.AddObject += Room_AddObject;
        }

        private void Room_AddObject(On.Room.orig_AddObject orig, Room self, UpdatableAndDeletable obj)
        {
            orig.Invoke(self, obj);
            if(manager != null && manager.missionHUD != null && obj is Weapon weapon)
            {
                manager.missionHUD.objTrackerProj.AddObject(weapon);
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            manager?.Destroy();
            manager = null;
            On.Room.AddObject -= Room_AddObject;
        }

        int level = 0;
        public override void InGameUpdate(RainWorldGame game)
        {
            base.InGameUpdate(game);

            if (manager == null && game.cameras != null && game.cameras[0].room != null)
            {
                game.cameras[0].room.AddObject(manager = new UltraKillManager(game.cameras[0].room, () => Finished = true));
            }
        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            return ConditionState.Ok_NoMore;
        }
    }

    internal class UltraKillManager : UpdatableAndDeletable
    {
        internal int time;
        internal int waveIndex;
        internal int rollBackWave;

        internal UltraKillMissionHUD missionHUD;
        internal UltraKillEventManager eventManager;
        internal UltraKillPlayerRevulver ultraKillPlayerRevulver;

        List<WaveObject> activeWaveObjects = new List<WaveObject>();

        List<BuffID> activeExtraBuffs = new List<BuffID>();
        List<int> activeBuffStackCount = new List<int>();

        Action finishCallBack;

        bool finished;
        int finishWaitCounter;

        int inWaveID;

        List<string> songs = new List<string>();
        int songIndex = 0;
        int switchSongDelay;

        MusicPlayer MusicPlayer => room.game.manager.musicPlayer;

        public UltraKillManager(Room room, Action finishCallBack)
        {
            this.room = room;
            this.finishCallBack = finishCallBack;
            BuffUtils.Log("UltraKillMission", $"Init UltraKillManager");
            UltraKillHooks.HooksOn();

            foreach(var template in StaticWorld.creatureTemplates)
            {
                if (template.socialMemory)
                {

                    room.world.game.session.creatureCommunities.InfluenceLikeOfPlayer(template.communityID, room.world.RegionNumber, 0, -1000f, 1000f, 1000f);
                }
            }
        }

        //bool keyDown;
        public override void Update(bool eu)
        {
            base.Update(eu);

            time++;

            if (missionHUD == null && room.game.cameras != null && room.game.cameras[0].hud != null && room.game.cameras[0].hud.fContainers != null)
            {
                room.game.cameras[0].hud.AddPart(missionHUD = new UltraKillMissionHUD(room.game, room.game.cameras[0].hud));
                eventManager = new UltraKillEventManager(missionHUD, room.game);
                ultraKillPlayerRevulver = new UltraKillPlayerRevulver(this, missionHUD);
            }

            //if(room.game.manager.musicPlayer.threatTracker.region != "SS")
            //{
            //    room.game.manager.musicPlayer.threatTracker.region = "SS";
            //    room.game.manager.musicPlayer.NewRegion("SS");
            //}

            //room.game.manager.musicPlayer.threatTracker.currentThreat = 1300f;

            //BuffUtils.Log("UltraKillMission", $"{MusicPlayer.song != null} : {(MusicPlayer.song != null ? MusicPlayer.song.name : null)}");
            if (switchSongDelay > 0)
                switchSongDelay--;

            if ((MusicPlayer.song == null) && songs.Count > 0 && switchSongDelay == 0)
            {
                MusicPlayer.GameRequestsSong(new MusicEvent()
                {
                    fadeInTime = 4f,
                    roomsRange = -1,
                    cyclesRest = 0,
                    volume = 0.15f,
                    prio = 10f,
                    stopAtDeath = false,
                    stopAtGate = false,
                    loop = false,
                    maxThreatLevel = 1300,
                    songName = $"BUFF_{UltraCoinsBuffEntry.ultraCoinsBuffID.GetStaticData().AssetPath}/{songs[songIndex]}",
                });
                BuffUtils.Log("UltraKillMission", $"switch song : {songs[songIndex]}");
                songIndex++;
                if (songIndex >= songs.Count)
                {
                    songIndex = 0;
                }
            }
            if (MusicPlayer.song != null)
            {
                MusicPlayer.song.baseVolume = Mathf.Lerp(0.2f, 0.26f, Mathf.InverseLerp(0f, 1.1f, MusicPlayer.threatTracker.currentMusicAgnosticThreat));
                //BuffUtils.Log("UltraKillMission", $"{MusicPlayer.threatTracker.currentThreat}");
            }

            eventManager?.Update();
            ultraKillPlayerRevulver?.Update();


            if (activeWaveObjects.Count == 0)
            {
                if(waveIndex < UltraKillWave.waves.Count)
                    InitNextWave();
                else if(!finished)
                {
                    finished = true;
                    finishCallBack?.Invoke();
                    finishWaitCounter = 160;
                }
            }

            if(finished && finishWaitCounter > 0)
            {
                finishWaitCounter--;
                //BuffUtils.Log("UltraKillMission", $"{finishWaitCounter}");
                if(finishWaitCounter == 0)
                {
                    //BuffUtils.Log("UltraKillMission", $"Win");
                    room.game.Win(false);
                }
            }

            for (int i = activeWaveObjects.Count - 1; i >= 0; i--)
            {
                if (activeWaveObjects[i].Finished)
                    activeWaveObjects.RemoveAt(i);
            }
            room.game.world.rainCycle.cycleLength = 4800;
            room.game.world.rainCycle.timer = 2400;

            //bool thiskeyDown = Input.GetKey(KeyCode.K);
            //if (thiskeyDown && !keyDown)
            //{
            //    for (int i = room.updateList.Count - 1; i >= 0; i--)
            //    {
            //        if (room.updateList[i] is Player)
            //            continue;
            //        if (room.updateList[i] is Creature creature && !creature.dead)
            //        {
            //            creature.Die();
            //        }
            //    }
            //}
            //keyDown = thiskeyDown;
        }

        public void InitNextWave()
        {
            var waveInfo = UltraKillWave.waves[waveIndex];
            BuffUtils.Log("UltraKillMission", $"Init Wave {waveIndex}");
            if (waveInfo is UltraKillWave.WaitInfo waitInfo)
            {
                activeWaveObjects.Add(new UltraKillWaveWait(waitInfo.waitTime));
                BuffUtils.Log("UltraKillMission", $"Wave wait {waitInfo.waitTime}");
            }
            else if (waveInfo is UltraKillWave.SpawnItemInfo spawnItemInfo)
            {
                foreach (var posInfo in spawnItemInfo.spawns.Keys)
                {
                    int bias = 0;
                    for (int i = 0; i < spawnItemInfo.spawns[posInfo].Count; i++)
                    {
                        activeWaveObjects.Add(new UltraKillWaveObjectGenerator(room, UltraKillWave.spawnItemPos[posInfo] + new IntVector2(bias, 0), spawnItemInfo.spawns[posInfo][i], spawnItemInfo.specials[posInfo][i], GetNewID()));
                        bias++;
                        BuffUtils.Log("UltraKillMission", $"Wave spawn item {spawnItemInfo.spawns[posInfo][i]} at {UltraKillWave.spawnItemPos[posInfo]}");
                    }
                }
                rollBackWave = waveIndex;
            }
            else if (waveInfo is UltraKillWave.SpawnCreatureInfo spawnCreatureInfo)
            {
                foreach (var posInfo in spawnCreatureInfo.spawns.Keys)
                {
                    int bias = 0;

                    for (int i = 0; i < spawnCreatureInfo.spawns[posInfo].Count; i++)
                    {
                        var creatureType = spawnCreatureInfo.spawns[posInfo][i];

                        if (spawnCreatureInfo.specials[posInfo][i] != 0 && (creatureType == CreatureTemplate.Type.Scavenger || creatureType == MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite))
                        {
                            activeWaveObjects.Add(new UltraKillWaveCreatureGenerator(room, UltraKillWave.spawnEnemyPos[posInfo] + new IntVector2(bias, 0), creatureType, new EntityID(-1, spawnCreatureInfo.specials[posInfo][i])));
                        }
                        else
                            activeWaveObjects.Add(new UltraKillWaveCreatureGenerator(room, UltraKillWave.spawnEnemyPos[posInfo] + new IntVector2(bias, 0), creatureType, GetNewID()));

                        bias++;
                        BuffUtils.Log("UltraKillMission", $"Wave spawn creature {creatureType} at {UltraKillWave.spawnEnemyPos[posInfo]}");
                    }
                        
                }
            }
            else if (waveInfo is UltraKillWave.ExtraBuffInfo extraBuff)
            {
                for (int i = 0; i < extraBuff.extraBuffs.Count; i++)
                {
                    activeExtraBuffs.Add(extraBuff.extraBuffs[i]);
                    activeBuffStackCount.Add(extraBuff.stackCount[i]);

                    for (int k = 0; k < extraBuff.stackCount[i]; k++)
                        extraBuff.extraBuffs[i].CreateNewBuff(extraBuff.stackCount[i] > 1);
                }
            }
            else if (waveInfo is UltraKillWave.ClearStageInfo)
            {
                ClearStage();
            }
            else if(waveInfo is UltraKillWave.FinishInfo)
            {
                finishCallBack?.Invoke();

                activeWaveObjects.Add(new WaveObject());
            }
            else if(waveInfo is UltraKillWave.SongInfo song)
            {
                MusicPlayer.GameRequestsSongStop(new StopMusicEvent()
                {
                    fadeOutTime = 2f,
                    prio = 100,
                    type = StopMusicEvent.Type.AllSongs
                });

                switchSongDelay = 40;
                songs.Clear();
                foreach(var s in song.songName)
                {
                    songs.Add(s);
                }
                songIndex = 0;
            }

            foreach (var obj in activeWaveObjects)
            {
                room.AddObject(obj);
            }
            waveIndex++;
        }

        internal void ClearStage()
        {
            for (int i = room.updateList.Count - 1; i >= 0; i--)
            {
                if (room.updateList[i] is Player)
                    continue;
                if (room.updateList[i] is PhysicalObject obj && (obj.grabbedBy == null || obj.grabbedBy.Count == 0))
                {
                    obj.Destroy();
                }
                if (room.updateList[i] is Creature creature)
                {
                    creature.Destroy();
                }
            }

            for (int i = 0; i < activeExtraBuffs.Count; i++)
            {
                for (int k = 0; k < activeBuffStackCount[i]; k++)
                    activeExtraBuffs[i].UnstackBuff();
            }

            foreach(var waveObj in activeWaveObjects)
                waveObj.Destroy();

            activeWaveObjects.Clear();
            activeExtraBuffs.Clear();
            activeBuffStackCount.Clear();


            foreach (var update in room.updateList)
            {
                if (update is Player player)
                {
                    if (player.graphicsModule != null)
                    {
                        bool get = false;
                        foreach (var sleaser in room.game.cameras[0].spriteLeasers)
                        {
                            if (sleaser.drawableObject == player.graphicsModule)
                            {
                                sleaser.CleanSpritesAndRemove();
                                sleaser.deleteMeNextFrame = false;
                                player.graphicsModule.InitiateSprites(sleaser, room.game.cameras[0]);
                            }
                        }
                    }
                    else
                    {
                        player.InitiateGraphicsModule();
                        room.game.cameras[0].NewObjectInRoom(player.graphicsModule);
                    }
                }
            }
        }

        internal void RollBack()
        {
            waveIndex = rollBackWave;
            inWaveID = 0;
        }

        public override void Destroy()
        {
            base.Destroy();
            eventManager?.Destroy();
            ultraKillPlayerRevulver?.Destroy();
            UltraKillHooks.HookOff();
            MusicPlayer.GameRequestsSongStop(new StopMusicEvent()
            {
                fadeOutTime = 1f,
                prio = 100,
                type = StopMusicEvent.Type.AllSongs
            });
        }

        EntityID GetNewID()
        {
            inWaveID++;
            return new EntityID(-1, waveIndex * 1000 + inWaveID);  
        }


        internal class WaveObject : UpdatableAndDeletable
        {
            public bool Finished { get; protected set; }
        }

        internal class UltraKillWaveWait : WaveObject
        {
            public int wait;
            public UltraKillWaveWait(int waitTime)
            {
                wait = waitTime;
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                if (wait > 0)
                {
                    wait--;
                    if (wait == 0)
                    {
                        Finished = true;
                        Destroy();
                    }
                }
            }
        }

        internal class UltraKillWaveCreatureGenerator : WaveObject
        {
            CreatureTemplate.Type generateType;
            IntVector2 bindTile;


            public bool targetKilled;
            public int beforeGenerateCounter = 80;

            Creature spawnedCreature;
            EntityID entityID;


            public UltraKillWaveCreatureGenerator(Room room, IntVector2 bindTile, CreatureTemplate.Type generateType, EntityID entityID)
            {
                this.generateType = generateType;
                this.bindTile = bindTile;
                this.entityID = entityID;
                room.AddObject(new WaveGenEffect(room, Color.red, bindTile, beforeGenerateCounter, 1f));
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                if (beforeGenerateCounter > 0)
                {
                    beforeGenerateCounter--;
                    if (beforeGenerateCounter == 0)
                    {
                        var abCreature = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(generateType), null, room.GetWorldCoordinate(bindTile), entityID);
                        room.abstractRoom.AddEntity(abCreature);

                        if(StaticWorld.GetCreatureTemplate(generateType).TopAncestor().type == CreatureTemplate.Type.Scavenger)
                        {
                            (abCreature.abstractAI as ScavengerAbstractAI).InitGearUp();
                        }

                        abCreature.RealizeInRoom();

                        spawnedCreature = abCreature.realizedCreature;
                    }
                }
                if (spawnedCreature != null && spawnedCreature.dead)
                {
                    Finished = true;
                    Destroy();
                }
            }

            public override void Destroy()
            {
                base.Destroy();
                spawnedCreature = null;
            }
        }

        internal class UltraKillWaveObjectGenerator : WaveObject
        {
            IntVector2 bindTile;
            public int beforeGenerateCounter = 60;

            AbstractPhysicalObject.AbstractObjectType generateType;
            EntityID entityID;
            int special;

            public UltraKillWaveObjectGenerator(Room room, IntVector2 bindTile, AbstractPhysicalObject.AbstractObjectType generateType, int special, EntityID entityID)
            {
                this.generateType = generateType;
                this.special = special;
                this.bindTile = bindTile;
                this.entityID = entityID;
                room.AddObject(new WaveGenEffect(room, Color.cyan, bindTile, beforeGenerateCounter, 0.5f));
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                if (beforeGenerateCounter > 0)
                {
                    beforeGenerateCounter--;
                    if (beforeGenerateCounter == 0)
                    {
                        try
                        {
                            AbstractPhysicalObject abObj;
                            if (generateType == AbstractPhysicalObject.AbstractObjectType.Spear)
                            {
                                abObj = new AbstractSpear(room.world, null, room.GetWorldCoordinate(bindTile), room.game.GetNewID(), special == 1)
                                {
                                    electric = special == 2
                                };
                            }
                            else
                                abObj = new AbstractPhysicalObject(room.world, generateType, null, room.GetWorldCoordinate(bindTile), room.game.GetNewID());

                            room.abstractRoom.AddEntity(abObj);
                            abObj.RealizeInRoom();
                        }
                        catch (Exception ex)
                        {
                            BuffUtils.Log($"UltraKillMission", $"Spawn {generateType} exception");
                            BuffUtils.Log($"UltraKillMission", $"{ex}");
                        }

                        Finished = true;
                        Destroy();
                    }
                }
            }
        }

        internal class WaveGenEffect : CosmeticSprite
        {
            static int extraExistLife = 40;
            int initWait;
            Color color;
            int life, lastLife;
            float scale;

            public WaveGenEffect(Room room, Color color, IntVector2 tile, int initWait, float scale)
            {
                this.color = color;
                this.initWait = initWait;
                pos = room.MiddleOfTile(tile) + Vector2.down * 10f;
                this.scale = scale;
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[2];
                sLeaser.sprites[0] = new FSprite(BuffUIAssets.ConicalLightOpaque400, true)
                {
                    rotation = 180f,
                    anchorY = 1f,
                    color = color,
                    alpha = 0f,
                    shader = Custom.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"]
                };
                sLeaser.sprites[1] = new FSprite(BuffUIAssets.ConicalLightOpaque400, true)
                {
                    rotation = 180f,
                    color = Color.white,
                    anchorY = 1f,
                    alpha = 0f,
                    shader = Custom.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"]
                };
                AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Water"));
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                if (slatedForDeletetion)
                    return;

                lastLife = life;
                life++;
                if (life > initWait + extraExistLife)
                    Destroy();
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                if (!sLeaser.deleteMeNextFrame && (slatedForDeletetion || room != rCam.room))
                {
                    return;
                }

                float l = Mathf.Lerp(lastLife, life, timeStacker);
                float f1 = Mathf.Clamp01(Mathf.Pow(l / initWait, 1.5f));
                float powF1 = Mathf.Pow(f1, 3f);
                float f2 = 0f;
                float powF2 = 0f;
                if (l > initWait)
                {
                    f2 = (l - initWait) / extraExistLife;
                    powF2 = Mathf.Pow(f2, 3f);
                }

                sLeaser.sprites[0].scaleX = (Mathf.Lerp(0f, 0.5f, f1) - Mathf.Lerp(0f, 0.5f, f2)) * scale;
                sLeaser.sprites[0].scaleY = (Mathf.Lerp(0f, 0.5f, f1) + Mathf.Lerp(0f, 0.5f, f2)) * scale;
                sLeaser.sprites[0].alpha = (f1 - f2) * 0.5f;
                sLeaser.sprites[0].SetPosition(pos - camPos);

                sLeaser.sprites[1].scaleX = (Mathf.Lerp(0f, 0.5f, powF1) - Mathf.Lerp(0f, 0.5f, f2)) * scale * 0.8f;
                sLeaser.sprites[1].scaleY = (Mathf.Lerp(0f, 0.5f, powF1) + Mathf.Lerp(0f, 0.5f, f2)) * scale * 1.1f;
                sLeaser.sprites[1].alpha = (powF1 - powF2) * 0.5f;
                sLeaser.sprites[1].SetPosition(pos - camPos);
            }
        }


        public static UltraKillManager Get(Room room)
        {
            foreach(var u in room.updateList)
            {
                if (u is UltraKillManager manager)
                    return manager;
            }
            return null;
        }
    }

    internal class UltraKillPlayerRevulver
    {
        UltraKillMissionHUD hud;
        UltraKillManager manager;

        List<AbstractCreature> playerToRevulv = new List<AbstractCreature>();

        float blood = 1f;
        float animBlood = 1f;

        bool CanRevulv => blood >= 1f;
        int revulvDelayCounter;

        public UltraKillPlayerRevulver(UltraKillManager manager, UltraKillMissionHUD hud)
        {
            this.manager = manager;
            this.hud = hud;
            On.Player.Die += Player_Die;
            On.Player.Update += Player_Update;
            On.Creature.Violence += Creature_Violence;
            On.Lizard.Violence += Lizard_Violence;
        }

        private void Lizard_Violence(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
        {
            float health = (self.State as HealthState).health;
            bool createBlood = !self.dead;

            orig.Invoke(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);

            if(createBlood)
                CreateBlood(self.room, hitChunk.pos, health - (self.State as HealthState).health, directionAndMomentum);
        }

        private void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            float health = 0f;
            bool createBlood = false;
            HealthState healthState = null;
            if (!self.dead && !(self is Player) && !self.dead && hitChunk != null && self.State is HealthState)
            {
                healthState = (HealthState)self.State;
                health = healthState.health;
                createBlood = true;
            }
                
            orig.Invoke(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
            if (createBlood && healthState != null)
            {
                CreateBlood(self.room, hitChunk.pos, health - healthState.health, directionAndMomentum);
            }
        }

        void CreateBlood(Room room, Vector2 pos, float damage, Vector2? directionAndMomentum)
        {
            int bloodCount = Mathf.Max(1, (int)damage) * 4;
            for(int i = 0;i < bloodCount; i++)
            {
                room.AddObject(new BloodEnergy(room, pos, (Custom.RNV() + Vector2.up) * Mathf.Lerp(5f, 10f, Random.value) + -directionAndMomentum ?? Vector2.up * 4, damage / bloodCount));
            }
        }

        private void Player_Die(On.Player.orig_Die orig, Player self)
        {
            orig.Invoke(self);
            if(CanRevulv)
                RevulvPlayer(self.firstChunk.pos);
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (self.dangerGraspTime > 80 && CanRevulv)
            {
                RevulvPlayer(self.firstChunk.pos);
            }
        }

        public void AddEnergy(float energy)
        {
            blood = Mathf.Clamp(blood + energy, 0f, 3f);
        }

        public void RevulvPlayer(Vector2 pos)
        {
            foreach (var update in manager.room.updateList)
            {
                if (update is Player player)
                {
                    player.dead = false;
                    player.playerState.alive = true;
                    player.playerState.permaDead = false;
                    player.aerobicLevel = 0f;
                    playerToRevulv.Add(player.abstractCreature);
                    player.abstractCreature.Abstractize(manager.room.GetWorldCoordinate(player.DangerPos));
                    player.slatedForDeletetion = true;
                }
                if (update is Explosion)
                    update.Destroy();
            }

            manager.room.game.cameras[0].hud.textPrompt.gameOverMode = false;
            manager.room.AddObject(new ShockWave(pos, 1400f, 0.15f, 80, false));

            manager.ClearStage();
            manager.RollBack();
            manager.InitNextWave();

            blood -= 1f;
            hud.BloodFlash(1f);

            manager.room.PlaySound(SoundID.Firecracker_Bang, 0f, 1f, 0.75f + UnityEngine.Random.value);
            manager.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, 0f, 1f, 0.5f + UnityEngine.Random.value * 0.5f);
        }

        public void Update()
        {
            //blood = Mathf.Clamp(blood + 1 / 320f,0f,3f);
            animBlood = Mathf.Lerp(animBlood, blood, 0.15f);
            hud.blood = animBlood;

            if (revulvDelayCounter > 0)
                revulvDelayCounter--;
            else if(playerToRevulv.Count > 0)
            {
                for (int i = playerToRevulv.Count - 1; i >= 0; i--)
                {
                    var bindAbPlayer = playerToRevulv[i];

                    //bindAbPlayer.pos = room.GetWorldCoordinate(endPosTile);
                    bindAbPlayer.RealizeInRoom();
                    if (!bindAbPlayer.Room.world.game.AlivePlayers.Contains(bindAbPlayer))
                        bindAbPlayer.Room.world.game.AlivePlayers.Add(bindAbPlayer);


                    if (!bindAbPlayer.Room.creatures.Contains(bindAbPlayer))
                        bindAbPlayer.Room.AddEntity(bindAbPlayer);

                    if (!bindAbPlayer.Room.realizedRoom.updateList.Contains(bindAbPlayer.realizedCreature))
                        bindAbPlayer.Room.realizedRoom.AddObject(bindAbPlayer.realizedCreature);

                    bindAbPlayer.realizedCreature.deaf = 0;
                    if (bindAbPlayer.realizedCreature.dead)
                        bindAbPlayer.realizedCreature.dead = false;
                    if ((bindAbPlayer.realizedCreature as Player).playerState.dead)
                        (bindAbPlayer.realizedCreature as Player).playerState.alive = true;
                    if ((bindAbPlayer.realizedCreature as Player).aerobicLevel > 0f)
                        (bindAbPlayer.realizedCreature as Player).aerobicLevel = 0f;
                    if ((bindAbPlayer.realizedCreature as Player).playerState.permanentDamageTracking > 0f)
                        (bindAbPlayer.realizedCreature as Player).playerState.permanentDamageTracking = 0f;

                    if (bindAbPlayer.Room.realizedRoom.game.cameras[0].hud.textPrompt.gameOverMode)
                    {
                        bindAbPlayer.Room.realizedRoom.game.cameras[0].hud.textPrompt.gameOverMode = false;
                        bindAbPlayer.Room.realizedRoom.game.cameras[0].hud.textPrompt.dependentOnGrasp = null;
                    }
                    if (bindAbPlayer.Room.realizedRoom.game.cameras[0].hud.owner == null || (bindAbPlayer.Room.realizedRoom.game.cameras[0].hud.owner is Player player && player.slatedForDeletetion))
                    {
                        bindAbPlayer.Room.realizedRoom.game.cameras[0].hud.owner = bindAbPlayer.realizedCreature as Player;
                    }
                    bindAbPlayer.realizedCreature.slatedForDeletetion = false;
                    playerToRevulv.RemoveAt(i);
                }
            }
        }

        public void Destroy()
        {
            On.Player.Die -= Player_Die;
            On.Player.Update -= Player_Update;
            On.Creature.Violence -= Creature_Violence;
            On.Lizard.Violence -= Lizard_Violence;
        }

    }

    internal static class UltraKillWave
    {
        static readonly string[] waveType = new string[] { "Wait", "SpawnItems", "SpawnCreatures", "ClearStage", "ExtraBuff", "Finish", "Song" };
        public static List<WaveInfo> waves = new List<WaveInfo>();
        public static readonly IntVector2[] spawnEnemyPos = new IntVector2[]
        {
            new IntVector2(10, 35),//0
            new IntVector2(27, 34),
            new IntVector2(61, 34),//2
            new IntVector2(10, 25),
            new IntVector2(46, 29),//4
            new IntVector2(23, 21),
            new IntVector2(61, 20),//6
            new IntVector2(20, 13),
            new IntVector2(54, 13),//8
            new IntVector2(20, 4),
            new IntVector2(52, 4)//10
        };
        public static readonly IntVector2[] spawnItemPos = new IntVector2[]
        {
            new IntVector2(9, 13),
            new IntVector2(17, 13),
            new IntVector2(25, 13),//2
            new IntVector2(36, 13),
            new IntVector2(45 , 13),
            new IntVector2(51, 13),
            new IntVector2(60, 13),//6

            new IntVector2(10, 35),//7
            new IntVector2(27, 34),
            new IntVector2(61, 34),//9
            new IntVector2(10, 25),
            new IntVector2(46, 29),//11
            new IntVector2(23, 21),
            new IntVector2(61, 20),//13
            new IntVector2(9, 6),
            new IntVector2(27, 4),//15
            new IntVector2(47, 4),
            new IntVector2(64, 6)//17
        };
        public static readonly string[] BattleSongs = new string[]
        {
            "UnstoppableForce",
            "CastleVein",
            "Versus"
        };
        public static void InitWaveInfos()
        {
            foreach(var song in BattleSongs)
            {
                BuffScene.PreloadTrack($"BUFF_{UltraCoinsBuffEntry.ultraCoinsBuffID.GetStaticData().AssetPath}/{song}");
            }

            string path = AssetManager.ResolveFilePath(UltraCoinsBuffEntry.ultraCoinsBuffID.GetStaticData().AssetPath + $"{Path.DirectorySeparatorChar}UltraKillWaves.txt");
            var lines = File.ReadAllLines(path);
            //CreatureTemplate.Type a = CreatureTemplate.Type.YellowLizard;
            //var b = AbstractPhysicalObject.AbstractObjectType.ScavengerBomb;

            WaveInfo currentWaveInfo = null;
            string currentWaveInfoType = string.Empty;

            foreach (var line in lines)
            {
                if (line.StartsWith("//") || string.IsNullOrEmpty(line))
                    continue;

                try
                {
                    var trimed = line.Trim();

                    if (waveType.Contains(trimed))
                    {
                        currentWaveInfoType = trimed;

                        if (currentWaveInfoType == "Wait")
                            waves.Add(currentWaveInfo = new WaitInfo());
                        else if (currentWaveInfoType == "SpawnItems")
                            waves.Add(currentWaveInfo = new SpawnItemInfo());
                        else if (currentWaveInfoType == "SpawnCreatures")
                            waves.Add(currentWaveInfo = new SpawnCreatureInfo());
                        else if (currentWaveInfoType == "ClearStage")
                            waves.Add(currentWaveInfo = new ClearStageInfo());
                        else if (currentWaveInfoType == "ExtraBuff")
                            waves.Add(currentWaveInfo = new ExtraBuffInfo());
                        else if (currentWaveInfoType == "Finish")
                            waves.Add(currentWaveInfo = new FinishInfo());
                        else if (currentWaveInfoType == "Song")
                            waves.Add(currentWaveInfo = new SongInfo());

                        BuffUtils.Log("UltraKillMission", $"new waveinfo {currentWaveInfoType}");
                        continue;
                    }
                    else
                    {
                        if (currentWaveInfoType == "Wait")
                        {
                            (currentWaveInfo as WaitInfo).waitTime = int.Parse(trimed);
                            BuffUtils.Log("UltraKillMission", $"WaitInfo {(currentWaveInfo as WaitInfo).waitTime}");
                        }
                        else if (currentWaveInfoType == "SpawnItems")
                        {
                            var itemInfo = currentWaveInfo as SpawnItemInfo;

                            string[] splited = trimed.Split(':');
                            int index = int.Parse(splited[0].Trim());

                            if (!itemInfo.spawns.TryGetValue(index, out var lst))
                            {
                                itemInfo.spawns.Add(index, lst = new List<AbstractPhysicalObject.AbstractObjectType>());
                                itemInfo.specials.Add(index, new List<int>());
                            }

                            lst.Add(new AbstractPhysicalObject.AbstractObjectType(splited[1].Trim()));

                            if (splited.Length > 2)
                            {
                                itemInfo.specials[index].Add(int.Parse(splited[2].Trim()));
                            }
                            else
                                itemInfo.specials[index].Add(0);

                            BuffUtils.Log("UltraKillMission", $"SpawnItems {index} {itemInfo.spawns[index].Last()} {itemInfo.specials[index].Last()}");
                        }
                        else if (currentWaveInfoType == "SpawnCreatures")
                        {
                            var creatureInfo = currentWaveInfo as SpawnCreatureInfo;

                            string[] splited = trimed.Split(':');
                            int index = int.Parse(splited[0].Trim());

                            if (!creatureInfo.spawns.TryGetValue(index, out var lst))
                            {
                                creatureInfo.spawns.Add(index, lst = new List<CreatureTemplate.Type>());
                                creatureInfo.specials.Add(index, new List<int>());
                            }

                            lst.Add(new CreatureTemplate.Type(splited[1].Trim()));

                            if (splited.Length > 2)
                            {
                                creatureInfo.specials[index].Add(int.Parse(splited[2].Trim()));
                            }
                            else
                                creatureInfo.specials[index].Add(0);

                            BuffUtils.Log("UltraKillMission", $"SpawnCreatures {index} {creatureInfo.spawns[index].Last()}");
                        }
                        else if (currentWaveInfoType == "ExtraBuff")
                        {
                            var buffInfo = currentWaveInfo as ExtraBuffInfo;

                            string[] splited = trimed.Split(':');

                            buffInfo.extraBuffs.Add(new BuffID(splited[0].Trim()));
                            if (splited.Length > 1)
                            {
                                buffInfo.stackCount.Add(int.Parse(splited[1].Trim()));
                            }
                            else
                                buffInfo.stackCount.Add(1);
                        }
                        else if(currentWaveInfoType == "Song")
                        {
                            var songInfo = currentWaveInfo as SongInfo;
                            songInfo.songName.Add(trimed);
                            BuffUtils.Log("UltraKillMission", $"song : {trimed}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    BuffUtils.Log("UltraKillMission", $"Load wave info exception, line : {line}");
                    BuffUtils.Log("UltraKillMission", $"{ex}");
                }
            }
        }


        public class WaveInfo { }

        public class WaitInfo : WaveInfo
        {
            public int waitTime;
        }

        public class ClearStageInfo : WaveInfo { }

        public class FinishInfo : WaveInfo { }

        public class SpawnCreatureInfo : WaveInfo
        {
            public Dictionary<int, List<CreatureTemplate.Type>> spawns = new Dictionary<int, List<CreatureTemplate.Type>>();
            public Dictionary<int, List<int>> specials = new Dictionary<int, List<int>>();
        }

        public class SpawnItemInfo : WaveInfo
        {
            public Dictionary<int, List<AbstractPhysicalObject.AbstractObjectType>> spawns = new Dictionary<int, List<AbstractPhysicalObject.AbstractObjectType>>();
            public Dictionary<int, List<int>> specials = new Dictionary<int, List<int>>();
        }

        public class ExtraBuffInfo : WaveInfo
        {
            public List<BuffID> extraBuffs = new List<BuffID>();
            public List<int> stackCount = new List<int>();
        }

        public class SongInfo : WaveInfo
        {
            public List<string> songName = new List<string>();
        }
    }
}
