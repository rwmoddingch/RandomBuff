using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using RandomBuff;
using TemplateGains;

namespace BuiltinBuffs.Negative
{
    internal class DamoclesBugBuff : Buff<DamoclesBugBuff, DamoclesBugBuffData>
    {
        public override BuffID ID => DamoclesBugBuffEntry.DamoclesBug;

        public DamoclesBugBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    if (DamoclesBugBuffEntry.DamoclesBugFeatures.TryGetValue(player, out _))
                        DamoclesBugBuffEntry.DamoclesBugFeatures.Remove(player);
                    var damoclesBug = new DamoclesBug(player);
                    DamoclesBugBuffEntry.DamoclesBugFeatures.Add(player, damoclesBug);
                }
            }
            CreateMyTimer();
        }

        public void CreateMyTimer()
        {
            MyTimer = new DownCountBuffTimer((timer, game) => MyTimer.Reset(), 1000000);
            MyTimer.ApplyStrategy(new DamoclesBugStrategy());
            MyTimer.Paused = true;
        }
    }

    internal class DamoclesBugBuffData : CountableBuffData
    {
        public override BuffID ID => DamoclesBugBuffEntry.DamoclesBug;
        public override int MaxCycleCount => 3;
    }

    internal class DamoclesBugBuffEntry : IBuffEntry
    {
        public static BuffID DamoclesBug = new BuffID("DamoclesBug", true);

        public static ConditionalWeakTable<Player, DamoclesBug> DamoclesBugFeatures = new ConditionalWeakTable<Player, DamoclesBug>();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<DamoclesBugBuff, DamoclesBugBuffData, DamoclesBugBuffEntry>(DamoclesBug);
        }

        public static void HookOn()
        {
            On.Player.Update += Player_Update;
        }
        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (DamoclesBugFeatures.TryGetValue(self, out var damoclesBug))
            {
                damoclesBug.Update();
            }
            else
            {
                DamoclesBugFeatures.Add(self, new DamoclesBug(self));
            }
        }
    }

    internal class DamoclesBug
    {
        WeakReference<Player> ownerRef;
        //是否延迟生成
        public bool delaySpawn;
        //条件合适时延迟生成的计时
        public List<int> delayCount;

        public int spawnCount
        {
            get
            {
                if (delayCount == null || delayCount.Count == 0)
                    return -1;
                return delayCount[0];
            }
        }

        public DamoclesBug(Player player)
        {
            ownerRef = new WeakReference<Player>(player);
            delaySpawn = false;
            delayCount = new List<int>();
        }

        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var player) || player.room == null)
                return;

            //玩家位于管道中、业力门或避难所
            if ((player.room.aimap != null && player.room.aimap.getAItile(player.bodyChunks[0].pos).narrowSpace) ||
                 InGateOrShelter())
                delaySpawn = true;
            //玩家离开管道
            else
                delaySpawn = false;

            //玩家不处于管道中、业力门或避难所， 且满足条件时，开始生成之前被推迟生成的落网虫
            if (!delaySpawn && delayCount.Count > 0 && delayCount[0] > 0 && HasCeiling())
                delayCount[0]--;

            if (delayCount.Count > 0)
            {
                if (DamoclesBugBuff.Instance.MyTimer != null)
                {
                    int minCount = delayCount[0];
                    foreach (var allPlayer in player.room.game.AlivePlayers.Select(i => i.realizedCreature as Player)
                                     .Where(i => i != null && i.graphicsModule != null))
                    {
                        if (DamoclesBugBuffEntry.DamoclesBugFeatures.TryGetValue(player, out var damoclesBug))
                        {
                            if (damoclesBug.spawnCount < delayCount[0])
                            {
                                minCount = damoclesBug.spawnCount;
                            }
                        }
                    }
                    if (minCount == delayCount[0])
                    {
                        DamoclesBugBuff.Instance.MyTimer.frames = delayCount[0];
                    }
                }
                else
                    SetHerald();
            }

            bool debugMode = false;
            #if TESTVERSION
            //debugMode = Input.GetKeyDown(KeyCode.C);
            #endif

            //概率和位置满足条件时
            if ((UnityEngine.Random.value * 40 * 30 * 10 < 1 || debugMode) && HasCeiling())
            {
                //落网虫延迟生成
                if (delaySpawn)
                    delayCount.Add(UnityEngine.Random.Range(40 * 3 + 1, 40 * 15));
                else
                    delayCount.Add(UnityEngine.Random.Range(40 * 3 + 1, 40 * 5));

                BuffUtils.Log(DamoclesBugBuff.Instance.ID, "A DropBug Has Delay Spawn. The number of bugs waiting to be generated now is: " + delayCount.Count);
                SetHerald();
            }
        }

        public void TrySpawnBug()
        {
            if (!ownerRef.TryGetTarget(out var player) || player.room == null)
                return;

            Vector2 corner = Custom.RectCollision(player.DangerPos, player.DangerPos + 100000f * Vector2.up, player.room.RoomRect).GetCorner(FloatRect.CornerLabel.D);
            IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(player.room, player.DangerPos, corner);

            if(intVector != null)
            {
                DamoclesBugBuff.Instance.TriggerSelf(true);

                corner = Custom.RectCollision(corner, player.DangerPos, player.room.TileRect(intVector.Value)).GetCorner(FloatRect.CornerLabel.D);
                AbstractCreature absDropBug = new AbstractCreature(player.abstractCreature.world,
                                                                StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.DropBug), null,
                                                                player.abstractCreature.pos, player.abstractCreature.world.game.GetNewID());
                player.room.abstractRoom.AddEntity(absDropBug);
                absDropBug.RealizeInRoom();
                DropBug dropBug = absDropBug.realizedCreature as DropBug;
                foreach (var body in dropBug.bodyChunks)
                {
                    body.HardSetPosition(corner);
                }
                dropBug.AI.ceilingModule.ceilingPos = player.room.GetWorldCoordinate(Room.StaticGetTilePosition(corner));
                for (int i = 0; i < 40; i++)
                {
                    dropBug.AI.behavior = DropBugAI.Behavior.SitInCeiling;
                    dropBug.inCeilingMode = 1f;
                    dropBug.AI.ceilingModule.SitUpdate();
                    dropBug.graphicsModule.Update();
                    dropBug.AI.ceilingModule.dropDelay = 0f;
                }
                dropBug.AI.ceilingModule.JumpFromCeiling(player.mainBodyChunk, Custom.DirVec(dropBug.mainBodyChunk.pos, player.mainBodyChunk.pos));
            }
            else
            {
                BuffPlugin.Log("[DamoclesBug] : Error! No Ceiling For DropBug To Spawn.");
            }
        }

        public void SetHerald()
        {
            DamoclesBugBuff.Instance.MyTimer = new DownCountBuffTimer((timer, game) =>
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    if (DamoclesBugBuffEntry.DamoclesBugFeatures.TryGetValue(player, out var damoclesBug))
                    {
                        if (damoclesBug.spawnCount == 0)
                        {
                            damoclesBug.TrySpawnBug();
                            damoclesBug.delayCount.RemoveAt(0);
                            BuffUtils.Log(DamoclesBugBuff.Instance.ID, "A DropBug Spawn! The number of bugs waiting to be generated now is: " + damoclesBug.delayCount.Count);
                            DamoclesBugBuff.Instance.MyTimer.Reset();
                        }
                    }
                }
            }, Mathf.FloorToInt((float)delayCount[0] / 40f));
            DamoclesBugBuff.Instance.MyTimer.ApplyStrategy(new DamoclesBugStrategy());
            DamoclesBugBuff.Instance.MyTimer.Paused = false;
        }

        public bool HasCeiling()
        {
            if (!ownerRef.TryGetTarget(out var player) || player.room == null)
                return false;
            Vector2 corner = Custom.RectCollision(player.DangerPos, player.DangerPos + 100000f * Vector2.up, player.room.RoomRect).GetCorner(FloatRect.CornerLabel.D);
            IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(player.room, player.DangerPos, corner);
            if (intVector != null)
                return true;
            else
                return false;
        }

        public bool InGateOrShelter()
        {
            if (!ownerRef.TryGetTarget(out var player) || player.room == null)
                return false;
            if (player.room.abstractRoom.gate || player.room.abstractRoom.shelter)
                return true;
            else
                return false;
        }
    }

    internal class DamoclesBugStrategy : BuffTimerDisplayStrategy
    {
        public override bool DisplayThisFrame => Second <= 3;
    }
}
