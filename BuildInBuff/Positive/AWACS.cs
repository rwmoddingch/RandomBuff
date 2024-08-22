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
    class AWACSBuff : Buff<AWACSBuff,AWACSBuffData>
    { 
        public override BuffID ID => AWACSIBuffEntry.AWACSBuffID;
        //触发
        public override bool Trigger(RainWorldGame game)
        {
            AWACSIBuffEntry.active = !AWACSIBuffEntry.active;
            foreach (var player in game.Players.Select(i => i.realizedCreature as Player))
            {
                if (player?.room == null)
                {
                    continue;
                }
                if (AWACSIBuffEntry.active)
                {
                    player.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Core_On, player.DangerPos, 1f, 1f+UnityEngine.Random.Range(-0.2f,0.5f));
                }
                else if (!AWACSIBuffEntry.active)
                {
                    player.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Core_Off, player.DangerPos, 1f, 1f + UnityEngine.Random.Range(-0.2f, 0.5f));
                }
            }
            return false;
        }
    }
    class AWACSBuffData : BuffData
    {
        public override BuffID ID => AWACSIBuffEntry.AWACSBuffID;
    }
    internal class AWACSIBuffEntry : IBuffEntry
    {
        /*-----------------------------------------------------字段-----------------------------------------------------*/
        //设置BuffID
        public static BuffID AWACSBuffID = new BuffID("AWACSID", true);
        //生物弱表
        public static ConditionalWeakTable<Creature,AWACSHUD> awacsHUD = new ConditionalWeakTable<Creature, AWACSHUD>();
        //启用/禁用
        public static bool active = true;
        /*-----------------------------------------------------挂钩-----------------------------------------------------*/
        public static void HookOn()
        {
            //玩家更新
            On.Player.Update += Player_Update;
            //玩家进入新房间
            On.Player.NewRoom += Player_NewRoom;
        }
        /*-----------------------------------------------------方法-----------------------------------------------------*/
        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            //原版更新方法
            orig.Invoke(self, eu);

            Room room = self.room;
            Vector2 pos = self.mainBodyChunk.pos;

            /*生成生物或物品通法，之后写空中威胁才会用到
            if (self.input[0].jmp &&!self.input[1].jmp)
            {
                AbstractCreature cicada = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.CicadaA), null, room.GetWorldCoordinate(pos + 500 * Vector2.up), room.world.game.GetNewID());
                room.abstractRoom.AddEntity(cicada);
                cicada.RealizeInRoom();
            }
            */
            //每次更新获取生物列表
            if (self.room != null)
            {
                //对于房间里的所有生物
                foreach (AbstractCreature creature in room.abstractRoom.creatures)
                {
                    //条件判断：抽象生物和对应的实际生物非空，活着，房间匹配，生物不是玩家，玩家也得活着，玩家没有进管道,以及还要启用HUD
                    //最后一条其实有小问题（没用） 玩家进入管道就意味着房间为空
                    //这会导致出现一些麻烦：判断是因为玩家在管道里导致房间为空或是真的不在任何房间里会很头疼，所以干脆直接通过图标来判断玩家进入管道
                    if (creature != null
                        && creature.realizedCreature != null
                        && creature.realizedCreature.room != null
                        && !creature.realizedCreature.dead
                        && creature.realizedCreature.room == self.room
                        && creature.realizedCreature.GetType() != self.GetType()
                        && !self.dead
                        && !self.inShortcut
                        && active)
                    {
                        //如果符合条件但不在表里面就添加到表里面并且建立对象
                        if (!awacsHUD.TryGetValue(creature.realizedCreature, out _))
                        {
                            awacsHUD.Add(creature.realizedCreature, new AWACSHUD(creature.realizedCreature, self));
                            if (awacsHUD.TryGetValue(creature.realizedCreature, out var hud))
                            {
                                room.AddObject(hud);
                            }
                        }
                    }
                    //不满足条件的生物如果存在于表里则从表中移除,如果查询不到表里的键值对那么对应的图标就自己移除自身
                    //避免了大部分需要由图标判断是否可以存在的情况，但仍然存在特例，比如如果生物死亡则需要播放动画
                    else
                    {
                        if (creature.realizedCreature != null)
                        {
                            if (awacsHUD.TryGetValue(creature.realizedCreature, out _))
                            {
                                awacsHUD.Remove(creature.realizedCreature);
                            }
                        }
                    }
                }
            }
        }
        private static void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            //原版更新方法
            orig.Invoke(self, newRoom);
            //换一张表（清空
            awacsHUD = new ConditionalWeakTable<Creature, AWACSHUD>();
        }
        public void OnEnable()
        {
            //注册BuffID
            BuffRegister.RegisterBuff<AWACSBuff, AWACSBuffData,AWACSIBuffEntry>(AWACSBuffID);
        }
    }
    //生物图标的类
    internal class AWACSHUD : CosmeticSprite
    {
        //弱引用
        WeakReference<Creature> ownerRef;
        //各种字段
        private Player player;
        private Creature creature;
        private Vector2 centerPos;
        private IconSymbol.IconSymbolData iconData;
        //构造函数，引入目标生物creature对象和玩家player对象,建立弱引用，确定中心位置
        public AWACSHUD(Creature creature,Player player)
        {
            ownerRef = new WeakReference<Creature>(creature);
            this.creature = creature;
            this.player = player;
            centerPos = player.mainBodyChunk.pos;

            room = player.room;
            lastPos = pos = creature.mainBodyChunk.pos;
        }
        //重写函数,更新+计时器,以及判断如果生物在管道里或者死亡则清除自身。
        public override void Update(bool eu)
        {
            centerPos = player.mainBodyChunk.pos;
            Vector2 dir = (this.creature.mainBodyChunk.pos - centerPos).normalized;
            //如果死了就提前destroy并且播放一个粒子特效
            //玩家在捷径里面也Destroy图标，似乎因为图标被移除，对应的键值对也会清除，所以会在离开管道时自动新建键值对并重新添加图标？
            //不知道具体原理 但似乎运作正常。
            if (this.creature.dead)
            {
                //粒 子 发 射 器 = 击杀特效
                //简直是变量命名反面教材，你可以注意到这里的东西有多混乱：
                //pos是图标的pos，但是图标的pos是生物的pos，但是只在每次更新后是，creature可以是局部变量creature，也可以是字段this.creature
                //虽说能跑就行，但这样写的代码自己看都头疼，这些注释很大一部分是写给自己看的，这是非常糟糕的编码习惯....
                //良好的代码能做到像自然语言一样易于阅读 根本不需要这么多惨烈的注释
                ParticleEmitter emitter = new ParticleEmitter(player.room);
                emitter.lastPos = emitter.pos = pos;
                //发射器设置
                emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 5, false));
                emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, 1));
                emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam(CreatureSymbol.SpriteNameOfCreature(iconData), "GateHologram", 8, 1f)));
                emitter.ApplyParticleModule(new SetRandomScale(emitter, new Vector2(1f, 1f), new Vector2(1f, 1f)));
                emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
                emitter.ApplyParticleModule(new SetRandomLife(emitter, 40, 40));
                emitter.ApplyParticleModule(new SetConstColor(emitter, CreatureSymbol.ColorOfCreature(iconData)));
                emitter.ApplyParticleModule(new SetConstVelociy(emitter, Vector2.zero));
                emitter.ApplyParticleModule(new SetRandomPos(emitter, 0f));
                emitter.ApplyParticleModule(new AlphaOverLife(emitter, (p, l) =>
                {
                        return 1f - l;
                }));
                emitter.ApplyParticleModule(new ScaleOverLife(emitter, (p, l) =>
                {
                    return new Vector2(1f,1f);
                }));
                emitter.ApplyParticleModule(new ColorOverLife(emitter, (p, l) =>
                {
                    return UnityEngine.Color.Lerp
                    (
                    UnityEngine.Color.red, CreatureSymbol.ColorOfCreature(iconData), (Mathf.Cos(l * 15 * Mathf.PI) + 1f) / 2f
                    );     
                }));
                //启动——
                ParticleSystem.ApplyEmitterAndInit(emitter);
                //音效，以及命名地狱。this.player.room，还有this.room或者其他方式来获取到同一个room...能跑而且一般不会出毛病，但出了毛病就很难排查
                this.player.room.PlaySound(SoundID.King_Vulture_Tusk_Aim_Beep, centerPos + dir * 120, 2.5f+UnityEngine.Random.Range(-0.5f,1f), 1f+UnityEngine.Random.Range(-0.2f,0.7f));
                this.player.room.PlaySound(SoundID.Slugcat_Rocket_Jump, centerPos + dir * 120, 2.5f, 1f);
                //别忘了删图标
                Destroy();
            }
            //如果没死但这个键值对不在表里面了就Destroy，或者其他需要清除的情况。
            //不知道因为什么原因 如果这里不加最后的active判断就会导致禁用时旧的图标不消失
            if (!ownerRef.TryGetTarget(out var creature) || player.room == null || player.inShortcut || creature.room == null || !AWACSIBuffEntry.active)
            {
                Destroy();
                return;
            }
            base.Update(eu);
            pos = centerPos + dir * 120;
        }
        //重写函数，删除自身，其实什么也没有重写因为里面就是base.Destroy
        public override void Destroy()
        {
            base.Destroy();
        }
        //重写函数，初始化图像元素，获取生物图标并且应用图标和着色，设置shader，然后AddToContainer
        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            iconData = new IconSymbol.IconSymbolData(creature.abstractCreature.creatureTemplate.type, AbstractPhysicalObject.AbstractObjectType.Creature, 0);
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite(Futile.atlasManager.GetElementWithName(CreatureSymbol.SpriteNameOfCreature(iconData)));
            sLeaser.sprites[0].color =CreatureSymbol.ColorOfCreature(iconData);
            sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["GateHologram"];
            AddToContainer(sLeaser, rCam, null);
        }
        //重写函数，更新位置和大小
        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (slatedForDeletetion)
            {
                sLeaser.RemoveAllSpritesFromContainer();
                return;
            }
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            sLeaser.sprites[0].alpha = 1f;          
            sLeaser.sprites[0].SetPosition(Vector2.Lerp(lastPos, pos, timeStacker) - camPos);
        }
        //重写函数，AddToContainer
        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner = rCam.ReturnFContainer("Foreground");
            newContatiner.AddChild(sLeaser.sprites[0]);
        }
    }
}
