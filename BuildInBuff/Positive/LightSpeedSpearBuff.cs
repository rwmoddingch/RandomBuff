using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;
using RWCustom;

namespace BuiltinBuffs.Positive
{
    //TODO : 完成随机生成
    internal class LightSpeedSpearBuff : Buff<LightSpeedSpearBuff, LightSpeedSpearBuffData>
    {
        public override BuffID ID => LightSpeedSpearIBuffEntry.LightSpeedSpearBuffID;
    }

    internal class LightSpeedSpearBuffData : BuffData
    {
        public override BuffID ID => LightSpeedSpearIBuffEntry.LightSpeedSpearBuffID;
    }

    internal class LightSpeedSpearIBuffEntry : IBuffEntry
    {
        public static BuffID LightSpeedSpearBuffID = new BuffID("LightSpeedSpear", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<LightSpeedSpearBuff, LightSpeedSpearBuffData, LightSpeedSpearIBuffEntry>(LightSpeedSpearBuffID);
        }

        public static void HookOn()
        {
            On.Spear.Update += Spear_Update;
            IL.Room.Loaded += Room_Loaded;
        }

        private static void Room_Loaded(MonoMod.Cil.ILContext il)
        {
            ILCursor c1 = new ILCursor(il);
            c1.GotoNext(MoveType.After,
                (i) => i.MatchCall<SlugcatStats>("SpearSpawnExplosiveRandomChance"),
                (i) => i.MatchClt(),
                (i) => i.MatchNewobj<AbstractSpear>(),
                (i) => i.MatchStloc(71),
                (i) => i.MatchLdsfld<ModManager>("MSC"),
                (i) => i.Match(OpCodes.Brfalse_S));
            c1.Index -= 2;
            c1.Emit(OpCodes.Ldloc, 71);
            c1.EmitDelegate<Action<AbstractSpear>>((abSpear) =>
            {
                abSpear.explosive = false;
                abSpear.electric = false;
                abSpear.hue = Mathf.Lerp(0.35f, 0.6f, Custom.ClampedRandomVariation(0.5f, 0.5f, 2f));
            });
        }

        private static void Spear_Update(On.Spear.orig_Update orig, Spear self, bool eu)
        {
            orig.Invoke(self, eu);

            if(self.bugSpear)
            {
                for(int i = 0;i < 80 && self.mode == Weapon.Mode.Thrown; i++)
                {
                    orig.Invoke(self, eu);
                }
            }
        }
    }
}
