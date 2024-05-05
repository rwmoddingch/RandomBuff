using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuiltinBuffs.Positive
{
    internal class SuperCapacitorBuffEntry : IBuffEntry
    {
        public static BuffID superCapacitorBuffID = new BuffID("SuperCapacitor", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SuperCapacitorBuffEntry>(superCapacitorBuffID);
        }

        public static void HookOn()
        {

        }
    }
}
