using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu.Remix.MixedUI;
using RandomBuff.Render.CardRender;
using RandomBuff.Render.UI;
using RWCustom;
using UnityEngine;
using static RandomBuff.Cardpedia.Elements.Config.OpCardpediaChainBox;


namespace RandomBuff.Cardpedia.Elements.Config
{
    internal class OpCardpediaChainBox : UIelement
    {
        public OpCardpediaChainBox chainTarget;
        public readonly MouseEvent mouseEvent;
        public string title;

        FLabel titleLabel;
        FSprite cosmeticRect;
        FSprite cosmeticLineA;
        FSprite cosmeticLineB;

        public Vector2 defaultPos;
        public Vector2 defaultRectSize;

        protected Vector2 setRectSize;
        protected Vector2 rectSize;
        protected Vector2 lastRectSize;

        public Vector2 RawChainedPos
        {
            get
            {
                Vector2 result;
                if(chainTarget == null)
                    result = defaultPos;
                else
                    result = new Vector2(0f, -CardpediaStatics.tinyGap - chainTarget.rectSize.y) + chainTarget.RawChainedPos;
                return result;
            }
        }
        public Vector2 ChainedPos => InScrollBox ? RawChainedPos + scrollBox._childOffset : RawChainedPos;


        //左上角锚点，因此要改变
        public override bool MouseOver => MousePos.x > 0f && MousePos.y < 0f && MousePos.x < rectSize.x && MousePos.y > -rectSize.y;

        /// <summary>
        /// 使用左上角锚点
        /// </summary>
        /// <param name="defaultPos"></param>
        /// <param name="size"></param>
        /// <param name="autoSetPosInScrollBox">自动反向计算坐标（适应scrollBox）</param>
        public OpCardpediaChainBox(string title, Vector2 defaultPos, Vector2 size, bool autoInitSprites = true) : base(defaultPos, Vector2.zero)
        {
            this.defaultPos = defaultPos;
            setRectSize = this.defaultRectSize = size;
            this.title = title;

            if(autoInitSprites) 
                InitSprites();

            mouseEvent = new MouseEvent(this);
            //mouseEvent.AddEvent(() => Vector2.zero, () => rectSize, () => setRectSize = new Vector2(defaultRectSize.x, defaultRectSize.y + 100f), () => setRectSize = defaultRectSize, ()=> rectSize += new Vector2(0, 200f), null, null);
        }

        public virtual void InitSprites()
        {
            cosmeticRect = new FSprite("pixel")
            {
                anchorX = 0f,
                anchorY = 1f,
                color = CardpediaStatics.pediaUIDarkGrey,
                shader = Custom.rainWorld.Shaders["MenuTextCustom"]
            };           
            myContainer.AddChild(cosmeticRect);
            
            cosmeticLineA = new FSprite("pixel") 
            { 
                anchorX = 0f, 
                anchorY = 1f, 
                color = CardpediaStatics.pediaUILightGrey 
            };
            myContainer.AddChild(cosmeticLineA);

            cosmeticLineB = new FSprite("pixel") 
            { 
                scaleX = 1f, 
                scaleY = size.y - 40f - CardpediaStatics.tinyGap, 
                anchorX = 0f, 
                anchorY = 1f, 
                color = CardpediaStatics.pediaUILightGrey 
            };
            myContainer.AddChild(cosmeticLineB);

            titleLabel = new FLabel(Custom.GetDisplayFont(), title)
            {
                alignment = FLabelAlignment.Left,
                anchorX = 0f,
                anchorY = 1f
            };
            myContainer.AddChild(titleLabel);
            RecaculateSpriteScaleAndPos(1f);
        }

        public virtual void RecaculateSpriteScaleAndPos(float timeStacker)
        {
            Vector2 smoothSize = Vector2.Lerp(lastRectSize, rectSize, timeStacker);
            Vector2 smoothScreenPos = Vector2.Lerp(lastScreenPos, ScreenPos, timeStacker);

            cosmeticRect.scaleX = smoothSize.x - CardpediaStatics.tinyGap * 2;
            cosmeticRect.scaleY = CardpediaStatics.chainBox_cosmeticRectHeight;
            cosmeticRect.SetPosition(new Vector2(CardpediaStatics.tinyGap, -CardpediaStatics.tinyGap)/* + smoothScreenPos*/);

            cosmeticLineA.scaleX = smoothSize.x;
            cosmeticLineA.scaleY = 1f;
            cosmeticLineA.SetPosition(new Vector2(0f, -smoothSize.y) /*+ smoothScreenPos*/);

            cosmeticLineB.scaleX = 1f;
            cosmeticLineB.scaleY = smoothSize.y - CardpediaStatics.chainBox_cosmeticRectHeight - CardpediaStatics.tinyGap;
            cosmeticLineB.SetPosition(smoothSize.x, -CardpediaStatics.tinyGap - CardpediaStatics.chainBox_cosmeticRectHeight);

            titleLabel.SetPosition(new Vector2(CardpediaStatics.tinyGap, -CardpediaStatics.tinyGap) /*+ smoothScreenPos*/);
        }

        //处理ScrollBox
        public override void Change()
        {
            base.Change();
            if (chainTarget != null)
                return;

            RecaculateSpriteScaleAndPos(1f);
        }

        public override void Update()
        {
            lastRectSize = rectSize;
            base.Update();

            rectSize = Vector2.Lerp(rectSize, setRectSize, 0.15f);
            mouseEvent.Update();
            _pos = ChainedPos;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            RecaculateSpriteScaleAndPos(timeStacker);
        }

        public class MouseEvent
        {
            public OpCardpediaChainBox chainBox;

            List<MouseEventInstance> eventInstances = new List<MouseEventInstance>();
            public Vector2 MousePos => chainBox.MousePos;

            public MouseEvent(OpCardpediaChainBox chainBox)
            {
                this.chainBox = chainBox;
            }

            public void Update()
            {
                foreach (var instance in eventInstances)
                {
                    instance.Update();
                }
            }

            public MouseEventInstance AddEvent(Func<Vector2> localPos, Func<Vector2> rect, Action OnMouseEnter, Action OnMouseExist, Action OnMouseClick, Action<Vector2> onMouseDrag, Action<Vector2> onMouseDragRelease)
            {
                var instance = new MouseEventInstance(this);
                instance.getLocalPos = localPos;
                instance.getRect = rect;
                instance.onMouseClick = OnMouseClick;
                instance.onMouseDrag = onMouseDrag;
                instance.onMouseDragRelease = onMouseDragRelease;
                instance.onMouseEnter = OnMouseEnter;
                instance.onMouseExist = OnMouseExist;
                eventInstances.Add(instance);
                return instance;
            }

            public class MouseEventInstance
            {
                public bool lastMouseDown;
                public bool mouseDown;

                public bool lastMouseOver;
                public bool mouseOver;

                public Func<Vector2> getLocalPos;
                public Func<Vector2> getRect;//top-left

                MouseEvent mouseEvent;

                public Action onMouseEnter;
                public Action onMouseExist;
                public Action onMouseClick;
                public Action<Vector2> onMouseDrag;
                public Action<Vector2> onMouseDragRelease;

                Vector2 begingDragPos;

                public bool enable = true;

                public MouseEventInstance(MouseEvent mouseEvent)
                {
                    this.mouseEvent = mouseEvent;
                }

                public void Update()
                {
                    if (!enable) return;
                    lastMouseOver = mouseOver;
                    lastMouseDown = mouseDown;

                    Vector2 localMousePos = mouseEvent.MousePos - getLocalPos.Invoke();
                    Vector2 rect = getRect.Invoke();
                    mouseOver = (localMousePos.x > 0 && localMousePos.x < rect.x && localMousePos.y < 0 && localMousePos.y > -rect.y);

                    if (!lastMouseOver && mouseOver)
                        onMouseEnter?.Invoke();
                    else if(lastMouseOver && !mouseOver)
                        onMouseExist?.Invoke();

                    if (mouseOver)
                        mouseDown = Input.GetMouseButton(0);
                    else /*if(!mouseDown)*/
                        mouseDown = false;

                    Vector2 mousePos = mouseEvent.chainBox.Menu.mousePosition;
                    if (!lastMouseDown && mouseDown)
                    {
                        onMouseClick?.Invoke();
                        begingDragPos = mousePos;
                    }
                    else if (lastMouseDown && mouseDown)
                        onMouseDrag?.Invoke(mousePos - begingDragPos);
                    else if (lastMouseDown && !mouseDown)
                        onMouseDragRelease?.Invoke(mousePos - begingDragPos);
                }
            }
        }
    
        public class AnimBehaviour
        {
            Color normalColor;
            Color animColor;
            Color flashColor;

            float flash;
            float lastFlash;

            float anim;
            float lastAnim;
            float targetAnim;

            public bool Anim
            {
                get => targetAnim == 1f;
                set => targetAnim = value ? 1f : 0f;
            }

            public AnimBehaviour(Color normalColor, Color animColor, Color flashColor)
            {
                this.normalColor = normalColor;
                this.animColor = animColor;
                this.flashColor = flashColor;
            }

            public void Flash()
            {
                flash = 1f;
            }

            public void Update()
            {
                lastFlash = flash;
                if(flash != 0f)
                {
                    flash = Mathf.Lerp(flash, 0f, 0.25f);
                    if (Mathf.Approximately(flash, 0f))
                        flash = 0f;
                }

                lastAnim = anim;
                if(anim != targetAnim)
                {
                    anim = Mathf.Lerp(anim, targetAnim, 0.15f);
                    if(Mathf.Approximately(anim, targetAnim))
                        anim = targetAnim;
                }
            }

            public float SmoothAnim(float timeStacker)
            {
                return Mathf.Lerp(lastAnim, anim, timeStacker);
            }

            public Color GetColor(float timeStacker)
            {
                float sAnim = SmoothAnim(timeStacker);
                float sFlash = Mathf.Lerp(lastFlash, flash, timeStacker);

                return Color.Lerp(Color.Lerp(normalColor, animColor, sAnim), flashColor, sFlash);
            }
        }
    }
}
