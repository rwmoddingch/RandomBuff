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
using System.Runtime.CompilerServices;
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
            TemperatureModule.AddProvider(new WormGrassProvider());
            TemperatureModule.AddProvider(new DaddyCorruptionProvider());

        }

        public static void HookOn()
        {
            On.DaddyCorruption.DrawSprites += DaddyCorruption_DrawSprites;
            On.DaddyCorruption.Bulb.DrawSprites += Bulb_DrawSprites;
            On.DaddyCorruption.Bulb.Update += Bulb_Update;
            On.DaddyCorruption.Bulb.HeardNoise += Bulb_HeardNoise;
            On.DaddyCorruption.Bulb.FeltSomething += Bulb_FeltSomething;
        }

        private static void DaddyCorruption_DrawSprites(On.DaddyCorruption.orig_DrawSprites orig, DaddyCorruption self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self,sLeaser,rCam,timeStacker,camPos);

            if (TemperatureModule.TryGetTemperatureModule<DaddyCorruptionTempModule>(self, out var module))
            {
                foreach(var tile in module.cleanTile)
                    if (tile.Value.Value < 1)
                        tile.Value.Value = Mathf.Clamp01(tile.Value.Value + 1.5f * Time.deltaTime);
                    
            }
        }

        private static void Bulb_FeltSomething(On.DaddyCorruption.Bulb.orig_FeltSomething orig, DaddyCorruption.Bulb self, float intensity, Vector2 feltAtPos)
        {
            if (TemperatureModule.TryGetTemperatureModule<DaddyCorruptionTempModule>(self.owner, out var module) && module.cleanTile.ContainsKey(self.tile))
                return;
            orig(self, intensity, feltAtPos);
        }

        private static void Bulb_HeardNoise(On.DaddyCorruption.Bulb.orig_HeardNoise orig, DaddyCorruption.Bulb self, Vector2 noisePos)
        {
            if (TemperatureModule.TryGetTemperatureModule<DaddyCorruptionTempModule>(self.owner, out var module) && module.cleanTile.ContainsKey(self.tile))
                return;
            orig(self, noisePos);
        }

        private static void Bulb_Update(On.DaddyCorruption.Bulb.orig_Update orig, DaddyCorruption.Bulb self)
        {
            if (TemperatureModule.TryGetTemperatureModule<DaddyCorruptionTempModule>(self.owner, out var module) && module.cleanTile.ContainsKey(self.tile))
            {
                self.eatChunk = null;
                return;
            }
            orig(self);
        }

        private static void Bulb_DrawSprites(On.DaddyCorruption.Bulb.orig_DrawSprites orig, DaddyCorruption.Bulb self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (TemperatureModule.TryGetTemperatureModule<DaddyCorruptionTempModule>(self.owner, out var module) && module.cleanTile.ContainsKey(self.tile))
            {
                for (int i = 0; i < self.totalSprites; i++)
                    sLeaser.sprites[self.firstSprite + i].alpha = 1 - module.cleanTile[self.tile].Value;
            }
        }
    }

    internal class WormGrassProvider : TemperatureModule.ITemperatureModuleProvider
    {

        public TemperatureModule ProvideModule(UpdatableAndDeletable target)
        {
            return new WormGrassTempModule(target as WormGrass);
        }

        public bool ProvideThisObject(UpdatableAndDeletable target)
        {
            if (FlamePurificationBuff.Instance == null)
                return false;

            return target is WormGrass;
        }
    }

    internal class WormGrassTempModule : TemperatureModule
    {
        WormGrass.WormGrassPatch[] patchIndexs;

        List<int> totalTiles = new List<int>();
        List<int> tileProgression = new List<int>();

        public WormGrassTempModule(WormGrass wormGrass)
        {
            patchIndexs = new WormGrass.WormGrassPatch[wormGrass.patches.Count];
            for(int i = 0; i < patchIndexs.Length; i++)
            {
                patchIndexs[i] = wormGrass.patches[i];
                totalTiles.Add(wormGrass.patches[i].tiles.Count);
                tileProgression.Add(0);
            }
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
                        float heat = heatSource.GetHeat(wormGrass, middleTile);

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
                            int patchIndex = patchIndexs.IndexOf(patch);
                            tileProgression[patchIndex] = totalTiles[patchIndex] - patch.tiles.Count;
                            BuffUtils.Log("FlamePurification", $"Wormgrass patch tile wiped out, {patch.tiles.Count} left");

                            foreach (var update in wormGrass.room.updateList.Where(u => u is INoticeWormGrassWipedOut).Select(u => u as INoticeWormGrassWipedOut))
                            {
                                update.NoticeWormGrassWipeProgression(tileProgression, totalTiles);
                            }

                            var lst1 = patch.cosmeticWormLengths.ToList();
                            lst1.RemoveAt(t);
                            patch.cosmeticWormLengths = lst1.ToArray();

                            var lst2 = patch.cosmeticWormPositions.ToList();
                            lst2.RemoveAt(t);
                            patch.cosmeticWormPositions = lst2.ToArray();


                            patch.trackedCreatures.Clear();
                            if (patch.tiles.Count == 0)
                            {
                                patchIndexs[patchIndexs.IndexOf(patch)] = null;
                                wormGrass.patches.Remove(patch);
                                BuffUtils.Log("FlamePurification", $"Wormgrass patch wiped out, {wormGrass.patches.Count} left");
                                if (wormGrass.patches.Count == 0)
                                {
                                    wormGrass.Destroy();

                                    BuffUtils.Log("FlamePurification", "Wormgrass wiped out");
                                    foreach (var update in wormGrass.room.updateList.Where(u => u is INoticeWormGrassWipedOut).Select(u => u as INoticeWormGrassWipedOut))
                                    {
                                        BuffUtils.Log("FlamePurification", $"{update.GetType()} notice wormgrass wiped out");
                                        update.WormGrassWipedOut(wormGrass);
                                    }

                                }
                            }
                        }
                    }
                }
            }

            void CreateFlameForWorm(WormGrass.Worm worm)
            {
                //for(int i = 0;i < 1; i++)
                //{
                //    worm.wormGrass.room.AddObject(new HolyFire.HolyFireSprite(Vector2.Lerp(worm.basePos, worm.pos, 1f)));
                //}
            }
        }
    }

    internal class DaddyCorruptionProvider : TemperatureModule.ITemperatureModuleProvider
    {
        public TemperatureModule ProvideModule(UpdatableAndDeletable target)
        {
            return new DaddyCorruptionTempModule();
        }

        public bool ProvideThisObject(UpdatableAndDeletable target)
        {
            if (FlamePurificationBuff.Instance == null)
                return false;

            return target is DaddyCorruption;
        }
    }

    internal class DaddyCorruptionTempModule : TemperatureModule
    {
        //public List<int> skipSpriteIndexRange = new List<int>();

        public Dictionary<IntVector2,StrongBox<float>> cleanTile = new Dictionary<IntVector2, StrongBox<float>>();
        public override void Update(UpdatableAndDeletable updatableAndDeletable)
        {
            DaddyCorruption corruption = updatableAndDeletable as DaddyCorruption;
            var lst = corruption.room.updateList.OfType<IHeatingCreature>().ToList();

            for (int h = lst.Count - 1; h >= 0; h--)
            {
                var heatSource = lst[h];

                foreach (var bu in corruption.bulbs)
                {
                    if (bu == null) continue;
                    foreach (var s in bu)
                    {
                        if (heatSource.GetHeat(corruption, corruption.room.MiddleOfTile(s.tile)) > 0f)
                        {
                            if (!cleanTile.ContainsKey(s.tile))
                            {
                                cleanTile[s.tile] = new StrongBox<float>(0);
                                s.eatChunk = null;
                            }
                        }
                    }
                }


                //for (int i = corruption.tiles.Count - 1; i >= 0; i--)
                //{
                //    var tile = corruption.tiles[i];
                //    if (heatSource.GetHeat(corruption, corruption.room.MiddleOfTile(tile)) > 0f)
                //    {
                //        var bulbLst = corruption.bulbs[tile.x - corruption.bottomLeft.x, tile.y - corruption.bottomLeft.y];
                //        foreach(var bulb in bulbLst)
                //        {
                //            for(int index = bulb.firstSprite; index < bulb.firstSprite + bulb.totalSprites; index++)
                //            {
                //                skipSpriteIndexRange.Add(index);
                //            }
                //        }
                //        bulbLst.Clear();
                //    }
                //}
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

    public interface INoticeWormGrassWipedOut
    {
        void WormGrassWipedOut(WormGrass wormGrass);

        void NoticeWormGrassWipeProgression(List<int> progression, List<int> total);
    }
}
