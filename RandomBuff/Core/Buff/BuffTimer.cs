using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.Buff
{
    /// <summary>
    /// 卡牌的计时器
    /// </summary>
    public abstract class BuffTimer
    {
        public int frames;
        public int lastFrame;

        /// <summary>
        /// 计时器完成时的回调
        /// </summary>
        public Action<BuffTimer, RainWorldGame> TimerCallBack {  get; private set; }
        public BuffTimerDisplayStrategy DisplayStrategy { get; protected set; }

        public virtual int Second { get => frames / 40; }
        public virtual int LastSecond { get => lastFrame / 40; }

        /// <summary>
        /// 是否暂停计时器
        /// </summary>
        public bool Paused { get; set; }

        public BuffTimer(Action<BuffTimer, RainWorldGame> timerCallBack)
        {
            TimerCallBack = timerCallBack;
        }

        /// <summary>
        /// 在更新中调用
        /// </summary>
        public virtual void Update(RainWorldGame game)
        {
        }

        /// <summary>
        /// 重置定时器
        /// </summary>
        public virtual void Reset()
        {
            BuffPlugin.Log($"{GetType()} timer reset");
        }

        /// <summary>
        /// 应用显示策略
        /// </summary>
        /// <param name="strategy"></param>
        public void ApplyStrategy(BuffTimerDisplayStrategy strategy)
        {
            DisplayStrategy = strategy;
            BuffPlugin.Log($"{GetType()} apply strategy {strategy.GetType()}");
        }
    }

    /// <summary>
    /// 累积计时器
    /// </summary>
    public abstract class StepedCountBuffTimer : BuffTimer
    {
        int step;
        int lowLimitFrame;
        int highLimitFrame;
        bool autoReset;

        public StepedCountBuffTimer(Action<BuffTimer, RainWorldGame> callBack, int step, int highLimit, int lowLimit, bool autoReset,  bool useDefaultStrategy) : base(callBack)
        {
            if (step == 0)
                throw new ArgumentOutOfRangeException("Argument 'step' cant be zero");
            if (highLimit < lowLimit)
                throw new ArgumentOutOfRangeException("Argument 'highLimit' must be greater than 'lowLimit'");

            this.step = step;
            this.lowLimitFrame = lowLimit * 40;
            this.highLimitFrame = highLimit * 40;
            this.autoReset = autoReset;

            if (useDefaultStrategy)
            {
                int timeSpan = highLimit - lowLimit;
                if(timeSpan <= 15)
                {
                    ApplyStrategy(new SpanTimerDisplayStrategy(
                        new SpanTimerDisplayStrategy.BuffTimeSpan(lowLimit, lowLimit + 3),
                        new SpanTimerDisplayStrategy.BuffTimeSpan(highLimit - 3, highLimit)
                        ));
                }
                else if(timeSpan > 15 && timeSpan <= 25)
                {
                    int midSec = lowLimit + timeSpan / 2;

                    ApplyStrategy(new SpanTimerDisplayStrategy(
                        new SpanTimerDisplayStrategy.BuffTimeSpan(lowLimit, lowLimit + 3),
                        new SpanTimerDisplayStrategy.BuffTimeSpan(highLimit - 3, highLimit),
                        new SpanTimerDisplayStrategy.BuffTimeSpan(midSec - 1, midSec + 2)
                        ));
                }
                else
                {
                    List<SpanTimerDisplayStrategy.BuffTimeSpan> lst = new();
                    for (int i = lowLimit;i < highLimit; i += 10)
                    {
                        lst.Add(new SpanTimerDisplayStrategy.BuffTimeSpan(i - 1, i + 2));
                    }
                    lst.Add(new SpanTimerDisplayStrategy.BuffTimeSpan(lowLimit, lowLimit + 3));
                    lst.Add(new SpanTimerDisplayStrategy.BuffTimeSpan(highLimit - 3, highLimit));

                    ApplyStrategy(new SpanTimerDisplayStrategy(lst.ToArray()));
                }
            }
            Reset();
        }

        public override void Update(RainWorldGame game)
        {
            lastFrame = frames;
            
            if (Paused)
                return;

            if (frames < highLimitFrame && frames > lowLimitFrame)
                frames += step;
            else if(frames == highLimitFrame || frames == lowLimitFrame)
            {
                if (autoReset)
                    Reset();

                TimerCallBack.Invoke(this, game);
                frames += step;
            }
            if (Second != LastSecond)
            {
                DisplayStrategy?.UpdateSecond(Second);
                //BuffPlugin.Log($"{GetType()} timer : {LastSecond} => {Second}, display : {DisplayStrategy.DisplayThisFrame}");
            }
        }

        public override void Reset()
        {
            base.Reset();
            if (step > 0)
                frames = lowLimitFrame + step;
            else
                frames = highLimitFrame + step;
        }
    }

    /// <summary>
    /// 倒计时计时器
    /// </summary>
    public class DownCountBuffTimer : StepedCountBuffTimer
    {
        public DownCountBuffTimer(Action<BuffTimer, RainWorldGame> callBack, int highLimit = 10, int lowLimit = 0, bool autoReset = true, bool useDefaultStrategy = true) : base(callBack, -1, highLimit, lowLimit, autoReset, useDefaultStrategy)
        {
        }

        public DownCountBuffTimer(Action<BuffTimer, RainWorldGame> callBack, BuffTimerDisplayStrategy strategy, int highLimit = 10, int lowLimit = 0, bool autoReset = true) : base(callBack, -1, highLimit, lowLimit, autoReset, false)
        {
            ApplyStrategy(strategy);
        }
    }

    /// <summary>
    /// 正计时计时器
    /// </summary>
    public class UpCountBuffTimer : StepedCountBuffTimer
    {
        public UpCountBuffTimer(Action<BuffTimer, RainWorldGame> callBack, int highLimit = 10, int lowLimit = 0, bool autoReset = true, bool useDefaultStrategy = true) : base(callBack, 1, highLimit, lowLimit, autoReset, useDefaultStrategy)
        {
        }

        public UpCountBuffTimer(Action<BuffTimer, RainWorldGame> callBack, BuffTimerDisplayStrategy strategy, int highLimit = 10, int lowLimit = 0, bool autoReset = true) : base(callBack, 1, highLimit, lowLimit, autoReset, false)
        {
            ApplyStrategy(strategy);
        }
    }


    #region Strategy
    /// <summary>
    /// 卡牌计时器的显示策略
    /// </summary>
    public abstract class BuffTimerDisplayStrategy
    {
        /// <summary>
        /// 绑定的计时器
        /// </summary>
        public int Second { get; protected set; }

        /// <summary>
        /// 该秒内是否展示计时器（在UI中实现）
        /// </summary>
        public virtual bool DisplayThisFrame { get; }

        public virtual void UpdateSecond(int newSecond)
        {
            Second = newSecond;
        }
    }

    /// <summary>
    /// 时间跨显示策略
    /// </summary>
    public class SpanTimerDisplayStrategy : BuffTimerDisplayStrategy
    {
        BuffTimeSpan[] ranges;

        bool displayThisFrame;
        public override bool DisplayThisFrame => displayThisFrame;

        /// <summary>
        /// </summary>
        /// <param name="timer"></param>
        /// <param name="ranges">键为时间跨下限</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public SpanTimerDisplayStrategy(params BuffTimeSpan[] ranges)
        {
            if (ranges.Length == 0)
                throw new ArgumentOutOfRangeException($"Argument 'ranges' must be longer than zero");

            this.ranges = ranges;
        }

        public override void UpdateSecond(int newSecond)
        {
            base.UpdateSecond(newSecond);
            foreach(var range in ranges)
            {
                if(Second >= range.low && Second <= range.high)
                {
                    displayThisFrame = true;
                    return;
                }
            }
            displayThisFrame = false;
        }

        /// <summary>
        /// 表示一个以秒为单位的时间跨，上下界均包含在内
        /// </summary>
        public struct BuffTimeSpan
        {
            public int low;
            public int high;

            public BuffTimeSpan(int low, int high)
            {
                this.low = low;
                this.high = high;

                if (high < low)
                    throw new ArgumentOutOfRangeException("Argument 'high' must be greater than 'low'");

            }
        }
    }
    #endregion
}
