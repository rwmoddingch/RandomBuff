using BuiltinBuffs.Duality;
using HarmonyLib;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RandomBuffUtils;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive
{
    internal class FlamePurificationBuff : IgnitionPointBaseBuff<FlamePurificationBuff, FlamePurificationBuffData>
    {
        public override BuffID ID => FlamePurificationBuffEntry.flamePurificationBuffID;
    }

    internal class FlamePurificationBuffData : BuffData
    {
        public override BuffID ID => FlamePurificationBuffEntry.flamePurificationBuffID;
    }

    internal class FlamePurificationBuffEntry : IBuffEntry
    {
        public static BuffID flamePurificationBuffID = new BuffID("FlamePurification", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<FlamePurificationBuff, FlamePurificationBuffData, FlamePurificationBuffEntry>(flamePurificationBuffID);
            TemperatrueModule.AddProvider(new WormGrassProvider());
        }

        public static void HookOn()
        {
        }
    }

    internal class WormGrassProvider : TemperatrueModule.ITemperatureModuleProvider
    {
        public TemperatrueModule ProvideModule(UpdatableAndDeletable target)
        {
            return new WormGrassTempModule();
        }

        public bool ProvideThisObject(UpdatableAndDeletable target)
        {
            if (FlamePurificationBuff.Instance == null)
                return false;

            return target is WormGrass;
        }
    }

    internal class WormGrassTempModule : TemperatrueModule
    {
        public WormGrassTempModule()
        {
            BuffUtils.Log("WormGrassTemp","Init");
        }

        public override void Update(UpdatableAndDeletable updatableAndDeletable)
        {
            //BuffUtils.Log("WormGrassTemp", "Wa");
            WormGrass wormGrass = updatableAndDeletable as WormGrass;
            var lst = wormGrass.room.updateList.Where((u) => u is IHeatingCreature).Select((u) => u as IHeatingCreature).ToList();
            for(int h = lst.Count - 1; h >= 0; h--)
            {
                var heatSource = lst[h];
                for(int p = wormGrass.patches.Count - 1; p >= 0; p--)
                {
                    var patch = wormGrass.patches[p];
                    for (int t = patch.tiles.Count - 1; t >= 0; t--)
                    {
                        var tile = patch.tiles[t];
                        Vector2 middleTile = wormGrass.room.MiddleOfTile(tile);
                        float heat = heatSource.GetHeat(middleTile);

                        var cosmeticPos = patch.cosmeticWormPositions[t];

                        if (heat > 0f)
                        {
                            for (int i = patch.worms.Count - 1; i >= 0; i--)
                            {
                                var worm = patch.worms[i];

                                if (Vector2.Distance(worm.basePos, middleTile) < 80f)
                                {
                                    worm.Destroy();
                                    patch.worms.Add(worm);
                                    CreateFlameForWorm(worm);
                                }
                            }

                            for(int i = patch.wormGrass.cosmeticWorms.Count - 1; i >= 0; i--)
                            {
                                var worm = patch.wormGrass.cosmeticWorms[i];
                                if (cosmeticPos.Contains(worm.basePos))
                                {
                                    worm.Destroy();
                                    patch.worms.Remove(worm);
                                    wormGrass.cosmeticWorms.Remove(worm);
                                    CreateFlameForWorm(worm);
                                }
                            }


                            patch.tiles.RemoveAt(t);

                            var lst1 = patch.cosmeticWormLengths.ToList();
                            lst1.RemoveAt(t);
                            patch.cosmeticWormLengths = lst1.ToArray();

                            var lst2 = patch.cosmeticWormPositions.ToList();
                            lst2.RemoveAt(t);
                            patch.cosmeticWormPositions = lst2.ToArray();


                            patch.trackedCreatures.Clear();
                            if (patch.tiles.Count == 0)
                                wormGrass.patches.Remove(patch);
                        }
                    }
                }
            }

            void CreateFlameForWorm(WormGrass.Worm worm)
            {
                for(int i = 0;i < 1; i++)
                {
                    worm.wormGrass.room.AddObject(new HolyFire.HolyFireSprite(Vector2.Lerp(worm.basePos, worm.pos, 1f)));
                }
            }
        }
    }

    class WormGrassPatchDebug : UpdatableAndDeletable, IDrawable
    {
        WormGrass.WormGrassPatch patch;
        List<Color> tileColors = new List<Color>();
        List<int> tileCosPosCount = new List<int>();

        public WormGrassPatchDebug(WormGrass.WormGrassPatch patch, Room room)
        {
            this.room = room;
            this.patch = patch;
            foreach(var tile in patch.tiles)
            {
                tileColors.Add(new HSLColor(Random.value, 1f, 0.5f).rgb);
                tileCosPosCount.Add(patch.cosmeticWormPositions[patch.tiles.IndexOf(tile)].Length);
            }
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner = rCam.ReturnFContainer(layerName: "HUD");
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                newContatiner.AddChild(sLeaser.sprites[i]);
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            int index = 0;
            for (int i = 0; i < patch.tiles.Count; i++)
            {
                sLeaser.sprites[index].color = tileColors[i];
                sLeaser.sprites[index].alpha = 0.2f;
                index++;
                for (int k = 0; k < tileCosPosCount[i]; k++)
                {
                    sLeaser.sprites[index].color = tileColors[i];
                    index++;
                }
            }
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            int index = 0;
            for (int i = 0; i < patch.tiles.Count; i++)
            {
                sLeaser.sprites[index].SetPosition(room.MiddleOfTile(patch.tiles[i]) - camPos);
                index++;
                for (int k = 0; k < tileCosPosCount[i]; k++)
                {
                    sLeaser.sprites[index].SetPosition(patch.cosmeticWormPositions[i][k] - camPos);
                    index++;
                }
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            int totalCount = 0;
            for(int i = 0;i < patch.tiles.Count;i++)
            {
                totalCount++;
                totalCount += tileCosPosCount[i];
            }
            sLeaser.sprites = new FSprite[totalCount];

            int index = 0;
            for(int i = 0;i < patch.tiles.Count;i++)
            {
                sLeaser.sprites[index] = new FSprite("Futile_White") { scale = 0.9f };
                index++;
                for(int k = 0;k < tileCosPosCount[i]; k++)
                {
                    sLeaser.sprites[index] = new FSprite("tinyStar");
                    index++;
                }
            }

            AddToContainer(sLeaser, rCam, null);
        }
    }
}
