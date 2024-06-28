using CustomSaveTx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static MoreSlugcats.MoreSlugcatsEnums;
using static RandomBuffUtils.BuffEvent;
using static RandomBuffUtils.BuffEvents.BuffRegionGateEvent;

namespace RandomBuffUtils.BuffEvents
{
    //TODO : 修复原游戏多次过门重复加载生物的问题

    public static partial class BuffRegionGateEvent
    {
        public static event RegionGateHandler OnGateLoaded;
        public static event RegionGateHandler OnGateOpened;

        public static RegionGateInstance TryGetGateInstance(string region1, string region2, RainWorldGame game)
        {
            region1 = region1.ToUpper();
            region2 = region2.ToUpper();

            foreach (var instance in loadedInstance)
            {
                if (instance.IsThisGate(region1, region2))
                    return instance;
            }

            string[] locks = File.ReadAllLines(AssetManager.ResolveFilePath(string.Concat(new string[]
            {
                "World",
                Path.DirectorySeparatorChar.ToString(),
                "Gates",
                Path.DirectorySeparatorChar.ToString(),
                "locks.txt"
            })));

            var gateRoomName = $"GATE_{region1}_{region2}";
            for (int i = 0; i < locks.Length; i++)
            {
                if (Regex.Split(locks[i], " : ")[0] == gateRoomName)
                {
                    var karmaRequirements = new RegionGate.GateRequirement[2];
                    karmaRequirements[0] = new RegionGate.GateRequirement(Regex.Split(locks[i], " : ")[1].Trim());
                    karmaRequirements[1] = new RegionGate.GateRequirement(Regex.Split(locks[i], " : ")[2].Trim());

                    var unlocked = game.GetStorySession.saveState.deathPersistentSaveData.unlockedGates.Contains(gateRoomName);

                    var result = new RegionGateInstance(gateRoomName, karmaRequirements, unlocked);
                    loadedInstance.Add(result);
                    return result;
                }
            }

            BuffUtils.LogError("BuffRegionGateEvent", $"{gateRoomName} doesnt exist!");
            return null;
        }

        public class  RegionGateInstance
        {
            public string[] BindRegions { get; private set; }

            internal RegionGate.GateRequirement[] _karmaReqs = new RegionGate.GateRequirement[2];
            public RegionGate.GateRequirement KarmaRequirementLeft
            {
                get => _karmaReqs[0];
                set
                {
                    if (value != _karmaReqs[0])
                    {
                        _karmaReqs[0] = value;
                        _gateStateModified = true;
                    }
                }
            }

            public RegionGate.GateRequirement KarmaRequirementRight
            {
                get => _karmaReqs[1];
                set
                {
                    if (value != _karmaReqs[1])
                    {
                        _karmaReqs[1] = value;
                        _gateStateModified = true;
                    }
                }
            }

            bool _unlocked;
            public bool Unlocked
            {
                get => _unlocked;
                set
                {
                    if (value != _unlocked)
                    {
                        _unlocked = value;
                        _gateStateModified = true;
                    }
                }
            }

            public bool EnergyEnoughToOpen;

            internal bool _gateStateModified;

            internal RegionGateInstance(string[] bindRegions, RegionGate.GateRequirement[] requirements, bool unlock)
            {
                BindRegions = bindRegions;
                _karmaReqs = requirements;
                _unlocked = unlock;
            }

            internal RegionGateInstance(string roomName, RegionGate.GateRequirement[] requirements, bool unlock)
            {
                string[] names = roomName.Split('_');
                BindRegions = new string[2] { names[1], names[2] };

                _karmaReqs = requirements;
                _unlocked = unlock;
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;
                if (obj is RegionGateInstance instance)
                {
                    return BindRegions.SequenceEqual(instance.BindRegions);
                }
                return false;
            }

            public bool IsThisGate(string region1, string region2)
            {
                return BindRegions[0] == region1 && BindRegions[1] == region2; ;
            }

            public bool IsThisGate(string roomName)
            {
                string[] names = roomName.Split('_');
                return IsThisGate(names[1], names[2]);
            }

            public override string ToString()
            {
                return $"{BindRegions[0]}_{BindRegions[1]}_{KarmaRequirementLeft.value}_{KarmaRequirementRight.value}_{_unlocked}";
            }

            public void ToString(StringBuilder builder)
            {
                builder.Append(this.ToString());
                builder.Append("|");
            }

            public static RegionGateInstance FromString(string str)
            {
                string[] array = str.Split('_');
                return new RegionGateInstance(new string[] { array[0], array[1] },
                                              new RegionGate.GateRequirement[] { new RegionGate.GateRequirement(array[2]), new RegionGate.GateRequirement(array[3]) },
                                              bool.Parse(array[4]));
            }
        }
    
        public static bool IsFakeGateRoom(Room room)
        {
            return !room.abstractRoom.name.StartsWith("GATE") || room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.FakeGate) > 0f;
        }
    }


    public static partial class BuffRegionGateEvent
    {
        private static RegionGateSaveDataTx saveData;
        internal static List<RegionGateInstance> loadedInstance = new List<RegionGateInstance>();

        internal static void OnEnable()
        {
            DeathPersistentSaveDataRx.AppplyTreatment(saveData = new RegionGateSaveDataTx(null));

            On.RegionGate.ctor += RegionGate_ctor;
            //On.RegionGate.NewWorldLoaded += RegionGate_NewWorldLoaded;
            On.RegionGate.Update += RegionGate_Update;
        }

        private static void RegionGate_Update(On.RegionGate.orig_Update orig, RegionGate self, bool eu)
        {
            RegionGate.Mode origMode = self.mode;
            orig.Invoke(self, eu);

            if(origMode != self.mode)
            {
                BuffUtils.Log("BuffRegionGateEvent", $"{origMode} => {self.mode}");
            }

            if (origMode == RegionGate.Mode.OpeningSide && self.mode == RegionGate.Mode.Closed)
            {
                BuffUtils.Log("BuffRegionGateEvent", $"Trigger OnGateOpened");
                RegionGateInstance gateInstance = CreateInstanceForGate(self);

                OnGateOpened?.SafeInvoke("OnGateOpened", gateInstance);

                if (gateInstance._gateStateModified && !saveData.modifiedGateInstances.Contains(gateInstance))
                {
                    saveData.modifiedGateInstances.Add(gateInstance);
                    BuffUtils.Log("BuffRegionGateEvent", $"add modified instance to record");
                }

                CheckRegionGateInstance(self.room, self, gateInstance);

                int creatureCount = 0;
                foreach(var abRoom in self.room.world.abstractRooms)
                {
                    creatureCount += abRoom.creatures.Count;
                }

                BuffUtils.Log("BuffRegionGateEvent", $"All creature in region : {creatureCount}");
            }
        }

        //public static void Test(RegionGateInstance instance)
        //{
        //    var a = instance.KarmaRequirementLeft;
        //    instance.KarmaRequirementLeft = instance.KarmaRequirementRight;
        //    instance.KarmaRequirementRight = a;
        //    instance.EnergyEnoughToOpen = true;
        //}

        //public static void Test2(RegionGateInstance instance)
        //{
        //    instance.KarmaRequirementLeft = MoreSlugcats.MoreSlugcatsEnums.GateRequirement.OELock;
        //    instance.KarmaRequirementRight = RegionGate.GateRequirement.OneKarma;
        //}

        private static void RegionGate_ctor(On.RegionGate.orig_ctor orig, RegionGate self, Room room)
        {
            orig.Invoke(self, room);

            if (IsFakeGateRoom(room))
                return;

            RegionGateInstance gateInstance = CreateInstanceForGate(self);
            
            OnGateLoaded?.SafeInvoke("OnGateLoaded", gateInstance);

            if (gateInstance._gateStateModified && !saveData.modifiedGateInstances.Contains(gateInstance))
            {
                saveData.modifiedGateInstances.Add(gateInstance);
                BuffUtils.Log("BuffRegionGateEvent", $"add modified instance to record");
            }

            CheckRegionGateInstance(room, self, gateInstance);
        }

        private static RegionGateInstance CreateInstanceForGate(RegionGate gate)
        {
            RegionGateInstance gateInstance = null;
            foreach (var instance in loadedInstance)
            {
                if (instance.IsThisGate(gate.room.abstractRoom.name))
                    gateInstance = instance;
            }
            if (gateInstance == null)
            {
                gateInstance = new RegionGateInstance(gate.room.abstractRoom.name, new RegionGate.GateRequirement[] { gate.karmaRequirements[0], gate.karmaRequirements[1] }, gate.unlocked);
                loadedInstance.Add(gateInstance);
                BuffUtils.Log("BuffRegionGateEvent", $"Create gate instance at : {gate.room.abstractRoom.name}");
            }
            gateInstance.EnergyEnoughToOpen = gate.EnergyEnoughToOpen;
            return gateInstance;
        }

        private static void CheckRegionGateInstance(Room room, RegionGate gate, RegionGateInstance gateInstance)
        {
            if (saveData.modifiedGateInstances.Contains(gateInstance))
            {
                BuffUtils.Log("BuffRegionGateEvent", $"gate.unlocked {gate.unlocked} => {gateInstance.Unlocked}");
                gate.unlocked = gateInstance.Unlocked;

                if (room.game.GetStorySession.saveState.deathPersistentSaveData.unlockedGates == null)
                    room.game.GetStorySession.saveState.deathPersistentSaveData.unlockedGates = new List<string>();
                if (room.game.GetStorySession.saveState.deathPersistentSaveData.unlockedGates.Contains(room.abstractRoom.name) && !gateInstance.Unlocked)
                    room.game.GetStorySession.saveState.deathPersistentSaveData.unlockedGates.Remove(room.abstractRoom.name);
                else if (!room.game.GetStorySession.saveState.deathPersistentSaveData.unlockedGates.Contains(room.abstractRoom.name) && gateInstance.Unlocked)
                    room.game.GetStorySession.saveState.deathPersistentSaveData.unlockedGates.Add(room.abstractRoom.name);

                for (int i = 0; i < 2; i++)
                {
                    gate.karmaRequirements[i] = new RegionGate.GateRequirement(gateInstance._karmaReqs[i].value);
                    gate.karmaGlyphs[i].symbolDirty = true;
                }
            }

            BuffUtils.Log("BuffRegionGateEvent", $"EnergyEnoughToOpen {gate.EnergyEnoughToOpen}, {gateInstance.EnergyEnoughToOpen}");
            if (gate.EnergyEnoughToOpen != gateInstance.EnergyEnoughToOpen)
            {
                BuffUtils.Log("BuffRegionGateEvent", $"EnergyEnoughToOpen {gate.EnergyEnoughToOpen} => {gateInstance.EnergyEnoughToOpen}");
                gate.dontOpen = !gateInstance.EnergyEnoughToOpen;
                room.world.regionState.gatesPassedThrough[room.abstractRoom.gateIndex] = !gateInstance.EnergyEnoughToOpen;
                if (gate is WaterGate waterGate)
                {
                    BuffUtils.Log("BuffRegionGateEvent", $"WaterGate waterLeft : {waterGate.waterLeft}");
                    waterGate.waterLeft = gateInstance.EnergyEnoughToOpen ? 1f : 0f;
                }

                if(gate is ElectricGate eGate)
                {
                    BuffUtils.Log("BuffRegionGateEvent", $"ElectricGate batteryLeft : {eGate.batteryLeft}");
                    eGate.batteryLeft = gateInstance.EnergyEnoughToOpen ? 1f : 0f;
                }
                if (gateInstance.EnergyEnoughToOpen && gate.mode == RegionGate.Mode.Closed)
                    gate.mode = RegionGate.Mode.MiddleClosed;
            }

            gateInstance._gateStateModified = false;
        }
    }

    internal class RegionGateSaveDataTx : DeathPersistentSaveDataTx
    {
        public override string header => "RegionGateSaveData";
        public List<RegionGateInstance> modifiedGateInstances = new List<RegionGateInstance>();

        public RegionGateSaveDataTx(SlugcatStats.Name name) : base(name)
        {
        }

        public override void LoadDatas(string data)
        {
            base.LoadDatas(data);
            BuffRegionGateEvent.loadedInstance.Clear();
            string[] instanceSaves = data.Split('|');
            foreach(var save in instanceSaves)
            {
                if(string.IsNullOrEmpty(save)) continue;
                var res = RegionGateInstance.FromString(save);
                modifiedGateInstances.Add(res);
                loadedInstance.Add(res);
            }
        }

        public override void ClearDataForNewSaveState(SlugcatStats.Name newSlugName)
        {
            base.ClearDataForNewSaveState(newSlugName);
            modifiedGateInstances.Clear();
            BuffRegionGateEvent.loadedInstance.Clear();
        }

        public override string SaveToString(bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            if (saveAsIfPlayerDied | saveAsIfPlayerQuit)
                return origSaveData;

            StringBuilder builder = new StringBuilder();
            foreach(var instance in modifiedGateInstances)
            {
                instance.ToString(builder);
            }
            return builder.ToString();
        }
    }
}
