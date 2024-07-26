using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;
using RWCustom;
using System.Runtime.CompilerServices;
using HUD;

namespace BuiltinBuffs.Duality
{
    internal class PixieSlug : Buff<PixieSlug, PixieSlugBuffData>
    {
        public override BuffID ID => PixieSlugBuffEntry.PixieSlug;
    }

    internal class PixieSlugBuffData : CountableBuffData
    {
        public override BuffID ID => PixieSlugBuffEntry.PixieSlug;

        public override int MaxCycleCount => 5;
    }

    internal class PixieSlugBuffEntry : IBuffEntry
    {
        public static BuffID PixieSlug = new BuffID("PixieSlug", true);
        public static ConditionalWeakTable<Player, Pixies> pixies = new ConditionalWeakTable<Player, Pixies>();
        public static int rainbowSprite;
        public static float hue;

        public void OnEnable()
        {

            BuffRegister.RegisterBuff<PixieSlug, PixieSlugBuffData, PixieSlugBuffEntry>(PixieSlug);
        }

        public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.FlyGraphics.InitiateSprites += FlyGraphics_InitiateSprites;
            On.FlyGraphics.DrawSprites += FlyGraphics_DrawSprites;
            On.Rock.HitSomething += Rock_HitSomething;
            On.SporeCloud.Update += SporeCloud_Update;
        }


        private static void SporeCloud_Update(On.SporeCloud.orig_Update orig, SporeCloud self, bool eu)
        {
            orig(self,eu);
            if (self.room == null || self.room.abstractRoom.creatures.Count == 0) return;
            if (!self.slatedForDeletetion && !self.nonToxic && self.checkInsectsDelay > -1)
            {                
                for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
                {
                    if (self.room.abstractRoom.creatures[i].realizedCreature != null)
                    {
                        if (self.room.abstractRoom.creatures[i].realizedCreature is Player)
                        {
                            if (Custom.DistLess(self.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos, self.rad + self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.rad + 20f))
                            {
                                self.room.abstractRoom.creatures[i].realizedCreature.Die();
                            }
                        }
                    }
                }
            }
        }

        private static bool Rock_HitSomething(On.Rock.orig_HitSomething orig, Rock self, SharedPhysics.CollisionResult result, bool eu)
        {
            if (result.obj != null && result.obj is Player)
            {
                if (!(result.obj as Player).dead)
                {
                    (result.obj as Player).Die();
                }
                return true;
            }
            return orig(self,result, eu);
        }

        private static void FlyGraphics_DrawSprites(On.FlyGraphics.orig_DrawSprites orig, FlyGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (rainbowSprite > 0)
            {
                sLeaser.sprites[rainbowSprite].SetPosition(Vector2.Lerp(self.fly.bodyChunks[0].lastPos, self.fly.bodyChunks[0].pos, timeStacker) - camPos);
                if (!rCam.ReturnFContainer("GrabShaders")._childNodes.Contains(sLeaser.sprites[rainbowSprite]) && self.fly.room != null)
                {
                    sLeaser.sprites[rainbowSprite].RemoveFromContainer();
                    rCam.ReturnFContainer("GrabShaders").AddChild(sLeaser.sprites[rainbowSprite]);
                }
            }

            float lastHue = hue;
            if (hue >= 1f) hue = 0f;
            hue += 0.004f;
            Vector3 wingCol = new Vector3(Mathf.Lerp(lastHue, hue, timeStacker), 1, 0.8f);

            for (int i = 0; i < 3; i++)
            {
                sLeaser.sprites[i].color = Custom.HSL2RGB(wingCol.x, wingCol.y, wingCol.z);
            }

            
        }

        private static void FlyGraphics_InitiateSprites(On.FlyGraphics.orig_InitiateSprites orig, FlyGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            Futile.atlasManager.LoadAtlasFromTexture("rainbow", Resources.Load("Atlases/rainbow") as Texture2D, true);
            var rainbow = new FSprite("rainbow");
            rainbow.color = Color.black;
            rainbow.shader = rCam.game.rainWorld.Shaders["Rainbow"];
            rainbow.scale = 0.8f;
            rainbowSprite = sLeaser.sprites.Length;
            Array.Resize<FSprite>(ref sLeaser.sprites, rainbowSprite + 1);
            sLeaser.sprites[rainbowSprite] = rainbow;
            rCam.ReturnFContainer("GrabShaders").AddChild(sLeaser.sprites[rainbowSprite]);
        }



        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (pixies.TryGetValue(self, out var _pixies))
            {
                _pixies.Update();
            }
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!pixies.TryGetValue(self, out var pixie))
            {
                pixies.Add(self, new Pixies(self));
            }
        }

    }

    public class Pixies
    {
        public Player self;
        public PixieWings wings;
        public Pixies(Player player)
        {
            self = player;
        }

        public void Update()
        {
            if (wings == null && self.room != null && self.graphicsModule != null)
            {
                wings = new PixieWings(self);
                self.room.AddObject(wings);
            }

            if (wings == null) return;

            if (self.room == null || self.room != wings.room || self.graphicsModule == null)
            {
                wings.Destroy();
                wings = null;
            }
        }
    }

    public class PixieWings : CosmeticSprite
    {
        public Player self;
        public PlayerGraphics playerGraphics;
        public int wingSprite;
        public int flapCoolDown;
        public int flapDown;
        public float lastFlapDeg;
        public float flapDeg;
        public float hue;


        public PixieWings(Player player)
        {
            wingSprite = 0;
            self = player;
            //playerGraphics = self.graphicsModule as PlayerGraphics;
            this.room = self.room;
            flapDeg = 145f;
            flapCoolDown = 15;
            flapDown = 10;
            Futile.atlasManager.LoadAtlasFromTexture("rainbow", Resources.Load("Atlases/rainbow") as Texture2D, true);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[3];
            sLeaser.sprites[wingSprite] = new FSprite("FlyWing", true);
            sLeaser.sprites[wingSprite].anchorY = 0;
            sLeaser.sprites[wingSprite].scaleX = -1.5f;
            sLeaser.sprites[wingSprite].scaleY = 2f;
            sLeaser.sprites[wingSprite + 1] = new FSprite("FlyWing", true);
            sLeaser.sprites[wingSprite + 1].anchorY = 0;
            sLeaser.sprites[wingSprite + 1].scaleX = 1.5f;
            sLeaser.sprites[wingSprite + 1].scaleY = 2f;
            sLeaser.sprites[wingSprite + 2] = new FSprite("rainbow");
            sLeaser.sprites[wingSprite + 2].shader = rCam.game.rainWorld.Shaders["Rainbow"];
            sLeaser.sprites[wingSprite + 2].scale = 5f;
            sLeaser.sprites[wingSprite + 2].color = Color.black;
            AddToContainer(sLeaser, rCam, null);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            FContainer fContainer = rCam.ReturnFContainer("Background");
            fContainer.AddChild(sLeaser.sprites[wingSprite]);
            fContainer.AddChild(sLeaser.sprites[wingSprite + 1]);

            rCam.ReturnFContainer("GrabShaders").AddChild(sLeaser.sprites[wingSprite + 2]);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            float lastHue = hue;
            if (hue >= 1f) hue = 0f;
            if (self.graphicsModule == null) return;
            hue += 0.004f;
            Vector3 wingCol = new Vector3(Mathf.Lerp(lastHue, hue, timeStacker), 1, 0.8f);

            var playerGraf = self.graphicsModule as PlayerGraphics;
            if (sLeaser.sprites[wingSprite] != null)
            {
                Vector2 bodyDir = (Vector2.Lerp(playerGraf.drawPositions[1, 1], playerGraf.drawPositions[1, 0], timeStacker) - Vector2.Lerp(playerGraf.drawPositions[0, 1], playerGraf.drawPositions[0, 0], timeStacker)).normalized;
                Vector2 wingPos = Vector2.Lerp(playerGraf.drawPositions[0, 0], playerGraf.drawPositions[1, 0], 0.2f);
                Vector2 lastWingPos = Vector2.Lerp(playerGraf.drawPositions[0, 1], playerGraf.drawPositions[1, 1], 0.2f);

                sLeaser.sprites[wingSprite].SetPosition(Vector2.Lerp(lastWingPos, wingPos, timeStacker) + 5f * Custom.PerpendicularVector(bodyDir) - camPos);
                sLeaser.sprites[wingSprite + 1].SetPosition(Vector2.Lerp(lastWingPos, wingPos, timeStacker) - 5f * Custom.PerpendicularVector(bodyDir) - camPos);
                sLeaser.sprites[wingSprite + 2].SetPosition(Vector2.Lerp(lastWingPos, wingPos, timeStacker) - 5f * Custom.PerpendicularVector(bodyDir) - camPos);
                float num = Mathf.Lerp(lastFlapDeg, flapDeg, timeStacker);
                sLeaser.sprites[wingSprite].rotation = -num + Custom.VecToDeg(-bodyDir);
                sLeaser.sprites[wingSprite + 1].rotation = num + Custom.VecToDeg(-bodyDir);
                sLeaser.sprites[wingSprite].color = Custom.HSL2RGB(wingCol.x, wingCol.y, wingCol.z);
                sLeaser.sprites[wingSprite + 1].color = Custom.HSL2RGB(wingCol.x, wingCol.y, wingCol.z);
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            
            if (self.graphicsModule == null || self.room == null || self.room != this.room || self.slatedForDeletetion) 
            { 
                this.Destroy();
                return;
            }

            lastFlapDeg = flapDeg;
            if (flapCoolDown > 0) flapCoolDown--;
            if (flapDown > 0)
            {
                flapDown--;
                if (flapDeg < 145f)
                {
                    flapDeg += 8.5f;
                }
            }

            if (self.canJump <= 0)
            {
                if (flapDeg > 90f && flapDown == 0)
                {
                    flapDeg -= 10f;
                }

                if (self.wantToJump > 0 && flapCoolDown == 0)
                {
                    if (self.bodyMode != Player.BodyModeIndex.CorridorClimb && self.bodyMode != Player.BodyModeIndex.Swimming)
                    {
                        flapCoolDown = 15;
                        flapDown = 10;
                        if (self.bodyMode != Player.BodyModeIndex.ZeroG)
                        {
                            self.mainBodyChunk.vel.y += 20f;
                            self.bodyChunks[1].vel.y += 20f;
                        }
                        else
                        {
                            self.mainBodyChunk.vel += 10f * new Vector2(self.input[0].x, self.input[0].y);
                            self.bodyChunks[1].vel += 10f * new Vector2(self.input[0].x, self.input[0].y);
                        }
                    }

                }
            }
            else
            {
                if (flapDeg < 145f)
                {
                    flapDeg += 8.5f;
                }
            }
        }
    }
}
