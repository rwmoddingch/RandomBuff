using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuffUtils;
using RandomBuffUtils.FutileExtend;
using UnityEngine;
using static RandomBuffUtils.PlayerUtils;

namespace RandomBuff.Core.Progression.CosmeticUnlocks
{
    internal class CrownCosmetic : CosmeticUnlock
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
            public override void InitSprites(SLeaserInstance sLeaserInstance, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaserInstance.sprites = new FSprite[1];
                sLeaserInstance.sprites[0] = new FMesh(grown,"Futile_White",true);
                Reset(self);
            }

            public override void DrawSprites(SLeaserInstance sLeaserInstance, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam,
                float timeStacker, Vector2 camPos)
            {
                base.DrawSprites(sLeaserInstance, self, sLeaser, rCam, timeStacker, camPos);
                var mesh = sLeaserInstance.sprites[0] as FMesh;
                mesh.SetPosition(smoothPos - camPos + Mathf.Sin(timer/40f * Mathf.PI)*3f * Vector2.up);
                mesh.rotation3D += new Vector3(0, 90, 0) * Time.deltaTime;
            }

            public override void Update(PlayerGraphics playerGraphics)
            {
                base.Update(playerGraphics);
                toPos = playerGraphics.head.pos + Vector2.up * 5f;
                smoothPos = Vector2.Lerp(smoothPos, toPos, 0.1f);
                timer++;
            }


            public override void Reset(PlayerGraphics playerGraphics)
            {
                base.Reset(playerGraphics);
                smoothPos = toPos = playerGraphics.head.pos + Vector2.up * 5f;
            }

            private int timer = 0;
            private Vector2 toPos;
            private Vector2 smoothPos;


        }

    }

 
}
