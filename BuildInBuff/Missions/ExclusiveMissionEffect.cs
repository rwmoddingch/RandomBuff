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
        static int totalBeamCount = 40;
        static int totalDustCount = 80;

        FSprite bloom;

        FSprite[] beams;
        Vector2[] pos;
        float[] lifes;
        float[] lastLifes;

        FSprite[] dust;
        Vector2[] dustPos;
        Vector2[] lastDustPos;
        Vector2[] dustVel;
        float[] dustLife;
        float[] lastDustLife;
        float[] dustInBackground;

        Color color;

        public ExclusiveMissionEffect(BuffMissionEffectContainer effectContainer, Color color) : base(effectContainer)
        {
            beams = new FSprite[totalBeamCount];
            pos = new Vector2[totalBeamCount];
            lifes = new float[totalBeamCount];
            lastLifes = new float[totalBeamCount];
            this.color = color / 3f;

            for(int i = 0;i < totalBeamCount; i++)
            {
                beams[i] = new FSprite(BuffUIAssets.ConicalLightOpaque400) { anchorX = 0.5f, anchorY = 1f, shader = Custom.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"], alpha = 1f};

                ApplyRandomFactorForBeam(i);
                Container.AddChild(beams[i]);
            }
            bloom = new FSprite(BuffUIAssets.MissionInfoBoxBloom1920, true)
            {
                shader = Custom.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"],
                color = color,
                scaleX = Custom.rainWorld.screenSize.x / 1920f,
                scaleY = Custom.rainWorld.screenSize.y / 1080f,
                anchorX = 0f,
                anchorY = 0f,
            };
            Container.AddChild(bloom);

            dust = new FSprite[totalDustCount];
            lastDustPos = new Vector2[totalDustCount];
            dustPos = new Vector2[totalDustCount];
            dustVel = new Vector2[totalDustCount];
            dustLife = new float[totalDustCount];
            lastDustLife = new float[totalDustCount];
            dustInBackground = new float[totalDustCount];

            for(int i = 0;i < totalDustCount; i++)
            {
                dust[i] = new FSprite(BuffUIAssets.CircleGradient20, true)
                {
                    shader = Custom.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"],
                    alpha = 1f
                };
                Container.AddChild(dust[i]);
                ApplyRandomFactorForDust(i);
            }
        }

        void ApplyRandomFactorForBeam(int i, float maxExtraLife = -1f)
        {
            float f = Random.value;

            pos[i] = new Vector2(f * Custom.rainWorld.options.ScreenSize.x, Custom.rainWorld.options.ScreenSize.y);
            lastLifes[i] = lifes[i] = Random.value * -1f;

            beams[i].scaleX = (Mathf.Pow(0.5f - f, 2f) + 0.5f) * 2f;
            beams[i].scaleY = (1f - 1.5f * Mathf.Pow(0.5f - f, 2f)) * 2f;
        }

        void ApplyRandomFactorForDust(int i, float maxExtraLife = -1f)
        {
            Vector2 screenSize = Custom.rainWorld.options.ScreenSize;
            lastDustPos[i] = dustPos[i] = new Vector2(Mathf.Lerp(0f, screenSize.x, Random.value), Mathf.Lerp(0f, screenSize.y, Random.value));
            dustVel[i] = Vector2.down * 15f + Custom.RNV() * 8f;
            dustInBackground[i] = Random.value;
            lastDustLife[i] = dustLife[i] = Random.value - maxExtraLife;
            dust[i].scale = Mathf.Lerp(0.5f, 2f, dustInBackground[i]);
            dust[i].color = Color.Lerp(Color.black, color, dustInBackground[i] * 0.5f + 0.5f);
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
                    ApplyRandomFactorForBeam(i, -0.2f);
                }
            }

            for(int i = 0;i < totalBeamCount; i++)
            {
                lastDustPos[i] = dustPos[i];               
                dustPos[i] += dustVel[i] / 40f;
                dustVel[i] += Custom.RNV() / 5f;

                lastDustLife[i] = dustLife[i];
                dustLife[i] += (1 / 1600f);
                if(dustLife[i] > 1.1f)
                {
                    ApplyRandomFactorForDust(i, -0.2f);
                }
            }
        }

        public override void GrafUpdate(Vector2 smoothZeroPoint, float t)
        {
            float smoothAlpha = Mathf.Lerp(LastAlpha, Alpha, t);
            Vector2 mid = Custom.rainWorld.options.ScreenSize / 2f;

            bloom.alpha = Mathf.Pow(smoothAlpha, 3f);
            bloom.SetPosition(smoothZeroPoint);

            for (int i = 0; i < totalBeamCount; i++)
            {
                float life = Mathf.Lerp(lastLifes[i], lifes[i], t);
                float f = (1f - Mathf.Pow(life - 0.5f, 2f) * 4f) * smoothAlpha;
                beams[i].color = Color.Lerp(Color.black, color, f);
                beams[i].SetPosition(smoothZeroPoint + pos[i]);
            }

            for(int i = 0;i < totalDustCount;i++)
            {
                float life = Mathf.Lerp(lastDustLife[i], dustLife[i], t);
                float f = (1f - Mathf.Pow(life - 0.5f, 2f) * 4f) * smoothAlpha;
                Vector2 smoothPos = Vector2.Lerp(lastDustPos[i], dustPos[i], t);
                smoothPos = Vector2.Lerp(mid, smoothPos, dustInBackground[i] * 0.3f + 0.7f);

                dust[i].color = Color.Lerp(Color.black, color, (dustInBackground[i] * 0.5f + 0.5f) * f);
                dust[i].SetPosition(smoothZeroPoint + smoothPos);
            }
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            beams = null;
            dust = null;
            bloom = null;
        }
    }
}
