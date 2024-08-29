using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuffUtils;
using RandomBuffUtils.FutileExtend;
using UnityEngine;
using static RandomBuffUtils.PlayerUtils;
using Random = UnityEngine.Random;

namespace RandomBuff.Core.Progression.CosmeticUnlocks
{
    internal class BuffCrownCosmetic : CosmeticUnlock
    {
        public override CosmeticUnlockID UnlockID => CosmeticUnlockID.Crown;

        public override string IconElement => "BuffCosmetic_Crown";

        public override SlugcatStats.Name BindCat => null;

        public override void StartGame(RainWorldGame game)
        {
            base.StartGame(game);
            PlayerUtils.AddPart(new CrownGraphicUtils());
        }

        public class CrownGraphicUtils : IOWnPlayerUtilsPart
        {
            public PlayerModulePart InitPart(PlayerModule module)
            {
                return null;
            }

            public PlayerModuleGraphicPart InitGraphicPart(PlayerModule module)
            {
                return new CrownGraphicsModule();
            }

        }

        public class CrownGraphicsModule : PlayerUtils.PlayerModuleGraphicPart
        {
            static int crownLightCount = 20;
            Vector2 pos;
            Vector2 lastPos;

            float[] rotations = new float[crownLightCount];
            float[] lastRotations = new float[crownLightCount];

            public override void InitSprites(SLeaserInstance sLeaserInstance, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaserInstance.sprites = new FSprite[1 + crownLightCount];
                sLeaserInstance.sprites[crownLightCount] = new FMesh(grown, "buffassets/illustrations/crownTex", customColor: true)
                {
                    shader = rCam.game.rainWorld.Shaders["UniformSimpleLighting"]
                };

                for(int i = 0;i < crownLightCount;i++)
                {
                    sLeaserInstance.sprites[i] = new FSprite("buffassets/illustrations/crownlight")
                    {
                        shader = rCam.game.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"],
                        color = Color.yellow * 0.7f + Color.red * 0.3f,
                        alpha = 0.1f,
                        anchorY = 0f
                    };
                }

                var mesh = sLeaserInstance.sprites[crownLightCount] as FMesh;
                mesh.Scale3D = new Vector3(5f, 5f, 5f);
                mesh.color = Color.yellow;
                Reset(self);
            }

            public override void DrawSprites(SLeaserInstance sLeaserInstance, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam,
                float timeStacker, Vector2 camPos)
            {
                base.DrawSprites(sLeaserInstance, self, sLeaser, rCam, timeStacker, camPos);
                Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);
                Vector2 drawPos = smoothPos + Mathf.Sin(timer / 40f * Mathf.PI) * 3f * Vector2.up - camPos;
                var mesh = sLeaserInstance.sprites[crownLightCount] as FMesh;
                mesh.color = Color.yellow;
                mesh.SetPosition(drawPos);
                mesh.rotation3D += new Vector3(90, 0, 0) * Time.deltaTime;
                
                for(int i = 0;i < crownLightCount;i++)
                {
                    sLeaserInstance.sprites[i].SetPosition(drawPos + Vector2.up * 10f);
                    sLeaserInstance.sprites[i].rotation = Mathf.Lerp(lastRotations[i], rotations[i], timeStacker);
                }
            }

            public override void Update(PlayerGraphics playerGraphics)
            {
                base.Update(playerGraphics);
                toPos = playerGraphics.head.pos + Vector2.up * 20f;
                lastPos = pos;
                pos = Vector2.Lerp(pos, toPos, 0.15f);
                timer++;

                for (int i = 0; i < crownLightCount; i++)
                {
                    rotations[i] += ((i + 1) % 2 == 0) ? (i + 1) % 5 * 1f : (i + 1) % 5 * -1f;
                    lastRotations[i] = rotations[i];
                }
            }

            public override void Reset(PlayerGraphics playerGraphics)
            {
                base.Reset(playerGraphics);
                lastPos = pos = toPos = playerGraphics.head.pos + Vector2.up * 5f;
                for(int i = 0;i < crownLightCount; i++)
                {
                    rotations[i] = Random.value * 360f;
                    lastRotations[i] = rotations[i];
                }
            }

            private int timer = 0;
            private Vector2 toPos;
            private Vector2 smoothPos;
        }
    }
}
