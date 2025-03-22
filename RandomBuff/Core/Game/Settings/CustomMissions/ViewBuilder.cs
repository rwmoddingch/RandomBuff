using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RandomBuff.Core.Game.Settings.CustomMissions;

internal class ElementBuilderContext
{
    public IHoldUIelements holder;
}

internal class ObjectContext
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
    public static void Build(ElementBuilderContext ctx, ObjectContext objCtx,Type type)
    {
        GetElementBuilder(type)
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

    public abstract void BuildElement(ElementBuilderContext ctx, ObjectContext objCtx);

}

internal class FallBackElementBuilder : ElementBuilder
{
    public override Type ElementType => null;
    public override void BuildElement(ElementBuilderContext ctx, ObjectContext objCtx)
    {
        throw new NotImplementedException();
    }
}

internal class ListElementBuilder : ElementBuilder
{
    public override Type ElementType => typeof(List<>);
    
    public override void BuildElement(ElementBuilderContext ctx, ObjectContext objCtx)
    {
        var list = (IList)objCtx.GetMethod();
        //修改位置
        for(int i = 0; i < list.Count;i++)
        {
            var i1 = i;
            ViewBuilder.Build(ctx,new ObjectContext(new ElementPropertyAttribute($"{i}")
            ,() => list[i1], (a) => list[i1] = a), list.GetType().GenericTypeArguments[0]);  
        }
        //添加添加与删除操作
        //复位S
    }
}