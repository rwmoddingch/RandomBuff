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
                             .Where(i => i != null && i.graphicsModule != null && i.room != null))
                {
                    var flashShield = new FlashShield(player, player.room);
                    player.room.AddObject(flashShield);
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
        public static ConditionalWeakTable<AbstractCreature, Illuminated> IlluminatedFeatures = new ConditionalWeakTable<AbstractCreature, Illuminated>();


        public static int StackLayer => FlashShield.GetBuffData()?.StackLayer ?? 0;

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<FlashShieldBuff, FlashShieldBuffData, FlashShieldBuffEntry>(FlashShield);
        }

        public static void HookOn()
        {
            IL.Room.Update += Room_UpdateIL;
            On.AbstractCreature.ctor += AbstractCreature_ctor;
            On.AbstractCreature.Update += AbstractCreature_Update;
            On.RoomCamera.SpriteLeaser.Update += RoomCamera_SpriteLeaser_Update;

            On.Player.ctor += Player_ctor;
            On.Player.NewRoom += Player_NewRoom;
        }

        private static void Room_UpdateIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                //找到ShouldBeDeferred结束的地方
                if (c.TryGotoNext(MoveType.After,
                    (i) => i.MatchLdloc(10),
                    (i) => i.MatchCall<Room>("ShouldBeDeferred"),
                    (i) => i.MatchStloc(11)))
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldloc_S, (byte)10);
                    c.Emit(OpCodes.Ldloc_S, (byte)11);
                    c.EmitDelegate<Func<Room, UpdatableAndDeletable, bool, bool>>((self, updatableAndDeletable, flag) =>
                    {
                        bool newFlag = false;

                        if ((updatableAndDeletable is Creature creature) &&
                             IlluminatedFeatures.TryGetValue(creature.abstractCreature, out var illuminated))
                        {
                            illuminated.Update();
                            newFlag = illuminated.ShouldSkipUpdate();
                        }

                        return flag || newFlag;
                    });
                    c.Emit(OpCodes.Stloc_S, (byte)11);
                }

                //找到即将绘图的地方
                if (c.TryGotoNext(MoveType.After,
                    (i) => i.MatchLdloc(10),
                    (i) => i.MatchIsinst<PhysicalObject>(),
                    (i) => i.Match(OpCodes.Brfalse_S),
                    (i) => i.MatchLdloc(11)))
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldloc_S, (byte)10);
                    c.EmitDelegate<Func<bool, Room, UpdatableAndDeletable, bool>>((flag, self, updatableAndDeletable) =>
                    {
                        flag = self.ShouldBeDeferred(updatableAndDeletable);
                        return flag;
                    });
                    c.Emit(OpCodes.Stloc_S, (byte)11);
                    c.Emit(OpCodes.Ldloc_S, (byte)11);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        private static void AbstractCreature_ctor(On.AbstractCreature.orig_ctor orig, AbstractCreature self, World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
        {
            orig(self, world, creatureTemplate, realizedCreature, pos, ID);
            if (!IlluminatedFeatures.TryGetValue(self, out _))
            {
                Illuminated illuminated = new Illuminated(self);
                IlluminatedFeatures.Add(self, illuminated);
            }
        }

        private static void AbstractCreature_Update(On.AbstractCreature.orig_Update orig, AbstractCreature self, int time)
        {
            orig(self, time);
            if (!IlluminatedFeatures.TryGetValue(self, out _))
            {
                Illuminated illuminated = new Illuminated(self);
                IlluminatedFeatures.Add(self, illuminated);
            }
        }

        private static void RoomCamera_SpriteLeaser_Update(On.RoomCamera.SpriteLeaser.orig_Update orig, RoomCamera.SpriteLeaser self, float timeStacker, RoomCamera rCam, Vector2 camPos)
        {
            Illuminated illuminated = null;
            bool canFind = (self.drawableObject is GraphicsModule graphicsModule && graphicsModule.owner is Creature creature1 &&
                 IlluminatedFeatures.TryGetValue(creature1.abstractCreature, out illuminated)) ||
                (self.drawableObject is Creature creature2 &&
                 IlluminatedFeatures.TryGetValue(creature2.abstractCreature, out illuminated));
            if (canFind)
            {
                self.drawableObject.ApplyPalette(self, rCam, rCam.currentPalette);
            }

            orig.Invoke(self, timeStacker, rCam, camPos);

            if (canFind)
            {
                illuminated.DrawSprites(self, rCam);
            }
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
                if (self.room != null)
                {
                    flashShield = new FlashShield(self, self.room);
                    self.room.AddObject(flashShield);
                    FlashShieldFeatures.Add(self, flashShield);
                }
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
        #region 外观
        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            base.InitiateSprites(sLeaser, rCam);
            this.room = player.room;
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
            if (!ownerRef.TryGetTarget(out var player) || slatedForDeletetion)
            {
                sLeaser.CleanSpritesAndRemove();
                return;
            }
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
        #endregion
        public override void Update(bool eu)
        {
            if (!ownerRef.TryGetTarget(out var player) || owner.room == null || this.room == null || owner.room != this.room)
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
                    if (this.room.abstractRoom.creatures[k].realizedCreature != null && !room.abstractRoom.creatures[k].realizedCreature.inShortcut)
                    {
                        Creature creature = this.room.abstractRoom.creatures[k].realizedCreature;

                        if (this.ShouldFired(creature))
                        {
                            creature.SetKillTag(this.owner.abstractCreature);
                            if (creature.Template.smallCreature)
                            {
                                creature.Die();
                            }
                            else
                            {
                                creature.Violence(this.owner.mainBodyChunk, Vector2.zero, creature.mainBodyChunk, null, Creature.DamageType.Blunt, 0.01f * level, 0f);
                                if (creature is Lizard)
                                {
                                    for(int m = 0; m < this.room.updateList.Count; m++)
                                    {
                                        if (this.room.updateList[m] is Spark && 
                                            (this.room.updateList[m] as Spark).lizard == creature.graphicsModule &&
                                            Custom.DistLess((this.room.updateList[m] as Spark).pos, this.owner.mainBodyChunk.pos, 30f))
                                        {
                                            this.room.updateList[m].Destroy();
                                        }
                                        else if (this.room.updateList[m] is StationaryEffect && 
                                            (this.room.updateList[m] as StationaryEffect).lizard == creature.graphicsModule &&
                                            Custom.DistLess((this.room.updateList[m] as StationaryEffect).pos, this.owner.mainBodyChunk.pos, 3f))
                                        {
                                            this.room.updateList[m].Destroy();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (this.lightSource == null)
            {
                this.lightSource = new LightSource(Center(1), false, this.color, this);
                this.lightSource.affectedByPaletteDarkness = 0.5f;
                this.lightSource.requireUpKeep = true;
                this.room.AddObject(this.lightSource);
            }
            else
            {
                this.lightSource.stayAlive = true;
                this.lightSource.setPos = Center(1);
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
                emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight", 8, 0.3f, 2f, this.color)));
                emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
                emitter.ApplyParticleModule(new SetRandomLife(emitter, 20, 40));
                emitter.ApplyParticleModule(new SetRandomScale(emitter, new Vector2(2f, 2f), new Vector2(1f, 1f)));
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

        public bool ShouldFired(Creature creature)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return false;

            bool shouldFire = !creature.dead;
            bool inRange = false;

            if (player != null && player.room != null &&
                Custom.DistLess(owner.DangerPos, creature.DangerPos, Radius(level, 0f)))
                inRange = true;

            if (creature is Player)
                shouldFire = false;
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

            return shouldFire && inRange;
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

    internal class Illuminated
    {
        WeakReference<AbstractCreature> ownerRef;
        private int illuminatedCount;
        private int illuminatedCycle;

        private bool isRecorded;
        private int colorCount;
        private int colorCycle;
        private Dictionary<FSprite, Color> oldColor;
        private Dictionary<FSprite, Color> newColor;
        private Dictionary<FSprite, Color[]> oldMeshColor;
        private Dictionary<FSprite, Color[]> newMeshColor;

        private float IlluminatedRatio
        {
            get
            {
                switch (FlashShieldBuffEntry.StackLayer)
                {
                    case 0:
                    case 1:
                        return 0.05f;
                    case 2:
                        return 0.1f;
                    case 3:
                        return 0.15f;
                    case 4:
                        return 0.2f;
                    default:
                        return 0.25f;
                }
            }
        }

        private float ColorRatio
        {
            get
            {
                switch (FlashShieldBuffEntry.StackLayer)
                {
                    case 0:
                    case 1:
                        return 0.4f;
                    case 2:
                        return 0.45f;
                    case 3:
                        return 0.5f;
                    case 4:
                        return 0.55f;
                    default:
                        return 0.6f;
                }
            }
        }

        private bool IsPetrified
        {
            get
            {
                return this.illuminatedCount > Mathf.Max(0f, (1f - this.IlluminatedRatio) * this.illuminatedCycle);
            }
        }

        private bool IsColorPetrified
        {
            get
            {
                return this.colorCount > Mathf.Max(0f, (1f - this.ColorRatio) * this.colorCycle);
            }
        }

        public Illuminated(AbstractCreature c)
        {
            ownerRef = new WeakReference<AbstractCreature>(c);
            illuminatedCount = 0;
            illuminatedCycle = Mathf.RoundToInt(1f / IlluminatedRatio);
            isRecorded = false;
            colorCount = 10;
            colorCycle = 30;
            oldColor = new Dictionary<FSprite, Color>();
            newColor = new Dictionary<FSprite, Color>();
            oldMeshColor = new Dictionary<FSprite, Color[]>();
            newMeshColor = new Dictionary<FSprite, Color[]>();
        }

        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var abstractCreature) ||
                abstractCreature.realizedCreature == null ||
                abstractCreature.realizedCreature.room == null)
                return;
            var creature = abstractCreature.realizedCreature;

            if (isRecorded && creature.room != creature.room.game.cameras[0].room)
            {
                oldColor.Clear();
                newColor.Clear();
                oldMeshColor.Clear();
                newMeshColor.Clear();
                isRecorded = false;
            }

            if (colorCount > 0)
                colorCount--;
            if (colorCount == 0 && ShouldBeFired())
                colorCount = colorCycle;

            //没有石化则逐渐解除冻结
            if (illuminatedCount > 0)
                illuminatedCount--;
            if (illuminatedCount == 0 && ShouldBeFired())
            {
                illuminatedCount = illuminatedCycle;
                //图像
                EmitterUpdate();
            }
            
            if (IsPetrified)
            {
                foreach (var bodyChunk in creature.bodyChunks)
                {
                    bodyChunk.vel *= 0f;
                    bodyChunk.HardSetPosition(bodyChunk.pos);
                }
            }
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam = null)
        {
            if (!ownerRef.TryGetTarget(out var abstractCreature) ||
                abstractCreature.realizedCreature == null ||
                abstractCreature.realizedCreature.room == null)
                return;
            Color backgroundColor = Color.gray;
            if (rCam == null)
                backgroundColor = rCam.currentPalette.blackColor;
            if (IsColorPetrified && !isRecorded)
            {
                isRecorded = true;
                abstractCreature.realizedCreature.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, abstractCreature.realizedCreature.mainBodyChunk.pos, 0.3f, 0.5f);

                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    if (sLeaser.sprites[i] is TriangleMesh)
                    {
                        var mesh = sLeaser.sprites[i] as TriangleMesh;
                        if (mesh.verticeColors == null)
                        {
                            oldColor.Add(sLeaser.sprites[i], sLeaser.sprites[i].color == null ? backgroundColor : sLeaser.sprites[i].color);
                            Color.RGBToHSV(oldColor[sLeaser.sprites[i]], out var hue, out var s, out var light);
                            newColor.Add(sLeaser.sprites[i], Color.HSVToRGB(hue, 0.3f * s, Mathf.Min(1f, light + 0.3f)));
                        }
                        else
                        {
                            oldMeshColor.Add(sLeaser.sprites[i], mesh.verticeColors);
                            Color[] hsvMeshColor = new Color[mesh.vertices.Length];
                            for (int j = 0; j < mesh.vertices.Length; j++)
                            {
                                Color.RGBToHSV(mesh.verticeColors[j], out var hue, out var s, out var light);
                                hsvMeshColor[j] = Color.HSVToRGB(hue, 0.3f * s, Mathf.Min(1f, light + 0.3f));
                            }
                            newMeshColor.Add(sLeaser.sprites[i], hsvMeshColor);
                        }
                    }
                    else
                    {
                        oldColor.Add(sLeaser.sprites[i], sLeaser.sprites[i].color == null ? backgroundColor : sLeaser.sprites[i].color);
                        Color.RGBToHSV(oldColor[sLeaser.sprites[i]], out var hue, out var s, out var light);
                        newColor.Add(sLeaser.sprites[i], Color.HSVToRGB(hue, 0.3f * s, Mathf.Min(1f, light + 0.3f)));
                    }
                }
            }
            //石化
            if (IsColorPetrified)
            {
                float colorLerp = (float)(this.colorCycle - this.colorCount) / ((1f - this.ColorRatio) * this.colorCycle);
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    if (sLeaser.sprites[i] is TriangleMesh)
                    {
                        var mesh = sLeaser.sprites[i] as TriangleMesh;
                        if (mesh.verticeColors == null && newColor.ContainsKey(sLeaser.sprites[i]))
                        {
                            sLeaser.sprites[i].color = Color.Lerp(oldColor[sLeaser.sprites[i]], newColor[sLeaser.sprites[i]], colorLerp);
                        }
                        else if (newMeshColor.ContainsKey(sLeaser.sprites[i]))
                        {
                            for (int j = 0; j < mesh.vertices.Length; j++)
                                mesh.verticeColors[j] = Color.Lerp(oldMeshColor[sLeaser.sprites[i]][j], newMeshColor[sLeaser.sprites[i]][j], colorLerp);
                        }
                    }
                    else if (newColor.ContainsKey(sLeaser.sprites[i]))
                    {
                        sLeaser.sprites[i].color = Color.Lerp(oldColor[sLeaser.sprites[i]], newColor[sLeaser.sprites[i]], colorLerp);
                    }
                }
            }
            //还原
            else if (isRecorded)
            {
                float colorLerp = (float)this.colorCount / (this.ColorRatio * this.colorCycle);
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    if (sLeaser.sprites[i] is TriangleMesh)
                    {
                        var mesh = sLeaser.sprites[i] as TriangleMesh;
                        if (mesh.verticeColors == null && newColor.ContainsKey(sLeaser.sprites[i]))
                        {
                            sLeaser.sprites[i].color = Color.Lerp(oldColor[sLeaser.sprites[i]], newColor[sLeaser.sprites[i]], colorLerp);
                        }
                        else if (newMeshColor.ContainsKey(sLeaser.sprites[i]))
                        {
                            for (int j = 0; j < mesh.vertices.Length; j++)
                                mesh.verticeColors[j] = Color.Lerp(oldMeshColor[sLeaser.sprites[i]][j], newMeshColor[sLeaser.sprites[i]][j], colorLerp);
                        }
                    }
                    else if (newColor.ContainsKey(sLeaser.sprites[i]))
                    {
                        sLeaser.sprites[i].color = Color.Lerp(oldColor[sLeaser.sprites[i]], newColor[sLeaser.sprites[i]], colorLerp);
                    }
                }
                if (this.colorCount <= 1f)
                {
                    for (int i = 0; i < sLeaser.sprites.Length; i++)
                    {
                        if (sLeaser.sprites[i] is TriangleMesh)
                        {
                            var mesh = sLeaser.sprites[i] as TriangleMesh;
                            if (mesh.verticeColors == null && newColor.ContainsKey(sLeaser.sprites[i]))
                            {
                                sLeaser.sprites[i].color = oldColor[sLeaser.sprites[i]];
                            }
                            else if (newMeshColor.ContainsKey(sLeaser.sprites[i]))
                            {
                                for (int j = 0; j < mesh.vertices.Length; j++)
                                    mesh.verticeColors[j] = oldMeshColor[sLeaser.sprites[i]][j];
                            }
                        }
                        else if (newColor.ContainsKey(sLeaser.sprites[i]))
                        {
                            sLeaser.sprites[i].color = oldColor[sLeaser.sprites[i]];
                        }
                    }
                    oldColor.Clear();
                    newColor.Clear();
                    oldMeshColor.Clear();
                    newMeshColor.Clear();
                    isRecorded = false;
                }
            }
        }

        public bool ShouldSkipUpdate()
        {
            if (!ownerRef.TryGetTarget(out var creature))
                return false;

            if (IsPetrified)
            {
                return true;
            }

            return false;
        }

        public bool ShouldBeFired()
        {
            if (!ownerRef.TryGetTarget(out var abstractCreature) ||
                abstractCreature.realizedCreature == null ||
                abstractCreature.realizedCreature.room == null)
                return false;

            bool shouldFire = true;
            var self = abstractCreature.realizedCreature;

            foreach (var player in self.room.game.AlivePlayers.Select(i => i.realizedCreature as Player)
                                     .Where(i => i != null && i.graphicsModule != null))
            {
                if (FlashShieldBuffEntry.FlashShieldFeatures.TryGetValue(player, out var flashShield))
                {
                    shouldFire = flashShield.ShouldFired(self);
                }
                else
                    return false;
            }

            return shouldFire;
        }

        private void EmitterUpdate()
        {
            if (!ownerRef.TryGetTarget(out var abstractCreature) ||
                abstractCreature.realizedCreature == null ||
                abstractCreature.realizedCreature.room == null)
                return;
            var creature = abstractCreature.realizedCreature;
            var emitter = new ParticleEmitter(creature.room);
            int randomBody = Random.Range(0, creature.bodyChunks.Length);
            emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 5, false));
            emitter.ApplyEmitterModule(new BindEmitterToPhysicalObject(emitter, creature));

            emitter.ApplyParticleSpawn(new RateSpawnerModule(emitter, 10, 20));

            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("pixel", "")));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight", 8, 0.3f, 0.15f, Color.white)));
            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 5, 10));
            emitter.ApplyParticleModule(new SetRandomScale(emitter, new Vector2(8f, 3f), new Vector2(6f, 4f)));
            emitter.ApplyParticleModule(new SetRandomPos(emitter, 1.2f * creature.bodyChunks[randomBody].rad + 10f));
            emitter.ApplyParticleModule(new SetRingRotation(emitter, creature.bodyChunks[randomBody].pos, 90f));
            emitter.ApplyParticleModule(new SetRingVelocity(emitter, creature.bodyChunks[randomBody].pos, 0));


            emitter.ApplyParticleModule(new ScaleOverLife(emitter, (p, a) =>
            {
                return p.setScaleXY * (1f - a);
            }));

            ParticleSystem.ApplyEmitterAndInit(emitter);
        }
    }
}
