using MonoMod.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Mono.Cecil.Cil;

namespace BuiltinBuffs.Negative
{
    internal class PhotophobiaBuff : Buff<PhotophobiaBuff, PhotophobiaBuffData>
    {
        public override BuffID ID => PhotophobiaBuffEntry.Photophobia;
    }

    internal class PhotophobiaBuffData : BuffData
    {
        public override BuffID ID => PhotophobiaBuffEntry.Photophobia;
    }

    internal class PhotophobiaBuffEntry : IBuffEntry
    {
        public static BuffID Photophobia = new BuffID("Photophobia", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<PhotophobiaBuff, PhotophobiaBuffData, PhotophobiaBuffEntry>(Photophobia);
        }

        public static void HookOn()
        {
            IL.FlareBomb.Update += FlareBomb_UpdateIL;
        }

        private static void FlareBomb_UpdateIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(MoveType.After,
                    (i) => i.MatchCallvirt<Creature>("get_abstractCreature"),
                    (i) => i.MatchCallvirt<Creature>("SetKillTag"),
                    (i) => i.Match(OpCodes.Ldarg_0)))
                {
                    c.Emit(OpCodes.Ldloc_0);
                    c.EmitDelegate<Action<FlareBomb, int>>((self, i) =>
                    {
                        if (self.room.abstractRoom.creatures[i].realizedCreature is Player)
                        {
                            Player player = self.room.abstractRoom.creatures[i].realizedCreature as Player;
                            player.Die();
                            if (self.thrownBy != null)
                            {
                                player.SetKillTag(self.thrownBy.abstractCreature);
                            }
                        }
                    });
                    c.Emit(OpCodes.Ldarg_0);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }
    }
}
