using RandomBuff.Render.UI;
using RandomBuff.Render.UI.Component;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Missions
{
    internal class ExclusiveMissionEffect : MissionEffect
    {
        static int totalBeamCount = 20;

        FSprite[] beams;
        Vector2[] pos;
        float[] lifes;
        float[] lastLifes;

        Color color;

        public ExclusiveMissionEffect(BuffMissionEffectContainer effectContainer, Color color) : base(effectContainer)
        {
            beams = new FSprite[totalBeamCount];
            pos = new Vector2[totalBeamCount];
            lifes = new float[totalBeamCount];
            lastLifes = new float[totalBeamCount];
            this.color = color;

            for(int i = 0;i < totalBeamCount; i++)
            {
                pos[i] = new Vector2(Random.value * Custom.rainWorld.options.ScreenSize.x, Custom.rainWorld.options.ScreenSize.y);
                lastLifes[i] = lifes[i] = Random.value * -1f;

                beams[i] = new FSprite(BuffUIAssets.ConicalLightOpaque400) { anchorX = 0.5f, anchorY = 1f, shader = Custom.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"], alpha = 1f };
                Container.AddChild(beams[i]);
            }
        }

        public override void Update(Vector2 zeroPoint)
        {
            base.Update(zeroPoint);

            for (int i = 0; i < totalBeamCount; i++)
            {
                lastLifes[i] = lifes[i];

                lifes[i] += 1 / 120f;

                if(lifes[i] > 1.1f)
                {
                    lastLifes[i] = lifes[i] = Random.value * -0.2f;
                    pos[i] = new Vector2(Random.value * Custom.rainWorld.options.ScreenSize.x, Custom.rainWorld.options.ScreenSize.y);
                }
            }
        }

        public override void GrafUpdate(Vector2 smoothZeroPoint, float t)
        {
            float smoothAlpha = Mathf.Lerp(LastAlpha, Alpha, t);
            for (int i = 0; i < totalBeamCount; i++)
            {
                float life = Mathf.Lerp(lastLifes[i], lifes[i], t);
                float f = (1f - Mathf.Pow(life - 0.5f, 2f) * 4f) * smoothAlpha;
                beams[i].color = Color.Lerp(Color.black, color, f);
                beams[i].SetPosition(smoothZeroPoint + pos[i]);
            }
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            beams = null;
        }
    }
}
