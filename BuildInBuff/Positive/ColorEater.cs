using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

namespace BuildInBuff.Positive
{
    class ColorEaterBuff : Buff<ColorEaterBuff, ColorEaterBuffData>
    {
        public override BuffID ID => ColorEaterBuffEntry.ColorEaterID;
      
        //按下按键可以吸取周围的颜色
        public override bool Trigger(RainWorldGame game)
        {
            if (game.AlivePlayers.Count > 0)
            {
                var player = (game.AlivePlayers[0].realizedCreature as Player);

                var color = game.cameras[0].PixelColorAtCoordinate(player.bodyChunks[1].pos - new Vector2(0, 10));
                player.EatPlate().MainColor = Color.Lerp(player.EatPlate().MainColor, color, 0.9f);
                player.EatPlate().MainColor = Custom.HSL2RGB(Custom.RGB2HSL(player.EatPlate().MainColor).x, 0.4f, 0.5f);
            }

            //return false;
            return base.Trigger(game);
        }

    }
    class ColorEaterBuffData : BuffData
    {
        public override BuffID ID => ColorEaterBuffEntry.ColorEaterID;

    }
    class ColorEaterBuffEntry : IBuffEntry
    {
        public static BuffID ColorEaterID = new BuffID("ColorEaterID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ColorEaterBuff, ColorEaterBuffData, ColorEaterBuffEntry>(ColorEaterID);
        }

        public static void HookOn()
        {
            On.Player.Update += Player_Update;
            On.Player.ShortCutColor += Player_ShortCutColor;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        }

        private static void ChangeColor(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser)
        {
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if (i != 9)
                {
                    sLeaser.sprites[i].color = self.player.EatPlate().MainColor;
                }
            }
            var mesh = sLeaser.sprites[2] as TriangleMesh;
            if (mesh != null && mesh.customColor)
            {
                for (int i = 0; i < mesh.verticeColors.Length; i++)
                {
                    mesh.verticeColors[i] = self.player.EatPlate().MainColor;
                }
            }
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);

            //ChangeColor(self, sLeaser);
        }

        private static Color Player_ShortCutColor(On.Player.orig_ShortCutColor orig, Player self)
        {
            return (self.EatPlate().MainColor);
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (self.room == null || !self.Consious)
                return;

            var plateData = self.EatPlate();
            //趴下按下吸色
            if (self.bodyMode == Player.BodyModeIndex.Crawl && self.input[0].y < 0)
            {
                var color = self.room.game.cameras[0].PixelColorAtCoordinate(self.firstChunk.pos - new Vector2(0, -20));

                self.EatPlate().lerpTo(color, 0.1f);
            }

            //可以让圣徒用舌头吸色
            if (self.tongue!=null&&self.tongue.Attached)
            {
                var color = self.room.game.cameras[0].PixelColorAtCoordinate(self.tongue.AttachedPos);

                self.EatPlate().lerpTo(color, 0.1f);
            }

            //让匹配吸色后玩家的饱食度
            self.slugcatStats.foodToHibernate = Convert.ToInt32(Mathf.Lerp(0, self.slugcatStats.maxFood, plateData.InvertLerpColor()));

            //如果饱食度显示不匹配就刷新显示
            if (self.room.game.cameras.Any())
            {
                if (self.room.game.cameras[0].hud.foodMeter.survivalLimit != self.slugcatStats.foodToHibernate)
                {
                    self.room.game.cameras[0].hud.foodMeter.survivalLimit = self.slugcatStats.foodToHibernate;
                    self.room.game.cameras[0].hud.foodMeter.RefuseFood();
                    self.PlayHUDSound(SoundID.HUD_Food_Meter_Fill_Plop_A);
                }
            }


        }
    }
    public class ColorPlateData
    {
        public Color targetColor;
        public Player player;
        public Color MainColor;

        public bool refresh = true;

        public float InvertLerpColor()
        {
            float targetH = Custom.RGB2HSL(targetColor).x;
            float mainH = Custom.RGB2HSL(MainColor).x;

            return Mathf.Min(Mathf.Abs(targetH - mainH), Mathf.Abs(targetH - mainH + 1));

        }
        public void lerpTo(Color target, float t)
        {
            MainColor = Color.Lerp(MainColor, target, t);
            MainColor = Custom.HSL2RGB(Custom.RGB2HSL(MainColor).x, 0.4f, 0.5f);
        }
        public ColorPlateData(Player player)
        {
            this.targetColor = Custom.HSL2RGB(Random.value, 0.4f, 0.5f);
            this.MainColor = Custom.HSL2RGB(Random.value, 0.4f, 0.5f);

            this.player = player;
        }
        public void Update()
        {


        }

    }
    public static class ColorPlate
    {
        private static readonly ConditionalWeakTable<Player, ColorPlateData> modules = new ConditionalWeakTable<Player, ColorPlateData>();

        public static ColorPlateData EatPlate(this Player player)
        {
            return modules.GetValue(player, (Player p) => new ColorPlateData(p));
        }

    }


}