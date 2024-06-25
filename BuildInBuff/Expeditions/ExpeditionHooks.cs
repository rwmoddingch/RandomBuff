using MonoMod.RuntimeDetour;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuffUtils.ObjectExtend;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Reflection.Emit;

namespace BuiltinBuffs.Expeditions
{
    internal static class ExpeditionHooks
    {
        #region DEBUG

        private static void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig(self, dt);
            if (Input.GetKey(KeyCode.I))
            {
                if (self.rainWorld.BuffMode() && Input.GetKeyDown(KeyCode.K))
                {
                    var all = ExpeditionProgression.burdenGroups["moreslugcats"];
                    var id = new BuffID(all[Random.Range(0, all.Count)]);
                    BuffPoolManager.Instance.CreateBuff(id);
                    BuffHud.Instance.AppendNewCard(id);
                }

                if (self.rainWorld.BuffMode() && Input.GetKeyDown(KeyCode.A))
                {
                    var all = ExpeditionProgression.burdenGroups.First().Value;

                    var id = new BuffID(all[index++]);
                    try
                    {
                        BuffPoolManager.Instance.CreateBuff(id);
                        BuffHud.Instance.AppendNewCard(id);
                    }
                    catch (Exception e)
                    {
                    }

                }

                if (self.rainWorld.BuffMode() && Input.GetKeyDown(KeyCode.S))
                {
                    var all = ExpeditionProgression.perkGroups.First().Value;
                    if (index2 == all.Count) return;

                    try
                    {
                        var id = new BuffID(all[index2++]);
                        BuffPoolManager.Instance.CreateBuff(id);
                        BuffHud.Instance.AppendNewCard(id);
                    }
                    catch (Exception e)
                    {
                    }

                }
                if (self.rainWorld.BuffMode() && Input.GetKeyDown(KeyCode.L))
                {
                    var ab = new TestAb(self.world, null, self.Players[0].pos, self.GetNewID(),false);
                    self.Players[0].Room.AddEntity(ab);
                    ab.RealizeInRoom();
                    ab.realizedObject.firstChunk.HardSetPosition(self.Players[0].realizedCreature.DangerPos);
                    self.Players[0].realizedCreature.room.AddObject(ab.realizedObject);

                }

            }

        }
        [BuffAbstractPhysicalObject]
        public class TestAb : AbstractSpear
        {
            [BuffAbstractPhysicalObjectProperty]
            public int test1 = -1;
            [BuffAbstractPhysicalObjectProperty]
            public int test2 { get; private set; } = -1;

            public override void Realize()
            {
                BuffUtils.Log("tEST","Test realized");
                realizedObject = new TestObject(this);
            }

            public override string ToString()
            {
                if (test1 == -1)
                    test1 = Random.Range(10, 50);
                if (test2 == -1)
                    test2 = Random.Range(10, 50);
                return base.ToString();
            }



            public TestAb(World world, Spear realizedObject, WorldCoordinate pos, EntityID ID, bool explosive) : base(world, realizedObject, pos, ID, explosive)
            {
            }

            public TestAb(World world, Spear realizedObject, WorldCoordinate pos, EntityID ID, bool explosive, float hue) : base(world, realizedObject, pos, ID, explosive, hue)
            {
            }

            public TestAb(World world, Spear realizedObject, WorldCoordinate pos, EntityID ID, bool explosive, bool electric) : base(world, realizedObject, pos, ID, explosive, electric)
            {
            }
        }

        public class TestObject : PlayerCarryableItem, IDrawable
        {
            private TestAb ab;
            public TestObject(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
            {
                ab = abstractPhysicalObject as TestAb;
                base.bodyChunks = new BodyChunk[1];
                base.bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0f, 0f), 5f, 0.07f);
                this.bodyChunkConnections = Array.Empty<BodyChunkConnection>();
                base.airFriction = 0.999f;
                base.gravity = 0.9f;
                this.bounce = 0.4f;
                this.surfaceFriction = 0.4f;
                this.collisionLayer = 2;
                base.waterFriction = 0.98f;
                base.buoyancy = 0.4f;
                base.firstChunk.loudness = 9f;
            }
            

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                label?.RemoveFromContainer();
                label = null;
                sLeaser.sprites = Array.Empty<FSprite>();
                label = new FLabel(Custom.GetFont(), $"{ab.test1},{ab.test2}");
                AddToContainer(sLeaser,rCam,null);
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                label.SetPosition(firstChunk.pos - camPos);
            }

            private FLabel label;

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                
            }

            public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                rCam.ReturnFContainer("HUD").AddChild(label);
            }
        }


        private static int index = 0;
        private static int index2 = 0;

        #endregion
        public static void OnModsInit()
        {
            _ = new Hook(typeof(RainWorld).GetProperty(nameof(RainWorld.ExpeditionMode)).GetGetMethod(),
                typeof(ExpeditionHooks).GetMethod(nameof(RainWorldExpeditionModeGet), BindingFlags.NonPublic | BindingFlags.Static));
            _ = new Hook(typeof(ExpeditionGame).GetProperty(nameof(ExpeditionGame.activeUnlocks)).GetGetMethod(),
                typeof(ExpeditionHooks).GetMethod(nameof(ExpeditionGameActiveUnlocksGet),BindingFlags.NonPublic | BindingFlags.Static));

            _ = new Hook(typeof(BuffPoolManager).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,null,new Type[] { typeof(RainWorldGame) },Array.Empty<ParameterModifier>()),
                typeof(ExpeditionHooks).GetMethod(nameof(BuffPoolManager_ctor), BindingFlags.NonPublic | BindingFlags.Static));
            On.Expedition.ExpeditionProgression.UnlockSprite += ExpeditionProgression_UnlockSprite;
            //On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
        }

    

        private static string ExpeditionProgression_UnlockSprite(On.Expedition.ExpeditionProgression.orig_UnlockSprite orig, string key, bool alwaysShow)
        {
            return orig(key, alwaysShow || Custom.rainWorld.BuffMode());
        }

        private static void BuffPoolManager_ctor(Action<BuffPoolManager,RainWorldGame> orig, BuffPoolManager self, RainWorldGame game)
        {
            activeUnlocks.Clear();
            orig(self, game);
            if (activeUnlocks.Any())
            {
                BuffUtils.Log("BuffExtend", "SetUp expedition trackers");
                ExpeditionGame.SetUpBurdenTrackers(game as RainWorldGame);
                ExpeditionGame.SetUpUnlockTrackers(game as RainWorldGame);
            }
        }




        private static bool RainWorldExpeditionModeGet(Func<RainWorld, bool> orig, RainWorld self)
        {
            return orig(self) || (self.BuffMode() && activeUnlocks.Any());
        }

        private static List<string> ExpeditionGameActiveUnlocksGet(Func<List<string>> orig)
        {
            if (Custom.rainWorld.BuffMode())
                return activeUnlocks;
            return orig();
        }


        public static List<string> activeUnlocks = new List<string>();

    }
}
