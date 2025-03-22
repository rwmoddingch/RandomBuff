using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Menu.Remix.MixedUI;
using RWCustom;

namespace RandomBuffUtils
{
    public static class BuffCustom
    {
        public static bool TryGetGame(out RainWorldGame game)
        {
            game = Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;
            return game != null;
        }

        public static Type[] SafeGetTypes(this Assembly assembly)
        {
            Type[] types = null;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(i => i != null).ToArray();
            }
            return types;
        }
        public static void AddEvent(this UIelement owner, string eventName, string func)
        {
            var type = owner.GetType();
            EventInfo eInfo = type.GetEvent(eventName);
            eInfo.GetAddMethod().Invoke(owner,
                new[]
                {
                    Delegate.CreateDelegate(eInfo.EventHandlerType, owner,
                        type.GetMethod(func, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                });
        }
        public static void AddEvent(this UIelement owner, string eventName,object funcOwner, string func)
        {
            var type = owner.GetType();
            EventInfo eInfo = type.GetEvent(eventName);
            eInfo.GetAddMethod().Invoke(owner,
                new[]
                {
                    Delegate.CreateDelegate(eInfo.EventHandlerType, funcOwner,
                        funcOwner.GetType().GetMethod(func, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                });
        }
        public static float TimeSpeed => (Custom.rainWorld.processManager.currentMainLoop?.framesPerSecond ?? 40) / 40f;
        
    }
}
