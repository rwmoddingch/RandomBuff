using BuiltinBuffs.Negative.SephirahMeltdown;
using BuiltinBuffs.Negative.SephirahMeltdown.Conditions;
using BuiltinBuffs.Positive;
using Newtonsoft.Json;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuff.Core.SaveData;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Missions
{
    internal class LawnCleanerMission : Mission, IMissionEntry
    {
        public static MissionID lawnCleanerMissionID = new MissionID("LawnCleaner", true);
        public override MissionID ID => lawnCleanerMissionID;

        public override SlugcatStats.Name BindSlug => null;

        public override Color TextCol => Color.white;

        public override string MissionName => "LAWN CLEANER";

        public LawnCleanerMission()
        {
            gameSetting = new GameSetting(BindSlug, startPos: "LF_S02")
            {
                conditions = new List<Condition>()
                {
                    new WormGrassWipeOutCondition(){roomRequirements = 3 }
                }
            };
            startBuffSet.Add(FlameThrowerBuffEntry.flameThrowerBuffID);
            startBuffSet.Add(FlamePurificationBuffEntry.flamePurificationBuffID);
        }

        public void RegisterMission()
        {
            BuffRegister.RegisterCondition<WormGrassWipeOutCondition>(WormGrassWipeOutCondition.wormGrassWipeOutConditionID, "Wipe out wormgrass", true);

            MissionRegister.RegisterMission(lawnCleanerMissionID, new LawnCleanerMission());
        }
    }

    internal class WormGrassWipeOutCondition : IntervalCondition
    {
        public static ConditionID wormGrassWipeOutConditionID = new ConditionID("WormGrassWipeOutCondition", true);
        public override ConditionID ID => wormGrassWipeOutConditionID;

        public override int Exp => 300;

        [JsonProperty]
        public int roomRequirements;

        public List<string> finishedRooms = new List<string>();

        public WormGrassWipeOutCondition()
        {
            maxConditionCycle = 1;
        }

        public override string InRangeDisplayName()
        {
            return "Wipe out all wormgrass in required rooms";
        }

        public override string InRangeDisplayProgress()
        {
            return $"{finishedRooms.Count} / {roomRequirements}";
        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> sameConditions)
        {
            roomRequirements = Random.Range(2, 5);
            return ConditionState.Ok_NoMore;
        }

        public override void EnterGame(RainWorldGame game)
        {
            base.EnterGame(game);
            On.Room.Loaded += Room_Loaded;
            BuffUtils.Log("WormGrassWipeOutCondition", $"EnterGame");
        }

        private void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            orig.Invoke(self);
            BuffUtils.Log("WormGrassWipeOutCondition", $"wormgrass count : {self.updateList.Count(u => u is WormGrass)}");
            if(self.updateList.Count(u => u is WormGrass) > 0)
            {
                self.AddObject(new RoomWormGrassWipeCheck(self));
            }
        }

        public override void SessionEnd(SaveState save)
        {
            base.SessionEnd(save);
            On.Room.Loaded -= Room_Loaded;
        }

        public void NoticeRoomFinished(Room room)
        {
            if (finishedRooms.Contains(room.abstractRoom.name))
                return;
            finishedRooms.Add(room.abstractRoom.name);
            if (finishedRooms.Count >= roomRequirements)
                Finished = true;
            onLabelRefresh?.Invoke(this);
        }
    }

    internal class RoomWormGrassWipeCheck : UpdatableAndDeletable, INoticeWormGrassWipedOut
    {
        public RoomWormGrassWipeCheck(Room room)
        {
            this.room = room;
            BuffUtils.Log("RoomWormGrassWipeCheck", "ctor");
        }

        public void WormGrassWipedOut(WormGrass wormGrass)
        {
            if (slatedForDeletetion)
                return;

            BuffUtils.Log("RoomWormGrassWipeCheck", $"{room.updateList.Count(u => (u is WormGrass) && !u.slatedForDeletetion)}");

            if (room.updateList.Count(u => (u is WormGrass) && !u.slatedForDeletetion) == 0)
            {
                (BuffPoolManager.Instance.GameSetting.conditions.Find(c => c is WormGrassWipeOutCondition) as WormGrassWipeOutCondition).NoticeRoomFinished(room);
            }
            Destroy();
        }
    }
}
