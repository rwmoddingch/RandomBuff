
using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;

namespace BuiltinBuffs.Negative
{
    internal class FoodLoverBuff : Buff<FoodLoverBuff, FoodLoverBuffData>
    {
        public override BuffID ID => FoodLoverIBuffEntry.FoodLoverBuffID;
    }

    internal class FoodLoverBuffData : BuffData
    {
        public override BuffID ID => FoodLoverIBuffEntry.FoodLoverBuffID;
    }

    internal class FoodLoverIBuffEntry : IBuffEntry
    {
        public static BuffID FoodLoverBuffID = new BuffID("FoodLover", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<FoodLoverBuff, FoodLoverBuffData, FoodLoverIBuffEntry>(FoodLoverBuffID);
        }

        public static void HookOn()
        {
            On.Player.CanIPickThisUp += Player_CanIPickThisUp;
            On.Player.Grabability += Player_Grabability;
        }

        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            var result = orig.Invoke(self, obj);
            if (!(obj is IPlayerEdible))
                result = Player.ObjectGrabability.CantGrab;
            return result;
        }

        private static bool Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
        {
            bool result = orig.Invoke(self, obj);
            if (obj is IPlayerEdible)
                return result;
            return false;
        }
    }
}
