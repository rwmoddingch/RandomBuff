﻿using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;

namespace BuiltinBuffs.Positive
{
    internal class HerbicideBuff : Buff<HerbicideBuff, HerbicideBuffData>
    {
        public override BuffID ID => HerbicideIBuffEntry.HerbicideBuffID;
    }

    internal class HerbicideBuffData : BuffData
    {
        public override BuffID ID => HerbicideIBuffEntry.HerbicideBuffID;
    }

    internal class HerbicideIBuffEntry : IBuffEntry
    {
        public static BuffID HerbicideBuffID = new BuffID("Herbicide", true);

        public  void OnEnable()
        {
            BuffRegister.RegisterBuff<HerbicideBuff, HerbicideBuffData, HerbicideIBuffEntry>(HerbicideBuffID);
        }

        public static void HookOn()
        {
            IL.Room.Loaded += Room_Loaded;
            IL.Room.LoadFromDataString += Room_LoadFromDataString;
        }

        private static void Room_LoadFromDataString(ILContext il)
        {
            if(!ApplySkip(il, (i) => i.Match(OpCodes.Ldloc_S)))
                BuffPlugin.Log("Room_LoadFromDataString hook failure");
        }

        private static void Room_Loaded(MonoMod.Cil.ILContext il)
        {
            if (!ApplySkip(il, (i) => i.MatchLdloc(1)))
                BuffPlugin.Log("Room_Loaded hook failure");
        }

        static bool ApplySkip(ILContext il, Func<Instruction, bool> midPredict)
        {
            ILCursor markCursor = new ILCursor(il);
            ILCursor emitCursor = new ILCursor(il);
            ILLabel label = null;

            Func<Instruction, bool>[] predicts = new Func<Instruction, bool>[]
            {
                (i) => i.MatchLdarg(0),
                (i) => i.MatchLdarg(0),
                midPredict,
                (i) => i.MatchNewobj<WormGrass>(),
                (i) => i.MatchCall<Room>("AddObject")
            };

            if (markCursor.TryGotoNext(MoveType.After,
                predicts
            ))
            {
                label = markCursor.MarkLabel();
            }
            else
                BuffPlugin.LogException(new NullReferenceException("Room_Loaded c1 cant mark"));

            if (emitCursor.TryGotoNext(MoveType.After,
                predicts
            ) && label != null)
            {
                if (emitCursor.TryGotoPrev(MoveType.After, (i) => i.Match(OpCodes.Ble_S)))
                {
                    emitCursor.Emit(OpCodes.Br, label);
                    return true;
                }
                else
                    BuffPlugin.LogException(new NullReferenceException( "Room_Loaded c2 cant emit"));
            }
            else
                BuffPlugin.LogException(new NullReferenceException($"Room_Loaded c2 cant find, {label != null}"));
            return false;
        }
    }
}