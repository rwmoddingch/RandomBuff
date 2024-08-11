using System;
using MonoMod;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Runtime.CompilerServices;
using RWCustom;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using HotDogGains.Positive;
using RandomBuff;
using BuiltinBuffs;
using RandomBuffUtils;
using BuiltinBuffs.Positive;

namespace TemplateGains
{
    //尾巴直升机
    public class Tail : PlayerUtils.PlayerModulePart
    {
        public bool HaveTail = true;
    }

    class TailcopterBuff : Buff<TailcopterBuff, TailcopterBuffData> { public override BuffID ID => TailcopterBuffEntry.TailcopterID; }
    class TailcopterBuffData : BuffData { public override BuffID ID => TailcopterBuffEntry.TailcopterID; }
    class TailcopterBuffEntry : IBuffEntry
    {
        public static BuffID TailcopterID = new BuffID("TailcopterID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<TailcopterBuff, TailcopterBuffData, TailcopterBuffEntry>(TailcopterID);
        }
        public static ConditionalWeakTable<Player, Tailcopter> modules = new ConditionalWeakTable<Player, Tailcopter>();
        public static void HookOn()
        {
            On.Player.MovementUpdate += Player_MovementUpdate;
        }


        private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            
            if (!self.GetExPlayerData().HaveTail || self.inShortcut || self.room == null || self.graphicsModule == null) return;

            //if (PlayerUtils.TryGetModulePart<Tail,TailcopterBuffEntry>(self,out var modulePart))
            //{
            //    if (!modulePart.HaveTail) return;
            //}

            if (CatNeuroBuffEntry.CatNeuroID.GetBuffData() != null) return;
            if (GeckoStrategyBuffEntry.geckoModule.TryGetValue(self, out var module) &&!( module.escapeCount >= 0 && !self.playerState.permaDead))
            {
                return;
            }



            var copter = self.copter();

            var tailBody = self.bodyChunks[1];

            var allTail = ((self.graphicsModule) as PlayerGraphics).tail;
            var tail = ((self.graphicsModule) as PlayerGraphics).tail[((self.graphicsModule) as PlayerGraphics).tail.Length - 1];

            if (self.input[0].jmp && !self.input[1].jmp && tailBody.contactPoint.y >= 0)//空中按跳提升飞行计数
            {
                copter.flyCount += 20;
                if (copter.flyCount > copter.flyLimit) copter.flyCount = copter.flyLimit;//如果超过上限就等于上限

                tailBody.vel += Custom.DirVec(self.bodyChunks[0].pos, self.bodyChunks[1].pos) * 0.2f;
            }
            //else if (self.input[0].jmp && tailBody.contactPoint.y >= 0)//长按按跳提升飞行计数
            //{
            //    copter.flyCount += 1;
            //    if (copter.flyCount > copter.flyLimit) copter.flyCount = copter.flyLimit;//如果超过上限就等于上限

            //    tailBody.vel += Custom.DirVec(self.bodyChunks[0].pos, self.bodyChunks[1].pos) * 0.01f;
            //}

            else if (copter.flyCount > 0) copter.flyCount--;//没在飞就让计数归位
            if (self.input[0].y<0&&self.room.gravity>0)copter.flyCount--;//让玩家按下的时候更快停止尾巴转动

            copter.flying = Custom.LerpMap(copter.flyCount, 0, copter.flyLimit / 2f, 0, 1f);
            //飞行计数提升时改变飞行程度


            //抵消房间的重力
            if (copter.flying > 0.5f)
            {
                self.standing = false;//起飞后让人弯腰

                if (self.input[0].y > 0)
                {
                    self.noGrabCounter = 3;//防止向上爬的时候抓杆子
                }

                tailBody.vel *= 0.95f;//在空中稍微减速提高操作性
            }

            foreach (var body in self.bodyChunks)
            {
                float g = self.room.gravity;//玩家房间的重力
                g *= copter.flying;//乘上飞行程度的倍率

                tailBody.vel.y += g;
            }//根据飞行程度抵消重力


            if (copter.flying > 0 && copter.flyCount > 0)
            {
                var y = Custom.DirVec(self.bodyChunks[0].pos, self.bodyChunks[1].pos);
                var x = Custom.PerpendicularVector(y);

                float size = 40;
                var rotatePos = tailBody.pos + y * 15;//中轴的位置
                rotatePos += ((float)Math.Sin(copter.t * copter.flying)) * x * size;

                for (int i = 2; i < allTail.Length; i++)
                {
                    float magnification = (i + 1) / allTail.Length;
                    allTail[i].vel += Custom.DirVec(allTail[i].pos, rotatePos) * Custom.LerpMap(Vector2.Distance(allTail[i].pos, rotatePos), 1, 100, 1, 100 * magnification);
                }
                self.room.PlaySound(SoundID.Bat_Idle_Flying_Sounds, self.bodyChunks[1]);
                copter.t++;
            }//尾巴摇起来
            else { copter.t = 0; }



            if (copter.flying > 0.7f)
            {
                self.bodyChunks[0].vel += self.input[0].analogueDir * copter.flying * 1f;
                self.bodyChunks[1].vel -= self.input[0].analogueDir * copter.flying * 0.2f;
            }//提供更强e移动力


        }

    }
    public static class ExPlaye
    {
        public static ConditionalWeakTable<Player, Tailcopter> modules = new ConditionalWeakTable<Player, Tailcopter>();
        public static Tailcopter copter(this Player player) => modules.GetValue(player, (Player p) => new Tailcopter(player));
    }
    public class Tailcopter
    {
        public Player self;

        public int flyCount = 0;
        public int flyLimit = 160;

        internal float flying;
        internal int t;

        public Tailcopter(Player self)
        {
            this.self = self;
        }
    }






}
