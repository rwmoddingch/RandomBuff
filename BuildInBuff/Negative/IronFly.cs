using BuiltinBuffs.Positive;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace BuildInBuff.Negative
{
    class IronFlyBuff : Buff<IronFlyBuff, IronFlyBuffData> { public override BuffID ID => IronFlyBuffEntry.IronFlyID; }
    class IronFlyBuffData : BuffData { public override BuffID ID => IronFlyBuffEntry.IronFlyID; }
    class IronFlyBuffEntry : IBuffEntry
    {
        public static BuffID IronFlyID = new BuffID("IronFlyID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<IronFlyBuff, IronFlyBuffData, IronFlyBuffEntry>(IronFlyID);
        }
        public static void HookOn()
        {

            //让矛没法扎成蝙蝠法杖
            On.Spear.TryImpaleSmallCreature += Spear_TryImpaleSmallCreature;

            //让蝙蝠不会被矛和石头致死
            IL.Weapon.HitSomethingWithoutStopping += Weapon_HitSomethingWithoutStopping;

            //防止矛大师吃到蝙蝠
            IL.Spear.HitSomethingWithoutStopping += Spear_HitSomethingWithoutStopping;
        }


        public static void DeflectionWeapon(PhysicalObject weapon, PhysicalObject fly)
        {
            if (weapon is Spear spear)
            {
                fly.firstChunk.vel = Vector2.zero;

                spear.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, spear.firstChunk);
                spear.vibrate = 20;
                spear.ChangeMode(Weapon.Mode.Free);
                spear.firstChunk.vel = spear.firstChunk.vel * -0.5f + Custom.DegToVec(UnityEngine.Random.value * 360f) * Mathf.Lerp(0.1f, 0.4f, UnityEngine.Random.value) * spear.firstChunk.vel.magnitude;
                spear.SetRandomSpin();
            }
            else if (weapon is Rock rock)
            {
                fly.firstChunk.vel = Vector2.zero;

                rock.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, rock.firstChunk);
                var norm = Custom.RNV();
                Vector2 dir = (norm + Custom.RNV() * 0.5f).normalized;

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

                rock.ChangeMode(Weapon.Mode.Thrown);

                float vel = 40f;
                rock.firstChunk.vel = vel * dir;
                rock.firstChunk.pos += dir;
                rock.setRotation = dir;
                rock.rotationSpeed = 0f;
            }
            fly.room.PlaySound(SoundID.Lizard_Head_Shield_Deflect, fly.firstChunk);
        }
        private static void Spear_TryImpaleSmallCreature(On.Spear.orig_TryImpaleSmallCreature orig, Spear self, Creature smallCrit)
        {
            if (smallCrit is Fly)
            {
                return;
            }
            orig.Invoke(self, smallCrit);
        }

        private static void Weapon_HitSomethingWithoutStopping(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.Before,
                (i) => i.MatchIsinst("Spear")
                ))
            {
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Func<PhysicalObject, PhysicalObject, PhysicalObject>>((weapon, creature) =>
                {
                    if (creature is Fly)
                    {
                        DeflectionWeapon(weapon, creature);
                        return null;
                    }
                    return weapon;
                });
            }

            if (c.TryGotoNext(MoveType.Before,
                (i) => i.MatchIsinst("Rock")
                ))
            {
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Func<PhysicalObject, PhysicalObject, PhysicalObject>>((weapon, creature) =>
                {
                    if (creature is Fly)
                    {
                        DeflectionWeapon(weapon, creature);
                        return null;
                    }
                    return weapon;
                });
            }
        }

        private static void Spear_HitSomethingWithoutStopping(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.Before,
                (i) => i.MatchIsinst("Fly"),
                (i) => i.Match(OpCodes.Brfalse)
                ))
            {
                c.EmitDelegate<Func<PhysicalObject, PhysicalObject>>((creature) =>
                {
                    if (creature is Fly)return null;
                    return creature;
                });
            }

        }
    }
}