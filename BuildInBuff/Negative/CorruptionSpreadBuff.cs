using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.SaveData.BuffConfig;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Negative
{
    internal class CorruptionSpreadBuff : Buff<CorruptionSpreadBuff, CorruptionSpreadBuffData>
    {
        public int randomSeed;
        public override BuffID ID => CorruptionSpreadBuffEntry.corruptionSpread;

        public CorruptionSpreadBuff()
        {
            randomSeed = Random.Range(0, 100000);
        }
    }

    internal class CorruptionSpreadBuffData : CountableBuffData
    {
        public override BuffID ID => CorruptionSpreadBuffEntry.corruptionSpread;

        public override int MaxCycleCount => 3;

        [CustomBuffConfigRange(0.33f ,0.1f, 1f)]
        public float SpawnRate { get; set; }
    }

    internal class CorruptionSpreadBuffEntry : IBuffEntry
    {
        static int maxCorruptionRadInt = 8;
        static int minCorruptionRadInt = 4;
        static float maxCorrputionRad = maxCorruptionRadInt * 20f;
        static float minCorrputionRad = minCorruptionRadInt * 20f;


        public static BuffID corruptionSpread = new BuffID("CorruptionSpread", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<CorruptionSpreadBuff, CorruptionSpreadBuffData, CorruptionSpreadBuffEntry>(corruptionSpread);
        }

        public static void HookOn()
        {
            On.Room.ShortCutsReady += Room_ShortCutsReady;
            On.Room.ReadyForAI += Room_ReadyForAI;
        }

        private static void Room_ShortCutsReady(On.Room.orig_ShortCutsReady orig, Room self)
        {
            orig.Invoke(self);
            //看起来香菇的ShortcutReady啥也没干，那么这么做应该没什么问题
            if (self.regionGate != null || self.shelterDoor != null)
            {
                return;
            }

            if (self.updateList.Count(u => u is DaddyCorruption) > 0)
            {
                return;
            }

            int maxCount = Mathf.CeilToInt((self.Width / 40f) * (self.Height / 30f));
            var state = Random.state;
            int hash = self.abstractRoom.name.GetHashCode() + CorruptionSpreadBuff.Instance.randomSeed;
            Random.InitState(hash);

            if (Random.value > CorruptionSpreadBuff.Instance.Data.SpawnRate && !self.abstractRoom.name.Contains("_AI"))
                return;

            addedObject.Add(self, new List<PlacedObject>());

            List<IntVector2> selectList = new List<IntVector2>();

            for (int x = 0; x < self.Width; x++)
            {
                for (int y = 0; y < self.Height; y++)
                {
                    IntVector2 pos = new IntVector2(x, y);
                    var tile = self.GetTile(pos);
                    if (tile.Solid || tile.wormGrass || tile.AnyBeam)
                        continue;

                    bool anySolid = false;
                    for (int i = 0; i < 8; i++)
                    {
                        if (self.GetTile(pos + Custom.eightDirections[i]).Solid)
                        {
                            anySolid = true;
                            break;
                        }
                    }
                    if (!anySolid)
                        continue;

                    bool closeToShortcut = false;
                    foreach (var shortcut in self.shortcutsIndex)
                    {
                        if (Custom.ManhattanDistance(shortcut, pos) < Mathf.CeilToInt(maxCorruptionRadInt * 1.5f))
                        {
                            closeToShortcut = true;
                            break;
                        }
                    }

                    if (!closeToShortcut)
                        selectList.Add(pos);
                }
            }

            if (selectList.Count == 0)
            {
                return;
            }

            var corrpution = new DaddyCorruption(self);
            self.AddObject(corrpution);


            for (int i = 0; i < maxCount; i++)
            {
                float probability = Mathf.Pow(0.8f, i);
                if (Random.value > probability)
                    break;

                var placedObj = new PlacedObject(PlacedObject.Type.Corruption, null);
                placedObj.pos = self.MiddleOfTile(selectList[Random.Range(0, selectList.Count)]);
                (placedObj.data as PlacedObject.ResizableObjectData).handlePos = Custom.RNV() * Mathf.Lerp(minCorrputionRad, maxCorrputionRad, Random.value);
                corrpution.places.Add(placedObj);

                probability = 0.2f;
                if (Random.value < probability)
                {
                    var stuckDaddy = new PlacedObject(PlacedObject.Type.StuckDaddy, null) { pos = placedObj.pos };
                    self.roomSettings.placedObjects.Add(stuckDaddy);
                    addedObject[self].Add(stuckDaddy);
                }
            }

            if(corrpution.places.Count > 1)
            {
                for(int i =0; i< corrpution.places.Count * 3; i++)
                {
                    float probability = Mathf.Pow(0.9f, i + 1);
                    if (Random.value > probability)
                        break;

                    var a = corrpution.places[Random.Range(0, corrpution.places.Count)];
                    corrpution.places.Remove(a);
                    var b = corrpution.places[Random.Range(0, corrpution.places.Count)];
                    corrpution.places.Add(a);

                    var tube = new PlacedObject(PlacedObject.Type.CorruptionTube, null) { pos = a.pos };
                    (tube.data as PlacedObject.ResizableObjectData).handlePos = b.pos - a.pos;

                    self.roomSettings.placedObjects.Add(tube);
                    addedObject[self].Add(tube);
                }
            }
        }

        private static void Room_ReadyForAI(On.Room.orig_ReadyForAI orig, Room self)
        {
            orig.Invoke(self);
            if (addedObject.ContainsKey(self))
            {
                foreach (var obj in addedObject[self])
                    self.roomSettings.placedObjects.Remove(obj);
                addedObject.Remove(self);   
            }
        }

        static Dictionary<Room,List<PlacedObject>> addedObject = new Dictionary<Room, List<PlacedObject>>();
        private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            
        }
    }
}
