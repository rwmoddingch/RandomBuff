using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Positive;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;

namespace BuiltinBuffs.Negative
{
    internal class HighPingIBuffEntry : IBuffEntry
    {
        public static BuffID highPingID = new BuffID("HighPing", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<HighPingBuff,HighPingBuffData,HighPingIBuffEntry>(highPingID);
        }

        public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
            On.Player.checkInput += Player_checkInput;
        }

        private static void Player_checkInput(On.Player.orig_checkInput orig, Player self)
        {
            orig(self);
            if(modules.TryGetValue(self,out var module))
                module.CheckInput(self);
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if(!modules.TryGetValue(self,out _) && !self.isNPC)
                modules.Add(self,new HighPingPlayerModule());
        }

        public static ConditionalWeakTable<Player, HighPingPlayerModule> modules = new ConditionalWeakTable<Player, HighPingPlayerModule>();
        public class HighPingPlayerModule
        {
            public Queue<Player.InputPackage> input = new Queue<Player.InputPackage>();


            public void CheckInput(Player player)
            {
                input.Enqueue(player.input[0]);
                if (input.Count < 20)
                    player.input[0] = new Player.InputPackage();
                else
                    player.input[0] = input.Dequeue();
            }
        }
    }

    class HighPingBuffData : BuffData
    {
        public override BuffID ID => HighPingIBuffEntry.highPingID;
    }

    class HighPingBuff : Buff<HighPingBuff,HighPingBuffData>
    {
        public override BuffID ID => HighPingIBuffEntry.highPingID;

        public HighPingBuff()
        {
            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
            {
                foreach (var player in game.Players.Where(i =>
                             i.state.alive && i.realizedCreature?.room != null))
                {
                    var rp = player.realizedCreature as Player;
                    if (HighPingIBuffEntry.modules.TryGetValue(rp, out _))
                        HighPingIBuffEntry.modules.Add(rp, new HighPingIBuffEntry.HighPingPlayerModule());
                }
            }
        }
    }
}
