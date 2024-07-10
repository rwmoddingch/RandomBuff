using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff;
using RandomBuff.Core.Buff;
using RWCustom;
using UnityEngine;

namespace BuiltinBuffs.Negative.SephirahMeltdown
{
    internal class MalkuthBuffData : SephirahMeltdownBuffData
    {
        public override BuffID ID => Malkuth;

        public static readonly BuffID Malkuth = new BuffID(nameof(Malkuth), true);

    }

    internal class MalkuthBuff : Buff<MalkuthBuff,MalkuthBuffData>
    {
        public override BuffID ID => MalkuthBuffData.Malkuth;
    }

    internal class MalkuthHook
    {
        public static void HookOn()
        {
            On.ShortcutGraphics.ShortCutColor += ShortcutGraphics_ShortCutColor;
     
            On.HUD.KarmaMeter.Draw += KarmaMeter_Draw;
            On.Player.checkInput += Player_checkInput;

            On.HUD.FoodMeter.Draw += FoodMeter_Draw;
            On.HUD.TextPrompt.Draw += TextPrompt_Draw;
            On.HUD.RainMeter.Draw += RainMeter_Draw;
        }

        private static void RainMeter_Draw(On.HUD.RainMeter.orig_Draw orig, HUD.RainMeter self, float timeStacker)
        {
            orig(self, timeStacker);
            foreach (var h in self.circles)
                h.sprite.isVisible = false;
        }

    
        private static void TextPrompt_Draw(On.HUD.TextPrompt.orig_Draw orig, HUD.TextPrompt self, float timeStacker)
        {
            orig(self,timeStacker);
            foreach (var sprite in self.sprites)
                sprite.isVisible = false;
        }

        private static void FoodMeter_Draw(On.HUD.FoodMeter.orig_Draw orig, HUD.FoodMeter self, float timeStacker)
        {
            orig(self, timeStacker);
            foreach (var circle in self.circles)
            {
                foreach(var h in circle.circles)
                    h.sprite.isVisible = false;
                circle.gradient.isVisible = false;
            }
            self.lineSprite.isVisible = false;
        }

        private static void KarmaMeter_Draw(On.HUD.KarmaMeter.orig_Draw orig, HUD.KarmaMeter self, float timeStacker)
        {
            orig(self,timeStacker);
            self.glowSprite.isVisible = false;
            self.karmaSprite.isVisible = false;
            if (self.darkFade != null)
                self.darkFade.isVisible = false;
            if (self.ringSprite != null)
             self.ringSprite.isVisible = false;
        }

        private static void Player_checkInput(On.Player.orig_checkInput orig, Player self)
        {
            orig(self);
            if (MalkuthBuffData.Malkuth.GetBuffData<MalkuthBuffData>()?.CycleUse >= 1)
                self.input[0].mp = false;
        }



        private static Color ShortcutGraphics_ShortCutColor(On.ShortcutGraphics.orig_ShortCutColor orig, ShortcutGraphics self, Creature crit, IntVector2 pos)
        {
            return MalkuthBuffData.Malkuth.GetBuffData<MalkuthBuffData>().CycleUse >= 2 ? Color.black : orig(self, crit, pos);
        }
    }



}
