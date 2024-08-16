using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.SaveData.BuffConfig;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;

namespace BuiltinBuffs.Negative
{


    internal class ShortSightedBuffData : CountableBuffData
    {
        public override BuffID ID => ShortSightedEntry.ShortSighted;
        public override int MaxCycleCount => 3;

        [CustomBuffConfigRange(2f,1.5f,2.5f)]
        [CustomBuffConfigInfo("Zoom Factor", "Screen zoom factor")]
        public float ZoomFactor { get; }
    }

    internal class ShortSightedBuff : Buff<ShortSightedBuff, ShortSightedBuffData>
    {
        public override BuffID ID => ShortSightedEntry.ShortSighted;

        public override void Destroy()
        {
            base.Destroy();
            ShortSightedEntry.localCenter = new Vector2(0.5f, 0.5f);
            ShortSightedEntry.scale = 1;
            if (BuffCustom.TryGetGame(out var game))
            {
                var camera = game.cameras[0];
                for (int i = 0; i < 11; i++)
                {
                    camera.SpriteLayers[i].SetPosition(0, 0);
                    camera.SpriteLayers[i].scale = 1;

                }
            }
        }

    }

    internal class ShortSightedEntry : IBuffEntry
    {
        public static readonly BuffID ShortSighted = new BuffID(nameof(ShortSighted), true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ShortSightedBuff,ShortSightedBuffData, ShortSightedEntry>(ShortSighted);
        }

        public static void HookOn()
        {
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;

        }
        public static Vector2 localCenter = new Vector2(0.5f,0.5f);
        public static float scale = 1;

        private static void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            orig(self, timeStacker, timeSpeed);

            var toLocalCenter = (self.followAbstractCreature?.realizedCreature?.DangerPos - self.pos) / Custom.rainWorld.screenSize ??
                                new Vector2(0.5F, 0.5f);
            scale = Mathf.Lerp(scale, ShortSightedBuff.Instance.Data.ZoomFactor, 0.1f * Time.deltaTime * 40);

            localCenter = Vector2.Lerp(localCenter, toLocalCenter, 0.1f * Time.deltaTime * 40);

            for (int i = 0; i < 11; i++)
            {
                self.SpriteLayers[i].SetPosition(0, 0);
                self.SpriteLayers[i].scale = 1;
                self.SpriteLayers[i].ScaleAroundPointAbsolute(self.sSize * localCenter, scale, scale);
            }

            var offset = self.SpriteLayers[0].GetPosition() / self.sSize;
            var rect = Shader.GetGlobalVector(RainWorld.ShadPropSpriteRect);
            var center = new Vector2(Mathf.Lerp(rect.x, rect.z, localCenter.x), Mathf.Lerp(rect.y, rect.w, localCenter.y));
            var length = new Vector2(rect.z - rect.x, rect.y - rect.w);
            var xy = (new Vector2(rect.x, rect.y) - center) * scale + center;
            var zw = (new Vector2(rect.z, rect.w) - center) * scale + center;





            (rect.x, rect.y, rect.z, rect.w) = (xy.x, xy.y, zw.x, zw.y);
            Shader.SetGlobalVector(RainWorld.ShadPropSpriteRect, rect);



        }
    }
}
