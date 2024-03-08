using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MonoMod.Cil;
using Random = UnityEngine.Random;
using Mono.Cecil.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using RandomBuffUtils;

namespace BuiltinBuffs.Positive
{

    internal class HardenedMaskIBuffEntry : IBuffEntry
    {
        public static BuffID HardenedMaskBuffID = new BuffID("HardenedMask", true);
        public static int countdown;
        public static ILCursor c;

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<HardenedMaskIBuffEntry>(HardenedMaskBuffID);
        }

        public static void HookOn()
        {
            On.Player.Update += Player_Update;
            IL.Spear.HitSomething += Spear_HitSomething;
        }

        private static void Spear_HitSomething(MonoMod.Cil.ILContext il)
        {
            ILCursor markCursor = new ILCursor(il);
            ILCursor emitCursor = new ILCursor(il);
            ILLabel markLabel = null;

            markCursor.Index = il.Instrs.Count - 1;
            markCursor.Index--;
            markLabel = markCursor.MarkLabel();

            if (markLabel != null && emitCursor.TryGotoNext(MoveType.After,
                (i) => i.MatchLdloc(2),
                (i) => i.MatchLdcR4(3),
                (i) => i.MatchMul(),
                (i) => i.MatchStloc(2),
                (i) => i.MatchLdarg(1)
            ))
            {
                BuffUtils.Log(HardenedMaskBuffID,"Spear_HitSomething");
                BuffUtils.Log(HardenedMaskBuffID, emitCursor.Next.OpCode);
                emitCursor.Emit(OpCodes.Ldarg_0);

                emitCursor.EmitDelegate<Func<SharedPhysics.CollisionResult, Spear, bool>>((result, self) =>
                {
                    BuffUtils.Log(HardenedMaskBuffID, $"Spear_HitSomething {result.obj}");
                    if(result.obj is Player player)
                    {
                        Vector2 movementum = self.firstChunk.vel * self.firstChunk.mass * 2f;
                        var res = CheckIfMaskBounce(player, self, new Vector2?(movementum));
                        if (res)
                        {
                            self.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, self.firstChunk);
                            self.vibrate = 20;
                            self.ChangeMode(Weapon.Mode.Free);
                            self.firstChunk.HardSetPosition(player.DangerPos);
                            self.firstChunk.vel = self.firstChunk.vel * -0.5f + Custom.DegToVec(Random.value * 360f) * Mathf.Lerp(0.1f, 0.4f, Random.value) * self.firstChunk.vel.magnitude;
                            self.SetRandomSpin();
                            return true;
                        }    
                    }
                    return false;
                });

                emitCursor.Emit(OpCodes.Brtrue, markLabel);
                emitCursor.Emit(OpCodes.Ldarg_1);
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (countdown > 0)
                countdown--;
            if (Input.GetKey(KeyCode.LeftAlt) && countdown == 0)
            {
                AbstractPhysicalObject abstractPhysical = new AbstractSpear(self.room.world, null, new WorldCoordinate(self.room.abstractRoom.index, self.coord.x + 5, self.coord.y + 1, -1), self.room.game.GetNewID(), false);
                self.room.abstractRoom.entities.Add(abstractPhysical);
                abstractPhysical.RealizeInRoom();

                var spear = abstractPhysical.realizedObject as Spear;
                spear.firstChunk.HardSetPosition(self.DangerPos + new Vector2(20f * 8f, 5f));

                spear.Shoot(null, spear.firstChunk.pos, Vector2.left, 1f, eu);
                countdown = 10;
            }
        }

        public static bool CheckIfMaskBounce(Player player, Spear spear, Vector2? directionAndMomentum)
        {
            for (int i = 0; i < player.grasps.Length; i++)
            {
                BuffUtils.Log(HardenedMaskBuffID, $"check grasp {i} : {player.grasps[i]?.grabbed}");
                if (player.grasps[i] != null && player.grasps[i].grabbed is VultureMask mask && mask.donned > 0.5f)
                {
                    Vector2 playerDir = new Vector2(player.ThrowDirection, 0f);

                    Vector2 spearDir = directionAndMomentum == null ? spear.firstChunk.vel.normalized : directionAndMomentum.Value;

                    BuffUtils.Log(HardenedMaskBuffID, $"{playerDir}, {spearDir}, {Vector2.Dot(playerDir, spearDir)}");

                    if (Vector2.Dot(playerDir, spearDir) < 0f)
                    {
                        if (player.room != null)
                        {
                            player.room.PlaySound(SoundID.Lizard_Head_Shield_Deflect, player.mainBodyChunk);
                        }
                        mask.blink = 10;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
