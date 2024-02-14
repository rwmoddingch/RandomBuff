using DevInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff
{
    /// <summary>
    /// 存放帮助类的类
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// 按键跟踪简化类
        /// </summary>
        public class InputButtonTracker
        {
            Func<bool> tracker;
            bool trackUp;
            bool trackDouble;
            int doubleThreashold;

            bool state;
            bool lastState;

            int lastTriggleCounter;
            /// <summary>
            /// 
            /// </summary>
            /// <param name="trackFunction">按键更新的方法</param>
            /// <param name="trackUp">是否追踪按键信号上升沿</param>
            /// <param name="trackDouble">是否追踪双击</param>
            /// <param name="doubleThreashold">双击追踪的间隔</param>
            public InputButtonTracker(Func<bool> trackFunction, bool trackUp = true, bool trackDouble = false, int doubleThreashold = 5)
            {
                tracker = trackFunction;
                this.trackUp = trackUp;
                this.trackDouble = trackDouble;
                this.doubleThreashold = doubleThreashold;
            }

            /// <summary>
            /// 在逻辑更新中调用
            /// </summary>
            /// <param name="triggleSingle">为true时表示单击触发</param>
            /// <param name="triggleDouble">为true时表示双击触发</param>
            public void Update(out bool triggleSingle, out bool triggleDouble)
            {
                lastState = state;
                state = tracker.Invoke();
                triggleSingle = false;
                triggleDouble = false;
                
                if(lastState == trackUp && state != trackUp)
                {
                    if (trackDouble)
                    {
                        if (lastTriggleCounter < doubleThreashold)
                        {
                            triggleDouble = true;
                            lastTriggleCounter = doubleThreashold;
                        }
                        else
                        {
                            lastTriggleCounter = 0;
                        }
                    }
                    else
                        triggleSingle = true;
                }
                
                if(trackDouble && lastTriggleCounter < doubleThreashold)
                {
                    lastTriggleCounter++;
                    if(lastTriggleCounter == doubleThreashold)
                    {
                        triggleSingle = true;
                    }
                }
            }
        }

        /// <summary>
        /// 获取未初始化的实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetUninit<T>()
        {
            return (T)FormatterServices.GetSafeUninitializedObject(typeof(T));
        }

        public static void BaseUpdate(this Menu.Page page)
        {
            for (int i = 0; i < page.subObjects.Count; i++)
            {
                page.subObjects[i].Update();
            }
            page.lastPos = page.pos;
        }
    
        /// <summary>
        /// 缓动函数
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float LerpEase(float t)
        {
            return Mathf.Lerp(t, 1f, Mathf.Pow(t, 0.5f));
        }
    }
}
