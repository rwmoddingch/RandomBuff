using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
