using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.CardRender;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI
{
    /// <summary>
    /// UI层初步封装卡牌渲染器
    /// </summary>
    internal class BuffCard
    {
        public const float interactiveScaleBound = 0.6f;
        public const float normalScale = 0.5f;

        //基础变量
        FTexture _ftexture;
        public FContainer Container { get; private set; }
        public RenderTexture RenderTexture { get => _cardRenderer.cardCameraController.targetTexture; }

        public BuffID ID { get; private set; }
        public BuffStaticData StaticData => BuffConfigManager.GetStaticData(ID);

        public Vector3 Rotation
        {
            get => new Vector3(_cardRenderer.Rotation.x, _cardRenderer.Rotation.y, _ftexture.rotation);
            set
            {
                _cardRenderer.Rotation = value;
                _ftexture.rotation = value.z;
            }
        }
        public Vector2 Position
        {
            get => _ftexture.GetPosition();
            set => _ftexture.SetPosition(value);
        }
        public float Scale
        {
            get => _ftexture.scale;
            set => _ftexture.scale = value;
        }
        public float Alpha
        {
            get => _ftexture.alpha;
            set => _ftexture.alpha = value;
        }

        //动画机
        public AnimatorState currentAniamtorState = AnimatorState.Test_None;
        public BuffCardAnimator currentAnimator;

        //交互
        public CardInteractionManager interactionManager;

        public Action onMouseSingleClick;
        public Action onMouseDoubleClick;

        public Vector2 LocalMousePos
        {
            get
            {
                if(interactionManager == null)
                {
                    return Vector2.zero;
                }
                return new Vector2((interactionManager.MousePos.x - Position.x + Scale * interactiveScaleBound * CardBasicAssets.RenderTextureSize.x / 2f) / (Scale * interactiveScaleBound * CardBasicAssets.RenderTextureSize.x),
                                   (interactionManager.MousePos.y - Position.y + Scale * interactiveScaleBound * CardBasicAssets.RenderTextureSize.y / 2f) / (Scale * interactiveScaleBound * CardBasicAssets.RenderTextureSize.y));
            }
        }
        public bool CurrentFocused
        {
            get
            {
                if(interactionManager == null)
                    return false;
                return interactionManager.CurrentFocusCard == this;
            }
        }

        //卡牌效果控制
        BuffCardRenderer _cardRenderer;

        public bool Highlight
        {
            get => _cardRenderer.EdgeHighlight;
            set => _cardRenderer.EdgeHighlight = value;
        }

        public bool Grey
        {
            get => _cardRenderer.Grey;
            set => _cardRenderer.Grey = value;
        }

        public bool DisplayDescription
        {
            get => _cardRenderer.DisplayDiscription;
            set => _cardRenderer.DisplayDiscription = value;
        }

        public bool DisplayTitle
        {
            get => _cardRenderer.DisplayTitle;
            set => _cardRenderer.DisplayTitle = value;
        }

        public bool DisplayAllGraphTexts
        {
            set
            {
                DisplayStacker = value;
                DisplayCycle = value;
                DisplayKeyBinder = value;
                if (!value)
                    KeyBinderFlash = false;
            }
        }

        public bool DisplayStacker
        {
            get
            {
                if (StaticData.Stackable)
                    return _cardRenderer.cardStackerTextController.Show;
                return false;
            }
            set
            {
                if(StaticData.Stackable)
                    _cardRenderer.cardStackerTextController.Show = value;
            }
        }

        public bool StackerAddOne
        {
            get
            {
                if (StaticData.Stackable)
                    return _cardRenderer.cardStackerTextController.AddOne;
                return false;
            }
            set
            {
                if (StaticData.Stackable)
                    _cardRenderer.cardStackerTextController.AddOne = value;
            }
        }

        public int StackerValue
        {
            get
            {
                if (StaticData.Stackable)
                    return _cardRenderer.cardStackerTextController.Value;
                return -1;
            }
            set
            {
                if (StaticData.Stackable)
                    _cardRenderer.cardStackerTextController.Value = value;
            }
        }

        public bool DisplayCycle
        {
            get
            {
                if (StaticData.Countable)
                    return _cardRenderer.cardCycleCounterTextController.Show;
                return false;
            }
            set
            {
                if (StaticData.Countable)
                    _cardRenderer.cardCycleCounterTextController.Show = value;
            }
        }

        public int CycleValue
        {
            get
            {
                if (StaticData.Countable)
                    return _cardRenderer.cardCycleCounterTextController.Value;
                return -1;
            }
            set
            {
                if (StaticData.Countable)
                    _cardRenderer.cardCycleCounterTextController.Value = value;
            }
        }

        public bool DisplayKeyBinder
        {
            get
            {
                if(StaticData.Triggerable)
                    return _cardRenderer.cardKeyBinderTextController.Show;
                return false;
            }
            set
            {
                if (StaticData.Triggerable)
                    _cardRenderer.cardKeyBinderTextController.Show = value;
            }
        }

        public string KeyBinderValue
        {
            get
            {
                if (StaticData.Triggerable)
                    return _cardRenderer.cardKeyBinderTextController.BindKey;
                return string.Empty;
            }
            set
            {
                if (StaticData.Triggerable)
                    _cardRenderer.cardKeyBinderTextController.BindKey = value;
            }
        }

        public bool KeyBinderFlash
        {
            get
            {
                if (StaticData.Triggerable)
                    return _cardRenderer.cardKeyBinderTextController.Flash;
                return false;
            }
            set
            {
                if(StaticData.Triggerable)
                    _cardRenderer.cardKeyBinderTextController.Flash = value;
            }
        }

        public BuffCard(BuffID buffID) : this(buffID, AnimatorState.Test_None)
        { 
        }

        public BuffCard(BuffID buffID, AnimatorState initState)
        {
            ID = buffID;
            _cardRenderer = CardRendererManager.GetRenderer(buffID);
            Container = new FContainer();

            Container.AddChild(_ftexture = new FTexture(_cardRenderer.cardCameraController.targetTexture));
            SetAnimatorState(initState);

            Scale = normalScale;
        }

        //更新方法，在交互管理器中调用
        public void Update()
        {
            currentAnimator?.Update();
        }

        public void GrafUpdate(float timeStacker)
        {
            currentAnimator?.GrafUpdate(timeStacker);
        }

        public void Destroy()
        {
            CardRendererManager.RecycleCardRenderer(_cardRenderer);
            Container.RemoveFromContainer();
        }

        //改变卡牌的状态
        public void SetAnimatorState(AnimatorState newState)
        {
            if (newState == currentAniamtorState && currentAnimator != null)
                return;

            currentAnimator?.Destroy();
            currentAniamtorState = newState;

            if (newState == AnimatorState.Test_None)
            {
                currentAnimator = new ClearStateAnimator(this, Position, Rotation, Scale);
            }
            else if(newState == AnimatorState.Test_MousePreview)
            {
                currentAnimator = new MousePreviewAnimator(this, Position, Rotation, Scale);
            }
            else if(newState == AnimatorState.InGameSlot_Hide)
            {
                currentAnimator = new InGameSlotHideAnimator(this, Position, Rotation, Scale);
            }
            else if(newState == AnimatorState.InGameSlot_Show)
            {
                currentAnimator = new InGameSlotShowAnimator(this, Position, Rotation, Scale);
            }
            else if(newState == AnimatorState.InGameSlot_Exclusive_Show)
            {
                currentAnimator = new InGameSlotExclusiveShowAnimator(this, Position, Rotation, Scale);
            }
            else if(newState == AnimatorState.CardPicker_Show)
            {
                currentAnimator = new CardPickerShowAnimator(this, Position, Rotation, Scale);
            }
            else if(newState == AnimatorState.CardPicker_Disappear)
            {
                currentAnimator = new CardPickerDisappearAnimator(this, Position, Rotation, Scale);
            }
            else if(newState == AnimatorState.BuffGameMenu_Show)
            {
                currentAnimator = new BuffGameMenuShowAnimataor(this, Position, Rotation, Scale);
            }
            else if(newState == AnimatorState.BuffGameMenu_Disappear)
            {
                currentAnimator = new BuffGameMenuDisappearAnimataor(this, Position, Rotation, Scale);
            }
            else if(newState == AnimatorState.ActivateCardAnimSlot_Append)
            {
                currentAnimator = new ActivateCardAnimSlotAppendAnimator(this, Position, Rotation, Scale);
            }
            else if(newState == AnimatorState.TriggerBuffAnimSlot_Trigger)
            {
                currentAnimator = new TriggerBuffAnimSlotTriggerAnimator(this, Position, Rotation, Scale);
            }
            else if(newState == AnimatorState.BuffTimerAnimSlot_Show)
            {
                currentAnimator = new BuffTimerAnimSlotShowAnimator(this, Position, Rotation, Scale);
            }
            else
            {
                BuffPlugin.LogWarning($"No matched animator for state {newState}, please check codes");
            }
        }

        public void UpdateGraphText(bool dirty = false, bool useKeyBindData = true)
        {
            if (StaticData.Stackable)
            {
                StackerValue = ID.GetBuffData()?.StackLayer ?? 0;
            }

            if (StaticData.Countable)
            {
                CycleValue = (ID.GetBuffData() is CountableBuffData countable) ? (countable.MaxCycleCount - countable.CycleUse) : StaticData.MaxCycleCount;
            }

            if (StaticData.Triggerable)
            {
                if (useKeyBindData)
                {
                    var key = BuffPlayerData.Instance.GetKeyBind(ID);
                    if (key == KeyCode.None.ToString())
                        KeyBinderValue = null;
                    else
                        KeyBinderValue = key;
                }
                else
                    KeyBinderValue = null;

            }
            if(dirty)
                _cardRenderer.cardCameraController.CardDirty = true;
        }

        public void UpdateGrey()
        {
            if (BuffPoolManager.Instance != null)
                Grey = (!BuffPoolManager.Instance.GetBuff(ID)?.Active) ?? false;
            else
                Grey = false;
        }

        public void OnMouseSingleClick()
        {
            onMouseSingleClick?.Invoke();
            BuffPlugin.Log("Card singleclick");
        }

        public void OnMouseDoubleClick()
        {
            onMouseDoubleClick?.Invoke();
        }

        public enum AnimatorState
        {
            //测试状态
            Test_None,
            Test_MousePreview,

            //游戏内卡槽状态
            InGameSlot_Hide,
            InGameSlot_Show,
            InGameSlot_Exclusive_Show,

            //选卡卡槽状态
            CardPicker_Show,
            CardPicker_Disappear,

            //开始游戏界面卡槽状态
            BuffGameMenu_Show,
            BuffGameMenu_Disappear,

            //游戏内卡槽预动画状态
            ActivateCardAnimSlot_Append,

            //触发buff卡槽动画状态
            TriggerBuffAnimSlot_Trigger,

            //计时器卡槽动画状态
            BuffTimerAnimSlot_Show
        }
    }
}
