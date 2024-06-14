using BuiltinBuffs.Positive;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BuiltinBuffs.Duality
{
    internal class IgnitionPointBuffEntry : IBuffEntry
    {
        public static BuffID ignitionPointBuffID = new BuffID("IgnitionPoint", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<IgnitionPointBuffEntry>(ignitionPointBuffID);
        }

        public static void HookOn()
        {
            On.Creature.Update += Creature_Update;
            //On.GraphicsModule.ctor += GraphicsModule_ctor;
            On.Creature.Violence += Creature_Violence;
            //On.ScavengerBomb.Explode += ScavengerBomb_Explode;
        }

        private static void ScavengerBomb_Explode(On.ScavengerBomb.orig_Explode orig, ScavengerBomb self, BodyChunk hitChunk)
        {
            orig.Invoke(self, hitChunk);

            int count = 10;
            for(int i = 0; i < count; i++)
            {
                float angle = 360f * i / count;
                Vector2 vel = Custom.RNV() * 10f;
                Vector2 pos = self.room.MiddleOfTile(self.room.GetTilePosition(self.firstChunk.pos));
                var newNapalm = new Napalm(self.room, 40 * 15, 60f, 2f, pos, vel);
                self.room.AddObject(newNapalm);
            }
        }

        private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            orig.Invoke(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
            if(type == Creature.DamageType.Explosion && CreatureHeatModule.TryGetHeatModule(self, out var module))
            {
                module.AddHeat(damage * 0.3f);
            }
        }

        private static void GraphicsModule_ctor(On.GraphicsModule.orig_ctor orig, GraphicsModule self, PhysicalObject ow, bool internalContainers)
        {
            orig.Invoke(self, ow, internalContainers);
            if(ow is Creature creature)
            {
                creature.room.AddObject(new CreatureHeatTestGraphics(creature, creature.room));
            }
        }

        private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
        {
            orig.Invoke(self, eu);
            if(CreatureHeatModule.TryGetHeatModule(self, out var heatModule))
            {
                heatModule.Update(self);
            }
        }

        public static ParticleEmitter CreateFireSparkle(Room room, Vector2 pos, float rad)
        {
            var emitter = new ParticleEmitter(room);
            emitter.lastPos = emitter.pos = pos;
            CreatFireSparkleInternal(emitter, rad);
            ParticleSystem.ApplyEmitterAndInit(emitter);
            return emitter;
        }

        public static ParticleEmitter CreateFireSparkle(Room room, Creature creature, int bodyChunk)
        {
            var emitter = new ParticleEmitter(room);
            emitter.ApplyEmitterModule(new BindEmitterToPhysicalObject(emitter, creature, bodyChunk));
            CreatFireSparkleInternal(emitter, creature.bodyChunks[bodyChunk].rad);
            ParticleSystem.ApplyEmitterAndInit(emitter);
            return emitter;
        }

        static ParticleEmitter CreatFireSparkleInternal(ParticleEmitter emitter, float rad/*, Vector2 pos, float rad*/)
        {
            emitter.ApplyEmitterModule(new RateSpawnerModule(emitter, 20, 2));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "")));
            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 40, 60));
            emitter.ApplyParticleModule(new SetRandomPos(emitter, rad));
            //emitter.ApplyParticleModule(new SetRandomColor(emitter, 0.01f, 0.07f, 1f, 0.5f));
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

            emitter.ApplyParticleModule(new VelocityOverLife(emitter, (p, l) =>
            {
                Vector2 vel = p.vel;
                vel += Custom.RNV() * 0.2f;
                return vel;
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

    public class CreatureHeatTestGraphics : CosmeticSprite
    {
        FLabel label;
        Creature creature;
        public CreatureHeatTestGraphics(Creature creature ,Room room)
        {
            this.creature = creature;
            this.room = room;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            label = new FLabel(Custom.GetFont(), "") { alignment = FLabelAlignment.Left};
            sLeaser.containers = new FContainer[1] { new FContainer() };
            AddToContainer(sLeaser, rCam, null);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            rCam.ReturnFContainer("Water").AddChild(sLeaser.containers[0]);
            sLeaser.containers[0].AddChild(label);
        }

        public override void Update(bool eu)
        {
            if(label != null)
            {
                if (CreatureHeatModule.TryGetHeatModule(creature, out var module))
                {
                    label.text = $"heat : {module.heat}\nignitedPoint : {module.ignitedPoint}\nextinguishPoint : {module.extinguishPoint}\nlowHeatRate : {module.lowHeatRate}\nburn : {module.burn}";
                }
                else
                    label.text = "Not ignitable";
            }
            base.Update(eu);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if(label != null)
            {
                Vector2 pos = Vector2.Lerp(creature.firstChunk.lastPos, creature.firstChunk.pos, timeStacker) + Vector2.up * 60f - camPos;
                label.SetPosition(pos);
            }
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        public override void Destroy()
        {
            if(label != null)
            {
                label.RemoveFromContainer();
            }
            base.Destroy();
        }
    }

    public class CreatureHeatModule
    {
        public static ConditionalWeakTable<Creature, CreatureHeatModule> heatModuleMapping = new ConditionalWeakTable<Creature, CreatureHeatModule>();

        //public ParticleEmitter fireEmitter;

        public float heat;
        public float ignitedPoint;
        public float extinguishPoint;
        public float lowHeatRate;
        public bool burn;

        public BurnFire fire;

        public CreatureHeatModule(Creature creature)
        {
            float explosiveDmgResist = (creature.Template.damageRestistances[(int)Creature.DamageType.Explosion, 0] > 0f ? creature.Template.damageRestistances[(int)Creature.DamageType.Explosion, 0] : 1f);
            ignitedPoint = creature.TotalMass * explosiveDmgResist;
            extinguishPoint = ignitedPoint * (0.6f + 0.2f * Mathf.InverseLerp(0f, 5f, creature.Template.bodySize));
            lowHeatRate = creature.bodyChunks.Length * explosiveDmgResist * 0.6f * Mathf.Lerp(0.15f, 0.5f, creature.Template.bodySize / 10f);
        }

        public void Update(Creature creature)
        {
            //热量计算
            foreach (var heatSource in creature.room.updateList.Where((u) => u is IHeatingCreature).Select((u) => u as IHeatingCreature))
            {
                AddHeat(heatSource.GetHeat(creature.mainBodyChunk.pos));
            }

            if (heat > 0)
            {
                float rateDecreaseHeat = heat - lowHeatRate / 40f;
                float submersionDecreaseHeat = heat * (1f - creature.Submersion / 2f);

                if (submersionDecreaseHeat < rateDecreaseHeat)
                    heat = Mathf.Lerp(rateDecreaseHeat, submersionDecreaseHeat, creature.Submersion);
                else
                    heat = rateDecreaseHeat;

                if (!burn)
                {
                    if(heat > ignitedPoint)
                        burn = true;
                }
                else
                {
                    if (heat < extinguishPoint)
                        burn = false;
                }
            }
            if (Input.GetKey(KeyCode.H))
            {
                heat += 0.1f;
            }

            if (burn)
            {
                (creature.State as HealthState).health -= 0.0025f;

                if (fire != null && fire.slatedForDeletetion)
                    fire = null;

                if (fire == null && creature.room != null)
                {
                    fire = new BurnFire(creature.room, creature);
                    creature.room.AddObject(fire);
                }

                BurnBehaviour(creature);
            }
            else
            {
                if (fire != null)
                {
                    fire.Kill();
                    fire = null;
                }
            }
        }

        public void AddHeat(float addHeat)
        {
            heat += addHeat;
        }

        void BurnBehaviour(Creature creature)
        {
            foreach(var crit in creature.room.updateList.Where((u) => (u is Creature) && u != creature).Select((u) => u as Creature))
            {
                if (TryGetHeatModule(crit, out var module))
                {
                    if (Mathf.Abs(creature.mainBodyChunk.pos.x - crit.mainBodyChunk.pos.x) + Mathf.Abs(creature.mainBodyChunk.pos.y - crit.mainBodyChunk.pos.y) < 40f)
                    {
                        module.AddHeat(lowHeatRate / 40f);
                    }
                }
            }
        }

        public static bool TryGetHeatModule(Creature creature, out CreatureHeatModule heatModule)
        {
            if(heatModuleMapping.TryGetValue(creature, out heatModule))
            {
                return true;
            }

            if(creature.State is HealthState)
            {
                heatModule = new CreatureHeatModule(creature);
                heatModuleMapping.Add(creature, heatModule);
                return true;
            }

            heatModule = null;
            return false;
        }

    }

    public class BurnFire : UpdatableAndDeletable
    {
        Creature bindCreature;

        LightSource[] lightSources;
        float[] getToRads;
        Vector2[] getToPositions;
        
        Vector2 creaturePos;

        int counter;
        bool kill;

        public BurnFire(Room room, Creature bindCraeture) 
        {
            this.room = room;
            this.bindCreature = bindCraeture;
            lightSources = new LightSource[3];
            getToPositions = new Vector2[this.lightSources.Length];
            getToRads = new float[this.lightSources.Length];
            for (int i = 0; i < this.lightSources.Length; i++)
            {
                lightSources[i] = new LightSource(bindCraeture.mainBodyChunk.pos, false, Custom.HSL2RGB(Mathf.Lerp(0.01f, 0.07f, (float)i / (float)(this.lightSources.Length - 1)), 1f, 0.5f), this);
                room.AddObject(this.lightSources[i]);
                lightSources[i].setAlpha = 0f;
            }
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
                creaturePos = bindCreature.mainBodyChunk.pos;
                if (counter < 20)
                {
                    counter++;
                    for(int i = 0;i < lightSources.Length;i++)
                        lightSources[i].setAlpha = counter / 20f;
                }
            }
            else
            {
                if(counter > 0)
                {
                    counter--;
                    for (int i = 0; i < lightSources.Length; i++)
                        lightSources[i].setAlpha = counter / 20f;

                    if (counter == 0)
                        Destroy();
                }
            }
   
            for (int i = 0; i < lightSources.Length; i++)
            {
                if (UnityEngine.Random.value < 0.2f)
                {
                    getToPositions[i] = Custom.RNV() * 2f * UnityEngine.Random.value * bindCreature.Template.bodySize;
                }
                if (UnityEngine.Random.value < 0.2f)
                {
                    getToRads[i] = Mathf.Lerp(50f, Mathf.Lerp(400f, 200f, (float)i / (float)(this.lightSources.Length - 1)), Mathf.Pow(UnityEngine.Random.value, 0.5f));
                }
                lightSources[i].setPos = new Vector2?(Vector2.Lerp(lightSources[i].Pos, bindCreature.mainBodyChunk.pos + getToPositions[i], 0.2f));
                lightSources[i].setRad = new float?(Mathf.Lerp(lightSources[i].Rad, this.getToRads[i], 0.2f));
            }
            for(int i = 0;i < bindCreature.bodyChunks.Length;i++)
            {
                if(UnityEngine.Random.value > 0.5f)
                    room.AddObject(new HolyFire.HolyFireSprite(bindCreature.bodyChunks[i].pos + Custom.RNV() * UnityEngine.Random.value * bindCreature.bodyChunks[i].rad));
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
                lightSources[i].Destroy();
        }
    }

    public interface IHeatingCreature
    {
        float GetHeat(Vector2 pos);
    }

    public class Napalm : UpdatableAndDeletable, IHeatingCreature
    {
        ParticleEmitter emitter;

        LightSource[] lightSources;
        float[] getToRads;
        Vector2[] getToPositions;

        public Vector2 pos;
        public Vector2 lastPos;
        public Vector2 vel;

        float rad;
        float heat;
        float velDamping = 0f;

        int life;
        int initLife;
        float burnRate
        {
            get
            {
                if (life < 20)
                    return life / 20f;
                //else if (life > initLife - 20)
                //    return (life - initLife) / 20f;
                return 1f;
            }
        }
        bool inTerrain;

        SharedPhysics.TerrainCollisionData scratchTerrainCollisionData;

        public Napalm(Room room, int life, float rad, float heat, Vector2 pos, Vector2 vel)
        {
            this.room = room;
            this.rad = rad;
            this.heat = heat;

            lastPos = this.pos = pos;

            this.vel = vel;
            initLife = this.life = life;

            lightSources = new LightSource[2];
            getToPositions = new Vector2[this.lightSources.Length];
            getToRads = new float[this.lightSources.Length];
            for (int i = 0; i < this.lightSources.Length; i++)
            {
                lightSources[i] = new LightSource(pos, false, Custom.HSL2RGB(Mathf.Lerp(0.01f, 0.07f, (float)i / (float)(this.lightSources.Length - 1)), 1f, 0.5f), this);
                room.AddObject(this.lightSources[i]);
                lightSources[i].setAlpha = burnRate * 0.2f;
            }
            emitter = IgnitionPointBuffEntry.CreateFireSparkle(room, pos, rad * 0.5f);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (slatedForDeletetion)
                return;

            vel.y -= this.room.gravity;
            lastPos = pos;
            pos += vel;

            emitter.pos = pos;

            bool submerged;
            if (ModManager.MSC)
            {
                submerged = room.PointSubmerged(new Vector2(pos.x, pos.y - 5f));
            }
            else
            {
                submerged = pos.y - 5f <= room.FloatWaterLevel(pos.x);
            }
            if (submerged)
                Destroy();


            if (life > 0)
            {
                life--;
                if (life == 0)
                    Destroy();
            }


            bool flag = false;
            IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(room, room.GetTilePosition(lastPos), room.GetTilePosition(pos));
            if (intVector != null)
            {
                FloatRect floatRect = Custom.RectCollision(pos, lastPos, room.TileRect(intVector.Value).Grow(4f));
                pos = floatRect.GetCorner(FloatRect.CornerLabel.D);
                if (floatRect.GetCorner(FloatRect.CornerLabel.B).x < 0f)
                {
                    vel.x = Mathf.Abs(vel.x) * velDamping;
                    vel.y = vel.y * velDamping;
                    flag = true;
                }
                else if (floatRect.GetCorner(FloatRect.CornerLabel.B).x > 0f)
                {
                    vel.x = -Mathf.Abs(vel.x) * velDamping;
                    vel.y = vel.y * velDamping;
                    flag = true;
                }
                else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y < 0f)
                {
                    vel.y = Mathf.Abs(vel.y) * velDamping;
                    vel.x = vel.x * velDamping;
                    flag = true;
                }
                else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y > 0f)
                {
                    vel.y = -Mathf.Abs(vel.y) * velDamping;
                    vel.x = vel.x * velDamping;
                    flag = true;
                }
            }
            if (!flag)
            {
                Vector2 vector5 = vel;
                SharedPhysics.TerrainCollisionData terrainCollisionData = scratchTerrainCollisionData.Set(pos, lastPos, vel, 4f, new IntVector2(0, 0), true);
                terrainCollisionData = SharedPhysics.VerticalCollision(room, terrainCollisionData);
                terrainCollisionData = SharedPhysics.HorizontalCollision(room, terrainCollisionData);
                terrainCollisionData = SharedPhysics.SlopesVertically(room, terrainCollisionData);
                pos = terrainCollisionData.pos;
                vel = terrainCollisionData.vel;
                if (terrainCollisionData.contactPoint.x != 0)
                {
                    vel.x = Mathf.Abs(vector5.x) * velDamping * -(float)terrainCollisionData.contactPoint.x;
                    vel.y = vel.y * velDamping;
                    inTerrain = true;
                }
                if (terrainCollisionData.contactPoint.y != 0)
                {
                    vel.y = Mathf.Abs(vector5.y) * velDamping * -(float)terrainCollisionData.contactPoint.y;
                    vel.x = vel.x * velDamping;
                    inTerrain = true;
                }
            }
            if (inTerrain)
                vel = Vector2.zero;

            if (UnityEngine.Random.value < burnRate)
                room.AddObject(new HolyFire.HolyFireSprite(pos + Custom.RNV() * Mathf.Pow(UnityEngine.Random.value, 2f) * rad * (inTerrain ? 1f : 0f)) /*{ vel = this.vel * 0.8f }*/);

            for (int i = 0; i < lightSources.Length; i++)
            {
                if (UnityEngine.Random.value < 0.2f)
                {
                    getToPositions[i] = Custom.RNV() * 2f * UnityEngine.Random.value * rad;
                }
                if (UnityEngine.Random.value < 0.2f)
                {
                    getToRads[i] = Mathf.Lerp(50f, Mathf.Lerp(400f, 200f, (float)i / (float)(this.lightSources.Length - 1)), Mathf.Pow(UnityEngine.Random.value, 0.5f));
                }

                if (inTerrain)
                {
                    lightSources[i].setPos = Vector2.Lerp(lightSources[i].Pos, pos + getToPositions[i], 0.2f);
                }
                else
                {
                    lightSources[i].setPos = pos;
                }
                lightSources[i].setRad = Mathf.Lerp(lightSources[i].Rad, this.getToRads[i], 0.2f);
                lightSources[i].setAlpha = burnRate * 0.2f;
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            foreach(var lightSource in lightSources) {  lightSource.Destroy(); }
            emitter.Die();
        }

        public float GetHeat(Vector2 pos)
        {
            float dist = Vector2.Distance(this.pos, pos);
            if (dist > rad)
                return 0f;

            return (heat / 40f) * (dist / rad) * burnRate;
        }
    }
}
