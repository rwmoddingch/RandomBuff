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
using System.Drawing;

namespace BuiltinBuffs.Positive //命名空间在BuiltinBuffs的Positive下
{
    internal class TurboPropulsionIBuffEntry : IBuffEntry
    {
        /*-----------------------------------------------------字段-----------------------------------------------------*/
        //设置BuffID
        public static BuffID turboPropulsionBuffID = new BuffID("TurboPropulsion", true);
        //是否处于推进状态，只是为了方便判断
        public static bool isPropelling = false;
        //推进等级，决定加速度和粒子数量
        public static float propulsionLevel = 0f;
        //推进速度上限，超过这个速度时只产生视觉效果而不再实际加速
        public static float proSpeedLimit = 20f;

        //粒子发射器
        public static ParticleEmitter emitter = null;
        //粒子发射器工作判断
        public static bool emittering = false;
        //尾巴转动系数
        public static int tailT = 0;
        /*-----------------------------------------------------挂钩-----------------------------------------------------*/
        public static void HookOn()
        {
            //实现旋转尾巴推进
            On.Player.Update += Player_Update;
        }
        /*-----------------------------------------------------方法-----------------------------------------------------*/
        //实现除了粒子效果外的所有功能
        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            //原版更新方法
            orig.Invoke(self, eu);

            if (self.room == null || self.inShortcut) return;

            //如果玩家意识清醒 且在水下或无重力 那么“允许推进”
            bool canPropel = self.Consious && (self.submerged || self.bodyMode == Player.BodyModeIndex.ZeroG);
            //如果允许推进 且氧气量大于0.5 且按下跳跃键 但还没在推进 那么“可以开始推进”
            if (canPropel && self.input[0].jmp && self.airInLungs > 0.5f) { isPropelling = true; }
            //如果允许推进 且氧气量大于0.1 且按下跳跃键 且已经在推进 那么“可以继续推进”
            if (!canPropel || self.airInLungs < 0.1f || !self.input[0].jmp) { isPropelling = false; }

            //玩家方向输入为向量,记得归一化不然斜向加速1.414倍
            Vector2 inputdir = new Vector2(self.input[0].x, self.input[0].y).normalized;
            //玩家身体朝向为向量，真的只是方向...0是头1是身体是吧 向量减法怎么算来着....
            Vector2 bodydir = (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized;
            //经典的房间和位置，但是这次位置是尾部而不是头，因为粒子是尾部喷出来的
            Room room = self.room;
            Vector2 pos = self.bodyChunks[1].pos;
            Vector2 vel = self.bodyChunks[1].vel;

            //推进基本效果
            if (isPropelling)
            {
                //提升推进等级
                propulsionLevel += 1f;

                //实际加速,如果没有方向输入则保持方向
                if (inputdir != Vector2.zero)
                { self.bodyChunks[0].vel += inputdir * propulsionLevel * 0.02f; }
                else
                { self.bodyChunks[0].vel += bodydir * propulsionLevel * 0.02f; }

                //限制最大速度
                if (self.bodyChunks[0].vel.magnitude > proSpeedLimit)
                { self.bodyChunks[0].vel = self.bodyChunks[0].vel.normalized * proSpeedLimit; }
            }
            //加速等级衰减
            else if (propulsionLevel > 0)
            { propulsionLevel -= 2f; }
            //加速等级不小于0
            if (propulsionLevel < 0)
            { propulsionLevel = 0; }
            //限制推进等级
            if (propulsionLevel > 100f)
            { propulsionLevel = 100f; }
            //未推进时氧气自然回复
            if (self.submerged && self.airInLungs < 0.95f && !isPropelling)
                self.airInLungs += 0.003f;

            //视觉效果实现
            //水下气泡
            if (isPropelling)
            {
                //有百分之（等级*0.45）的概率为true
                if (UnityEngine.Random.Range(0, 100) < propulsionLevel * 0.45)
                { 
                    room.AddObject(new Bubble((pos + 15 * Custom.RNV()), -bodydir * (3f + propulsionLevel * 0.15f), false, false)); 
                }            
            }
            //附加粒子效果
            if (self.slugcatStats.name == new SlugcatStats.Name("TheTraveler"))
            {
                //发射器为空但房间不为空-生成发射器
                if (emitter == null && room != null)
                {
                    emitter = new ParticleEmitter(room);
                }
                //发射器和房间不匹配-重新生成发射器
                else if (room != null && emitter.room != room)
                {
                    emitter.Die();
                    emitter = new ParticleEmitter(room);
                }
                //发射器和房间匹配
                if (room != null && emitter.room == room)
                {
                    //开始推进但未产出粒子（初始化）
                    if (isPropelling & !emittering)
                    {
                        //确定开始产出粒子
                        emittering = true;
                        //重新生成发射器，基础赋值
                        emitter.Die();
                        emitter = new ParticleEmitter(room);
                        emitter.vel = vel;
                        emitter.lastPos = emitter.pos = pos;
                        //发射器设置
                        emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 5, true));
                        emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, 1));
                        emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("EndGameCircle", "", 8, 0.85f)));
                        emitter.ApplyParticleModule(new SetRandomScale(emitter, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)));
                        emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
                        emitter.ApplyParticleModule(new SetRandomLife(emitter, 20, 20));
                        emitter.ApplyParticleModule(new SetConstColor(emitter, UnityEngine.Color.HSVToRGB(0.60f, 0.35f, 0.95f)));
                        emitter.ApplyParticleModule(new SetConstVelociy(emitter, -bodydir));
                        emitter.ApplyParticleModule(new SetRandomPos(emitter, 0f));
                        emitter.ApplyParticleModule(new AlphaOverLife(emitter, (p, l) =>
                        {
                            if (l > 0.5f)
                                return (1f - l) * 2f;
                            else
                                return 1f;
                        }));
                        emitter.ApplyParticleModule(new ScaleXYOverLife(emitter, (p, l) =>
                        {
                            return new Vector2(1f - 0.5f * l, (0.5f - 0.2f * l) * (1f - 0.5f * l));
                        }));
                        emitter.ApplyParticleModule(new ColorOverLife(emitter, (p, l) =>
                        {
                            return UnityEngine.Color.HSVToRGB(0.60f - 0.3f * l, 0.35f + 0.25f * l, 0.95f - 0.45f * l);
                        }));
                        //启动————
                        ParticleSystem.ApplyEmitterAndInit(emitter);
                    }
                    //开始推进且产出粒子（更新状态）
                    if (isPropelling && emittering)
                    {
                        //更新速度和位置，重设粒子速度和旋转
                        emitter.vel = vel;
                        emitter.lastPos = emitter.pos = pos;
                        emitter.ApplyParticleModule(new SetRandomRotation(emitter, Custom.VecToDeg(bodydir), Custom.VecToDeg(bodydir)));
                        emitter.ApplyParticleModule(new SetConstVelociy(emitter, -bodydir));
                    }
                    //未在推进但产出粒子（清除）
                    if (!isPropelling && emittering)
                    {
                        //清除发射器
                        emitter.Die();
                        emittering = false;
                    }
                }
            }
            else
            {
                //发射器为空但房间不为空-生成发射器
                if (emitter == null && room != null)
                {
                    emitter = new ParticleEmitter(room);
                }
                //发射器和房间不匹配-重新生成发射器
                else if (room != null && emitter.room != room)
                {
                    emitter.Die();
                    emitter = new ParticleEmitter(room);
                }
                //发射器和房间匹配
                if (room != null && emitter.room == room)
                {
                    //开始推进但未产出粒子（初始化）
                    if (isPropelling & !emittering)
                    {
                        //确定开始产出粒子
                        emittering = true;
                        //重新生成发射器，基础赋值
                        emitter.Die();
                        emitter = new ParticleEmitter(room);
                        emitter.vel = vel;
                        emitter.lastPos = emitter.pos = pos;
                        //发射器设置
                        emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 2, true));
                        emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, 1));
                        emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "", 8, 0.85f)));
                        emitter.ApplyParticleModule(new SetRandomScale(emitter, new Vector2(0.25f, 1f), new Vector2(0.25f, 1f)));
                        emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
                        emitter.ApplyParticleModule(new SetRandomLife(emitter, 20, 20));
                        emitter.ApplyParticleModule(new SetConstColor(emitter, UnityEngine.Color.HSVToRGB(0.60f, 0.2f, 0.95f)));
                        emitter.ApplyParticleModule(new SetConstVelociy(emitter, -bodydir));
                        emitter.ApplyParticleModule(new SetRandomPos(emitter, 20f));
                        emitter.ApplyParticleModule(new AlphaOverLife(emitter, (p, l) =>
                        {
                            if (l > 0.5f)
                                return (1f - l) * 2f;
                            else
                                return 1f;
                        }));
                        emitter.ApplyParticleModule(new ScaleXYOverLife(emitter, (p, l) =>
                        {
                            return new Vector2(0.25f * (1f - 0.5f * l),1f - 0.5f * l);
                        }));
                        emitter.ApplyParticleModule(new ColorOverLife(emitter, (p, l) =>
                        {
                            return UnityEngine.Color.HSVToRGB(0.60f - 0.3f * l, 0.2f + l * 0.3f, 0.95f - 0.45f * l);
                        }));
                        //启动————
                        ParticleSystem.ApplyEmitterAndInit(emitter);
                    }
                    //开始推进且产出粒子（更新状态）
                    if (isPropelling && emittering)
                    {
                        //更新速度和位置，重设粒子速度和旋转
                        emitter.vel = vel;
                        emitter.lastPos = emitter.pos = pos;
                        emitter.ApplyParticleModule(new SetRandomRotation(emitter, Custom.VecToDeg(bodydir), Custom.VecToDeg(bodydir)));
                        emitter.ApplyParticleModule(new SetConstVelociy(emitter, -bodydir * 3f));
                    }
                    //未在推进但产出粒子（清除）
                    if (!isPropelling && emittering)
                    {
                        //清除发射器
                        emitter.Die();
                        emittering = false;
                    }
                }
            }


            //尾巴转转（！）部分参考自TailCopter
            var tailBody = self.bodyChunks[1];
            if ((self.graphicsModule) is PlayerGraphics graphics)
            {
                var allTail = graphics.tail;
                if (self.slugcatStats.name != new SlugcatStats.Name("TheTraveler"))
                {
                    if (isPropelling && propulsionLevel > 0)
                    {
                        var y = Custom.DirVec(self.bodyChunks[0].pos, self.bodyChunks[1].pos);
                        var x = Custom.PerpendicularVector(y);
                        //似乎是幅度...?
                        float size = 20;
                        var rotatePos = tailBody.pos + y * 30; //中轴的位置
                        rotatePos += ((float)Math.Sin(tailT * propulsionLevel * 0.01f)) * x * size;

                        for (int i = 2; i < allTail.Length; i++)
                        {
                            float magnification = (i + 1) / allTail.Length;
                            allTail[i].vel += Custom.DirVec(allTail[i].pos, rotatePos) *
                                              Custom.LerpMap(Vector2.Distance(allTail[i].pos, rotatePos), 1, 100, 1,
                                                  100 * magnification);
                        }

                        //音效暂时弃用
                        //self.room.PlaySound(SoundID.Bat_Idle_Flying_Sounds, self.bodyChunks[1]);
                        tailT += 1;
                    }
                    else
                    {
                        tailT = 0;
                    }
                }
            }

        }
        public void OnEnable()
        {
            //注册BuffID
            BuffRegister.RegisterBuff<TurboPropulsionIBuffEntry>(
                turboPropulsionBuffID);
        }

        //可控制XY的粒子比例控制
        public class ScaleXYOverLife : EmitterModule, IParticleUpdateModule
        {
            Func<Particle, float, Vector2> scaleFunc;
            public ScaleXYOverLife(ParticleEmitter emitter, Func<Particle, float, Vector2> scaleFunc) : base(emitter)
            {
                this.scaleFunc = new Func<Particle, float, Vector2>((p, life) =>
                {
                    Vector2 scaleXY = scaleFunc.Invoke(p, life);

                    return scaleXY;
                });
            }
            public void ApplyUpdate(Particle particle)
            {
                particle.scaleXY = scaleFunc.Invoke(particle, particle.LifeParam);
            }
        }
    }
}
