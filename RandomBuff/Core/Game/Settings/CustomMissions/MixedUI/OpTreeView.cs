using System.Collections.Generic;
using Menu.Remix.MixedUI;
using RandomBuffUtils;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.CustomMissions.MixedUI;

public class OpTreeView : UIfocusable, IHoldUIelements
{
    public OpTreeView(Vector2 pos, Vector2 size, float contentHeight) : base(pos, size)
    {
        fakeScrollBox = Helper.GetUninit<OpScrollBox>();
        fakeScrollBox.horizontal = false;
        fakeScrollBox._childOffset = pos;
        ContentHeight = contentHeight;
    }
    public void AddItems(params UIelement[] elements)
    {
        foreach (var ele in elements)
        {
            items.Add(ele);
            ele._AddToScrollBox(fakeScrollBox);
        }
    }
    
    public HashSet<UIelement> items { get; } = new();
    public bool IsTab => false;
    public Vector2 CanvasSize => isExpand ? new (size.x, ContentHeight) : size;
    
    public float ContentHeight { get; set; }
    
    private bool isExpand = false;

    private OpScrollBox fakeScrollBox;

}