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
    internal class FireShieldBuff : IgnitionPointBaseBuff<FireShieldBuff, FireShieldBuffData>
    {
        public override bool Triggerable => true;

        public override BuffID ID => FireShieldBuffEntry.FireShield;

        public FireShieldBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    var fireShield = new FireShield(player, player.room);
                    FireShieldBuffEntry.FireShieldFeatures.Add(player, fireShield);
                    player.room.AddObject(fireShield);

                    FireShieldState fireShieldState = new FireShieldState(player);
                    FireShieldBuffEntry.FireShieldStateFeatures.Add(player, fireShieldState);
                    FireShieldBuffEntry.PlayerList.Add(player);
                }
            }
        }

        public override bool Trigger(RainWorldGame game)
        {
            foreach (var player in FireShieldBuffEntry.PlayerList)
            {
                if (FireShieldBuffEntry.FireShieldStateFeatures.TryGetValue(player, out var fireShield) &&
                    BuffInput.GetKeyDown(GetBindKey()))
                {
                    if (fireShield.IsActivate)
                        fireShield.Deactivate();
                    else if (!fireShield.IsActivate)
                        fireShield.Activate();
                }
            }
            return false;
        }
    }

    internal class FireShieldBuffData : BuffData
    {
        public override BuffID ID => FireShieldBuffEntry.FireShield;
    }

    internal class FireShieldBuffEntry : IBuffEntry
    {
        public static BuffID FireShield = new BuffID("FireShield", true);
        public static ConditionalWeakTable<Player, FireShield> FireShieldFeatures = new ConditionalWeakTable<Player, FireShield>();
        public static ConditionalWeakTable<Player, FireShieldState> FireShieldStateFeatures = new ConditionalWeakTable<Player, FireShieldState>();
        public static List<Player> PlayerList = new List<Player>();

        public static int StackLayer
        {
            get
            {
                return FireShield.GetBuffData().StackLayer;
            }
        }

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<FireShieldBuff, FireShieldBuffData, FireShieldBuffEntry>(FireShield);
        }

        public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
            On.Player.NewRoom += Player_NewRoom;
            On.Player.Update += Player_Update;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!FireShieldFeatures.TryGetValue(self, out _))
            {
                FireShield fireShield = new FireShield(self, self.room);
                FireShieldFeatures.Add(self, fireShield);
                self.room.AddObject(fireShield);
            }
            if (!FireShieldStateFeatures.TryGetValue(self, out _))
            {
                FireShieldState fireShieldState = new FireShieldState(self);
                FireShieldStateFeatures.Add(self, fireShieldState);
                PlayerList.Add(self);
            }
        }

        private static void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            orig(self, newRoom);

            if (FireShieldFeatures.TryGetValue(self, out var fireShield))
            {
                FireShieldFeatures.Remove(self);
                fireShield.Destroy();
                fireShield = new FireShield(self, self.room);
                FireShieldFeatures.Add(self, fireShield);
                self.room.AddObject(fireShield);
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            //if (Input.GetKeyDown(KeyCode.M))
            //{
            //    FireShield.GetBuffData().Stack();
            //}

            orig(self, eu); 

            if (FireShieldFeatures.TryGetValue(self,out var fireShield))
            {
                if (fireShield.IsExisting)
                    self.Hypothermia = Mathf.Min(0, self.Hypothermia - 0.05f - 0.01f * StackLayer);
            }
        }
    }

    internal class FireShield : UpdatableAndDeletable
    {
        ParticleEmitter emitter;
        WeakReference<Player> ownerRef;
        Player owner;
        Color color;

        int level;
        int emitterCount;
        int emitterMaxCount;
        float angle;
        float angleSpeed;

        private int coolingTime;

        public bool IsExisting
        {
            get
            {
                if (!ownerRef.TryGetTarget(out var player))
                    return false;
                bool isActivate = true;
                if (FireShieldBuffEntry.FireShieldStateFeatures.TryGetValue(player, out var fireShieldState))
                {
                    isActivate = fireShieldState.IsActivate;
                }
                return isActivate &&
                       (owner.mainBodyChunk.submersion <= 0.5 ||
                       FireShieldBuff.Instance.GetTemporaryBuffPool().allBuffIDs.Contains(BuiltinBuffs.Negative.HellIBuffEntry.hellBuffID));
            }
        }

        public FireShield(Player player, Room room)
        {
            this.ownerRef = new WeakReference<Player>(player);
            this.owner = player;
            this.room = room;
            this.color = new Color(227f / 255f, 171f / 255f, 78f / 255f);
            this.level = FireShieldBuffEntry.StackLayer;
            this.emitterCount = 0;
            this.emitterMaxCount = 3;
            this.angle = 0f;
            this.angleSpeed = 1f;
        }

        public override void Update(bool eu)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (owner.room == null || this.room == null || owner.room != this.room)
            {
                this.Destroy();
                return;
            }

            base.Update(eu);

            if (emitterCount > 0)
                emitterCount--;

            if (angle < 360f)
                angle += angleSpeed;
            else
                angle += angleSpeed - 360f;

            EmitterUpdate();
        }

        private float Radius(float ring)
        {
            return (3f + 0.5f * ring) * 20f;
        }

        private void EmitterUpdate()
        {
            if (emitterCount == 0 && IsExisting)
            {
                emitterCount = emitterMaxCount;
                this.emitter = CreateFireSparkle(owner, room, owner.mainBodyChunk.pos, this.Radius(level));
            }
        }

        private ParticleEmitter CreateFireSparkle(Creature owner, Room room, Vector2 pos, float rad)
        {
            var emitter = new ParticleEmitter(room);
            emitter.lastPos = emitter.pos = pos;
            CreatFireSparkleInternal(emitter, owner, rad);
            ParticleSystem.ApplyEmitterAndInit(emitter);
            return emitter;
        }

        private ParticleEmitter CreatFireSparkleInternal(ParticleEmitter emitter, Creature owner, float rad)
        {
            emitter.ApplyEmitterModule(new BindEmitterToPhysicalObject(emitter, owner));
            emitter.ApplyEmitterModule(new SetEmitterLife(emitter, emitterMaxCount, false));
            emitter.ApplyEmitterModule(new BurstSpawnerModule(emitter, Mathf.FloorToInt((2 + level) * rad / Radius(1))));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "")));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight", 8, 0.3f, 2f, this.color)));
            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 40, 60));
            emitter.ApplyParticleModule(new SetRotateRingPosAddFire(emitter, owner, rad, angle, 2 + level));
            emitter.ApplyParticleModule(new SetConstVelociy(emitter, Vector2.up * 2f));

            emitter.ApplyParticleModule(new ColorOverLife(emitter, (p, l) =>
            {
                if (l < 0.5f)
                    return Color.Lerp(Color.white, Color.yellow, l * 2f);
                else
                    return Color.Lerp(Color.yellow, Color.red, (l - 0.5f) * 2f);
            }));

            emitter.ApplyParticleModule(new AlphaOverLife(emitter, (p, l) =>
            {
                if (l < 0.2f)
                    return l * 5f;
                else if (l > 0.5f)
                    return (1f - l) * 2f;
                else
                    return 1f;
            }));

            emitter.ApplyParticleModule(new TrailDrawer(emitter, 0, 5)
            {
                gradient = (p, i, max) => p.color,
                alpha = (p, i, max) => p.alpha,
                width = (p, i, max) => 1f
            });

            return emitter;
        }
    }

    internal class FireShieldState
    {
        WeakReference<Player> ownerRef;

        public bool IsActivate { get; set; }

        public FireShieldState(Player c)
        {
            this.ownerRef = new WeakReference<Player>(c);
            this.IsActivate = true;
        }

        public void Activate()
        {
            IsActivate = true;
        }

        public void Deactivate()
        {
            IsActivate = false;
        }
    }

    public class NoAttachedFire : UpdatableAndDeletable, IHeatingCreature
    {
        Creature bindCreature;
        Vector2[] getToPositions;
        Vector2 relativePosition;
        Vector2 pos;

        HolyFire.HolyFireSprite fire;
        LightSource[] lightSources;
        float[] getToRads;

        int lifeTime;
        int level;
        int counter;
        bool kill;
        bool shouldFire;
        float rad;

        public NoAttachedFire(Room room, Creature bindCreature, Vector2 relativePosition)
        {
            this.room = room;
            this.bindCreature = bindCreature;
            this.relativePosition = relativePosition;
            this.level = FireShieldBuffEntry.StackLayer;
            this.pos = bindCreature.mainBodyChunk.pos + relativePosition;
            this.lifeTime = 40;
            this.rad = 20f;

            lightSources = new LightSource[1];
            getToPositions = new Vector2[this.lightSources.Length];
            getToRads = new float[this.lightSources.Length];
            for (int i = 0; i < this.lightSources.Length; i++)
            {
                lightSources[i] = new LightSource(pos, false, new Color(227f / 255f, 171f / 255f, 78f / 255f), this);
                room.AddObject(this.lightSources[i]);
                lightSources[i].setAlpha = 0f;
            }
        }

        public float GetHeat(UpdatableAndDeletable updatableAndDeletable, Vector2 pos)
        {
            float dist = Vector2.Distance(this.pos, pos);
            if (dist > rad)
                return 0f;

            if (updatableAndDeletable is Creature)
            {
                Creature creature = updatableAndDeletable as Creature;
                shouldFire = true;
                if (creature is TubeWorm)
                    shouldFire = false;
                if (creature is Player)
                    shouldFire = false;
                if (creature is Overseer && (creature as Overseer).AI.LikeOfPlayer(this.bindCreature.abstractCreature) > 0.5f)
                    shouldFire = false;
                if (creature is Lizard)
                {
                    foreach (RelationshipTracker.DynamicRelationship relationship in (creature as Lizard).AI.relationshipTracker.relationships.
                        Where((RelationshipTracker.DynamicRelationship m) => m.trackerRep.representedCreature == this.bindCreature.abstractCreature))
                    {
                        if ((creature as Lizard).AI.LikeOfPlayer(relationship.trackerRep) > 0.5f)
                            shouldFire = false;
                    }
                }
                if (creature is Scavenger &&
                    (double)(creature as Scavenger).abstractCreature.world.game.session.creatureCommunities.
                    LikeOfPlayer(CreatureCommunities.CommunityID.Scavengers,
                                (creature as Scavenger).abstractCreature.world.game.world.RegionNumber,
                                (this.bindCreature as Player).playerState.playerNumber) > 0.5)
                {
                    shouldFire = false;
                }
                if (creature is Cicada)
                {
                    foreach (RelationshipTracker.DynamicRelationship relationship in (creature as Cicada).AI.relationshipTracker.relationships.
                        Where((RelationshipTracker.DynamicRelationship m) => m.trackerRep.representedCreature == this.bindCreature.abstractCreature))
                    {
                        if ((creature as Cicada).AI.LikeOfPlayer(relationship.trackerRep) > 0.5f)
                            shouldFire = false;
                    }
                }
                if (shouldFire)
                {
                    creature.SetKillTag(this.bindCreature.abstractCreature);
                    if (TemperatureModule.TryGetTemperatureModule(creature, out var heatModule))
                    {
                        return 0.005f;// + 0.001f * level;
                    }
                }
                else
                    return 0f;
            }
            return 0.005f;// + 0.001f * level;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (slatedForDeletetion)
                return;

            if (!kill)
            {
                if (bindCreature.room == null || bindCreature.room != room)
                    Kill();
            }

            if (!kill)
            {
                if (counter < this.lifeTime / 2f)
                {
                    counter++;
                    for (int i = 0; i < lightSources.Length; i++)
                        lightSources[i].setAlpha = 0.1f * ((float)counter) / (this.lifeTime / 2f);
                }
                else
                    Kill();
            }
            else
            {
                if (counter > 0)
                {
                    counter--;
                    for (int i = 0; i < lightSources.Length; i++)
                        lightSources[i].setAlpha = 0.1f * ((float)counter) / (this.lifeTime / 2f);

                    if (counter == 0)
                        this.Destroy();
                }
            }
            if (fire == null || fire.slatedForDeletetion && !kill)
            {
                fire = new HolyFire.HolyFireSprite(bindCreature.mainBodyChunk.pos + relativePosition);
                room.AddObject(fire);
            }

            for (int i = 0; i < lightSources.Length; i++)
            {
                getToPositions[i] = relativePosition; 
                getToRads[i] = 40f;
                lightSources[i].setPos = new Vector2?(Vector2.Lerp(lightSources[i].Pos, bindCreature.mainBodyChunk.pos + getToPositions[i], 0.2f));
                lightSources[i].setRad = new float?(Mathf.Lerp(lightSources[i].Rad, this.getToRads[i], 0.2f));
            }
        }

        public void Kill()
        {
            kill = true;
        }

        public override void Destroy()
        {
            base.Destroy();
            for (int i = 0; i < lightSources.Length; i++)
            {
                if (lightSources[i] != null && !lightSources[i].slatedForDeletetion)
                    lightSources[i].Destroy();
            }
            if(fire != null && !fire.slatedForDeletetion)
            {
                room.RemoveObject(fire);
                fire.Destroy();
            }
        }
    }

    public class SetRotateRingPosAddFire : EmitterModule, IParticleInitModule
    {
        int level;
        float rad;
        float angle;
        Creature owner;

        public SetRotateRingPosAddFire(ParticleEmitter emitter, Creature owner, float rad, float angle, int level) : base(emitter)
        {
            this.rad = rad;
            this.angle = angle;
            this.owner = owner;
            this.level = level;
        }

        public void ApplyInit(Particle particle)
        {
            Vector2 pos;
            for (int i = 0; i < 3; i++){

                pos = emitter.pos + Custom.DegToVec((360f / level) * Mathf.FloorToInt(level * Random.value) - angle) * rad + Custom.RNV() * 5f;
                NoAttachedFire fire = new NoAttachedFire(emitter.room, owner, pos - emitter.pos);
                emitter.room.AddObject(fire);
            }

            pos = emitter.pos + Custom.DegToVec((360f / level) * Mathf.FloorToInt(level * Random.value) - angle) * rad + Custom.RNV() * 5f;
            particle.HardSetPos(pos);
        }
    }
}
