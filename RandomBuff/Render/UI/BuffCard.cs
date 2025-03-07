using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.CardRender;
using RandomBuff.Render.Quest;
using RandomBuff.Render.UI.Component;
using RandomBuffUtils;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

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
        bool _texInit = true;
        FSprite _ftexture;
        public FContainer Container { get; private set; }
        public RenderTexture RenderTexture { get => _cardRenderer.cardCameraController.targetTexture; }

        public BuffID ID { get; private set; }
        public BuffStaticData StaticData => BuffConfigManager.GetStaticData(ID);

        float _tempRotZ;
        public Vector3 Rotation
        {
            get => new Vector3(_cardRenderer.Rotation.x, _cardRenderer.Rotation.y, _ftexture != null ? _ftexture.rotation : _tempRotZ);
            set
            {
                _cardRenderer.Rotation = value;
                if (_ftexture != null)
                    _ftexture.rotation = value.z;
                else
                    _tempRotZ = value.z;
            }
        }

        Vector2 _tempPos;
        public Vector2 Position
        {
            get => _ftexture != null ? _ftexture.GetPosition() : _tempPos;
            set
            {
                if (_ftexture != null)
                    _ftexture.SetPosition(value);
                else
                    _tempPos = value;
            }
        }

        float _tempScale = 1f;
        public float Scale
        {
            get => (_ftexture != null) ? _ftexture.scale : _tempScale;
            set
            {
                if(_ftexture != null)
                    _ftexture.scale = value;
                else
                    _tempScale = value;

            }
        }

        float _tempAlpha = 1f;
        public float Alpha
        {
            get => _ftexture != null ? _ftexture.alpha : _tempAlpha;
            set
            {
                if (_ftexture != null)
                    _ftexture.alpha = value;
                else
                    _tempAlpha = value;
            }
        }

        //动画机
        public AnimatorState currentAniamtorState = AnimatorState.Test_None;
        public AnimatorState lastAnimatorState = AnimatorState.Test_None;
        public BuffCardAnimator currentAnimator;

        //交互
        public CardInteractionManager interactionManager;

        public Action onMouseSingleClick;
        public Action onMouseRightClick;

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
        internal BuffCardRenderer _cardRenderer;
        internal SpecialBuffEffect _specialBuffEffect;

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
                if(StaticData.Stackable || !value)
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
                if (StaticData.Countable || !value)
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
                if (StaticData.Triggerable || !value)
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

            if(ID.GetStaticData().BuffProperty == BuffProperty.Special)
                _specialBuffEffect = new SpecialBuffEffect(ID.GetStaticData().BuffType, Container);

            _ftexture = _cardRenderer.CleanGetTexture();
            Container.AddChild(_cardRenderer.Texture);

            Reset();
            SetAnimatorState(initState);


            //Helper.TraceStack();
            //BuffPlugin.Log("BuffCard init");
        }

        //更新方法，在交互管理器中调用
        public void Update()
        {
            //if(!_texInit && _cardRenderer.cardCameraController.targetTexture != null)
            //{
            //    Container.AddChild(_ftexture = new FTexture(_cardRenderer.cardCameraController.targetTexture, ""));
            //    _ftexture.rotation = _tempRotZ;
            //    _ftexture.scale = _tempScale;
            //    _ftexture.SetPosition(_tempPos);
            //    _ftexture.alpha = _tempAlpha;
            //    _texInit = true;
            //}
            currentAnimator?.Update();
            _specialBuffEffect?.Update(this);
        }

        public void GrafUpdate(float timeStacker)
        {
            currentAnimator?.GrafUpdate(timeStacker);
            _specialBuffEffect?.GrafUpdate(this, timeStacker);
        }

        public void Destroy()
        {
            CardRendererManager.RecycleCardRenderer(_cardRenderer);
            onMouseSingleClick = null;
            onMouseRightClick = null;
            Container.RemoveAllChildren();
            Container.RemoveFromContainer();
            _ftexture.RemoveFromContainer();
            _specialBuffEffect?.Destroy();

            //Helper.TraceStack();
            //BuffPlugin.Log("BuffCard Destroy");
        }

        //改变卡牌的状态
        public void SetAnimatorState(AnimatorState newState)
        {
            if (newState == currentAniamtorState && currentAnimator != null)
                return;

            //BuffPlugin.Log($"{ID}-{interactionManager} switch to {newState}");
            //if (newState == AnimatorState.Test_None)
            //    Helper.TraceStack();

            lastAnimatorState = currentAniamtorState;
            var lastAnimator = currentAnimator;
            currentAnimator?.Destroy();
            currentAniamtorState = newState;

            ResetComponents();

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
            else if(newState == AnimatorState.CardpediaSlot_Scrolling)
            {
                currentAnimator = new CardpediaSlotScrollingAnimator(this, Position, Rotation, Scale);
            }
            else if(newState == AnimatorState.CardpediaSlot_StaticShow)
            {
                currentAnimator = new CardpediaSlotStaticShowAnimator(this, Position, Rotation, Scale);
            }
            else if(newState == AnimatorState.CardPocketSlot_Normal)
            {
                currentAnimator = new CardPocketNormalAnimator(this, Position, Rotation, Scale);
            }
            else if (newState == AnimatorState.CardPocketSlot_Exclusive)
            {
                currentAnimator = new CardPocketExclusiveAnimator(this, Position, Rotation, Scale);
            }
            else
            {
                BuffPlugin.LogWarning($"No matched animator for state {newState}, please check codes");
            }
            currentAnimator.LastAnimatorHandInfo(lastAnimator);
        }

        public void ResetComponents()
        {
            DisplayAllGraphTexts = false;
            DisplayDescription = false;
            DisplayTitle = false;
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

        public void Reset()
        {
            Scale = normalScale;
            Alpha = 1f;
            Rotation = Vector3.zero;
        }

        public void OnMouseSingleClick()
        {
            onMouseSingleClick?.Invoke();
            BuffPlugin.Log("Card singleclick");
        }

        public void OnMouseRightClick()
        {
            onMouseRightClick?.Invoke();
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
            BuffTimerAnimSlot_Show,

            //图鉴界面卡槽动画状态
            CardpediaSlot_Scrolling,
            CardpediaSlot_Displaying,
            CardpediaSlot_StaticShow,

            //卡包卡槽动画状态
            CardPocketSlot_Normal,
            CardPocketSlot_Exclusive,
        }
    }

    internal class SpecialBuffEffect
    {
        static int particleCount = 80;
        static float emitRate = 1 / 10f;
        static Color gold = Custom.hexToColor("FFB81D");

        BuffType buffType;

        FSprite[] particle, light;
        Vector2[] vel, pos, lastPos;
        int[] life, lastLife, initLife;
        float[] scale;

        bool lastEmit;
        float emitCounter;
        int continueEmitCounter;
        Vector2 lastBuffCardPos;
        Color effectCol;
        public float alpha;

        public SpecialBuffEffect(BuffType buffType, FContainer container)
        {
            this.buffType = buffType;

            particle = new FSprite[particleCount];
            light = new FSprite[particleCount];
            pos = new Vector2[particleCount];
            lastPos = new Vector2[particleCount];
            life = new int[particleCount];
            lastLife = new int[particleCount];
            life = new int[particleCount];
            initLife = new int[particleCount];
            scale = new float[particleCount];
            vel = new Vector2[particleCount];

            if (buffType == BuffType.Positive)
                effectCol = gold;
            else if (buffType == BuffType.Negative)
                effectCol = Custom.hexToColor("80013A");
            else if (buffType == BuffType.Duality)
                effectCol = Color.gray;

            for (int i = 0; i < particleCount; i++)
            {
                particle[i] = new FSprite(buffType == BuffType.Negative ? BuffUIAssets.CircleGradient20  : "pixel", true)
                {
                    color = buffType == BuffType.Negative ? Color.red : Color.white,
                    alpha = 0f,
                    shader = Custom.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"],
                    isVisible = false
                };
                container.AddChild(particle[i]);
                light[i] = new FSprite(buffType == BuffType.Negative ? "buffinfos\\BuiltinBuffs\\cardinfos\\positive\\flamethrower\\flameVFX1" : BuffUIAssets.CircleGradient20, true)
                {
                    color = effectCol,
                    alpha = 0f,
                    shader = Custom.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"],
                    isVisible = false
                };
                container.AddChild(light[i]);
            }
        }

        internal void Update(BuffCard buffCard)
        {

            if (continueEmitCounter > 0)
                continueEmitCounter--;

            if(!buffCard.DisplayTitle && lastEmit)
            {
                continueEmitCounter = 20;
            }

            Vector2 deltaVel = Vector2.ClampMagnitude((buffCard.Position - lastBuffCardPos) * 20, 40f);
            if (buffCard.DisplayTitle || continueEmitCounter > 0)
            {
                emitCounter += 1/40f;

                float delta = Vector2.Distance(buffCard.Position, lastBuffCardPos);
                emitCounter += emitRate * delta / 50f;
            }

            lastEmit = buffCard.DisplayTitle;
            lastBuffCardPos = buffCard.Position;

            int emit = 0;
            while(emitCounter >= emitRate)
            {
                emit++;
                emitCounter -= emitRate;
            }

            for(int i = 0;i < particleCount; i++)
            {
                lastLife[i] = life[i];

                if (life[i] > 0)
                    life[i]--;

                if (lastLife[i] == life[i] && life[i] == 0)
                {
                    if (emit > 0)
                    {
                        EmitParticle(buffCard, i, deltaVel);
                        emit--;
                    }
                    else
                    {
                        particle[i].isVisible = false;
                        light[i].isVisible = false;
                        continue;
                    }
                }

                lastPos[i] = pos[i];
                pos[i] += vel[i] / 40f;

                if(buffType == BuffType.Positive)
                {
                    vel[i] *= 0.95f;
                    vel[i] += Custom.RNV();
                }
                else if(buffType == BuffType.Negative)
                {
                    vel[i] *= 0.93f;
                    vel[i].x += Mathf.Sin((life[i] + i) * 0.075f) * life[i] / (float)initLife[i];
                    vel[i] += Custom.RNV();
                }
                else
                {
                    vel[i] *= 0.95f;
                    vel[i] += Custom.RNV();
                }
            }
        }

        void EmitParticle(BuffCard buffCard, int i, Vector2 velEffect)
        {
            particle[i].isVisible = true;
            light[i].isVisible = true;
            

            if (buffType == BuffType.Positive)
            {
                pos[i] = lastPos[i] = buffCard.Position + buffCard.Scale * Custom.RNV() * 50f * Random.value;

                vel[i] = Custom.RNV() * Mathf.Lerp(155f, 185f, Random.value) * buffCard.Scale * 4f;
                vel[i].x *= 0.6f;
                vel[i] += velEffect;
            }
            else if(buffType == BuffType.Negative)
            {
                pos[i] = lastPos[i] = buffCard.Position + buffCard.Scale * Custom.RNV() * 150f * Random.value;

                vel[i] = Custom.RNV() * 50f + Vector2.down * Mathf.Lerp(175f, 195f, Random.value) * buffCard.Scale * 4f;
                vel[i].x *= 0.6f;
                vel[i] += velEffect * 0.6f;
            }
            else
            {
                pos[i] = lastPos[i] = buffCard.Position + buffCard.Scale * Custom.RNV() * 50f * Random.value;

                vel[i] = Custom.RNV() * Mathf.Lerp(155f, 185f, Random.value) * buffCard.Scale * 4f;
                vel[i].x *= 0.6f;
                vel[i] += velEffect;
            }
            
            
            initLife[i] = life[i] = lastLife[i] = 80;
            scale[i] = buffCard.Scale;

            if(buffType == BuffType.Negative)
            {
                particle[i].scale = buffCard.Scale * 2f;
                light[i].scale = buffCard.Scale * 4f;
            }
            else
            {
                particle[i].scale = buffCard.Scale * 2f * 4f;
                light[i].scale = buffCard.Scale * 2f;
            }
            
        }

        public void GrafUpdate(BuffCard buffCard, float timeStacker)
        {
            for(int i = 0; i < particle.Length; i++)
            {
                if (!particle[i].isVisible)
                    continue;

                Vector2 smoothPos = Vector2.Lerp(lastPos[i], pos[i], timeStacker);
                float smoothL = Mathf.Lerp(lastLife[i] , life[i], timeStacker) / initLife[i];

                if(buffType == BuffType.Negative)
                {
                    particle[i].alpha = light[i].alpha = (smoothL + Mathf.Sin(smoothL * 40f + Random.value * 1f) * 0.2f) * buffCard.Alpha;
                    light[i].alpha = particle[i].alpha * 0.5f;
                }
                else
                {
                    light[i].alpha = particle[i].alpha = light[i].alpha = (smoothL + Mathf.Sin(smoothL * 18f) * 0.1f) * buffCard.Alpha;
                }
                

                particle[i].SetPosition(smoothPos);
                light[i].SetPosition(smoothPos);
            }
        }

        public void Destroy()
        {
            for(int i = 0; i < particle.Length; ++i)
            {
                particle[i].isVisible = false;
                particle[i].RemoveFromContainer();

                light[i].isVisible = false;
                light[i].RemoveFromContainer();
            }
        }
    }
}
