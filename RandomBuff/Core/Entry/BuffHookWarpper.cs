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
using System.Runtime.CompilerServices;
using BepInEx;
using Mono.Cecil;
using On.Menu;
using RandomBuff.Core.Game.Settings.Conditions;
using UnityEngine;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using Mono.Cecil.Rocks;

namespace RandomBuff.Core.Entry
{
    internal static partial class BuffHookWarpper
    {

        public static void RegisterBuffHook(BuffID id, Type type)
        {
             HookLifeTimeLevel[] levels = new[] { HookLifeTimeLevel.InGame, HookLifeTimeLevel.UntilQuit };

             string GetHookName(HookLifeTimeLevel level) => level == HookLifeTimeLevel.InGame ? "HookOn" : "LongLifeCycleHookOn";

             foreach (var level in levels)
             {
                 if (type.GetMethod($"__BuffHook_{level}", BindingFlags.NonPublic | BindingFlags.Static) is { })
                 {
                     RegisterBuffHook_Impl(id, type, $"__BuffHook_{level}", $"__BuffDisableHook_{level}", level);
                 }
                 else if (type.GetMethod(GetHookName(level),
                              BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) is not {} method)
                 {
                     if(level == HookLifeTimeLevel.InGame)
                         BuffPlugin.LogWarning($"No HookOn, Type:{type.Name}");
                 }
                 else
                 {
                     _ = new ILHook(method, (il) => RegisterBuffHook_Impl(id, type, il, method, HookLifeTimeLevel.InGame));
                 }
             }
             
        }

        public static void RegisterConditionHook<TCondition>() where TCondition : Condition
        {
            var type = typeof(TCondition);
            var origMethod = type.GetMethod("HookOn", BindingFlags.Public | BindingFlags.Instance);
            RegisterConditionHook_Impl<TCondition>(type, origMethod);
        }


        /// <summary>
        /// 后备函数，强制清除所有Hook
        /// </summary>
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

        /// <summary>
        /// 在重开的时候关闭所有UntilQuit
        /// TODO:或许可以改成存档清除时处理
        /// </summary>
        /// <param name="allUsed"></param>
        public static void DisableAllUtilQuitNoUsed(HashSet<BuffID> allUsed)
        {
            foreach(var dic in HasEnabled)
            {
                if(!allUsed.Contains(dic.Key) && dic.Value.TryGetValue(HookLifeTimeLevel.UntilQuit,out var value) && value)
                    DisableBuff(dic.Key, HookLifeTimeLevel.UntilQuit);

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
                    action.Invoke(buffID.value);
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
                    action.Invoke(buffID.value);
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
        internal static bool HasInterface<T>(this TypeReference type)
        {

            if (type.SafeResolve() is { } def)
                return def.Interfaces.Any(i => i.InterfaceType.Is(typeof(T)));
            //BuffPlugin.LogWarning($"Can't resolve type:{type.Name}");
            return false;
        }


        private static void RegisterConditionHook_Impl<TCondition>(Type type, MethodInfo origMethod) where TCondition : Condition
        {
            if (hookAssembly == null)
                hookAssembly = typeof(On.Player).Assembly;
            DynamicMethodDefinition method = new($"BuffDisableHook_{type.Name}", origMethod.ReturnType,
                new []{type});
            ILProcessor ilProcessor = method.GetILProcessor();
            BuffPlugin.Log($"RegisterCondition - {Helper.GetUninit<Condition>(type).ID}");

            
            _ = new ILHook(origMethod, (il) =>
            {
                bool hasShownBranchMessage = false;
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
                            labelDictionary.Add(str, ilProcessor.Body.Instructions.Last());
                        }
                        else
                            BuffPlugin.LogError($"Can't find remove for:{m.FullName}");
                    }
                    else if (str.MatchNewobj(out var ctor) && ctor.DeclaringType.HasInterface<IDetour>())
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
                    method.Module.ImportReference(exception.CatchType);
                    var re = new ExceptionHandler(exception.HandlerType)
                    {
                        TryStart = labelDictionary[exception.TryStart],
                        TryEnd = labelDictionary[exception.TryEnd],
                        HandlerStart = labelDictionary[exception.HandlerStart],
                        HandlerEnd = labelDictionary[exception.HandlerEnd],
                        CatchType = exception.CatchType
                    };
                    if (exception.FilterStart != null)
                        re.FilterStart = labelDictionary[exception.FilterStart];
                    ilProcessor.Body.ExceptionHandlers.Add(re);
                }

                foreach (var pair in labelList)
                    pair.from.Operand = labelDictionary[pair.target];

                var tmpValueIndex = il.Body.Variables.Count;

                il.Body.Variables.Add(new VariableDefinition(il.Body.Method.Module.ImportReference(typeof(IDetour))));
                ILCursor c = new ILCursor(il);
                while (c.TryGotoNext(MoveType.After, i => i.MatchNewobj(out var newObj) && newObj.DeclaringType.HasInterface<IDetour>()))
                {
                    c.Emit(OpCodes.Dup);
                    c.Emit(OpCodes.Stloc, tmpValueIndex);
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldfld,
                        typeof(Condition).GetField("runtimeHooks", BindingFlags.Instance | BindingFlags.NonPublic));
                    c.Emit(OpCodes.Ldloc, tmpValueIndex);
                    c.Emit(OpCodes.Call, typeof(List<IDetour>).GetMethod(nameof(List<IDetour>.Add)));
                }

            });
   
            var deg = method.Generate().CreateDelegate<Action<TCondition>>();
            RegistedRemoveCondition.Add(Helper.GetUninit<Condition>(type).ID, (condition => deg.Invoke(condition as TCondition)));

        }


        private static void RegisterBuffHook_Impl(BuffID id, Type type, ILContext il, MethodBase origMethod, HookLifeTimeLevel level)
        {
            if(hookAssembly == null)
                hookAssembly = typeof(On.Player).Assembly;

            bool hasShownBranchMessage = false;

            DynamicMethodDefinition disableMethod = new ($"BuffDisableHook_{id}_{level}", typeof(void), new[] {typeof(string)});
            DynamicMethodDefinition method = new($"BuffHook_{id}_{level}", typeof(void), new[] { typeof(string) });

            var eil = method.GetILProcessor();
            var dil = disableMethod.GetILProcessor();

            foreach (var v in il.Body.Variables)
            {
                dil.Body.Variables.Add(new VariableDefinition(v.VariableType));
                eil.Body.Variables.Add(new VariableDefinition(v.VariableType));
            }

            Dictionary<ILProcessor, List<(Instruction target, Instruction from)>> labelList = new() {{dil, new()},{eil, new()}};
            Dictionary<ILProcessor, Dictionary<Instruction, Instruction>> labelDictionary = new() { { dil, new() }, { eil, new() } };
            foreach (var str in il.Instrs)
            {
            
                if (str.MatchCallOrCallvirt(out var m) && m.Name.Contains("add"))
                {
                    if (TryGetRemoveMethod(origMethod, m, out var removeMethod))
                    {
                        dil.Emit(OpCodes.Callvirt, removeMethod);
                        labelDictionary[dil].Add(str,dil.Body.Instructions.Last());
                    }
                    else
                        BuffPlugin.LogError($"Can't find remove for:{m.FullName}");

                    eil.Emit(OpCodes.Callvirt, m);
                    labelDictionary[eil].Add(str, eil.Body.Instructions.Last());

                }
                else if (str.MatchNewobj(out var ctor) && ctor.DeclaringType.HasInterface<IDetour>())
                {
                    if (ctor.Parameters.Count != 0)
                    {
                        dil.Emit(OpCodes.Pop);
                        labelDictionary[dil].Add(str, dil.Body.Instructions.Last());
                        for (int i = 1; i < ctor.Parameters.Count; i++)
                            dil.Emit(OpCodes.Pop);
                        dil.Emit(OpCodes.Ldnull);
                    }
                    else
                    {
                        dil.Emit(OpCodes.Ldnull);
                        labelDictionary[dil].Add(str, dil.Body.Instructions.Last());
                    }
                    eil.Append(str);
                    labelDictionary[eil].Add(str, eil.Body.Instructions.Last());

                    eil.Emit(OpCodes.Dup);
                    eil.Emit(OpCodes.Ldarg_0);
                    eil.Emit(OpCodes.Ldc_I4, (int)level);
                    eil.Emit(OpCodes.Call, typeof(BuffHookWarpper).GetMethod("AddRuntimeHook", BindingFlags.NonPublic | BindingFlags.Static));

                }
                else if (str.OpCode == OpCodes.Ret)
                {
                    dil.Emit(OpCodes.Ldarg_0);
                    labelDictionary[dil].Add(str, dil.Body.Instructions.Last());

                    dil.Emit(OpCodes.Ldc_I4, (int)level);
                    dil.Emit(OpCodes.Call, typeof(BuffHookWarpper).GetMethod("RemoveRuntimeHook", BindingFlags.NonPublic | BindingFlags.Static));
                    dil.Append(str);

                    eil.Append(str);
                    labelDictionary[eil].Add(str, eil.Body.Instructions.Last());

                }
                else if (str.Operand is ILLabel label)
                {
                    var dFrom = dil.Create(str.OpCode, str);
                    dil.Append(dFrom);
                    labelDictionary[dil].Add(str, dil.Body.Instructions.Last());
                    labelList[dil].Add(new (label.Target,dFrom));

                    var eFrom = eil.Create(str.OpCode, str);
                    eil.Append(eFrom);
                    labelDictionary[eil].Add(str, dil.Body.Instructions.Last());
                    labelList[eil].Add(new(label.Target, eFrom));

                    if (!hasShownBranchMessage)
                    {
                        BuffPlugin.LogWarning($"Find Branch in {type.Name}:HookOn, maybe cause error!");
                        hasShownBranchMessage = true;
                    }
                }
                else
                {
                    dil.Append(str);
                    labelDictionary[dil].Add(str, dil.Body.Instructions.Last());

                    eil.Append(str);
                    labelDictionary[eil].Add(str, eil.Body.Instructions.Last());

                }
            }

            foreach (var exception in il.Body.ExceptionHandlers)
            {
                foreach (var item in labelList)
                {
                    var re = new ExceptionHandler(exception.HandlerType)
                    {
                        TryStart = labelDictionary[item.Key][exception.TryStart],
                        TryEnd = labelDictionary[item.Key][exception.TryEnd],
                        HandlerStart = labelDictionary[item.Key][exception.HandlerStart],
                        HandlerEnd = labelDictionary[item.Key][exception.HandlerEnd],
                        CatchType = exception.CatchType
                    };
                    if (exception.FilterStart != null)
                        re.FilterStart = labelDictionary[item.Key][exception.FilterStart];
                    item.Key.Body.ExceptionHandlers.Add(re);
                    item.Key.Body.Method.Module.ImportReference(exception.CatchType);
                }
            }
            foreach(var item in labelList)
            foreach (var pair in item.Value)
                pair.from.Operand = labelDictionary[item.Key][pair.target];



            RegistedAddHooks.SimplyAdd(id, level, method.Generate().CreateDelegate<Action<string>>());
            RegistedRemoveHooks.SimplyAdd(id, level, disableMethod.Generate().CreateDelegate<Action<string>>());
            RegistedRuntimeHooks.SimplyAdd(id, level, new List<IDetour>());
            HasEnabled.SimplyAdd(id, level, false);

        }


        private static void RegisterBuffHook_Impl(BuffID id, Type type,string enableMethod, string disableMethod ,HookLifeTimeLevel level)
        {
            RegistedAddHooks.SimplyAdd(id, level, type.GetMethod(enableMethod, BindingFlags.Static | BindingFlags.NonPublic).CreateDelegate<Action<string>>());
            RegistedRemoveHooks.SimplyAdd(id, level, type.GetMethod(disableMethod, BindingFlags.Static | BindingFlags.NonPublic).CreateDelegate<Action<string>>());
            RegistedRuntimeHooks.SimplyAdd(id, level, new List<IDetour>());
            HasEnabled.SimplyAdd(id, level, false);
        }
        private static void SimplyAdd<T>(this Dictionary<BuffID, Dictionary<HookLifeTimeLevel, T>> dics, BuffID id,HookLifeTimeLevel level, T data)
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

  
        private static void AddRuntimeHook(IDetour hook, string id, HookLifeTimeLevel level)
        {
            RegistedRuntimeHooks[new BuffID(id)][level].Add(hook);
        }
        private static void RemoveRuntimeHook(string idStr, HookLifeTimeLevel level)
        {
            var id = new BuffID(idStr);
            BuffPlugin.LogDebug($"RemoveRuntimeHook: {idStr},{level}, Count:{RegistedRuntimeHooks[id][level].Count}");
            RegistedRuntimeHooks[id][level].ForEach(i => i.Dispose());
            RegistedRuntimeHooks[id][level].Clear();
        }

        private static Dictionary<string, Assembly> resolvedAssembly = new();
        private static Assembly hookAssembly;


        private static readonly Dictionary<BuffID, Dictionary<HookLifeTimeLevel, Action<string>>> RegistedAddHooks = new();
        private static readonly Dictionary<BuffID, Dictionary<HookLifeTimeLevel, Action<string>>> RegistedRemoveHooks = new();
        private static readonly Dictionary<BuffID, Dictionary<HookLifeTimeLevel, List<IDetour>>> RegistedRuntimeHooks = new();
        private static readonly Dictionary<BuffID, Dictionary<HookLifeTimeLevel, bool>> HasEnabled = new();


        private static readonly Dictionary<ConditionID,Action<Condition>> RegistedRemoveCondition = new();

    }



    //预处理部分
    internal static partial class BuffHookWarpper
    {
        internal static void BuildStaticHook(TypeDefinition type)
        {
            HookLifeTimeLevel[] levels = new[] { HookLifeTimeLevel.InGame, HookLifeTimeLevel.UntilQuit };

            BuffPlugin.Log($"Built static hook for Type:{type.FullName}");

            string GetHookName(HookLifeTimeLevel level) => level == HookLifeTimeLevel.InGame ? "HookOn" : "LongLifeCycleHookOn";

            foreach (var level in levels)
            {
                bool hasShownBranchMessage = false;
                if (type.FindMethod(GetHookName(level)) is not {} origMethod)
                    continue;
                MethodDefinition disableMethod = new($"__BuffDisableHook_{level}", MethodAttributes.Static | MethodAttributes.Private,type.Module.TypeSystem.Void)
                    { Parameters = { new ParameterDefinition(type.Module.TypeSystem.String) } };
                MethodDefinition method = new($"__BuffHook_{level}", MethodAttributes.Static | MethodAttributes.Private, type.Module.TypeSystem.Void)
                    { Parameters = { new ParameterDefinition(type.Module.TypeSystem.String) } };
                type.Methods.Add(disableMethod);
                type.Methods.Add(method);
                var attrCtor =
                    type.Module.ImportReference(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes));
                disableMethod.CustomAttributes.Add(new CustomAttribute(attrCtor));
                method.CustomAttributes.Add(new CustomAttribute(attrCtor));

                var eil = method.Body.GetILProcessor();
                var dil = disableMethod.Body.GetILProcessor();

                foreach (var v in origMethod.Body.Variables)
                {
                    dil.Body.Variables.Add(new VariableDefinition(v.VariableType));
                    eil.Body.Variables.Add(new VariableDefinition(v.VariableType));
                }

                Dictionary<ILProcessor, List<(Instruction target, Instruction from)>> labelList = new() { { dil, new() }, { eil, new() } };
                Dictionary<ILProcessor, Dictionary<Instruction, Instruction>> labelDictionary = new() { { dil, new() }, { eil, new() } };
                foreach (var str in origMethod.Body.Instructions)
                {
                    if (str.MatchCallOrCallvirt(out var m) && m.Name.Contains("add"))
                    {
                        if (m.DeclaringType.SafeResolve() is {} methodType) 
                        {
                            if (methodType.FindMethod(m.Name.Replace("add","remove")) is { } removeMethod)
                            {
                                dil.Emit(OpCodes.Callvirt, type.Module.ImportReference(removeMethod));
                                labelDictionary[dil].Add(str, dil.Body.Instructions.Last());
                            }
                            else
                            {
                                BuffPlugin.LogError($"Can't find remove for:{m.FullName}");
                            }
                        }
                        else
                            BuffPlugin.LogError($"Can't find method type for:{m.DeclaringType.Name}");

                        eil.Emit(OpCodes.Callvirt, m);
                        labelDictionary[eil].Add(str, eil.Body.Instructions.Last());

                    }
                    else if (str.MatchNewobj(out var ctor) && ctor.DeclaringType.HasInterface<IDetour>())
                    {
                        if (ctor.Parameters.Count != 0)
                        {
                            dil.Emit(OpCodes.Pop);
                            labelDictionary[dil].Add(str, dil.Body.Instructions.Last());
                            for (int i = 1; i < ctor.Parameters.Count; i++)
                                dil.Emit(OpCodes.Pop);
                            dil.Emit(OpCodes.Ldnull);
                        }
                        else
                        {
                            dil.Emit(OpCodes.Ldnull);
                            labelDictionary[dil].Add(str, dil.Body.Instructions.Last());
                        }
                        eil.Append(str);
                        labelDictionary[eil].Add(str, eil.Body.Instructions.Last());

                        eil.Emit(OpCodes.Dup);
                        eil.Emit(OpCodes.Ldarg_0);
                        eil.Emit(OpCodes.Ldc_I4, (int)level);
                        eil.Emit(OpCodes.Call, typeof(BuffHookWarpper).GetMethod("AddRuntimeHook", BindingFlags.NonPublic | BindingFlags.Static));

                    }
                    else if (str.OpCode == OpCodes.Ret)
                    {
                        dil.Emit(OpCodes.Ldarg_0);
                        labelDictionary[dil].Add(str, dil.Body.Instructions.Last());

                        dil.Emit(OpCodes.Ldc_I4, (int)level);
                        dil.Emit(OpCodes.Call, typeof(BuffHookWarpper).GetMethod("RemoveRuntimeHook", BindingFlags.NonPublic | BindingFlags.Static));
                        dil.Append(str);

                        eil.Append(str);
                        labelDictionary[eil].Add(str, eil.Body.Instructions.Last());

                    }
                    else if (str.Operand is Instruction label)
                    {
                        var dFrom = dil.Create(str.OpCode, str);
                        dil.Append(dFrom);
                        labelDictionary[dil].Add(str, dil.Body.Instructions.Last());
                        labelList[dil].Add(new(label, dFrom));

                        var eFrom = eil.Create(str.OpCode, str);
                        eil.Append(eFrom);
                        labelDictionary[eil].Add(str, dil.Body.Instructions.Last());
                        labelList[eil].Add(new(label, eFrom));

                        if (!hasShownBranchMessage)
                        {
                            BuffPlugin.LogWarning($"Find Branch in {type.Name}:HookOn, maybe cause error!");
                            hasShownBranchMessage = true;
                        }
                    }
                    else
                    {
                        dil.Append(str);
                        labelDictionary[dil].Add(str, dil.Body.Instructions.Last());

                        eil.Append(str);
                        labelDictionary[eil].Add(str, eil.Body.Instructions.Last());

                    }
                }

                foreach (var exception in origMethod.Body.ExceptionHandlers)
                {
                    foreach (var item in labelList)
                    {
                        var re = new ExceptionHandler(exception.HandlerType)
                        {
                            TryStart = labelDictionary[item.Key][exception.TryStart],
                            TryEnd = labelDictionary[item.Key][exception.TryEnd],
                            HandlerStart = labelDictionary[item.Key][exception.HandlerStart],
                            HandlerEnd = labelDictionary[item.Key][exception.HandlerEnd],
                            CatchType = exception.CatchType
                        };
                        if (exception.FilterStart != null)
                            re.FilterStart = labelDictionary[item.Key][exception.FilterStart];
                        item.Key.Body.ExceptionHandlers.Add(re);
                        item.Key.Body.Method.Module.ImportReference(exception.CatchType);

                    }
                }
                foreach (var item in labelList)
                    foreach (var pair in item.Value)
                        pair.from.Operand = labelDictionary[item.Key][pair.target];

                method.Body.Optimize();
                disableMethod.Body.Optimize();


            }
        }
    }
}
