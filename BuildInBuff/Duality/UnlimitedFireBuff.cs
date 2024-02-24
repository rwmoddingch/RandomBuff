using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;


namespace BuiltinBuffs.Duality
{
    internal class UnlimitedFirepowerBuff : Buff<UnlimitedFirepowerBuff, UnlimitedFirepowerBuffData>
    {
        public override BuffID ID => UnlimitedFirepowerIBuffEntry.UnlimitedFirepowerBuffID;
    }

    internal class UnlimitedFirepowerBuffData : BuffData
    {
        public override BuffID ID => UnlimitedFirepowerIBuffEntry.UnlimitedFirepowerBuffID;
    }

    internal class UnlimitedFirepowerIBuffEntry : IBuffEntry
    {
        public static BuffID UnlimitedFirepowerBuffID = new BuffID("UnlimitedFirepower", true);
        static ILCursor c1;
        public  void OnEnable()
        {
            BuffRegister.RegisterBuff<UnlimitedFirepowerBuff, UnlimitedFirepowerBuffData, UnlimitedFirepowerIBuffEntry>(UnlimitedFirepowerBuffID);
        }

        public static void HookOn()
        {
            IL.ScavengerBomb.Explode += ScavengerBomb_Explode;
            IL.ExplosiveSpear.Explode += ExplosiveSpear_Explode;
            IL.FirecrackerPlant.Explode += FirecrackerPlant_Explode;
            //On.UpdatableAndDeletable.Destroy += UpdatableAndDeletable_Destroy;
        }

        private static void FirecrackerPlant_Explode(ILContext il)
        {
            ApplyILHook<FirecrackerPlant>(il, (self) =>
            {
                for(int i = 0; i < self.lumpsPopped.Length; i++)
                {
                    self.lumpsPopped[i] = false;
                }
                self.fuseCounter = 0;
            });
        }

        private static void ExplosiveSpear_Explode(ILContext il)
        {
            ApplyILHook<ExplosiveSpear>(il, (self) => 
            { 
                self.exploded = false;
                self.igniteCounter = 0;
            });
        }

        //private static void UpdatableAndDeletable_Destroy(On.UpdatableAndDeletable.orig_Destroy orig, UpdatableAndDeletable self)
        //{
        //    orig.Invoke(self);
        //    if (self is ScavengerBomb)
        //    {
        //        EmgTxCustom.Log("Bomb destroyed");
        //        var instr = c1.Next;
        //        while (instr != null)
        //        {
        //            EmgTxCustom.Log(instr);
        //            instr = instr.Next;
        //        }
        //    }
        //}

        private static void ScavengerBomb_Explode(MonoMod.Cil.ILContext il)
        {
            ApplyILHook<ScavengerBomb>(il, (self) => { self.ignited = false; });
        }

        private static void ApplyILHook<T>(ILContext il,Action<T> func)
        {
            ILCursor c1 = new ILCursor(il);
            if (c1.TryGotoNext(MoveType.Before,
               (i) => i.MatchLdarg(0),
               (i) => i.MatchCallvirt<UpdatableAndDeletable>("Destroy"),
               (i) => i.MatchRet()
            ))
            {
                c1.Index++;
                c1.EmitDelegate<Action<T>>(func);
                c1.Emit(OpCodes.Ret);
                c1.Emit(OpCodes.Ldarg_0);
            }
            else
            {
                BuffUtils.Log(UnlimitedFirepowerBuffID, new NullReferenceException("c1 cant find"));
            }
        }
    }
}
