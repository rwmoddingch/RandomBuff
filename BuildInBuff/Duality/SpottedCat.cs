//using System;
//using RandomGains.Frame.Core;
//using RandomGains.Gains;
//using RandomGains;
//using MonoMod;
//using UnityEngine;
//using Random = UnityEngine.Random;
//using System.Runtime.CompilerServices;
//using RWCustom;

//namespace TemplateGains
//{
//    internal class SpottedCatDataImpl : GainDataImpl
//    {
//        public override GainID GainID => SpottedCatGainEntry.SpottedCatID;


//    }

//    internal class SpottedCatGainImpl : GainImpl<SpottedCatGainImpl, SpottedCatDataImpl>
//    {
//        public override GainID GainID => SpottedCatGainEntry.SpottedCatID;
//    }

//    internal class SpottedCatGainEntry : GainEntry
//    {
//        public static GainID SpottedCatID = new GainID("SpottedCatID", true);
//        public static ConditionalWeakTable<Player, EatSomeFood> modules = new ConditionalWeakTable<Player, EatSomeFood>();

//        public static void HookOn()
//        {
//            On.Player.ctor += Player_ctor;

//            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
//            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
//            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;

//            On.Player.Update += Player_Update;
//            On.Player.EatMeatUpdate += Player_EatMeatUpdate;
//        }

//        private static void Player_EatMeatUpdate(On.Player.orig_EatMeatUpdate orig, Player self, int graspIndex)
//        {
//            orig.Invoke(self, graspIndex);
//            if (!modules.TryGetValue(self, out var eatSome)) return;
//            if (self.grasps[graspIndex] == null || !(self.grasps[graspIndex].grabbed is Creature))
//            {
//                return;
//            }
//            if (self.eatMeat>20)
//            {
//                eatSome.foodColor = self.grasps[graspIndex].grabber.ShortCutColor();
//            }
//        }

//        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
//        {
//            orig.Invoke(self, eu);
//            if (!modules.TryGetValue(self, out var eatSome)) return;
//            if (self.firstChunk.submersion > 0.9f && !self.room.game.setupValues.invincibility)
//            {
//                eatSome.foodColor = null;
//            }



//        }

//        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
//        {
//            orig.Invoke(self, abstractCreature, world);
//            var module = new EatSomeFood(self);
//            modules.Add(self, module);
//            //初始化会流口水modules


//        }
//        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
//        {
//            orig.Invoke(self, sLeaser, rCam);
//            if (!modules.TryGetValue(self.player, out var module)) return;

//            //扩容贴图
//            module.Foodindex = sLeaser.sprites.Length;
//            Array.Resize<FSprite>(ref sLeaser.sprites, sLeaser.sprites.Length + module.FoodNumber);

//            //个扩容的贴图赋予材质
//            for (int i = 0; i < module.FoodNumber; i++)
//            {
//                sLeaser.sprites[module.Foodindex + i] = new FSprite("Circle20");
//            }
//            //添加到图层
//            self.AddToContainer(sLeaser, rCam, null);
//        }

//        private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
//        {
//            orig.Invoke(self, sLeaser, rCam, newContatiner);
//            if (!modules.TryGetValue(self.player, out var module)) return;


//            //如果扩容完毕则开始添加图层
//            if ((module.Foodindex == 0) || (sLeaser.sprites.Length <= module.Foodindex)) return;
//            FContainer fContainer2 = rCam.ReturnFContainer("Midground");//新建一个中间层
//            for (int i = 0; i < module.FoodNumber; i++)
//            {
//                fContainer2.AddChild(sLeaser.sprites[module.Foodindex + i]);
//            }

//        }

//        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
//        {
//            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
//            if (!modules.TryGetValue(self.player, out var module)) return;
//            if ((module.Foodindex == 0) || (sLeaser.sprites.Length <= module.Foodindex)) return;

//            if (module.foodColor == null)
//            {
//                for (int i = 0; i < module.FoodNumber; i++)
//                {
//                    var foodMark = sLeaser.sprites[module.Foodindex + i];

//                    foodMark._isVisible=false;
//                }
//                return;
//            }
//            //用循环吧所有的嘴巴痕迹添加
//            for (int i = 0; i < module.FoodNumber; i++)
//            {
//                var foodMark = sLeaser.sprites[module.Foodindex + i];
//                foodMark._isVisible= true;

//                foodMark.color = (Color)module.foodColor;
//                foodMark.scale = 1f / 4f;
//                foodMark.alpha = 0.4f;

//                //贴图根据头的旋转做一定的旋转
//                Vector2 markMove = Custom.rotateVectorDeg(module.markPos[i], Custom.VecToDeg(self.player.mainBodyChunk.Rotation));

//                //玩家脸部贴图朝下一点的位置
//                var vector1 = sLeaser.sprites[9].GetPosition() - self.player.mainBodyChunk.Rotation * 3f;

//                foodMark.SetPosition(vector1 + markMove + ChangePosAndRotation(self.player));
//                //移动到脸的前面
//                foodMark.MoveInFrontOfOtherNode(sLeaser.sprites[9]);

//            }
//        }

//        public static Vector2 ChangePosAndRotation(Player player)
//        {
//            if (player.bodyMode == Player.BodyModeIndex.Stand)
//            {
//                if (player.input[0].x > 0)
//                {
//                    return Vector2.Perpendicular(player.mainBodyChunk.Rotation) * player.mainBodyChunk.rad * -0.6f - (player.mainBodyChunk.Rotation) * 2;
//                }
//                else if (player.input[0].x < 0)
//                {
//                    return Vector2.Perpendicular(player.mainBodyChunk.Rotation) * player.mainBodyChunk.rad * 0.6f - (player.mainBodyChunk.Rotation) * 2;
//                }
//            }
//            if (player.bodyMode == Player.BodyModeIndex.Crawl)
//            {
//                if (player.bodyChunks[0].pos.x > player.bodyChunks[1].pos.x)
//                {
//                    return Vector2.Perpendicular(player.mainBodyChunk.Rotation) * player.mainBodyChunk.rad * -0.3f - (player.mainBodyChunk.Rotation) * 0.8f;
//                }
//                else
//                {
//                    return Vector2.Perpendicular(player.mainBodyChunk.Rotation) * player.mainBodyChunk.rad * 0.3f - (player.mainBodyChunk.Rotation) * 0.8f;
//                }
//            }




//            return Vector2.zero;
//        }
//        public override void OnEnable()
//        {
//            GainRegister.RegisterGain<SpottedCatGainImpl, SpottedCatDataImpl, SpottedCatGainEntry>(SpottedCatID);
//        }
//    }
//    public class EatSomeFood
//    {
//        public Color? foodColor;
//        public Player player;
//        public int Foodindex = 0;
//        public int FoodNumber = 5;
//        public Vector2[] markPos;

//        public EatSomeFood(Player player)
//        {
//            this.player = player;
//            markPos = new Vector2[FoodNumber];

//            for (int i = 0; i < FoodNumber; i++)
//            {
//                markPos[i] = Custom.RNV() * Random.Range(0f, 5f);
//            }

//        }
//    }
//}
