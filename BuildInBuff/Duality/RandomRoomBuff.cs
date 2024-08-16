using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Duality
{
    internal class RandomRoomBuff : Buff<RandomRoomBuff, RandomRoomBuffData>
    {
        public override BuffID ID => RandomRoomBuffEntry.randomRoomBuffID;

        int randomizeDelay;
        bool randomized;
        List<AbstractRoomRepresent> activeRoomRepresents = new List<AbstractRoomRepresent>();

        public override void Update(RainWorldGame game)
        {
            if (randomizeDelay > 0)
                randomizeDelay--;
            else
            {
                if (!randomized)
                {
                    RandomizeRoomConnections(game.world);
                    randomized = true;
                }
            }
        }

        public override void Destroy()
        {
            try
            {
                foreach (var represent in activeRoomRepresents)
                {
                    if (represent.room.connections == null || represent.origConnections == null)
                        continue;

                    for (int i = 0; i < represent.room.connections.Length; i++)
                    {
                        represent.room.connections[i] = represent.origConnections[i].index;
                    }
                }
            }
            catch (Exception e)
            {
                BuffPlugin.LogError(e);
            }
            finally
            {
                activeRoomRepresents.Clear();
                randomized = false;
            }
        }

        public void ChangeRegion()
        {
            activeRoomRepresents.Clear();
            randomizeDelay = 40;
            randomized = false;
            BuffUtils.Log("RandomRoom", "ChangeRegion");
        }

        void RandomizeRoomConnections(World world)
        {
            //List<AbstractRoomRepresent> roomRepresents = new List<AbstractRoomRepresent>();
            List<AbstractRoomRepresent> roomForConnect = new List<AbstractRoomRepresent>();
            if (activeRoomRepresents.Count > 0)
                activeRoomRepresents.Clear();

            foreach (var room in world.abstractRooms)
            {
                if (room.connections.Length == 0)
                    continue;

                var represet = new AbstractRoomRepresent(room);
                activeRoomRepresents.Add(represet);
                roomForConnect.Add(represet);

                for(int i = 0;i < room.connections.Length; i++)
                {
                    represet.origConnections[i] = world.GetAbstractRoom(room.connections[i]);
                }
            }

            //排除业力门相关、单口房间、避难所链接
            for (int m = activeRoomRepresents.Count - 1; m >= 0; m--)
            {
                var matchRepreset = activeRoomRepresents[m];
                if (matchRepreset.room.gate || matchRepreset.room.shelter)
                {
                    for (int i = 0; i < matchRepreset.room.connections.Length; i++)
                    {
                        var room = world.GetAbstractRoom(matchRepreset.room.connections[i]);

                        if (room == null)//非该区域房间
                            continue;
                        else
                        {
                            var newRep = AbstractRoomRepresent.GetRepresentFromList(activeRoomRepresents, room);
                            //TODO:这是一个简易修改
                            if (newRep != null)
                            {
                                for (int j = 0; j < room.connections.Length; j++)
                                {
                                    if (world.GetAbstractRoom(room.connections[j]) == matchRepreset.room)
                                        newRep.connectionRepresents[j] = matchRepreset.room;
                                }

                                if (newRep.CurrentEmptyConnectionIndex == -1)
                                    roomForConnect.Remove(newRep);
                            }
                        }
                    }
                    activeRoomRepresents.Remove(matchRepreset);
                    roomForConnect.Remove(matchRepreset);
                }
                if (matchRepreset.room.connections.Length == 1)//单链接房间
                {
                    var room = world.GetAbstractRoom(matchRepreset.room.connections[0]);
                    if (room == null)//非该区域房间
                        continue;
                    else
                    {
                        var newRep = AbstractRoomRepresent.GetRepresentFromList(activeRoomRepresents, room);
                        for (int j = 0; j < room.connections.Length; j++)
                        {
                            if (world.GetAbstractRoom(room.connections[j]) == matchRepreset.room)
                                newRep.connectionRepresents[j] = matchRepreset.room;
                        }
                        if (newRep.CurrentEmptyConnectionIndex == -1)
                            roomForConnect.Remove(newRep);
                    }
                    activeRoomRepresents.Remove(matchRepreset);
                    roomForConnect.Remove(matchRepreset);
                }
            }

            foreach (var roomRepresent in activeRoomRepresents)//随机链接
            {
                int index = -1;
                while ((index = roomRepresent.CurrentEmptyConnectionIndex) != -1 && roomForConnect.Count > 0)
                {
                    List<int> randomIndex = new List<int>();
                    for (int i = 0; i < roomForConnect.Count; i++)
                        randomIndex.Add(i);
                    randomIndex.Remove(roomForConnect.IndexOf(roomRepresent));
                    if (randomIndex.Count == 0)
                        break;

                    AbstractRoomRepresent randomRoom = roomForConnect[randomIndex[Random.Range(0, randomIndex.Count)]];

                    BuffUtils.Log($"ConnectTest", $"Connect room : {roomRepresent.room.name}_{index}_{roomRepresent.connectionRepresents.Count()} <-> {randomRoom.room.name}_{randomRoom.CurrentEmptyConnectionIndex}_{randomRoom.connectionRepresents.Count()}");

                    roomRepresent.connectionRepresents[index] = randomRoom.room;
                    randomRoom.connectionRepresents[randomRoom.CurrentEmptyConnectionIndex] = roomRepresent.room;


                    if (roomRepresent.CurrentEmptyConnectionIndex == -1)
                        roomForConnect.Remove(roomRepresent);
                    if (randomRoom.CurrentEmptyConnectionIndex == -1)
                        roomForConnect.Remove(randomRoom);
                }
            }

            var builder = new StringBuilder("\n");
            foreach (var roomRepresent in activeRoomRepresents)//debug输出链接
            {
                builder.Append($"{roomRepresent.room.name} : ");
                foreach (var connection in roomRepresent.connectionRepresents)
                {


                    builder.Append(connection == null ? "NONE" : connection.name);
                    builder.Append(" ");
                }
                builder.Append("\n");

                for (int i = 0; i < roomRepresent.connectionRepresents.Length; i++)
                {
                    if (roomRepresent.connectionRepresents[i] == null)
                        continue;
                    roomRepresent.room.connections[i] = roomRepresent.connectionRepresents[i].index;
                }
            }
            BuffUtils.Log("ConnectionTest", builder.ToString());
        }

        class AbstractRoomRepresent
        {
            public AbstractRoom room;
            public AbstractRoom[] connectionRepresents;
            public AbstractRoom[] origConnections;

            public int CurrentEmptyConnectionIndex
            {
                get
                {
                    for (int i = 0; i < connectionRepresents.Length; i++)
                        if (connectionRepresents[i] == null)
                            return i;
                    return -1;
                }
            }

            public AbstractRoomRepresent(AbstractRoom room)
            {
                this.room = room;
                connectionRepresents = new AbstractRoom[room.connections.Length];
                origConnections = new AbstractRoom[room.connections.Length];
            }

            public static AbstractRoomRepresent GetRepresentFromList(List<AbstractRoomRepresent> list, AbstractRoom room)
            {
                foreach (var rr in list)
                    if (rr.room == room)
                        return rr;
                return null;
            }
        }
    }

    internal class RandomRoomBuffData : CountableBuffData
    {
        public override BuffID ID => RandomRoomBuffEntry.randomRoomBuffID;

        public override int MaxCycleCount => 10;
    }

    internal class RandomRoomBuffEntry : IBuffEntry
    {
        public static BuffID randomRoomBuffID = new BuffID("RandomRoom", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<RandomRoomBuff, RandomRoomBuffData, RandomRoomBuffEntry>(randomRoomBuffID);
        }

        public static void HookOn()
        {
            //On.WorldLoader.CreatingWorld += WorldLoader_CreatingWorld;
            On.OverWorld.WorldLoaded += OverWorld_WorldLoaded;
        }

        private static void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld self)
        {
            orig.Invoke(self);
            RandomRoomBuff.Instance.ChangeRegion();
        }
    }
}
