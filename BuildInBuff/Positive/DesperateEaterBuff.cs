using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using MoreSlugcats;
using UnityEngine;

namespace BuiltinBuffs.Positive
{
    internal class DesperateEaterBuff : Buff<DesperateEaterBuff, DesperateEaterBuffData>
    {
        public override BuffID ID => DesperateEaterBuffEntry.DesperateEater;
    }

    class DesperateEaterBuffData : CountableBuffData
    {
        public override int MaxCycleCount => 10;
        public override BuffID ID => DesperateEaterBuffEntry.DesperateEater;
    }

    class DesperateEaterBuffEntry : IBuffEntry
    {
        public static BuffID DesperateEater = new BuffID("DesperateEater", true);
        public static ConditionalWeakTable<PhysicalObject, ItemModule> module = new ConditionalWeakTable<PhysicalObject, ItemModule>();
        public static ConditionalWeakTable<Player, EaterModule> playerModule = new ConditionalWeakTable<Player, EaterModule>();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<DesperateEaterBuff, DesperateEaterBuffData, DesperateEaterBuffEntry>(DesperateEater);
        }

        public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.Player.GrabUpdate += Player_GrabUpdate;
            On.Player.BiteEdibleObject += Player_BiteEdibleObject;
            On.Player.CanBeSwallowed += Player_CanBeSwallowed;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!(playerModule.TryGetValue(self, out var module)))
            {
                playerModule.Add(self, new EaterModule(self));
            }
        }

        private static bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
        {
            if (self.Malnourished && (self.FoodInStomach < self.MaxFoodInStomach))
            {
                return false;
            }
            else return orig(self, testObj);
        }

        private static void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self.grasps != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (self.grasps[i] != null && !(self.grasps[i].grabbed is IPlayerEdible) && !(self.grasps[i].grabbed is Creature) &&
                        (!ModManager.MSC || self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear) && self.Malnourished)
                    {
                        var obj = self.grasps[i].grabbed;
                        if (!(module.TryGetValue(obj, out var itemModule)))
                        {
                            module.Add(obj, new ItemModule(obj));
                        }

                        if (module.TryGetValue(obj, out var itemModule1))
                        {
                            itemModule1.GetItemSprites();
                        }

                        if (playerModule.TryGetValue(self, out var playermodule))
                        {
                            playermodule.PickUpUpdate();
                        }
                        break;
                    }
                }
            }



        }

        private static void Player_BiteEdibleObject(On.Player.orig_BiteEdibleObject orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self.FoodInStomach < self.MaxFoodInStomach)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (self.grasps[i] != null && self.grasps[i].grabbed != null && (!ModManager.MSC || self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear))
                    {
                        if (!(self.grasps[i].grabbed is IPlayerEdible))
                        {
                            var obj = self.grasps[i].grabbed;
                            if (module.TryGetValue(obj, out var itemModule))
                            {
                                if (self.graphicsModule != null)
                                {
                                    (self.graphicsModule as PlayerGraphics).BiteFly(i);
                                }

                                itemModule.BitByPlayer(self.grasps[i], eu);
                            }

                        }
                        break;
                    }
                }
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self.room != null && self.room.physicalObjects != null)
            {
                for (int i = 0; i < self.room.physicalObjects.Length; i++)
                {
                    if (self.room.physicalObjects[i].Count <= 0)
                    {
                        continue;
                    }
                    else
                    {
                        for (int j = 0; j < self.room.physicalObjects[i].Count; j++)
                        {
                            var obj = self.room.physicalObjects[i][j];
                            if (module.TryGetValue(obj, out var itemModule))
                            {
                                itemModule.Update();
                                continue;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                }
            }
        }

    }

    internal class EaterModule
    {
        public Player self;
        public int grabCounter;

        public EaterModule(Player player)
        {
            self = player;
        }

        public void PickUpUpdate()
        {
            if (self.input[0].pckp)
            {
                grabCounter++;
            }
            else if (grabCounter > 0)
            {
                grabCounter--;
            }

            if (grabCounter >= 50)
            {
                self.BiteEdibleObject(self.input[0].pckp);
                grabCounter = 20;
            }

        }
    }

    internal class ItemModule
    {
        public int bites = -2;
        public PhysicalObject self;
        public RoomCamera.SpriteLeaser spriteLeasers;
        public int maxPips;
        public static Dictionary<string, float> pipDict = new Dictionary<string, float>()
        {
            {"Spear",2f},
            {"VultureMask", 1.5f},
            {"Rock",0.5f },
            {"Oracle",10f },
            {"SeedCob",30f },
            {"Lantern",0.75f },
            {"VoidSpawn",0f },
            {"FlareBomb",0.5f },
            {"FlyLure",0.5f },
            {"ScavengerBomb",1.5f },
            {"SporePlant",2f },
            {"AttachedBee",0f },
            {"NeedleEgg",4f },
            {"DartMaggot",0.5f },
            {"BubbleGrass",0.25f },
            {"OverseerCarcass",0.75f },
            {"BlinkingFlower",0.5f },
            {"Bullet",0.25f },
            {"SingularityBomb",6f },
            {"EnergyCell",10f },
            {"MoonCloak",0.75f },
        };

        public ItemModule(PhysicalObject physicalObject)
        {
            self = physicalObject;
        }

        public bool ReversedErase(PhysicalObject resultObj)
        {
            return (resultObj is ExplosiveSpear || resultObj is SingularityBomb || resultObj is Oracle);
        }

        public void GetItemSprites()
        {
            if (self.slatedForDeletetion || self.room == null)
            {
                spriteLeasers = null;
                return;
            }
            IDrawable drawable = self is IDrawable ? (self as IDrawable) : self.graphicsModule;
            foreach (var sLeaser in self.room.game.cameras[0].spriteLeasers)
            {
                if (sLeaser.drawableObject == drawable)
                {
                    spriteLeasers = sLeaser;

                    if (bites == -2)
                    {
                        bites = sLeaser.sprites.Length > 5 ? 5 : spriteLeasers.sprites.Length;
                        maxPips = bites;
                    }
                    return;
                }
            }

        }

        public void BitByPlayer(Creature.Grasp grasp, bool eu)
        {
            if (spriteLeasers == null) return;
            Debug.Log("Bites left:" + bites);
            bites--;
            self.room.PlaySound((this.bites == 0) ? SoundID.Slugcat_Eat_Dangle_Fruit : SoundID.Slugcat_Bite_Dangle_Fruit, self.firstChunk.pos);
            self.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
            if (this.bites < (self is ExplosiveSpear ? 2 : 1))
            {
                if (grasp.grabbed != null)
                {
                    PhysicalObject obj = grasp.grabbed;
                    float num = GetPips(obj);
                    int num2 = (int)((GetPips(obj) - (int)GetPips(obj)) / 0.25);
                    (grasp.grabber as Player).AddFood((int)num);
                    if (num2 > 0)
                    {
                        for (int i = 0; i < num2; i++)
                        {
                            (grasp.grabber as Player).AddQuarterFood();
                        }
                    }
                    if (grasp.grabbed is DartMaggot)
                    {
                        (grasp.grabber as Player).Stun(40);
                    }
                }
                grasp.Release();
                self.Destroy();
            }
        }

        public void Update()
        {
            if (spriteLeasers == null) return;
            int l = spriteLeasers.sprites.Length;
            if (bites >= 0)
            {
                if (l <= 5)
                {
                    for (int i = 0; i < l; i++)
                    {
                        if (spriteLeasers.sprites[i] is TriangleMesh)
                        {
                            for (int j = 0; j < (spriteLeasers.sprites[i] as TriangleMesh).vertices.Length; j++)
                            {
                                if (!ReversedErase(self))
                                {
                                    if (i >= bites)
                                    {
                                        (spriteLeasers.sprites[i] as TriangleMesh).MoveVertice(j, Vector2.zero);
                                    }
                                }
                                else
                                {
                                    if (i < l - bites)
                                    {
                                        (spriteLeasers.sprites[i] as TriangleMesh).MoveVertice(j, Vector2.zero);
                                    }
                                }
                            }
                        }

                        if (!ReversedErase(self))
                        {
                            spriteLeasers.sprites[i].isVisible = !(i >= bites);

                        }
                        else
                        {
                            spriteLeasers.sprites[i].isVisible = i >= (l - bites);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < l; i++)
                    {
                        if (spriteLeasers.sprites[i] is TriangleMesh)
                        {
                            for (int j = 0; j < (spriteLeasers.sprites[i] as TriangleMesh).vertices.Length; j++)
                            {
                                if (!ReversedErase(self))
                                {
                                    if (i >= (l + bites - 5))
                                    {
                                        (spriteLeasers.sprites[i] as TriangleMesh).MoveVertice(j, Vector2.zero);
                                    }
                                }
                                else
                                {
                                    if (i < 6 - bites)
                                    {
                                        (spriteLeasers.sprites[i] as TriangleMesh).MoveVertice(j, Vector2.zero);
                                    }
                                }
                            }
                        }

                        if (!ReversedErase(self))
                        {
                            spriteLeasers.sprites[i].isVisible = !(i >= (l + bites - 5));
                        }
                        else
                        {
                            spriteLeasers.sprites[i].isVisible = i >= (6 - bites);
                        }
                    }

                }
            }
        }

        public static float GetPips(PhysicalObject result)
        {
            string value = result.abstractPhysicalObject.type.value;
            if (pipDict.ContainsKey(value))
            {
                if (result is ExplosiveSpear || result is ElectricSpear)
                {
                    return 2.5f;
                }
                else
                    return pipDict[value];
            }
            else return 1f;
        }

    }
}
