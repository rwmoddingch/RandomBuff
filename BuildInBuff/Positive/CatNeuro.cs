using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System.CodeDom;
using System.Dynamic;
using UnityEngine;
using MoreSlugcats;
using RandomBuff.Core.Game;
using RandomBuff;
using BuiltinBuffs;

namespace HotDogGains.Positive
{
    public static class EXCatNeuro
    {
        public static ConditionalWeakTable<PlayerGraphics,CatNeuro> modules= new ConditionalWeakTable<PlayerGraphics, CatNeuro>();
        public static CatNeuro neuroCatLight(this PlayerGraphics self)
        {
               return modules.GetValue(self, (PlayerGraphics s) => new CatNeuro());
        } 
    }
    public class CatNeuro
    {
        public LightSource light;
    }
    class CatNeuroBuff : Buff<CatNeuroBuff, CatNeuroBuffData> { public override BuffID ID => CatNeuroBuffEntry.CatNeuroID; }
    class CatNeuroBuffData : BuffData { public override BuffID ID => CatNeuroBuffEntry.CatNeuroID; }
    class CatNeuroBuffEntry : IBuffEntry
    {
        public static BuffID CatNeuroID = new BuffID("CatNeuroID", true);
        
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<CatNeuroBuff, CatNeuroBuffData, CatNeuroBuffEntry>(CatNeuroID);
        }
        public static void HookOn()
        {
            On.PlayerGraphics.DrawSprites+= HideTail;
            On.PlayerGraphics.Update += CatNeuroLight;
            On.Player.Update += Player_Update;
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            self.GetExPlayerData().HaveTail = false;
        }

        private static void HideTail(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self,sLeaser,rCam,timeStacker,camPos);
            sLeaser.sprites[2].alpha=0;
        }

        private static void CatNeuroLight(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig.Invoke(self);
            if (self.neuroCatLight().light != null)
            {
                self.neuroCatLight().light.stayAlive = true;
                self.neuroCatLight().light.setRad = new float?(400f*(1+CatNeuroID.GetBuffData().StackLayer));
                self.neuroCatLight().light.setPos = new Vector2?(self.player.mainBodyChunk.pos);
                if (self.neuroCatLight().light.slatedForDeletetion || self.player.room.Darkness(self.player.mainBodyChunk.pos) == 0f)
                {
                    self.neuroCatLight().light = null;
                }
            }
            else if (self.player.room.Darkness(self.player.mainBodyChunk.pos) > 0f && !self.player.DreamState)
            {
                self.neuroCatLight().light = new LightSource(self.player.mainBodyChunk.pos, false, Color.Lerp(new Color(1f, 1f, 1f), (ModManager.MSC && self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Slugpup) ? self.player.ShortCutColor() : PlayerGraphics.SlugcatColor(self.CharacterForColor), 0.5f), self.player)
                {
                    requireUpKeep = true,
                    setRad = new float?(400f*(1+CatNeuroID.GetBuffData().StackLayer)),
                    setAlpha = new float?(1f)
                };
                self.player.room.AddObject(self.neuroCatLight().light);
            }
        }
    }

}