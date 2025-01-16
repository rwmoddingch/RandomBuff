using RandomBuffUtils;
using RandomBuffUtils.ParticleSystem.EmitterModules;
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
    internal class BuffLaserDrone : Creature
    {
        public readonly int teleportCounter = 10;
        public readonly float maxAcceleration = 0.5f;
        public readonly float maxDeltaCourse = 15f;
        public readonly float acceptableDistance = 35f;
        public readonly float _maxVelocity = 20f;

        public static bool pauseMovement = false;

        public static int teleportCooler = 0;

        public Player owner
        {
            get
            {
                if (port != null && port.owner != null && port.owner.TryGetTarget(out var player)) return player;
                return null;
            }
        }
        public BuffDronePort port;
        public DroneLaserGun gun;
        public DroneState droneState;


        public BuffLaserDroneAI AI = null;
        public MovementConnection lastFollowedConnection;
        public MovementConnection currentConnection;
        public InfinitRotation infRotation = new InfinitRotation();

        public DamageEffect damageEffect;

        public int life = 1600;
        public int noOwnerCount = 200;
        public int stuckInShortCutCounter = 0;
        public int attackCoolDown = 0;
        public int notInSameRoomCounter = 0;
        public int dontEnterShortcutCounter = 0;
        public int dontRecieveDamageCount = 0;

        public Vector2 hangPos = Vector2.zero;
        public Vector2 randomHangPosBias;
        public Vector2? startCallBackPos = null;

        public float CourseAngle = 0f;

        public float accelereation = 0f;
        public float velocity = 0f;

        public float callBackLerpFactor = 0f;

        public bool flyToShortCut = false;
        public bool forceUsingTempPather = false;

        public MovementType movementType = MovementType.FacingFoward;

        public bool notInSameRoom => owner != null && room != owner.room && !inShortcut && !owner.inShortcut;
        public float MaxVelocity => _maxVelocity * (AI.pathFinder.DoneMappingAccessibility ? 1f : 0.7f);


        public BuffLaserDrone(AbstractCreature abstractCreature) : base(abstractCreature, abstractCreature.world)
        {
            abstractCreature.destroyOnAbstraction = true;
            gun = new DroneLaserGun(this);
            damageEffect = new DamageEffect(this);

            bodyChunks = new BodyChunk[1];
            bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0f, 0f), 5f, 0.07f);

            bodyChunkConnections = new BodyChunkConnection[0];

            airFriction = 0.999f;
            gravity = 0f;
            Random.State state = Random.state;
            Random.InitState(abstractPhysicalObject.ID.RandomSeed);

            randomHangPosBias = new Vector2(Random.value * 2f - 1f, Random.value * 2f - 1f);
            randomHangPosBias *= Random.value * 40f;
            Random.state = state;

            GoThroughFloors = true;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            damageEffect.Update(DangerPos, eu);

            mainBodyChunk.vel *= 0.9f;

            for (int i = 0; i < grabbedBy.Count; i++)
            {
                if (grabbedBy[i].grabber == owner)
                {
                    GrabbedUpdate();
                    return;
                }
            }
            gun.Update();

            if (notInSameRoom) notInSameRoomCounter++;
            if (dontEnterShortcutCounter > 0) dontEnterShortcutCounter--;
            if (dontRecieveDamageCount > 0) dontRecieveDamageCount--;
            if (shortcutDelay > 0) shortcutDelay--;

            CallBackUpdate();

            if (room == null)
            {
                return;
            }

            if (owner == null)
            {
                Des("Owner is null", false);
                noOwnerCount--;
                return;
            }


            if (notInSameRoomCounter > teleportCounter)
            {
                TryTeleportToOwner();
            }

            if (life <= 0 || noOwnerCount <= 0)
            {
                Des("Owner is null or life count to zero", false);
            }
            if (!inShortcut) Act();
        }

        public void Act()
        {
            GetCourseAngle();
            if (movementType == MovementType.CallBack) return;

            Vector2 followingPos = Vector2.zero;
            AI.Update();
            followingPos = bodyChunks[0].pos;

            StandardPather pather = AI.pathFinder as StandardPather;
            //TempPather tempPather = AI.tempPather;
            LaserDroneQuickPather quickPather = AI.quickPather;

            MovementConnection? movementConnection = null;
            if (pather != null && pather.DoneMappingAccessibility && !forceUsingTempPather)
            {
                movementConnection = pather.FollowPath(room.GetWorldCoordinate(followingPos), true);
                currentConnection = movementConnection.Value;
            }

            if (movementConnection != null)
            {
                FlyFollow(movementConnection.Value, pather.destination);
            }
            else
            {
                if (room == null)
                {
                    dontEnterShortcutCounter++;
                    return;
                }
                IntVector2? nextTile;
                //nextTile = tempPather.Update(DangerPos, room);
                nextTile = quickPather.UpdatePath(DangerPos, room);

                //if (tempPather.path.Count > 0 && room.GetTile(nextTile).shortCut != 0 && shortcutDelay == 0)
                //{
                //    SuckedIntoShortCut(nextTile, false);
                //    tempPather.ActuallyFollowPathIndex++;
                //}
                if (nextTile != null)
                {
                    if (room.shortcutData(nextTile.Value).shortCutType != ShortcutData.Type.DeadEnd && shortcutDelay == 0)
                    {
                        SuckedIntoShortCut(nextTile.Value, false);
                        quickPather.MoveNext();
                    }
                    else
                    {
                        MoveTowards(room.MiddleOfTile(nextTile.Value), room.MiddleOfTile(quickPather.dest));
                    }

                    if (Custom.DistLess(room.MiddleOfTile(nextTile.Value), DangerPos, 15f))
                    {
                        quickPather.MoveNext();
                    }

                }

                //MoveTowards(room.MiddleOfTile(nextTile.Value), room.MiddleOfTile(tempPather.Dest));
            }

            if (Submersion > .5)
            {
                firstChunk.vel += Vector2.up;
            }

            if (AI.targetAvailable && room.VisualContact(mainBodyChunk.pos, AI.attackTarget.representedCreature.realizedCreature.DangerPos))
            {
                float deltaAngle = Mathf.Abs(infRotation.CaculateDelta(Custom.VecToDeg(Custom.DirVec(base.mainBodyChunk.pos, AI.attackTarget.representedCreature.realizedCreature.DangerPos))));
                float dist = Vector2.Distance(mainBodyChunk.pos, AI.attackTarget.representedCreature.realizedCreature.DangerPos);
                if (deltaAngle > 20f || dist > 600f) return;
                gun.shouldCharge = true;
            }
            else
            {
                gun.shouldCharge = false;
            }
        }

        public void GrabbedUpdate()
        {
            if (owner.bodyMode != Player.BodyModeIndex.ClimbingOnBeam)
            {
                owner.mainBodyChunk.vel += Vector2.up * owner.gravity * 0.8f;
            }
            PlayerUtils.TryGetModulePart<DronesFromAfarBuff.DronesModule, DronesFromAfarBuff>(owner, out var module);
            //PlayerPatchs.modules.TryGetValue(owner, out var module);

            //if (Plugin.instance.config.OverPowerdSuperJump.Value)
            //{
            //    Vector2 adder = owner.input[0].analogueDir;
            //    adder.x *= 0.25f;
            //    adder.y *= 0.4f;
            //    module.playerExtraMovement.PlusSpeed(adder);
            //}

            if (owner.bodyMode != Player.BodyModeIndex.ZeroG)
            {
                Vector2 airBoost = owner.input[0].analogueDir * 2f;
                airBoost.y *= 0.1f;
                module.playerExtraMovement.extraVelocity += airBoost;
                infRotation.RotateTo(Custom.VecToDeg(owner.mainBodyChunk.vel.normalized));
            }
            else
            {
                if (owner.input[0].AnyDirectionalInput)
                {
                    Vector2 bodyDir = Custom.DirVec(owner.bodyChunks[1].pos, owner.bodyChunks[0].pos);
                    Vector2 velDir = owner.mainBodyChunk.vel.normalized;

                    float factor = Mathf.Abs(Vector2.Dot(bodyDir, velDir));
                    module.playerExtraMovement.PlusSpeed(bodyDir * factor * 0.15f);
                    infRotation.RotateTo(Custom.VecToDeg(bodyDir));
                }
            }

            CourseAngle = infRotation.RealRotation;
        }

        public override Color ShortCutColor()
        {
            if (owner != null && PlayerUtils.TryGetModulePart<DronesFromAfarBuff.DronesModule, DronesFromAfarBuff>(owner, out var module)) return module.laserColor;
            return new Color(1f, 0.26f, 0.45f);
        }

        public override void InitiateGraphicsModule()
        {
            if (graphicsModule == null) graphicsModule = new BuffLaserDroneGraphics(this);
            graphicsModule.Reset();
        }

        public override void Stun(int st)
        {
            //cant stun
        }

        public override void Violence(BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, Appendage.Pos hitAppendage, DamageType type, float damage, float stunBonus)
        {
            if (dontRecieveDamageCount > 0 || type == null) return;
            if (droneState == null) return;
            if (type == DamageType.Explosion)
            {
                droneState.RecieveDamage();
                dontRecieveDamageCount = 40;
            }
            else if (type != DamageType.Electric && damage > 0.95)
            {
                droneState.RecieveDamage();
                dontRecieveDamageCount = 40;
            }
        }

        public override bool CanBeGrabbed(Creature grabber)
        {
            return grabber is Player;
        }

        public override void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            base.Collide(otherObject, myChunk, otherChunk);
        }

        public override void Die()
        {
        }

        public override void HitByWeapon(Weapon weapon)
        {
            if (dontRecieveDamageCount > 0) return;

            if (weapon is Spear)
            {
                room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, mainBodyChunk);
                weapon.ChangeMode(Weapon.Mode.Free);
                weapon.SetRandomSpin();
                if (droneState != null) droneState.RecieveDamage();
                dontRecieveDamageCount = 40;
            }
        }

        public void Des(string message, bool summonBang)
        {
            if (summonBang)
            {
                room.AddObject(new ShockWave(DangerPos, 330f, 0.15f, 10, false));
                room.PlaySound(SoundID.Fire_Spear_Explode, DangerPos);
                room.AddObject(new SootMark(room, DangerPos, 50f, false));

                for (int i = 0; i < 14; i++)
                {
                    room.AddObject(new Explosion.ExplosionSmoke(DangerPos, Custom.RNV() * 5f * Random.value, 1f));
                }

                for (int i = 0; i < 5; i++)
                {
                    room.AddObject(new ExplosiveSpear.SpearFragment(DangerPos, Custom.RNV() * Mathf.Lerp(20f, 40f, Random.value)));

                }

                room.AddObject(new Explosion.ExplosionLight(DangerPos, 280f, 1f, 7, ShortCutColor()));
                room.AddObject(new Explosion.ExplosionLight(DangerPos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
                room.AddObject(new ExplosionSpikes(room, DangerPos, 14, 30f, 9f, 7f, 170f, ShortCutColor()));
            }


            //Plugin.Log(ToString() + " Destroy because : " + message);
            Destroy();
        }

        public override void SpitOutOfShortCut(IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            base.SpitOutOfShortCut(pos, newRoom, spitOutAllSticks);

            shortcutDelay = 40;
            Vector2 a = Custom.IntVector2ToVector2(newRoom.ShorcutEntranceHoleDirection(pos));
            mainBodyChunk.pos = newRoom.MiddleOfTile(pos) - a * 5f;
            mainBodyChunk.lastPos = mainBodyChunk.pos;
            mainBodyChunk.vel = a * 15f;

            if (owner != null && newRoom == owner.room)
            {
                abstractCreature.abstractAI.SetDestination(owner.coord);
            }
        }

        public override void Destroy()
        {
            if (port != null)
            {
                for (int i = port.drones.Count - 1; i >= 0; i--)
                {
                    if (port.drones[i].TryGetTarget(out var getDrone) && getDrone == this)
                    {
                        port.drones.RemoveAt(i);
                    }
                }
            }



            if (droneState != null)
            {
                droneState.currentDrone.SetTarget(null);
            }

            if (room.updateList.Contains(this))
            {
                room.updateList.Remove(this);
            }
            if (room.abstractRoom.entities.Contains(this.abstractCreature))
            {
                room.abstractRoom.entities.Remove(this.abstractCreature);
            }

            base.Destroy();

            //Plugin.Log(ToString() + " Destroy");
        }

        public void FlyFollow(MovementConnection followingConnection, WorldCoordinate dest)
        {
            if (followingConnection.type == MovementConnection.MovementType.ShortCut && dontEnterShortcutCounter == 0)
            {
                if ((room.GetTile(followingConnection.StartTile).Terrain == Room.Tile.TerrainType.ShortcutEntrance))
                {
                    enteringShortCut = new IntVector2?(followingConnection.StartTile);
                    dontEnterShortcutCounter = 20;
                }
            }
            else
            {
                MoveTowards(room.MiddleOfTile(followingConnection.DestTile), room.MiddleOfTile(dest));
            }
            lastFollowedConnection = followingConnection;
        }

        public void MoveTowards(Vector2 moveTo, Vector2 dest)
        {
            try
            {
                //change vel or pos
                if (AI.pathFinder.destination == null || Vector2.Distance(firstChunk.pos, dest) <= acceptableDistance || pauseMovement)
                {
                    bodyChunks[0].vel = Vector2.Lerp(bodyChunks[0].vel, Vector2.zero, 0.05f);
                }
                else
                {
                    switch (movementType)
                    {
                        case MovementType.FacingFoward:
                            Vector2 dir = Custom.DirVec(firstChunk.pos, moveTo);
                            accelereation = Vector2.Distance(firstChunk.pos, moveTo) / 4f;
                            accelereation = Mathf.Clamp(accelereation, 0f, maxAcceleration);

                            bodyChunks[0].vel += accelereation * dir;
                            bodyChunks[0].vel = Vector2.ClampMagnitude(bodyChunks[0].vel, MaxVelocity);

                            break;
                        case MovementType.Following:
                            float vel = MaxVelocity;
                            dir = Custom.DirVec(firstChunk.pos, moveTo);
                            bodyChunks[0].vel = dir * vel;
                            break;
                        case MovementType.AttackFloating:
                        default:
                            Vector2 destPos = room.MiddleOfTile(AI.pathFinder.destination);
                            var aimDir = Custom.DirVec(firstChunk.pos, moveTo);
                            var currentDir = bodyChunks[0].vel.normalized;

                            dir = Vector2.Lerp(currentDir, aimDir, 0.05f);
                            vel = Custom.LerpMap(Vector2.Distance(firstChunk.pos, moveTo), 0f, 120f, 0f, MaxVelocity * 0.75f);

                            Vector2 thisVel = dir * vel;

                            thisVel = Vector2.ClampMagnitude(thisVel, MaxVelocity * 0.75f);

                            bodyChunks[0].vel = thisVel;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void GetCourseAngle()
        {
            switch (movementType)
            {
                case MovementType.CallBack:
                    if (startCallBackPos != null)
                    {
                        Vector2 dir = Custom.DirVec(startCallBackPos.Value, DangerPos);
                        infRotation.RotateTo(Custom.VecToDeg(dir));
                    }
                    break;
                case MovementType.FacingFoward:
                    infRotation.RotateTo(Custom.VecToDeg(bodyChunks[0].vel), maxDeltaCourse);
                    break;
                case MovementType.AttackFloating:
                default:
                    if (AI.targetAvailable)
                    {
                        Vector2 realizeTargetPos = AI.attackTarget.representedCreature.realizedCreature.DangerPos;
                        Vector2 lookDir = Custom.DirVec(bodyChunks[0].pos, realizeTargetPos);

                        infRotation.RotateTo(Custom.VecToDeg(lookDir), maxDeltaCourse * 1.5f);
                    }
                    else
                    {
                        infRotation.RotateTo(Custom.VecToDeg(bodyChunks[0].vel), maxDeltaCourse);
                    }
                    break;
            }
            CourseAngle = infRotation.RealRotation;
        }

        public void TryTeleportToOwner(bool forceSpit = false)
        {
            if (teleportCooler > 0)
            {
                teleportCooler--;
                return;
            }
            if (port == null) return;
            if (owner == null) return;
            if (!port.owner.TryGetTarget(out Player player)) return;
            if (!player.inShortcut && !inShortcut)
            {
                if (room == null || room == player.room) return;
                foreach (var shortcut in room.shortcuts)
                {
                    if (shortcut.shortCutType == ShortcutData.Type.RoomExit)
                    {
                        int connection = room.abstractRoom.connections[shortcut.destNode];
                        AbstractRoom abstractRoom = room.world.GetAbstractRoom(connection);
                        if (abstractRoom == player.room.abstractRoom)
                        {
                            SuckedIntoShortCut(shortcut.StartTile, false);
                            enteringShortCut = null;
                            //Plugin.Log("Drone " + this.ToString() + " teleport to player by entering shortcut");
                            notInSameRoomCounter = 0;
                            teleportCooler = 10;
                            return;
                        }
                    }
                }
            }

            if (port.lastSpitOutShortcut != null && port.lastSpitOutShortcut.HasValue && port.lastSpitOutRoom != null && port.lastSpitOutRoom.TryGetTarget(out Room targer))
            {
                if (port.owner.TryGetTarget(out player))
                {
                    if (player.inShortcut) return;

                    Des("Teleport", false);
                    notInSameRoomCounter = 0;
                    teleportCooler = 10;
                    //Plugin.Log("Drone " + this.ToString() + " teleport to player over 1 room" + (forceSpit ? " but forceSpit" : ""));
                }
            }
        }

        public void ChangeMovementType(MovementType newType)
        {
            if (newType != movementType && movementType != MovementType.CallBack)
            {
                movementType = newType;
                //Plugin.Log("ChangeMovementType " + newType.ToString());
            }
        }

        public void CallBackUpdate()
        {
            if (movementType == MovementType.CallBack && !notInSameRoom)
            {
                if (startCallBackPos == null)
                {
                    startCallBackPos = DangerPos;
                    CollideWithTerrain = false;
                }

                Vector2 dir = (owner.DangerPos - DangerPos).normalized;
                float distance = Vector2.Distance(owner.DangerPos, DangerPos);

                Vector2 delta = dir * Mathf.Min(7f, distance);
                bodyChunks[0].HardSetPosition(DangerPos + delta);

                if (Custom.DistLess(DangerPos, owner.DangerPos, 5f))
                {
                    Des("Call back", false);
                }
                return;
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    public class InfinitRotation
    {
        public int round = 0;
        public float rotation = 0f;
        public float lastRotation;
        public float smoothedRotation;

        public float totalRotation
        {
            get
            {
                return round * 360f + rotation;
            }
            set
            {
                round = (int)((value - value % 360f) / 360f);
                rotation = value - round * 360f;
            }
        }

        public float RealRotation => MappingInfToOrigin(rotation);
        public float SmoothedRealRotation
        {
            get
            {
                float round = (int)((smoothedRotation - smoothedRotation % 360f) / 360f);
                float temp = smoothedRotation - round * 360f;
                return MappingInfToOrigin(temp);
            }
        }

        public void CaculateSmoothedRealRotion(float timeStacker)
        {
            lastRotation = smoothedRotation;
            if (timeStacker > 1f) lastRotation = totalRotation;
            smoothedRotation = Mathf.Lerp(lastRotation, totalRotation, timeStacker);
        }

        public float MappingOriginToInf(float orig)
        {
            return orig + 180f;
        }

        public float MappingInfToOrigin(float inf)
        {
            return inf - 180f;
        }

        public float CaculateDelta(float orig)
        {
            float toInf = MappingOriginToInf(orig);
            float total1 = toInf + 360f * (round);
            float total2 = toInf + 360f * (round - 1);
            float total3 = toInf + 360f * (round + 1);

            float[] absDeltas = new float[3]
            {
                Mathf.Abs(total1 - totalRotation),
                Mathf.Abs(total2 - totalRotation),
                Mathf.Abs(total3 - totalRotation),
            };

            float[] deltas = new float[3]
            {
                total1 - totalRotation,
                total2 - totalRotation,
                total3 - totalRotation
            };

            return deltas[Array.IndexOf(absDeltas, absDeltas.Min())];
        }

        public void RotateTo(float orig, float maxDelta = float.MaxValue)
        {
            float delta = Mathf.Clamp(CaculateDelta(orig), -maxDelta, maxDelta);

            totalRotation += delta;
        }
    }

    internal class DroneState
    {
        public int life = 3;
        public BuffDronePort port;
        public WeakReference<BuffLaserDrone> currentDrone = new WeakReference<BuffLaserDrone>(null);

        public DroneState(BuffDronePort port)
        {
            this.port = port;
        }

        public bool ThisStateAvailableForMe(BuffLaserDrone drone)
        {
            bool result = true;
            if (life <= 0) result = false;
            if (currentDrone.TryGetTarget(out var _)) result = false;
            return result;
        }

        public void RecieveDamage()
        {
            life--;
            if (currentDrone.TryGetTarget(out var drone))
            {
                BuffUtils.Log("BuffLaserDrone",drone.ToString() + " recieve damage");
            }
            if (life == 0)
            {
                BuffUtils.Log("BuffLaserDrone", "Drone Crash");
                if (currentDrone.TryGetTarget(out drone)) drone.Des("Crash", true);
                port.crashedDroneCount++;
            }
        }
    }

    public enum MovementType
    {
        FacingFoward,
        AttackFloating,
        Following,
        CallBack
    }

}
