using System;
using MonoMod;
using UnityEngine;
using Random = UnityEngine.Random;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff;
using RandomBuffUtils;
using BuildInBuff.Positive;
using System.Runtime.CompilerServices;

namespace TemplateGains
{
    public class LittleCatData
    {
        public bool changed = false;
    }
    public static class LittleCat
    {
        private static readonly ConditionalWeakTable<Player, LittleCatData> modules = new ConditionalWeakTable<Player, LittleCatData>();
        public static LittleCatData littleCat(this Player player)
        {
            return modules.GetValue(player, (Player p) => new LittleCatData());
        }
    }
    class ImLittleCatBuff : Buff<ImLittleCatBuff, ImLittleCatBuffData>
    {
        public override void Destroy()
        {
            if(BuffCustom.TryGetGame(out var game)&&game!=null)
            {
                foreach (var absPlayer in game.Players)
                {
                    Player player = absPlayer.realizedCreature as Player;
                    if (player!=null &&player.littleCat().changed)
                    {
                        player.setPupStatus(false);
                        player.playerState.isPup = false;
                        player.littleCat().changed = false;
                    }

                }
            }
            base.Destroy();
        }
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
                self.littleCat().changed= true;
                self.playerState.isPup = true;
            }
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig.Invoke(self, abstractCreature, world);
            self.setPupStatus(true);
            self.littleCat().changed = true;
            self.playerState.isPup = true;
        }
    }
}
