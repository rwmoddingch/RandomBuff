using Mono.Cecil.Cil;
using MonoMod.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace HotDogGains.Duality
{
    class FeatherfallBuff : Buff<FeatherfallBuff, FeatherfallBuffData> { public override BuffID ID => FeatherfallBuffEntry.FeatherfallID; }
    class FeatherfallBuffData : CountableBuffData
    {
        public override BuffID ID => FeatherfallBuffEntry.FeatherfallID;

        public override int MaxCycleCount => 3;
    }
    class FeatherfallBuffEntry : IBuffEntry
    {
        public static BuffID FeatherfallID = new BuffID("FeatherfallID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<FeatherfallBuff, FeatherfallBuffData, FeatherfallBuffEntry>(FeatherfallID);
        }
        public static void HookOn()
        {
            //On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
            On.Player.Update += Player_Update;

            IL.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate1;
        }

        private static void RainWorldGame_RawUpdate1(MonoMod.Cil.ILContext il)
        {
            var c = new ILCursor(il);

            //修改每口的间隔
            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdcI4(40)
                ))
            {
                c.EmitDelegate<Func<int, int>>((number) =>
                {
                    return (int)stagnant;

                    return number;
                });

            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (!self.dead && self.bodyChunks[1].contactPoint.y >= 0 && self.bodyChunks[0].contactPoint.y >= 0 && self.bodyMode != Player.BodyModeIndex.WallClimb && self.bodyMode != Player.BodyModeIndex.Swimming && self.bodyMode != Player.BodyModeIndex.ClimbingOnBeam &&
                self.bodyMode != Player.BodyModeIndex.ClimbIntoShortCut && self.bodyMode != Player.BodyModeIndex.CorridorClimb&&self.mainBodyChunk.vel.y<0)
            {
                //if (!stagnant)FeatherfallBuff.Instance.TriggerSelf(true);//弹出卡牌使用提示
                stagnant = Custom.LerpAndTick(40,20,stagnant,0.2f);
            }
            else stagnant = Custom.LerpAndTick(20, 40, stagnant, 0.2f);
        }
        public static float stagnant = 40;







    }
}