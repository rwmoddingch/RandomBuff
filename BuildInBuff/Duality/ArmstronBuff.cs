using RandomBuff;
using RandomBuff;
using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RandomBuffUtils;
using UnityEngine;

namespace BuiltinBuffs.Duality
{
    internal class ArmstronBuff : Buff<ArmstronBuff, ArmstronBuffData>
    {
        public override BuffID ID => ArmstronIBuffEntry.ArmstronBuffID;
    }

    internal class ArmstronBuffData : BuffData
    {
        public override BuffID ID => ArmstronIBuffEntry.ArmstronBuffID;
    }

    internal class ArmstronIBuffEntry : IBuffEntry
    {
        public static BuffID ArmstronBuffID = new BuffID("Armstron", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ArmstronBuff, ArmstronBuffData, ArmstronIBuffEntry>(ArmstronBuffID);
            //On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
        }

        //private void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        //{
        //    orig(self, dt);
        //    if(Input.GetKeyDown(KeyCode.K) && self.rainWorld.BuffMode())
        //       typeof(BuffPoolManager).GetMethod("CreateBuff", BindingFlags.Instance | BindingFlags.NonPublic)
        //           .Invoke(BuffPoolManager.Instance,new object[] { ArmstronBuffID });

        //}

        public static void HookOn()
        {
            On.Room.ctor += Room_ctor;
            On.AntiGravity.Update += AntiGravity_Update;
            //BuffEvent.OnCreatureKilled += BuffEvent_OnCreatureKilled;
        }

        private static void BuffEvent_OnCreatureKilled(Creature creature, int playerNumber)
        {
            BuffUtils.Log(ArmstronBuffID,"killed creature");
        }

        private static void AntiGravity_Update(On.AntiGravity.orig_Update orig, AntiGravity self, bool eu)
        {
            orig.Invoke(self, eu);
            if (!self.active)
                return;
            self.room.gravity /= 6f;
        }

        private static void Room_ctor(On.Room.orig_ctor orig, Room self, RainWorldGame game, World world, AbstractRoom abstractRoom)
        {
            orig.Invoke(self, game, world, abstractRoom);
            if (game?.session is StoryGameSession storyGameSession)
            {
                if (storyGameSession.saveState.miscWorldSaveData.EverMetMoon)
                {
                    self.gravity /= 6f;
                }
            }
        }
    }
}
