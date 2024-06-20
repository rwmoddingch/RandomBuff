using System;
using MonoMod;
using UnityEngine;
using Random = UnityEngine.Random;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff;
using RandomBuffUtils;

namespace TemplateGains
{
    class ImLittleCatBuff : Buff<ImLittleCatBuff, ImLittleCatBuffData>
    {
        public override BuffID ID => ImLittleCatBuffEntry.ImLittleCatID;
    }
    class ImLittleCatBuffData : BuffData
    {
        public override BuffID ID => ImLittleCatBuffEntry.ImLittleCatID;
    }
    class ImLittleCatBuffEntry : IBuffEntry
    {
        public static BuffID ImLittleCatID = new BuffID("ImLittleCatID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ImLittleCatBuff, ImLittleCatBuffData, ImLittleCatBuffEntry>(ImLittleCatID);
        }
        public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (!self.playerState.isPup)
            {
                self.setPupStatus(true);
                self.playerState.isPup = true;
            }
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig.Invoke(self, abstractCreature, world);
            self.setPupStatus(true);
            self.playerState.isPup = true;
        }
    }
}
