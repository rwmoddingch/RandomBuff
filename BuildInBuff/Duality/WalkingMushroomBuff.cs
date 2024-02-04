
using RWCustom;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Duality
{
    internal class WalkingMushroomBuff : Buff<WalkingMushroomBuff, WalkingMushroomBuffData>
    {
        public override BuffID ID => WalkingMushroomIBuffEntry.WalkingMushroomBuffID;
    }

    internal class WalkingMushroomBuffData : BuffData
    {
        public override BuffID ID => WalkingMushroomIBuffEntry.WalkingMushroomBuffID;
    }

    internal class WalkingMushroomIBuffEntry : IBuffEntry
    {
        public static BuffID WalkingMushroomBuffID = new BuffID("WalkingMushroom", true);

        public static ConditionalWeakTable<Mushroom, MushroomModule> mushroomModules = new ConditionalWeakTable<Mushroom, MushroomModule>();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<WalkingMushroomBuff, WalkingMushroomBuffData, WalkingMushroomIBuffEntry>(WalkingMushroomBuffID);
        }

        public static void HookOn()
        {
            On.Mushroom.ctor += Mushroom_ctor;
            On.Mushroom.Update += Mushroom_Update;
        }

        private static void Mushroom_Update(On.Mushroom.orig_Update orig, Mushroom self, bool eu)
        {
            orig.Invoke(self, eu);
            if(mushroomModules.TryGetValue(self, out var module))
            {
                module.Update(self);
            }
        }

        private static void Mushroom_ctor(On.Mushroom.orig_ctor orig, Mushroom self, AbstractPhysicalObject abstractPhysicalObject)
        {
            orig.Invoke(self, abstractPhysicalObject);
            if(!mushroomModules.TryGetValue(self, out var _))
            {
                mushroomModules.Add(self, new MushroomModule(self));
            }
        }
    }

    public class MushroomModule
    {
        public WeakReference<Mushroom> mushroomRef;

        int lastPathFindCounter;
        MushroomPathFinder pathFinder;

        List<IntVector2> idealPath = new List<IntVector2>();
        IntVector2? nextCoord = null;

        public MushroomModule(Mushroom mushroom) 
        {
            mushroomRef = new WeakReference<Mushroom>(mushroom);
        }

        public void Update(Mushroom self)
        {
            if (self.grabbedBy != null && self.grabbedBy.Count > 0)//被抓住的时候不进行额外行为
            {
                if (pathFinder != null)
                    pathFinder = null;
                if (idealPath != null && idealPath.Count > 0)
                    idealPath.Clear();
                nextCoord = null;
                return;
            }

            if (lastPathFindCounter > 0)
                lastPathFindCounter--;

            if(lastPathFindCounter == 0 && self.room.aimap != null)
            {
                FindNewGoalPos(self);
            }

            if(pathFinder != null)
            {
                PathFinderUpdate(self);
            }

            if(idealPath != null && idealPath.Count > 0)
            {
                UpdateNextPath(self);
            }

            if(nextCoord != null)
            {
                MoveUpdate(self);
            }
        }

        void FindNewGoalPos(Mushroom self)
        {
            IntVector2 goal = new IntVector2(Random.Range(0, self.room.Width), Random.Range(0, self.room.Height));
            IntVector2 start = self.room.GetTilePosition(self.firstChunk.pos);

            int i = 0;
            for (i = 0;i < 1000; i++)
            {
                IntVector2 newGoal = new IntVector2(Random.Range(0, self.room.Width), Random.Range(0, self.room.Height));
                if (start == newGoal)
                    continue;

                if (newGoal.y == 0)
                    continue;

                if (self.room.GetTile(newGoal.x, newGoal.y - 1).Solid && !self.room.GetTile(newGoal).Solid)
                {
                    goal = newGoal;
                    break;
                }
            }
            if(i == 1000)
            {
                lastPathFindCounter = 400;
                return;
            }

            pathFinder = new MushroomPathFinder(start, goal, self.room.aimap);
            lastPathFindCounter = 400;

            nextCoord = null;
            idealPath.Clear();
            BuffPlugin.Log($"{self} find new goal pos {goal}:{self.room.GetTile(goal).Terrain}");
        }

        void PathFinderUpdate(Mushroom self)
        {
            //EmgTxCustom.Log($"{self} PathFinderUpdate {pathFinder.status}");
            if (pathFinder.status == -1)
            {
                lastPathFindCounter = 0;
                pathFinder = null;
                return;
            }
            if (pathFinder.status == 0)
                pathFinder.Update();
            if(pathFinder.status == 1)
            {
                idealPath = pathFinder.path;
                nextCoord = null;
                pathFinder = null;
            }    
        }

        void UpdateNextPath(Mushroom self)
        {
            IntVector2 currentCoord = self.room.GetTilePosition(self.firstChunk.pos);
            IntVector2? closetsCoord = null;

            int index = 0;
            foreach(var coord in idealPath)
            {
                if (!self.room.VisualContact(currentCoord, coord))
                    continue;
                if (closetsCoord == null)
                    closetsCoord = coord;
                else
                {
                    if (Custom.ManhattanDistance(currentCoord, closetsCoord.Value) > Custom.ManhattanDistance(currentCoord, coord))
                    {
                        closetsCoord = coord;
                        index = idealPath.IndexOf(coord);
                    }
                }
            }

            if (closetsCoord == null)
            {
                //EmgTxCustom.Log($"{self} no ideal next coord");
                return;
            }
                

            if(currentCoord == closetsCoord && index < idealPath.Count - 1)
            {
                closetsCoord = idealPath[index + 1];
            }
            nextCoord = closetsCoord;
            //EmgTxCustom.Log($"{self} {currentCoord} -> {nextCoord},{index},{idealPath.Count}");
        }
    
        void MoveUpdate(Mushroom self)
        {
            if(nextCoord == null) 
                return;
            IntVector2 currentCoord = self.room.GetTilePosition(self.firstChunk.pos);
            Vector2 delta = (nextCoord.Value - currentCoord).ToVector2().normalized;
            self.firstChunk.vel += delta * 5f;
            self.firstChunk.vel *= 0.5f;
            self.firstChunk.vel += self.room.gravity * Vector2.up * 0.5f;

            if (self.room.GetTile(currentCoord + new IntVector2(delta.x > 0 ? 1 : -1, 0)).Solid)
                self.firstChunk.vel += Vector2.up * self.room.gravity * 1.2f;

            if(self.growPos != null)
            {
                self.growPos = self.firstChunk.pos + Vector2.down * 20f;
            }
        }
    }

    public class MushroomPathFinder : QuickPathFinder
    {
        public MushroomPathFinder(IntVector2 start, IntVector2 goal, AImap map) : base(start, goal, map, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.GreenLizard))
        {
        }
    }
}
