    using System;
using System.Collections.Generic;
using System.Linq;
    using System.Net.WebSockets;
    using System.Text;
using System.Threading.Tasks;
    using Rewired;
    using UnityEngine;
using static System.Net.Mime.MediaTypeNames;
using static RandomBuffUtils.BuffInput;

namespace RandomBuffUtils
{
    public class BuffInput
    {
        /// <summary>
        /// 获取按键输入
        /// </summary>
        /// <param name="action">通过OnAnyKeyDown获取的keyCode值</param>
        /// <returns></returns>
        public static bool GetKeyDown(string action)
        {
            if (action.StartsWith("Axis"))
            {
                action = action.Replace("Axis ", "");
                if (ReInput.controllers?.Joysticks == null)
                    return false;
                return ReInput.controllers.Joysticks.Any(col => col.GetAxisTimeActiveById(action[0] - '0') != 0 &&
                                                                col.GetAxisLastTimeActiveById(action[0] - '0') == 0);
            }
            return Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode), action));
        }


        /// <summary>
        /// 获取按键输入
        /// </summary>
        /// <param name="action">通过OnAnyKeyDown获取的keyCode值</param>
        /// <returns></returns>
        public static bool GetKey(string action)
        {
            if (action.StartsWith("Axis"))
            {
                action = action.Replace("Axis ", "");
                if (ReInput.controllers?.Joysticks == null)
                    return false;
                return ReInput.controllers.Joysticks.Any(col => col.GetAxisTimeActiveById(action[0] - '0') != 0);
            }
            return Input.GetKey((KeyCode)Enum.Parse(typeof(KeyCode), action));
        }

        public delegate void KeyDownHandler(string keyDown);
        
        public static event KeyDownHandler OnAnyKeyDown
        {
            add
            {
                if (Listeners.Count == 0)
                    On.ProcessManager.Update -= ProcessManager_Update;
                
                if(!Listeners.ContainsKey(value))
                    Listeners.Add(value,new BuffInputListener(value));
            }
            remove
            {
                if (Listeners.ContainsKey(value))
                {
                    Listeners.Remove(value);
                    if (Listeners.Count == 0)
                        On.ProcessManager.Update -= ProcessManager_Update;
                }
            }
        }

        private static void ProcessManager_Update(On.ProcessManager.orig_Update orig, ProcessManager self, float deltaTime)
        {
            orig(self,deltaTime);
            foreach(var listener in Listeners)
                listener.Value.ListenInput();
        }

        static readonly Dictionary<KeyDownHandler,BuffInputListener> Listeners = new Dictionary<KeyDownHandler,BuffInputListener>();
    }


    internal class BuffInputListener
    {
        internal BuffInputListener(KeyDownHandler handler)
        {
            keyDownHandler = handler;
        }
        public void ListenInput()
        {
            foreach (int code in Enum.GetValues(typeof(KeyCode)))
            {
                string name = ((KeyCode)(code)).ToString();
                if (Input.GetKey((KeyCode)code))
                {
                    if (!alreadyDown.Contains(name))
                    {
                        alreadyDown.Add(name);
                        keyDownHandler?.Invoke(name);
                    }
                }
                else
                {
                    if (alreadyDown.Contains(name))
                        alreadyDown.Remove(name);
                }
            }
            foreach (var col in ReInput.controllers.Joysticks)
            {
                foreach (var axis in col.Axes)
                {
                    string name = "Axis " + axis.id;
                    if (axis.timeActive != 0)
                    {
                        if (!alreadyDown.Contains(name))
                        {
                            alreadyDown.Add(name);
                            keyDownHandler?.Invoke(name);
                        }
                    }
                    else
                    {
                        if (alreadyDown.Contains(name))
                            alreadyDown.Remove(name);
                    }
                }
            }
        }

        private readonly HashSet<string> alreadyDown = new HashSet<string>();

        private readonly KeyDownHandler keyDownHandler;
    }
}
