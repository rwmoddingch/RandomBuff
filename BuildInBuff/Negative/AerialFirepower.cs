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
            //������¸�ì��cd
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

            c.GotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<KingTusks.Tusk>("modeCounter"),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<KingTusks.Tusk>("owner"),
                i => i.MatchCallvirt<KingTusks>("get_CloseQuarters"),
                i => i.Match(OpCodes.Brtrue_S),
                i => i.MatchLdcI4(25)

            );
            {
                c.EmitDelegate<Func<int, int>>(
                   (modeCount) =>
                   {
                       //û��׼���������ʱ��
                       return 10;
                   }
               );
            }

            c.GotoNext(MoveType.After,
                i => i.Match(OpCodes.Br_S),
                i => i.MatchLdcI4(10)
            );
            {
                c.EmitDelegate<Func<int, int>>(
                   (modeCount) =>
                   {
                       //��׼�˺���������ʱ��
                       return 5;
                   }
               );
            }


            c.GotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdcR4(0.0f),
                i => i.MatchStfld<KingTusks.Tusk>("attached"),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<KingTusks.Tusk>("modeCounter"),
                i => i.MatchLdcI4(80)
            );
            {
                c.EmitDelegate<Func<int, int>>(
                   (modeCount) =>
                   {
                       //Debug.Log("���ٽ�������");
                       return 0;
                   }
               );

            }

            //�޸�һ�λ��յ����ӳ���
            c.GotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdcR4(0.0f),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<KingTusks.Tusk>("currWireLength"),
                i => i.MatchLdsfld<KingTusks.Tusk>("maxWireLength"),
                i => i.MatchLdcR4(90)
            );
            {
                c.EmitDelegate<Func<float,float>>(
                   (speed) =>
                   {
                       //Debug.Log("���ٻ���");
                       return 4f;
                   }
               );

            }

        }
    }
}