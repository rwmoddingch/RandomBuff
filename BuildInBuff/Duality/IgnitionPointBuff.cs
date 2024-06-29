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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static BuiltinBuffs.Duality.LittleBoyNukeBuffEntry;
using static BuiltinBuffs.Positive.StagnantForcefieldPlayerModule;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Duality
{
    internal class IgnitionPointBuffEntry : IBuffEntry
    {
        public static BuffID ignitionPointBuffID = new BuffID("IgnitionPoint", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<IgnitionPointBuffEntry>(ignitionPointBuffID);
            TemperatrueModule.AddProvider(new CreatureProvider());
        }

        public static void HookOn()
        {
            //On.Creature.Update += Creature_Update;
            //On.GraphicsModule.ctor += GraphicsModule_ctor;
            On.Creature.Violence += Creature_Violence;
            On.Room.ShouldBeDeferred += Room_ShouldBeDeferred;
            On.RoomCamera.SpriteLeaser.Update += DeferredDrawUpdate;
        }

        private static void DeferredDrawUpdate(On.RoomCamera.SpriteLeaser.orig_Update orig, RoomCamera.SpriteLeaser self, float timeStacker, RoomCamera rCam, Vector2 camPos)
        {
            bool freeze = false;
            TemperatrueModule module = null;
            if(self.drawableObject is UpdatableAndDeletable updatable && TemperatrueModule.TryGetTemperatureModule(updatable, out module) && module.freeze)
            {
                freeze = true;
            }
            else if(self.drawableObject is GraphicsModule graphics && TemperatrueModule.TryGetTemperatureModule(graphics.owner, out module) && module.freeze)
            {
                freeze = true;
            }

            if (freeze)
                timeStacker = 0f;

            orig.Invoke(self, timeStacker, rCam, camPos);

            if (module != null && (module is CreatureHeatModule cModule) && cModule.freezeIce != null)
            {
                if(!cModule.freezeIce.melt)
                {
                    for (int i = 0; i < self.sprites.Length; i++)
                    {
                        FreezeIce.ApplyColor(self.sprites[i], cModule.freezeIce.origColors[i], cModule.freezeIce.iceColors[i], cModule.freezeIce.alpha);
                    }
                }
                else
                {
                    for (int i = 0; i < self.sprites.Length; i++)
                    {
                        FreezeIce.ApplyColor(self.sprites[i], cModule.freezeIce.origColors[i], cModule.freezeIce.iceColors[i], 0f);
                    }
                    cModule.DestroyIce();
                }
            }
        }

        private static bool Room_ShouldBeDeferred(On.Room.orig_ShouldBeDeferred orig, Room self, UpdatableAndDeletable obj)
        {
            var result = orig.Invoke(self, obj);
            if (!result)
            {
                if(TemperatrueModule.TryGetTemperatureModule(obj, out var module))
                {
                    module.Update(obj);
                    if (module.freeze)
                        result = true;
                }
            }
            return result;
        }

        private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            orig.Invoke(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
            if(type == Creature.DamageType.Explosion && TemperatrueModule.TryGetTemperatureModule(self, out var module))
            {
                module.AddTemperature(damage * 0.3f);
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
                if (TemperatrueModule.TryGetTemperatureModule(creature, out var module))
                {
                    label.text = $"temperature : {module.temperature}\nignitedPoint : {module.ignitingPoint}\nextinguishPoint : {module.extinguishPoint}\nlowHeatRate : {module.coolOffRate}\nburn : {module.burn}\n\nfreezePoint : {module.freezePoint}\nunfreezePoint:{module.unfreezePoint}\nwarmUpRate : {module.warmUpRate}\nfreeze : {module.freeze}";
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

    public partial class TemperatrueModule
    {
        public float temperature;

        public float ignitingPoint;
        public float extinguishPoint;
        public float coolOffRate;
        public bool burn;

        public float freezePoint;//温度会降低到负数，但冰点不需要赋值为负数，运算时会自动判断
        public float unfreezePoint;//负数
        public float warmUpRate;
        public bool freeze;

        public Color lastFreezeCol = Color.blue * 0.3f + Color.white * 0.7f;

        public virtual void Update(UpdatableAndDeletable updatableAndDeletable)
        {
        }

        public virtual void AddTemperature(float temperature, Color? freezeCol = null)
        {
            this.temperature += temperature;
            if(freezeCol != null)
                lastFreezeCol = freezeCol.Value;
        }
    }

    //静态部分
    public partial class TemperatrueModule
    {
        public static ConditionalWeakTable<UpdatableAndDeletable, TemperatrueModule> temperatureModuleMapping = new ConditionalWeakTable<UpdatableAndDeletable, TemperatrueModule>();
        public static List<ITemperatureModuleProvider> providers = new List<ITemperatureModuleProvider>();
        
        public static bool TryGetTemperatureModule(UpdatableAndDeletable target, out TemperatrueModule temperatrueModule)
        {
            if(temperatureModuleMapping.TryGetValue(target, out temperatrueModule))
                return true;

            foreach(var provider in providers)
            {
                if(provider.ProvideThisObject(target))
                {
                    temperatrueModule = provider.ProvideModule(target);
                    temperatureModuleMapping.Add(target, temperatrueModule);
                    return true;
                }
            }
            temperatrueModule = null;
            return false;
        }

        public static void AddProvider(ITemperatureModuleProvider provider)
        {
            providers.Add(provider);
        }

        public interface ITemperatureModuleProvider
        {
            bool ProvideThisObject(UpdatableAndDeletable target);
            TemperatrueModule ProvideModule(UpdatableAndDeletable target);
        }
    }

    public class CreatureHeatModule : TemperatrueModule
    {
        public BurnFire fire;
        public FreezeIce freezeIce;

        public CreatureHeatModule(Creature creature)
        {
            float explosiveDmgResist = (creature.Template.damageRestistances[(int)Creature.DamageType.Explosion, 0] > 0f ? creature.Template.damageRestistances[(int)Creature.DamageType.Explosion, 0] : 1f);
            ignitingPoint = creature.TotalMass * explosiveDmgResist;
            extinguishPoint = ignitingPoint * (0.6f + 0.2f * Mathf.InverseLerp(0f, 5f, creature.Template.bodySize));
            coolOffRate = creature.bodyChunks.Length * explosiveDmgResist * 0.6f * Mathf.Lerp(0.15f, 0.5f, creature.Template.bodySize / 10f);

            freezePoint = creature.TotalMass * Mathf.Lerp(1f, 2f, Mathf.InverseLerp(0f, 10f, creature.Template.bodySize)) * (creature.Template.BlizzardAdapted ? 2f : 1f);
            unfreezePoint = freezePoint * (0.6f + 0.2f * Mathf.InverseLerp(0f, 5f, creature.Template.bodySize));
            warmUpRate = creature.bodyChunks.Length * 0.4f;
        }

        public override void Update(UpdatableAndDeletable updateable)
        {
            var creature = updateable as Creature;
            if (creature.room == null)
                return;

            //热量计算
            foreach (var heatSource in creature.room.updateList.Where((u) => u is IHeatingCreature).Select((u) => u as IHeatingCreature))
            {
                AddTemperature(heatSource.GetHeat(creature.mainBodyChunk.pos));
            }

            if (temperature > 0)
            {
                float rateDecreaseHeat = temperature - coolOffRate / 40f;
                float submersionDecreaseHeat = temperature * (1f - creature.Submersion / 2f);

                if (submersionDecreaseHeat < rateDecreaseHeat)
                    temperature = Mathf.Lerp(rateDecreaseHeat, submersionDecreaseHeat, creature.Submersion);
                else
                    temperature = rateDecreaseHeat;

                if (temperature < 0)
                    temperature = 0f;

                if (!burn)
                {
                    if(temperature > ignitingPoint)
                        burn = true;
                }
                else
                {
                    if (temperature < extinguishPoint)
                        burn = false;
                }
            }
            else if(temperature < 0)
            {
                float rateWarmUp = temperature + warmUpRate / 40f;

                temperature = rateWarmUp;

                if (temperature > 0)
                    temperature = 0f;

                if (!freeze)
                {
                    if (temperature < -freezePoint)
                        freeze = true;
                }
                else
                {
                    if (temperature > -unfreezePoint)
                        freeze = false;
                }
            }

            if (Input.GetKey(KeyCode.H))
                temperature += 0.1f;
            else if (Input.GetKey(KeyCode.J))
                temperature -= 0.1f;

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

            if (freeze)
            {
                if (freezeIce != null && freezeIce.slatedForDeletetion)
                    freezeIce = null;

                if(freezeIce == null && creature.room != null)
                {
                    freezeIce = new FreezeIce(creature, creature.DangerPos);
                    creature.room.AddObject(freezeIce);
                }
            }
            else
            {
                if(freezeIce != null)
                {
                    freezeIce.Destroy();
                    freezeIce = null;
                }
            }
        }

        public void DestroyIce()
        {
            freezeIce.Destroy();
            freezeIce = null;
        }


        void BurnBehaviour(Creature creature)
        {
            foreach(var crit in creature.room.updateList.Where((u) => (u is Creature) && u != creature).Select((u) => u as Creature))
            {
                if (TryGetTemperatureModule(crit, out var module))
                {
                    if (Mathf.Abs(creature.mainBodyChunk.pos.x - crit.mainBodyChunk.pos.x) + Mathf.Abs(creature.mainBodyChunk.pos.y - crit.mainBodyChunk.pos.y) < 40f)
                    {
                        module.AddTemperature(coolOffRate / 40f);
                    }
                }
            }
        }
    }

    public class CreatureProvider : TemperatrueModule.ITemperatureModuleProvider
    {
        public TemperatrueModule ProvideModule(UpdatableAndDeletable target)
        {
            return new CreatureHeatModule(target as Creature);
        }

        public bool ProvideThisObject(UpdatableAndDeletable target)
        {
            return (target is Creature creature && creature.State is HealthState);
        }
    }

    public class PlayerHeatModule : CreatureHeatModule
    {
        int killCooldown;
        public PlayerHeatModule(Creature creature) : base(creature)
        {
            if(!(creature.State as PlayerState).isPup)
            {
                ignitingPoint = 10f;
                extinguishPoint = 9f;
                coolOffRate = 0.5f;
            }
            else
            {
                ignitingPoint = 8f;
                extinguishPoint = 7f;
                coolOffRate = 0.3f;
            }
        }

        public override void Update(UpdatableAndDeletable u)
        {
            base.Update(u);

            var creature = u as Creature;
            if (creature.room == null)
                return;

            if(temperature > ignitingPoint)
            {
                temperature = ignitingPoint;
                if (creature.State.alive && killCooldown == 0)
                {
                    creature.Die();
                    killCooldown = 40;
                }
            }
            if (killCooldown > 0)
                killCooldown--;
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

    public class FreezeIce : CosmeticSprite
    {
        public UpdatableAndDeletable binder;
        ParticleEmitter emitter;

        Color color;
        public Color[][] origColors;
        public Color[][] iceColors;

        public float alpha;
        float lastAlpha;

        public bool melt;
        float rad;

        public FreezeIce(UpdatableAndDeletable updatableAndDeletable, Vector2 pos)
        {
            room = updatableAndDeletable.room;
            binder = updatableAndDeletable;
            this.pos = pos;
            if (updatableAndDeletable is PhysicalObject obj)
                rad = obj.bodyChunks[0].rad * 3f;
            if (TemperatrueModule.TryGetTemperatureModule(updatableAndDeletable, out var module))
                color = module.lastFreezeCol;
            CreateIceSparkle();
            BuffUtils.Log("IgnitionPoint", "Create FreezeIce");
        }

        public override void Update(bool eu)
        {
            if (slatedForDeletetion)
                return;

            if (binder.slatedForDeletetion || binder.room != room)
                Destroy();


            if (emitter.slateForDeletion)
                CreateIceSparkle();

            lastAlpha = alpha;
            if (TemperatrueModule.TryGetTemperatureModule(binder, out var temperature))
            {
                alpha = Mathf.InverseLerp(temperature.unfreezePoint, temperature.freezePoint, -temperature.temperature);
            }
            else
                Destroy();
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[0];
            foreach (var sleaser in rCam.spriteLeasers)
            {
                if(sleaser.drawableObject is GraphicsModule graphics && binder is PhysicalObject physicalObject && physicalObject.graphicsModule == graphics)
                {
                    BuffUtils.Log("IgnitionPoint", $"Create for {binder}");
                    RecordColors(sleaser.sprites, rCam);
                    break;
                }
                if (sleaser.drawableObject == binder)
                {
                    RecordColors(sleaser.sprites, rCam);
                    break;
                }   
            }
        }

        void RecordColors(FSprite[] origs, RoomCamera roomCamera)
        {
            origColors = new Color[origs.Length][];

            for (int i = 0; i < origs.Length; i++)
            {
                origColors[i] = GetColors(origs[i]);
            }
            iceColors = new Color[origColors.Length][];

            if(TemperatrueModule.TryGetTemperatureModule(binder, out var temperature))
            {
                float hue = Custom.RGB2HSL(temperature.lastFreezeCol).x;
                for (int i = 0; i < iceColors.Length; i++)
                {
                    iceColors[i] = new Color[origColors[i].Length];
                    for (int t = 0; t < iceColors[i].Length; t++)
                    {
                        var orig = Custom.RGB2HSL(iceColors[i][t]);
                        iceColors[i][t] = Color.Lerp(Custom.HSL2RGB(hue, orig.y, orig.z), temperature.lastFreezeCol, orig.z*0.5f + 0.5f);
                    }
                }
            }
        }

        Color[] GetColors(FSprite sprite)
        {
            Color[] result;
            if(sprite is CustomFSprite customFSprite)
            {
                result = new Color[4];
                for(int i = 0;i < 4;i++)
                    result[i] = customFSprite.verticeColors[i];
            }
            else if(sprite is  TriangleMesh triangleMesh && triangleMesh.customColor)
            {
                result = new Color[triangleMesh.verticeColors.Length];
                for (int i = 0; i < triangleMesh.verticeColors.Length; i++)
                {
                    result[i] = triangleMesh.verticeColors[i];
                }
            }
            else
            {
                result = new Color[1];
                result[0] = sprite.color;
            }
            return result;
        }

        public static void ApplyColor(FSprite sprite, Color[] origs, Color[] colors,float alpha)
        {
            if (sprite is CustomFSprite customFSprite)
            {
                for (int i = 0; i < 4; i++)
                    customFSprite.verticeColors[i] = Color.Lerp(origs[i], colors[i], alpha);
            }
            else if (sprite is TriangleMesh triangleMesh && triangleMesh.customColor)
            {
                for (int i = 0; i < triangleMesh.verticeColors.Length; i++)
                {
                    triangleMesh.verticeColors[i] = Color.Lerp(origs[i], colors[i], alpha);
                }
            }
            else
            {
                sprite.color = Color.Lerp(origs[0], colors[0], alpha);
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            binder = null;
            emitter?.Die();
            BuffUtils.Log("IgnitionPoint", "Die");
        }

        void CreateIceSparkle()
        {
            emitter = new ParticleEmitter(room);
            
            emitter.pos = pos;
            emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 40, false));

            emitter.ApplyParticleSpawn(new RateSpawnerModule(emitter, 260, 20));

            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight", alpha: 0.5f)));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("pixel", "", constCol: Color.white)));
            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 40 * 2 - 20, 40 * 2));
            emitter.ApplyParticleModule(new SetConstColor(emitter, color));
            emitter.ApplyParticleModule(new SetRandomScale(emitter, 2f, 2.5f));
            emitter.ApplyParticleModule(new SetRandomPos(emitter, rad));

            emitter.ApplyParticleModule(new AlphaOverLife(emitter,
                (p, l) =>
                {
                    if (l < 0.2f)
                    {
                        return l * 5f;
                    }
                    else if (l > 0.8f)
                    {
                        return 1f - (l - 0.8f) * 5f;
                    }
                    return 1f * (Random.value * 0.1f + 0.9f);
                }));
            ParticleSystem.ApplyEmitterAndInit(emitter);
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
