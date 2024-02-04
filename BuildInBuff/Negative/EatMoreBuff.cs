using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Positive;
using HUD;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using UnityEngine;

namespace BuiltinBuffs.Negative
{
    internal class EatMoreBuff : Buff<EatMoreBuff, EatMoreBuffData>
    {
        public override BuffID ID => EatMoreIBuffEntry.eatMoreBuffID;

        public EatMoreBuff()
        {
            var game = Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;
            if (game?.session is StoryGameSession session)
            {
                session.characterStats.foodToHibernate += Data.StackLayer;
                session.characterStats.maxFood =
                    Mathf.Max(session.characterStats.foodToHibernate, session.characterStats.maxFood);
            }
        }
    }

    class EatMoreBuffData : BuffData
    {

        public override BuffID ID => EatMoreIBuffEntry.eatMoreBuffID;

    }

    class EatMoreIBuffEntry : IBuffEntry
    {
        public static BuffID eatMoreBuffID = new BuffID("EatMore", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<EatMoreBuff,EatMoreBuffData,EatMoreIBuffEntry>(eatMoreBuffID);
        }

        public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self,abstractCreature,world);
            if (world.game.session is StoryGameSession session)
            {
                self.slugcatStats.maxFood = session.characterStats.maxFood;
                self.slugcatStats.foodToHibernate = session.characterStats.foodToHibernate;
            }
        }
    }
}
