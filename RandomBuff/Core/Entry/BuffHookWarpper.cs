using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Reflection;
using UnityEngine;

namespace RandomBuff.Core.Entry
{
    internal static class BuffHookWarpper
    {
        static readonly Dictionary<BuffID, Action> registedAddHooks = new Dictionary<BuffID, Action>();
        static readonly Dictionary<BuffID, Action> registedRemoveHooks = new Dictionary<BuffID, Action>();

        static readonly Dictionary<BuffID, List<Hook>> registedRuntimeHooks = new ();

        public static void RegisterHook(BuffID id, Type type)
        {
            if (type.GetMethod("HookOn", BindingFlags.Static | BindingFlags.Public) == null)
                BuffPlugin.LogError("No HookOn");
            else
            {
                ILHook hook = new ILHook(type.GetMethod("HookOn", BindingFlags.Static | BindingFlags.Public),
                    (il) => RegisterHook_Impl(id, type, il));
            }
        }


        public static void EnableBuff(BuffID buffID)
        {
            if (registedAddHooks.ContainsKey(buffID))
            {
                try
                {
                    registedAddHooks[buffID].Invoke();
                    BuffPlugin.Log($"Invoke add of {buffID}");
                }
                catch (Exception ex)
                {
                    BuffPlugin.Log($"BuffHookWarpper : Exception when enable hook ");
                    BuffPlugin.LogException(ex);
                }
            }
        }

        public static void DisableBuff(BuffID buffID)
        {

            if (registedRemoveHooks.ContainsKey(buffID))
            {
                try
                {
                    registedRemoveHooks[buffID].Invoke();
                    BuffPlugin.Log($"Invoke remove of {buffID}");
                }
                catch (Exception ex)
                {
                    BuffPlugin.Log($"BuffHookWarpper : Exception when enable hook ");
                    BuffPlugin.LogException(ex);
                }
            }
        }



        private static void RegisterHook_Impl(BuffID id, Type type, ILContext il)
        {
            DynamicMethodDefinition method =
                new DynamicMethodDefinition($"BuffDisableHook_{id}", typeof(void), Type.EmptyTypes);
            var hookAssembly = typeof(On.Player).Assembly;

            var ilProcessor = method.GetILProcessor();
            foreach (var v in il.Body.Variables)
                ilProcessor.Body.Variables.Add(new VariableDefinition(v.VariableType));

            foreach (var str in il.Instrs)
            {
                if (str.MatchCallOrCallvirt(out var m))
                {
                    if (m.Name.Contains("add") && hookAssembly.GetType(m.DeclaringType.FullName) != null)
                    {
                        ilProcessor.Emit(OpCodes.Call,
                            hookAssembly.GetType(m.DeclaringType.FullName).GetMethod(m.Name.Replace("add", "remove")));
                        //BuffPlugin.Log($"Add {m.Name.Replace("add", "remove")}");
                        continue;
                    }
                }
                else if (str.MatchNewobj<Hook>() && str.MatchNewobj(out var ctor))
                {
                    for (int i = 0; i < ctor.Parameters.Count; i++)
                        ilProcessor.Emit(OpCodes.Pop);
                    ilProcessor.Emit(OpCodes.Ldnull);
                    //BuffPlugin.Log($"Remove RuntimeDetour in remove function");
                    continue;
                }
                else if (str.OpCode == OpCodes.Ret)
                {
                    ilProcessor.Emit(OpCodes.Ldstr, id.value);
                    ilProcessor.Emit(OpCodes.Call, typeof(BuffHookWarpper).GetMethod("RemoveRuntimeHook", BindingFlags.NonPublic | BindingFlags.Static));
                }
                ilProcessor.Append(str);

            }
            //foreach (var a in il.Instrs)
            //    Print(a);
            //BuffPlugin.Log($"------------");
            //foreach (var a in method.Definition.Body.Instructions)
            //    Print(a);

            ILCursor c = new ILCursor(il);
            while (c.TryGotoNext(MoveType.After, i => i.MatchNewobj<Hook>()))
                c.EmitDelegate<Func<Hook, Hook>>(hook => AddRuntimeHook(id, hook));

            if (!registedAddHooks.ContainsKey(id))
                registedAddHooks.Add(id, type.GetMethod("HookOn").CreateDelegate<Action>());
            if (!registedRemoveHooks.ContainsKey(id))
                registedRemoveHooks.Add(id, method.Generate().CreateDelegate<Action>());
            if (!registedRuntimeHooks.ContainsKey(id))
                registedRuntimeHooks.Add(id, new List<Hook>());
        }

        private static void Print(Instruction instr)
        {
            try
            {
                BuffPlugin.Log($"{instr}");
            }
            catch (Exception e)
            {
                BuffPlugin.Log($"{instr.Offset}: {instr.OpCode} _____");
            }
        }

        private static Hook AddRuntimeHook(BuffID id, Hook hook)
        {
            registedRuntimeHooks[id].Add(hook);
            return hook;
        }
        private static void RemoveRuntimeHook(string idStr)
        {
            var id = new BuffID(idStr);
            registedRuntimeHooks[id].ForEach(i => i.Dispose());
            registedRuntimeHooks[id].Clear();
        }

    }
}
