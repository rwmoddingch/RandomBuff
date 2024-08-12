using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuiltinBuffs.Positive
{
    internal class MoneyWorshippersBuffEntry : IBuffEntry
    {
        public static BuffID moneyWorshipperBuffID = new BuffID("MoneyWorshippers", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<MoneyWorshippersBuffEntry>(moneyWorshipperBuffID);
        }

        public static void HookOn()
        {
            On.Scavenger.PlayerHasImmunity += Scavenger_PlayerHasImmunity;
        }

        private static bool Scavenger_PlayerHasImmunity(On.Scavenger.orig_PlayerHasImmunity orig, Scavenger self, Player player)
        {
            var result = orig.Invoke(self, player);

            if(player.grasps != null)
            {
                foreach(var grasp in player.grasps)
                {
                    if (grasp == null)
                        continue;
                    if (grasp.grabbed.abstractPhysicalObject is DataPearl.AbstractDataPearl)
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }
    }
}
