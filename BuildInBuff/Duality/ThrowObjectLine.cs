using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.SaveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace HotDogGains.Duality
{
    class ThrowObjectLineBuff : Buff<ThrowObjectLineBuff, ThrowObjectLineBuffData> { public override BuffID ID => ThrowObjectLineBuffEntry.ThrowObjectLineID; }
    class ThrowObjectLineBuffData : BuffData { public override BuffID ID => ThrowObjectLineBuffEntry.ThrowObjectLineID; }
    class ThrowObjectLineBuffEntry : IBuffEntry
    {
        public static BuffID ThrowObjectLineID = new BuffID("ThrowObjectLineID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ThrowObjectLineBuff, ThrowObjectLineBuffData, ThrowObjectLineBuffEntry>(ThrowObjectLineID);


        }
        public static void HookOn()
        {
            On.PoleMimic.Update += PoleMimic_Update1;

        }


        private static void PoleMimic_Update1(On.PoleMimic.orig_Update orig, PoleMimic self, bool eu)
        {
            orig.Invoke(self, eu);

            for (int i = 0; i < self.tentacle.tChunks.Length; i++)
            {
                if (self.stickChunks[i] != null)
                {
                    self.stickChunks[i].vel.y +=20f;

                }
            }
            if (self.grasps[0] != null)
            {
                self.grasps[0].grabbedChunk.vel.y += 10;
            }
        }

        private static void PoleMimic_Update(ILContext il)
        {
            var c = new ILCursor(il);


            byte index = 6;

            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdfld<BodyChunk>("mass"),
                i => i.MatchLdcR4(0.18f),
                i => i.MatchAdd(),
                i => i.MatchDiv(),
                i => i.MatchStloc(6)
                ))
            {
                c.Emit(OpCodes.Ldloc_S, index);
                c.EmitDelegate<Func<float, float>>(
                    (vel) =>
                    {
                        //Debug.Log("反转抓取");
                        return -vel;
                    }
                );
                c.Emit(OpCodes.Stloc_S, index);
            }

            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdelemRef(),
                i => i.MatchCallvirt<Creature.Grasp>("get_grabbedChunk"),
                i => i.MatchLdfld<BodyChunk>("mass")
                ))
            {
                //c.Emit(OpCodes.Ldloc_S, 6);
                c.EmitDelegate<Func<float, float>>(
                    (vel) =>
                    {
                        return -vel;
                    }
                );
            }
        }

    }
}