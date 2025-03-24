using System;
using System.Collections.Generic;
using System.Linq;
using Menu.Remix.MixedUI;
using RandomBuffUtils;
using RandomBuffUtils.MixedUI;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.CustomMissions;

public abstract class Element(bool isTop = false)
{

    public const float YSpacing = 10;
    public const float XSpacing = 5;

    
    public virtual void AddedToPanel(Panel panel)
    {
        Owner = panel;
    }
    protected abstract void Move(Vector2 newPos, float xBorder);
    
    public void Move(ref Vector2 newPos, float xBorder, float mul = 1)
    {
        if(!isTop)
            newPos.y -= Height * mul;
        if(lastPos != newPos)
            Move(newPos, xBorder);
        
        lastPos = newPos;
        if(isTop)
            newPos.y -= Height * mul;
        newPos.y -= YSpacing;
    }

    public void MakeDirty() => lastPos = new(float.NaN, float.NaN);

    public abstract float Height { get; }
    public abstract float NeedHeight { get; }
    public Panel Owner { get; private set; }
    
    private Vector2 lastPos = new(float.NaN,float.NaN);


}

public class ElementWithLabel(string labelText, float height, params UIelement[] elements)
    : Element
{
    public override void AddedToPanel(Panel panel)
    {
        base.AddedToPanel(panel);
        panel.Holder.AddItems(elements);
    }

    protected override void Move(Vector2 newPos,float xBorder)
    {
        xBorder -= newPos.x;
        for (int i =0;i<elements.Length;i++)
        {
            var ele = elements[i];
            if(ele == null) continue;
            var size = 1;
            for(int j = i+1;j<elements.Length;j++)
                if (elements[j] == null) size++;
                else break;
            var sizeX = Mathf.Min(xBorder / elements.Length * size, 150);
            ele.SetPos(new Vector2( newPos.x + xBorder/ elements.Length * i +
                                   ((xBorder / elements.Length * size) - sizeX)/2 + 
                                   (ele is OpCheckBox ? sizeX - 24 : 0)/2,
                newPos.y));

            ele.size = new Vector2(sizeX, Height);

            if(ele is UIconfig con)
                con.description = con.cfgEntry.info.description;
        }
    }

    private readonly UIelement[] elements = 
        new UIelement[]{new OpLabel(Vector2.zero,new Vector2(0,height),labelText)}
        .Concat(elements).ToArray();

    public override float Height => height;
    
    public override float NeedHeight => height;

}


public abstract class Panel : Element
{
    protected Panel(IHoldUIelements holder,bool isTop = false) : base(isTop) 
    {
        ((UIelement)holder).AddEvent("OnUpdate",this, nameof(Update));
        Holder = holder;
    }
    
    public virtual void Update() { }

    public override void AddedToPanel(Panel panel)
    {
        base.AddedToPanel(panel);
        panel.Holder.AddItems((UIelement)Holder);
    }
    
    public virtual void AddObject(Element element,int index = -1)
    {
        if (elements.Contains(element))
            return;
        element.AddedToPanel(this);
        if (index == -1)
            elements.Add(element);
        else 
            elements.Insert(index,element);
    }

    public readonly IHoldUIelements Holder;
    
    protected readonly List<Element> elements = new();
    
}


public class ScrollBox(OpScrollBox scrollBox,float XOffset = 20) : Panel(scrollBox)
{
    protected override void Move(Vector2 newPos, float xBorder) 
    {
        scrollBox.SetPos(newPos);
        scrollBox.size = new Vector2(xBorder - newPos.x,scrollBox.size.y);
    }

    public override void Update()
    {
        base.Update();
        scrollBox.contentSize = elements.Sum(i => i.NeedHeight + YSpacing) + YSpacing;
        var currentPos = new Vector2(XOffset + XSpacing,scrollBox.contentSize - YSpacing);
        foreach (var ele in elements)
            ele.Move(ref currentPos, scrollBox.size.x - XSpacing);
        
    }

    public override float Height => scrollBox.size.y;
    public override float NeedHeight => scrollBox.size.y;

}

public class TreeView(OpTreeView tree,float XOffset = 20) : Panel(tree,true)
{
    protected override void Move(Vector2 newPos, float xBorder)
    {
        tree.SetPos(newPos - Vector2.up * tree.HeaderHeight);
        tree.size = new Vector2(xBorder - newPos.x,tree.size.y);

        tree.HeaderButton.SetPos(newPos - Vector2.up * tree.HeaderHeight);
        tree.HeaderButton.size = new Vector2(xBorder - newPos.x,tree.HeaderButton.size.y);
    }

    public override void Update()
    {
        base.Update();
        tree.ContentHeight = elements.Sum(i => i.NeedHeight + YSpacing) + YSpacing;
        var currentPos = new Vector2(XOffset + XSpacing,tree.ContentHeight - YSpacing);
        foreach (var ele in elements)
            ele.Move(ref currentPos, tree.size.x - XSpacing);
    }
    public override void AddedToPanel(Panel panel)
    {
        base.AddedToPanel(panel);
        panel.Holder.AddItems((OpTreeView)Holder,((OpTreeView)Holder).HeaderButton);
    }
    public override float Height => tree.size.y;
    public override float NeedHeight => tree.ContentHeight + tree.HeaderHeight;

}

