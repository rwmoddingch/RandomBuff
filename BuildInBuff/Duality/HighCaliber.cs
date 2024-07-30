/*
        _            _            _           _            _          
       / /\         /\ \         /\ \        /\ \         /\ \     _  
      / /  \       /  \ \       /  \ \      /  \ \       /  \ \   /\_\
     / / /\ \__   / /\ \ \     / /\ \ \    / /\ \ \     / /\ \ \_/ / /
    / / /\ \___\ / / /\ \_\   / / /\ \_\  / / /\ \ \   / / /\ \___/ / 
    \ \ \ \/___// /_/_ \/_/  / / /_/ / / / / /  \ \_\ / / /  \/____/  
     \ \ \     / /____/\    / / /__\/ / / / /   / / // / /    / / /   
 _    \ \ \   / /\____\/   / / /_____/ / / /   / / // / /    / / /    
/_/\__/ / /  / / /______  / / /\ \ \  / / /___/ / // / /    / / /     
\ \/___/ /  / / /_______\/ / /  \ \ \/ / /____\/ // / /    / / /      
 \_____\/   \/__________/\/_/    \_\/\/_________/ \/_/     \/_/       

*/
using UnityEngine;
using Expedition;
using MoreSlugcats;
using RWCustom;
using RandomBuff;
using RandomBuffUtils;
using Smoke;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;

namespace BuiltinBuffs.Duality //命名空间在BuiltinBuffs的Duality下
{
    internal class HighCaliberIBuffEntry : IBuffEntry
    {
        /*-----------------------------------------------------字段-----------------------------------------------------*/
        //设置BuffID
        public static BuffID highCaliberBuffID = new BuffID("HighCaliber", true);
        /*-----------------------------------------------------挂钩-----------------------------------------------------*/
        public static void HookOn()
        {
            //判断投掷（播放发射效果）
            On.Player.ThrowObject += Player_Throwblast;
            //炸弹被投出（如果玩家投出就加速）
            On.ScavengerBomb.Thrown += ScavengerBomb_Thrown;
            //炸弹触地（加大爆炸范围和威力）
            On.ScavengerBomb.Explode += ScavengerBomb_Explode;
        }

        /*-----------------------------------------------------方法-----------------------------------------------------*/
        //发射效果实现
        private static void Player_Throwblast(On.Player.orig_ThrowObject orig,Player self,int grasp,bool eu)
        {            
            //获取房间和位置和速度
            var room = self.room;
            var pos = self.mainBodyChunk.pos;           
            //获取物品投掷方向
            //获取水平投掷方向
            IntVector2 throwDirection = new IntVector2(self.ThrowDirection, 0);
            //判断上下投
            bool inputY = self.input[0].y < 0;
            if(ModManager.MMF && MMF.cfgUpwardsSpearThrow.Value)
            {
                inputY = (self.input[0].y != 0);
            }
            //合成投掷方向向量(那个地方是写-1吗...?）
            if (self.animation == Player.AnimationIndex.Flip && inputY && self.input[0].x == 0)
            {
                throwDirection = new IntVector2(0, (ModManager.MMF && MMF.cfgUpwardsSpearThrow.Value) ? self.input[0].y : -1);
            }
            //零重力的情况
            if(ModManager.MMF && self.bodyMode == Player.BodyModeIndex.ZeroG && MMF.cfgUpwardsSpearThrow.Value)
            {
                int y = self.input[0].y;
                if(y != 0)
                {
                    throwDirection = new IntVector2(0, y);
                }
                else
                {
                    throwDirection = new IntVector2(self.ThrowDirection, 0);
                }
            }
            //?????这样的真的可以吗 能转换过去吗.....
            Vector2 throwDir = new Vector2(throwDirection.x, throwDirection.y);
            //如果你丢了个炸弹出来....
            if (self.grasps[grasp].grabbed is ScavengerBomb )
            {
                //后坐力实现 -60？
                self.bodyChunks[1].vel += (throwDir * -60);

                //生成烟雾
                FireSmoke smoke = new FireSmoke(room);
                FireSmoke smoke1 = new FireSmoke(room);
                room.AddObject(smoke);
                room.AddObject(smoke1);
                //循环发射烟雾
                for (int i = 0; i < 60; i++)
                {
                    smoke.EmitSmoke(pos, 0.5f * Vector2.up + (throwDir * UnityEngine.Random.value * 85f), new Color(1f, 0.55f, 0f), 10);
                }
                for (int i = 0; i < 15; i++)
                {
                    smoke1.EmitSmoke(pos, 0.1f * Vector2.up + (throwDir * UnityEngine.Random.value * 35f), new Color(1f, 0.85f, 0f), 40);
                }
                smoke.Destroy();
                smoke1.Destroy();
                //炮口碎屑（未实现

                //冲击波,视觉效果和震动
                room.AddObject(new ShockWave(pos, 250f + UnityEngine.Random.value * 50f, 0.025f, 20, false));
                room.AddObject(new ExplosionSpikes(room, pos, 14, 35f, 10f, 10f, 40f, new Color(0f, 0f, 0f)));
                room.ScreenMovement(pos, throwDir, 20f);
                //音效
                room.PlaySound(SoundID.Bomb_Explode, pos, 2.5f + UnityEngine.Random.value * 0.5f, 0.4f + UnityEngine.Random.value * 0.4f);
            }
            //先判断丢了什么要不要加特效 然后再真的丢出去
            orig.Invoke(self, grasp, eu);
        }
        //加速效果实现
        private static void ScavengerBomb_Thrown(On.ScavengerBomb.orig_Thrown orig, ScavengerBomb self, Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
        {
            orig.Invoke(self, thrownBy, thrownPos,firstFrameTraceFromPos,throwDir,frc, eu);
            if(thrownBy is Player)
            {
                    self.firstChunk.vel = new Vector2(self.firstChunk.vel.x * 5f, self.firstChunk.vel.y);
            }            
        }
        //触地效果实现
        private static void ScavengerBomb_Explode(On.ScavengerBomb.orig_Explode orig, ScavengerBomb self, BodyChunk hitChunk)
        {
            //获取房间和位置
            Room room = self.room;
            Vector2 pos = self.firstChunk.pos;
            Vector2 vel = self.firstChunk.vel;
            //如果是由玩家发射的
            if(self.thrownBy is Player)
            {
                //152mm榴弹爆炸-
                room.AddObject(new Explosion(room, self, pos, 7, 325f, 6f, 200f, 280f, 0f, self.thrownBy, 0.7f, 160f, 1f));
                //闪光效果-
                room.AddObject(new Explosion.ExplosionLight(pos, 1500f, 1f, 5, new Color(1f, 1f, 1f)));
                //尖刺效果-
                room.AddObject(new ExplosionSpikes(room, pos, 20, 175, 30f, 20f, 300f, new Color(0f, 0f, 0f)));
                //冲击波效果-
                room.AddObject(new ShockWave(pos, 1500f, 0.045f, 50));
            }
            //正常的小爆炸
            orig.Invoke(self, hitChunk);
        }
        public void OnEnable()
        {
            //注册BuffID
            BuffRegister.RegisterBuff<HighCaliberIBuffEntry>(
                highCaliberBuffID);
        }
    }
}
