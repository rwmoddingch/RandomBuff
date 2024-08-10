using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;

namespace BuiltinBuffs.Negative
{
    internal class PresbyopiaEntry : IBuffEntry
    {
        public static readonly BuffID Presbyopia = new BuffID(nameof(Presbyopia), true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<PresbyopiaBuff, PresbyopiaBuffData, PresbyopiaEntry>(Presbyopia);
        }

        public static void HookOn()
        {
            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;

        }


        public static void LoadAssets()
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath(Presbyopia.GetStaticData().AssetPath + Path.DirectorySeparatorChar + "presbyopia"));
            Custom.rainWorld.Shaders.Add($"{Presbyopia}.Presbyopia", FShader.CreateShader($"{Presbyopia}.Presbyopia", bundle.LoadAsset<Shader>("Presbyopia")));
        }
        private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig(self, cam);
            if(Presbyopia.GetBuff() is PresbyopiaBuff buff) 
                self.AddPart(buff.blurPart = new FullScreenBlur(self));
        }

    }

    internal class PresbyopiaBuffData : CountableBuffData
    {
        public override BuffID ID => PresbyopiaEntry.Presbyopia;
        public override int MaxCycleCount => 5;
    }

    internal class PresbyopiaBuff : Buff<PresbyopiaBuff,PresbyopiaBuffData>
    {
        public override BuffID ID => PresbyopiaEntry.Presbyopia;

        public PresbyopiaBuff()
        {
            if(BuffCustom.TryGetGame(out var game) && game.cameras[0]?.hud is HUD.HUD hud)
                hud.AddPart(blurPart = new FullScreenBlur(hud));

        }

        public override void Destroy()
        {
            base.Destroy();
            blurPart.ClearSprites();

        }

        public FullScreenBlur blurPart;

    }

    class FullScreenBlur : HudPart
    {
        public FullScreenBlur(HUD.HUD hud) : base(hud)
        {
            blur = new CustomFSprite("Futile_White");
            hud.fContainers[0].AddChild(blur);
            blur.MoveToBack();
            blur.vertices[3] = Vector2.zero;
            blur.vertices[0] = Vector2.up * hud.rainWorld.screenSize.y;
            blur.vertices[1] = Vector2.up * hud.rainWorld.screenSize.y + Vector2.right * hud.rainWorld.screenSize.x;
            blur.vertices[2] = Vector2.right * hud.rainWorld.screenSize.x;
            blur.shader = hud.rainWorld.Shaders[$"{PresbyopiaEntry.Presbyopia}.Presbyopia"];
            for (int i = 0; i < blur.verticeColors.Length; i++)
            {
                blur.verticeColors[i].r = 0.5f;
                blur.verticeColors[i].a = 1f;
            }


        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            for (int i = 0; i < blur.verticeColors.Length; i++)
            {
                blur.verticeColors[i].r = 0.5f;
                blur.verticeColors[i].a = 1;
            }
        }

        public override void Update()
        {
            base.Update();
            if (hud.owner is Player player && player.room != null)
            {
                var pos = (player.DangerPos - player.room.game.cameras[0].pos) / Custom.rainWorld.screenSize;
                for (int i = 0; i < blur.verticeColors.Length; i++)
                {
                    blur.verticeColors[i].g = pos.x;
                    blur.verticeColors[i].b = pos.y;

                }
            }
        }


        public override void ClearSprites()
        {
            blur.RemoveFromContainer();
            base.ClearSprites();
        }



        private CustomFSprite blur;
    }
}
