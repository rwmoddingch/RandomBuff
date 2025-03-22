using System;
using System.Collections.Generic;
using Menu.Remix.MixedUI;
using RandomBuffUtils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RandomBuff.Core.Game.Settings.CustomMissions.MixedUI;

public class OpTreeView : UIfocusable, IHoldUIelements
{
    public OpTreeView(Vector2 pos, float width, float headerHeight, float contentHeight,string displayName) : base(pos, new Vector2(width,headerHeight))
    {
        ContentHeight = contentHeight;
        HeaderHeight = headerHeight;
        HeaderButton = new OpSimpleButton(pos, new Vector2(width, headerHeight), displayName);
        HeaderButton.AddEvent("OnClick",this,"HeaderButton_OnClick");
        
        GameObject gameObject = new GameObject("OpTreeView Camera " + camIndex);
        camera = gameObject.AddComponent<Camera>();
        camIndex = -1;
        int i = 0;
        for (int count = cameras.Count; i < count; i++)
        {
            if (cameras[i] == null)
            {
                camIndex = i;
                cameras[i] = camera;
                break;
            }
        }
        if (camIndex == -1)
        {
            cameras.Add(camera);
            camIndex = cameras.Count - 1;
        }
        gameObject.name = "OpTreeView Camera " + camIndex;
        camPos = new Vector2(10000f + 10300f * camIndex, 50000f);
        
        
        fakeScrollBox = Helper.GetUninit<OpScrollBox>();
        fakeScrollBox.horizontal = false;
        fakeScrollBox._camPos = fakeScrollBox._childOffset = camPos;
        
    }
    
    public void AddItems(params UIelement[] elements)
    {
        if (tab == null)
        {
            throw new InvalidOperationException("OpTreeView must be added to an OpTab before items are added.");
        }
        foreach (var ele in elements)
        {
            if (ele._AddToScrollBox(fakeScrollBox))
            {
                tab.AddItems(ele);
                items.Add(ele);
            }
        }
        
    }

    public void RemoveItem(params UIelement[] elements)
    {
        foreach (var ele in elements)
        {
            if (items.Remove(ele))
            {
                ele._RemoveFromScrollBox();
                tab.RemoveItems(ele);
            }
        }
    }

    public override void Update()
    {
        size = new(size.x, Mathf.Lerp(size.y, isExpand ? ContentHeight+HeaderHeight : HeaderHeight, 0.2f));
        MoveCam();
        if (firstUpdate)
        {
            firstUpdate = false;
            Change();
        }
        base.Update();
    }

    public override void Change()
    {
        base.Change();
        UpdateCam();
    }

    private void MoveCam()
    {
        Vector3 vector = (Vector3)camPos + new Vector3(size.x / 2f, ContentHeight/2, -50f) + Vector3.down * ScrollOffset;
        if (Owner != null)
            vector += (Vector3)Owner.ScreenPos;
        if (tab != null)
            vector += (Vector3)tab._container.GetPosition();
        camera.gameObject.transform.position = new Vector3(Mathf.Round(vector.x), Mathf.Round(vector.y), Mathf.Round(vector.z));
    }

    private void UpdateCam()
    {
        camera.aspect = size.x / ContentHeight;
        camera.orthographic = true;
        camera.orthographicSize = ContentHeight / 2f;
        camera.nearClipPlane = 1f;
        camera.farClipPlane = 100f;
        MoveCam();
        camera.depth = -1000f;
        
        if (rt != null && Mathf.Approximately(rt.width, size.x) && Mathf.Approximately(rt.height, ContentHeight))
            return;
        
        if (rt != null)
            rt.Release();
        
        rt = new RenderTexture((int)size.x, (int)ContentHeight, 8) { filterMode = FilterMode.Point };
        camera.targetTexture = rt;
        if (texture == null)
        {
            texture = new FTexture(rt, "OpTreeView" + camIndex)
            {
                anchorX = 0f,
                anchorY = 1f,
                x = 0f,
                y = 0f,
            };
            myContainer.AddChild(texture);
        }
        else
            texture.SetTexture(rt);
        
    }
    
    private void HeaderButton_OnClick(UIfocusable trigger)
    {
        isExpand = !isExpand;
    }
    
    
    public HashSet<UIelement> items { get; } = new();
    public bool IsTab => false;
    public Vector2 CanvasSize =>  size;
    public float ScrollOffset => ContentHeight - CurrentHeight;
    
    public float ContentHeight { get; }
    
    public float HeaderHeight { get; }
    public float CurrentHeight => size.y - HeaderHeight;
    
    private bool isExpand = false;

    private OpScrollBox fakeScrollBox;

    private Camera camera;

    private static List<Camera> cameras = new();

    private RenderTexture rt;

    private FTexture texture;

    private int camIndex;

    private Vector2 camPos;

    private bool firstUpdate;

    public OpSimpleButton HeaderButton { get; }

    public override void Unload()
    {
        base.Unload();
        if (camera)
        {
            Object.Destroy(camera.gameObject);
        }

        if (texture != null)
            texture.Destroy();
        
    }

}