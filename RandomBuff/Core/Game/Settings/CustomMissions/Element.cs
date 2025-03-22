using System.Collections.Generic;
using Menu.Remix.MixedUI;
using RandomBuff.Core.Game.Settings.CustomMissions.MixedUI;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.CustomMissions;

public abstract class Element(float height)
{
    public virtual void AddedToPanel(Panel panel)
    {
        Owner = panel;
    }
    protected abstract void Move(Vector2 newPos, float xBorder);
    
    public void Move(ref Vector2 newPos, float xBorder, float mul = 1)
    {
        Move(newPos, xBorder);
        newPos.y -= Height * mul;
    }

    public float Height { get; private set; } = height;

    public Panel Owner { get; private set; }
}

public class ElementWithLabel(float height, params UIelement[] elements) : Element(height)
{
    public override void AddedToPanel(Panel panel)
    {
        base.AddedToPanel(panel);
        panel.holder.AddItems(elements);
    }

    protected override void Move(Vector2 newPos,float xBorder)
    {
        foreach (var ele in elements)
        {
        }
    }
    
    private UIelement[] elements = elements;


}


public abstract class Panel(IHoldUIelements holder, float initHeight) : Element(initHeight) 
{
    public abstract void Rearrange(Element at);
    
    protected abstract void AddObjectInternal(Element element);

    public override void AddedToPanel(Panel panel)
    {
        base.AddedToPanel(panel);
        panel.holder.AddItems((UIelement)this.holder);
    }

    public void AddObject(Element element,int index = -1)
    {
        if (index == -1)
            elements.Add(element);
        else 
            elements.Insert(index,element);
        AddObjectInternal(element);
    }

    public readonly IHoldUIelements holder = holder;
    
    protected readonly List<Element> elements = new();
    
}


public class ScrollBox(OpScrollBox scrollBox) : Panel(scrollBox,scrollBox.size.y)
{
    protected override void Move(Vector2 newPos, float xBorder) 
    {
        throw new System.NotImplementedException();
    }

    public override void Rearrange(Element at)
    {
        throw new System.NotImplementedException();
    }

    protected override void AddObjectInternal(Element element)
    {
        throw new System.NotImplementedException();
    }
}

public class TreeView(OpTreeView tree) : Panel(tree,tree.size.y)
{
    protected override void Move(Vector2 newPos, float xBorder)
    {
        throw new System.NotImplementedException();
    }

    public override void Rearrange(Element at)
    {
        throw new System.NotImplementedException();
    }

    protected override void AddObjectInternal(Element element)
    {
        throw new System.NotImplementedException();
    }
}

