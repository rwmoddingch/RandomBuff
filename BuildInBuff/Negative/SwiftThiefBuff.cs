using MonoMod.RuntimeDetour;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using MoreSlugcats;

namespace BuiltinBuffs.Negative
{
    internal class SwiftThiefBuff : Buff<SwiftThiefBuff, SwiftThiefBuffData>
    {
        public override BuffID ID => SwiftThiefBuffEntry.SwiftThiefID;
    }

    class SwiftThiefBuffData : BuffData
    {
        public override BuffID ID => SwiftThiefBuffEntry.SwiftThiefID;
    }

    class SwiftThiefBuffEntry : IBuffEntry
    {
        public static BuffID SwiftThiefID = new BuffID("SwiftThief", true);
        public static ConditionalWeakTable<Scavenger, ThiefScav> thiefScavs = new ConditionalWeakTable<Scavenger, ThiefScav>();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SwiftThiefBuff, SwiftThiefBuffData, SwiftThiefBuffEntry>(SwiftThiefID);
        }

        public static void HookOn()
        {
            Hook scavSpeed = new Hook(
                typeof(Scavenger).GetProperty("MovementSpeed", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(),
                typeof(SwiftThiefBuffEntry).GetMethod("Scavenger_MovementSpeed_get", BindingFlags.Public | BindingFlags.Static)
                );
            On.Scavenger.ctor += Scavenger_ctor;
            On.Scavenger.Update += Scavenger_Update;
        }
        
        private static void Scavenger_ctor(On.Scavenger.orig_ctor orig, Scavenger self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            thiefScavs.Add(self, new ThiefScav(null));
        }

        private static void Scavenger_Update(On.Scavenger.orig_Update orig, Scavenger self, bool eu)
        {
            orig(self, eu);
            try
            {
                bool escaping = false;
                if (thiefScavs.TryGetValue(self, out var thiefScav) && thiefScav.scaringPlayer.Count > 0)
                {
                    for (int i = 0; i < thiefScav.scaringPlayer.Count; i++)
                    {
                        if (thiefScav.scaringPlayer[i].state.dead)
                        {
                            thiefScav.scaringPlayer.RemoveAt(i);
                        }
                        else if (self.room != null)
                        {
                            TryLeaveRoom(self.AI, self.room);
                            escaping = true;
                        }
                    }
                }

                int freeGrasp = -1;
                for (int i = 0; i < self.grasps.Length; i++)
                {
                    if (self.grasps[i] == null)
                    {
                        freeGrasp++;
                    }
                }

                bool hasStolen = false;
                if (!escaping && !(self.room == null || self.room.physicalObjects.Length == 0))
                {
                    for (int i = 0; i < self.room.physicalObjects.Length; i++)
                    {
                        if (self.room.physicalObjects[i].Count > 0)
                        {
                            for (int j = 0; j < self.room.physicalObjects[i].Count; j++)
                            {
                                if (self.room.physicalObjects[i][j] is Player player && !player.dead && Custom.DistLess(self.firstChunk.pos, player.firstChunk.pos, 40f))
                                {

                                    if (player.grasps.Length == 0)
                                    {
                                        if (freeGrasp > -1 ||player.spearOnBack != null && player.spearOnBack.spear != null)
                                        {
                                            self.PickUpAndPlaceInInventory(player.spearOnBack.spear);
                                            freeGrasp--;
                                            player.spearOnBack.DropSpear();
                                            UnityEngine.Debug.Log("Thief! Spear on back");
                                            hasStolen = true;
                                        }
                                    }
                                    else
                                    {
                                        for (int k = 0; k < player.grasps.Length; k++)
                                        {
                                            if (player.grasps[k] != null && (freeGrasp > -1 || ValuableItem(player.grasps[k].grabbed.abstractPhysicalObject)))
                                            {
                                                var item = player.grasps[k].grabbed;
                                                player.ReleaseGrasp(k);
                                                self.PickUpAndPlaceInInventory(item);
                                                UnityEngine.Debug.Log("Thief! Items in hands");
                                                freeGrasp--;
                                                hasStolen = true;
                                            }
                                        }
                                    }


                                    if (hasStolen)
                                    {
                                        if (!thiefScav.scaringPlayer.Contains(player.abstractCreature))
                                        {
                                            thiefScav.scaringPlayer.Add(player.abstractCreature);
                                        }
                                        break;
                                    }

                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }

        }

        public delegate float orig_MovementSpeed(Scavenger self);
        public static float Scavenger_MovementSpeed_get(orig_MovementSpeed orig, Scavenger self)
        {
            if (thiefScavs.TryGetValue(self, out var thiefScav) && thiefScav.scaringPlayer.Count > 0)
                return 5f * orig(self);
            return orig(self);
        }


        public static void TryLeaveRoom(ArtificialIntelligence AI, Room room)
        {
            if (AI.creature.abstractAI.destination.room != room.abstractRoom.index)
            {
                return;
            }
            int num = AI.threatTracker.FindMostAttractiveExit();
            if (num > -1 && num < room.abstractRoom.nodes.Length && room.abstractRoom.nodes[num].type == AbstractRoomNode.Type.Exit)
            {
                int num2 = room.world.GetAbstractRoom(room.abstractRoom.connections[num]).ExitIndex(room.abstractRoom.index);
                if (num2 > -1)
                {
                    UnityEngine.Debug.Log("Thief Scav Fleeing!");
                    AI.creature.abstractAI.MigrateTo(new WorldCoordinate(room.abstractRoom.connections[num], -1, -1, num2));
                }
            }
        }

        public static bool ValuableItem(AbstractPhysicalObject type)
        {
            if (type.type == AbstractPhysicalObject.AbstractObjectType.DataPearl || type.type == AbstractPhysicalObject.AbstractObjectType.VultureMask || type.type == AbstractPhysicalObject.AbstractObjectType.ScavengerBomb ||
                type.type == AbstractPhysicalObject.AbstractObjectType.OverseerCarcass || type.type == MoreSlugcatsEnums.AbstractObjectType.SingularityBomb
                || (type is AbstractSpear && ((type as AbstractSpear).explosive || (type as AbstractSpear).electric)))
            {
                return true;
            }
            return false;
        }

        public class ThiefScav
        {
            public List<AbstractCreature> scaringPlayer = new List<AbstractCreature>();
            public ThiefScav(Player scaringPlayer)
            {
                if (scaringPlayer != null)
                    this.scaringPlayer.Add(scaringPlayer.abstractCreature);
            }
        }
    }
}
