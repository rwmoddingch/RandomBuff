using CustomSaveTx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuffUtils.BuffEvents
{
    public static class BuffRoomReachEvent
    {
        /// <summary>
        /// 玩家初次到达某房间时触发，包括业力门和避难所房间
        /// </summary>
        public static event Action<AbstractRoom> OnRoomReached;

        static BuffRoomReachSaveDataTx saveData;

        internal static void OnEnable()
        {
            DeathPersistentSaveDataRx.AppplyTreatment(saveData = new BuffRoomReachSaveDataTx(null));

            On.Player.ctor += Player_ctor;
            On.Player.SpitOutOfShortCut += Player_SpitOutOfShortCut;
        }

        private static void Player_SpitOutOfShortCut(On.Player.orig_SpitOutOfShortCut orig, Player self, RWCustom.IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            orig.Invoke(self, pos, newRoom ,spitOutAllSticks);
            OnEnterRoom(newRoom.abstractRoom);
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig.Invoke(self, abstractCreature, world);
            OnEnterRoom(self.room.abstractRoom);
        }

        static void OnEnterRoom(AbstractRoom room)
        {
            if (!saveData.IsRoomReached(room))
            {
                saveData.ReachRoom(room);
                OnRoomReached?.Invoke(room);

                BuffUtils.Log("BuffRoomReachEvent", $"Reach room {room.name} for the first time");
            }
        }

        /// <summary>
        /// 获取所有到达过的房间。请尽量不要高频使用该方法
        /// </summary>
        /// <returns></returns>
        public static string[] GetReachedRoomNames()
        {
            return saveData.reachedRooms.ToArray();
        }
    }

    internal class BuffRoomReachSaveDataTx : DeathPersistentSaveDataTx
    {
        public override string header => "BuffRoomReachSaveData";
        public List<string> reachedRooms = new List<string>();

        public BuffRoomReachSaveDataTx(SlugcatStats.Name name) : base(name)
        {
        }

        public override void LoadDatas(string data)
        {
            base.LoadDatas(data);

            if (!string.IsNullOrEmpty(data))
            {
                string decompressedData = BuffUtils.Decompress(data);
                string[] rooms = decompressedData.Split('|');
                reachedRooms.AddRange(rooms);
            }
        }

        public override void ClearDataForNewSaveState(SlugcatStats.Name newSlugName)
        {
            base.ClearDataForNewSaveState(newSlugName);
            reachedRooms.Clear();
        }

        public override string SaveToString(bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            if (saveAsIfPlayerDied | saveAsIfPlayerQuit) 
                return origSaveData;

            if (reachedRooms.Count == 0)
                return "";

            var builder = new StringBuilder();
            foreach(var room in reachedRooms)
            {
                builder.Append(room);
                builder.Append('|');
            }
            return BuffUtils.Compress(builder.ToString());
        }

        public bool IsRoomReached(AbstractRoom room)
        {
            return reachedRooms.Contains(room.name);
        }

        public void ReachRoom(AbstractRoom room)
        {
            reachedRooms.Add(room.name);
        }
    }
}
