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
using HotDogGains.Positive;
using BuiltinBuffs.Negative;
using System.Runtime.CompilerServices;

namespace BuiltinBuffs.Positive //命名空间在BuiltinBuffs的Positive下
{
    internal class TurboPropulsionBuff : Buff<TurboPropulsionBuff, TurboPropulsionBuffData>
    {
        public override BuffID ID => TurboPropulsionIBuffEntry.turboPropulsionBuffID;
        //对所有玩家添加到CWT并建立Turbo对象
        public TurboPropulsionBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    var turbo = new Turbo(player);
                    TurboPropulsionIBuffEntry.TurboFeatures.Add(player, turbo);
                }
            }
        }
    }
    internal class TurboPropulsionBuffData : BuffData
    {
        public override BuffID ID => TurboPropulsionIBuffEntry.turboPropulsionBuffID;
    }
    internal class TurboPropulsionIBuffEntry : IBuffEntry
    {
        /*-----------------------------------------------------字段-----------------------------------------------------*/
        //设置BuffID
        public static BuffID turboPropulsionBuffID = new BuffID("TurboPropulsion", true);
        //推进速度上限，超过这个速度时只产生视觉效果而不再实际加速
        public static float proSpeedLimit = 20f;
        //CWT,用来存玩家和turbo（作为数据储存）的对应关系
        public static ConditionalWeakTable<Player, Turbo> TurboFeatures = new ConditionalWeakTable<Player, Turbo>();

        /*
        //是否处于推进状态，只是为了方便判断
        public static bool isPropelling = false;
        //推进等级，决定加速度和粒子数量
        public static float propulsionLevel = 0f;
        //粒子发射器
        public static ParticleEmitter emitter = null;
        //粒子发射器工作判断
        public static bool emittering = false;
        //尾巴转动系数
        public static int tailT = 0;
        */
        /*-----------------------------------------------------挂钩-----------------------------------------------------*/
        public static void HookOn()
        {
            //实现旋转尾巴推进
            On.Player.Update += Player_Update;
            //在玩家创建时再检查一下并对所有还没进表的对象进表
            On.Player.ctor += Player_ctor;
        }
        /*-----------------------------------------------------方法-----------------------------------------------------*/
        //玩家创建时进表
        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!TurboFeatures.TryGetValue(self, out _))
                TurboFeatures.Add(self, new Turbo(self));
        }
        //实现除了粒子效果外的所有功能
        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            //原版更新方法
            orig.Invoke(self, eu);
            //这里只是把所有原本的功能都塞进了if下面的大括号里 并且把"共用的静态字段”变成“每个玩家自己对应的turbo里面的字段”
            //防止多玩家出bug，而且以后可以回来抄这里的写法（？
            if (TurboFeatures.TryGetValue(self, out var turbo))
            {

                //没尾巴就直接删粒子再Return跳过之后的步骤
                if (!self.GetExPlayerData().HaveTail || self.inShortcut || self.room == null || self.graphicsModule == null)
                {
                    //!!!这边出现过一次CS0176，也就是说静态字段不需要实例就能访问且只通过类型名访问
                    //但改用Turbo类来存储数据后，就要通过turbo实例访问相应的值了，也就不能是静态字段了
                    if (turbo.emitter != null)
                    {
                        turbo.emitter.Die();
                    }
                    return;
                }

                if (CatNeuroBuffEntry.CatNeuroID.GetBuffData() != null)
                {
                    if (turbo.emitter != null)
                    {
                        turbo.emitter.Die();
                    }
                    return;
                }

                if (GeckoStrategyBuffEntry.geckoModule.TryGetValue(self, out var module) && !(module.escapeCount >= 0 && !self.playerState.permaDead))
                {
                    if (turbo.emitter != null)
                    {
                        turbo.emitter.Die();
                    }
                    return;
                }

                //如果玩家意识清醒 且在水下或无重力 那么“允许推进”
                bool canPropel = self.Consious && (self.submerged || self.bodyMode == Player.BodyModeIndex.ZeroG);
                //如果允许推进 且氧气量大于0.5 且按下跳跃键 但还没在推进 那么“可以开始推进”
                if (canPropel && self.input[0].jmp && self.airInLungs > 0.5f) { turbo.isPropelling = true; }
                //如果允许推进 且氧气量大于0.1 且按下跳跃键 且已经在推进 那么“可以继续推进”
                if (!canPropel || self.airInLungs < 0.1f || !self.input[0].jmp) { turbo.isPropelling = false; }

                //玩家方向输入为向量,记得归一化不然斜向加速1.414倍
                Vector2 inputdir = new Vector2(self.input[0].x, self.input[0].y).normalized;
                //玩家身体朝向为向量，真的只是方向...0是头1是身体是吧 向量减法怎么算来着....
                Vector2 bodydir = (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized;
                //经典的房间和位置，但是这次位置是尾部而不是头，因为粒子是尾部喷出来的
                Room room = self.room;
                Vector2 pos = self.bodyChunks[1].pos;
                Vector2 vel = self.bodyChunks[1].vel;

                //推进基本效果
                if (turbo.isPropelling)
                {
                    //提升推进等级
                    turbo.propulsionLevel += 1f;

                    //实际加速,如果没有方向输入则保持方向
                    if (inputdir != Vector2.zero)
                    { self.bodyChunks[0].vel += inputdir * turbo.propulsionLevel * 0.02f; }
                    else
                    { self.bodyChunks[0].vel += bodydir * turbo.propulsionLevel * 0.02f; }

                    //限制最大速度
                    if (self.bodyChunks[0].vel.magnitude > proSpeedLimit)
                    { self.bodyChunks[0].vel = self.bodyChunks[0].vel.normalized * proSpeedLimit; }
                }
                //加速等级衰减
                else if (turbo.propulsionLevel > 0)
                { turbo.propulsionLevel -= 2f; }
                //加速等级不小于0
                if (turbo.propulsionLevel < 0)
                { turbo.propulsionLevel = 0; }
                //限制推进等级
                if (turbo.propulsionLevel > 100f)
                { turbo.propulsionLevel = 100f; }
                //未推进时氧气自然回复
                if (self.submerged && self.airInLungs < 0.95f && !turbo.isPropelling)
                    self.airInLungs += 0.003f;

                //视觉效果实现
                //水下气泡
                if (turbo.isPropelling)
                {
                    //有百分之（等级*0.45）的概率为true
                    if (UnityEngine.Random.Range(0, 100) < turbo.propulsionLevel * 0.45)
                    {
                        room.AddObject(new Bubble((pos + 15 * Custom.RNV()), -bodydir * (3f + turbo.propulsionLevel * 0.15f), false, false));
                    }
                }
                //附加粒子效果
                if (self.slugcatStats.name == new SlugcatStats.Name("TheTraveler"))
                {
                    //发射器为空但房间不为空-生成发射器
                    if (turbo.emitter == null && room != null)
                    {
                        turbo.emitter = new ParticleEmitter(room);
                    }
                    //发射器和房间不匹配-重新生成发射器
                    else if (room != null && turbo.emitter.room != room)
                    {
                        turbo.emitter.Die();
                        turbo.emitter = new ParticleEmitter(room);
                    }
                    //发射器和房间匹配
                    if (room != null && turbo.emitter.room == room)
                    {
                        //开始推进但未产出粒子（初始化）
                        if (turbo.isPropelling & !turbo.emittering)
                        {
                            //确定开始产出粒子
                            turbo.emittering = true;
                            //重新生成发射器，基础赋值
                            turbo.emitter.Die();
                            turbo.emitter = new ParticleEmitter(room);
                            turbo.emitter.vel = vel;
                            turbo.emitter.lastPos = turbo.emitter.pos = pos;
                            //发射器设置
                            turbo.emitter.ApplyEmitterModule(new SetEmitterLife(turbo.emitter, 5, true));
                            turbo.emitter.ApplyParticleSpawn(new BurstSpawnerModule(turbo.emitter, 1));
                            turbo.emitter.ApplyParticleModule(new AddElement(turbo.emitter, new Particle.SpriteInitParam("EndGameCircle", "", 8, 0.85f)));
                            turbo.emitter.ApplyParticleModule(new SetRandomScale(turbo.emitter, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)));
                            turbo.emitter.ApplyParticleModule(new SetMoveType(turbo.emitter, Particle.MoveType.Global));
                            turbo.emitter.ApplyParticleModule(new SetRandomLife(turbo.emitter, 20, 20));
                            turbo.emitter.ApplyParticleModule(new SetConstColor(turbo.emitter, UnityEngine.Color.HSVToRGB(0.60f, 0.35f, 0.95f)));
                            turbo.emitter.ApplyParticleModule(new SetConstVelociy(turbo.emitter, -bodydir));
                            turbo.emitter.ApplyParticleModule(new SetRandomPos(turbo.emitter, 0f));
                            turbo.emitter.ApplyParticleModule(new AlphaOverLife(turbo.emitter, (p, l) =>
                            {
                                if (l > 0.5f)
                                    return (1f - l) * 2f;
                                else
                                    return 1f;
                            }));
                            turbo.emitter.ApplyParticleModule(new ScaleXYOverLife(turbo.emitter, (p, l) =>
                            {
                                return new Vector2(1f - 0.5f * l, (0.5f - 0.2f * l) * (1f - 0.5f * l));
                            }));
                            turbo.emitter.ApplyParticleModule(new ColorOverLife(turbo.emitter, (p, l) =>
                            {
                                return UnityEngine.Color.HSVToRGB(0.60f - 0.3f * l, 0.35f + 0.25f * l, 0.95f - 0.45f * l);
                            }));
                            //启动————
                            ParticleSystem.ApplyEmitterAndInit(turbo.emitter);
                        }
                        //开始推进且产出粒子（更新状态）
                        if (turbo.isPropelling && turbo.emittering)
                        {
                            //更新速度和位置，重设粒子速度和旋转
                            turbo.emitter.vel = vel;
                            turbo.emitter.lastPos = turbo.emitter.pos = pos;
                            turbo.emitter.ApplyParticleModule(new SetRandomRotation(turbo.emitter, Custom.VecToDeg(bodydir), Custom.VecToDeg(bodydir)));
                            turbo.emitter.ApplyParticleModule(new SetConstVelociy(turbo.emitter, -bodydir));
                        }
                        //未在推进但产出粒子（清除）
                        if (!turbo.isPropelling && turbo.emittering)
                        {
                            //清除发射器
                            turbo.emitter.Die();
                            turbo.emittering = false;
                        }
                    }
                }
                else
                {
                    //发射器为空但房间不为空-生成发射器
                    if (turbo.emitter == null && room != null)
                    {
                        turbo.emitter = new ParticleEmitter(room);
                    }
                    //发射器和房间不匹配-重新生成发射器
                    else if (room != null && turbo.emitter.room != room)
                    {
                        turbo.emitter.Die();
                        turbo.emitter = new ParticleEmitter(room);
                    }
                    //发射器和房间匹配
                    if (room != null && turbo.emitter.room == room)
                    {
                        //开始推进但未产出粒子（初始化）
                        if (turbo.isPropelling & !turbo.emittering)
                        {
                            //确定开始产出粒子
                            turbo.emittering = true;
                            //重新生成发射器，基础赋值
                            turbo.emitter.Die();
                            turbo.emitter = new ParticleEmitter(room);
                            turbo.emitter.vel = vel;
                            turbo.emitter.lastPos = turbo.emitter.pos = pos;
                            //发射器设置
                            turbo.emitter.ApplyEmitterModule(new SetEmitterLife(turbo.emitter, 2, true));
                            turbo.emitter.ApplyParticleSpawn(new BurstSpawnerModule(turbo.emitter, 1));
                            turbo.emitter.ApplyParticleModule(new AddElement(turbo.emitter, new Particle.SpriteInitParam("Futile_White", "", 8, 0.85f)));
                            turbo.emitter.ApplyParticleModule(new SetRandomScale(turbo.emitter, new Vector2(0.25f, 1f), new Vector2(0.25f, 1f)));
                            turbo.emitter.ApplyParticleModule(new SetMoveType(turbo.emitter, Particle.MoveType.Global));
                            turbo.emitter.ApplyParticleModule(new SetRandomLife(turbo.emitter, 20, 20));
                            turbo.emitter.ApplyParticleModule(new SetConstColor(turbo.emitter, UnityEngine.Color.HSVToRGB(0.60f, 0.2f, 0.95f)));
                            turbo.emitter.ApplyParticleModule(new SetConstVelociy(turbo.emitter, -bodydir));
                            turbo.emitter.ApplyParticleModule(new SetRandomPos(turbo.emitter, 20f));
                            turbo.emitter.ApplyParticleModule(new AlphaOverLife(turbo.emitter, (p, l) =>
                            {
                                if (l > 0.5f)
                                    return (1f - l) * 2f;
                                else
                                    return 1f;
                            }));
                            turbo.emitter.ApplyParticleModule(new ScaleXYOverLife(turbo.emitter, (p, l) =>
                            {
                                return new Vector2(0.25f * (1f - 0.5f * l), 1f - 0.5f * l);
                            }));
                            turbo.emitter.ApplyParticleModule(new ColorOverLife(turbo.emitter, (p, l) =>
                            {
                                return UnityEngine.Color.HSVToRGB(0.60f - 0.3f * l, 0.2f + l * 0.3f, 0.95f - 0.45f * l);
                            }));
                            //启动————
                            ParticleSystem.ApplyEmitterAndInit(turbo.emitter);
                        }
                        //开始推进且产出粒子（更新状态）
                        if (turbo.isPropelling && turbo.emittering)
                        {
                            //更新速度和位置，重设粒子速度和旋转
                            turbo.emitter.vel = vel;
                            turbo.emitter.lastPos = turbo.emitter.pos = pos;
                            turbo.emitter.ApplyParticleModule(new SetRandomRotation(turbo.emitter, Custom.VecToDeg(bodydir), Custom.VecToDeg(bodydir)));
                            turbo.emitter.ApplyParticleModule(new SetConstVelociy(turbo.emitter, -bodydir * 3f));
                        }
                        //未在推进但产出粒子（清除）
                        if (!turbo.isPropelling && turbo.emittering)
                        {
                            //清除发射器
                            turbo.emitter.Die();
                            turbo.emittering = false;
                        }
                    }
                }


                //尾巴转转（！）部分参考自TailCopter
                var tailBody = self.bodyChunks[1];
                var allTail = ((self.graphicsModule) as PlayerGraphics).tail;
                if (self.slugcatStats.name != new SlugcatStats.Name("TheTraveler"))
                {
                    if (turbo.isPropelling && turbo.propulsionLevel > 0)
                    {
                        var y = Custom.DirVec(self.bodyChunks[0].pos, self.bodyChunks[1].pos);
                        var x = Custom.PerpendicularVector(y);
                        //似乎是幅度...?
                        float size = 20;
                        var rotatePos = tailBody.pos + y * 30;//中轴的位置
                        rotatePos += ((float)Math.Sin(turbo.tailT * turbo.propulsionLevel * 0.01f)) * x * size;

                        for (int i = 2; i < allTail.Length; i++)
                        {
                            float magnification = (i + 1) / allTail.Length;
                            allTail[i].vel += Custom.DirVec(allTail[i].pos, rotatePos) * Custom.LerpMap(Vector2.Distance(allTail[i].pos, rotatePos), 1, 100, 1, 100 * magnification);
                        }
                        //音效暂时弃用
                        //self.room.PlaySound(SoundID.Bat_Idle_Flying_Sounds, self.bodyChunks[1]);
                        turbo.tailT += 1;
                    }
                    else
                    { turbo.tailT = 0; }
                }
            }
            else
            {
                //应急进表
                TurboFeatures.Add(self, new Turbo(self));
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
    //为了兼容多玩家而添加的类，通过CWT与玩家关联，仅用于存储数据
    internal class Turbo
    {
        WeakReference<Player> ownerRef;

        //是否处于推进状态，只是为了方便判断
        public bool isPropelling = false;
        //推进等级，决定加速度和粒子数量
        public float propulsionLevel = 0f;
        //粒子发射器
        public ParticleEmitter emitter = null;
        //粒子发射器工作判断
        public bool emittering = false;
        //尾巴转动系数
        public int tailT = 0;

        public Turbo(Player player)
        {
            ownerRef = new WeakReference<Player>(player);
        }
    }
}
