using MonoMod.Cil;
using RandomBuff;
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
using Mono.Cecil.Cil;
using RWCustom;
using BuiltinBuffs.Positive;

namespace BuiltinBuffs.Negative
{
    internal class PhotophobiaBuff : Buff<PhotophobiaBuff, PhotophobiaBuffData>
    {
        public override BuffID ID => PhotophobiaBuffEntry.Photophobia;
    }

    internal class PhotophobiaBuffData : BuffData
    {
        public override BuffID ID => PhotophobiaBuffEntry.Photophobia;
    }

    internal class PhotophobiaBuffEntry : IBuffEntry
    {
        public static BuffID Photophobia = new BuffID("Photophobia", true);
        public static ConditionalWeakTable<Player, Photophobia> PhotophobiaFeatures = new ConditionalWeakTable<Player, Photophobia>();

        public static int StackLayer
        {
            get
            {
                return Photophobia.GetBuffData()?.StackLayer ?? 0;
            }
        }

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<PhotophobiaBuff, PhotophobiaBuffData, PhotophobiaBuffEntry>(Photophobia);
        }

        public static void HookOn()
        {
            IL.FlareBomb.Update += FlareBomb_UpdateIL;

            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
        }

        private static void FlareBomb_UpdateIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(MoveType.After,
                    (i) => i.MatchCallvirt<Creature>("get_abstractCreature"),
                    (i) => i.MatchCallvirt<Creature>("SetKillTag"),
                    (i) => i.Match(OpCodes.Ldarg_0)))
                {
                    c.Emit(OpCodes.Ldloc_0);
                    c.EmitDelegate<Action<FlareBomb, int>>((self, i) =>
                    {
                        if (self.room.abstractRoom.creatures[i].realizedCreature is Player)
                        {
                            Player player = self.room.abstractRoom.creatures[i].realizedCreature as Player;
                            player.Die();
                            if (self.thrownBy != null)
                            {
                                player.SetKillTag(self.thrownBy.abstractCreature);
                            }
                        }
                    });
                    c.Emit(OpCodes.Ldarg_0);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }


        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!PhotophobiaFeatures.TryGetValue(self, out _))
                PhotophobiaFeatures.Add(self, new Photophobia(self));
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);

            if (PhotophobiaFeatures.TryGetValue(self, out var photophobia))
            {
                photophobia.Update();
            }
            else
            {
                PhotophobiaFeatures.Add(self, new Photophobia(self));
            }
        }
    }
    internal class Photophobia
    {
        WeakReference<Player> ownerRef;

        public Photophobia(Player player)
        {
            ownerRef = new WeakReference<Player>(player);
        }

        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var player) || PhotophobiaBuffEntry.StackLayer <= 1)
                return;
            int level = BeingIlluminatedLevel();
            if (level > 0)
            {
                if(player.aerobicLevel < 0.7f)
                    player.AerobicIncrease(0.01f * Mathf.Min(5f, (PhotophobiaBuffEntry.StackLayer - 1) * level));
                Vector2 addPos = Vector2.Lerp(player.bodyChunks[0].pos - player.bodyChunks[0].lastPos, player.bodyChunks[1].pos - player.bodyChunks[1].lastPos, 0.5f) * 
                                 Mathf.Max(1f - Mathf.Pow(0.9f, (PhotophobiaBuffEntry.StackLayer - 1) * level), 0.5f);
                player.bodyChunks[0].pos -= addPos;
                player.bodyChunks[1].pos -= addPos;
            }
        }

        private int BeingIlluminatedLevel()
        {
            int level = 0;
            if (!ownerRef.TryGetTarget(out var player))
                return level;
            if (IsBeingExposedToSunlight(player.room, player.DangerPos))
                level++;
            for (int i = 0; i < player.room.updateList.Count; i++)
            {
                if (player.room.updateList[i] is LightSource && (player.room.updateList[i] as LightSource).alpha > 0.1f)
                {
                    LightSource light = player.room.updateList[i] as LightSource;
                    if (Custom.DistLess(player.DangerPos, light.pos, light.rad))
                        level++;
                    continue;
                }
                if (player.room.updateList[i] is LightBeam && (player.room.updateList[i] as LightBeam).colorAlpha > 0.1f)
                {
                    LightBeam light = player.room.updateList[i] as LightBeam;
                    if (light.quad.Length >= 4 && Custom.PointInPoly4(player.DangerPos, light.quad[0], light.quad[1], light.quad[2], light.quad[3]))
                        level++;
                    continue;
                }
                if (player.room.updateList[i] is LightFixture && (player.room.updateList[i] as LightFixture).placedObject.active)
                {
                    LightFixture light = player.room.updateList[i] as LightFixture;
                    if (Custom.DistLess(player.DangerPos, light.placedObject.pos, 50f))
                        level++;
                    continue;
                }
            }
            return level;
        }

        private static bool IsBeingExposedToSunlight(Room room, Vector2 pos)
        {
            if (room.abstractRoom.skyExits < 1)
            {
                return false;
            }
            Vector2 corner = Custom.RectCollision(pos, pos + 100000f * Vector2.up, room.RoomRect).GetCorner(FloatRect.CornerLabel.D);
            if (SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(room, pos, corner) != null)
            {
                return false;
            }
            if (corner.y >= room.PixelHeight - 5f)
            {
                return true;
            }
            return false;
        }
    }
}
