using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RandomBuff.Render.UI.Component.BuffLevelBar;

namespace RandomBuff.Render.UI.Component
{
    internal abstract class BuffLevelBar
    {
        protected static float barHeight = 5;
        protected static Color barColor = Color.white;
        protected static Color barBackgroundColor = new Color(0.5f, 0.5f, 0.5f);

        protected int _exp;
        public virtual int Exp
        {
            get => _exp;
            set => _exp = value;
        }
        protected int level;
        protected int expNextLevelNeed;

        public readonly Func<int, int> exp2Level;
        public readonly Func<int, int> level2Exp;
        protected readonly float width;      

        protected FContainer ownerContainer;

        protected FSprite expBarBackground;
        protected FSprite expBar;

        //状态变量
        public float setAlpha;
        public float alpha;
        protected float lastAlpha;

        protected float barProgress;
        protected float lastBarProgress;

        public Vector2 pos;
        protected Vector2 lastPos;

        public virtual bool FinishState { get; }


        public BuffLevelBar(FContainer container, Vector2 pos, float width, int exp, Func<int,int> exp2Level, Func<int,int> level2Exp)
        {
            this.ownerContainer = container;
            this.width = width;
            this.Exp = exp;
            this.exp2Level = exp2Level;
            this.level2Exp = level2Exp;
            this.pos = pos;
            lastPos = pos;


            expBarBackground = new FSprite("pixel")
            {
                anchorX = 0f,
                anchorY = 0f,
                scaleX = width,
                scaleY = barHeight,
                color = barBackgroundColor
            };
            ownerContainer.AddChild(expBarBackground);

            expBar = new FSprite("pixel")
            {
                anchorX = 0f,
                anchorY = 0f,
                scaleX = width,
                scaleY = barHeight,
                color = barColor
            };
            ownerContainer.AddChild(expBar);
        }

        public virtual void Update()
        {
            lastPos = pos;
            lastBarProgress = barProgress;
            lastAlpha = alpha;
            if(alpha != setAlpha)
            {
                alpha = Mathf.Lerp(alpha, setAlpha, 0.15f);
                if (Mathf.Approximately(alpha, setAlpha))
                    alpha = setAlpha;
            }
        }

        public virtual void GrafUpdate(float timeStacker)
        {
            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);
            float smoothAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
            float smoothProgress = Mathf.Lerp(lastBarProgress, barProgress, timeStacker);

            expBar.SetPosition(smoothPos);
            expBar.scaleX = smoothProgress * width;
            expBar.alpha = smoothAlpha;

            expBarBackground.SetPosition(smoothPos);
            expBarBackground.alpha = smoothAlpha;
        }

        public class TimerOwner : BuffCardTimer.IOwnBuffTimer
        {
            Func<int> getSecond;
            public TimerOwner(Func<int> getSecond)
            {
                this.getSecond = getSecond;
            }

            public int Second => getSecond.Invoke();
        }
    }

    internal class BuffLevelBarDynamic : BuffLevelBar
    {
        //dynamicParam:
        int dynamic_animExpCurrentLevel;
        int dynamic_animCurrentExp;
        int expInLevel;
        int expCurrentLevelNeed;

        BuffCountDisplay dynamic_currentExp;
        BuffCountDisplay dynamic_currentLevel;
        BuffCountDisplay dynamic_nextExp;
        FLabel dynamic_levelLabel;
        FLabel dynamic_expLabel;

        TimerOwner dynamic_currentExpNum;
        TimerOwner dynamic_currentLevelNum;
        TimerOwner dynamic_nextExpNum;

        public override bool FinishState => dynamic_animCurrentExp == Exp;

        public BuffLevelBarDynamic(FContainer container, Vector2 pos, float width, int exp, Func<int, int> exp2Level, Func<int, int>level2Exp) : base(container, pos, width, exp, exp2Level, level2Exp)
        {
            UpdateLevel();

            dynamic_currentExpNum = new TimerOwner(() => dynamic_animCurrentExp);
            dynamic_currentExp = new BuffCountDisplay(ownerContainer, dynamic_currentExpNum)
            {
                alightment = BuffCountDisplay.Alightment.Left,
                scale = 1f,
            };

            dynamic_currentLevelNum = new TimerOwner(() => level);
            dynamic_currentLevel = new BuffCountDisplay(ownerContainer, dynamic_currentLevelNum)
            {
                alightment = BuffCountDisplay.Alightment.Left,
                scale = 1f,
                //largeStepMode = true
            };

            dynamic_nextExpNum = new TimerOwner(() => expNextLevelNeed);
            dynamic_nextExp = new BuffCountDisplay(ownerContainer, dynamic_nextExpNum)
            {
                alightment = BuffCountDisplay.Alightment.Right,
                scale = 1f,
                //largeStepMode = true
            };

            dynamic_levelLabel = new FLabel(Custom.GetDisplayFont(), "Lv.")
            {
                anchorX = 0f,
                anchorY = 0f,
            };
            ownerContainer.AddChild(dynamic_levelLabel);

            dynamic_expLabel = new FLabel(Custom.GetDisplayFont(), "exp:")
            {
                anchorX = 0f,
                anchorY = 1f,
            };
            ownerContainer.AddChild(dynamic_expLabel);
        }

        public override void Update()
        {
            base.Update();

            dynamic_currentExp.Update();
            dynamic_currentLevel.Update();
            dynamic_nextExp.Update();

            dynamic_currentExp.pos = pos + new Vector2(50f, -20f);
            dynamic_currentExp.lastPos = lastPos + new Vector2(50f, -20f);

            dynamic_currentLevel.pos = pos + new Vector2(35f, 25f);
            dynamic_currentLevel.lastPos = lastPos + new Vector2(35f, 25f);

            dynamic_nextExp.pos = pos + new Vector2(width, -20f);
            dynamic_nextExp.lastPos = lastPos + new Vector2(width, -20f);

            dynamic_currentExp.alpha = dynamic_currentLevel.alpha = dynamic_nextExp.alpha = alpha;

            if (dynamic_animCurrentExp != _exp)
            {
                for (int i = 0; i < 1; i++)
                    StepAnimExp();

                UpdateAnimExp();
            }

            if(dynamic_animExpCurrentLevel != expNextLevelNeed)
            {
                for (int i = 0; i < 10; i++)
                    StepAnimExpCurrentLevel();
            }

            void StepAnimExp()
            {
                if (dynamic_animCurrentExp < _exp)
                    dynamic_animCurrentExp++;
                else if (dynamic_animCurrentExp > _exp)
                    dynamic_animCurrentExp--;
            }

            void StepAnimExpCurrentLevel()
            {
                if (dynamic_animExpCurrentLevel < expNextLevelNeed)
                    dynamic_animExpCurrentLevel++;
                else if (dynamic_animExpCurrentLevel > expNextLevelNeed)
                    dynamic_animExpCurrentLevel--;
            }
        }

        public void HardSet()
        {
            dynamic_animCurrentExp = Exp;
            UpdateAnimExp();
            dynamic_animExpCurrentLevel = expNextLevelNeed;

            dynamic_currentExp.HardSet();
            dynamic_currentLevel.HardSet();
            dynamic_nextExp.HardSet();
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            dynamic_currentExp.GrafUpdate(timeStacker);
            dynamic_currentLevel.GrafUpdate(timeStacker);
            dynamic_nextExp.GrafUpdate(timeStacker);

            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);
            float smoothAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);

            dynamic_levelLabel.SetPosition(smoothPos + new Vector2(0f, 10f));
            dynamic_expLabel.SetPosition(smoothPos + new Vector2(0f, -5f));

            dynamic_levelLabel.alpha = smoothAlpha;
            dynamic_expLabel.alpha = smoothAlpha;

            if (Input.GetKey(KeyCode.I))
                Exp++;
        }

        
        void UpdateAnimExp()
        {
            if (dynamic_animCurrentExp > expNextLevelNeed)
            {
                UpdateLevel();
            }

            barProgress = (dynamic_animCurrentExp - expCurrentLevelNeed) / (float)expInLevel;
        }

        void UpdateLevel()
        {
            level = exp2Level(dynamic_animCurrentExp);
            expCurrentLevelNeed = level2Exp(level);
            expNextLevelNeed = level2Exp(level + 1);
            expInLevel = expNextLevelNeed - level2Exp(level);
        }
    }
}
