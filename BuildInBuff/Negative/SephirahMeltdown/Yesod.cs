using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Negative.SephirahMeltdown
{
    internal class YesodBuffData : SephirahMeltdownBuffData
    {
        public static BuffID Yesod = new BuffID(nameof(Yesod), true);
        public override BuffID ID => Yesod;
    }

    internal class YesodBuff : Buff<YesodBuff, YesodBuffData>
    {
        public override BuffID ID => YesodBuffData.Yesod;

        public YesodBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
                effect = new YesodScreenEffect(game.cameras[0], Data.CycleUse >=2 ? "HUD2" : Data.CycleUse >=1 ? "Water" : "Foreground", Data.CycleUse > 1 ? Data.CycleUse/3f : 0);
            
        }

        public override void Destroy()
        {
            base.Destroy();
            effect.ClearSprites();
        }

        public YesodScreenEffect effect;

        public static void HookOn()
        {
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
        }

        private static void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            orig(self, timeStacker, timeSpeed);
            YesodBuff.Instance.effect.Draw(timeStacker);
        }
    }

    class YesodScreenEffect
    {
        private bool moveToFront = false;
        private float inst;
        private bool isEnable = false;

        public YesodScreenEffect(RoomCamera rCam, string layerName,float inst) : base()
        {
            effect = new CustomFSprite("Futile_White");
            effect.vertices[3] = Vector2.zero;
            effect.vertices[0] = Vector2.up * Custom.rainWorld.screenSize.y;
            effect.vertices[1] = Vector2.up * Custom.rainWorld.screenSize.y + Vector2.right * Custom.rainWorld.screenSize.x;
            effect.vertices[2] = Vector2.right * Custom.rainWorld.screenSize.x;
            effect.shader = Custom.rainWorld.Shaders[$"SephirahMeltdownEntry.Yesod"];
            rCam.ReturnFContainer(layerName).AddChild(effect);
            if(layerName == "Foreground")
                effect.MoveToBack();
            else
            {
                effect.MoveToFront();
                moveToFront = true;
            }

            isEnable = layerName == "HUD2";
            this.inst = inst;

        }

        private float timer = 0;
        private float waitTimer = 0;

        private float instFac = 0.1f;
        private float yClamp = 0.05f;
        private bool IsEnable => timer > 0 && waitTimer <= 0;

        public void Draw(float timeStacker)
        {
            if(moveToFront)
                effect.MoveToFront();

            if (waitTimer <= 0)
            {
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    instFac = Random.Range(0.2f, 0.5f) * (Random.value < 0.03f ?  Random.Range(2,3) : 1) * (Random.value < 0.03f ? Random.Range(2, 3) : 1);
                    timer = Random.Range(0.7f, 2f);
                    yClamp = Random.Range(0.05f, 0.15f);
                }
            }
            else
            {
                waitTimer-= Time.deltaTime;
            }
            for (int i = 0; i < effect.verticeColors.Length; i++)
            {
                effect.verticeColors[i].r = 0.042f;
                effect.verticeColors[i].g = 0.5f;
                effect.verticeColors[i].b = yClamp;
                effect.verticeColors[i].a = inst * instFac * 0.01f * (IsEnable ? 1 : 0);
            }

        }

        public void ClearSprites()
        {
            effect.RemoveFromContainer();
        }



        private CustomFSprite effect;
    }
}
