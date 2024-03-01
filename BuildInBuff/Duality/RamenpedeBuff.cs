using Mono.Cecil.Cil;
using MonoMod.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuiltinBuffs.Duality 
{
    internal class RamenpedeBuff : Buff<RamenpedeBuff, RamenpedeBuffData>
    {
        public override BuffID ID => RamenpedeBuffEntry.ramenpedeBuffID;
    }

    internal class RamenpedeBuffData : BuffData 
    {
        public override BuffID ID => RamenpedeBuffEntry.ramenpedeBuffID;
    }

    internal class RamenpedeBuffEntry : IBuffEntry
    {
        public static BuffID ramenpedeBuffID = new BuffID("Ramenpede", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<RamenpedeBuff, RamenpedeBuffData, RamenpedeBuffEntry>(ramenpedeBuffID);
        }

        public static void HookOn()
        {
            IL.Centipede.ctor += Centipede_ctor;
        }

        private static void Centipede_ctor(MonoMod.Cil.ILContext il)
        {
            ILCursor c1 = new ILCursor(il);
            if(c1.TryGotoNext(MoveType.After,i => i.MatchNewarr<BodyChunk>())){
                c1.EmitDelegate<Func<BodyChunk[], BodyChunk[]>>(orig =>
                {
                    return new BodyChunk[orig.Length * 2];
                });
            }
        }
    }
}
