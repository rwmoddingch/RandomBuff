using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;

namespace BuiltinBuffs.Duality
{
  
    internal class StoneofBlessingIBuffEntry : IBuffEntry
    {
        public static BuffID StoneofBlessingBuffID = new BuffID("StoneofBlessing", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<StoneofBlessingIBuffEntry>(StoneofBlessingBuffID);
        }

        public static void HookOn()
        {
            On.Creature.Violence += Creature_Violence;
        }

        private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, UnityEngine.Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if(!(self is Player) && self.room != null && self.abstractCreature != null)
            {
                foreach (var player in self.room.game.Players)
                {
                    if (player.Room == self.abstractCreature.Room)
                    {
                        damage /= 2f;
                        break;
                    }
                }
            }
            orig.Invoke(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }
    }
}
