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


    internal class RamenpedeBuffEntry : IBuffEntry
    {
        public static BuffID ramenpedeBuffID = new BuffID("Ramenpede", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<RamenpedeBuffEntry>(ramenpedeBuffID);
        }

        public static void HookOn()
        {
            IL.Centipede.ctor += Centipede_ctor;
        }

        private static void Centipede_ctor(ILContext il)
        {
            ILCursor c1 = new ILCursor(il);
            if(c1.TryGotoNext(MoveType.After,i => i.MatchNewarr<BodyChunk>()))
            {
                c1.Emit(OpCodes.Ldarg_0);
                c1.EmitDelegate<Func<BodyChunk[],Centipede, BodyChunk[]>>((orig,self) =>
                {
                    if(!self.Small)
                        return new BodyChunk[orig.Length * 2];
                    return orig;
                });
            }
        }
    }
}
