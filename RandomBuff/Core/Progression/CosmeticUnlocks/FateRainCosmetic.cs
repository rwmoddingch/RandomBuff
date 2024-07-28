using RandomBuffUtils.FutileExtend;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using static RandomBuffUtils.PlayerUtils.PlayerModuleGraphicPart;
using static RandomBuffUtils.PlayerUtils;

namespace RandomBuff.Core.Progression.CosmeticUnlocks
{
    internal class FateRainCosmetic : CosmeticUnlock
    {
        public override CosmeticUnlockID UnlockID => CosmeticUnlockID.FateRain;

        public override string IconElement => "BuffCosmetic_FateRain";

        public override SlugcatStats.Name BindCat => SlugcatStats.Name.White;

        public override void StartGame(RainWorldGame game)
        {
            base.StartGame(game);
            PlayerUtils.AddPart(new FateRainGraphicUtils());
        }

        public class FateRainGraphicUtils : IOWnPlayerUtilsPart
        {

            public PlayerModulePart InitPart(PlayerModule module)
            {
                return null;
            }

            public PlayerModuleGraphicPart InitGraphicPart(PlayerModule module)
            {
                if(module.Name == SlugcatStats.Name.White)
                    return new FateRainGraphicsModule();
                return null;
            }

        }

        public class FateRainGraphicsModule : PlayerUtils.PlayerModuleGraphicPart
        {            
            public float width = 70f;
            public float cloudHeight = 80f;
            public Vector2 cloudPos;
            
            public override void InitSprites(SLeaserInstance sLeaserInstance, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaserInstance.sprites = new FSprite[10];
                sLeaserInstance.sprites[0] = TriangleMesh.MakeLongMesh(4, false, false);               
                sLeaserInstance.sprites[0].shader = Custom.rainWorld.Shaders["FateRain"];
                sLeaserInstance.sprites[1] = new FSprite("buffassets/illustrations/EmoCloud");
                sLeaserInstance.sprites[1].scale = 0.5f;

                float x;
                float y;
                for (int i = 0; i < (sLeaserInstance.sprites[0] as TriangleMesh).UVvertices.Length; i++)
                {
                    x = (sLeaserInstance.sprites[0] as TriangleMesh).UVvertices[i].x;
                    y = (sLeaserInstance.sprites[0] as TriangleMesh).UVvertices[i].y;
                    (sLeaserInstance.sprites[0] as TriangleMesh).UVvertices[i] = new Vector2(y, x);
                }

                int num = -1;
                for (int j = 2; j < 10; j++)
                {
                    num = -num;
                    sLeaserInstance.sprites[j] = new FSprite("buffassets/illustrations/TinySplash");
                    sLeaserInstance.sprites[j].scaleX = num;
                }
            }

            public override void AddToContainer(SLeaserInstance sLeaserInstance, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                base.AddToContainer(sLeaserInstance, self, sLeaser, rCam, newContatiner);
                rCam.ReturnFContainer("GrabShaders").AddChild(sLeaserInstance.sprites[0]);
                rCam.ReturnFContainer("Bloom").AddChild(sLeaserInstance.sprites[1]);
                for (int i = 2; i < 10; i++)
                {
                    rCam.ReturnFContainer("Items").AddChild(sLeaserInstance.sprites[i]);
                }
            }

            public override void DrawSprites(SLeaserInstance sLeaserInstance, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam,
                float timeStacker, Vector2 camPos)
            {
                base.DrawSprites(sLeaserInstance, self, sLeaser, rCam, timeStacker, camPos);
                try
                {
                    sLeaserInstance.sprites[0].color = new Color(0.8f, 0.4f, 1f);
                    sLeaserInstance.sprites[1].color = Color.white;
                    cloudPos = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker) + cloudHeight * Vector2.up;
                    sLeaserInstance.sprites[1].SetPosition(cloudPos - camPos);
                    float x;
                    float y;
                    float splashLv = -1000f;
                    y = Mathf.Lerp(self.drawPositions[0, 1].y, self.drawPositions[0, 0].y, timeStacker);

                    for (int i = 0; i < 8; i++)
                    {
                        x = cloudPos.x - 0.5f * width + i * width / 7f;
                        (sLeaserInstance.sprites[0] as TriangleMesh).MoveVertice(2 * i, new Vector2(x, y + cloudHeight) - camPos);

                        splashLv = RayTraceFirstSplashFloorLevel(self.player.room, new Vector2(x, y + cloudHeight));
                        (sLeaserInstance.sprites[0] as TriangleMesh).MoveVertice(2 * i + 1, new Vector2(x, splashLv) - camPos);
                        sLeaserInstance.sprites[2 + i].SetPosition(new Vector2(x, splashLv) - camPos);
                        sLeaserInstance.sprites[2 + i].scaleX *= -1f;
                        sLeaserInstance.sprites[2 + i].color = Color.white;
                    }

                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }

            }

            public float RayTraceFirstSplashFloorLevel(Room room,Vector2 from)
            {
                if (room == null)
                {
                    return from.y;
                }
                if (room.GetTile(from).Solid)
                {
                    return from.y;
                }

                float startX = Mathf.CeilToInt(from.x / 20f);
                float startY = Mathf.CeilToInt(from.y / 20f);

                for (int i = 0; i < 30; i++)
                {
                    if (room.GetTile(new Vector2(from.x, (startY - i) * 20f - 10f)).Solid)
                    {
                        return (startY - i) * 20f;
                    }
                }

                return -1000f;
            }

            public override void Update(PlayerGraphics playerGraphics)
            {
                base.Update(playerGraphics);
            }

            public override void Reset(PlayerGraphics playerGraphics)
            {
                base.Reset(playerGraphics);
                
            }

            
        }
    }
}
