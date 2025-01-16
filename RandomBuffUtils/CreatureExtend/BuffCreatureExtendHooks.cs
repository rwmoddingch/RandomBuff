using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuffUtils.CreatureExtend
{
    internal static class BuffCreatureExtendHooks
    {
        public static void HookOn()
        {


            On.ProcessManager.PreSwitchMainProcess += ProcessManager_PreSwitchMainProcess;

            On.DevInterface.MapPage.CreatureVis.CritString += CreatureVis_CritString;
            On.DevInterface.MapPage.CreatureVis.CritCol += CreatureVis_CritCol;

            On.AbstractCreature.Realize += AbstractCreature_Realize;
            On.AbstractCreature.InitiateAI += AbstractCreature_InitiateAI;
            On.AbstractCreature.ctor += AbstractCreature_ctor;

            On.RoomRealizer.GetCreaturePerformanceEstimation += RoomRealizer_GetCreaturePerformanceEstimation;
            On.Player.Grabability += Player_Grabability;
        }

        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            var result = orig.Invoke(self, obj);
            if(obj is Creature creature && BuffCreatureExtend.typeValue2RegisterMapping.TryGetValue(creature.abstractCreature.creatureTemplate.type.value, out var register) && register.currentlyEnabled)
            {
                result = register.MyRealizeCreatureInfo.grabability;
            }
            return result;
        }

        private static float RoomRealizer_GetCreaturePerformanceEstimation(On.RoomRealizer.orig_GetCreaturePerformanceEstimation orig, AbstractCreature crit)
        {
            if (BuffCreatureExtend.typeValue2RegisterMapping.TryGetValue(crit.creatureTemplate.type.value, out var register) && register.currentlyEnabled)
            {
                return register.MyRealizeCreatureInfo.loadPerformanceCost;
            }
            return orig(crit);
        }

        private static void AbstractCreature_ctor(On.AbstractCreature.orig_ctor orig, AbstractCreature self, World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
        {
            if (BuffCreatureExtend.typeValue2RegisterMapping.TryGetValue(self.creatureTemplate.type.value, out var register) && register.currentlyEnabled)
            {
                self.state = register.CreateState(self);

                AbstractCreatureAI abstractAI = register.InitiateAbstractAI(self);

                if (creatureTemplate.AI && abstractAI != null)
                {
                    self.abstractAI = abstractAI;

                    if (pos.abstractNode > -1 && pos.abstractNode < self.Room.nodes.Length
                        && self.Room.nodes[pos.abstractNode].type == AbstractRoomNode.Type.Den && !pos.TileDefined)
                        self.abstractAI.denPosition = pos;
                }
                else if (abstractAI != null)
                {
                    Debug.LogError($"Register {register.typeValue} InitiateAbstractAI returned a non-null object but template.AI is false");
                }
            }
        }

        private static void AbstractCreature_InitiateAI(On.AbstractCreature.orig_InitiateAI orig, AbstractCreature self)
        {
            orig(self);

            if (BuffCreatureExtend.typeValue2RegisterMapping.TryGetValue(self.creatureTemplate.type.value, out var register) && register.currentlyEnabled)
            {
                if (self.abstractAI != null && self.creatureTemplate.AI)
                {
                    self.abstractAI.RealAI = register.InitiateAI(self) ?? throw new InvalidOperationException($"Register {register.typeValue} InitiateAI returned null but template.AI was true!");
                }
            }
        }

        private static void AbstractCreature_Realize(On.AbstractCreature.orig_Realize orig, AbstractCreature self)
        {
            if (self.realizedCreature == null && BuffCreatureExtend.typeValue2RegisterMapping.TryGetValue(self.creatureTemplate.type.value, out var register) && register.currentlyEnabled)
            {
                self.realizedObject = register.Realize(self) ?? throw new InvalidOperationException($"Regsiter {register.typeValue} Realize return null!");

                self.InitiateAI();

                foreach (var stuck in self.stuckObjects)
                {
                    if (stuck.A.realizedObject == null)
                    {
                        stuck.A.Realize();
                    }
                    if (stuck.B.realizedObject == null)
                    {
                        stuck.B.Realize();
                    }
                }
            }

            orig.Invoke(self);
        }

        private static Color CreatureVis_CritCol(On.DevInterface.MapPage.CreatureVis.orig_CritCol orig, AbstractCreature crit)
        {
            if(BuffCreatureExtend.typeValue2RegisterMapping.TryGetValue(crit.creatureTemplate.type.value, out var register) && register.currentlyEnabled)
            {
                if (crit.InDen && UnityEngine.Random.value < 0.5f)
                    return new(0.5f, 0.5f, 0.5f);
                return register.MyDevMapInfo.color;
            }
            return orig.Invoke(crit);
        }

        private static string CreatureVis_CritString(On.DevInterface.MapPage.CreatureVis.orig_CritString orig, AbstractCreature crit)
        {
            if (BuffCreatureExtend.typeValue2RegisterMapping.TryGetValue(crit.creatureTemplate.type.value, out var register) && register.currentlyEnabled)
            {
                return register.MyDevMapInfo.name;
            }
            return orig.Invoke(crit);
        }

        private static void ProcessManager_PreSwitchMainProcess(On.ProcessManager.orig_PreSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if(self.currentMainLoop.ID == ProcessManager.ProcessID.Game)
            {
                BuffCreatureExtend.ActuallyDisableRegisters();
            }
            orig.Invoke(self, ID);
        }
    }
}
