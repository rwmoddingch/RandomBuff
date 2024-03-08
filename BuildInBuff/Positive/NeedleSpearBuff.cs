using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RWCustom;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;

namespace BuiltinBuffs.Positive
{


    internal class NeedleSpearIBuffEntry : IBuffEntry
    {
        public static BuffID NeedleSpearBuffID = new BuffID("NeedleSpear", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<NeedleSpearIBuffEntry>(NeedleSpearBuffID);
        }

        public static void HookOn()
        {
            On.Player.ThrowObject += Player_ThrowObject;
            On.BigNeedleWorm.Update += BigNeedleWorm_Update;
        }

        private static void BigNeedleWorm_Update(On.BigNeedleWorm.orig_Update orig, BigNeedleWorm self, bool eu)
        {

            if (self.grabbedBy.Count > 0 && self.grabbedBy[0] != null && self.grabbedBy[0].grabber is Player)
            {
                for (int i = 0; i < self.bodyChunks.Length; i++)
                {
                    float num3 = Mathf.InverseLerp(0f, (float)(self.bodyChunks.Length - 1), (float)i);
                    float t = Mathf.Lerp(0.6f, Mathf.Clamp01(Mathf.Sin(Mathf.Pow(num3, 0.5f) * 3.1415927f)), 0.5f + 0.5f * num3);
                    self.bodyChunks[i].mass = Mathf.Lerp(0.05f, 0.15f, t) * 0.01f;
                }
            }
            else
            {
                for (int i = 0; i < self.bodyChunks.Length; i++)
                {
                    float num3 = Mathf.InverseLerp(0f, (float)(self.bodyChunks.Length - 1), (float)i);
                    float t = Mathf.Lerp(0.6f, Mathf.Clamp01(Mathf.Sin(Mathf.Pow(num3, 0.5f) * 3.1415927f)), 0.5f + 0.5f * num3);
                    self.bodyChunks[i].mass = Mathf.Lerp(0.05f, 0.15f, t);
                }
            }
      
            orig.Invoke(self, eu);
        }

        private static void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
        {
            BigNeedleWorm worm = null;
            if (self.grasps[grasp]  != null && self.grasps[grasp].grabbed is BigNeedleWorm)
            {
                worm = self.grasps[grasp].grabbed as BigNeedleWorm;
            }

            orig.Invoke(self, grasp, eu);

            if(worm != null)
            {
                IntVector2 throwDir = new IntVector2(self.ThrowDirection, 0);
                bool flag = self.input[0].y < 0;
                if (ModManager.MMF && MMF.cfgUpwardsSpearThrow.Value)
                {
                    flag = (self.input[0].y != 0);
                }
                if (self.animation == Player.AnimationIndex.Flip && flag && self.input[0].x == 0)
                {
                    throwDir = new IntVector2(0, (ModManager.MMF && MMF.cfgUpwardsSpearThrow.Value) ? self.input[0].y : -1);
                }
                if (ModManager.MMF && self.bodyMode == Player.BodyModeIndex.ZeroG && MMF.cfgUpwardsSpearThrow.Value)
                {
                    int y = self.input[0].y;
                    if (y != 0)
                    {
                        throwDir = new IntVector2(0, y);
                    }
                    else
                    {
                        throwDir = new IntVector2(self.ThrowDirection, 0);
                    }
                }
                worm.swishCounter = 6;
                worm.swishDir = throwDir.ToVector2();
                worm.attackRefresh = true;
                worm.room.PlaySound(SoundID.Big_Needle_Worm_Attack, worm.mainBodyChunk.pos);
            }
        }
    }
}
