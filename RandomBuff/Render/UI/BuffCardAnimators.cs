using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI
{
    /// <summary>
    /// 卡牌动画机
    /// </summary>
    internal abstract class BuffCardAnimator
    {
        protected BuffCard buffCard;

        protected Vector2 initPosition;
        protected Vector3 initRotation;
        protected float initScale;

        public BuffCardAnimator(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale)
        {
            this.buffCard = buffCard;
            this.initPosition = initPosition;
            this.initRotation = initRotation;
            this.initScale = initScale;
        }

        public virtual void Update()
        {

        }

        public virtual void GrafUpdate(float timeStacker)
        {

        }

        public virtual void Destroy()
        {

        }
    }

    #region Test
    /// <summary>
    /// 测试无状态
    /// </summary>
    internal class ClearStateAnimator : BuffCardAnimator
    {
        bool finished;
        public ClearStateAnimator(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            buffCard.Highlight = false;
            if (!buffCard.DisplayTitle)
                buffCard.DisplayTitle = true;
        }

        public override void GrafUpdate(float timeStacker)
        {
            if (finished) return;

            buffCard.Rotation = Vector3.Lerp(buffCard.Rotation, Vector3.zero, 0.1f);
            if (Mathf.Abs(buffCard.Rotation.x) < 0.01f && Mathf.Abs(buffCard.Rotation.y) < 0.01f)
                finished = true;
        }
    }

    /// <summary>
    /// 测试鼠标交互状态
    /// </summary>
    internal class MousePreviewAnimator : BuffCardAnimator
    {
        bool flip;
        Vector3 basicRotation = Vector3.zero;
        public MousePreviewAnimator(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            buffCard.Highlight = true;

            buffCard.onMouseSingleClick += OnSingleClickFlip;
        }

        public override void GrafUpdate(float timeStacker)
        {
            Vector3 rotationTarget = basicRotation + new Vector3((30f * buffCard.LocalMousePos.y - 15f) * (flip ? -1f : 1f), -(30f * buffCard.LocalMousePos.x - 15f), 0f);

            if (Mathf.Abs(buffCard.Rotation.x - rotationTarget.x) >= 0.01f || Mathf.Abs(buffCard.Rotation.y - rotationTarget.y) >= 0.01f || Mathf.Abs(buffCard.Rotation.z - rotationTarget.z) >= 0.01f)
            {
                buffCard.Rotation = Vector3.Lerp(buffCard.Rotation, rotationTarget, 0.15f);
            }
        }

        public override void Destroy()
        {
            buffCard.onMouseSingleClick -= OnSingleClickFlip;
        }

        void OnSingleClickFlip()
        {
            if (!flip)
            {
                basicRotation = new Vector3(0, 180f, 0f);
                flip = true;
                buffCard.DisplayDescription = true;
            }
            else
            {
                basicRotation = Vector3.zero;
                flip = false;
                buffCard.DisplayDescription = false;
            }
        }
    }

    #endregion

    #region InGameSlot
    internal class InGameSlotHideAnimator : BuffCardAnimator
    {
        bool rotationFinished;
        bool positionFinished;
        bool scaleFinished;

        float targetScale;
        Vector2 targetPosition;

        InGameSlotInteractionManager InGameSlotInteractionManager => buffCard.interactionManager as InGameSlotInteractionManager;

        public InGameSlotHideAnimator(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            buffCard.Highlight = false;
            buffCard.DisplayDescription = false;
            buffCard.DisplayTitle = false;
            buffCard.DisplayAllGraphTexts = false;
            buffCard.KeyBinderFlash = false;
            buffCard.UpdateGrey();

            targetScale = BuffCard.normalScale * 0.1f;
            targetPosition = new Vector2(Custom.rainWorld.screenSize.x - 40f - 5f * InGameSlotInteractionManager.IndexInManagedCards(buffCard), 40f);
        }

        public override void GrafUpdate(float timeStacker)
        {
            if (!rotationFinished)
            {
                buffCard.Rotation = Vector3.Lerp(buffCard.Rotation, Vector3.zero, 0.1f);
                if (Mathf.Abs(buffCard.Rotation.x) < 0.01f && Mathf.Abs(buffCard.Rotation.y) < 0.01f)
                    rotationFinished = true;
            }

            if (!positionFinished)
            {
                buffCard.Position = Vector2.Lerp(buffCard.Position, targetPosition, 0.1f);
                if (Mathf.Abs(buffCard.Position.x - targetPosition.x) < 0.01f && Mathf.Abs(buffCard.Position.y - targetPosition.y) < 0.01f)
                    positionFinished = true;
            }

            if (!scaleFinished)
            {
                buffCard.Scale = Mathf.Lerp(buffCard.Scale, targetScale, 0.1f);
                if (Mathf.Abs(buffCard.Scale - targetScale) < 0.01f)
                    scaleFinished = true;
            }
        }
    }

    internal class InGameSlotShowAnimator : BuffCardAnimator
    {
        Vector2 halfScreenSize = Custom.rainWorld.screenSize / 2f;

        Vector2 TargetPosition
        {
            get
            {
                return new Vector2(halfScreenSize.x + inGameSlotInteractionManager.IndexBiasInGroupedCards(buffCard) * 100f,
                                   buffCard.StaticData.BuffType == Core.Buff.BuffType.Positive ? (halfScreenSize.y + 80f) : (halfScreenSize.y - 80f));
            }
        }

        float TargetScale => BuffCard.normalScale * 0.5f + (buffCard.CurrentFocused ? 0.05f : 0f);

        Vector3 targetRotation = Vector3.zero;

        InGameSlotInteractionManager inGameSlotInteractionManager;

        public InGameSlotShowAnimator(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            buffCard.Highlight = false;
            buffCard.DisplayDescription = false;
            buffCard.DisplayTitle = true;
            buffCard.DisplayAllGraphTexts = false;
            buffCard.KeyBinderFlash = false;
            buffCard.UpdateGrey();

            inGameSlotInteractionManager = buffCard.interactionManager as InGameSlotInteractionManager;
        }

        public override void Update()
        {
            if (buffCard.CurrentFocused && !buffCard.Highlight)
                buffCard.Highlight = true;
            else if (!buffCard.CurrentFocused && buffCard.Highlight)
                buffCard.Highlight = false;
        }

        public override void GrafUpdate(float timeStacker)
        {
            if (Mathf.Abs(buffCard.Position.x - TargetPosition.x) > 0.01f || Mathf.Abs(buffCard.Position.y - TargetPosition.y) > 0.01f)
                buffCard.Position = Vector2.Lerp(buffCard.Position, TargetPosition, 0.1f);

            if (Mathf.Abs(buffCard.Scale - TargetScale) > 0.01f)
                buffCard.Scale = Mathf.Lerp(buffCard.Scale, TargetScale, 0.1f);

            if (Mathf.Abs(buffCard.Rotation.x - targetRotation.x) > 0.01f || Mathf.Abs(buffCard.Rotation.y - targetRotation.y) > 0.01f)
                buffCard.Rotation = Vector3.Lerp(buffCard.Rotation, targetRotation, 0.1f);
        }
    }

    internal class InGameSlotExclusiveShowAnimator : BuffCardAnimator
    {
        Vector2 targetPosition = Custom.rainWorld.screenSize / 2f;
        float targetScale = BuffCard.normalScale;

        Vector3 basicRotation = new Vector3(0f, 360f, 0f);
        Vector3 TargetRotation => basicRotation + (buffCard.CurrentFocused ? new Vector3((30f * buffCard.LocalMousePos.y - 15f) * (flip ? -1f : 1f), -(30f * buffCard.LocalMousePos.x - 15f), 0f) : Vector3.zero);

        bool flip;

        InGameSlotInteractionManager inGameSlotInteractionManager;

        public InGameSlotExclusiveShowAnimator(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            buffCard.Highlight = false;
            buffCard.DisplayDescription = false;
            buffCard.DisplayTitle = true;
            buffCard.DisplayAllGraphTexts = true;
            buffCard.KeyBinderFlash = false;
            buffCard.UpdateGrey();

            buffCard.UpdateGraphText();
            buffCard.StackerAddOne = false;

            inGameSlotInteractionManager = buffCard.interactionManager as InGameSlotInteractionManager;
            buffCard.onMouseSingleClick += OnSingleClickFlip;
        }

        public override void Update()
        {
            base.Update();
            if(inGameSlotInteractionManager.keyBinderProcessor != null && inGameSlotInteractionManager.keyBinderProcessor.InBindMode)
                buffCard.KeyBinderFlash = true;
            else
                buffCard.KeyBinderFlash = false;
        }

        public override void GrafUpdate(float timeStacker)
        {
            if (Mathf.Abs(buffCard.Rotation.x - TargetRotation.x) > 0.01f || Mathf.Abs(buffCard.Rotation.y - TargetRotation.y) > 0.01f)
                buffCard.Rotation = Vector3.Lerp(buffCard.Rotation, TargetRotation, 0.1f);

            if (Mathf.Abs(buffCard.Position.x - targetPosition.x) > 0.01f || Mathf.Abs(buffCard.Position.y - targetPosition.y) > 0.01f)
                buffCard.Position = Vector2.Lerp(buffCard.Position, targetPosition, 0.1f);

            if (Mathf.Abs(buffCard.Scale - targetScale) > 0.01f)
                buffCard.Scale = Mathf.Lerp(buffCard.Scale, targetScale, 0.1f);
        }

        void OnSingleClickFlip()
        {
            if (!flip)
            {
                basicRotation = new Vector3(0, 180f + 360f, 0f);
                flip = true;
                buffCard.DisplayDescription = true;
            }
            else
            {
                basicRotation = new Vector3(0, 360f, 0f);
                flip = false;
                buffCard.DisplayDescription = false;
            }
        }

        public override void Destroy()
        {
            buffCard.onMouseSingleClick -= OnSingleClickFlip;
        }
    }
    #endregion

    #region CardPicker
    internal class CardPickerShowAnimator : BuffCardAnimator
    {
        const int initFlipTime = 40;
        CardPickerInteractionManager cardPickerInteractionManager;
        BuffCard linkedCard;

        int initDelay;
        int delayTimer;

        int initFlipTimer;

        public bool CurrentFocused
        {
            get
            {
                if (linkedCard == null)
                    return buffCard.CurrentFocused;
                return linkedCard.CurrentFocused || buffCard.CurrentFocused;
            }
        }

        Vector2 halfScreenSize = Custom.rainWorld.screenSize / 2f;
        Vector2 TargetPosition
        {
            get
            {
                int index = cardPickerInteractionManager.GetCardShowIndex(buffCard, out int majorOrAdditional);
                return new Vector2(halfScreenSize.x + index * 200f + (CurrentFocused ? 0f : 10f * -majorOrAdditional),
                                    halfScreenSize.y + 10f * majorOrAdditional + (CurrentFocused ? 130f : 0f) * majorOrAdditional);
            }
        }
        float TargetScale => BuffCard.normalScale + (CurrentFocused ? 0.05f : 0f);

        Vector3 basicRotation = new Vector3(0f, 180f, 0f);
        Vector3 TargetRotation => basicRotation + (buffCard.CurrentFocused ? new Vector3((30f * buffCard.LocalMousePos.y - 15f) * (flip ? -1f : 1f), -(30f * buffCard.LocalMousePos.x - 15f), 0f) : Vector3.zero);

        bool flip;

        public CardPickerShowAnimator(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            buffCard.Highlight = false;
            buffCard.DisplayDescription = false;
            buffCard.DisplayTitle = true;
            buffCard.DisplayAllGraphTexts = true;
            buffCard.Grey = false;

            buffCard.UpdateGraphText(true, false);
            buffCard.StackerAddOne = true;

            cardPickerInteractionManager = buffCard.interactionManager as CardPickerInteractionManager;
            buffCard.onMouseSingleClick += OnSingleClickFlip;

            initDelay = cardPickerInteractionManager.GetCardIndex(buffCard, out int _) * 20;

            if (cardPickerInteractionManager.Major2AdditionalMapper.TryGetValue(buffCard, out var additional))
                linkedCard = additional;
            if (cardPickerInteractionManager.Additional2MajorMapper.TryGetValue(buffCard, out var major))
                linkedCard = major;
        }

        public override void Update()
        {
            base.Update();
            if (delayTimer < initDelay)
                delayTimer++;

            if (delayTimer == initDelay)
            {
                delayTimer++;
            }

            if (delayTimer >= initDelay)
            {
                if (initFlipTimer < initFlipTime)
                    initFlipTimer++;

                if (initFlipTimer == initFlipTime)
                {
                    basicRotation = new Vector3(0f, 360f, 0f);
                    initFlipTimer++;
                }
            }

            if (CurrentFocused && !buffCard.Highlight)
                buffCard.Highlight = true;
            if (!CurrentFocused && buffCard.Highlight)
                buffCard.Highlight = false;
        }

        public override void GrafUpdate(float timeStacker)
        {
            if (delayTimer < initDelay)
                return;

            if (Mathf.Abs(buffCard.Rotation.x - TargetRotation.x) > 0.01f || Mathf.Abs(buffCard.Rotation.y - TargetRotation.y) > 0.01f)
                buffCard.Rotation = Vector3.Lerp(buffCard.Rotation, TargetRotation, 0.1f);

            if (Mathf.Abs(buffCard.Position.x - TargetPosition.x) > 0.01f || Mathf.Abs(buffCard.Position.y - TargetPosition.y) > 0.01f)
                buffCard.Position = Vector2.Lerp(buffCard.Position, TargetPosition, 0.1f);

            if (Mathf.Abs(buffCard.Scale - TargetScale) > 0.01f)
                buffCard.Scale = Mathf.Lerp(buffCard.Scale, TargetScale, 0.1f);
        }

        void OnSingleClickFlip()
        {
            if (!flip)
            {
                basicRotation = new Vector3(0, 180f + 360f, 0f);
                flip = true;
                buffCard.DisplayDescription = true;
            }
            else
            {
                basicRotation = new Vector3(0, 360f, 0f);
                flip = false;
                buffCard.DisplayDescription = false;
            }
        }

        public override void Destroy()
        {
            buffCard.onMouseSingleClick -= OnSingleClickFlip;
        }
    }

    internal class CardPickerDisappearAnimator : BuffCardAnimator
    {
        bool finished;
        float f = 0f;

        public CardPickerDisappearAnimator(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            buffCard.Highlight = false;
            buffCard.DisplayDescription = false;
            buffCard.DisplayTitle = false;
            buffCard.DisplayAllGraphTexts = false;
            buffCard.Highlight = false;
        }

        public override void GrafUpdate(float timeStacker)
        {
            if (finished)
            {
                buffCard.interactionManager.DismanageCard(buffCard);
                buffCard.Destroy();
            }

            f = Mathf.Lerp(f, 1f, 0.05f);

            buffCard.Rotation = Vector3.Lerp(initRotation, Vector3.zero, f);
            buffCard.Position = Vector2.Lerp(initPosition, new Vector2(2000, -200), f);
            buffCard.Alpha = 1f - f;
            if (Mathf.Abs(f - 1) < 0.01f)
                finished = true;
        }
    }
    #endregion

    #region BuffGameMenu
    internal class BuffGameMenuShowAnimataor : BuffCardAnimator
    {
        bool finished;
        BuffGameMenuSlot slot;

        Vector2 position;
        Vector2 lastPosition;

        Vector2 basicPosition;

        public BuffGameMenuShowAnimataor(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            buffCard.Highlight = false;
            buffCard.DisplayDescription = false;
            buffCard.DisplayTitle = false;
            buffCard.DisplayAllGraphTexts = false;
            buffCard.Highlight = false;
            buffCard.Grey = false;

            slot = (buffCard.interactionManager as DoNotingInteractionManager<BuffGameMenuSlot>).Slot;

            Vector2 halfScreenSize = Custom.rainWorld.screenSize / 2f;

            basicPosition = halfScreenSize + Vector2.up * 140f;
            int index = slot.GetCurrentIndex(buffCard, out int positiveOrNegative, out int totalLength);
            basicPosition += positiveOrNegative * Vector2.up * 70f;

            float biasFromMid = index - (totalLength - 1) / 2f;
            basicPosition += Vector2.right * Mathf.Min((600 / totalLength), 80f) * biasFromMid;

            position = lastPosition = basicPosition + slot.basePos;
            buffCard.Position = position;
            buffCard.Scale = BuffCard.normalScale * 0.5f;
            buffCard.Rotation = new Vector3(0f, 90f, 0f);
        }

        public override void Update()
        {
            base.Update();
            lastPosition = position;
            position = basicPosition + slot.basePos;
        }

        public override void GrafUpdate(float timeStacker)
        {
            buffCard.Position = Vector2.Lerp(lastPosition, position, timeStacker);

            if (finished)
            {
                return;
            }

            float t = 1f - Mathf.InverseLerp(0, 0.5f, slot.Scroll(timeStacker));
            buffCard.Rotation = new Vector3(0f, 90f * (1f - t), 0f);

            if (t == 1f)
                finished = true;
        }

        public override void Destroy()
        {
            buffCard.Position = basicPosition;
        }
    }

    internal class BuffGameMenuDisappearAnimataor : BuffCardAnimator
    {
        bool finished;
        BuffGameMenuSlot slot;

        Vector2 basicPosition;
        Vector2 lastPosition;
        Vector2 position;

        public BuffGameMenuDisappearAnimataor(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            buffCard.Highlight = false;
            buffCard.DisplayDescription = false;
            buffCard.DisplayTitle = false;
            buffCard.DisplayAllGraphTexts = false;
            buffCard.Highlight = false;
            buffCard.Grey = false;
            slot = (buffCard.interactionManager as DoNotingInteractionManager<BuffGameMenuSlot>).Slot;

            Vector2 halfScreenSize = Custom.rainWorld.screenSize / 2f;
            basicPosition = halfScreenSize + Vector2.up * 140f;
            int index = slot.GetCurrentIndex(buffCard, out int positiveOrNegative, out int totalLength);
            basicPosition += positiveOrNegative * Vector2.up * 70f;

            position = lastPosition = basicPosition + slot.basePos;
            buffCard.Position = position;
        }

        public override void Update()
        {
            base.Update();
            lastPosition = position;
            position = basicPosition + slot.basePos;
        }

        public override void GrafUpdate(float timeStacker)
        {
            buffCard.Position = Vector2.Lerp(lastPosition, position, timeStacker);

            if (finished)
            {
                slot.DestroyCard(buffCard);
            }

            float t = 1f - Mathf.InverseLerp(0.5f, 1f, slot.Scroll(timeStacker));
            buffCard.Rotation = new Vector3(0f, -90f * t, 0f);

            if (t == 1)
                finished = true;
        }
    }
    #endregion

    internal class ActivateCardAnimSlotAppendAnimator : BuffCardAnimator
    {
        static int flyInTime = 40;
        static int turnTime = 40;

        State currentState = State.FlyIn;
        int timer;
        int lastTimer;

        Vector2 halfScreen = Custom.rainWorld.screenSize / 2f;
        Vector2 targetPosition;
        Vector3 targetRotation;

        CommmmmmmmmmmmmmpleteInGameSlot.ActivateCardAnimSlot Slot;
        bool finished;

        public ActivateCardAnimSlotAppendAnimator(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            buffCard.Highlight = false;
            buffCard.DisplayDescription = false;
            buffCard.DisplayTitle = false;
            buffCard.DisplayAllGraphTexts = false;
            buffCard.Highlight = false;
            buffCard.Grey = false;

            buffCard.Scale = BuffCard.normalScale * 0.5f;

            Slot = buffCard.interactionManager.BaseSlot as CommmmmmmmmmmmmmpleteInGameSlot.ActivateCardAnimSlot;

            this.initRotation = buffCard.Rotation = new Vector3(0, 180, 0);
            this.initPosition = buffCard.Position = new Vector2(Custom.rainWorld.screenSize.x + 200f, halfScreen.y - 100f);
            targetPosition = new Vector2(Custom.rainWorld.screenSize.x - 50f, halfScreen.y - 100f);
            targetRotation = Vector3.zero;
        }

        public override void Update()
        {
            if (finished)
            {
                return;   
            }
            if (currentState == State.FlyIn)
            {
                if (timer < flyInTime)
                {
                    lastTimer = timer;
                    timer++;
                }
                else
                {
                    currentState = State.Turn;
                    timer = 0;
                    lastTimer = 0;
                }
            }
            else if(currentState == State.Turn)
            {
                if(timer < turnTime)
                {
                    lastTimer = timer;
                    timer++;
                }
                else
                {
                    Slot.FinishAnimation(buffCard);
                    finished = true;
                }
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            if(currentState == State.FlyIn)
            {
                float t = Mathf.Lerp((float)lastTimer / flyInTime, (float)timer / flyInTime, timeStacker);
                buffCard.Position = Vector2.Lerp(initPosition, targetPosition, Helper.LerpEase(t));
            }
            else if(currentState == State.Turn)
            {
                float t = Mathf.Lerp((float)lastTimer / turnTime, (float)timer / turnTime, timeStacker);
                buffCard.Rotation = Vector3.Lerp(initRotation, targetRotation, Helper.LerpEase(t));
            }
        }

        enum State
        {
            FlyIn,
            Turn
        }
    }

    internal class TriggerBuffAnimSlotTriggerAnimator : BuffCardAnimator
    {
        static int showUpTime = 40;
        static int hideTimer = 120;
        static int totalDisplayTime = 160;

        CommmmmmmmmmmmmmpleteInGameSlot.TriggerBuffAnimSlot Slot;

        Vector2 halfScreen = Custom.rainWorld.screenSize / 2f;
        Vector2 targetPosition;
        Vector3 targetRotation;

        Vector2 TargetPosition
        {
            get => targetPosition + Vector2.down * 60f * Slot.BuffCards.IndexOf(buffCard);
        }

        float targetScale;
        float targetAlpha;
        int timer;

        float lerpMulti = 1f;

        bool stage2;

        public TriggerBuffAnimSlotTriggerAnimator(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            buffCard.Highlight = false;
            buffCard.DisplayDescription = false;
            buffCard.DisplayTitle = false;
            buffCard.DisplayStacker = false;
            buffCard.Highlight = false;
            buffCard.Grey = false;

            buffCard.Scale = BuffCard.normalScale * 0.5f;

            Slot = buffCard.interactionManager.BaseSlot as CommmmmmmmmmmmmmpleteInGameSlot.TriggerBuffAnimSlot;

            this.initRotation = buffCard.Rotation = new Vector3(0, 360, 0);
            this.initPosition = buffCard.Position = CommmmmmmmmmmmmmpleteInGameSlot.TriggerBuffAnimSlot.hoverPos;

            targetPosition = this.initPosition;
            targetScale = BuffCard.normalScale * 0.3f;
            targetRotation = Vector3.zero;

            this.initScale = buffCard.Scale = 0f;
            buffCard.Alpha = 0f;
            targetAlpha = 1f;
            lerpMulti = 2f;
        }

        public override void Update()
        {
            base.Update();
            if(timer < totalDisplayTime)
            {
                timer++;
            }
            if(timer == totalDisplayTime)
            {
                timer++;
                Slot.RemoveCard(buffCard, true);
            }
            else if(!stage2 && (timer == showUpTime || Slot.BuffCards.IndexOf(buffCard) != 0))
            {
                IntoStage2();
            }
            else if(timer == hideTimer)
            {
                targetPosition += Vector2.left * 40f;
                targetAlpha = 0f;
                lerpMulti = 1f;
            }
        }

        void IntoStage2()
        {
            if (stage2)
                return;

            targetScale *= 0.6f;
            lerpMulti = 1.5f;

            stage2 = true;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            if (Mathf.Abs(buffCard.Rotation.x - targetRotation.x) > 0.01f || Mathf.Abs(buffCard.Rotation.y - targetRotation.y) > 0.01f)
                buffCard.Rotation = Vector3.Lerp(buffCard.Rotation, targetRotation, 0.1f * lerpMulti);

            if (Mathf.Abs(buffCard.Position.x - TargetPosition.x) > 0.01f || Mathf.Abs(buffCard.Position.y - TargetPosition.y) > 0.01f)
                buffCard.Position = Vector2.Lerp(buffCard.Position, TargetPosition, 0.1f * lerpMulti);

            if (Mathf.Abs(buffCard.Scale - targetScale) > 0.01f)
                buffCard.Scale = Mathf.Lerp(buffCard.Scale, targetScale, 0.1f * lerpMulti);

            if (Mathf.Abs(buffCard.Alpha - targetAlpha) > 0.01f)
                buffCard.Alpha = Mathf.Lerp(buffCard.Alpha, targetAlpha, 0.1f * lerpMulti);
        }
    }

    internal class BuffTimerAnimSlotShowAnimator : BuffCardAnimator
    {
        CommmmmmmmmmmmmmpleteInGameSlot.BuffTimerAnimSlot slot;
        CommmmmmmmmmmmmmpleteInGameSlot.BuffTimerAnimSlot.TimerInstance timerInstance;

        Vector2 pos;
        Vector2 lastPos;

        bool start;

        public BuffTimerAnimSlotShowAnimator(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            buffCard.Highlight = false;
            buffCard.DisplayDescription = false;
            buffCard.DisplayTitle = false;
            buffCard.DisplayAllGraphTexts = false;
            buffCard.Highlight = false;
            buffCard.Grey = false;

            buffCard.Scale = BuffCard.normalScale * 0.3f;
            buffCard.Alpha = 0f;

            slot = buffCard.interactionManager.BaseSlot as CommmmmmmmmmmmmmpleteInGameSlot.BuffTimerAnimSlot;
            buffCard._cardRenderer.cardCameraController.CardDirty = true;
        }

        public override void Update()
        {
            if (!start)
            {
                if(slot.buffCard2TimerInstanceMapper.TryGetValue(buffCard, out var timerInstance))
                {
                    this.timerInstance = timerInstance;
                    buffCard.Scale = BuffCard.normalScale * 0.15f;
                    start = true;
                }
            }
            else
            {
                lastPos = pos;
                pos = timerInstance.pos + Vector2.right * (1f - Helper.LerpEase(timerInstance.ShowTimerFactor)) * 80f;
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            if(start)
            {
                buffCard.Position = Vector2.Lerp(lastPos, pos, timeStacker);
                buffCard.Alpha = Mathf.Lerp(timerInstance.LastShowTimerFactor, timerInstance.ShowTimerFactor, timeStacker);
            }
        }
    }

    #region CardPedia
    internal class CardpediaSlotScrollingAnimator : BuffCardAnimator
    {
        public bool displayMode;
        public Vector2 offDisplayPos;
        public CardpediaSlotScrollingAnimator( BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            buffCard.DisplayAllGraphTexts = false;
            buffCard.Highlight = false;
        }
        
        public bool MouseOver
        {
            get
            {
                Vector2 screenPos = buffCard.Position;
                Vector2 mousePos = Cardpedia.CardpediaMenu.Instance.mousePosition;
                return mousePos.x > screenPos.x - 160f * buffCard.Scale && mousePos.y > screenPos.y - 335f * buffCard.Scale && 
                    mousePos.x < screenPos.x + 160f * buffCard.Scale && mousePos.y < screenPos.y + 335f * buffCard.Scale;
            }
        }

        public override void Update()
        {
            
        }
    }

    internal class CardpediaSlotDisplayAnimator : BuffCardAnimator
    {
        public CardpediaSlotDisplayAnimator(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            buffCard.DisplayAllGraphTexts = false;
        }


    }

    internal class CardpediaSlotStaticShowAnimator : BuffCardAnimator
    {
        public static float sheetYOffset = 120f;
        CardpediaSlot cardpediaSlot;

        public CardpediaSlotStaticShowAnimator(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            buffCard.Highlight = false;
            buffCard.DisplayDescription = false;
            buffCard.DisplayTitle = false;
            buffCard.DisplayAllGraphTexts = false;
            buffCard.Highlight = false;
            buffCard.Grey = false;

            cardpediaSlot = buffCard.interactionManager.BaseSlot as CardpediaSlot;

            buffCard.Scale = BuffCard.normalScale * 0.5f;
            buffCard.Alpha = 0f;

            int index = cardpediaSlot.BuffCards.IndexOf(buffCard);
            Vector2 position = new Vector2(350f + (300f * buffCard.Scale + 20f) * index, sheetYOffset);

            this.initPosition = buffCard.Position = position;
        }

        public override void Update()
        {
            if (buffCard.CurrentFocused && !buffCard.Highlight)
                buffCard.Highlight = true;
            else if (!buffCard.CurrentFocused && buffCard.Highlight)
                buffCard.Highlight = false;
        }

        public override void GrafUpdate(float timeStacker)
        {
            buffCard.Alpha = cardpediaSlot.alpha;
        }
    }
    #endregion
}
