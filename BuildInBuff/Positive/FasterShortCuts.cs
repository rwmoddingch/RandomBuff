using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
    
namespace BuiltinBuffs.Positive
{
    internal class FasterShortCutsEntry : IBuffEntry
    {

        public static readonly BuffID FasterShortCuts = new BuffID(nameof(FasterShortCuts), true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<FasterShortCutsEntry>(FasterShortCuts);
        }

        public static void HookOn()
        {
            On.ShortcutHandler.Update += ShortcutHandler_Update;
        }

        private const int Speed = 3;

        private static void ShortcutHandler_Update(On.ShortcutHandler.orig_Update orig, ShortcutHandler self)
        {
            orig(self);
            for (int j = 0; j < Speed - 1; j++)
            {
                for (int i = self.transportVessels.Count - 1; i >= 0; i--)
                {
                    if (self.transportVessels[i].room.realizedRoom != null &&
                        self.transportVessels[i].creature is Player)
                    {
                        Room realizedRoom = self.transportVessels[i].room.realizedRoom;
                        IntVector2 pos = self.transportVessels[i].pos;
                        self.transportVessels[i].pos =
                            ShortcutHandler.NextShortcutPosition(self.transportVessels[i].pos,
                                self.transportVessels[i].lastPos, realizedRoom);
                        self.transportVessels[i].PushNewLastPos(pos);
                        if (realizedRoom.GetTile(self.transportVessels[i].pos).Terrain ==
                            Room.Tile.TerrainType.ShortcutEntrance)
                        {
                            self.SpitOutCreature(self.transportVessels[i]);
                            self.transportVessels.RemoveAt(i);
                        }
                        else if (self.transportVessels[i].pos != self.transportVessels[i].lastPos &&
                                 realizedRoom.GetTile(self.transportVessels[i].pos).shortCut > 1)
                        {
                            int num = realizedRoom.exitAndDenIndex.IndexfOf(self.transportVessels[i].pos);
                            self.transportVessels[i].creature.abstractCreature.pos.abstractNode = num;
                            switch (realizedRoom.GetTile(self.transportVessels[i].pos).shortCut)
                            {
                                case 2:
                                    if (self.game.IsArenaSession)
                                    {
                                        if (!(self.transportVessels[i].creature is Player) ||
                                            !self.game.GetArenaGameSession.PlayerTryingToEnterDen(
                                                self.transportVessels[i]))
                                        {
                                            return;
                                        }

                                        self.transportVessels[i].wait = self.game.world.rainCycle.TimeUntilRain + 10000;
                                        self.transportVessels[i].pos = pos;
                                    }
                                    else
                                    {
                                        if (self.transportVessels[i].room.connections.Length != 0)
                                        {
                                            if (num >= self.transportVessels[i].room.connections.Length)
                                            {
                                                self.transportVessels[i].PushNewLastPos(self.transportVessels[i].pos);
                                                self.transportVessels[i].pos = pos;
                                            }
                                            else
                                            {
                                                int num2 = self.transportVessels[i].room.connections[num];
                                                if (num2 <= -1)
                                                {
                                                    return;
                                                }

                                                self.transportVessels[i].entranceNode = self.game.world
                                                    .GetAbstractRoom(num2)
                                                    .ExitIndex(self.transportVessels[i].room.index);
                                                self.transportVessels[i].room = self.game.world.GetAbstractRoom(num2);
                                                self.betweenRoomsWaitingLobby.Add(self.transportVessels[i]);
                                            }
                                        }

                                        self.transportVessels.RemoveAt(i);
                                    }

                                    break;
                                case 3:
                                    self.transportVessels[i].creature.abstractCreature
                                        .OpportunityToEnterDen(new WorldCoordinate(self.transportVessels[i].room.index,
                                            -1, -1, num));
                                    if (self.transportVessels[i].creature.abstractCreature.InDen)
                                    {
                                        self.transportVessels.RemoveAt(i);
                                    }

                                    break;
                                case 5:
                                    if (self.transportVessels[i].creature.NPCTransportationDestination.room > -1 &&
                                        self.transportVessels[i].creature.NPCTransportationDestination.NodeDefined)
                                    {
                                        self.transportVessels[i].entranceNode = self.transportVessels[i].creature
                                            .NPCTransportationDestination.abstractNode;
                                        self.transportVessels[i].room =
                                            self.game.world.GetAbstractRoom(self.transportVessels[i].creature
                                                .NPCTransportationDestination.room);
                                        self.betweenRoomsWaitingLobby.Add(self.transportVessels[i]);
                                        self.transportVessels.RemoveAt(i);
                                    }

                                    break;
                            }
                        }
                    }
                
                }
            }
        }
    }


}
