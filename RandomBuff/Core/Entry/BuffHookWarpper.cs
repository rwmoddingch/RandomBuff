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
using On.Menu;
using RandomBuff.Core.Game.Settings.Conditions;
using UnityEngine;

namespace RandomBuff.Core.Entry
{
    internal static partial class BuffHookWarpper
    {

        public static void RegisterHook(BuffID id, Type type)
        {
            if (type.GetMethod("HookOn", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) == null)
                BuffPlugin.LogWarning("No HookOn");
            else
            {
                var method = type.GetMethod("HookOn", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                ILHook hook = new ILHook(method,
                    (il) => RegisterHook_Impl(id, type, il, method, HookLifeTimeLevel.InGame));
            }

            if (type.GetMethod("LongLifeCycleHookOn", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) != null)
            {
                var method = type.GetMethod("LongLifeCycleHookOn", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                ILHook hook = new ILHook(method,
                    (il) => RegisterHook_Impl(id, type, il, method, HookLifeTimeLevel.UntilQuit));
            }
        }

        public static void RegisterConditionHook<TCondition>() where TCondition : Condition
        {
            var type = typeof(TCondition);
            var origMethod = type.GetMethod("HookOn", BindingFlags.Public | BindingFlags.Instance);
            RegisterConditionHook_Impl<TCondition>(type, origMethod);
        }


        public static void CheckAndDisableAllHook()
        {
            List<(BuffID, HookLifeTimeLevel)> list = new ();
            foreach (var dic in HasEnabled)
            {
                foreach (var item in dic.Value)
                {
                    if (item.Value)
                        list.Add((dic.Key,item.Key));
                }
            }

            foreach (var needDisable in list)
            {
                BuffPlugin.LogError($"Fallback Disable Hook {needDisable.Item1}:{needDisable.Item2}, Forget call DisableBuff?");
                DisableBuff(needDisable.Item1,needDisable.Item2);
            }
        }


        public static void EnableBuff(BuffID buffID, HookLifeTimeLevel level)
        {
            if (RegistedAddHooks.TryGetValue(buffID, out var dic) &&
                dic.TryGetValue(level, out var action))
            {
                if (HasEnabled[buffID][level])
                {
                    BuffPlugin.LogError($"Already Enabled {buffID}:{level}");
                    return;
                }

                try
                {
                    action.Invoke();
                    HasEnabled[buffID][level] = true;
                    BuffPlugin.Log($"HookWarpper Enable Buff - {buffID}:{level}");
                }
                catch (Exception ex)
                {
                    BuffPlugin.LogException(ex, $"BuffHookWarpper : Exception when enable hook for {buffID}:{level}");
                }
            }
        }

        public static void DisableBuff(BuffID buffID, HookLifeTimeLevel level)
        {
            if (RegistedRemoveHooks.TryGetValue(buffID, out var dic) &&
                dic.TryGetValue(level, out var action))
            {
                if (!HasEnabled[buffID][level])
                {
                    BuffPlugin.LogError($"Already Disabled {buffID}:{level}");
                    return;
                }

                try
                {
                    action.Invoke();
                    HasEnabled[buffID][level] = false;
                    BuffPlugin.Log($"HookWarpper Disable Buff - {buffID}:{level}");
                }
                catch (Exception ex)
                {
                    BuffPlugin.LogException(ex, $"BuffHookWarpper : Exception when enable hook for {buffID}:{level}");
                }
            }
        }


        public static void DisableCondition(Condition condition)
        {
            if (RegistedRemoveCondition.TryGetValue(condition.ID, out var method))
            {
                BuffPlugin.Log($"HookWarpper Disable Condition - {condition.ID}");
                method.Invoke(condition);
            }
            else
            {
                BuffPlugin.LogError($"HookWarpper No Find Condition - {condition.ID}");

            }
        }

    }

    internal static partial class BuffHookWarpper
    {

        private static void RegisterConditionHook_Impl<TCondition>(Type type, MethodInfo origMethod) where TCondition : Condition
        {
            if (hookAssembly == null)
                hookAssembly = typeof(On.Player).Assembly;
            DynamicMethodDefinition method = new(origMethod);
            method.Definition.Body.Instructions.Clear();
            method.Definition.Body.ExceptionHandlers.Clear();
            ILProcessor ilProcessor = method.GetILProcessor();
            BuffPlugin.Log($"RegisterCondition - {Helper.GetUninit<Condition>(type).ID}");

            
            _ = new ILHook(origMethod, (il) =>
            {
                bool hasShownBranchMessage = false;

                List<(Instruction target, Instruction from)> labelList = new();
                Dictionary<Instruction, Instruction> labelDictionary = new();
                foreach (var str in il.Instrs)
                {

                    if (str.MatchCallOrCallvirt(out var m) && m.Name.Contains("add"))
                    {
                        if (TryGetRemoveMethod(origMethod, m, out var removeMethod))
                        {
                            ilProcessor.Emit(OpCodes.Call, removeMethod);
                            labelDictionary.Add(str, ilProcessor.Body.Instructions.Last());
                        }
                        else
                            BuffPlugin.LogError($"Can't find remove for:{m.FullName}");
                    }
                    else if (str.MatchNewobj(out var ctor) && ctor.ReturnType.Is(typeof(IDetour)))
                    {
                        if (ctor.Parameters.Count != 0)
                        {
                            ilProcessor.Emit(OpCodes.Pop);
                            labelDictionary.Add(str, ilProcessor.Body.Instructions.Last());

                            for (int i = 1; i < ctor.Parameters.Count; i++)
                                ilProcessor.Emit(OpCodes.Pop);

                            ilProcessor.Emit(OpCodes.Ldnull);

                        }
                        else
                        {
                            ilProcessor.Emit(OpCodes.Ldnull);
                            labelDictionary.Add(str, ilProcessor.Body.Instructions.Last());

                        }

                    }
                    else if (str.Operand is ILLabel label)
                    {
                        var from = ilProcessor.Create(str.OpCode, str);
                        ilProcessor.Append(from);
                        labelDictionary.Add(str, ilProcessor.Body.Instructions.Last());

                        labelList.Add(new(label.Target, from));
                        if (!hasShownBranchMessage)
                        {
                            BuffPlugin.LogWarning("Find Branch in HookOn, maybe cause error!");
                            hasShownBranchMessage = true;
                        }
                    }
                    else
                    {
                        ilProcessor.Append(str);
                        labelDictionary.Add(str, ilProcessor.Body.Instructions.Last());

                    }
                }

                foreach (var exception in il.Body.ExceptionHandlers)
                {
                    var re = new ExceptionHandler(exception.HandlerType)
                    {
                        TryStart = labelDictionary[exception.TryStart],
                        TryEnd = labelDictionary[exception.TryEnd],
                        HandlerStart = labelDictionary[exception.HandlerStart],
                        HandlerEnd = labelDictionary[exception.HandlerEnd],
                        CatchType = exception.CatchType
                    };
                    method.Module.ImportReference(exception.CatchType);
                    if (exception.FilterStart != null)
                        re.FilterStart = labelDictionary[exception.FilterStart];
                    ilProcessor.Body.ExceptionHandlers.Add(re);
                }

                foreach (var pair in labelList)
                    pair.from.Operand = labelDictionary[pair.target];


                ILCursor c = new ILCursor(il);
                while (c.TryGotoNext(MoveType.After, i => i.MatchNewobj(out var newObj) && newObj.ReturnType.Is(typeof(IDetour))))
                {
                    c.Emit(OpCodes.Dup);
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldfld,
                        typeof(Condition).GetField("runtimeHooks", BindingFlags.Instance | BindingFlags.NonPublic));
                    c.Emit(OpCodes.Call, typeof(List<IDetour>).GetMethod(nameof(List<IDetour>.Add)));
                }

;           


            });
            var deg = method.Generate().CreateDelegate<Action<TCondition>>();
            RegistedRemoveCondition.Add(Helper.GetUninit<Condition>(type).ID, (condition => deg.Invoke(condition as TCondition)));

        }


        private static void RegisterHook_Impl(BuffID id, Type type, ILContext il, MethodBase origMethod, HookLifeTimeLevel level)
        {
            if(hookAssembly == null)
                hookAssembly = typeof(On.Player).Assembly;

            bool hasShownBranchMessage = false;

            DynamicMethodDefinition method = new ($"BuffDisableHook_{id}_{level}", typeof(void), Type.EmptyTypes);
            var ilProcessor = method.GetILProcessor();
            foreach (var v in il.Body.Variables)
                ilProcessor.Body.Variables.Add(new VariableDefinition(v.VariableType));

            List<(Instruction target, Instruction from)> labelList = new();
            Dictionary<Instruction, Instruction> labelDictionary = new();
            foreach (var str in il.Instrs)
            {
            
                if (str.MatchCallOrCallvirt(out var m) && m.Name.Contains("add"))
                {
                    if (TryGetRemoveMethod(origMethod, m, out var removeMethod))
                    {
                        ilProcessor.Emit(OpCodes.Call, removeMethod);
                        labelDictionary.Add(str,ilProcessor.Body.Instructions.Last());
                    }
                    else
                        BuffPlugin.LogError($"Can't find remove for:{m.FullName}");
                }
                else if (str.MatchNewobj(out var ctor) && ctor.ReturnType.Is(typeof(IDetour)))
                {
                    if (ctor.Parameters.Count != 0)
                    {
                        ilProcessor.Emit(OpCodes.Pop);
                        labelDictionary.Add(str, ilProcessor.Body.Instructions.Last());

                        for (int i = 1; i < ctor.Parameters.Count; i++)
                            ilProcessor.Emit(OpCodes.Pop);

                        ilProcessor.Emit(OpCodes.Ldnull);

                    }
                    else
                    {
                        ilProcessor.Emit(OpCodes.Ldnull);
                        labelDictionary.Add(str, ilProcessor.Body.Instructions.Last());

                    }

                }
                else if (str.OpCode == OpCodes.Ret)
                {
                    ilProcessor.Emit(OpCodes.Ldstr, id.value);
                    labelDictionary.Add(str, ilProcessor.Body.Instructions.Last());


                    ilProcessor.Emit(OpCodes.Ldstr, level.ToString());

                    ilProcessor.Emit(OpCodes.Call, typeof(BuffHookWarpper).GetMethod("RemoveRuntimeHook", BindingFlags.NonPublic | BindingFlags.Static));
                    ilProcessor.Append(str);

                }
                else if (str.Operand is ILLabel label)
                {
                    var from = ilProcessor.Create(str.OpCode, str);
                    ilProcessor.Append(from);
                    labelDictionary.Add(str, ilProcessor.Body.Instructions.Last());

                    labelList.Add(new (label.Target,from));
                    if (!hasShownBranchMessage)
                    {
                        BuffPlugin.LogWarning("Find Branch in HookOn, maybe cause error!");
                        hasShownBranchMessage = true;
                    }
                }
                else
                {
                    ilProcessor.Append(str);
                    labelDictionary.Add(str, ilProcessor.Body.Instructions.Last());

                }
            }

            foreach (var exception in il.Body.ExceptionHandlers)
            {
                ilProcessor.Body.ExceptionHandlers.Add(new ExceptionHandler(exception.HandlerType)
                {
                    TryStart = labelDictionary[exception.TryStart],
                    TryEnd = labelDictionary[exception.TryEnd],
                    HandlerStart = labelDictionary[exception.HandlerStart],
                    HandlerEnd = labelDictionary[exception.HandlerEnd],
                    CatchType = exception.CatchType,
                });
            }
            foreach (var pair in labelList)
                pair.from.Operand = labelDictionary[pair.target];
            



            RegistedAddHooks.JustAAdd(id, level, type.GetMethod(origMethod.Name).CreateDelegate<Action>());
            RegistedRemoveHooks.JustAAdd(id, level, method.Generate().CreateDelegate<Action>());
            RegistedRuntimeHooks.JustAAdd(id, level, new List<IDetour>());
            HasEnabled.JustAAdd(id, level, false);

            ILCursor c = new ILCursor(il);
            while (c.TryGotoNext(MoveType.After,
                       i => i.MatchNewobj(out var newObj) && newObj.ReturnType.Is(typeof(IDetour))))
            {
                c.Emit(OpCodes.Dup);
                c.EmitDelegate<Action<IDetour>>(hook => AddRuntimeHook(id, level, hook));
            }



        }

        private static void JustAAdd<T>(this Dictionary<BuffID, Dictionary<HookLifeTimeLevel, T>> dics, BuffID id,HookLifeTimeLevel level, T data)
        {
            if (!dics.ContainsKey(id)) dics.Add(id, new ());
            if (!dics[id].ContainsKey(level)) dics[id].Add(level, data);
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

        internal static bool TryGetRemoveMethod(MethodBase origMethod, MethodReference methodRef, out MethodInfo method)
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

  
        private static void AddRuntimeHook(BuffID id, HookLifeTimeLevel level, IDetour hook)
        {
            RegistedRuntimeHooks[id][level].Add(hook);
        }
        private static void RemoveRuntimeHook(string idStr,string levelStr)
        {
            var id = new BuffID(idStr);
            if (Enum.TryParse(levelStr, out HookLifeTimeLevel level))
            {
                BuffPlugin.LogDebug($"RemoveRuntimeHook: {idStr},{levelStr}, Count:{RegistedRuntimeHooks[id][level].Count}");
                RegistedRuntimeHooks[id][level].ForEach(i => i.Dispose());
            }

            RegistedRuntimeHooks[id][level].Clear();
        }

        private static Dictionary<string, Assembly> resolvedAssembly = new();
        private static Assembly hookAssembly;


        private static readonly Dictionary<BuffID, Dictionary<HookLifeTimeLevel, Action>> RegistedAddHooks = new();
        private static readonly Dictionary<BuffID, Dictionary<HookLifeTimeLevel, Action>> RegistedRemoveHooks = new();
        private static readonly Dictionary<BuffID, Dictionary<HookLifeTimeLevel, List<IDetour>>> RegistedRuntimeHooks = new();
        private static readonly Dictionary<BuffID, Dictionary<HookLifeTimeLevel, bool>> HasEnabled = new();


        private static readonly Dictionary<ConditionID,Action<Condition>> RegistedRemoveCondition = new();

    }
}
