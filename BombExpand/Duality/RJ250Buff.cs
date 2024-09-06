using MonoMod.Cil;
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
    internal class RJ250Buff : IBuffEntry
    {
        public static BuffID rj250BuffID = new BuffID("RJ250", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<RJ250Buff>(rj250BuffID);
        }

        public static void HookOn()
        {
            IL.ScavengerBomb.Explode += ScavengerBomb_Explode1;
            On.ScavengerBomb.Explode += ScavengerBomb_Explode;
            On.Weapon.Thrown += Weapon_Thrown;
        }

        private static void ScavengerBomb_Explode(On.ScavengerBomb.orig_Explode orig, ScavengerBomb self, BodyChunk hitChunk)
        {
            self.explodeColor = Color.yellow;
            orig.Invoke(self, hitChunk);
        }

        private static void Weapon_Thrown(On.Weapon.orig_Thrown orig, Weapon self, Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
        {
            orig.Invoke(self, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
            if (self is ScavengerBomb /*&& thrownBy is Player*/)
            {
                self.firstChunk.vel.x = -thrownBy.mainBodyChunk.vel.x * 10f;
                self.firstChunk.vel.y = thrownBy.mainBodyChunk.vel.y * 0.5f - 25f * frc;
            }
        }

        private static void ScavengerBomb_Explode1(MonoMod.Cil.ILContext il)
        {
            ILCursor c1 = new ILCursor(il);
            if (c1.TryGotoNext(MoveType.After,
                (i) => i.MatchNewobj<Explosion>(),
                (i) => i.MatchCallvirt<Room>("AddObject")))
            {
                if (c1.TryGotoPrev(MoveType.After,
                    (i) => i.MatchLdcR4(0.25f)))
                {
                    c1.EmitDelegate<Func<float, float>>((orig) =>
                    {
                        return orig * 0f;
                    });
                }

                if (c1.TryGotoPrev(MoveType.After,
                    (i) => i.MatchLdcR4(280f)))
                {
                    c1.EmitDelegate<Func<float, float>>((orig) =>
                    {
                        return orig * 0f;
                    });
                }

                if (c1.TryGotoPrev(MoveType.After,
                    (i) => i.MatchLdcR4(2f)))
                {
                    c1.EmitDelegate<Func<float, float>>((orig) =>
                    {
                        return orig * 0.1f;
                    });
                }

                if (c1.TryGotoPrev(MoveType.After,
                    (i) => i.MatchLdcR4(6.2f)))
                {
                    c1.EmitDelegate<Func<float, float>>((orig) =>
                    {
                        return orig * 0.75f;
                    });
                }

                if(c1.TryGotoNext(MoveType.After,(i) => i.MatchNewobj<ShockWave>()))
                {
                    if(c1.TryGotoPrev(MoveType.After, (i) => i.MatchLdcI4(5)))
                    {
                        c1.EmitDelegate<Func<int, int>>((orig) =>
                        {
                            return 20;
                        });
                    }

                    if(c1.TryGotoPrev(MoveType.After, (i) => i.MatchLdcR4(330f)))
                    {
                        c1.EmitDelegate<Func<float, float>>((orig) =>
                        {
                            return orig * 2f;
                        });
                    }
                }
            }
        }
    }
}
