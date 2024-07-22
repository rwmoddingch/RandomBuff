using BuiltinBuffs.Positive;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuffUtils.ObjectExtend;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static AbstractPhysicalObject;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Duality
{
    internal class SurpriseGiftBuff : Buff<SurpriseGiftBuff, SurpriseGiftBuffData>
    {
        public override BuffID ID => SurpriseGiftBuffEntry.surpriseGiftBuffID;
        public override bool Triggerable => true;

        bool firstRound = false;
        public override void Update(RainWorldGame game)
        {
            base.Update(game);
            if(!firstRound)
            {
                foreach(var room in game.world.abstractRooms)
                {
                    foreach(var crit in room.creatures)
                    {
                        if(crit.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Scavenger)
                        {
                            BuffUtils.Log("SurpriseGiftBuff", $"Replace gear for {crit.ID}");
                            SurpriseGiftBuffEntry.ReplaceGear(crit.abstractAI as ScavengerAbstractAI);
                        }
                    }
                }
                firstRound = true;
            }
        }

        public override bool Trigger(RainWorldGame game)
        {
            return false;
        }
    }

    internal class SurpriseGiftBuffData : BuffData
    {
        public override BuffID ID => SurpriseGiftBuffEntry.surpriseGiftBuffID;
    }

    internal class SurpriseGiftBuffEntry : IBuffEntry
    {
        public static BuffID surpriseGiftBuffID = new BuffID("SupriseGift", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SurpriseGiftBuff, SurpriseGiftBuffData, SurpriseGiftBuffEntry>(surpriseGiftBuffID);
            
        }

        public static void HookOn()
        {
            //IL.ScavengerAbstractAI.InitGearUp += ScavengerAbstractAI_InitGearUp;
            On.ScavengerAbstractAI.InitGearUp += ScavengerAbstractAI_InitGearUp1;
            On.ScavengerAbstractAI.ReGearInDen += ScavengerAbstractAI_ReGearInDen;
        }

        private static void ScavengerAbstractAI_ReGearInDen(On.ScavengerAbstractAI.orig_ReGearInDen orig, ScavengerAbstractAI self)
        {
            orig.Invoke(self);

            ReplaceGear(self);
        }

        private static void ScavengerAbstractAI_InitGearUp1(On.ScavengerAbstractAI.orig_InitGearUp orig, ScavengerAbstractAI self)
        {
            orig.Invoke(self);

            ReplaceGear(self);
        }

        public static void ReplaceGear(ScavengerAbstractAI self)
        {
            for (int i = self.parent.stuckObjects.Count - 1; i >= 0; i--)
            {
                if (Random.value > 0.1f)
                    return;
                var obj = self.parent.stuckObjects[i];
                if (obj is AbstractPhysicalObject.CreatureGripStick grip)
                {
                    grip.B.stuckObjects.Remove(obj);
                    grip.B.Destroy();

                    var gift = new AbstractSurpriseGift(self.world, null, self.parent.pos, self.world.game.GetNewID());
                    self.world.GetAbstractRoom(self.parent.pos).AddEntity(gift);
                    grip.B = gift;
                }
            }
        }

        static void ScavengerAbstractAI_InitGearUp(MonoMod.Cil.ILContext il)
        {
            var c1 = new ILCursor(il);
            c1.TryGotoPrev(MoveType.After, (i) => i.MatchRet());
            c1.Emit(OpCodes.Ldarg_0);
            c1.Emit(OpCodes.Ldloc_0);
            c1.EmitDelegate<Action<ScavengerAbstractAI, int>>((self, i) =>
            {
                while(i >= 0)
                {
                    if (Random.value < 0.1f)
                    {
                        var gift = new AbstractSurpriseGift(self.world, null, self.parent.pos, self.world.game.GetNewID());
                        self.world.GetAbstractRoom(self.parent.pos).AddEntity(gift);
                        new AbstractPhysicalObject.CreatureGripStick(self.parent, gift, i, true);
                    }
                    else
                        break;
                    i--;
                }
            });
        }
    }

    [BuffAbstractPhysicalObject]
    public class AbstractSurpriseGift : AbstractPhysicalObject
    {
        public static AbstractObjectType giftType = new AbstractObjectType("SupriseGift", true);
        [BuffAbstractPhysicalObjectProperty]
        public float Hue { get; private set; }

        public AbstractSurpriseGift(World world, AbstractObjectType type, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID) : base(world, type, realizedObject, pos, ID)
        {
        }

        public AbstractSurpriseGift(World world, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID) : this(world, giftType, realizedObject, pos, ID)
        {
            var state = Random.state;
            Random.InitState(ID.RandomSeed);
            Hue = Random.value;
            Random.state = state;
        }

        public override void Realize()
        {
            base.Realize();
            realizedObject = new SurpriseGiftBox(this, world);
        }
    }

    public class SurpriseGiftBox : Weapon
    {
        AbstractSurpriseGift abGift;
        public override bool HeavyWeapon => true;

        Vector2[] silkPos = new Vector2[2];
        Vector2[] lastSilkPos = new Vector2[2];

        public SurpriseGiftBox(AbstractPhysicalObject abstractPhysicalObject, World world) : base(abstractPhysicalObject, world)
        {
            abGift = abstractPhysicalObject as AbstractSurpriseGift;
            bodyChunks = new BodyChunk[1];
            bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0f, 0f), 10.5f, 0.2f);
            bodyChunkConnections = new PhysicalObject.BodyChunkConnection[0];
            airFriction = 0.8f;
            gravity = 0.7f;
            bounce = 0.4f;
            surfaceFriction = 0.4f;
            collisionLayer = 1;
            waterFriction = 0.98f;
            buoyancy = 0.4f;

            silkPos[0] = firstChunk.pos + new Vector2(-8f, 21f);
            silkPos[1] = firstChunk.pos + new Vector2(8f, 21f);
            for(int i = 0;i < 2;i++)
                lastSilkPos[i] = silkPos[i];
        }

        public override void Update(bool eu)
        {
            if (mode == Mode.Free && collisionLayer != 1)
            {
                ChangeCollisionLayer(1);
            }
            else if(mode != Mode.Free && collisionLayer != 2)
            {
                ChangeCollisionLayer(2);
            }
            base.Update(eu);
            for (int i = 0; i < 2; i++)
                lastSilkPos[i] = silkPos[i];
            silkPos[0] = Vector2.Lerp(silkPos[0], firstChunk.pos + new Vector2(-8f, 21f), 0.5f);
            silkPos[1] = Vector2.Lerp(silkPos[1], firstChunk.pos + new Vector2(8f, 21f), 0.5f);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            silkPos[0] = firstChunk.pos + new Vector2(-8f, 21f);
            silkPos[1] = firstChunk.pos + new Vector2(8f, 21f);
            for (int i = 0; i < 2; i++)
                lastSilkPos[i] = silkPos[i];

            sLeaser.sprites = new FSprite[5];

            sLeaser.sprites[0] = new FSprite("pixel") { scaleX = 6f, scaleY = 20f };//彩绑带
            sLeaser.sprites[1] = new FSprite("pixel") { scaleX = 3f, scaleY = 10f };//彩绑带
            sLeaser.sprites[2] = new FSprite("pixel") { scaleX = 3f, scaleY = 10f };//彩绑带

            sLeaser.sprites[3] = new FSprite("pixel") { scaleX = 20f, scaleY = 20f};//盒子底
            sLeaser.sprites[4] = new FSprite("pixel") { scaleX = 22f, scaleY = 6f};//盒子盖

            AddToContainer(sLeaser, rCam, null);
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            for(int i = 0;i < 3;i++)
                sLeaser.sprites[i].color = Custom.HSL2RGB(abGift.Hue, 1f, 0.5f);

            sLeaser.sprites[3].color = Custom.HSL2RGB(abGift.Hue, 0.54f, 0.78f);
            sLeaser.sprites[4].color = Custom.HSL2RGB(abGift.Hue, 0.50f, 0.62f);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker) - camPos;
            Vector2 tip = pos + Vector2.up * 10;
            for(int i = 0;i < 2; i++)
            {
                Vector2 smoothSilkTip = Vector2.Lerp(lastSilkPos[i], silkPos[i], timeStacker) - camPos;
                Vector2 mid = (smoothSilkTip + tip) / 2f;
                float length = Vector2.Distance(smoothSilkTip, tip);
                float angle = Custom.VecToDeg(smoothSilkTip - tip);

                sLeaser.sprites[i + 1].SetPosition(mid);
                sLeaser.sprites[i + 1].rotation = angle;
                sLeaser.sprites[i + 1].scaleY = length;
            }

            sLeaser.sprites[0].SetPosition(pos);

            sLeaser.sprites[3].SetPosition(pos);
            sLeaser.sprites[4].SetPosition(pos + Vector2.up * (10f - 3f));

            if (slatedForDeletetion || room != rCam.room)
                sLeaser.CleanSpritesAndRemove();
        }

        public override void HitByWeapon(Weapon weapon)
        {
            if (weapon is SurpriseGiftBox)
                return;

            weapon.HitByWeapon(this);
            Destroy();

            var state = Random.state;
            Random.InitState(abGift.ID.RandomSeed);
            if(Random.value < 0.99f)
            {
                int intdata = Random.Range(0, 5);
                AbstractObjectType abType = GetRandomObject();
                var coord = abGift.pos;

                PandoraBoxIBuffEntry.session.SpawnItems(new IconSymbol.IconSymbolData(null, abType, intdata), coord, room.game.GetNewID());
                var newObj = PandoraBoxIBuffEntry.session.game.world.GetAbstractRoom(0).entities.Pop() as AbstractPhysicalObject;
                newObj.world = room.world;
                room.abstractRoom.AddEntity(newObj);
                newObj.RealizeInRoom();
            }
            else
            {
                AbstractCreature abstractCreature = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.RedLizard),null, abGift.pos, room.game.GetNewID());
                room.abstractRoom.AddEntity(abstractCreature);
                abstractCreature.RealizeInRoom();
            }
            Random.state = state;
        }

        public override void Destroy()
        {
            base.Destroy();
            CreateRibbons();
        }

        void CreateRibbons()
        {
            var emitter = new ParticleEmitter(room);

            emitter.pos = firstChunk.pos;
            emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, Random.Range(20, 40)));
            emitter.ApplyParticleModule(new SetEmitterLife(emitter, 1, false, true));

            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("pixel", "", 4)));

            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 40 * 3, 40 * 5));

            emitter.ApplyParticleModule(new SetRandomColor(emitter, abGift.Hue - 0.2f, abGift.Hue + 0.2f, 1f, 0.5f));
            emitter.ApplyParticleModule(new SetRandomPos(emitter, 10f));
            emitter.ApplyParticleModule(new RotationOverLife(emitter, (p, l) =>
            {
                return Mathf.Lerp(360 * 5f, 360 * 12f, p.randomParam1) * l;
            }));
            emitter.ApplyParticleModule(new SetRandomScale(emitter, new Vector2(4f, 2f), new Vector2(6f, 2f)));
            emitter.ApplyParticleModule(new VelocityOverLife(emitter, (p, l) =>
            {
                Vector2 circleVel = Custom.DegToVec(p.randomParam2 * 360f);
                float cV = Mathf.Max(0.2f - l, 0f) * 5f * 6f * p.randomParam3;
                circleVel *= cV;

                Vector2 gVel = Vector2.down * Mathf.Min(Mathf.Max(l - 0.2f, 0f) * 1.25f, 1f) * 2f;
                return circleVel + gVel;
            }));
            emitter.ApplyParticleModule(new ScaleOverLife(emitter, (p, l) =>
            {
                if (l > 0.9f)
                    return new Vector2(p.setScaleXY.x * (1f - l) * 10f, p.setScaleXY.y);
                return p.setScaleXY;
            }));
            ParticleSystem.ApplyEmitterAndInit(emitter);
        }

        static AbstractObjectType[] choices = new AbstractObjectType[]
        {
            AbstractObjectType.DangleFruit,
            AbstractObjectType.Spear,
            AbstractObjectType.Rock,
            AbstractObjectType.ScavengerBomb,
            AbstractObjectType.SporePlant,
            AbstractObjectType.SlimeMold,
            AbstractObjectType.FlareBomb,
            AbstractObjectType.FirecrackerPlant,
            AbstractObjectType.BubbleGrass,
            AbstractObjectType.FlyLure,
        };

        static AbstractObjectType GetRandomObject()
        {
            return choices[Random.Range(0, choices.Length - 1)];
        }
    }
}
