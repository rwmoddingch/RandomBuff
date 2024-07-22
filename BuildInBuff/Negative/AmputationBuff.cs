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

namespace BuiltinBuffs.Negative
{
    internal class AmputationBuff : Buff<AmputationBuff, AmputationBuffData>
    {
        public override BuffID ID => AmputationBuffEntry.Amputation;

        public AmputationBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    var amputation = new Amputation(player);
                    AmputationBuffEntry.AmputationFeatures.Add(player, amputation);
                }
            }
        }
    }

    internal class AmputationBuffData : CountableBuffData
    {
        public override BuffID ID => AmputationBuffEntry.Amputation;
        public override int MaxCycleCount => 3;
    }

    internal class AmputationBuffEntry : IBuffEntry
    {
        public static BuffID Amputation = new BuffID("Amputation", true);

        public static ConditionalWeakTable<Player, Amputation> AmputationFeatures = new ConditionalWeakTable<Player, Amputation>();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<AmputationBuff, AmputationBuffData, AmputationBuffEntry>(Amputation);
        }

        public static void HookOn()
        {
            On.Player.Update += Player_Update;
            On.Player.Grabability += Player_Grabability;
            On.Player.FreeHand += Player_FreeHand;

            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        }
        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (AmputationFeatures.TryGetValue(self, out var amputation))
            {
                amputation.Update();
            }
        }

        //只能一次拿一个东西
        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            Player.ObjectGrabability result = orig(self, obj);

            if (AmputationFeatures.TryGetValue(self, out var amputation))
            {
                result = amputation.Grabability(result);
            }

            return result;
        }

        private static int Player_FreeHand(On.Player.orig_FreeHand orig, Player self)
        {
            int result = orig(self);
            if (AmputationFeatures.TryGetValue(self, out var amputation))
                if (self.grasps[0] != null || self.grasps[1] != null)
                {
                    result = -1;
                }
            return result;
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (AmputationFeatures.TryGetValue(self.player, out var amputation))
                amputation.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }
    }

    public class Amputation
    {
        WeakReference<Player> ownerRef;

        public Amputation(Player player)
        {
            ownerRef = new WeakReference<Player>(player);
        }

        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return; 
            if (player.grasps[0] != null && player.grasps[1] != null)
            {
                player.ReleaseGrasp(1);
            }
        }

        //只能一次拿一个东西
        public Player.ObjectGrabability Grabability(Player.ObjectGrabability result)
        {
            if (!ownerRef.TryGetTarget(out var self))
                return result;

            if (result == Player.ObjectGrabability.OneHand)
                result = Player.ObjectGrabability.BigOneHand;
            else if (result == Player.ObjectGrabability.BigOneHand)
                result = Player.ObjectGrabability.BigOneHand;
            else if (result == Player.ObjectGrabability.TwoHands)
                result = Player.ObjectGrabability.CantGrab;
            else if (result == Player.ObjectGrabability.Drag)
                result = Player.ObjectGrabability.CantGrab;

            return result;
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (!ownerRef.TryGetTarget(out var player) || player.graphicsModule == null || sLeaser == null)
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;

            if (sLeaser.sprites.Length >= 9)
                for (int i = 5; i <= 8; i++)
                    if (i == 6 || i == 8)
                        sLeaser.sprites[i].isVisible = false;
        }
    }
}
