using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;

namespace BuiltinBuffs.Negative
{
    internal class PoorAimBuff : Buff<PoorAimBuff, PoorAimBuffData>
    {
        public override BuffID ID => PoorAimBuffEntry.PoorAimID;
    }

    class PoorAimBuffData : CountableBuffData
    {
        public override BuffID ID => PoorAimBuffEntry.PoorAimID;

        public override int MaxCycleCount => 3;
    }

    class PoorAimBuffEntry : IBuffEntry
    {
        public static BuffID PoorAimID = new BuffID("PoorAim", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<PoorAimBuff, PoorAimBuffData, PoorAimBuffEntry>(PoorAimID);
        }

        public static void HookOn()
        {
            On.Weapon.Thrown += Weapon_Thrown;
            On.Spear.Update += Spear_Update;
        }

        private static void Spear_Update(On.Spear.orig_Update orig, Spear self, bool eu)
        {
            orig(self, eu);
            if (self.mode == Weapon.Mode.Thrown)
            {
                self.setRotation = self.firstChunk.vel;
            }
        }

        private static void Weapon_Thrown(On.Weapon.orig_Thrown orig, Weapon self, Creature thrownBy, UnityEngine.Vector2 thrownPos, UnityEngine.Vector2? firstFrameTraceFromPos, RWCustom.IntVector2 throwDir, float frc, bool eu)
        {
            orig(self, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
            float num = UnityEngine.Random.Range(-60f, 60f);
            float num2 = Mathf.Tan(num / 180f * Mathf.PI);
            if (throwDir.x != 0)
            {
                self.firstChunk.vel.y += self.firstChunk.vel.x * num2;
            }
            else
            {
                self.firstChunk.vel.x += self.firstChunk.vel.y * num2;
            }
            self.setRotation = throwDir.ToVector2() + num2 * Custom.PerpendicularVector(throwDir.ToVector2());
        }
    }
}
