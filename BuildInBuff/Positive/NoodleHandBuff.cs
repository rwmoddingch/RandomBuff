using MonoMod.Cil;
using RandomBuff;
using System;

using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;

namespace BuiltinBuffs.Positive
{


    internal class NoodleHandIBuffEntry : IBuffEntry
    {
        public static BuffID noodleHandBuffID = new BuffID("NoodleHand", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<NoodleHandIBuffEntry>(noodleHandBuffID);
        }

        public static void HookOn()
        {
            IL.Player.PickupCandidate += Player_PickupCandidate;
        }

        private static void Player_PickupCandidate(MonoMod.Cil.ILContext il)
        {
            ILCursor c1 = new ILCursor(il);
            if (c1.TryGotoNext(MoveType.After,
                (i) => i.MatchLdcR4(40),
                (i) => i.MatchAdd()
                ))
            {
                c1.EmitDelegate<Func<float, float>>(TwiceRange);
            }

            if (c1.TryGotoNext(MoveType.After,
                (i) => i.MatchLdcR4(20),
                (i) => i.MatchAdd()
                ))
            {
                c1.EmitDelegate<Func<float, float>>(TwiceRange);
            }

            float TwiceRange(float orig)
            {
                return orig * 2f;
            }
        }
    }
}
