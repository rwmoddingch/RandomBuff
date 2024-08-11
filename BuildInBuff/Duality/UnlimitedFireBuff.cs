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
using System.Runtime.CompilerServices;

namespace BuiltinBuffs.Duality
{
  

    internal class UnlimitedFirepowerIBuffEntry : IBuffEntry
    {
        public static BuffID UnlimitedFirepowerBuffID = new BuffID("UnlimitedFirepower", true);
        static ILCursor c1;
        public  void OnEnable()
        {
            BuffRegister.RegisterBuff<UnlimitedFirepowerIBuffEntry>(UnlimitedFirepowerBuffID);
        }

        public static void HookOn()
        {
            IL.ScavengerBomb.Explode += ScavengerBomb_Explode;
            IL.ExplosiveSpear.Explode += ExplosiveSpear_Explode;
            IL.FirecrackerPlant.Explode += FirecrackerPlant_Explode;

            On.ScavengerBomb.Explode += ScavengerBomb_Explode1;
            On.ExplosiveSpear.Explode += ExplosiveSpear_Explode1;
            On.FirecrackerPlant.Explode += FirecrackerPlant_Explode1;

            On.Weapon.Update += Weapon_Update;
        }

        private static void Weapon_Update(On.Weapon.orig_Update orig, Weapon self, bool eu)
        {
            orig.Invoke(self, eu);
            if (ExplodeCD.cdMapper.TryGetValue(self, out var cd))
                cd.Update(self);
        }

        private static void FirecrackerPlant_Explode1(On.FirecrackerPlant.orig_Explode orig, FirecrackerPlant self)
        {
            if (ExplodeCD.cdMapper.TryGetValue(self, out var cd) && cd.cd > 0)
                return;
            orig.Invoke(self);
        }

        private static void ExplosiveSpear_Explode1(On.ExplosiveSpear.orig_Explode orig, ExplosiveSpear self)
        {
            if (ExplodeCD.cdMapper.TryGetValue(self, out var cd) && cd.cd > 0)
                return;
            orig.Invoke(self);
        }

        private static void ScavengerBomb_Explode1(On.ScavengerBomb.orig_Explode orig, ScavengerBomb self, BodyChunk hitChunk)
        {
            if (ExplodeCD.cdMapper.TryGetValue(self, out var cd) && cd.cd > 0)
                return;
            orig.Invoke(self, hitChunk);
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
                ExplodeCD.cdMapper.Add(self, new ExplodeCD());
            });
        }

        private static void ExplosiveSpear_Explode(ILContext il)
        {
            ApplyILHook<ExplosiveSpear>(il, (self) => 
            { 
                self.exploded = false;
                self.igniteCounter = 0;
                ExplodeCD.cdMapper.Add(self, new ExplodeCD());
            });
        }

        private static void ScavengerBomb_Explode(MonoMod.Cil.ILContext il)
        {
            ApplyILHook<ScavengerBomb>(il, (self) => 
            { 
                self.ignited = false; 
                ExplodeCD.cdMapper.Add(self, new ExplodeCD()); 
            });
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

        public class ExplodeCD
        {
            public static ConditionalWeakTable<Weapon, ExplodeCD> cdMapper = new ConditionalWeakTable<Weapon, ExplodeCD>();

            public int cd;

            public ExplodeCD()
            {
                cd = 8;
            }

            public void Update(Weapon weapon)
            {
                if(cd > 0)
                {
                    cd--;
                    if(cd == 0)
                    {
                        cdMapper.Remove(weapon);
                    }
                }
            }
        }
    }
}
