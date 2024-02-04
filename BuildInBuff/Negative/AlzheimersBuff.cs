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

namespace BuiltinBuffs.Negative
{
    internal class AlzheimersBuff : Buff<AlzheimersBuff, AlzheimersBuffData>
    {
        public override BuffID ID => AlzheimersIBuffEntry.AlzheimersBuffID;
    }

    internal class AlzheimersBuffData : BuffData
    {
        public override BuffID ID => AlzheimersIBuffEntry.AlzheimersBuffID;
    }

    internal class AlzheimersIBuffEntry : IBuffEntry
    {
        public static BuffID AlzheimersBuffID = new BuffID("Alzheimers", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<AlzheimersBuff, AlzheimersBuffData, AlzheimersIBuffEntry>(AlzheimersBuffID);
        }

        public static void HookOn()
        {
            IL.CoralBrain.CoralNeuronSystem.PlaceSwarmers += CoralNeuronSystem_PlaceSwarmers;
        }

        private static void CoralNeuronSystem_PlaceSwarmers(MonoMod.Cil.ILContext il)
        {
            ILCursor c1 = new ILCursor(il);
            if(c1.TryGotoNext(MoveType.After,
                (i) => i.MatchLdloc(5),
                (i) => i.MatchLdloc(2),
                (i) => i.Match(OpCodes.Blt_S)))
            {
                c1.Index--;
                c1.EmitDelegate<Func<int, int>>((orig) =>
                {
                    return Mathf.CeilToInt(orig / 20f);
                });
            }
        }
    }
}
