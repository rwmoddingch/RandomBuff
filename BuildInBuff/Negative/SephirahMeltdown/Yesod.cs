using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;

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
                effect = new YesodScreenEffect(game.cameras[0], Data.CycleUse >=2 ? "HUD2" : "Foreground", Data.CycleUse/3f);
            
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

            for (int i = 0; i < effect.verticeColors.Length; i++)
            {
                effect.verticeColors[i].r = 0.07f;
                effect.verticeColors[i].g = 0.1f;
                effect.verticeColors[i].b = 0.1f;
                effect.verticeColors[i].a = inst * 0f;
            }


        }


        public void Draw(float timeStacker)
        {
            if(moveToFront)
                effect.MoveToFront();
        }

        public void ClearSprites()
        {
            effect.RemoveFromContainer();
        }



        private CustomFSprite effect;
    }
}
