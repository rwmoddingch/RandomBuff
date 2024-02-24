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
using Mono.Cecil;
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
                var method = type.GetMethod("HookOn", BindingFlags.Static | BindingFlags.Public);
                ILHook hook = new ILHook(method, (il) => RegisterHook_Impl(id, type, il, method));
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
                    BuffPlugin.LogException(ex, $"BuffHookWarpper : Exception when enable hook for {buffID}");
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
                    BuffPlugin.LogException(ex, $"BuffHookWarpper : Exception when enable hook for {buffID}");
                }
            }
        }



        private static void RegisterHook_Impl(BuffID id, Type type, ILContext il, MethodBase origMethod)
        {
            if(hookAssembly == null)
                hookAssembly = typeof(On.Player).Assembly;

            DynamicMethodDefinition method =
                new DynamicMethodDefinition($"BuffDisableHook_{id}", typeof(void), Type.EmptyTypes);
            var ilProcessor = method.GetILProcessor();
            foreach (var v in il.Body.Variables)
                ilProcessor.Body.Variables.Add(new VariableDefinition(v.VariableType));

            List<(Instruction target, Instruction from)> labelList = new();
           
            foreach (var str in il.Instrs)
            {
            
                if (str.MatchCallOrCallvirt(out var m) && m.Name.Contains("add") &&
                    TryGetRemoveMethod(origMethod,m,out var removeMethod))
                {
                    ilProcessor.Emit(OpCodes.Call, removeMethod);
                }
                else if (str.MatchNewobj<Hook>() && str.MatchNewobj(out var ctor))
                {
                    for (int i = 0; i < ctor.Parameters.Count; i++)
                        ilProcessor.Emit(OpCodes.Pop);
                    ilProcessor.Emit(OpCodes.Ldnull);
                }
                else if (str.OpCode == OpCodes.Ret)
                {
                    ilProcessor.Emit(OpCodes.Ldstr, id.value);
                    ilProcessor.Emit(OpCodes.Call, typeof(BuffHookWarpper).GetMethod("RemoveRuntimeHook", BindingFlags.NonPublic | BindingFlags.Static));
                    ilProcessor.Append(str);

                }
                else if (str.MatchBr(out var label) || str.MatchBrtrue(out label) || str.MatchBrfalse(out label))
                {
                    var from = ilProcessor.Create(str.OpCode, str);
                    ilProcessor.Append(from);
                    labelList.Add(new (label.Target,from));
                    BuffPlugin.LogWarning("Find Branch in HookOn, maybe cause error!");
                }
                else
                {
                    ilProcessor.Append(str);
                }

                foreach (var pair in labelList)
                {
                    if (str == pair.target)
                        pair.from.Operand = ilProcessor.Body.Instructions.Last();
                }
            }

            

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

        private static Dictionary<string, Assembly> resolvedAssembly = new ();

        private static Assembly hookAssembly;

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

        private static bool TryGetRemoveMethod(MethodBase origMethod, MethodReference methodRef, out MethodInfo method)
        {
            method = hookAssembly.GetType(methodRef.DeclaringType.FullName)?.
                GetMethod(methodRef.Name.Replace("add", "remove"));
            if (method != null)
                return true;

            LoadAllReferenceAssembly(origMethod);

            foreach (var ass in resolvedAssembly.Values)
            {
                method = ass.GetType(methodRef.DeclaringType.FullName)?.
                    GetMethod(methodRef.Name.Replace("add", "remove"));
                if (method != null)
                    return true;
            }
            return false;
        }


        private static void LoadAllReferenceAssembly(MethodBase origMethod)
        {
            foreach (var a in origMethod.Module.Assembly.GetReferencedAssemblies())
            {
                if (a.Name.Contains("UnityEngine") ||
                    a.Name.Contains("Mono") ||
                    a.Name.Contains("System") ||
                    a.Name is "mscorlib" or "Newtonsoft.Json" or "HOOKS-Assembly-CSharp")
                    continue;
                if (!resolvedAssembly.ContainsKey(a.FullName))
                {
                    resolvedAssembly.Add(a.FullName, Assembly.Load(a));
                    BuffPlugin.LogDebug($"load reference assembly: {a.FullName}");
                }
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
