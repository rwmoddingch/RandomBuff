using MonoMod.Cil;
using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using RandomBuff;

namespace HotDogGains.Negative
{

    class StomachAcheBuff : Buff<StomachAcheBuff, StomachAcheBuffData> { public override BuffID ID => StomachAcheBuffEntry.StomachAcheID; }
    class StomachAcheBuffData : RandomBuff.Core.Buff.CountableBuffData
    {
        public override BuffID ID => StomachAcheBuffEntry.StomachAcheID;

        public override int MaxCycleCount => 5;

        //[JsonProperty]
        //public int cycleLeft;

    }
    class StomachAcheBuffEntry : IBuffEntry
    {
        public static BuffID StomachAcheID = new BuffID("StomachAcheID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<StomachAcheBuff, StomachAcheBuffData, StomachAcheBuffEntry>(StomachAcheID);
        }
        public static void HookOn()
        {
            On.Player.ObjectEaten += Player_ObjectEaten;//吃小东西会晕
            IL.Player.EatMeatUpdate += Player_EatMeatUpdateIL;//吃大东西会晕
        }

        private static void Player_EatMeatUpdateIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(MoveType.After,
                    (i) => i.MatchCall<Player>("AddFood"),
                    (i) => i.Match(OpCodes.Ldarg_0)))
                {
                    c.EmitDelegate<Action<Player>>((self) =>
                    {
                        self.Stun(20*StomachAcheID.GetBuffData().StackLayer);
                    });
                    c.Emit(OpCodes.Ldarg_0);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        private static void Player_ObjectEaten(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
        {
            StomachAcheBuff.Instance.TriggerSelf(true);
            self.Stun(80*StomachAcheID.GetBuffData().StackLayer);
            orig.Invoke(self, edible);
        }

    }
}
