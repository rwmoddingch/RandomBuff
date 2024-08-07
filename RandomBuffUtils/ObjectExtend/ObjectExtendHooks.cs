using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Newtonsoft.Json;
using UnityEngine;

namespace RandomBuffUtils.ObjectExtend
{
    internal static class ObjectExtendHooks
    {
        public static void OnEnable()
        {
            On.SaveState.AbstractPhysicalObjectFromString += SaveState_AbstractPhysicalObjectFromString;

            IL.SaveState.AbstractPhysicalObjectFromString += SaveState_AbstractPhysicalObjectFromStringIL;
            On.SaveState.SetCustomData_AbstractPhysicalObject_string += SaveState_SetCustomData_AbstractPhysicalObject_string;
        }

        private static string SaveState_SetCustomData_AbstractPhysicalObject_string(On.SaveState.orig_SetCustomData_AbstractPhysicalObject_string orig, AbstractPhysicalObject self, string re)
        {
            re = orig(self, re);
            if (self.GetType().GetCustomAttribute<BuffAbstractPhysicalObjectAttribute>() != null)
            {
                foreach (var field in self.GetType().GetFields().Where(i =>
                             i.GetCustomAttribute<BuffAbstractPhysicalObjectPropertyAttribute>() != null))
                {
                    try
                    {
                        re += $"<oA>BUFF-{field.Name}<oBuff>{JsonConvert.SerializeObject(field.GetValue(self))}";
                    }
                    catch (Exception e)
                    {
                        BuffUtils.LogException("ObjectExtend", e);
                        BuffUtils.LogError("ObjectExtend", $"Try serialize field for Type: {self.GetType().Name}, Field Name: {field.Name} FAILED!");
                    }
                }

                foreach (var property in self.GetType().GetProperties().Where(i =>
                             i.GetCustomAttribute<BuffAbstractPhysicalObjectPropertyAttribute>() != null && (i.GetGetMethod() ?? i.GetGetMethod(true)) != null))
                {
                    try
                    {
                        re += $"<oA>BUFF-{property.Name}<oBuff>{JsonConvert.SerializeObject(property.GetValue(self))}";
                    }
                    catch (Exception e)
                    {
                        BuffUtils.LogException("ObjectExtend", e);
                        BuffUtils.LogError("ObjectExtend", $"Try serialize property for Type: {self.GetType().Name}, Property Name: {property.Name} FAILED!");
                    }
                }

                re += $"<oA>BUFFTYPE-{self.GetType().AssemblyQualifiedName}";
                BuffUtils.Log("ObjectExtend", re);
            }

            return re;
        }



        private static void SaveState_AbstractPhysicalObjectFromStringIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            il.Body.Variables.Add(new VariableDefinition(il.Module.ImportReference(typeof(object[]))));
            il.Body.Variables.Add(new VariableDefinition(il.Module.ImportReference(typeof(ConstructorInfo))));

            il.Body.MaxStackSize = Mathf.Max(il.Body.MaxStackSize, 12);
            var index = (byte)(il.Body.Variables.Count - 2);
            var ctorIndex = (byte)(index + 1);

            var abType = il.Module.ImportReference(typeof(AbstractPhysicalObject));
            var ctorInvokeRef =
                il.Module.ImportReference(typeof(ConstructorInfo).GetMethod(nameof(ConstructorInfo.Invoke), new[] { typeof(object[]) }));
            while (c.TryGotoNext(i => i.MatchNewobj(out var method) &&
                                      method.DeclaringType.IsDerivedType(abType)))
            {

                var method = c.Next.Operand as MethodReference;
                c.Emit(OpCodes.Ldloc_0);
                c.EmitDelegate<Func<string[], ConstructorInfo>>((split) =>
                {
                    BuffUtils.Log("ObjectExtend", split[split.Length - 1]);
                    if (IsBuffAbstractObject(split, out var name) &&
                        Type.GetType(name) is { } type &&
                        type.GetCustomAttribute<BuffAbstractPhysicalObjectAttribute>() != null)
                    {
                        var re = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(i =>
                        {
                            var param = i.GetParameters();
                            if (param.Length != method.Parameters.Count) return false;
                            for (int j = 0; j < i.GetParameters().Length; j++)
                                if (!method.Parameters[j].Name.Contains(param[j].Name))
                                    return false;


                            return true;
                        });
                        if (re.Any())
                            return re.First();

                    }

                    return null;
                });

                var label = c.DefineLabel();
                var label2 = c.DefineLabel();

                c.Emit(OpCodes.Dup);
                c.Emit(OpCodes.Stloc_S, ctorIndex);
                c.Emit(OpCodes.Brfalse, label);

                EmitArray(c, method, index);
                c.Emit(OpCodes.Ldloc_S, ctorIndex);
                c.Emit(OpCodes.Ldloc_S, index);
                c.Emit(OpCodes.Callvirt, ctorInvokeRef);
                c.Emit(OpCodes.Br, label2);
                c.MarkLabel(label);

                c.GotoNext(MoveType.After, i => i.MatchNewobj(out _));
                c.MarkLabel(label2);

            }


            void EmitArray(ILCursor c, MethodReference method, byte index)
            {
                c.Emit(OpCodes.Ldc_I4, method.Parameters.Count);
                c.Emit(OpCodes.Newarr, c.Module.TypeSystem.Object);
                c.Emit(OpCodes.Stloc_S, index);
                var methodRef = il.Module.ImportReference(typeof(ObjectExtendHooks).GetMethod(nameof(IL_SetArray),
                    BindingFlags.NonPublic | BindingFlags.Static));
                for (int i = method.Parameters.Count - 1; i >= 0; i--)
                {
                    EmitCastToReference(c, method.Parameters[i].ParameterType);
                    c.Emit(OpCodes.Ldloc_S, index);
                    c.Emit(OpCodes.Ldc_I4, i);
                    c.Emit(OpCodes.Call, methodRef);
                }
            }


            void EmitCastToReference(ILCursor c, TypeReference type)
            {
                if (type.IsValueType)
                    c.Emit(OpCodes.Box, type);
            }
        }
        private static void IL_SetArray(object o, object[] objects, int j) => objects[j] = o;
        private static bool IsBuffAbstractObject(string[] array, out string typeName)
        {
            var re = array.Where(i => i.StartsWith("BUFFTYPE-"));
            if (re.Any())
            {
                typeName = re.First().Replace("BUFFTYPE-", "");
                return true;
            }
            typeName = null;
            return false;
        }

        private static AbstractPhysicalObject SaveState_AbstractPhysicalObjectFromString(
            On.SaveState.orig_AbstractPhysicalObjectFromString orig, World world, string objString)
        {
            var re = orig(world, objString);

            string[] split = Regex.Split(objString, "<oA>");
            try
            {
                EntityID id = EntityID.FromString(split[0]);
                AbstractPhysicalObject.AbstractObjectType abstractObjectType =
                    new AbstractPhysicalObject.AbstractObjectType(split[1], false);
                WorldCoordinate pos = WorldCoordinate.FromString(split[2]);

                if (IsBuffAbstractObject(split, out var name) &&
                    Type.GetType(name) is { } type &&
                    type.GetCustomAttribute<BuffAbstractPhysicalObjectAttribute>() != null)
                {
                    BuffUtils.Log("ObjectExtend", pos.SaveToString());

                    if (re?.GetType() != type)
                    {
                        if (type.GetInterface(nameof(IBuffAbstractPhysicalObjectInitialization)) != null)
                        {

                            re = ((IBuffAbstractPhysicalObjectInitialization)FormatterServices
                                .GetSafeUninitializedObject(type)).Initialize(world, abstractObjectType, pos, id,
                                split.Skip(3).Where(i => !i.StartsWith("BUFF-") && !i.StartsWith("BUFFTYPE-"))
                                    .ToArray());
                        }
                        else
                        {
                            BuffUtils.LogWarning("ObjectExtend", $"{type.Name} don't have same ctor as base type");
                            re = (AbstractPhysicalObject)FormatterServices.GetSafeUninitializedObject(type);
                            re.pos = pos;
                            re.world = world;
                            re.ID = id;
                            re.type = abstractObjectType;
                            re.stuckObjects = new List<AbstractPhysicalObject.AbstractObjectStick>();
                            re.unrecognizedAttributes = split.Skip(3)
                                .Where(i => !i.StartsWith("BUFF-") && !i.StartsWith("BUFFTYPE-")).ToArray();
                        }
                    }
                    else
                    {
                        re.unrecognizedAttributes = re.unrecognizedAttributes
                            .Where(i => !i.StartsWith("BUFF-") && !i.StartsWith("BUFFTYPE-")).ToArray();

                    }


                    foreach (var attr in split.Where(i => i.StartsWith("BUFF-")).Select(i => i.Replace("BUFF-", "")))
                    {
                        string[] attrSplit = Regex.Split(attr, "<oBuff>");
                        if (re.GetType().GetField(attrSplit[0],
                                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance) is { } field)
                        {
                            try
                            {
                                field.SetValue(re, JsonConvert.DeserializeObject(attrSplit[1], field.FieldType));
                            }
                            catch (Exception e)
                            {
                                BuffUtils.LogException("ObjectExtend", e);
                                BuffUtils.LogError("ObjectExtend",
                                    $"Try set custom field for Type: {abstractObjectType}, Field Name: {attrSplit[0]} FAILED!");
                            }
                        }
                        else if (re.GetType().GetProperty(attrSplit[0],
                                         BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance) is
                                     { } property &&
                                 property.GetCustomAttribute<BuffAbstractPhysicalObjectPropertyAttribute>() != null &&
                                 (property.GetSetMethod() ?? property.GetSetMethod(true)) is { } setMethod)
                        {
                            try
                            {
                                setMethod.Invoke(re,
                                    new object[]
                                        { JsonConvert.DeserializeObject(attrSplit[1], property.PropertyType) });
                            }
                            catch (Exception e)
                            {
                                BuffUtils.LogException("ObjectExtend", e);
                                BuffUtils.LogError("ObjectExtend",
                                    $"Try set custom property for Type: {abstractObjectType}, Property Name: {attrSplit[0]} FAILED!");
                            }
                        }
                        else
                        {
                            BuffUtils.LogError("ObjectExtend",
                                $"Can't find field or property for Type: {abstractObjectType}, Name: {attrSplit[0]}!");

                        }
                    }

                }
            }
            catch (Exception e)
            {
                BuffUtils.LogWarning("ObjectExtend", $"Unexcepted object format {objString}\n{e.ToString()}");
            }

            return re;
        }


        private static bool IsDerivedType(this TypeReference checkType, TypeReference baseType)
        {
            var aType = checkType;
            while (aType != null)
            {
                if (aType.FullName == baseType.FullName)
                    return true;
                aType = aType?.Resolve()?.BaseType;
            }

            return false;
        }
    }
}
