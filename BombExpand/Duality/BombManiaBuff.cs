using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BuiltinBuffs.Duality
{
    internal class BombManiaBuffEntry : IBuffEntry
    {
        public static BuffID bombManiaBuffID = new BuffID("BombMania", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<BombManiaBuffEntry>(bombManiaBuffID);
        }

        public static void HookOn()
        {
            On.Room.Loaded += Room_Loaded;
        }

        private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            orig.Invoke(self);
            int total = 0;
            if (!self.abstractRoom.shelter && !self.abstractRoom.gate && self.game != null && (!self.game.IsArenaSession || self.game.GetArenaGameSession.GameTypeSetup.levelItems) && (!ModManager.MMF || self.roomSettings.RandomItemDensity > 0f))
            {
                for (int num16 = (int)((float)self.TileWidth * (float)self.TileHeight * (0.07f + 0.07f * Mathf.Pow(self.roomSettings.RandomItemDensity, 2f) / 5f)); num16 >= 0; num16--)
                {
                    IntVector2 intVector = self.RandomTile();
                    if (!self.GetTile(intVector).Solid)
                    {
                        bool actuallySpawn = true;
                        if (!ModManager.MMF || self.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.ZeroG) < 1f || self.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.BrokenZeroG) > 0f)
                        {
                            for (int num17 = -1; num17 < 2; num17++)
                            {
                                if (!self.GetTile(intVector + new IntVector2(num17, -1)).Solid)
                                {
                                    actuallySpawn = false;
                                    break;
                                }
                                if (ModManager.MMF && self.GetTile(intVector).Terrain == Room.Tile.TerrainType.Slope && self.GetTile(intVector + new IntVector2(0, 1)).Solid)
                                {
                                    actuallySpawn = false;
                                    break;
                                }
                            }
                        }
                        else if (ModManager.MMF)
                        {
                            bool flag6 = false;
                            for (int num18 = -1; num18 < 2; num18++)
                            {
                                if (self.GetTile(intVector + new IntVector2(num18, 0)).Solid)
                                {
                                    flag6 = true;
                                    break;
                                }
                            }
                            bool flag7 = false;
                            for (int num19 = -1; num19 < 2; num19++)
                            {
                                if (self.GetTile(intVector + new IntVector2(0, num19)).Solid)
                                {
                                    flag7 = true;
                                    break;
                                }
                            }
                            if (flag6 && flag7)
                            {
                                actuallySpawn = false;
                            }
                            else if (!(flag6 ^ flag7) && UnityEngine.Random.value > 0.1f)
                            {
                                actuallySpawn = false;
                            }
                        }
                        if (total > 25)
                            return;
                        if (actuallySpawn)
                        {
                            EntityID newID3 = self.game.GetNewID(-self.abstractRoom.index);
                            AbstractPhysicalObject abstractBomb = new AbstractPhysicalObject(self.world, AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, null, new WorldCoordinate(self.abstractRoom.index, intVector.x, intVector.y, -1), newID3);
                            self.abstractRoom.AddEntity(abstractBomb);
                            total++;
                        }
                    }
                }
            }
        }
    }
}
