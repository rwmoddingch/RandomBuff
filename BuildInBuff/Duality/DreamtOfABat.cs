using BuiltinBuffs.Positive;
using HotDogGains.Duality;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BuiltinBuffs.Negative;
using UnityEngine;

namespace BuildInBuff.Duality
{
    class DreamtOfABatBuff : Buff<DreamtOfABatBuff, DreamtOfABatBuffData> { public override BuffID ID => DreamtOfABatBuffEntry.DreamtOfABatID; }
    class DreamtOfABatBuffData : CountableBuffData
    {
        public override BuffID ID => DreamtOfABatBuffEntry.DreamtOfABatID;
        public override bool CanStackMore() => StackLayer < 4;

        public override int MaxCycleCount => 3;

    }
    class DreamtOfABatBuffEntry : IBuffEntry
    {
        public static BuffID DreamtOfABatID = new BuffID("DreamtOfABatID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<DreamtOfABatBuff, DreamtOfABatBuffData, DreamtOfABatBuffEntry>(DreamtOfABatID);
        }
        public static void HookOn()
        {
            //��ѣ��ʱ��������
            On.Player.Stun += Player_Stun;
            //��ֹ��ʧʱ��������
            On.Player.Die += Player_Die;


            //�ı�����������ɫ
            On.FlyGraphics.ApplyPalette += ButteFly_ApplyPalette;

            //��ֹ�ܹ�������ҵ�����
            IL.MeltLights.Update += MeltLights_Update;
        }

        private static void MeltLights_Update(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                (i) => i.MatchLdarg(0),
                (i) => i.MatchLdfld<UpdatableAndDeletable>("room"),
                (i) => i.MatchLdfld<Room>("physicalObjects"),
                (i) => i.MatchLdcI4(0),
                (i) => i.MatchLdelemRef(),
                (i) => i.MatchLdloc(1),
                (i) => i.Match(OpCodes.Callvirt)
                ))
            {
                c.EmitDelegate<Func<PhysicalObject, PhysicalObject>>((obj) =>
                {
                    if (obj is Fly fly && fly.IsButterFly())
                    {
                        return null;//������ε��ʹ�����ֵ
                    }
                    return obj;
                });
            }
        }

        private static void ButteFly_ApplyPalette(On.FlyGraphics.orig_ApplyPalette orig, FlyGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig.Invoke(self, sLeaser, rCam, palette);

            //�������������ɫ����ҵ���ɫһ��
            if (self.fly.IsButterFly(out var butteFly))
            {
                for (int i = 0; i < 3; i++)
                {
                    sLeaser.sprites[i].color = butteFly.color;
                }
            }

        }


        private static void Player_Die(On.Player.orig_Die orig, Player self)
        {
            if (self.stun > 0 && self.slatedForDeletetion)
            {
                foreach (var item in self.room.updateList)
                {
                    if (item is BatBody body && body.player == self)
                    {
                        return;
                    }
                }
            }

            orig.Invoke(self);
        }


        private static void Player_Stun(On.Player.orig_Stun orig, Player self, int st)
        {
            orig.Invoke(self, st);

            if (self.dead) return;

            if (self.room != null && self.room.updateList != null)
            {
                //fp�����ڲ��������Ʒ�ֹ����
                if (self.room.abstractRoom.name.Length > 2 && self.room.abstractRoom.name.Substring(self.room.abstractRoom.name.Length - 2) == "AI") return;
                if (self.room.abstractRoom.name == "SB_E05SAINT") return;


                //�Ѿ�
                foreach (var item in self.room.updateList)
                {
                    if (item is BatBody body && body.player == self)
                    {
                        return;
                    }
                }

                //��΢����һ����ֵ��ֹĪ������ķ�������
                var activeLimite = 12 - (DreamtOfABatID.GetBuffData().StackLayer > 2 ? (DreamtOfABatID.GetBuffData().StackLayer - 2) * 5 : 0);
                //����״̬���ѷ���������
                activeLimite *= self.exhausted ? 2 : 1;
                if (st > activeLimite) self.room.AddObject(new BatBody(self.abstractCreature, HeartDevouringWormBuffEntry.IsInfected(self)));
            }


        }
    }

    public class BatBody : UpdatableAndDeletable
    {

        public AbstractCreature absPlayer;
        public Player player => absPlayer.realizedCreature as Player;

        public Fly batBody;

        private bool dieAfterDestroy;

        public BatBody(AbstractCreature absPlayer, bool dieAfterDestroy)
        {
            DreamtOfABatBuff.Instance.TriggerSelf(true);
            this.dieAfterDestroy = dieAfterDestroy;
            this.absPlayer = absPlayer;


            //�ٻ���ɫ����
            var room = player.room;
            var absFly = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly), null, room.GetWorldCoordinate(player.DangerPos), room.world.game.GetNewID());

            absFly.TurnButteFLy(absPlayer.realizedCreature.ShortCutColor());

            absFly.lavaImmune = player.abstractCreature.lavaImmune;//让蝙蝠同步玩家的岩浆免疫能力
            absFly.tentacleImmune = player.abstractCreature.tentacleImmune;//让蝙蝠同步玩家的触手免疫能力

            room.abstractRoom.AddEntity(absFly);
            absFly.RealizeInRoom();

            batBody = absFly.realizedCreature as Fly;

            //�ƶ�����
            batBody.firstChunk.HardSetPosition(player.firstChunk.pos);
            batBody.firstChunk.vel += player.firstChunk.vel;


            //batBody.abstractCreature.controlled=true;

            //��Ч
            AddEffect(room);

            if ((ModManager.MSC || ModManager.CoopAvailable) && player.slugOnBack != null && player.slugOnBack.slugcat != null)
            {
                player.slugOnBack.DropSlug();
            }
            if (player.spearOnBack != null && player.spearOnBack.spear != null)
            {
                player.spearOnBack.DropSpear();
            }

            //�÷����Զ�ɾ�����
            player.slatedForDeletetion = true;

            player.wantToPickUp = 0;

        }

        public override void Destroy()
        {
            if (player.slatedForDeletetion && !batBody.slatedForDeletetion)
            {
                player.slatedForDeletetion = false;

                //��ֹ�ظ��������
                bool notHavePlayer = true;
                //�������Ƿ��Ѿ�������
                foreach (var item in room.abstractRoom.creatures)
                {
                    if (item == player.abstractCreature) notHavePlayer = false;
                }

                //�������
                if (notHavePlayer)
                {

                    //������û�оʹ���һ�����
                    //room.abstractRoom.AddEntity(player.abstractCreature);
                    //player.PlaceInRoom(room);
                    var absPlayer = player.abstractCreature;

                    if (!room.abstractRoom.creatures.Contains(absPlayer))
                        room.abstractRoom.AddEntity(absPlayer);

                    if (!room.abstractRoom.realizedRoom.updateList.Contains(player))
                        room.abstractRoom.realizedRoom.AddObject(player);


                    //����ҵ�����λ��
                    for (int i = 0; i < player.bodyChunks.Length; i++)
                    {
                        //player.bodyChunks[i].HardSetPosition(batBody.firstChunk.pos);

                        //player.bodyChunks[i].vel = batBody.firstChunk.vel;
                    }
                    //�������վ��
                    player.standing = true;
                    if (dieAfterDestroy)
                    {
                        player.Die();
                        player.abstractCreature.state.meatLeft = 0;
                    }

                    player.graphicsModule.Reset();
                }

            }
            if (batBody.dead || batBody.slatedForDeletetion) player.Die();

            batBody.Destroy();
            base.Destroy();

        }

        public void AddEffect(Room room)
        {
            room.AddObject(new Explosion.ExplosionLight(player.firstChunk.pos, 80, 1, 20, Custom.hexToColor("93c5d4")));
            room.AddObject(new SporePlant.BeeSpark(player.firstChunk.pos));
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            //��ֹ����ܵ�
            batBody.enteringShortCut = null;
            batBody.shortcutDelay = 40;

            if (player != null)
            {
                if (player.dead) batBody.dead = true;

                
                if (batBody.Consious)
                {
                    if (player.airInLungs > 0)
                    {
                        player.airInLungs -= 1f / (40f * (player.lungsExhausted ? 4.5f : 9f) * ((float)this.room.game.setupValues.lungs / 100f)) * player.slugcatStats.lungsFac * 2;
                        batBody.drown = 0;
                    }

                    if (DreamtOfABatBuffEntry.DreamtOfABatID.GetBuffData().StackLayer > 1)
                    {
                        batBody.abstractCreature.controlled = true;
                        batBody.inputWithDiagonals = RWInput.PlayerInput(player.playerState.playerNumber);
                    }
                }


                if (batBody.slatedForDeletetion) player.stun = 0;

                if (player.stun <= 0) this.Destroy();
                else
                {
                    player.stun--;
                    player.AerobicIncrease(0.1f);
                    //����ҵ�����λ��
                    for (int i = 0; i < player.bodyChunks.Length; i++)
                    {
                        player.bodyChunks[i].HardSetPosition(batBody.firstChunk.pos);
                        player.bodyChunks[i].vel = batBody.firstChunk.vel;
                    }
                }
            }


        }


    }

    public static class EXFly
    {
        public static bool IsButterFly(this Fly fly) => ButteFly.modules.TryGetValue(fly.abstractCreature, out var butteFly);
        public static bool IsButterFly(this Fly fly, out ButteFly butteFly) => ButteFly.modules.TryGetValue(fly.abstractCreature, out butteFly);

        public static void TurnButteFLy(this AbstractCreature fly, Color color)
        {
            ButteFly.modules.Add(fly, new ButteFly(color));
        }
    }

    public class ButteFly
    {
        public static ConditionalWeakTable<AbstractCreature, ButteFly> modules = new ConditionalWeakTable<AbstractCreature, ButteFly>();

        public Color color;
        public ButteFly(Color color) { this.color = color; }
    }
}