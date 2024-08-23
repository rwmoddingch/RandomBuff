using RandomBuff;
using RandomBuff.Core.Game;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RWCustom;
using Random = UnityEngine.Random;
using BuiltinBuffs.Duality;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RandomBuffUtils.ParticleSystem;

namespace BuiltinBuffs.Positive
{
    internal class PermanentShieldBuff : Buff<PermanentShieldBuff, PermanentShieldBuffData>
    {
        public override BuffID ID => PermanentShieldBuffEntry.PermanentShield;
        
        public PermanentShieldBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    var permanentShieldList = new PermanentShieldList(player);

                    if (PermanentShieldBuffEntry.PermanentShieldFeatures.TryGetValue(player, out _))
                        PermanentShieldBuffEntry.PermanentShieldFeatures.Remove(player);
                    PermanentShieldBuffEntry.PermanentShieldFeatures.Add(player, permanentShieldList);
                    if (permanentShieldList.permanentShieldList.Count > 0)
                    {
                        foreach (var permanentShield in permanentShieldList.permanentShieldList)
                        {
                            permanentShield.Destroy();
                        }
                        permanentShieldList.permanentShieldList.Clear();
                    }
                }
            }
        }
    }

    internal class PermanentShieldBuffData : BuffData
    {
        public override BuffID ID => PermanentShieldBuffEntry.PermanentShield;
    }

    internal class PermanentShieldBuffEntry : IBuffEntry
    {
        public static BuffID PermanentShield = new BuffID("PermanentShield", true);

        public static ConditionalWeakTable<Player, PermanentShieldList> PermanentShieldFeatures = new ConditionalWeakTable<Player, PermanentShieldList>();

        public static int StackLayer
        {
            get
            {
                return PermanentShield.GetBuffData().StackLayer;
            }
        }

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<PermanentShieldBuff, PermanentShieldBuffData, PermanentShieldBuffEntry>(PermanentShield);
        }
        
        public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
            On.Player.NewRoom += Player_NewRoom;
            On.Player.Update += Player_Update;
            On.KingTusks.Update += KingTusks_Update;
        }

        private static void KingTusks_Update(On.KingTusks.orig_Update orig, KingTusks self)
        {
            orig(self);
            foreach (var tusk in self.tusks)
            {
                if (tusk.mode == KingTusks.Tusk.Mode.ShootingOut)
                {
                    var shootPos = tusk.chunkPoints[0, 0] +
                                   tusk.shootDir * (20 + Custom.LerpMap(tusk.modeCounter, 0f, 8f, 50f, 30f, 3f));
                    foreach (var shield in self.vulture.room.updateList.OfType<PermanentShield>().Where(i => i.IsExisting))
                    {
                        if (Custom.DistLess(shield.CenterPos, shootPos,
                                shield.Radius(shield.stackIndex,0)))
                        {
                            shield.ShowEffect(shootPos);
                            var rnv = Custom.DirVec(shield.CenterPos, shootPos);
                            tusk.chunkPoints[0, 2] += rnv * 20f;
                            tusk.chunkPoints[1, 2] -= rnv * 20f;
                            tusk.SwitchMode(KingTusks.Tusk.Mode.Dangling);
                            tusk.room.ScreenMovement(shootPos, tusk.shootDir * 0.75f, 0.25f);
                            tusk.room.PlaySound(SoundID.King_Vulture_Tusk_Bounce_Off_Terrain, tusk.chunkPoints[1,0]);
                            break;
                        }
                    }
                }
            }
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!PermanentShieldFeatures.TryGetValue(self, out _))
                PermanentShieldFeatures.Add(self, new PermanentShieldList(self));
        }

        private static void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            orig(self, newRoom);

            if (PermanentShieldFeatures.TryGetValue(self, out var list))
            {
                if (list.permanentShieldList.Count > 0)
                {
                    foreach (var permanentShield in list.permanentShieldList)
                    {
                        permanentShield.Destroy();
                    }
                    list.permanentShieldList.Clear();
                }
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (PermanentShieldFeatures.TryGetValue(self, out var list))
            {
                if (list.permanentShieldList.Count != StackLayer)
                {
                    if (self != null && self.room != null)
                    {
                        for (int j = list.permanentShieldList.Count; j < StackLayer; j++)
                        {
                            var permanentShield = new PermanentShield(self, j, self.room);
                            list.permanentShieldList.Add(permanentShield);
                            self.room.AddObject(permanentShield);
                        }
                    }
                }
            }
        }
    }

    internal class PermanentShield : CosmeticSprite
    {
        Player owner;
        public int stackIndex;
        int disappearCount;
        float averageVoice;
        Color color;

        int firstSprite;
        int totalSprites;
        float expand;
        float lastExpand;
        float getToExpand;
        float push;
        float lastPush;
        float getToPush;

        private ParticleEmitter emitter;
        public bool IsExisting => disappearCount == 0;

        public PermanentShield(Player player, int stackIndex, Room room)
        {
            this.owner = player;
            this.room = room;
            this.stackIndex = stackIndex;
            this.disappearCount = 0;
            this.averageVoice = 0f;
            this.color = new Color(227f / 255f, 171f / 255f, 78f / 255f);
            this.firstSprite = 0;
            this.totalSprites = 1;
            this.getToExpand = 1f;
            this.getToPush = 1f;
            this.emitter = null;
        }

        public Vector2 CenterPos => Center(0);

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            this.room = rCam.room;
            sLeaser.sprites = new FSprite[totalSprites];
            for (int i = 0; i < totalSprites; i++)
            {
                sLeaser.sprites[this.firstSprite + i] = new FSprite("Futile_White", true);
                sLeaser.sprites[this.firstSprite + i].shader = rCam.game.rainWorld.Shaders["VectorCircle"];
                sLeaser.sprites[this.firstSprite + i].color = this.color;
            }
            this.AddToContainer(sLeaser, rCam, null);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (sLeaser.sprites[this.firstSprite].isVisible != this.IsExisting)
            {
                for (int i = 0; i < totalSprites; i++)
                {
                    sLeaser.sprites[this.firstSprite + i].isVisible = this.IsExisting;
                }
            }

            Vector2 vector = this.Center(timeStacker);
            for (int k = 0; k < totalSprites; k++)
            {
                sLeaser.sprites[this.firstSprite + k].x = vector.x - camPos.x;
                sLeaser.sprites[this.firstSprite + k].y = vector.y - camPos.y;
                sLeaser.sprites[this.firstSprite + k].scale = this.Radius(stackIndex, timeStacker) / 8f;
                sLeaser.sprites[this.firstSprite + k].alpha = 3f / this.Radius(stackIndex, timeStacker);
            }
            //有粒子就先不要贴图了
            //sLeaser.sprites[this.firstSprite + 0].isVisible = false;
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        } 

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);

            for (int i = 0; i < totalSprites; i++)
            {
                sLeaser.sprites[firstSprite + i].color = this.color;
            }
        }

        public override void Update(bool eu)
        {
            if(owner.room == null || this.room == null || owner.room != this.room)
            {
                this.Destroy();
                return;
            }

            base.Update(eu);

            this.lastExpand = this.expand;
            this.lastPush = this.push;
            this.expand = Custom.LerpAndTick(this.expand, this.getToExpand, 0.05f, 0.0125f);
            this.push = Custom.LerpAndTick(this.push, this.getToPush, 0.02f, 0.025f);
            bool flag = true;
            if (UnityEngine.Random.value < 0.00625f)
            {
                this.getToExpand = ((UnityEngine.Random.value < 0.5f) ? 1f : Mathf.Lerp(0.95f, 1.05f, Mathf.Pow(UnityEngine.Random.value, 1.5f)));
            }
            if (UnityEngine.Random.value < 0.00625f || flag)
            {
                this.getToPush = 1f;//;((UnityEngine.Random.value < 0.5f && !flag) ? 0f : ((float)(-1 + UnityEngine.Random.Range(0, UnityEngine.Random.Range(0, 1)))));
            }

            if (disappearCount > 0)
                disappearCount--;

            if (IsExisting && owner.room != null)
            {
                //if (this.emitter == null)
                //    CreateEmitter();

                List<PhysicalObject>[] physicalObjects = owner.room.physicalObjects;
                for (int i = 0; i < physicalObjects.Length; i++)
                {
                    for (int j = 0; j < physicalObjects[i].Count; j++)
                    {
                        PhysicalObject physicalObject = physicalObjects[i][j];
                        if (physicalObject is Weapon)
                        {
                            Weapon weapon = physicalObject as Weapon;
                            if (weapon.mode == Weapon.Mode.Thrown && !(weapon.thrownBy is Player) &&
                                Custom.Dist(weapon.firstChunk.pos, owner.firstChunk.pos) < this.Radius(stackIndex, 0f))
                            {
                                weapon.ChangeMode(Weapon.Mode.Free);
                                weapon.SetRandomSpin();
                                weapon.firstChunk.vel *= -0.2f;
                                ShowEffect(weapon.firstChunk.pos);
                            }
                        }
                    }
                }
            }
            else
            {
                if (emitter != null && (owner.room == null || emitter.room != owner.room))
                {
                    emitter.Die();
                    emitter = null;
                }
            }
        }

        public void ShowEffect(Vector2 pos)
        {
            for (int num8 = 0; num8 < 5; num8++)
            {
                owner.room.AddObject(new Spark(pos, Custom.RNV(), Color.white, null, 16, 24));
            }
            owner.room.AddObject(new Explosion.ExplosionLight(pos, 150f, 1f, 8, Color.white));
            owner.room.AddObject(new ShockWave(pos, 60f, 0.1f, 8, false));
            owner.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, pos, 1f, 1.5f + Random.value * 0.5f);
            disappearCount = 1200;
        }

        private void CreateEmitter()
        {
            Color.RGBToHSV(this.color, out var h, out var s, out var v);
            var emitter = new ParticleEmitter(this.room);
            emitter.ApplyEmitterModule(new BindEmitterToPhysicalObject(emitter, this.owner));

            emitter.ApplyParticleSpawn(new RateSpawnerModule(emitter, Mathf.FloorToInt(4f * AverageRadius(this.stackIndex)), Mathf.FloorToInt(4f * AverageRadius(this.stackIndex))));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("pixel", "", 11, 1f, 1f, this.color)));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight", 8, 0.3f, 0.2f, this.color)));

            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 20, 40));
            emitter.ApplyParticleModule(new SetRandomScale(emitter, new Vector2(8f, 3f), new Vector2(6f, 4f)));
            emitter.ApplyParticleModule(new SetRandomColor(emitter, h - 0.1f, h + 0.1f, s, v));
            emitter.ApplyParticleModule(new SetRingPos(emitter, AverageRadius(this.stackIndex)));
            emitter.ApplyParticleModule(new SetRingRotation(emitter, emitter.pos, 0f));
            emitter.ApplyParticleModule(new SetOriginalAlpha(emitter, 0f));

            emitter.ApplyParticleModule(new AlphaOverLife(emitter, (p, a) =>
            {
                if (Custom.Dist(emitter.pos, this.owner.mainBodyChunk.pos) > 10f)
                    return Mathf.Max(0f, p.alpha - 0.05f);
                if (a < 0.2f)
                    return Mathf.Min(1f, p.alpha + 0.02f);
                else if (a > 0.5f)
                    return Mathf.Max(0f, p.alpha - 0.01f);
                else
                    return Mathf.Min(1f, p.alpha + 0.05f);
            }));
            emitter.ApplyParticleModule(new ScaleOverLife(emitter, (p, a) => p.setScaleXY * 4f * a * (1f - a)));
            emitter.ApplyParticleModule(new PositionOverLife(emitter, (p, a) => (p.pos - emitter.pos).normalized * Radius(this.stackIndex, 0f) + emitter.pos));
            emitter.ApplyParticleModule(new RotationOverLife(emitter, (p, a) => Custom.VecToDeg(p.pos - emitter.pos)));

            ParticleSystem.ApplyEmitterAndInit(emitter);
            this.emitter = emitter;
        }

        public override void Destroy()
        {
            if (emitter != null)
            {
                emitter.Die();
                emitter = null;
            }
            base.Destroy();
        }

        public Vector2 Center(float timeStacker)
        {
            Vector2 vector = Vector2.Lerp(this.owner.bodyChunks[0].lastPos, this.owner.bodyChunks[0].pos, timeStacker);
            return vector + Custom.DirVec(vector, Vector2.Lerp(this.owner.bodyChunks[1].lastPos, this.owner.bodyChunks[1].pos, timeStacker)) * 5f;
        }

        public float Radius(float ring, float timeStacker)
        {
            return (3f + ring + Mathf.Lerp(this.lastPush, this.push, timeStacker) - 0.5f * this.averageVoice) * Mathf.Lerp(this.lastExpand, this.expand, timeStacker) * 10f;
        }

        public float AverageRadius(float ring)
        {
            return (3f + ring + this.getToPush - 0.5f * this.averageVoice) * this.getToExpand * 10f;
        }
    }

    internal class PermanentShieldList
    {
        WeakReference<Player> ownerRef;

        public List<PermanentShield> permanentShieldList;

        public PermanentShieldList(Player player)
        {
            this.ownerRef = new WeakReference<Player>(player);
            this.permanentShieldList = new List<PermanentShield>();
        }
    }

    public class SetRingPos : EmitterModule, IParticleInitModule
    {
        float rad;
        public SetRingPos(ParticleEmitter emitter, float rad) : base(emitter)
        {
            this.rad = rad;
        }

        public void ApplyInit(Particle particle)
        {
            Vector2 pos = Custom.RNV() * rad + emitter.pos;
            particle.HardSetPos(pos);
        }
    }

    public class SetRingRotation : EmitterModule, IParticleInitModule
    {
        Vector2 center; 
        float rotation;
        public SetRingRotation(ParticleEmitter emitter, Vector2 center, float rotation) : base(emitter)
        {
            this.center = center;
            this.rotation = rotation;
        }

        public void ApplyInit(Particle particle)
        {
            particle.rotation = Custom.VecToDeg(particle.pos - center) + rotation;
        }
    }

    public class SetRingVelocity : EmitterModule, IParticleInitModule
    {
        Vector2 center;
        float rotation;

        public SetRingVelocity(ParticleEmitter emitter, Vector2 center, float rotation) : base(emitter)
        {
            this.center = center;
            this.rotation = rotation;
        }

        public void ApplyInit(Particle particle)
        {
            Vector2 vel = (particle.pos - center).normalized * Custom.DegToVec(Custom.VecToDeg(particle.pos - center) + rotation);
            particle.SetVel(vel);
        }
    }

    public class SetOriginalAlpha : EmitterModule, IParticleInitModule
    {
        public float alpha;
        public SetOriginalAlpha(ParticleEmitter emitter, float alpha) : base(emitter)
        {
            this.alpha = alpha;
        }

        public void ApplyInit(Particle particle)
        {
            particle.alpha = alpha;
        }
    }
}
