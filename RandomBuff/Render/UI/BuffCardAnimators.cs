using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI
{
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

        public virtual void GrafUpdate()
        {

        }

        public virtual void Destroy()
        {

        }
    }

    /// <summary>
    /// 无状态
    /// </summary>
    internal class ClearStateAnimator : BuffCardAnimator
    {
        bool finished;
        public ClearStateAnimator(BuffCard buffCard, Vector2 initPosition, Vector3 initRotation, float initScale) : base(buffCard, initPosition, initRotation, initScale)
        {
            buffCard.Highlight = false;
            if(!buffCard.DisplayTitle) 
                buffCard.DisplayTitle = true;
        }

        public override void GrafUpdate()
        {
            if (finished) return;

            buffCard.Rotation = Vector3.Lerp(buffCard.Rotation, Vector3.zero, 0.1f);
            if(Mathf.Abs(buffCard.Rotation.x) < 0.01f && Mathf.Abs(buffCard.Rotation.y) < 0.01f)
                finished = true;
        }
    }


    /// <summary>
    /// 鼠标交互状态
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

        public override void GrafUpdate()
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
}
