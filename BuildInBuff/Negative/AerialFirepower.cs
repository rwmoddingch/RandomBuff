using Mono.Cecil.Cil;
using MonoMod.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace HotDogGains.Negative
{
    class AerialFirepowerBuff : Buff<AerialFirepowerBuff, AerialFirepowerBuffData>{public override BuffID  ID => AerialFirepowerBuffEntry.AerialFirepowerID;}
    class AerialFirepowerBuffData :BuffData{public override BuffID ID => AerialFirepowerBuffEntry.AerialFirepowerID;}
    class AerialFirepowerBuffEntry : IBuffEntry
    {
        public static BuffID AerialFirepowerID = new BuffID("AerialFirepowerID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<AerialFirepowerBuff,AerialFirepowerBuffData,AerialFirepowerBuffEntry>(AerialFirepowerID);
        }
            public static void HookOn()
        {
            IL.KingTusks.Tusk.Update += Tusk_Update;
            On.KingTusks.Tusk.Update += Tusk_Update1;
            On.KingTusks.Tusk.Shoot += Tusk_Shoot;
        }

        private static void Tusk_Shoot(On.KingTusks.Tusk.orig_Shoot orig, KingTusks.Tusk self, Vector2 tuskHangPos)
        {
            orig.Invoke(self,tuskHangPos);
            //射击后下个矛的cd
            self.owner.noShootDelay = 3;
            self.laserPower = 1f;
        }

        private static void Tusk_Update1(On.KingTusks.Tusk.orig_Update orig, KingTusks.Tusk self)
        {
            orig.Invoke(self);
            if (self.stuck>0)
            {
                self.stuck = Mathf.Max(0, self.stuck - 0.1f);
            }
        }

        private static void Tusk_Update(MonoMod.Cil.ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<KingTusks.Tusk>("modeCounter"),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<KingTusks.Tusk>("owner"),
                i => i.MatchCallvirt<KingTusks>("get_CloseQuarters"),
                i => i.Match(OpCodes.Brtrue_S),
                i => i.MatchLdcI4(25)

                ))
            {
                c.EmitDelegate<Func<int, int>>(
                   (modeCount) =>
                   {
                       //没瞄准的射击所需时间
                       return 10;
                   }
               );
            }
            if (c.TryGotoNext(MoveType.After,
                i => i.Match(OpCodes.Br_S),
                i => i.MatchLdcI4(10)
                ))
            {
                c.EmitDelegate<Func<int, int>>(
                   (modeCount) =>
                   {
                       //瞄准了后的射击所需时间
                       return 5;
                   }
               );
            }


            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdcR4(0.0f),
                i => i.MatchStfld<KingTusks.Tusk>("attached"),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<KingTusks.Tusk>("modeCounter"),
                i => i.MatchLdcI4(80)
                ))
            {
                c.EmitDelegate<Func<int, int>>(
                   (modeCount) =>
                   {
                       //Debug.Log("快速进入拉绳");
                       return 0;
                   }
               );

            }

            //修改一次回收的绳子长度
            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdcR4(0.0f),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<KingTusks.Tusk>("currWireLength"),
                i => i.MatchLdsfld<KingTusks.Tusk>("maxWireLength"),
                i => i.MatchLdcR4(90)

                ))
            {
                c.EmitDelegate<Func<float,float>>(
                   (speed) =>
                   {
                       //Debug.Log("加速回收");
                       return 4f;
                   }
               );

            }

        }
    }
}