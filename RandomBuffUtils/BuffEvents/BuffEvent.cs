using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuffUtils.BuffEvents;
using static RandomBuffUtils.BuffEvents.BuffRegionGateEvent;

namespace RandomBuffUtils
{
    /// <summary>
    /// 管理一些通用的事件
    /// </summary>
    public static partial class BuffEvent
    {

        /// <summary>
        /// 生物被玩家击杀时调用
        /// </summary>
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

        /// <summary>
        /// 玩家进入更换房间调用
        /// </summary>
        public static event NewRoomHandler OnPlayerChangeRoom
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

        /// <summary>
        /// 玩家到达新房间调用
        /// </summary>
        public static event ReachNewRoomHandler OnReachNewRoom
        {
            add => BuffRoomReachEvent.OnRoomReached += value;
            remove => BuffRoomReachEvent.OnRoomReached -= value;
        }

        /// <summary>
        /// 业力门被加载时调用
        /// </summary>
        public static event RegionGateHandler OnGateLoaded
        {
            add => BuffRegionGateEvent.OnGateLoaded += value;
            remove => BuffRegionGateEvent.OnGateLoaded -= value;
        }

        /// <summary>
        /// 业力门被开启时调用
        /// </summary>
        public static event RegionGateHandler OnGateOpened
        {
            add => BuffRegionGateEvent.OnGateOpened += value;
            remove => BuffRegionGateEvent.OnGateOpened -= value;
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

        public delegate void ReachNewRoomHandler(AbstractRoom room);

        public delegate void RegionGateHandler(RegionGateInstance gateInstance);
    }

}
