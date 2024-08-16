using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuff.Core.Buff;
using UnityEngine;
using System.Reflection;
using System;
using MonoMod.Cil;
using System.Linq;
using System.Runtime.CompilerServices;
using RWCustom;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using Smoke;
using RandomBuff;

namespace BuiltinBuffs.Duality
{
    internal class DustExplosionBuff : Buff<DustExplosionBuff, DustExplosionBuffData>
    {
        public override BuffID ID => DustExplosionBuffEntry.DustExplosion;

        public DustExplosionBuff()
        {
        }
    }

    internal class DustExplosionBuffData : BuffData
    {
        public override BuffID ID => DustExplosionBuffEntry.DustExplosion;
    }

    internal class DustExplosionBuffEntry : IBuffEntry
    {
        public static BuffID DustExplosion = new BuffID("DustExplosion", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<DustExplosionBuff, DustExplosionBuffData, DustExplosionBuffEntry>(DustExplosion);
            TemperatureModule.AddProvider(new SmokeProvider());
            TemperatureModule.AddProvider(new CosmeticSpriteProvider());
        }

        public static void HookOn()
        {
        }
    }

    public class SmokeProvider : TemperatureModule.ITemperatureModuleProvider
    {
        public TemperatureModule ProvideModule(UpdatableAndDeletable target)
        {
            return new SmokeHeatModule(target as SmokeSystem);
        }

        public bool ProvideThisObject(UpdatableAndDeletable target)
        {
            if (DustExplosionBuff.Instance == null)
                return false;
            return (target is SmokeSystem);
        }
    }

    public class CosmeticSpriteProvider : TemperatureModule.ITemperatureModuleProvider
    {
        public TemperatureModule ProvideModule(UpdatableAndDeletable target)
        {
            return new CosmeticSpriteHeatModule(target as CosmeticSprite);
        }

        public bool ProvideThisObject(UpdatableAndDeletable target)
        {
            if (DustExplosionBuff.Instance == null)
                return false;
            return (target is CosmeticSprite && CosmeticSpriteHeatModule.IsCosmeticSpriteNeedModule(target as CosmeticSprite));
        }
    }

    public class SmokeHeatModule : TemperatureModule
    {
        //public BurnFire fire;
        //public FreezeIce freezeIce;
        private Color explodeColor;
        private float submersion;

        public float Submersion
        {
            get
            {
                return submersion;
            }
            set
            {
                submersion = value;
            }
        }

        public SmokeHeatModule(SmokeSystem smoke)
        {
            float explosiveDmgResist = SmokeHeatResist(smoke.smokeType);
            ignitingPoint = explosiveDmgResist;//smoke.particles.Count * 
            extinguishPoint = ignitingPoint * 0.6f;// + 0.2f * Mathf.InverseLerp(0f, 5f, smoke.Template.bodySize));
            coolOffRate = explosiveDmgResist * 0.6f;// smoke.particles.Count * * Mathf.Lerp(0.15f, 0.5f, smoke.Template.bodySize / 10f)

            freezePoint = 1f;//smoke.particles.Count * 
            unfreezePoint = freezePoint * 0.6f;
            warmUpRate = 0.4f; //smoke.particles.Count*
            submersion = 0;
            explodeColor = new Color(227f / 255f, 171f / 255f, 78f / 255f);
        }

        public override void Update(UpdatableAndDeletable updateable)
        {
            var smoke = updateable as SmokeSystem;
            if (smoke.room == null)
                return;

            //热量计算
            foreach (var heatSource in smoke.room.updateList.Where((u) => u is IHeatingCreature).Select((u) => u as IHeatingCreature))
            {
                foreach (var p in smoke.particles)
                    AddTemperature(heatSource.GetHeat(smoke, p.pos));
            }

            if (temperature > 0)
            {
                submersion = 0f;
                foreach (var p in smoke.particles)
                {
                    if(smoke.room.PointSubmerged(p.pos))
                        submersion += 1f / smoke.particles.Count;
                }

                float rateDecreaseHeat = temperature - coolOffRate / 40f;
                float submersionDecreaseHeat = temperature * (1f - this.Submersion / 2f);

                if (submersionDecreaseHeat < rateDecreaseHeat)
                    temperature = Mathf.Lerp(rateDecreaseHeat, submersionDecreaseHeat, this.Submersion);
                else
                    temperature = rateDecreaseHeat;

                if (temperature < 0)
                    temperature = 0f;

                if (!burn)
                {
                    if (temperature > ignitingPoint)
                        burn = true;
                }
                else
                {
                    if (temperature < extinguishPoint)
                        burn = false;
                }
            }
            else if (temperature < 0)
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

            if (BuffPlugin.DevEnabled)
            {
                if (Input.GetKey(KeyCode.H))
                    temperature += 0.1f;
                else if (Input.GetKey(KeyCode.J))
                    temperature -= 0.1f;
            }

            if (burn)
            {/*
                if (fire != null && fire.slatedForDeletetion)
                    fire = null;

                if (fire == null && smoke.room != null)
                {
                    //fire = new BurnFire(smoke.room, smoke);
                    smoke.room.AddObject(fire);
                }*/

                BurnBehaviour(smoke);
            }
            else
            {/*
                if (fire != null)
                {
                    fire.Kill();
                    fire = null;
                }*/
            }
            /*
            if (freeze)
            {
                if (freezeIce != null && freezeIce.slatedForDeletetion)
                    freezeIce = null;

                if (freezeIce == null && smoke.room != null)
                {
                    freezeIce = new FreezeIce(smoke, smoke.DangerPos);
                    smoke.room.AddObject(freezeIce);
                }
            }
            else
            {
                if (freezeIce != null)
                {
                    freezeIce.Destroy();
                    freezeIce = null;
                }
            }*/
        }

        public void DestroyIce()
        {/*
            freezeIce.Destroy();
            freezeIce = null;*/
        }

        private void BurnBehaviour(SmokeSystem smoke)
        {
            Vector2 centroidPos = Vector2.zero;
            float averageRad = 0f;
            float num = smoke.particles.Count;
            foreach (SmokeSystem.SmokeSystemParticle p in smoke.particles)
            {
                p.life = 0f;
                centroidPos += p.pos / num;
            }
            foreach (SmokeSystem.SmokeSystemParticle p in smoke.particles)
            {
                averageRad += 2f * Mathf.Sqrt((centroidPos - p.pos).magnitude) / num;
            }

            foreach (var crit in smoke.room.updateList.Where((u) => (u is Creature)).Select((u) => u as Creature))
            {
                if (TryGetTemperatureModule(crit, out var module))
                {
                    foreach (SmokeSystem.SmokeSystemParticle p in smoke.particles)
                    {
                        if (Mathf.Abs(p.pos.x - crit.mainBodyChunk.pos.x) + Mathf.Abs(p.pos.y - crit.mainBodyChunk.pos.y) < averageRad)
                        {
                            module.AddTemperature(coolOffRate);
                        }
                    }
                }
            }
            foreach (var crit in smoke.room.updateList.Where((u) => (u is SmokeSystem) && u != smoke).Select((u) => u as SmokeSystem))
            {
                if (TryGetTemperatureModule(crit, out var module))
                {
                    foreach (SmokeSystem.SmokeSystemParticle p1 in smoke.particles)
                    {
                        foreach (SmokeSystem.SmokeSystemParticle p2 in crit.particles)
                        {
                            if (Mathf.Abs(p1.pos.x - p2.pos.x) + Mathf.Abs(p1.pos.y - p2.pos.y) < averageRad)
                            {
                                module.AddTemperature(coolOffRate);
                            }
                        }
                    }
                }
            }
            foreach (var crit in smoke.room.updateList.Where((u) => (u is CosmeticSprite)).Select((u) => u as CosmeticSprite))
            {
                if (TryGetTemperatureModule(crit, out var module))
                {
                    foreach (SmokeSystem.SmokeSystemParticle p in smoke.particles)
                    {
                        if (Mathf.Abs(p.pos.x - crit.pos.x) + Mathf.Abs(p.pos.y - crit.pos.y) < averageRad + CosmeticSpriteHeatModule.GetRad(crit))
                        {
                            module.AddTemperature(coolOffRate);
                        }
                    }
                }
            }

            smoke.room.PlaySound(SoundID.Bomb_Explode, centroidPos);
            smoke.room.AddObject(new Explosion(smoke.room, null, centroidPos, 7, 2.5f * averageRad, 6.2f, 10f, 280f, 0f, null, 0.3f, 160f, 1f));
            smoke.room.AddObject(new Explosion(smoke.room, null, centroidPos, 7, 10f * averageRad, 4f, 0f, 400f, 0f, null, 0.3f, 200f, 1f));
            smoke.room.AddObject(new Explosion.ExplosionLight(centroidPos, averageRad, 1f, 7, this.explodeColor));
            smoke.room.AddObject(new Explosion.ExplosionLight(centroidPos, 0.8f * averageRad, 1f, 3, new Color(1f, 1f, 1f)));
            smoke.room.AddObject(new Explosion.ExplosionLight(centroidPos, 5f * averageRad, 2f, 60, this.explodeColor));
            smoke.room.AddObject(new ExplosionSpikes(smoke.room, centroidPos, 9, 2.5f * averageRad, 5f, 5f, 90f, this.explodeColor));
            smoke.room.AddObject(new ShockWave(centroidPos, 3f * averageRad, 0.285f, 30, true));
            smoke.room.AddObject(new ShockWave(centroidPos, 15f * averageRad, 0.85f, 18, false));

            smoke.Destroy();
        }

        private static float SmokeHeatResist(SmokeSystem.SmokeType type)
        {
            if (type == SmokeSystem.SmokeType.Steam)
                return 1000f;
            if (type == SmokeSystem.SmokeType.FireSmoke)
                return 100f;
            return 1f;
        }
    }

    public class CosmeticSpriteHeatModule : TemperatureModule
    {
        //public BurnFire fire;
        //public FreezeIce freezeIce;
        private Color explodeColor;
        private float submersion;
        private float rad;

        public float Submersion
        {
            get
            {
                return submersion;
            }
            set
            {
                submersion = value;
            }
        }

        public CosmeticSpriteHeatModule(CosmeticSprite sp)
        {
            float explosiveDmgResist = CosmeticSpriteHeatResist(sp);
            ignitingPoint = explosiveDmgResist;
            extinguishPoint = ignitingPoint * 0.6f;
            coolOffRate = explosiveDmgResist * 0.6f;

            freezePoint = 1f;
            unfreezePoint = freezePoint * 0.6f;
            warmUpRate = 0.4f;
            submersion = 0;
            explodeColor = new Color(227f / 255f, 171f / 255f, 78f / 255f);
        }

        public override void Update(UpdatableAndDeletable updateable)
        {
            var sp = updateable as CosmeticSprite;
            if (sp.room == null)
                return;

            //热量计算
            foreach (var heatSource in sp.room.updateList.Where((u) => u is IHeatingCreature).Select((u) => u as IHeatingCreature))
            {
                AddTemperature(heatSource.GetHeat(sp, sp.pos));
            }

            if (temperature > 0)
            {
                submersion = 0f;

                if (sp.room.PointSubmerged(sp.pos))
                    submersion = 1f;

                float rateDecreaseHeat = temperature - coolOffRate / 40f;
                float submersionDecreaseHeat = temperature * (1f - this.Submersion / 2f);

                if (submersionDecreaseHeat < rateDecreaseHeat)
                    temperature = Mathf.Lerp(rateDecreaseHeat, submersionDecreaseHeat, this.Submersion);
                else
                    temperature = rateDecreaseHeat;

                if (temperature < 0)
                    temperature = 0f;

                if (!burn)
                {
                    if (temperature > ignitingPoint)
                        burn = true;
                }
                else
                {
                    if (temperature < extinguishPoint)
                        burn = false;
                }
            }
            else if (temperature < 0)
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
                BurnBehaviour(sp);
            }
        }
        private void BurnBehaviour(CosmeticSprite sp)//由于爆炸，只运行一帧
        {
            foreach (var crit in sp.room.updateList.Where((u) => (u is Creature)).Select((u) => u as Creature))
            {
                if (TryGetTemperatureModule(crit, out var module))
                {
                    float d = (sp.pos - crit.mainBodyChunk.pos).magnitude;
                    if (d < 1.1f * rad)
                    {
                        module.AddTemperature(2f * coolOffRate);
                    }
                    else if (d < 2.5f * rad)
                    {
                        module.AddTemperature(coolOffRate);
                    }
                }
            }
            foreach (var crit in sp.room.updateList.Where((u) => (u is SmokeSystem)).Select((u) => u as SmokeSystem))
            {
                if (TryGetTemperatureModule(crit, out var module))
                {
                    foreach (SmokeSystem.SmokeSystemParticle p in crit.particles)
                    {
                        if (Mathf.Abs(sp.pos.x - p.pos.x) + Mathf.Abs(sp.pos.y - p.pos.y) < 1.1f * rad)
                        {
                            module.AddTemperature(2f * coolOffRate);
                        }
                    }
                }
            }
            foreach (var crit in sp.room.updateList.Where((u) => (u is CosmeticSprite) && u != sp).Select((u) => u as CosmeticSprite))
            {
                if (TryGetTemperatureModule(crit, out var module))
                {
                    float d = (sp.pos - crit.pos).magnitude;
                    if (d < rad + GetRad(crit))
                    {
                        module.AddTemperature(2f * coolOffRate);
                    }
                    else if(d < 2.5f * (rad + GetRad(crit)))
                    {
                        module.AddTemperature(coolOffRate);
                    }
                }
            }

            sp.room.PlaySound(SoundID.Bomb_Explode, sp.pos);
            sp.room.AddObject(new Explosion(sp.room, null, sp.pos, 7, 2.5f * rad, 6.2f, 10f, 280f, 0f, null, 0.3f, 160f, 1f));
            sp.room.AddObject(new Explosion(sp.room, null, sp.pos, 7, 10f * rad, 4f, 0f, 400f, 0f, null, 0.3f, 200f, 1f));
            sp.room.AddObject(new Explosion.ExplosionLight(sp.pos, rad, 1f, 7, this.explodeColor));
            sp.room.AddObject(new Explosion.ExplosionLight(sp.pos, 0.8f * rad, 1f, 3, new Color(1f, 1f, 1f)));
            sp.room.AddObject(new Explosion.ExplosionLight(sp.pos, 5f * rad, 2f, 60, this.explodeColor));
            sp.room.AddObject(new ExplosionSpikes(sp.room, sp.pos, 9, 2.5f * rad, 5f, 5f, 90f, this.explodeColor));
            sp.room.AddObject(new ShockWave(sp.pos, 3f * rad, 0.285f, 30, true));
            sp.room.AddObject(new ShockWave(sp.pos, 15f * rad, 0.85f, 18, false));

            sp.Destroy();
        }

        private static float CosmeticSpriteHeatResist(CosmeticSprite sp)
        {
            if (sp is SporeCloud)
                return 1f;
            return 1f;
        }

        public static bool IsCosmeticSpriteNeedModule(CosmeticSprite sp)
        {
            if (sp is SporeCloud)
                return true;
            return false;
        }

        public static float GetRad(CosmeticSprite sp)
        {
            if (sp is SporeCloud)
                return 7f * (sp as SporeCloud).rad;
            return 1f + 0.5f * sp.vel.magnitude;
        }
    }
}
