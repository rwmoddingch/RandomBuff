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

        public DamoclesBug(Player player)
        {
            ownerRef = new WeakReference<Player>(player);
        }

        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var player) || player.room == null)
                return;
            if (UnityEngine.Random.value * 40 * 30 * 10 > 1)
            {
                Vector2 corner = Custom.RectCollision(player.DangerPos, player.DangerPos + 100000f * Vector2.up, player.room.RoomRect).GetCorner(FloatRect.CornerLabel.D);
                IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(player.room, player.DangerPos, corner);
                if (intVector != null)
                {
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
                    dropBug.AI.behavior = DropBugAI.Behavior.SitInCeiling;
                    for(int i = 0; i < 100; i++)
                    {
                        dropBug.AI.ceilingModule.SitUpdate();
                        dropBug.AI.ceilingModule.dropDelay = 0f;
                    }
                }
            }
        }
    }
}
