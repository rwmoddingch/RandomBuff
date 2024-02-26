
using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine;
using RWCustom;
using BuiltinBuffs.Positive;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RandomBuffUtils;

namespace BuiltinBuffs.Negative
{
    internal class RockCobBuff : Buff<RockCobBuff, RockCobBuffData>
    {
        public override BuffID ID => RockCobIBuffEntry.RockCobBuffID;
    }

    internal class RockCobBuffData : BuffData
    {
        public override BuffID ID => RockCobIBuffEntry.RockCobBuffID;
    }

    internal class RockCobIBuffEntry : IBuffEntry
    {
        public static BuffID RockCobBuffID = new BuffID("RockCob", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<RockCobBuff, RockCobBuffData, RockCobIBuffEntry>(RockCobBuffID); 
        }

        public static void HookOn()
        {
            IL.SeedCob.Update += SeedCob_Update;
            On.SeedCob.Open += SeedCob_Open;
        }

        private static void SeedCob_Open(On.SeedCob.orig_Open orig, SeedCob self)
        {
            orig.Invoke(self);
            if(!self.AbstractCob.dead) 
            {
                self.AbstractCob.dead = true;
                foreach (var sleaser in self.room.game.cameras[0].spriteLeasers)
                {
                    if (sleaser.drawableObject == self)
                    {
                        BuffUtils.Log(RockCobBuffID,$"SeedCob reapply palettes");
                        sleaser.RemoveAllSpritesFromContainer();
                        self.InitiateSprites(sleaser, self.room.game.cameras[0]);
                        self.ApplyPalette(sleaser, self.room.game.cameras[0], self.room.game.cameras[0].currentPalette);
                        break;
                    }
                }
            }
        }

        private static void SeedCob_Update(ILContext il)
        {
            ILCursor c1 = new ILCursor(il);

            if(c1.TryGotoNext(MoveType.After, 
                (i) => i.MatchLdarg(0),
                (i) => i.MatchLdfld<UpdatableAndDeletable>("room"),
                (i) => i.MatchLdsfld<SoundID>("Seed_Cob_Pop"),
                (i) => i.MatchLdloc(6),
                (i) => i.MatchCallvirt<Room>("PlaySound")
            ))
            {
                c1.Emit(OpCodes.Ldarg_0);//this
                c1.Emit(OpCodes.Ldloc_3);//i
                c1.Emit(OpCodes.Ldloc, 7);//normalized
                c1.EmitDelegate<Action<SeedCob, int, Vector2>>((self, index, normalized) =>
                {
                    //self.seedsPopped[index] = false;
                    AbstractPhysicalObject abstractPhysicalObject = new AbstractPhysicalObject(self.room.world, AbstractPhysicalObject.AbstractObjectType.Rock, null, self.room.GetWorldCoordinate(self.seedPositions[index] + self.firstChunk.pos), self.room.game.GetNewID());
                    abstractPhysicalObject.RealizeInRoom();
                    Rock rock = abstractPhysicalObject.realizedObject as Rock;

                    Vector2 dir = normalized;

                    rock.firstChunk.pos += dir * 10f;

                    rock.thrownPos = rock.firstChunk.pos;
                    rock.thrownBy = null;
                    IntVector2 throwDir = new IntVector2(0, 0);

                    if (dir.x > 0)
                        throwDir.x = 1;
                    else if (dir.x < 0)
                        throwDir.x = -1;

                    if (dir.y > 0)
                        throwDir.y = 1;
                    else if (dir.y < 0)
                        throwDir.y = -1;
                    rock.throwDir = throwDir;

                    rock.firstFrameTraceFromPos = rock.thrownPos;
                    rock.changeDirCounter = 3;
                    rock.ChangeOverlap(true);
                    rock.firstChunk.MoveFromOutsideMyUpdate(false, rock.thrownPos);

                    float vel = 10f;

                    rock.firstChunk.vel = vel * dir;
                    rock.firstChunk.pos = dir + self.seedPositions[index] + self.firstChunk.pos;
                    rock.firstChunk.lastPos = rock.firstChunk.pos;
                    rock.tailPos = rock.firstChunk.pos;
                    rock.setRotation = dir;
                    

                    if(BuffCore.TryGetBuff(EjectionRockIBuffEntry.ejectionRockBuffID, out var _))
                    {
                        rock.ChangeMode(Weapon.Mode.Thrown);rock.doNotTumbleAtLowSpeed = true;
                        rock.rotationSpeed = 0f;
                        rock.meleeHitChunk = null;
                        rock.overrideExitThrownSpeed = 0f;
                    }
                });
            }
        }
    }
}
