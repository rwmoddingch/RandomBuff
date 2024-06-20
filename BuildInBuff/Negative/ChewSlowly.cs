using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;

namespace HotDogGains.Negative
{
    class ChewSlowlyBuff : Buff<ChewSlowlyBuff, ChewSlowlyBuffData> { public override BuffID ID => ChewSlowlyBuffEntry.ChewSlowlyID; }
    class ChewSlowlyBuffData : BuffData { public override BuffID ID => ChewSlowlyBuffEntry.ChewSlowlyID; }
    class ChewSlowlyBuffEntry : IBuffEntry
    {
        public static BuffID ChewSlowlyID = new BuffID("ChewSlowlyID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ChewSlowlyBuff, ChewSlowlyBuffData, ChewSlowlyBuffEntry>(ChewSlowlyID);
            ///细嚼慢咽
            ///增加吃小零食的时间
        }
        public static void HookOn()
        {
            IL.Player.GrabUpdate += AddEatCountLimite;
        }

        private static void AddEatCountLimite(ILContext il)
        {
            var c = new ILCursor(il);

            //修改每口的间隔
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchStfld<Player>("eatCounter"),
                i => i.MatchLdarg(0),
                i => i.MatchLdarg(1),
                i => i.MatchCall<Player>("BiteEdibleObject"),
                i => i.Match(OpCodes.Br),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Player>("eatCounter"),
                i => i.MatchLdcI4(20)
                ))
            {
                c.Emit(OpCodes.Ldc_I4, 80);
                c.Emit(OpCodes.Add);

            }

            //进食预备时间
            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Player>("eatCounter"),
                i => i.MatchLdcI4(1),
                i => i.MatchSub(),
                i => i.MatchStfld<Player>("eatCounter"),

                i => i.Match(OpCodes.Br_S),

                i => i.MatchLdloc(0),
                i => i.Match(OpCodes.Brtrue_S),

                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Player>("eatCounter"),
                i => i.MatchLdcI4(40)
                ))
            {
                // c.Emit(OpCodes.Ldarg_0);//这里开始0号位是input[0].x的值,1号位是玩家
                //c.Emit(OpCodes.Ldc_I4, (int)400);
                //c.Emit(OpCodes.Add);
            }

        }
    }
}