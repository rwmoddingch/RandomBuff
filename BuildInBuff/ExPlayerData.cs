using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BuiltinBuffs
{
    public static class ExPlayer
    {
        private static readonly ConditionalWeakTable<Player, ExPlayerData> modules =new ConditionalWeakTable<Player, ExPlayerData> ();

        public static ExPlayerData GetExPlayerData(this Player player)
        {
            return modules.GetValue(player, (Player p) => new ExPlayerData(p));
        }

    }
    public class ExPlayerData
    {
        public Player player;

        public bool HaveTail = true;


        public ExPlayerData(Player player)
        {
            this.player= player;
        }
    }
}
