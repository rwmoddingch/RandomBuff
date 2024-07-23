using System;
using MonoMod;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Runtime.CompilerServices;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;

namespace HotDogBuff
{
    class NeurodegenerativeBuff : Buff<NeurodegenerativeBuff, NeurodegenerativeBuffData> { public override BuffID ID => NeurodegenerativeBuffEntry.NeurodegenerativeID; }
    class NeurodegenerativeBuffData : BuffData { public override BuffID ID => NeurodegenerativeBuffEntry.NeurodegenerativeID; }
    class NeurodegenerativeBuffEntry : IBuffEntry
    {
        public static BuffID NeurodegenerativeID = new BuffID("NeurodegenerativeID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<NeurodegenerativeBuff, NeurodegenerativeBuffData, NeurodegenerativeBuffEntry>(NeurodegenerativeID);
        }
        public static ConditionalWeakTable<Player, MyLastRoom> module = new ConditionalWeakTable<Player, MyLastRoom>();
        public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
            On.Player.UpdateMSC += Player_UpdateMSC;
        }

        private static void Player_UpdateMSC(On.Player.orig_UpdateMSC orig, Player self)
        {
            orig.Invoke(self);
            if (!module.TryGetValue(self, out var myLastRoom))
            {
                module.Add(self, new MyLastRoom());
                return;
            }
            if (myLastRoom.lastRoom != self.room)
            {
                myLastRoom.cd = myLastRoom.cdMax;
            }
            if (myLastRoom.cd > 0)
            {
                self.input[0].x = 0;
                self.input[0].y = 0;
                self.input[0].jmp = false;
                self.input[0].pckp = false;
                self.input[0].thrw = false;
                myLastRoom.cd--;
            }
            myLastRoom.lastRoom = self.room;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig.Invoke(self, abstractCreature, world);
            module.Add(self, new MyLastRoom());
        }
    }    
    internal class MyLastRoom
    {
        public Room lastRoom;
        public int cdMax = 100;
        public int cd = 0;
        public MyLastRoom() { }
    }
}
