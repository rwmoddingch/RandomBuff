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

    public class DamoclesBug
    {
        WeakReference<Player> ownerRef;
        //是否需要生成
        bool shouldSpawn;
        //是否延迟生成
        bool delaySpawn;
        //条件合适时延迟生成的计时
        List<int> delayCount;

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

            //玩家位于管道中
            if (player.room.aimap != null && player.room.aimap.getAItile(player.bodyChunks[0].pos).narrowSpace)
                delaySpawn = true;
            //玩家离开管道
            else
                delaySpawn = false;

            //玩家不处于管道中时，开始生成之前被推迟生成的落网虫
            if (!delaySpawn && delayCount.Count > 0 && HasCeiling())
            {
                delayCount[0]--;

                if (delayCount[0] <= 0)
                {
                    TrySpawnBug();
                    delayCount.RemoveAt(0);
                }
            }

            bool debugMode = false;
            #if TESTVERSION
            //debugMode = Input.GetKeyDown(KeyCode.C);
            #endif

            //概率和位置满足条件时
            if ((UnityEngine.Random.value * 40 * 30 * 10 < 1 || debugMode) && HasCeiling())
            {
                //位于管道中时，落网虫延迟生成
                if (delaySpawn)
                {
                    delayCount.Add(UnityEngine.Random.Range(40 * 3, 40 * 15));
                    BuffPlugin.Log("[DamoclesBug] : A DropBug Has Delay Spawn. The number of bugs waiting to be generated now is: " + delayCount.Count);
                }
                //若不用延迟生成，那么立刻生成
                else
                    TrySpawnBug();
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
    }
}
