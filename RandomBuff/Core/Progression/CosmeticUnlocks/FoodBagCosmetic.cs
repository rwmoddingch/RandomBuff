using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RandomBuffUtils;
using MoreSlugcats;
using RWCustom;
using static RandomBuffUtils.PlayerUtils;
using System.Diagnostics.Eventing.Reader;
using RandomBuff.Core.Option;

namespace RandomBuff.Core.Progression.CosmeticUnlocks
{
    internal class FoodBagCosmetic : CosmeticUnlock
    {
        public override CosmeticUnlockID UnlockID => CosmeticUnlockID.FoodBag;

        public override string IconElement => "BuffCosmetic_FoodBag";

        public override SlugcatStats.Name BindCat => MoreSlugcatsEnums.SlugcatStatsName.Gourmand;

        public override void StartGame(RainWorldGame game)
        {
            base.StartGame(game);
            PlayerUtils.AddPart(new FoodBagGraphicUtils(BuffOptionInterface.Instance.CosmeticForEverySlug.Value));
        }
    }

    public class FoodBagGraphicUtils : IOWnPlayerUtilsPart
    {
        bool applyForAll;
        public FoodBagGraphicUtils(bool applyForAll)
        {
            this.applyForAll = applyForAll;
        }
        public PlayerModulePart InitPart(PlayerModule module)
        {
            return null;
        }

        public PlayerModuleGraphicPart InitGraphicPart(PlayerModule module)
        {
            if(module.Name == MoreSlugcatsEnums.SlugcatStatsName.Gourmand || applyForAll)
                return new FoodBagGraphicsModule();
            return null;
        }
    }

    public class FoodBagGraphicsModule : PlayerUtils.PlayerModuleGraphicPart
    {
        public static int maxFoodCount = 4;
        public int coolDown = 40;

        public override void InitSprites(SLeaserInstance sLeaserInstance, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaserInstance.sprites = new FSprite[1];
            sLeaserInstance.sprites[0] = new FSprite("buffassets/illustrations/FoodBag");
            sLeaserInstance.sprites[0].anchorY *= 0.4f;
        }

        public override void AddToContainer(SLeaserInstance sLeaserInstance, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            base.AddToContainer(sLeaserInstance, self, sLeaser, rCam, newContatiner);
            if (newContatiner == null)
            {
                rCam.ReturnFContainer("Midground").AddChild(sLeaserInstance.sprites[0]);
            }
            else
            {
                newContatiner.AddChild(sLeaserInstance.sprites[0]);
            }
            sLeaserInstance.sprites[0].MoveToBack();
        }

        public override void DrawSprites(SLeaserInstance sLeaserInstance, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaserInstance, self, sLeaser, rCam, timeStacker, camPos);
            sLeaserInstance.sprites[0].SetPosition(Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker) - camPos);
            Vector2 vector = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker);
            Vector2 vector2 = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
            Vector2 dir = (vector - vector2).normalized;
            sLeaserInstance.sprites[0].rotation = Custom.VecToDeg(dir);
            sLeaserInstance.sprites[0].color = Color.white;
        }

        public override void Update(PlayerGraphics playerGraphics)
        {
            base.Update(playerGraphics);
            coolDown--;
            if (playerGraphics.owner.room != null)
            {
                if (UnityEngine.Random.value <= 0.02f) coolDown = 0;
                if (coolDown < 1)
                {
                    coolDown = 80;

                    Vector2 vector = playerGraphics.head.pos + 0.5f * (playerGraphics.head.pos - playerGraphics.legs.pos);
                    int num = UnityEngine.Random.value > 0.5f ? -1 : 1;
                    Vector2 vector2;
                    if (playerGraphics.player.bodyMode != Player.BodyModeIndex.ZeroG)
                    {
                        vector2 = 10f * new Vector2(num * UnityEngine.Random.value, 1 - UnityEngine.Random.value);
                    }
                    else
                    {
                        vector2 = 10f * num * new Vector2(UnityEngine.Random.value, 1 - UnityEngine.Random.value);
                    }

                    playerGraphics.owner.room.AddObject(FakeFoodPool.GetFood(playerGraphics.owner.room, vector, vector2));
                }
            }
        }

        public override void Reset(PlayerGraphics playerGraphics)
        {
            base.Reset(playerGraphics);
        }


    }

    public static class FakeFoodPool
    {
        public static List<FakeFood> fFoodPool = new List<FakeFood>();

        public static FakeFood GetFood(Room room, Vector2 initPos, Vector2 initVel)
        {
            if (fFoodPool.Count == 0)
            {
                //UnityEngine.Debug.Log("Spawn New Food");
                FakeFood fakeFood = new FakeFood(room, Mathf.Clamp(Mathf.RoundToInt(UnityEngine.Random.Range(1, 6)), 1, 5), initPos, initVel);
                return fakeFood;
            }
            else
            {
                //UnityEngine.Debug.Log("Spawn Stinky Food");
                FakeFood fakeFood = fFoodPool.Pop();
                if (fakeFood.inactiveLife >= 1200)
                {
                    fakeFood.Destroy();
                    return new FakeFood(room, Mathf.Clamp(Mathf.RoundToInt(UnityEngine.Random.Range(1, 6)), 1, 5), initPos, initVel);
                }
                fakeFood.room = room;
                fakeFood.pos = initPos;
                fakeFood.lastPos = initPos;
                fakeFood.vel = initVel;
                fakeFood.life = 1f;
                fakeFood.rotation = 360f * UnityEngine.Random.value;
                fakeFood.lastRotation = fakeFood.rotation;
                fakeFood.foodType = Mathf.Clamp(UnityEngine.Random.Range(1, 8), 1, 7);
                fakeFood.slatedForDeletetion = false;
                fakeFood.Active = true;
                for (int i = 0; i < fakeFood.slimes.GetLength(0); i++)
                {
                    fakeFood.slimes[i, 0] = fakeFood.pos + 5f * Custom.RNV();
                    fakeFood.slimes[i, 1] = fakeFood.slimes[i, 0];
                    fakeFood.slimes[i, 2] = Vector2.zero;
                }
                return fakeFood;
            }
        }

        public static void RecycleFood(FakeFood fakeFood)
        {
            //UnityEngine.Debug.Log("Recycle Food");
            fakeFood.Active = false;
            fakeFood.RemoveFromRoom();
            fFoodPool.Add(fakeFood);
        }

        public static void UpdateInactiveItems()
        {
            try
            {
                if (fFoodPool.Count > 0)
                {
                    for (int i = fFoodPool.Count - 1; i >= 0; i--)
                    {
                        fFoodPool[i].inactiveLife++;
                        if (fFoodPool[i].inactiveLife >= 1200)
                        {
                            fFoodPool.RemoveAt(i);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }
    }

    public class FakeFood : UpdatableAndDeletable, IDrawable
    {
        public Vector2 lastPos;
        public Vector2 pos;
        public Vector2 vel;
        public Vector2[,] slimes;
        public bool firstInit = true;
        public bool lastActive;
        public bool active;
        public bool terrainContact;
        public float lastRotation;
        public float rotation;
        public float life;
        public float gravity = 1f;
        public int foodType;
        public int inactiveLife;
        public float collisionRad;
        public float airFrc = 0.999f;
        public float surfaceFric = 0.6f;
        public float waterFrc = 0.8f;
        private SharedPhysics.TerrainCollisionData scratchTerrainCollisionData;
        public static float[] getRad = new float[]
        {
            8f,//DangleFruit
            6f,//Lantern
            5f,//SlimeMold
            9.5f,//WaterNut
            4f,//EggBugEgg
            8f,//GlowWeed
            5.5f//DandelionPeach
        };
        public static float[] getBuoyancy = new float[]
        {
            1.1f,//DangleFruit
            0.8f,//Lantern
            1.1f,//SlimeMold
            1.2f,//WaterNut
            1.1f,//EggBugEgg
            1.1f,//GlowWeed
            1.2f//DandelionPeach
        };
        public static int maxSprite = 5;

        public bool Active
        {
            set
            {
                if (active != value)
                {
                    lastActive = !value;
                }
                else
                {
                    lastActive = value;
                }
                active = value;

                if (value)
                {
                    inactiveLife = 0;
                }
            }
        }

        public FakeFood(Room room, int foodType, Vector2 initPos, Vector2 initVel)
        {
            this.life = 1f;
            this.lastActive = true;
            this.active = true;
            this.room = room;
            this.foodType = foodType > getBuoyancy.Length ? getBuoyancy.Length : foodType;
            this.pos = initPos;
            this.vel = initVel;
            this.slimes = new Vector2[2, 3];
            for (int i = 0; i < slimes.GetLength(0); i++)
            {
                this.slimes[i, 0] = this.pos + 5f * Custom.RNV();
                this.slimes[i, 1] = this.slimes[i, 0];
                this.slimes[i, 2] = Vector2.zero;
            }
        }

        public void RenewSprite(RoomCamera.SpriteLeaser sLeaser, int index, string elementName, bool firstInit)
        {
            if (firstInit)
            {
                sLeaser.sprites[index] = new FSprite(elementName, true);
            }
            else
            {
                sLeaser.sprites[index].element = Futile.atlasManager.GetElementWithName(elementName);
                sLeaser.sprites[index].scale = 1f;
                sLeaser.sprites[index].SetAnchor(0.5f, 0.5f);
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (sLeaser.sprites == null)
            {
                sLeaser.sprites = new FSprite[maxSprite];
                firstInit = true;
            }
            else
            {
                firstInit = false;
            }

            int num = 0;
            if (foodType == 1)
            {
                RenewSprite(sLeaser, 0, "DangleFruit0A", firstInit);
                RenewSprite(sLeaser, 1, "DangleFruit0B", firstInit);
                num = 3;
            }
            else if (foodType == 2)
            {
                RenewSprite(sLeaser, 0, "DangleFruit0A", firstInit);
                RenewSprite(sLeaser, 1, "DangleFruit0B", firstInit);
                for (int i = 0; i < 2; i++)
                {
                    sLeaser.sprites[i].scaleX = 0.8f;
                    sLeaser.sprites[i].scaleY = 0.9f;
                }
                RenewSprite(sLeaser, 2, "Futile_White", firstInit);
                sLeaser.sprites[2].shader = rCam.game.rainWorld.Shaders["FlatLightBehindTerrain"];
                RenewSprite(sLeaser, 3, "Futile_White", firstInit);
                sLeaser.sprites[3].shader = rCam.game.rainWorld.Shaders["LightSource"];
                sLeaser.sprites[3].scale = 5f;
                num = 1;
            }
            else if (foodType == 3)
            {
                RenewSprite(sLeaser, 0, "DangleFruit0A", firstInit);
                sLeaser.sprites[0].scale = 0.85f;
                RenewSprite(sLeaser, 1, "Circle20", firstInit);
                sLeaser.sprites[1].scale = 0.25f;
                for (int i = 2; i <= 3; i++)
                {
                    RenewSprite(sLeaser, i, "KrakenTusk0", firstInit);
                    sLeaser.sprites[i].anchorY = 0.0f;
                    sLeaser.sprites[i].scaleX = 0.8f;

                }
                num = 1;
            }
            else if (foodType == 4)
            {
                RenewSprite(sLeaser, 0, "JetFishEyeA", firstInit);
                sLeaser.sprites[0].scaleX = 1.2f;
                sLeaser.sprites[0].scaleY = 1.4f;
                RenewSprite(sLeaser, 1, "tinyStar", firstInit);
                sLeaser.sprites[1].scaleX = 1.5f;
                sLeaser.sprites[1].scaleY = 2.4f;
                RenewSprite(sLeaser, 2, "Futile_White", firstInit);
                sLeaser.sprites[2].shader = rCam.game.rainWorld.Shaders["WaterNut"];
                num = 2;
            }
            else if (foodType == 5)
            {
                RenewSprite(sLeaser, 0, "DangleFruit0A", firstInit);
                RenewSprite(sLeaser, 1, "EggBugEggColor", firstInit);
                RenewSprite(sLeaser, 2, "JetFishEyeA", firstInit);
                sLeaser.sprites[0].anchorY = 0.3f;
                sLeaser.sprites[1].anchorY = 0.3f;
                sLeaser.sprites[2].anchorY = 0.7f;
                sLeaser.sprites[0].scaleX = 0.7f;
                sLeaser.sprites[0].scaleY = 0.75f;
                sLeaser.sprites[1].scaleX = 0.7f;
                sLeaser.sprites[1].scaleY = 0.75f;
                sLeaser.sprites[2].scale = 0.45f;
                num = 2;
            }
            else if (foodType == 6)
            {
                RenewSprite(sLeaser, 0, "Futile_White", firstInit);
                sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["WaterNut"];
                sLeaser.sprites[0].scaleX = 1.2f;
                sLeaser.sprites[0].scaleY = 1.6f;
                RenewSprite(sLeaser, 1, "DangleFruit0A", firstInit);               
                RenewSprite(sLeaser, 2, "DangleFruit0B", firstInit);
                for (int i = 1; i < 3; i++)
                {
                    sLeaser.sprites[i].scaleX = 0.9f;
                    sLeaser.sprites[i].scaleY = 1.3f;
                }
                RenewSprite(sLeaser, 3, "DangleFruit2A", firstInit);
                sLeaser.sprites[3].scaleX = 1.1f;
                sLeaser.sprites[3].scaleY = -1.4f;
                RenewSprite(sLeaser, 4, "DangleFruit2A", firstInit);
                sLeaser.sprites[4].scaleY = 1.4f;
                sLeaser.sprites[4].scaleX = 1.1f;
            }
            else
            {
                RenewSprite(sLeaser, 0, "DangleFruit0A", firstInit);
                sLeaser.sprites[0].scaleX = 0.92f;
                sLeaser.sprites[0].scaleY = 1.11f;
                sLeaser.sprites[0].alpha = 0.6f;
                RenewSprite(sLeaser, 1, "JellyFish0B", firstInit);
                sLeaser.sprites[1].scaleX = 0.92f;
                sLeaser.sprites[1].scaleY = 1.11f;
                sLeaser.sprites[1].alpha = 1f;
                RenewSprite(sLeaser, 2, "tinyStar", firstInit);
                sLeaser.sprites[2].scaleY = 4f;
                sLeaser.sprites[2].alpha = 0.9f;
                RenewSprite(sLeaser, 3, "SkyDandelion", firstInit);
                sLeaser.sprites[3].scale = 1f;
                sLeaser.sprites[3].anchorY *= 0.3f;
                num = 1;
            }
            if (num > 0)
            {
                for (int i = 0; i < num; i++)
                {
                    RenewSprite(sLeaser, maxSprite - i - 1, "pixel", firstInit);
                    sLeaser.sprites[maxSprite - i - 1].alpha = 0f;
                }
            }
            this.AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (slatedForDeletetion)
            {
                sLeaser.CleanSpritesAndRemove();
                return;
            }
            if (!active || life <= 0f)
            {
                sLeaser.RemoveAllSpritesFromContainer();
                return;
            }
            if (!lastActive && active)
            {
                this.Active = true;
                this.AddToContainer(sLeaser, rCam, null);
            }

            Vector2 vector = Vector2.Lerp(this.lastPos, this.pos, timeStacker);
            float rotation = Mathf.Lerp(this.lastRotation, this.rotation, timeStacker);
            if (foodType == 1 || foodType == 2 || foodType == 4)
            {
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    sLeaser.sprites[i].SetPosition(vector - camPos);
                }
            }
            else if (foodType == 3)
            {
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    if (i <= 1)
                        sLeaser.sprites[i].SetPosition(vector - camPos);
                    else if (i < 4)
                    {
                        Vector2 vector2 = Vector2.Lerp(this.slimes[i - 2, 1], this.slimes[i - 2, 0], timeStacker);
                        sLeaser.sprites[i].SetPosition(vector2 - camPos);
                        sLeaser.sprites[i].scaleY = -((vector - vector2).magnitude + 5f) / 25f;
                        sLeaser.sprites[i].rotation = Custom.VecToDeg(vector2 - vector);
                    }
                }
            }   
            else if (foodType == 6)
            {
                for (int i = 0; i < 3; i++)
                {
                    sLeaser.sprites[i].SetPosition(vector - camPos);
                }
                Vector2 vector3 = Custom.DegToVec(rotation);
                sLeaser.sprites[3].SetPosition(vector + 10f * vector3 - camPos);
                sLeaser.sprites[4].SetPosition(vector - 10f * vector3 - camPos);
            }
            else
            {
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    sLeaser.sprites[i].SetPosition(Vector2.Lerp(this.lastPos, this.pos, timeStacker) - camPos);
                }
            }

            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if (foodType != 3)
                    sLeaser.sprites[i].rotation = Mathf.Lerp(lastRotation, rotation, timeStacker);

                sLeaser.sprites[i].alpha *= 0.99f;
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (foodType == 1)
            {
                sLeaser.sprites[0].color = palette.blackColor;
                if (ModManager.MSC && rCam.room.game.session is StoryGameSession && rCam.room.world.name == "HR")
                {
                    sLeaser.sprites[1].color = RainWorld.SaturatedGold;
                    return;
                }
                sLeaser.sprites[1].color = new Color(0f, 0f, 1f);
            }
            else if (foodType == 2)
            {
                sLeaser.sprites[0].color = new Color(1f, 0.2f, 0f);
                sLeaser.sprites[1].color = new Color(1f, 1f, 1f);
                sLeaser.sprites[2].color = Color.Lerp(new Color(1f, 0.2f, 0f), new Color(1f, 1f, 1f), 0.3f);
                sLeaser.sprites[3].color = new Color(1f, 0.4f, 0.3f);
            }
            else if (foodType == 3)
            {
                sLeaser.sprites[0].color = Color.Lerp(Custom.HSL2RGB(Mathf.Lerp(0.07f, 0.05f, palette.darkness), 1f, 0.55f), palette.fogColor, Mathf.Lerp(0.25f, 0.35f, palette.fogAmount) * Mathf.Lerp(0.1f, 1f, palette.darkness));
                sLeaser.sprites[1].color = Color.Lerp(sLeaser.sprites[0].color, Color.white, 0.5f);
                sLeaser.sprites[2].color = sLeaser.sprites[0].color;
                sLeaser.sprites[3].color = sLeaser.sprites[0].color;
            }
            else if (foodType == 4)
            {
                sLeaser.sprites[1].color = Color.Lerp(new Color(0f, 0.4f, 1f), palette.blackColor, Mathf.Lerp(0f, 0.5f, rCam.PaletteDarkness()));
                sLeaser.sprites[2].color = Color.Lerp(palette.waterColor1, palette.waterColor2, 0.5f);
            }
            else if (foodType == 5)
            {
                float hue = Mathf.Lerp(-0.15f, 0.1f, Custom.ClampedRandomVariation(0.5f, 0.5f, 2f));
                Color a = Custom.HSL2RGB(Custom.Decimal(hue + 1.5f), 1f, 0.5f);
                Color a2 = Custom.HSL2RGB(Custom.Decimal(hue + 1f), 1f, 0.5f);
                Color color = palette.blackColor;                
                Color[] colors = new Color[]
                {
                    color,
                    a,
                    a2
                };
                for (int i = 0; i < 3; i++)
                {
                    sLeaser.sprites[i].color = colors[i];
                }
            }
            else if (foodType == 6)
            {
                sLeaser.sprites[0].color = Color.Lerp(palette.waterColor1, palette.waterColor2, 0.5f);
                Color color = new Color(0.8f, 1f, 0.4f);
                sLeaser.sprites[1].color = color;
                sLeaser.sprites[2].color = color;
                sLeaser.sprites[3].color = Color.Lerp(color, rCam.currentPalette.blackColor, 0.4f);
                sLeaser.sprites[4].color = Color.Lerp(color, rCam.currentPalette.blackColor, 0.4f);
            }
            else
            {
                Color col = new Color(0.59f, 0.78f, 0.96f);
                sLeaser.sprites[0].color = col;
                sLeaser.sprites[1].color = Color.Lerp(palette.fogColor, new Color(1f, 1f, 1f), 0.5f);
                sLeaser.sprites[2].color = Color.Lerp(col, sLeaser.sprites[1].color, 0.3f);
                sLeaser.sprites[3].color = Color.Lerp(palette.fogColor, new Color(1f, 1f, 1f), 0.5f);
                sLeaser.sprites[3].alpha = 0.8f;

            }
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Water");
            }
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].RemoveFromContainer();
                newContatiner.AddChild(sLeaser.sprites[i]);
            }

            if (foodType == 4)
            {
                sLeaser.sprites[2].RemoveFromContainer();
                rCam.ReturnFContainer("GrabShaders").AddChild(sLeaser.sprites[2]);
            }
            else if (foodType == 6)
            {
                sLeaser.sprites[0].RemoveFromContainer();
                rCam.ReturnFContainer("GrabShaders").AddChild(sLeaser.sprites[0]);
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (this.slatedForDeletetion || !this.active) return;

            this.life -= 0.00625f;
            if (this.room == null || this.life <= 0f || !this.room.ViewedByAnyCamera(this.pos, 10f))
            {
                FakeFoodPool.RecycleFood(this);
                return;
            }

            this.lastPos = this.pos;
            this.UpdateFrictionImpact();
            this.pos += this.vel;
            this.PushOutOfTerrain(this.room, this.pos);

            this.lastRotation = rotation;
            if (this.terrainContact)
            {
                this.rotation += 5f * vel.x;
            }

            if (foodType == 3)
            {
                for (int i = 0; i < slimes.GetLength(0); i++)
                {
                    this.slimes[i, 1] = this.slimes[i, 0];
                    this.slimes[i, 0] += this.slimes[i, 2];
                    this.slimes[i, 2] *= 0.98f;
                    if (this.room != null)
                    {
                        this.slimes[i, 2] += this.room.gravity * 0.9f * Vector2.down;
                    }

                    float rad = (i + 1) * 15f;
                    if (!Custom.DistLess(this.slimes[i, 0], this.pos, rad))
                    {
                        Vector2 dir = (this.pos - this.slimes[i, 0]).normalized;
                        float dist = (this.pos - this.slimes[i, 0]).magnitude;
                        this.slimes[i, 0] += 0.9f * dir * (dist - rad);
                        this.slimes[i, 2] += 0.9f * dir * (dist - rad);
                    }
                    if (Custom.DistLess(this.slimes[i, 0], this.pos, 100f))
                    {
                        SharedPhysics.TerrainCollisionData terrainCollisionData = this.scratchTerrainCollisionData.Set(this.slimes[i, 0], this.slimes[i, 1], this.slimes[i, 2], 3f, new IntVector2(0, 0), true);
                        terrainCollisionData = SharedPhysics.VerticalCollision(this.room, terrainCollisionData);
                        terrainCollisionData = SharedPhysics.HorizontalCollision(this.room, terrainCollisionData);
                        this.slimes[i, 0] = terrainCollisionData.pos;
                        this.slimes[i, 2] = terrainCollisionData.vel;
                    }
                }
            }

        }

        public override void Destroy()
        {
            base.Destroy();
            FakeFoodPool.RecycleFood(this);
        }

        public void UpdateFrictionImpact()
        {
            if (this.room == null) return;
            if (this.room.PointSubmerged(this.pos))
            {
                this.vel *= this.waterFrc;
                this.vel -= Vector2.up * (1 - getBuoyancy[foodType - 1]);
            }
            else
            {
                this.vel -= Vector2.up * this.gravity;
                this.vel *= airFrc;
                if (terrainContact)
                {
                    this.vel *= surfaceFric;
                }
            }
        }

        public void PushOutOfTerrain(Room room, Vector2 basePoint)
        {
            this.terrainContact = false;
            if (room == null) return;
            for (int i = 0; i < 9; i++)
            {
                if (room.GetTile(room.GetTilePosition(this.pos) + Custom.eightDirectionsAndZero[i]).Terrain == Room.Tile.TerrainType.Solid)
                {
                    Vector2 vector = room.MiddleOfTile(room.GetTilePosition(this.pos) + Custom.eightDirectionsAndZero[i]);
                    float num = 0f;
                    float num2 = 0f;
                    if (this.pos.y >= vector.y - 10f && this.pos.y <= vector.y + 10f)
                    {
                        if (this.lastPos.x < vector.x)
                        {
                            if (this.pos.x > vector.x - 10f - getRad[foodType - 1] && room.GetTile(room.GetTilePosition(this.pos) + Custom.eightDirectionsAndZero[i] + new IntVector2(-1, 0)).Terrain != Room.Tile.TerrainType.Solid)
                            {
                                num = vector.x - 10f - getRad[foodType - 1];
                            }
                        }
                        else if (this.pos.x < vector.x + 10f + getRad[foodType - 1] && room.GetTile(room.GetTilePosition(this.pos) + Custom.eightDirectionsAndZero[i] + new IntVector2(1, 0)).Terrain != Room.Tile.TerrainType.Solid)
                        {
                            num = vector.x + 10f + getRad[foodType - 1];
                        }
                    }
                    if (this.pos.x >= vector.x - 10f && this.pos.x <= vector.x + 10f)
                    {
                        if (this.lastPos.y < vector.y)
                        {
                            if (this.pos.y > vector.y - 10f - getRad[foodType - 1] && room.GetTile(room.GetTilePosition(this.pos) + Custom.eightDirectionsAndZero[i] + new IntVector2(0, -1)).Terrain != Room.Tile.TerrainType.Solid)
                            {
                                num2 = vector.y - 10f - getRad[foodType - 1];
                            }
                        }
                        else if (this.pos.y < vector.y + 10f + getRad[foodType - 1] && room.GetTile(room.GetTilePosition(this.pos) + Custom.eightDirectionsAndZero[i] + new IntVector2(0, 1)).Terrain != Room.Tile.TerrainType.Solid)
                        {
                            num2 = vector.y + 10f + getRad[foodType - 1];
                        }
                    }
                    if (Mathf.Abs(this.pos.x - num) < Mathf.Abs(this.pos.y - num2) && num != 0f)
                    {
                        this.pos.x = num;
                        this.vel.x = num - this.pos.x;
                        this.vel.y = this.vel.y * this.surfaceFric;
                        this.terrainContact = true;
                    }
                    else if (num2 != 0f)
                    {
                        this.pos.y = num2;
                        this.vel.y = num2 - this.pos.y;
                        this.vel.x = this.vel.x * this.surfaceFric;
                        this.terrainContact = true;
                    }
                    else
                    {
                        Vector2 vector2 = new Vector2(Mathf.Clamp(this.pos.x, vector.x - 10f, vector.x + 10f), Mathf.Clamp(this.pos.y, vector.y - 10f, vector.y + 10f));
                        if (Custom.DistLess(this.pos, vector2, getRad[foodType - 1]))
                        {
                            float num3 = Vector2.Distance(this.pos, vector2);
                            Vector2 a = Custom.DirVec(this.pos, vector2);
                            this.vel *= this.surfaceFric;
                            this.pos -= (getRad[foodType - 1] - num3) * a;
                            this.vel -= (getRad[foodType - 1] - num3) * a;
                            this.terrainContact = true;
                        }
                    }
                }
                else if (Custom.eightDirectionsAndZero[i].x == 0 && room.GetTile(room.GetTilePosition(this.pos) + Custom.eightDirectionsAndZero[i]).Terrain == Room.Tile.TerrainType.Slope)
                {
                    Vector2 vector3 = room.MiddleOfTile(room.GetTilePosition(this.pos) + Custom.eightDirectionsAndZero[i]);
                    if (room.IdentifySlope(room.GetTilePosition(this.pos) + Custom.eightDirectionsAndZero[i]) == Room.SlopeDirection.UpLeft)
                    {
                        if (this.pos.y < vector3.y - (vector3.x - this.pos.x) + getRad[foodType - 1])
                        {
                            this.pos.y = vector3.y - (vector3.x - this.pos.x) + getRad[foodType - 1];
                            this.vel.y = 0f;
                            this.vel.x = this.vel.x * this.surfaceFric;
                            this.terrainContact = true;
                        }
                    }
                    else if (room.IdentifySlope(room.GetTilePosition(this.pos) + Custom.eightDirectionsAndZero[i]) == Room.SlopeDirection.UpRight)
                    {
                        if (this.pos.y < vector3.y + (vector3.x - this.pos.x) + getRad[foodType - 1])
                        {
                            this.pos.y = vector3.y + (vector3.x - this.pos.x) + getRad[foodType - 1];
                            this.vel.y = 0f;
                            this.vel.x = this.vel.x * this.surfaceFric;
                            this.terrainContact = true;
                        }
                    }
                    else if (room.IdentifySlope(room.GetTilePosition(this.pos) + Custom.eightDirectionsAndZero[i]) == Room.SlopeDirection.DownLeft)
                    {
                        if (this.pos.y > vector3.y + (vector3.x - this.pos.x) - getRad[foodType - 1])
                        {
                            this.pos.y = vector3.y + (vector3.x - this.pos.x) - getRad[foodType - 1];
                            this.vel.y = 0f;
                            this.vel.x = this.vel.x * this.surfaceFric;
                            this.terrainContact = true;
                        }
                    }
                    else if (room.IdentifySlope(room.GetTilePosition(this.pos) + Custom.eightDirectionsAndZero[i]) == Room.SlopeDirection.DownRight && this.pos.y > vector3.y - (vector3.x - this.pos.x) - getRad[foodType - 1])
                    {
                        this.pos.y = vector3.y - (vector3.x - this.pos.x) - getRad[foodType - 1];
                        this.vel.y = 0f;
                        this.vel.x = this.vel.x * this.surfaceFric;
                        this.terrainContact = true;
                    }
                }
            }
        }
    }
}
