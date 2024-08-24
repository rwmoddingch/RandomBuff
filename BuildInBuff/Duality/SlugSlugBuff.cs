using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MoreSlugcats;
using RWCustom;
using MonoMod.RuntimeDetour;
using System.Reflection;
using RandomBuffUtils;

namespace BuiltinBuffs.Duality
{
    internal class SlugSlugBuff : Buff<SlugSlugBuff, SlugSlugBuffData>
    {
        public override BuffID ID => SlugSlugBuffEntry.SlugSlugID;

        public override void Destroy()
        {
            base.Destroy();
            PlayerUtils.UndoAll(this);
        }
    }

    class SlugSlugBuffData : CountableBuffData
    {
        public override BuffID ID => SlugSlugBuffEntry.SlugSlugID;

        public override int MaxCycleCount => 3;
    }


    class SlugSlugBuffEntry : IBuffEntry
    {
        public static BuffID SlugSlugID = new BuffID("SlugSlug", true);
        public static ConditionalWeakTable<Player, SlugSlugModule> slugSlugModule = new ConditionalWeakTable<Player, SlugSlugModule>();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SlugSlugBuff, SlugSlugBuffData, SlugSlugBuffEntry>(SlugSlugID);
        }

        public static void HookOn()
        {
            On.Creature.Grab += Creature_Grab;
            On.Creature.SuckedIntoShortCut += Creature_SuckedIntoShortCut;

            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        }

        public static bool CanEatEdible(PhysicalObject physicalObject)
        {
            if (!(physicalObject is IPlayerEdible)) return false;
            if (physicalObject is Hazer && !(physicalObject as Hazer).hasSprayed) return false;
            if (physicalObject is VultureGrub && !(physicalObject as VultureGrub).dead) return false;
            return true;
        }

        private static void Creature_SuckedIntoShortCut(On.Creature.orig_SuckedIntoShortCut orig, Creature self, IntVector2 entrancePos, bool carriedByOther)
        {
            if (self is Player && slugSlugModule.TryGetValue(self as Player, out var module))
            {
                module.ReleaseGrasp();
            }
            orig(self, entrancePos, carriedByOther);
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (!slugSlugModule.TryGetValue(self, out var slugslug) && self.slugcatStats.name != MoreSlugcatsEnums.SlugcatStatsName.Spear)
            {
                slugSlugModule.Add(self, new SlugSlugModule(self));
            }

            if (slugSlugModule.TryGetValue(self, out var module))
            {
                if (module.mouthGrasp != null)
                {
                    if (!self.Consious || module.mouthGrasp.grabbed.slatedForDeletetion || self.room == null)
                    {
                        module.ReleaseGrasp();
                        return;
                    }
                    
                    if (module.mouthGrasp.grabbed is Player && !self.slugOnBack.HasASlug)
                    {
                        var slug = module.mouthGrasp.grabbed as Player;
                        bool canPickUp = false;
                        if (!slug.dead)
                        {
                            canPickUp = true;
                        }
                        else
                        {
                            if (!self.CanEatMeat(slug) || self.FoodInStomach >= self.MaxFoodInStomach)
                            {
                                canPickUp = true;
                            }
                        }

                        if (canPickUp)
                        {
                            slug.slugOnBack.DropSlug();
                            self.slugOnBack.SlugToBack(slug);
                            module.ReleaseGrasp();
                        }
                    }

                    module.grabCounter++;

                    if ((module.mouthGrasp.grabbed is IPlayerEdible && (!(module.mouthGrasp.grabbed is Centipede) || (module.mouthGrasp.grabbed as Centipede).Small)) || module.mouthGrasp.grabbed is WaterNut)
                    {
                        if (module.grabCounter > 40)
                        {
                            module.grabCounter = 0;

                            if (self.FoodInStomach < self.MaxFoodInStomach && CanEatEdible(module.mouthGrasp.grabbed))
                            {
                                for (int i = 0; i < 3; i++)
                                {
                                    self.room.AddObject(new WaterDrip(self.mainBodyChunk.pos, 5f * Custom.RNV(), false));
                                }
                                module.BiteEdibleObject(eu);                               
                            }
                            else
                            {
                                //(module.mouthGrasp.grabbed as WaterNut).AbstrNut.Consume();
                                //(module.mouthGrasp.grabbed as WaterNut).stalk = null;
                               module.ReleaseGrasp();
                            }
                        }
                    }
                    else
                    {
                        module.UpdateHeavyCarry();
                        module.BiteFood(eu);
                        self.room.AddObject(new WaterDrip(self.mainBodyChunk.pos, 6f * Custom.RNV(), false));
                        if (module.grabCounter > 40)
                        {
                            if ((module.mouthGrasp.grabbed as Creature).State != null)
                            {
                                if ((module.mouthGrasp.grabbed as Creature).State.meatLeft == 0) module.ReleaseGrasp();
                                if ((module.mouthGrasp.grabbed as Creature).State.meatLeft > 0 && self.FoodInStomach < self.MaxFoodInStomach)
                                {
                                    (module.mouthGrasp.grabbed as Creature).State.meatLeft--;
                                    if (ModManager.MSC && self.slugcatStats.name == MoreSlugcatsEnums.SlugcatStatsName.Saint)
                                    {
                                        module.ReleaseGrasp();
                                        self.Stun(60);
                                    }
                                    else
                                    {                                        
                                        if (ModManager.MSC && (self.slugcatStats.name == MoreSlugcatsEnums.SlugcatStatsName.Gourmand || self.slugcatStats.name == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
                                            && !(module.mouthGrasp.grabbed is Centipede))
                                        {
                                            self.AddQuarterFood();
                                            self.AddQuarterFood();
                                        }
                                        else
                                        {
                                            self.AddFood(1);
                                        }                                       
                                    }                                   
                                }
                                else
                                {
                                    module.ReleaseGrasp();
                                }                                
                            }
                            module.grabCounter = 0;
                        }
                    }
                }
            }
        }

        private static bool Creature_Grab(On.Creature.orig_Grab orig, Creature self, PhysicalObject obj, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool overrideEquallyDominant, bool pacifying)
        {
            if (self is Player)
            {
                if (slugSlugModule.TryGetValue(self as Player, out var module))
                {
                    bool flag = obj is Creature && (self as Player).CanEatMeat(obj as Creature);
                    bool flag2 = obj is IPlayerEdible || obj is WaterNut;
                    if (!(flag || flag2)) return false;

                    if (module.mouthGrasp != null) return false;
                    if (obj.slatedForDeletetion || obj is Creature && !(obj is Player) && !(obj as Creature).CanBeGrabbed(self))
                    {
                        return false;
                    }

                    UnityEngine.Debug.Log("Mouth Grab");

                    module.mouthGrasp = new Creature.Grasp(self, obj, 0, chunkGrabbed, shareability, dominance, pacifying);
                    obj.Grabbed(module.mouthGrasp);
                    module.grabChunkCollisionRad = module.mouthGrasp.grabbed.collisionRange;
                    obj.bodyChunks[chunkGrabbed].collideWithObjects = false;
                    obj.collisionRange = -200f;
                    new AbstractPhysicalObject.CreatureGripStick(self.abstractCreature, obj.abstractPhysicalObject, graspUsed, pacifying || obj.TotalMass < self.TotalMass);
                    return true;
                }
            }
            return orig(self, obj, graspUsed, chunkGrabbed, shareability, dominance, overrideEquallyDominant, pacifying);
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!slugSlugModule.TryGetValue(self, out var module) && self.slugcatStats.name != MoreSlugcatsEnums.SlugcatStatsName.Spear)
            {
                slugSlugModule.Add(self, new SlugSlugModule(self));
                self.slugcatStats.Modify(SlugSlugBuff.Instance,PlayerUtils.Multiply, "corridorClimbSpeedFac", 1.5f);
            }            
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);           
            if (slugSlugModule.TryGetValue(self.player, out var module))
            {
                for (int i = 5; i <= 8; i++)
                {
                    sLeaser.sprites[i].isVisible = false;
                }

                if (module.mouthGrasp != null)
                {
                    module.mouthGrasp.grabbed.firstChunk.HardSetPosition(sLeaser.sprites[9].GetPosition() + camPos);
                    module.mouthGrasp.grabbed.firstChunk.vel *= 0f;                    
                }
            }
        }


        public class SlugSlugModule
        {
            public Player self;
            public Creature.Grasp mouthGrasp;
            public float grabChunkCollisionRad;
            public int grabCounter;
            public SlugSlugModule(Player player)
            {
                self = player;
            }

            public void ReleaseGrasp()
            {
                try
                {                 
                    if (mouthGrasp != null)
                    {
                        mouthGrasp.grabbed.collisionRange = grabChunkCollisionRad;
                        mouthGrasp.grabbedChunk.collideWithObjects = true;
                    }                        
                    grabChunkCollisionRad = -1f;
                    mouthGrasp?.Release();
                    mouthGrasp = null;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }

            }

            public void BiteEdibleObject(bool eu)
            {
                try
                {
                    if (mouthGrasp != null)
                    {
                        if ((mouthGrasp.grabbed as IPlayerEdible).BitesLeft == 1 && self.SessionRecord != null)
                        {
                            self.SessionRecord.AddEat(mouthGrasp.grabbed);
                        }
                        if (mouthGrasp.grabbed is Creature)
                        {
                            (mouthGrasp.grabbed as Creature).SetKillTag(self.abstractCreature);
                        }

                        BiteFood(eu);
                        (mouthGrasp.grabbed as IPlayerEdible).BitByPlayer(mouthGrasp, eu);
                    }

                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            public void BiteFood(bool eu)
            {
                if (self.graphicsModule != null)
                {
                    (self.graphicsModule as PlayerGraphics).head.vel += ((self.graphicsModule as PlayerGraphics).legs.pos - (self.graphicsModule as PlayerGraphics).head.pos).normalized;
                    if ((self.graphicsModule as PlayerGraphics).blink < 5)
                        (self.graphicsModule as PlayerGraphics).blink = 5;
                }
            }

            public void UpdateHeavyCarry()
            {
                if (mouthGrasp == null) return;
                float rad = mouthGrasp.grabbedChunk.rad;
                if (!Custom.DistLess(self.mainBodyChunk.pos, mouthGrasp.grabbedChunk.pos, rad))
                {
                    float elastic = 0.5f;
                    Vector2 dir = (self.mainBodyChunk.pos - mouthGrasp.grabbedChunk.pos).normalized;
                    float dist = (self.mainBodyChunk.pos - mouthGrasp.grabbedChunk.pos).magnitude;
                    self.mainBodyChunk.pos -= dir * (dist - rad) * (self.TotalMass / (mouthGrasp.grabbed.TotalMass + mouthGrasp.grabbed.TotalMass));
                    self.mainBodyChunk.vel -= dir * (dist - rad) * (self.TotalMass / (mouthGrasp.grabbed.TotalMass + mouthGrasp.grabbed.TotalMass));
                    mouthGrasp.grabbedChunk.pos += dir * (dist - rad) * (self.TotalMass / (self.TotalMass + mouthGrasp.grabbed.TotalMass));
                    mouthGrasp.grabbedChunk.vel += dir * (dist - rad) * (self.TotalMass / (self.TotalMass + mouthGrasp.grabbed.TotalMass));
                }
            }
        }
    }
}
