using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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

        public static float TimeSpeed => (Custom.rainWorld.processManager.currentMainLoop?.framesPerSecond ?? 40) / 40f;
        
    }
}
