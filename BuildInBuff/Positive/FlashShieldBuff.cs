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
    internal class FlashShieldBuff : Buff<FlashShieldBuff, FlashShieldBuffData>
    {
        public override BuffID ID => FlashShieldBuffEntry.FlashShield;

        public FlashShieldBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    var flashShield = new FlashShield(player, player.room);
                    FlashShieldBuffEntry.FlashShieldFeatures.Add(player, flashShield);
                }
            }
        }
    }

    internal class FlashShieldBuffData : BuffData
    {
        public override BuffID ID => FlashShieldBuffEntry.FlashShield;
    }

    internal class FlashShieldBuffEntry : IBuffEntry
    {
        public static BuffID FlashShield = new BuffID("FlashShield", true);

        public static ConditionalWeakTable<Player, FlashShield> FlashShieldFeatures = new ConditionalWeakTable<Player, FlashShield>();


        public static int StackLayer
        {
            get
            {
                return FlashShield.GetBuffData().StackLayer;
            }
        }

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<FlashShieldBuff, FlashShieldBuffData, FlashShieldBuffEntry>(FlashShield);
        }

        public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
            On.Player.NewRoom += Player_NewRoom;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!FlashShieldFeatures.TryGetValue(self, out _))
            {
                FlashShield flashShield = new FlashShield(self, self.room);
                self.room.AddObject(flashShield);
                FlashShieldFeatures.Add(self, flashShield);
            }
        }

        private static void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            orig(self, newRoom);

            if (FlashShieldFeatures.TryGetValue(self, out var flashShield))
            {
                FlashShieldFeatures.Remove(self);
                flashShield.Destroy();
                flashShield = new FlashShield(self, self.room);
                self.room.AddObject(flashShield);
                FlashShieldFeatures.Add(self, flashShield);
            }
            else
            {
                FlashShield newFlashShield = new FlashShield(self, self.room);
                self.room.AddObject(newFlashShield);
                FlashShieldFeatures.Add(self, newFlashShield);
            }
        }
    }

    internal class FlashShield : CosmeticSprite
    {
        WeakReference<Player> ownerRef;
        Player owner;
        LightSource lightSource;
        Color color;
        float averageVoice;
        int emitterCount;

        bool shouldFire;
        int level;
        int firstSprite;
        int totalSprites;
        float expand;
        float lastExpand;
        float getToExpand;
        float push;
        float lastPush;
        float getToPush;

        public FlashShield(Player player, Room room)
        {
            this.ownerRef = new WeakReference<Player>(player);
            this.owner = player;
            this.room = room;
            this.averageVoice = 0f;
            this.color = new Color(227f / 255f, 171f / 255f, 78f / 255f);
            this.firstSprite = 0;
            this.totalSprites = 3;
            this.getToExpand = 1f;
            this.getToPush = 1f;
            this.emitterCount = 30;
            this.level = FlashShieldBuffEntry.StackLayer;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            base.InitiateSprites(sLeaser, rCam);
            this.room = rCam.room;
            sLeaser.sprites = new FSprite[totalSprites];
            sLeaser.sprites[this.firstSprite + 0] = new FSprite("Futile_White", true);
            sLeaser.sprites[this.firstSprite + 0].shader = rCam.game.rainWorld.Shaders["VectorCircle"];
            sLeaser.sprites[this.firstSprite + 0].color = this.color;

            sLeaser.sprites[this.firstSprite + 1] = new FSprite("Futile_White", true);
            sLeaser.sprites[this.firstSprite + 1].shader = rCam.game.rainWorld.Shaders["FlatLightNoisy"];
            sLeaser.sprites[this.firstSprite + 1].color = this.color;

            sLeaser.sprites[this.firstSprite + 2] = new FSprite("Futile_White", true);
            sLeaser.sprites[this.firstSprite + 2].shader = rCam.game.rainWorld.Shaders["LightSource"];
            this.AddToContainer(sLeaser, rCam, null);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Items");
            }
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].RemoveFromContainer();
            }
            newContatiner.AddChild(sLeaser.sprites[0]);
            rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[1]);
            rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[2]);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            Vector2 vector = this.Center(timeStacker);
            for (int k = 0; k < totalSprites; k++)
            {
                sLeaser.sprites[this.firstSprite + k].x = vector.x - camPos.x;
                sLeaser.sprites[this.firstSprite + k].y = vector.y - camPos.y;
                sLeaser.sprites[this.firstSprite + k].scale = this.Radius(level, timeStacker) / 8f;
            }

            sLeaser.sprites[this.firstSprite + 0].alpha = 3f / this.Radius(level, timeStacker);
            sLeaser.sprites[this.firstSprite + 1].alpha = 0.5f;
            sLeaser.sprites[this.firstSprite + 1].scale *= 1.5f;

            sLeaser.sprites[this.firstSprite + 0].isVisible = false;
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            base.ApplyPalette(sLeaser, rCam, palette);

            for (int i = 0; i < totalSprites; i++)
            {
                sLeaser.sprites[firstSprite + i].color = this.color;
            }
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
                this.getToPush = 1f;
            }

            if (owner.room != null)
            {
                for (int k = 0; k < this.room.abstractRoom.creatures.Count; k++)
                {
                    if (this.room.abstractRoom.creatures[k].realizedCreature != null &&
                        !(this.room.abstractRoom.creatures[k].realizedCreature is Player) &&
                        Custom.DistLess(owner.DangerPos, this.room.abstractRoom.creatures[k].realizedCreature.DangerPos, Radius(level, 0f)))
                    {
                        Creature creature = this.room.abstractRoom.creatures[k].realizedCreature;
                        shouldFire = true;
                        if (creature is Overseer && (creature as Overseer).AI.LikeOfPlayer(this.owner.abstractCreature) > 0.5f)
                        {
                            shouldFire = false;
                        }
                        if (creature is Lizard)
                        {
                            foreach (RelationshipTracker.DynamicRelationship relationship in (creature as Lizard).AI.relationshipTracker.relationships.
                                Where((RelationshipTracker.DynamicRelationship m) => m.trackerRep.representedCreature == this.owner.abstractCreature))
                            {
                                if ((creature as Lizard).AI.LikeOfPlayer(relationship.trackerRep) > 0.5f)
                                    shouldFire = false;
                            }
                        }
                        if (creature is Scavenger &&
                            (double)(creature as Scavenger).abstractCreature.world.game.session.creatureCommunities.
                            LikeOfPlayer(CreatureCommunities.CommunityID.Scavengers,
                                        (creature as Scavenger).abstractCreature.world.game.world.RegionNumber,
                                        this.owner.playerState.playerNumber) > 0.5)
                        {
                            shouldFire = false;
                        }
                        if (creature is Cicada)
                        {
                            foreach (RelationshipTracker.DynamicRelationship relationship in (creature as Cicada).AI.relationshipTracker.relationships.
                                Where((RelationshipTracker.DynamicRelationship m) => m.trackerRep.representedCreature == this.owner.abstractCreature))
                            {
                                if ((creature as Cicada).AI.LikeOfPlayer(relationship.trackerRep) > 0.5f)
                                    shouldFire = false;
                            }
                        }
                        if (shouldFire)
                        {
                            this.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Hard, creature.mainBodyChunk);
                            creature.SetKillTag(this.owner.abstractCreature);
                            creature.firstChunk.vel *= 0.9f / Mathf.Pow(level, 0.3f);
                            creature.Violence(this.owner.mainBodyChunk, 
                                              Custom.DirVec(this.owner.DangerPos, creature.DangerPos).normalized * Radius(level, 0f) / (Custom.Dist(this.owner.DangerPos, creature.DangerPos) + 0.5f * Radius(level, 0f)), 
                                              creature.firstChunk, null, Creature.DamageType.Blunt, 0.01f * level, 0f);
                        }
                    }
                }
            }

            if (this.lightSource == null)
            {
                this.lightSource = new LightSource(this.pos, false, this.color, this);
                this.lightSource.affectedByPaletteDarkness = 0.5f;
                this.room.AddObject(this.lightSource);
            }
            else
            {
                this.lightSource.setPos = new Vector2?(this.pos);
                this.lightSource.setRad = 4f * Radius(level, 0f) + 150f;
                this.lightSource.setAlpha = new float?(1f);
                if (this.lightSource.slatedForDeletetion || this.lightSource.room != this.room)
                {
                    this.lightSource = null;
                }
            }

            if (emitterCount > 0)
                emitterCount--;
            EmitterUpdate();
        }

        public override void Destroy()
        {
            base.Destroy();
        }

        private void EmitterUpdate()
        {
            if (emitterCount == 0)
            {
                emitterCount = 240;
                var emitter = new ParticleEmitter(this.room);
                emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 240, false));
                emitter.ApplyEmitterModule(new BindEmitterToPhysicalObject(emitter, this.owner));

                emitter.ApplyParticleSpawn(new RateSpawnerModule(emitter, Mathf.FloorToInt(0.1f * Radius(this.level, 0f)), Mathf.FloorToInt(0.1f * Radius(this.level, 0f))));

                emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("tinyStar", "", 11, 1f, 1f, this.color)));
                emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight", 8, 0.3f, 0.15f, this.color)));
                emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
                emitter.ApplyParticleModule(new SetRandomLife(emitter, 20, 40));
                emitter.ApplyParticleModule(new SetRandomScale(emitter, new Vector2(4f, 4f), new Vector2(3f, 3f)));
                emitter.ApplyParticleModule(new SetRingPos(emitter, Radius(this.level, 0f)));
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
                emitter.ApplyParticleModule(new ScaleOverLife(emitter, (p, a) =>
                {
                    return p.setScaleXY * 4f * a * (1f - a);
                }));
                emitter.ApplyParticleModule(new PositionOverLife(emitter, (p, a) =>
                {
                    return (p.pos - emitter.pos).normalized * Radius(this.level, 0f) + emitter.pos;
                }));
                emitter.ApplyParticleModule(new RotationOverLife(emitter, (p, a) =>
                {
                    return p.rotation - 2f;
                }));

                ParticleSystem.ApplyEmitterAndInit(emitter);
            }
        }

        public Vector2 Center(float timeStacker)
        {
            Vector2 vector = Vector2.Lerp(this.owner.bodyChunks[0].lastPos, this.owner.bodyChunks[0].pos, timeStacker);
            return vector + Custom.DirVec(vector, Vector2.Lerp(this.owner.bodyChunks[1].lastPos, this.owner.bodyChunks[1].pos, timeStacker)) * 5f;
        }

        private float Radius(float ring, float timeStacker)
        {
            return (5f + 2f * ring + Mathf.Lerp(this.lastPush, this.push, timeStacker) - 0.5f * this.averageVoice) * Mathf.Lerp(this.lastExpand, this.expand, timeStacker) * 20f;
        }
    }
}
