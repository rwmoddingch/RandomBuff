using RandomBuffUtils;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BuiltinBuffs.Positive.DronesFromAFarBuff
{
    internal class BuffDronePort
    {
        public static float threatMaxDistance = 800f;

        public WeakReference<Player> owner;
        public List<WeakReference<BuffLaserDrone>> drones;
        public DroneState[] states;

        public IntVector2? lastSpitOutShortcut;
        public WeakReference<Room> lastSpitOutRoom;
        public WeakReference<AbstractCreature> closestDangerCreature = new WeakReference<AbstractCreature>(null);

        public int spawnDroneCoolDown = 40;
        public int preSpawnWaitCounter = 450;
        public int crashedDroneCount = 0;

        public bool usingOverrideSetting = false;

        public int availableDroneCount
        {
            get
            {
                int result = 1;
                if (owner.TryGetTarget(out Player player))
                    result = Mathf.CeilToInt((player.Karma + 1f) * 0.5f);

                return result - crashedDroneCount;
            }
        }

        public bool CanSpawnDrone
        {
            get
            {
                bool preCondition = preSpawnWaitCounter == 0 && spawnDroneCoolDown <= 0 && drones.Count < availableDroneCount;
                preCondition &= owner.TryGetTarget(out var player) && player.room.regionGate == null;

                return preCondition;
            }
        }

        public bool InRegionGateOrInShelter => owner.TryGetTarget(out var player) && (player.room.regionGate != null || player.room.shelterDoor != null);


        public BuffDronePort(Player player)
        {
            owner = new WeakReference<Player>(player);
            drones = new List<WeakReference<BuffLaserDrone>>();
            //pearlReader = new PortPearlReader();

            states = new DroneState[availableDroneCount];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = new DroneState(this);
            }
            BuffUtils.Log($"DronesFromAfar - DronePort","This cycle player can get " + availableDroneCount.ToString() + " drones");
        }



        public void Update()
        {
            for (int i = drones.Count - 1; i >= 0; i--)
            {
                if (!drones[i].TryGetTarget(out var drone) || drone.slatedForDeletetion)
                {
                    drones.RemoveAt(i);
                }
            }
            if (spawnDroneCoolDown > 0) spawnDroneCoolDown--;
            if (preSpawnWaitCounter > 0) preSpawnWaitCounter--;

            if (owner.TryGetTarget(out Player player))
            {
                if (player.inShortcut) return;

                if (InRegionGateOrInShelter) CallBackAllDrones();

                for (int i = drones.Count - 1; i >= 0; i--)
                {
                    if (drones[i].TryGetTarget(out var drone))
                    {
                        if (drone.inShortcut || player.inShortcut) continue;
                        if (drone.room.world.region != player.room.world.region)
                        {
                            drone.Des("Not in same region", false);
                        }
                    }
                }

                if (CanSpawnDrone && !player.room.abstractRoom.shelter)
                {
                    SpawnNewDrone(player);
                }
                if (drones.Count > availableDroneCount)
                {
                    for (int i = drones.Count - availableDroneCount - 1; i >= 0; i--)
                    {
                        var reference = drones.Pop();
                        if (reference.TryGetTarget(out var drone))
                        {
                            drone.Des("Too many drones", false);
                        }
                        else continue;
                    }
                }
            }
            SearchThreatCreature(player);
        }

        public void SearchThreatCreature(Player player)
        {
            float mostDanger = float.MinValue;
            float closestDist = float.MaxValue;
            AbstractCreature mostDangerousCreature = null;
            AbstractCreature closestDangerCreature = null;


            for (int i = 0; i < player.room.abstractRoom.creatures.Count; i++)
            {
                if (player.room.abstractRoom.creatures[i].realizedCreature != null && player.room.abstractRoom.creatures[i].realizedCreature != player)
                {
                    if (player.room.abstractRoom.creatures[i].state == null || !player.room.abstractRoom.creatures[i].state.alive) continue;
                    float danger = ThreatOfCreature(player.room.abstractRoom.creatures[i].realizedCreature, player);
                    float distance = Vector2.Distance(player.DangerPos, player.room.abstractRoom.creatures[i].realizedCreature.DangerPos);

                    float threshold = 0.2f;
                    if (danger > threshold)
                    {
                        if (danger > mostDanger && danger > threshold)
                        {
                            mostDangerousCreature = player.room.abstractRoom.creatures[i];
                            mostDanger = danger;
                        }
                        if (distance < closestDist)
                        {
                            closestDangerCreature = player.room.abstractRoom.creatures[i];
                            closestDist = distance;
                        }
                    }
                }
            }

            this.closestDangerCreature.SetTarget(closestDangerCreature);
            if (mostDangerousCreature != null && closestDist <= threatMaxDistance)
            {
                for (int i = drones.Count - 1; i >= 0; i--)
                {
                    if (drones[i].TryGetTarget(out var drone))
                    {
                        drone.AI.ChangeAttackTarget(mostDangerousCreature);
                    }
                }
            }
        }

        public float ThreatOfCreature(Creature creature, Player player)
        {
            //copy from ThreatDetermination.ThreatOfCreature();
            float danger = creature.Template.dangerousToPlayer;
            if (creature is Cicada) danger = 0.5f;
            if (creature.inShortcut) return 0f;
            if (danger == 0f)
            {
                return 0f;
            }
            if (creature.dead)
            {
                return 0f;
            }
            bool visualContact = false;
            float chanceOfFinding = 0f;
            if (creature.abstractCreature.abstractAI != null && creature.abstractCreature.abstractAI.RealAI != null && creature.abstractCreature.abstractAI.RealAI.tracker != null)
            {
                for (int i = 0; i < creature.abstractCreature.abstractAI.RealAI.tracker.CreaturesCount; i++)
                {
                    if (creature.abstractCreature.abstractAI.RealAI.tracker.GetRep(i).representedCreature == player.abstractCreature)
                    {
                        visualContact = creature.abstractCreature.abstractAI.RealAI.tracker.GetRep(i).VisualContact;
                        chanceOfFinding = creature.abstractCreature.abstractAI.RealAI.tracker.GetRep(i).EstimatedChanceOfFinding;
                        break;
                    }
                }
            }
            danger *= Custom.LerpMap(Vector2.Distance(creature.DangerPos, player.mainBodyChunk.pos), 300f, 2400f, 1f, visualContact ? 0.2f : 0f);
            danger *= 1f + Mathf.InverseLerp(300f, 20f, Vector2.Distance(creature.DangerPos, player.mainBodyChunk.pos)) * Mathf.InverseLerp(2f, 7f, creature.firstChunk.vel.magnitude);
            danger *= Mathf.Lerp(0.33333334f, 1f, Mathf.Pow(chanceOfFinding, 0.75f));
            if (creature.abstractCreature.abstractAI != null && creature.abstractCreature.abstractAI.RealAI != null)
            {
                danger *= creature.abstractCreature.abstractAI.RealAI.CurrentPlayerAggression(player.abstractCreature);
            }

            if (creature is Centipede)
            {
                danger *= (player.CurrentFood < player.MaxFoodInStomach) ? 0.1f : 1f;
            }
            return danger;
        }

        public void SpawnNewDrone(Player player)
        {
            spawnDroneCoolDown = 40;

            AbstractCreature abstractDrone = new AbstractCreature(player.room.world, DronesFromAfarBuffEntry.laserDroneRegister.Template, null, new WorldCoordinate(player.room.abstractRoom.index, player.coord.x, player.coord.y, -1), player.room.game.GetNewID());
            player.room.abstractRoom.AddEntity(abstractDrone);
            abstractDrone.RealizeInRoom();
            BuffLaserDrone drone = abstractDrone.realizedObject as BuffLaserDrone;
            drone.firstChunk.pos = player.DangerPos;
            drone.port = this;

            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].ThisStateAvailableForMe(drone))
                {
                    states[i].currentDrone.SetTarget(drone);
                    drone.droneState = states[i];
                    break;
                }
            }

            AddDroneToPort(drone);
        }

        public void CallBackAllDrones()
        {
            for (int i = drones.Count - 1; i >= 0; i--)
            {
                if (drones[i].TryGetTarget(out var getDrone))
                {
                    getDrone.ChangeMovementType(MovementType.CallBack);
                }
            }
        }

        public void ResummonDrones()
        {
            if (!owner.TryGetTarget(out var player)) return;
            for (int i = drones.Count - 1; i >= 0; i--)
            {
                if (drones[i].TryGetTarget(out var getDrone))
                {
                    if (getDrone.room == player.room)
                    {
                        getDrone.ChangeMovementType(MovementType.CallBack);
                    }
                    else
                    {
                        getDrone.Des("Resummon Drones", false);
                    }
                }
            }
        }

        public void ClearOutAllDrones()
        {
            crashedDroneCount += drones.Count;
            for (int i = drones.Count - 1; i >= 0; i--)
            {
                if (drones[i].TryGetTarget(out var getDrone))
                {
                    getDrone.Des("Player Die", true);
                }
            }
        }

        public void AddDroneToPort(BuffLaserDrone drone)
        {
            for (int i = drones.Count - 1; i >= 0; i--)
            {
                if (drones[i].TryGetTarget(out var getDrone) && getDrone == drone)
                {
                    return;
                }
            }
            drones.Add(new WeakReference<BuffLaserDrone>(drone));
        }
    }
}
