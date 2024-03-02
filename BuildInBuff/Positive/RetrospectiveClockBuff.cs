using HUD;
using Menu;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace BuiltinBuffs.Positive {
    internal class RetrospectiveClockBuff : Buff<RetrospectiveClockBuff, RetrospectiveClockBuffData>
    {
        public override BuffID ID => RetrospectiveClockBuffEntry.retrospectiveClockBuffID;

        public override bool Triggerable => !triggerd;
        public override bool Active => record && !triggerd;

        bool record;
        bool triggerd;
        bool triggerdFinish;

        public RegionSwitcher regionSwitcher = new RegionSwitcher();

        public PlayerRecord playerRecord;
        bool playerRecordFinished;

        public CycleRecord cycleRecord;
        bool cycleRecordFinished;

        CreatureRecord[] creatureRecords;
        Dictionary<EntityID, CreatureRecord> creatureRecordMapper;

        bool creatureRecordsAllFinished;

        List<AbstractCreature> creaturesInRegion;
        List<CreatureRecord> normalRecords = new List<CreatureRecord>();
        List<CreatureRecord> missingCreatureRecords = new List<CreatureRecord>();

        public override bool Trigger(RainWorldGame game)
        {
            if (!record)
            {
                playerRecord = new PlayerRecord();
                playerRecord.Record(game, game.Players[0].realizedCreature as Player);

                cycleRecord = new CycleRecord();
                cycleRecord.Record(game, game.world.rainCycle);
                record = true;

                var creatures = GetAllCeatures(game);
                creatureRecords = new CreatureRecord[creatures.Length];
                creatureRecordMapper = new Dictionary<EntityID, CreatureRecord>();

                for (int i = 0; i < creatureRecords.Length; i++)
                {
                    creatureRecords[i] = new CreatureRecord();
                    creatureRecords[i].Record(game, creatures[i]);
                    creatureRecordMapper.Add(creatureRecords[i].idRecord, creatureRecords[i]);
                }


                foreach (var pair in creatureRecordMapper)
                {
                    BuffUtils.Log("RetrospectiveClock", $"{pair.Key} - {pair.Value.typeRecord}");
                }
            }
            else
            {
                triggerd = true;
            }


            return false;
        }

        public override void Update(RainWorldGame game)
        {
            base.Update(game);

            if(triggerd && !triggerdFinish)
            {
                if (!playerRecordFinished)
                {
                    playerRecordFinished = playerRecord.RecoverUpdate(game, game.Players[0].realizedCreature as Player, false);
                    if (playerRecordFinished)
                    {
                        creaturesInRegion = GetAbstractCreaturesList(game);//获取区域内所有生物，并且删除未记录的生物（除了玩家）
                        for (int i = creaturesInRegion.Count - 1; i >= 0; i--)
                        {
                            if (game.Players.Contains(creaturesInRegion[i]))
                                continue;

                            if (creatureRecordMapper.TryGetValue(creaturesInRegion[i].ID, out var record))
                            {
                                normalRecords.Add(record);
                            }
                            else
                            {
                                BuffUtils.Log("RetrospectiveClock", $"Delete {creaturesInRegion[i]} for missing record");
                                creaturesInRegion[i].Destroy();
                                creaturesInRegion.RemoveAt(i);
                            }
                        }

                        foreach(var record in creatureRecords)
                        {
                            if(!normalRecords.Contains(record))
                            {
                                BuffUtils.Log("RetrospectiveClock", $"Record {record.typeRecord} {record.idRecord} has no creature matched");
                                missingCreatureRecords.Add(record);
                            }
                        }
                    }
                }
                if (!cycleRecordFinished)
                {
                    cycleRecordFinished = cycleRecord.RecoverUpdate(game, game.world.rainCycle, false);
                }

                if (playerRecordFinished)
                {
                    foreach(var creature in creaturesInRegion)
                    {

                        if(creatureRecordMapper.TryGetValue(creature.ID, out var record))
                        {
                            if (record.RecoverUpdate(game, creature, false))
                            {
                                creatureRecordMapper.Remove(creature.ID);
                            }
                        }
                        else
                        {
                            BuffUtils.Log("RetrospectiveClock", $"{creature} missing record!");
                        }
                    }

                    for(int i = missingCreatureRecords.Count - 1; i >= 0; i--)
                    {
                        if (missingCreatureRecords[i].RecoverUpdate(game, null, true))
                            missingCreatureRecords.RemoveAt(i);
                    }

                    if (creatureRecordMapper.Count == 0 && missingCreatureRecords.Count == 0)
                        creatureRecordsAllFinished = true;
                }

                if (cycleRecordFinished && playerRecordFinished && creatureRecordsAllFinished)
                {
                    triggerdFinish = true;
                    TriggerSelf(true);
                }
            }
        }

        //排除玩家
        public static List<AbstractCreature> GetAbstractCreaturesList(RainWorldGame game)
        {
            List<AbstractCreature> creatures = new List<AbstractCreature>();
            foreach (var abRoom in game.world.abstractRooms)
            {
                foreach (var creature in abRoom.creatures)
                {
                    if (game.Players.Contains(creature))
                        continue;
                    creatures.Add(creature);
                }

                if(abRoom.realizedRoom != null)
                {
                    TryAddCreature(abRoom.realizedRoom.updateList.Where((i) => i is Creature).Select((i) => i as Creature));
                }
            }

            TryAddCreature(game.shortcuts.transportVessels.Select((i) => i.creature));
            TryAddCreature(game.shortcuts.betweenRoomsWaitingLobby.Select((i) => i.creature));
            TryAddCreature(game.shortcuts.borderTravelVessels.Select((i) => i.creature));

            void TryAddCreature(IEnumerable<Creature> crits)
            {
                foreach (var creature in crits)
                {
                    if (creature is Player)
                        continue;
                    if (creatures.Contains(creature.abstractCreature))
                        continue;
                    creatures.Add(creature.abstractCreature);
                }
            }

            return creatures;
        }

        public static AbstractCreature[] GetAllCeatures(RainWorldGame game)
        {
            return GetAbstractCreaturesList(game).ToArray();
        }
    }

    internal class RetrospectiveClockBuffData : BuffData
    {
        public override BuffID ID => RetrospectiveClockBuffEntry.retrospectiveClockBuffID;
    }

    internal class RetrospectiveClockBuffEntry : IBuffEntry
    {
        public static BuffID retrospectiveClockBuffID = new BuffID("RetrospectiveClock", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<RetrospectiveClockBuff, RetrospectiveClockBuffData, RetrospectiveClockBuffEntry>(retrospectiveClockBuffID);
        }
    }

    abstract class RecordData<T>
    {
        public Type RecordType { get => typeof(T); }
        bool recovered;

        public abstract void Record(RainWorldGame game, T obj);
        public virtual bool RecoverUpdate(RainWorldGame game, T obj, bool recoverAsLost)
        {
            recovered = true;
            return true;
        }
    }

    class CreatureRecord : RecordData<AbstractCreature>
    {
        public EntityID idRecord;
        public CreatureTemplate.Type typeRecord;
        WorldCoordinate coordRecord;
        string stateRecord;

        public override void Record(RainWorldGame game, AbstractCreature obj)
        {
            idRecord = obj.ID;
            typeRecord = obj.creatureTemplate.type;
            coordRecord = obj.pos;
            stateRecord = obj.state.ToString();

            BuffUtils.Log("RetrospectiveClock", $"Record creature : id {idRecord}, coordRecord : {coordRecord}, typeRecord : {typeRecord}, state : {stateRecord}");
        }

        public override bool RecoverUpdate(RainWorldGame game, AbstractCreature obj, bool recoverAsLost)
        {
            base.RecoverUpdate(game, obj, recoverAsLost);
            if(recoverAsLost)
            {
                var abRoom = game.world.GetAbstractRoom(coordRecord.room);
                AbstractCreature abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate(typeRecord), null, coordRecord, idRecord);
                abstractCreature.state.LoadFromString(Regex.Split(stateRecord, "<cB>"));
                abRoom.AddEntity(abstractCreature);
            }
            else
            {
                if(obj.realizedCreature != null)
                {
                    KickOutofShortcut(game, obj.realizedCreature);
                    obj.Abstractize(obj.pos);
                }

                obj.Move(coordRecord);
                var abRoom = game.world.GetAbstractRoom(coordRecord.room);
                if(abRoom.realizedRoom != null)
                {
                    obj.RealizeInRoom();
                }
            }
            return true;

            //需要完善效果
            void KickOutofShortcut(RainWorldGame game1, Creature creature)
            {
                for (int i = game1.shortcuts.transportVessels.Count - 1; i >= 0; i--)
                {
                    if (game1.shortcuts.transportVessels[i].creature == creature)
                        game1.shortcuts.transportVessels.RemoveAt(i);
                }
                for (int i = game1.shortcuts.betweenRoomsWaitingLobby.Count - 1; i >= 0; i--)
                {
                    if (game1.shortcuts.betweenRoomsWaitingLobby[i].creature == creature)
                        game1.shortcuts.betweenRoomsWaitingLobby.RemoveAt(i);
                }
                for (int i = game1.shortcuts.borderTravelVessels.Count - 1; i >= 0; i--)
                {
                    if (game1.shortcuts.borderTravelVessels[i].creature == creature)
                        game1.shortcuts.borderTravelVessels.RemoveAt(i);
                }
                creature.enteringShortCut = null;
                creature.shortcutDelay = 40;
            }
        }
    }

    class PlayerRecord : RecordData<Player>
    {
        IntVector2 posRecord;
        string roomRecord;
        string regionRecord;
        string stateRecord;

        AbstractRoom switchRoom;

        public override void Record(RainWorldGame game, Player obj)
        {
            posRecord = obj.abstractCreature.pos.Tile;
            stateRecord = obj.playerState.ToString();
            roomRecord = obj.room.abstractRoom.name;
            regionRecord = obj.room.world.region.name;

            BuffUtils.Log("RetrospectiveClock", $"Record player : pos {posRecord}, room : {roomRecord}, region : {regionRecord}, state : {stateRecord}");
        }

        public override bool RecoverUpdate(RainWorldGame game, Player obj, bool recoverAsLost)
        {
            base.RecoverUpdate(game, obj, recoverAsLost);
            if (!recoverAsLost)
            {
                if(game.world.region.name != regionRecord)
                {
                    RetrospectiveClockBuff.Instance.regionSwitcher.SwitchRegions(game, regionRecord, roomRecord, posRecord);
                    return true;
                }
                if(switchRoom == null)
                {
                    if (roomRecord != obj.room.abstractRoom.name)
                    {
                        obj.room.abstractRoom.RemoveEntity(obj.abstractCreature);
                        foreach (var room in game.world.abstractRooms)
                        {
                            if (room.name == roomRecord)
                            {
                                switchRoom = room;
                                switchRoom.RealizeRoom(game.world, game);
                                return false;
                            }
                        }
                    }
                    else//无需切换房间
                        return true;
                }
                else
                {
                    if (switchRoom.realizedRoom == null || !switchRoom.realizedRoom.ReadyForPlayer)
                        return false;

                    if (obj.grasps != null)
                    {
                        for (int g = 0; g < obj.grasps.Length; g++)
                        {
                            if (obj.grasps[g] != null && obj.grasps[g].grabbed != null && !obj.grasps[g].discontinued && obj.grasps[g].grabbed is Creature)
                            {
                                obj.ReleaseGrasp(g);
                            }
                        }
                    }
                    obj.abstractCreature.Move(switchRoom.realizedRoom.GetWorldCoordinate(posRecord));
                    obj.PlaceInRoom(switchRoom.realizedRoom);
                    obj.abstractCreature.pos = switchRoom.realizedRoom.GetWorldCoordinate(posRecord);
                    return true;
                }
            }
            return true;
        }
    }

    class CycleRecord : RecordData<RainCycle>
    {
        int timerRecord;
        int pauseRecord;
        int preTimerRecord;
        int dayNightCounterRecord;

        float preCycleRainPulse_WaveARecord;
        float preCycleRainPulse_WaveBRecord;
        float preCycleRainPulse_WaveCRecord;

        bool deathRainHasHitRecord;

        public override void Record(RainWorldGame game, RainCycle obj)
        {
            timerRecord = obj.timer;
            pauseRecord = obj.pause;
            preTimerRecord = obj.preTimer;
            dayNightCounterRecord = obj.dayNightCounter;

            preCycleRainPulse_WaveARecord = obj.preCycleRainPulse_WaveA;
            preCycleRainPulse_WaveBRecord = obj.preCycleRainPulse_WaveB;
            preCycleRainPulse_WaveCRecord = obj.preCycleRainPulse_WaveC;

            deathRainHasHitRecord = obj.deathRainHasHit;
        }

        public override bool RecoverUpdate(RainWorldGame game, RainCycle obj, bool recoverAsLost)
        {
            obj.timer = timerRecord;
            obj.pause = pauseRecord;
            obj.preTimer = preTimerRecord;
            obj.dayNightCounter = dayNightCounterRecord;

            obj.preCycleRainPulse_WaveA = preCycleRainPulse_WaveARecord;
            obj.preCycleRainPulse_WaveB = preCycleRainPulse_WaveBRecord;
            obj.preCycleRainPulse_WaveC = preCycleRainPulse_WaveCRecord;

            obj.deathRainHasHit = deathRainHasHitRecord;

            var globalRain = game.globalRain;
            if (globalRain.deathRain != null)
                globalRain.deathRain.globalRain = null;
            globalRain.deathRain = null;
            globalRain.forceSlowFlood = false;
            globalRain.RumbleSound = 0f;
            globalRain.MicroScreenShake = 0f;
            globalRain.ScreenShake = 0f;
            globalRain.ShaderLight = -1f;
            globalRain.floodSpeed = 0f;
            globalRain.drainWorldFlood = 0f;

            var hud = game.cameras[0].hud;
            foreach (var meter in hud.rainMeter.circles)
                meter.ClearSprite();

            hud.parts.Remove(hud.rainMeter);
            hud.rainMeter = null;
            hud.AddPart(new RainMeter(hud, hud.fContainers[1]));//切换区域可能导致的错误
            hud.rainMeter.remainVisibleCounter = 120;

            return base.RecoverUpdate(game, obj, recoverAsLost);
        }
    }

    //RegionSwitcher from wrap mod，huge thanks!
    public class RegionSwitcher
    {
        public void SwitchRegions(RainWorldGame game, string destWorld, string destRoom, IntVector2 destPos)
        {
            error = RegionSwitcher.ErrorKey.LoadWorld;
            BuffUtils.Log("RetrospectiveClock", string.Concat(new string[] { "WARP: Loading room ", destRoom, " from region ", destWorld, "!" }));
            for (int i = 0; i < game.Players.Count; i++)
            {
                AbstractCreature absPly = game.Players[i];
                if (absPly != null)
                {
                    BuffUtils.Log("RetrospectiveClock", "WARP: Initiating region warp.");
                    AbstractRoom oldRoom = absPly.Room;
                    try
                    {
                        BuffUtils.Log("RetrospectiveClock", "WARP: Invoking original LoadWorld method for new region.");
                        World oldWorld = game.overWorld.activeWorld;
                        this._OverWorld_LoadWorld.Invoke(game.overWorld, new object[]
                        {
                        destWorld,
                        game.overWorld.PlayerCharacterNumber,
                        false
                        });
                        BuffUtils.Log("RetrospectiveClock", "WARP: Moving player to new region.");
                        WorldLoaded(game, oldRoom, oldWorld, destRoom, destPos);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        BuffUtils.LogError("RetrospectiveClock", "WARP ERROR: " + GetErrorText(error));
                        game.pauseMenu = new PauseMenu(game.manager, game);
                        break;
                    }
                }
            }
        }

        public AbstractRoom GetFirstRoom(AbstractRoom[] abstractRooms, string regionName)
        {
            for (int i = 0; i < abstractRooms.Length; i++)
            {
                if (abstractRooms[i].name.StartsWith(regionName))
                {
                    return abstractRooms[i];
                }
            }
            return null;
        }

        private void WorldLoaded(RainWorldGame game, AbstractRoom oldRoom, World oldWorld, string newRoomName, IntVector2 newPos)
        {
            this.error = RegionSwitcher.ErrorKey.AbstractRoom;
            World newWorld = game.overWorld.activeWorld;
            AbstractRoom newRoom = newWorld.GetAbstractRoom(newRoomName);
            this.error = RegionSwitcher.ErrorKey.RealiseRoom;
            newRoom.RealizeRoom(newWorld, game);
            this.error = RegionSwitcher.ErrorKey.RoomRealiser;
            while (newWorld.loadingRooms.Count > 0)
            {
                for (int i = 0; i < 1; i++)
                {
                    for (int j = newWorld.loadingRooms.Count - 1; j >= 0; j--)
                    {
                        if (newWorld.loadingRooms[j].done)
                        {
                            newWorld.loadingRooms.RemoveAt(j);
                        }
                        else
                        {
                            newWorld.loadingRooms[j].Update();
                        }
                    }
                }
            }
            if (game.roomRealizer != null)
            {
                game.roomRealizer = new RoomRealizer(game.roomRealizer.followCreature, newWorld);
            }
            game.overWorld.activeWorld = newWorld;
            this.error = RegionSwitcher.ErrorKey.FindNode;
            int abstractNode = 0;
            for (int k = 0; k < newRoom.nodes.Length; k++)
            {
                if (newRoom.nodes[k].type == AbstractRoomNode.Type.Exit && k < newRoom.connections.Length && newRoom.connections[k] > -1)
                {
                    abstractNode = k;
                    break;
                }
            }
            game.cameras[0].virtualMicrophone.AllQuiet();
            for (int l = 0; l < game.cameras[0].hud.fContainers.Length; l++)
            {
                game.cameras[0].hud.fContainers[l].RemoveAllChildren();
            }
            game.cameras[0].hud = null;
            for (int m = 0; m < game.Players.Count; m++)
            {
                error = RegionSwitcher.ErrorKey.MovePlayer;
                AbstractCreature ply = game.Players[m];
                if (ply.realizedCreature.grasps != null)
                {
                    for (int g = 0; g < ply.realizedCreature.grasps.Length; g++)
                    {
                        if (ply.realizedCreature.grasps[g] != null && ply.realizedCreature.grasps[g].grabbed != null && !ply.realizedCreature.grasps[g].discontinued && ply.realizedCreature.grasps[g].grabbed is Creature)
                        {
                            ply.realizedCreature.ReleaseGrasp(g);
                        }
                    }
                }
                ply.world = newWorld;
                ply.pos.room = newRoom.index;
                ply.pos.abstractNode = abstractNode;
                ply.pos.x = newPos.x;
                ply.pos.y = newPos.y;
                if (m == 0)
                {
                    newRoom.realizedRoom.aimap.NewWorld(newRoom.index);
                }
                if (ply.realizedObject is Player)
                {
                    (ply.realizedObject as Player).enteringShortCut = null;
                }
                List<AbstractPhysicalObject> objs = ply.GetAllConnectedObjects();
                for (int n = 0; n < objs.Count; n++)
                {
                    objs[n].world = newWorld;
                    objs[n].pos = newRoom.realizedRoom.GetWorldCoordinate(newPos);
                    objs[n].Room.RemoveEntity(objs[n]);
                    newRoom.AddEntity(objs[n]);
                    objs[n].realizedObject.sticksRespawned = true;
                }
                Spear hasSpear = null;
                AbstractPhysicalObject stomachObject = null;
                if (ply.realizedCreature != null && (ply.realizedCreature as Player).objectInStomach != null)
                {
                    (ply.realizedCreature as Player).objectInStomach.world = newWorld;
                    stomachObject = (ply.realizedCreature as Player).objectInStomach;
                }
                if (ply.realizedCreature != null && (ply.realizedCreature as Player).spearOnBack != null && (ply.realizedCreature as Player).spearOnBack.spear != null)
                {
                    hasSpear = (ply.realizedCreature as Player).spearOnBack.spear;
                }
                ply.timeSpentHere = 0;
                ply.distanceToMyNode = 0;
                oldRoom.realizedRoom.RemoveObject(ply.realizedCreature);
                ply.Move(newRoom.realizedRoom.GetWorldCoordinate(newPos));
                if (ply.creatureTemplate.AI && ply.abstractAI.RealAI != null && ply.abstractAI.RealAI.pathFinder != null)
                {
                    ply.abstractAI.SetDestination(QuickConnectivity.DefineNodeOfLocalCoordinate(ply.abstractAI.destination, ply.world, ply.creatureTemplate));
                    ply.abstractAI.timeBuffer = 0;
                    if (ply.abstractAI.destination.room == ply.pos.room && ply.abstractAI.destination.abstractNode == ply.pos.abstractNode)
                    {
                        ply.abstractAI.path.Clear();
                    }
                    else
                    {
                        List<WorldCoordinate> list = ply.abstractAI.RealAI.pathFinder.CreatePathForAbstractreature(ply.abstractAI.destination);
                        if (list != null)
                        {
                            ply.abstractAI.path = list;
                        }
                        else
                        {
                            ply.abstractAI.FindPath(ply.abstractAI.destination);
                        }
                    }
                    ply.abstractAI.RealAI = null;
                }
                ply.RealizeInRoom();
                if (m == 0)
                {
                    for (int i2 = 0; i2 < objs.Count; i2++)
                    {
                        int num = 0;
                        for (int s = 0; s < newRoom.realizedRoom.updateList.Count; s++)
                        {
                            if (objs[i2].realizedObject == newRoom.realizedRoom.updateList[s])
                            {
                                num++;
                            }
                            if (num > 1)
                            {
                                newRoom.realizedRoom.updateList.RemoveAt(s);
                            }
                        }
                    }
                }
                this.error = RegionSwitcher.ErrorKey.MoveObjects;
                if (hasSpear != null && (ply.realizedCreature as Player).spearOnBack != null && (ply.realizedCreature as Player).spearOnBack.spear != hasSpear)
                {
                    (ply.realizedCreature as Player).spearOnBack.SpearToBack(hasSpear);
                    (ply.realizedCreature as Player).abstractPhysicalObject.stuckObjects.Add((ply.realizedCreature as Player).spearOnBack.abstractStick);
                }
                if (stomachObject != null && (ply.realizedCreature as Player).objectInStomach == null)
                {
                    (ply.realizedCreature as Player).objectInStomach = stomachObject;
                }
                if (ply != null && ply.creatureTemplate.AI)
                {
                    ply.abstractAI.NewWorld(newWorld);
                    ply.InitiateAI();
                    ply.abstractAI.RealAI.NewRoom(newRoom.realizedRoom);
                    if (ply.creatureTemplate.type == CreatureTemplate.Type.Overseer && (ply.abstractAI as OverseerAbstractAI).playerGuide)
                    {
                        MethodInfo kpginw = typeof(OverWorld).GetMethod("KillPlayerGuideInNewWorld", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        kpginw.Invoke(game.overWorld, new object[] { newWorld, ply });
                    }
                }
                if (m == 0)
                {
                    newRoom.world.game.roomRealizer.followCreature = ply;
                }
                Debug.Log("Player " + m.ToString() + " Moved to new Region");
            }
            for (int i3 = game.shortcuts.transportVessels.Count - 1; i3 >= 0; i3--)
            {
                if (!game.overWorld.activeWorld.region.IsRoomInRegion(game.shortcuts.transportVessels[i3].room.index))
                {
                    game.shortcuts.transportVessels.RemoveAt(i3);
                }
            }
            for (int i4 = game.shortcuts.betweenRoomsWaitingLobby.Count - 1; i4 >= 0; i4--)
            {
                if (!game.overWorld.activeWorld.region.IsRoomInRegion(game.shortcuts.betweenRoomsWaitingLobby[i4].room.index))
                {
                    game.shortcuts.betweenRoomsWaitingLobby.RemoveAt(i4);
                }
            }
            for (int i5 = game.shortcuts.borderTravelVessels.Count - 1; i5 >= 0; i5--)
            {
                if (!game.overWorld.activeWorld.region.IsRoomInRegion(game.shortcuts.borderTravelVessels[i5].room.index))
                {
                    game.shortcuts.borderTravelVessels.RemoveAt(i5);
                }
            }
            error = RegionSwitcher.ErrorKey.MoveCamera;
            game.cameras[0].MoveCamera(newRoom.realizedRoom, 0);
            game.cameras[0].FireUpSinglePlayerHUD(game.Players[0].realizedCreature as Player);
            for (int i6 = 0; i6 < game.cameras.Length; i6++)
            {
                game.cameras[0].hud.ResetMap(new Map.MapData(newWorld, game.rainWorld));
                if (game.cameras[i6].hud.textPrompt.subregionTracker != null)
                {
                    game.cameras[i6].hud.textPrompt.subregionTracker.lastShownRegion = 0;
                }
            }
            game.cameras[0].virtualMicrophone.NewRoom(game.cameras[0].room);
            oldWorld.regionState.AdaptRegionStateToWorld(-1, newRoom.index);
            oldWorld.regionState.world = null;
            newWorld.rainCycle.cycleLength = oldWorld.rainCycle.cycleLength;
            newWorld.rainCycle.timer = oldWorld.rainCycle.timer;
        }

        public string GetErrorText(RegionSwitcher.ErrorKey key)
        {
            switch (key)
            {
                case RegionSwitcher.ErrorKey.LoadWorld:
                    return "An error occurred while loading the new region, check your room connections";
                case RegionSwitcher.ErrorKey.AbstractRoom:
                    return "An error occurred while loading the destination AbstractRoom";
                case RegionSwitcher.ErrorKey.RealiseRoom:
                    return "An error occurred while realising the destination room";
                case RegionSwitcher.ErrorKey.RoomRealiser:
                    return "An error occurred while loading rooms in the new region";
                case RegionSwitcher.ErrorKey.FindNode:
                    return "An error occurred while finding a node to place the player";
                case RegionSwitcher.ErrorKey.MovePlayer:
                    return "An error occurred while moving the player to the new region";
                case RegionSwitcher.ErrorKey.MoveObjects:
                    return "An error occurred while moving the player's items";
                case RegionSwitcher.ErrorKey.MoveCamera:
                    return "An error occurred while moving the RoomCamera to the new room";
                default:
                    return "I have no idea how you got this error";
            }
        }

        public RegionSwitcher.ErrorKey error;

        private MethodInfo _OverWorld_LoadWorld = typeof(OverWorld).GetMethod("LoadWorld", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public enum ErrorKey
        {
            LoadWorld,
            AbstractRoom,
            RealiseRoom,
            RoomRealiser,
            FindNode,
            MovePlayer,
            MoveObjects,
            MoveCamera
        }
    }
}
