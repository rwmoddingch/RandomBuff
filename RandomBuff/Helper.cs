using DevInterface;
using MonoMod.Utils;
using RewiredConsts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Action = System.Action;

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

        public static class DynamicImitator
        {
            static Dictionary<Type, FastReflectionDelegate> additionDelegates = new();
            static Dictionary<Type, FastReflectionDelegate> subtractionDelegates = new();
            static Dictionary<Type, FastReflectionDelegate> multipyDelegates = new();
            static Dictionary<Type, FastReflectionDelegate> divisionDelegates = new();


            public static bool SupportAndCreate(Type type)
            {
                if (additionDelegates.ContainsKey(type))
                    return true;

                bool hasComparable = false;
                foreach(var i in type.GetInterfaces())
                {
                    if(i == typeof(IComparable))
                    {
                        hasComparable = true;
                        break;
                    }
                }
                if (!hasComparable)
                    return false;

                int matched = 0;
                FastReflectionDelegate addition = null;
                FastReflectionDelegate subtraction = null;
                FastReflectionDelegate multipy = null;
                FastReflectionDelegate division = null;

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    Console.WriteLine(method.Name);
                    if (method.Name.Contains("op_Addition")){
                        matched++;
                        addition = method.CreateFastDelegate();
                    }
                    else if (method.Name.Contains("op_Subtraction"))
                    {
                        matched++;
                        subtraction = method.CreateFastDelegate();
                    }
                    else if (method.Name.Contains("op_Multiply"))
                    {
                        matched++;
                        multipy = method.CreateFastDelegate();
                    }
                    else if (method.Name.Contains("op_Division"))
                    {
                        matched++;
                        division = method.CreateFastDelegate();
                    }
                }
                if (matched == 4)
                {
                    additionDelegates.Add(type, addition);
                    subtractionDelegates.Add(type, subtraction);
                    multipyDelegates.Add(type, multipy);
                    divisionDelegates.Add(type, division);

                    return true;
                }
                else
                    return false;
            }
        
            public static object Addition(object a, object b)
            {
                return Addition(a.GetType(), a, b);
            }
            public static object Addition(Type type, object a, object b)
            {
                return additionDelegates[type].Invoke(null, a, b);
            }

            public static object Subtraction(object a, object b)
            {
                return Subtraction(a.GetType(), a, b);
            }
            public static object Subtraction(Type type, object a, object b)
            {
                return subtractionDelegates[type].Invoke(null, a, b);
            }

            public static object Multiply(object a, object b)
            {
                return Multiply(a.GetType(), a, b);
            }
            public static object Multiply(Type type, object a, object b)
            {
                return multipyDelegates[type].Invoke(null, a, b);
            }

            public static object Division(object a, object b)
            {
                return Division(a.GetType(), a, b);
            }
            public static object Division(Type type, object a, object b)
            {
                return divisionDelegates[type].Invoke(null, a, b);
            }

            public static bool Greater(object a, object b)
            {
                return (a as IComparable).CompareTo(b) > 0;
            }

            public static bool Smaller(object a, object b)
            {
                return (a as IComparable).CompareTo(b) < 0;
            }

            public static bool Equal(object a, object b)
            {
                return (a as IComparable).CompareTo(b) == 0;
            }

            public static bool GreaterOrEqual(object a, object b)
            {
                return Greater(a, b) || Equal(a, b);
            }

            public static bool SmallerOrEqual(object a, object b)
            {
                return Smaller(a, b) || Equal(a, b);
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

        public static T GetUninit<T>(Type type)
        {
            return (T)FormatterServices.GetSafeUninitializedObject(type);
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
