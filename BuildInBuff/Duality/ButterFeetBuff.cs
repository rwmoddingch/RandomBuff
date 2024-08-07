using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RandomBuff.Core;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System.Runtime.CompilerServices;
using RandomBuffUtils;

namespace BuiltinBuffs.Duality
{
    internal class ButterFeetBuff : Buff<ButterFeetBuff, ButterFeetBuffData>
    {
        public override BuffID ID => ButterFeetBuffEntry.ButterFeet;

        public override void Destroy()
        {
            base.Destroy();
            PlayerUtils.UndoAll(this);
        }
    }

    class ButterFeetBuffData : CountableBuffData
    {
        public override BuffID ID => ButterFeetBuffEntry.ButterFeet;
        public override int MaxCycleCount => 3;
    }

    class ButterFeetBuffEntry : IBuffEntry
    {
        public static BuffID ButterFeet = new BuffID("ButterFeet", true);
        public static ConditionalWeakTable<Player, ButterSpeedModule> butterModule = new ConditionalWeakTable<Player, ButterSpeedModule> ();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ButterFeetBuff, ButterFeetBuffData, ButterFeetBuffEntry>(ButterFeet);
        }

        public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.Player.TerrainImpact += Player_TerrainImpact;
        }

        private static void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player self, int chunk, RWCustom.IntVector2 direction, float speed, bool firstContact)
        {
            orig(self, chunk, direction, speed, firstContact);
            if (butterModule.TryGetValue(self, out var module))
            {
                if (direction.x != 0 && direction.x * module.butterVel.x > 0)
                {
                    module.butterVel.x = 0;
                }
            }
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            butterModule.Add(self, new ButterSpeedModule());

            self.slugcatStats.Modify(ButterFeetBuff.Instance, PlayerUtils.Multiply, "runspeedFac", 1.5f);
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            float lastCanJump = self.canJump;
            orig(self, eu);
            if (butterModule.TryGetValue(self, out var module))
            {
                Vector2 currentVel = 0.5f * (self.firstChunk.vel + self.bodyChunks[1].vel);

                if ((self.bodyMode == Player.BodyModeIndex.ZeroG || self.bodyMode == Player.BodyModeIndex.Swimming || self.bodyMode == Player.BodyModeIndex.Stunned || self.bodyMode == Player.BodyModeIndex.CorridorClimb ||
                    self.bodyMode == Player.BodyModeIndex.WallClimb || self.animation == Player.AnimationIndex.AntlerClimb || self.animation == Player.AnimationIndex.VineGrab 
                    || self.animation == Player.AnimationIndex.LedgeCrawl || self.animation == Player.AnimationIndex.LedgeGrab))
                {
                    module.butterVel *= 0f;
                    return;
                }

                if (self.animation == Player.AnimationIndex.ClimbOnBeam)
                {
                    module.butterVel *= 0f;
                }

                module.butterVel *= self.bodyMode == Player.BodyModeIndex.Crawl ? 0.96f : 0.99f;
                if ((module.butterVel.magnitude < currentVel.magnitude && module.butterVel.x * currentVel.x > 0)||(lastCanJump <= 0 && self.canJump > 0))
                {
                    module.butterVel = currentVel;
                }

                if (self.canJump > 0 || self.wantToJump <= 0)
                {                                                            
                    if(currentVel.x * module.butterVel.x >= 0)
                    {
                        if (Mathf.Abs(currentVel.x) < Mathf.Abs(module.butterVel.x))
                        {
                            self.firstChunk.vel += new Vector2(module.butterVel.x - currentVel.x, 0);
                            self.bodyChunks[1].vel += new Vector2(module.butterVel.x - currentVel.x, 0);
                        }
                        
                    }                    

                    if (module.butterVel.x * self.input[0].x <= 0)
                    {
                        module.butterVel += 0.2f * new Vector2(self.input[0].x, 0);
                    }
                    
                }
                /*
                else if(self.bodyMode != Player.BodyModeIndex.CorridorClimb)
                {
                    self.firstChunk.vel += new Vector2(module.butterVel.x - currentVel.x, 0);
                    self.bodyChunks[1].vel += new Vector2(module.butterVel.x - currentVel.x, 0);
                }
                */
            }
        }

    }

    public class ButterSpeedModule
    {
        public Vector2 butterVel;
    }
}
