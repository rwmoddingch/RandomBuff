using RandomBuff.Core.Buff;
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
        const float interactiveScaleBound = 0.6f;

        //基础变量
        FTexture _ftexture;
        public FContainer Container { get; private set; }

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
        public AnimatorState currentAniamtorState = AnimatorState.None;
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
                return interactionManager.currentFocusCard == this;
            }
        }

        //卡牌效果控制
        BuffCardRenderer _cardRenderer;

        public bool Highlight
        {
            get => _cardRenderer.EdgeHighlight;
            set => _cardRenderer.EdgeHighlight = value;
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

        public BuffCard(BuffID buffID)
        {
            _cardRenderer = CardRendererManager.GetRenderer(buffID);
            Container = new FContainer();

            Container.AddChild(_ftexture = new FTexture(_cardRenderer.cardCameraController.targetTexture));
            SetAnimataorState(AnimatorState.None);

            Scale = 0.5f;
        }

        //更新方法，在交互管理器中调用
        public void Update()
        {
            currentAnimator?.Update();
        }

        public void GrafUpdate()
        {
            currentAnimator?.GrafUpdate();
        }

        //改变卡牌的状态
        public void SetAnimataorState(AnimatorState newState)
        {
            if (newState == currentAniamtorState && currentAnimator != null)
                return;

            currentAnimator?.Destroy();
            currentAniamtorState = newState;
            if(newState == AnimatorState.None)
            {
                currentAnimator = new ClearStateAnimator(this, Position, Rotation, Scale);
            }
            else if(newState == AnimatorState.MousePreview)
            {
                currentAnimator = new MousePreviewAnimator(this, Position, Rotation, Scale);
            }
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
            None,
            MousePreview
        }
    }
}
