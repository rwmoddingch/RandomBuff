using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RandomBuffUtils.MixedUI;
using UnityEngine;
using Object = System.Object;

namespace RandomBuff.Core.Game.Settings.CustomMissions;

public class ModifyBuilderContext : IDisposable
{
    public ModifyBuilderContext(ElementBuilderContext ctx, Panel newHolder)
    {
        this.ctx = ctx;
        oldHolder = ctx.holder;
        ctx.holder = newHolder;
    }
    public void Dispose()
    {
        ctx.holder = oldHolder;
    }

    private readonly ElementBuilderContext ctx;
    private readonly Panel oldHolder;
}

public class ElementBuilderContext
{
    public Panel holder;

    public const float DefaultHeight = 25;
}

public class ObjectContext
{
    public ElementPropertyAttribute Attr { get; }
    public Func<object> GetMethod{ get; }
    public Action<object> SetMethod{ get; }

    public ObjectContext(ElementPropertyAttribute attr, Func<object> getMethod, Action<object> setMethod)
    {
        Attr = attr;
        GetMethod = getMethod;
        SetMethod = setMethod;
    }
}

internal static class ViewBuilder
{
    public static Element Build(ElementBuilderContext ctx, ObjectContext objCtx,Type type)
    {
        return GetElementBuilder(type)
            .BuildElement(ctx,objCtx);
    }
    public static void Build(object obj)
    {
        ElementBuilderContext ctx = new();
        var type = obj.GetType();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
                                                    BindingFlags.Instance))
        {
            if(!property.GetCustomAttributes<JsonPropertyAttribute>().Any()) 
                continue;

            if (property.SetMethod == null || property.GetMethod == null )
            {
                BuffPlugin.LogError($"[ViewBuilder] Property must both has set and get methods, {type.Name}:{property.Name}");
                continue;
            }
      
 
            var attr =  property.GetCustomAttributes<ElementPropertyAttribute>().FirstOrDefault() 
                        ?? new ElementPropertyAttribute(property.Name);

 
            GetElementBuilder(property.PropertyType)
                .BuildElement(ctx,new ObjectContext(attr, 
                () => property.GetMethod.Invoke(obj, null),
                (i) => property.SetMethod.Invoke(obj,new[] {i}) 
            ));
        }

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                                BindingFlags.Instance))
        {
            if(!field.GetCustomAttributes<JsonPropertyAttribute>().Any()) 
                continue;
            
            var attr =  field.GetCustomAttributes<ElementPropertyAttribute>().FirstOrDefault() ??
                        new ElementPropertyAttribute(field.Name);
            
            
            GetElementBuilder(field.FieldType).BuildElement(ctx,new ObjectContext(attr, 
                () => field.GetValue(obj),
                (i) => field.SetValue(obj,i) 
            ));
        }
    }

    private static ElementBuilder GetElementBuilder(Type type)
    {
        if (type.IsGenericType && Builders.TryGetValue(type.GetGenericTypeDefinition(), out var ge))
            return ge;
        else if (Builders.TryGetValue(type, out var element))
            return element;
        else if (Builders.TryGetValue(type.BaseType ?? typeof(Object), out element) && !element.IsPrecise)
            return element;
        return FallBackElementBuilder;
    }

    private static readonly Dictionary<Type, ElementBuilder> Builders = new();

    private static readonly FallBackElementBuilder FallBackElementBuilder = new();

}

internal abstract class ElementBuilder
{
    public abstract Type ElementType { get; }

    public virtual bool IsPrecise => true;

    public abstract Element BuildElement(ElementBuilderContext ctx, ObjectContext objCtx);

}

internal class FallBackElementBuilder : ElementBuilder
{
    public override Type ElementType => null;
    public override Element BuildElement(ElementBuilderContext ctx, ObjectContext objCtx)
    {
        throw new NotImplementedException();
    }
}

internal class ListElementBuilder : ElementBuilder
{
    public override Type ElementType => typeof(List<>);
    
    public override Element BuildElement(ElementBuilderContext ctx, ObjectContext objCtx)
    {
        throw new NotImplementedException();
    }
}