
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HotDogBuff.Negative
{

    class ByeByeWeaponBuff : Buff<ByeByeWeaponBuff, ByeByeWeaponBuffData>{public override BuffID  ID => ByeByeWeaponBuffEntry.ByeByeWeaponID;}

    class ByeByeWeaponBuffData :BuffData{public override BuffID ID => ByeByeWeaponBuffEntry.ByeByeWeaponID;}

    class ByeByeWeaponBuffEntry : IBuffEntry
    {
        public static BuffID ByeByeWeaponID = new BuffID("ByeByeWeaponID", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ByeByeWeaponBuff,ByeByeWeaponBuffData,ByeByeWeaponBuffEntry>(ByeByeWeaponID);
        }
        public static void HookOn()
        {
            On.Player.ThrowObject += Player_ThrowObject;
        }

        private static void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
        {
            if (self.grasps[grasp].grabbed is Weapon weapon)
            {
                self.TossObject(grasp, eu);
                weapon.SetRandomSpin(); 
                self.ReleaseGrasp(grasp);
            }
            else
                orig.Invoke(self, grasp, eu);

        }

        
    }
}
