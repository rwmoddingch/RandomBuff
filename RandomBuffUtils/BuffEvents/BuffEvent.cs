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
                {
                    On.PlayerSessionRecord.AddKill += PlayerSessionRecord_AddKill;
                }

                onCreatureKilled += value;
            }
            remove
            {
                onCreatureKilled -= value;
                if ((onCreatureKilled?.GetInvocationList()?.Length ?? 0) == 0)
                    On.PlayerSessionRecord.AddKill -= PlayerSessionRecord_AddKill;
            }

        }

        /// <summary>
        /// 玩家更换房间调用
        /// </summary>
        public static event NewRoomHandler OnPlayerChangeRoom
        {
            add
            {
                if (onNewRoom == null || onNewRoom.GetInvocationList().Length == 0)
                    On.Player.NewRoom += Player_NewRoom;
                onNewRoom += value;
            }
            remove
            {
                onNewRoom -= value;
                if (onNewRoom == null || onNewRoom.GetInvocationList().Length == 0)
                    On.Player.NewRoom -= Player_NewRoom;
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

        /// <summary>
        /// 当有任何输入时调用
        /// 请务必及时取消减少监听时间
        /// </summary>
        public static event KeyDownHandler OnAnyKeyDown
        {
            add => BuffInput.OnAnyKeyDown += value;
            remove => BuffInput.OnAnyKeyDown -= value;
        }


        /// <summary>
        /// 当成就达成时调用
        /// </summary>
        public static event AchievementCompleteHandler OnAchievementCompleted
        {
            add
            {
                if (onAchievementCompleted == null || onAchievementCompleted.GetInvocationList().Length == 0)
                    On.WinState.CycleCompleted += WinState_CycleCompleted;
                onAchievementCompleted += value;
            }
            remove
            {
                onAchievementCompleted -= value;
                if (onAchievementCompleted == null || onAchievementCompleted.GetInvocationList().Length == 0)
                    On.WinState.CycleCompleted -= WinState_CycleCompleted;
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
            onCreatureKilled.SafeInvoke("onCreatureKilled", victim, self.playerNumber);
        
        }

        private static void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            orig(self, newRoom);
            onNewRoom.SafeInvoke("onNewRoom", newRoom.abstractRoom.name);
        }

        private static void WinState_CycleCompleted(On.WinState.orig_CycleCompleted orig, WinState self, RainWorldGame game)
        {
            orig(self, game);
            var finished = self.endgameTrackers.Where(i => i.GoalFullfilled || !i.GoalAlreadyFullfilled).Select(i => i.ID);
            var unFinished = self.endgameTrackers.Where(i => !i.GoalFullfilled || i.GoalAlreadyFullfilled).Select(i => i.ID);

            if (finished.Any() || unFinished.Any())
            {
                onAchievementCompleted.SafeInvoke("onAchievementCompleted", finished.ToList(), unFinished.ToList());
            }
        }
    }

    public static partial class BuffEvent
    {
        public static event ExtraDialogBoxHandler OnExtraDialogsCreated
        {
            add => BuffExtraDialogBoxEvent.OnExtraDialogsCreated += value;
            remove => BuffExtraDialogBoxEvent.OnExtraDialogsCreated -= value;
        }
    }

    public static partial class BuffEvent
    {
        private static CreatureKilledHandler onCreatureKilled;
        public delegate void CreatureKilledHandler(Creature creature, int playerNumber);

        private static NewRoomHandler onNewRoom;
        public delegate void NewRoomHandler(string roomName);

        private static AchievementCompleteHandler onAchievementCompleted;
        public delegate void AchievementCompleteHandler(List<WinState.EndgameID> newFinished, List<WinState.EndgameID> newUnfinished);

        public delegate void ReachNewRoomHandler(AbstractRoom room);

        public delegate void RegionGateHandler(RegionGateInstance gateInstance);

        public delegate void ExtraDialogBoxHandler(ExtraDialogBoxInstance[] extraDialogInstance);

        public delegate void KeyDownHandler(string keyDown);


        internal static void SafeInvoke(this Delegate del,string eventName, params object[] param)
        {
            foreach (var single in del.GetInvocationList())
            {
                try
                {
                    single.Method.Invoke(single.Target, param);
                }
                catch (Exception e)
                {
                    BuffUtils.LogException($"BuffEvent - {eventName}",e);
                }
            }
        }


        
    }

}
