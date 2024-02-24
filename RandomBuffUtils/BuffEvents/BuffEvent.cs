using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuffUtils
{
    /// <summary>
    /// 管理一些通用的事件
    /// </summary>
    public static partial class BuffEvent
    {
        public static event CreatureKilledHandler OnCreatureKilled
        {
            add
            {
                if (onCreatureKilled == null || onCreatureKilled.GetInvocationList().Length == 0)
                    On.PlayerSessionRecord.AddKill += PlayerSessionRecord_AddKill;
                onCreatureKilled += value;
            }
            remove
            {
                if(onCreatureKilled.GetInvocationList().Length == 1)
                    On.PlayerSessionRecord.AddKill -= PlayerSessionRecord_AddKill;
                onCreatureKilled -= value;
            }

        }
        public static event NewRoomHandler OnPlayerNewRoom
        {
            add
            {
                if (onNewRoom == null || onNewRoom.GetInvocationList().Length == 0)
                    On.PlayerSessionRecord.AddKill += PlayerSessionRecord_AddKill;
                onNewRoom += value;
            }
            remove
            {
                if (onNewRoom.GetInvocationList().Length == 1)
                    On.Player.NewRoom += Player_NewRoom;
                onNewRoom -= value;
            }
        }

 
    }

    /// <summary>
    /// Hook函数
    /// </summary>
    public static partial class BuffEvent
    {
        private static void PlayerSessionRecord_AddKill(On.PlayerSessionRecord.orig_AddKill orig, PlayerSessionRecord self, Creature victim)
        {
            orig(self, victim);
            onCreatureKilled.Invoke(victim, self.playerNumber);
        }

        private static void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            orig(self, newRoom);
            onNewRoom.Invoke(newRoom.abstractRoom.name);
        }
    }

    public static partial class BuffEvent
    {
        private static CreatureKilledHandler onCreatureKilled;
        public delegate void CreatureKilledHandler(Creature creature, int playerNumber);

        private static NewRoomHandler onNewRoom;
        public delegate void NewRoomHandler(string roomName);
    }

}
