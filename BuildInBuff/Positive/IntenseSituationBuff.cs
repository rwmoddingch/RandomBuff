using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;

namespace BuiltinBuffs.Positive
{
    internal class IntenseSituationBuffData : BuffData
    {
        public override BuffID ID => IntenseSituationBuffEntry.IntenseSituation;
    }

    internal class IntenseSituationBuff : Buff<IntenseSituationBuff,IntenseSituationBuffData>
    {
        public override BuffID ID => IntenseSituationBuffEntry.IntenseSituation;

        public override void Destroy()
        {
            base.Destroy();
            PlayerUtils.UndoAll(this);
        }

        private ConditionalWeakTable<Player, List<PlayerUtils.SlugcatStatModifer>> maps =
            new ConditionalWeakTable<Player, List<PlayerUtils.SlugcatStatModifer>>();

        public void Modify(Player player, float value)
        {
            if (!maps.TryGetValue(player, out var list))
            {
                maps.Add(player,list = new List<PlayerUtils.SlugcatStatModifer>());
                list.Add(player.slugcatStats.Modify(this, PlayerUtils.Multiply, "runspeedFac",value));
                list.Add(player.slugcatStats.Modify(this, PlayerUtils.Multiply, "poleClimbSpeedFac", value));
                list.Add(player.slugcatStats.Modify(this, PlayerUtils.Multiply, "corridorClimbSpeedFac", value));
            }
            else
            {

                foreach (var item in list)
                {
                    if(Mathf.Abs(item.ExecValue - value) > 0.05f)
                        item.ExecValue = value;
                }
            }
        }
    }

    internal class IntenseSituationBuffEntry : IBuffEntry
    {
        public static readonly BuffID IntenseSituation = new BuffID(nameof(IntenseSituation), true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<IntenseSituationBuff, IntenseSituationBuffData, IntenseSituationBuffEntry>(IntenseSituation);
        }

        public static void HookOn()
        {
            On.Player.ThrownSpear += Player_ThrownSpear;
            On.Player.ThrowObject += Player_ThrowObject;

            On.Player.Update += Player_Update;
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self.room == null || self.inShortcut)
                return;

            IntenseSituationBuff.Instance.Modify(self, Mathf.Lerp(1, 2f, PlayerDangerBonus(self)));
        }

        private static void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
        {
            orig(self,grasp, eu);
            self.dontGrabStuff = Mathf.RoundToInt(self.dontGrabStuff * Mathf.Lerp(1,0.25f, PlayerDangerBonus(self)));
        }

        private static void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig(self, spear);
            spear.spearDamageBonus *= Mathf.Lerp(1, 3, PlayerDangerBonus(self));
        }

        private static float PlayerDangerBonus(Player player)
        {
            if (player.room == null || player.inShortcut)
                return 0;
            float re = 0;
            foreach (var crit in player.room.updateList.OfType<Creature>())
            {
                if(crit is Player) continue;
                if (crit.abstractCreature.abstractAI?.RealAI.friendTracker is FriendTracker tracker &&
                    tracker.friend == player)
                    continue;
                
                re = Mathf.Max(re, Custom.LerpMap(crit.bodyChunks.Min(i => Custom.Dist(i.pos, player.DangerPos)), 75,
                    500, 1,
                    0) * crit.Template.dangerousToPlayer * 2);

            }

            return re;
        }
    }
}
