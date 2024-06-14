using MonoMod.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BuiltinBuffs.Duality
{
    internal class NapalmBuffEntry : IBuffEntry
    {
        public static BuffID napalmBuffID = new BuffID("Napalm", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<NapalmBuffEntry>(napalmBuffID);
        }

        public static void HookOn()
        {
            IL.ScavengerBomb.Explode += ScavengerBomb_Explode1;
            On.ScavengerBomb.Explode += ScavengerBomb_Explode;
        }

        private static void ScavengerBomb_Explode1(MonoMod.Cil.ILContext il)
        {
            ILCursor c1 = new ILCursor(il);
            if(c1.TryGotoNext(MoveType.After, 
                (i) => i.MatchNewobj<Explosion>(),
                (i) => i.MatchCallvirt<Room>("AddObject")))
            {
                if (c1.TryGotoPrev(MoveType.After,
                    (i) => i.MatchLdcR4(2f)))
                {
                    c1.EmitDelegate<Func<float, float>>((orig) =>
                    {
                        return orig * 0.1f;
                    });
                }
            }
        }

        private static void ScavengerBomb_Explode(On.ScavengerBomb.orig_Explode orig, ScavengerBomb self, BodyChunk hitChunk)
        {
            orig.Invoke(self, hitChunk);

            int count = 10;
            for (int i = 0; i < count; i++)
            {
                float angle = 360f * i / (float)count;
                Vector2 vel = Custom.DegToVec(angle) * 10f;
                Vector2 pos = self.room.MiddleOfTile(self.room.GetTilePosition(self.firstChunk.pos));
                var newNapalm = new Napalm(self.room, 40 * 15, 60f, 2f, pos, vel);
                self.room.AddObject(newNapalm);
            }
        }
    }
}
