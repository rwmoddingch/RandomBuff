using BuiltinBuffs.Negative.SephirahMeltdown;
using BuiltinBuffs.Negative.SephirahMeltdown.Conditions;
using BuiltinBuffs.Positive;
using Newtonsoft.Json;
using RandomBuff;
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

        public override Color TextCol => Helper.GetRGBColor(74, 185, 109);

        public override string MissionName => BuffResourceString.Get("Mission_Display_LawnCleaner");

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
        string progressionString;
        bool requestClear;
        bool requestProgression;

        public WormGrassWipeOutCondition()
        {
            maxConditionCycle = 1;
        }

        public override string InRangeDisplayName()
        {
            return BuffResourceString.Get("InRangeDisplayName_WormGrassWipeOutCondition");
        }

        public override string InRangeDisplayProgress()
        {
            return $"[{finishedRooms.Count} / {roomRequirements}]{progressionString}";
        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            roomRequirements = Random.Range(2, 5);
            return ConditionState.Ok_NoMore;
        }

        public override void HookOn()
        {
            On.Room.Loaded += Room_Loaded;
        }

        public override void InGameUpdate(RainWorldGame game)
        {
            base.InGameUpdate(game);

            if(requestProgression)
            {
                requestClear = false;
                requestProgression = false;
                onLabelRefresh?.Invoke(this);
            }
            if (requestClear)
            {
                requestClear = false;
                progressionString = string.Empty;
                onLabelRefresh?.Invoke(this);
            }
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


        public void NoticeRoomFinished(Room room)
        {
            if (finishedRooms.Contains(room.abstractRoom.name))
                return;
            finishedRooms.Add(room.abstractRoom.name);
            if (finishedRooms.Count >= roomRequirements)
                Finished = true;
            onLabelRefresh?.Invoke(this);
        }

        public void RequestClearProgressionDisplay(bool forceClear = false)
        {
            if (forceClear)
            {
                requestProgression = false;
            }
            requestClear = true;
        }

        public void ReportProgression(string progressionString)
        {
            this.progressionString = progressionString;
            requestProgression = true;
        }
    }

    internal class RoomWormGrassWipeCheck : UpdatableAndDeletable, INoticeWormGrassWipedOut
    {
        bool lastViewed;
        public RoomWormGrassWipeCheck(Room room)
        {
            this.room = room;
            BuffUtils.Log("RoomWormGrassWipeCheck", "ctor");
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            bool viewed = room.BeingViewed;
            if (lastViewed && !viewed)
            {
                (BuffPoolManager.Instance.GameSetting.conditions.Find(c => c is WormGrassWipeOutCondition) as WormGrassWipeOutCondition).RequestClearProgressionDisplay();
            }
        }

        public void NoticeWormGrassWipeProgression(List<int> progression, List<int> total)
        {
            if (!room.BeingViewed)
                return;

            StringBuilder builder = new StringBuilder();
            builder.Append('(');
            for(int i = 0;i < total.Count;i++)
            {
                builder.Append(progression[i]).Append('/').Append(total[i]);
                if (i < total.Count - 1)
                    builder.Append("  ");
            }
            builder.Append(')');
            (BuffPoolManager.Instance.GameSetting.conditions.Find(c => c is WormGrassWipeOutCondition) as WormGrassWipeOutCondition).ReportProgression(builder.ToString());
        }

        public void WormGrassWipedOut(WormGrass wormGrass)
        {
            if (slatedForDeletetion)
                return;

            BuffUtils.Log("RoomWormGrassWipeCheck", $"{room.updateList.Count(u => (u is WormGrass) && !u.slatedForDeletetion)}");

            if (room.updateList.Count(u => (u is WormGrass) && !u.slatedForDeletetion) == 0)
            {
                var condition = (BuffPoolManager.Instance.GameSetting.conditions.Find(c => c is WormGrassWipeOutCondition) as WormGrassWipeOutCondition);
                condition.NoticeRoomFinished(room);
                condition.RequestClearProgressionDisplay(true);
            }
            Destroy();
        }
    }
}
