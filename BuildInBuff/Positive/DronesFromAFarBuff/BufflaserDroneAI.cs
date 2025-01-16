using RandomBuffUtils;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive.DronesFromAFarBuff
{
    internal class BuffLaserDroneAI : ArtificialIntelligence
    {
        public BuffLaserDrone drone;
        public DroneCommandSystem commandSystem;
        public Tracker.CreatureRepresentation _attackTarget;
        //public TempPather tempPather;
        public LaserDroneQuickPather quickPather;//only use when necensary;
        public Tracker.CreatureRepresentation attackTarget
        {
            get => _attackTarget;
            set
            {
                _attackTarget = value;
            }
        }

        public int playerMoveCounter = 200;
        public int staySamePosCounter = 0;

        public WorldCoordinate lastIdlePos;
        public WorldCoordinate lastAttackPos;
        public WorldCoordinate lastProtectPos;
        public WorldCoordinate? stayPos;


        public Behaviour behaviour = Behaviour.Follow;

        public bool shouldSetTarget => drone.room == drone.owner.room;
        public bool targetAvailable => attackTarget != null && attackTarget.representedCreature != null && attackTarget.representedCreature.realizedCreature != null && !attackTarget.representedCreature.realizedCreature.inShortcut;
        public bool notInSameRoom => drone.notInSameRoom;
        public Creature realizedTarget => attackTarget.representedCreature.realizedCreature;

        public BuffLaserDroneAI(AbstractCreature abstractDrone, BuffLaserDrone drone) : base(abstractDrone, abstractDrone.world)
        {
            this.drone = drone;
            drone.AI = this;
            commandSystem = new DroneCommandSystem(this);
            //tempPather = new TempPather();
            quickPather = new LaserDroneQuickPather(this);

            AddModule(new StandardPather(this, abstractDrone.world, abstractDrone));
            AddModule(new Tracker(this, 10, 10, 600, 0.5f, 5, 5, 10));
            AddModule(new NoiseTracker(this, tracker));
            pathFinder.stepsPerFrame = 20;
            noiseTracker.hearingSkill = 1f;
        }

        public override void Update()
        {
            try
            {
                base.Update();
                if (drone.room == null) return;
                //计时器更新
                if (drone.owner.input[0].AnyInput && playerMoveCounter < 200) playerMoveCounter++;
                else if (playerMoveCounter > 0) playerMoveCounter--;


                if (lastIdlePos == pathFinder.currentlyFollowingDestination && commandSystem.targetCoord == null) staySamePosCounter++;
                else staySamePosCounter = 0;

                commandSystem.Update();

                AttackTargetUpdate();
                Act();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }
        }

        public void AttackTargetUpdate()
        {
            try
            {
                bool setTargetToNull = false;
                if (attackTarget == null) return;
                if (drone == null || drone.owner == null) return;
                if (drone.room == null || (drone.room != drone.owner.room && drone.owner.room != null && drone.room != null))
                {
                    setTargetToNull = true;
                }
                else if (drone.room != null && attackTarget != null)
                {

                    if (attackTarget.representedCreature == null || attackTarget.representedCreature.realizedCreature == null)
                    {
                        setTargetToNull = true;
                    }
                    if (attackTarget.representedCreature.realizedCreature.inShortcut)
                    {
                        setTargetToNull = true;
                    }
                    if ((attackTarget.representedCreature.state != null && !attackTarget.representedCreature.state.alive) || attackTarget.representedCreature.realizedCreature.room != drone.room)
                    {
                        setTargetToNull = true;
                    }
                    if (Vector2.Distance(drone.owner.DangerPos, attackTarget.representedCreature.realizedCreature.DangerPos) > BuffDronePort.threatMaxDistance)
                    {
                        setTargetToNull = true;
                    }
                }

                if (setTargetToNull)
                {
                    commandSystem.SendCommand(DroneCommandSystem.CommandType.Auto);
                    attackTarget = null;
                    BuffUtils.Log("DronesFromAfar - DroneAI", drone.ToString() + " target clear");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                attackTarget = null;
                commandSystem.SendCommand(DroneCommandSystem.CommandType.Auto);
            }
        }

        public void Act()
        {
            switch (behaviour)
            {
                case Behaviour.Follow:
                    WorldCoordinate playerCoord = drone.owner.abstractCreature.pos;
                    playerCoord.y += 2;
                    SetDest(playerCoord);
                    drone.ChangeMovementType(MovementType.Following);
                    break;
                case Behaviour.Stay:
                    if (stayPos != null)
                    {
                        SetDest(stayPos.Value);
                    }
                    if (targetAvailable) drone.ChangeMovementType(MovementType.AttackFloating);
                    else drone.ChangeMovementType(MovementType.FacingFoward);
                    break;
                case Behaviour.Protect:
                    bool newCoord = false;
                    for (int i = 0; i < 10; i++)//一次测试十个坐标
                    {
                        WorldCoordinate testCoord = new WorldCoordinate(drone.room.abstractRoom.index, Random.Range(0, drone.room.TileWidth), Random.Range(0, drone.room.TileHeight), -1);
                        if (ProtectCoordScore(lastProtectPos) > ProtectCoordScore(testCoord))
                        {
                            lastProtectPos = testCoord;
                            newCoord = true;
                        }
                    }
                    if (newCoord && lastProtectPos != null) SetDest(lastProtectPos);
                    //if (drone.port.closestDangerCreature.TryGetTarget(out var target)) ChangeAttackTarget(target);

                    if (targetAvailable && drone.room.VisualContact(drone.DangerPos, realizedTarget.DangerPos)) drone.ChangeMovementType(MovementType.AttackFloating);
                    else drone.ChangeMovementType(MovementType.FacingFoward);

                    break;
                case Behaviour.Attack:
                    newCoord = false;
                    for (int i = 0; i < 10; i++)//一次测试十个坐标
                    {
                        WorldCoordinate testCoord = new WorldCoordinate(drone.room.abstractRoom.index, Random.Range(0, drone.room.TileWidth), Random.Range(0, drone.room.TileHeight), -1);
                        if (AttackCoordScore(lastAttackPos) > AttackCoordScore(testCoord))
                        {
                            lastAttackPos = testCoord;
                            newCoord = true;
                        }
                    }
                    if (lastAttackPos != null && newCoord) SetDest(lastAttackPos);

                    if (targetAvailable && drone.room.VisualContact(drone.DangerPos, realizedTarget.DangerPos)) drone.ChangeMovementType(MovementType.AttackFloating);
                    else drone.ChangeMovementType(MovementType.FacingFoward);

                    break;
                case Behaviour.Idle:
                default:
                    WorldCoordinate coord = new WorldCoordinate(drone.room.abstractRoom.index, Random.Range(0, drone.room.TileWidth), Random.Range(0, drone.room.TileHeight), -1);
                    if (IdleCoordScore(lastIdlePos) > IdleCoordScore(coord) + Custom.LerpMap(staySamePosCounter, 500, 1000, 0f, -400f))
                    {
                        lastIdlePos = coord;
                        SetDest(lastIdlePos);
                    }

                    drone.ChangeMovementType(MovementType.FacingFoward);
                    break;
            }
        }

        public void ChangeBehaviour(Behaviour newBehaviour)
        {
            if (newBehaviour == behaviour) return;
            behaviour = newBehaviour;
            BuffUtils.Log("DronesFromAfar - DroneAI", drone.ToString() + " change behaviour to " + newBehaviour.ToString());
            staySamePosCounter = 0;
        }

        public float AttackCoordScore(WorldCoordinate coord)
        {
            if (coord == null) return float.MaxValue;
            if (coord.room != creature.pos.room || drone.room.GetTile(coord).Solid)
            {
                return float.MaxValue;
            }
            float score = 10000f;
            if (drone.room.aimap.getAItile(coord).narrowSpace)
            {
                score += 500f;//不想去太窄的地方
            }
            if (targetAvailable)
            {
                var preyPos = attackTarget.representedCreature.pos;
                if (preyPos.room != drone.coord.room)
                {
                    return float.MaxValue;//不在一个房间不行
                }
                if (!drone.room.VisualContact(coord, preyPos)) score += 1000f;//看不到不行

                Vector2 realCoordPos = drone.room.MiddleOfTile(coord.Tile);
                Vector2 realPreyPos = drone.room.MiddleOfTile(preyPos.Tile);

                float distance = Vector2.Distance(realPreyPos, realCoordPos);

                score += Custom.LerpMap(distance, 0f, 200f, 600f, 0f);//太近不行，太远也不行
                score += Custom.LerpMap(distance, 300f, 700f, 0f, 1000f);
            }
            return score;
        }

        public float ProtectCoordScore(WorldCoordinate coord)
        {
            if (coord == null) return float.MaxValue;
            if (coord.room != creature.pos.room || drone.room.GetTile(coord).Solid)
            {
                return float.MaxValue;
            }
            float score = 10000f;

            Vector2 realCoordPos = drone.room.MiddleOfTile(coord.Tile);
            Vector2 playerHeadPos = drone.owner.DangerPos + Vector2.up * 80f;

            float distance = Vector2.Distance(realCoordPos, playerHeadPos);
            score += Custom.LerpMap(distance, 0f, 500f, 0f, 400f);

            return score;
        }

        //Score越低表示越倾向于选择
        public float IdleCoordScore(WorldCoordinate coord)
        {
            if (coord == null) return float.MaxValue;
            if (coord.room != creature.pos.room || drone.room.GetTile(coord).Solid || coord.room != creature.pos.room)
            {
                return float.MaxValue;
            }
            float score = 10000f;
            if (drone.room.aimap.getAItile(coord).narrowSpace)
            {
                score += 100f;
            }
            for (int i = 0; i < noiseTracker.sources.Count; i++)
            {
                score -= Custom.LerpMap(Vector2.Distance(drone.room.MiddleOfTile(coord), noiseTracker.sources[i].pos), 40f, 500f, 40f, 0f);
            }
            if (drone.owner != null)
            {
                score -= Custom.LerpMap(Vector2.Distance(drone.room.MiddleOfTile(coord), drone.owner.DangerPos), 120f, 400f, 600f, 0f);
            }
            return score;
        }

        public void ChangeAttackTarget(AbstractCreature target)
        {
            if (!shouldSetTarget) return;
            if (target == null) return;
            if (commandSystem.targetCreature != null) return;

            var rep = tracker.RepresentationForCreature(target, true);

            if (attackTarget == null || attackTarget.representedCreature != target)
            {
                BuffUtils.Log("DronesFromAfar - DroneAI", drone.ToString() + "Set Attack Target : " + target.ToString());
                attackTarget = rep;

                //if (PlayerPatchs.modules.TryGetValue(drone.owner, out var module))
                //{
                //    module.portGraphics.ContinuousCast(drone.DangerPos, 10, 10f);
                //}
            }
        }

        public void SetDest(WorldCoordinate dest)
        {
            drone.abstractCreature.abstractAI.SetDestination(dest);
            //tempPather.SetDest(dest);
            quickPather.SetDest(dest);
        }

        public override PathCost TravelPreference(MovementConnection coord, PathCost cost)
        {
            return new PathCost(cost.resistance, cost.legality);
        }

        public enum Behaviour
        {
            Idle,
            Follow,
            Attack,
            Protect,
            Stay
        }
    }

    internal class DroneCommandSystem
    {
        public BuffLaserDroneAI AI;
        public BuffLaserDrone drone => AI.drone;

        public CommandType command;
        public CommandType autoSetCommand;

        public WorldCoordinate? targetCoord;
        public AbstractCreature targetCreature;

        public DroneCommandSystem(BuffLaserDroneAI ai)
        {
            AI = ai;
        }

        public void Update()
        {
            if (AI.behaviour == BuffLaserDroneAI.Behaviour.Follow)
            {
                SendCommand(CommandType.Auto);
            }

            if (AI.notInSameRoom) SendCommand(CommandType.Auto);

            switch (command)
            {
                case CommandType.Stay:
                    if (targetCoord != null)
                    {
                        AI.ChangeBehaviour(BuffLaserDroneAI.Behaviour.Stay);
                        AI.stayPos = targetCoord;
                    }
                    else SendCommand(CommandType.Auto);

                    break;
                case CommandType.Attack:
                    if (AI.targetAvailable) AI.ChangeBehaviour(BuffLaserDroneAI.Behaviour.Attack);
                    else SendCommand(CommandType.Auto);

                    break;
                case CommandType.Auto:
                default:
                    if (AI.notInSameRoom)
                    {
                        AI.ChangeBehaviour(BuffLaserDroneAI.Behaviour.Follow);
                        AI.attackTarget = null;
                        break;
                    }

                    if (AI.targetAvailable)
                    {
                        AI.behaviour = BuffLaserDroneAI.Behaviour.Attack;
                        break;
                    }

                    if (AI.playerMoveCounter > 190 || !AI.pathFinder.DoneMappingAccessibility)
                    {
                        AI.ChangeBehaviour(BuffLaserDroneAI.Behaviour.Protect);
                        break;
                    }

                    AI.ChangeBehaviour(BuffLaserDroneAI.Behaviour.Idle);
                    break;
            }

        }

        public void SendCommand(CommandType command, AbstractCreature targetCreature = null, WorldCoordinate? targetCoord = null)
        {
            if (command == this.command)
            {
                if (command == CommandType.Auto) return;
                if (command == CommandType.Stay && targetCoord == AI.stayPos) return;
            }
            switch (command)
            {
                case CommandType.Attack:
                    if (targetCreature != null)
                    {
                        this.targetCreature = null;
                        this.command = CommandType.Attack;
                        this.targetCoord = null;
                        AI.ChangeAttackTarget(targetCreature);
                        this.targetCreature = targetCreature;
                    }
                    break;
                case CommandType.Stay:
                    if (targetCoord != null)
                    {
                        this.targetCreature = null;
                        this.command = CommandType.Stay;
                        this.targetCoord = targetCoord;
                    }
                    break;
                case CommandType.Auto:
                default:
                    this.targetCoord = null;
                    this.targetCreature = null;
                    this.command = CommandType.Auto;
                    break;
            }

            //SyncUIButtons();

            //if (PlayerPatchs.modules.TryGetValue(drone.owner, out var module) && drone.owner.room == drone.room && drone.room != null && drone.owner.room != null)
            //{
            //    module.portGraphics.ContinuousCast(drone.DangerPos, 40, 10f);
            //}

            BuffUtils.Log("DronesFromAfar - DroneAI", drone.ToString() + " recive command : " + command.ToString());
        }

        //public void SyncUIButtons()
        //{
        //    if (DroneHUD.instance != null && DroneHUD.instance.droneUIs.Count > 0)
        //    {
        //        foreach (var ui in DroneHUD.instance.droneUIs)
        //        {
        //            foreach (var button in ui.buttons)
        //            {
        //                if (button.command == this.command)
        //                {
        //                    button.PressMe(true);
        //                    return;
        //                }
        //            }
        //        }
        //    }
        //}

        public enum CommandType
        {
            Auto,
            Attack,
            Stay
        }
    }

    public class TempPather
    {
        public static readonly float updateBreakTime = 1f;

        public WorldCoordinate Dest;
        public WorldCoordinate startCoord;
        public List<IntVector2> path = new List<IntVector2>();
        public float lastUpdateTime = 0f;

        public bool isDestChange = false;

        public int actuallyFollowPathIndex = 0;
        public int ActuallyFollowPathIndex
        {
            get => actuallyFollowPathIndex;
            set
            {
                if (path.Count > 0) actuallyFollowPathIndex = Mathf.Clamp(value, 0, path.Count - 1);
                else actuallyFollowPathIndex = 0;
            }
        }

        public void SetDest(WorldCoordinate dest)
        {
            if (dest != Dest)
            {
                Dest = dest;
                isDestChange = true;
            }
        }

        public IntVector2 Update(Vector2 current, Room room)
        {
            WorldCoordinate currentCoord = room.GetWorldCoordinate(current);
            if (isDestChange && Time.time - lastUpdateTime >= updateBreakTime)
            {
                startCoord = currentCoord;
                FindPath(room);

                isDestChange = false;
                lastUpdateTime = Time.time + Random.value;
            }

            if (path.Count == 0) return currentCoord.Tile;

            if (room.GetTilePosition(current).FloatDist(path[ActuallyFollowPathIndex]) <= 1.5f)
            {
                ActuallyFollowPathIndex++;
            }
            return path[ActuallyFollowPathIndex];
        }

        public void FindPath(Room room)
        {
            ActuallyFollowPathIndex = 0;
            path.Clear();
            QuickConnectivity.QuickPath(room, DronesFromAfarBuffEntry.laserDroneRegister.Template, startCoord.Tile, Dest.Tile, 4000, 1000, true, ref path);
        }
    }

    internal class LaserDroneQuickPather
    {
        public BuffLaserDroneAI droneAI;
        public QuickPathFinder updatingPather;

        public int currentFollowIndex = 0;
        public int forceFollowPathCounter = 0;

        public bool shouldChangePath = false;
        public WorldCoordinate dest;

        public QuickPath currentPath;

        public LaserDroneQuickPather(BuffLaserDroneAI ai)
        {
            droneAI = ai;
        }

        public void SetDest(WorldCoordinate newDest)
        {
            if (newDest != dest)
            {
                dest = newDest;
                shouldChangePath = true;
            }
        }

        public IntVector2? UpdatePath(Vector2 current, Room room)
        {
            IntVector2? result = null;
            IntVector2 currentTile = room.GetTilePosition(current);

            if (room == null) return result;
            if (droneAI.drone.owner == null) return result;
            if (droneAI.drone.room != droneAI.drone.owner.room) return result;

            if (forceFollowPathCounter > 0) forceFollowPathCounter--;

            if (currentPath != null && currentPath.tiles != null) result = currentPath.tiles[currentFollowIndex];

            if (shouldChangePath)
            {
                if (updatingPather == null && forceFollowPathCounter == 0)
                {
                    updatingPather = new QuickPathFinder(currentTile, dest.Tile, room.aimap, DronesFromAfarBuffEntry.laserDroneRegister.Template);
                }

                if (updatingPather != null)
                {
                    int step = 0;
                    while (updatingPather.status == 0 && step < 240)
                    {
                        step++;
                        updatingPather.Update();
                    }
                    if (updatingPather.status == 0)
                    {
                        return result;
                    }

                    currentPath = updatingPather.ReturnPath();
                    updatingPather = null;
                    forceFollowPathCounter = 160;
                    currentFollowIndex = 0;
                    if (currentPath == null) return result;
                    else
                    {
                        shouldChangePath = false;
                    }
                }
            }
            return result;
        }

        public void MoveNext()
        {
            currentFollowIndex = Mathf.Min(currentFollowIndex + 1, currentPath.tiles.Length - 1);
        }
    }
}
