using BuiltinBuffs.Duality;
using BuiltinBuffs.Negative;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RandomBuffUtils.ParticleSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static BuiltinBuffs.Positive.FlameThrower;
using RWCustom;
using RandomBuff;
using RandomBuff.Core.Game;

namespace BuiltinBuffs.Positive
{
    internal class AggressiveVentingBuff : IgnitionPointBaseBuff<AggressiveVentingBuff, AggressiveVentingBuffData>
    {
        public override BuffID ID => AggressiveVentingBuffEntry.aggresiveVentingBuffID;

        bool canTrigger = true;
        bool autoTriggered;
        public override bool Triggerable => canTrigger;
        public override bool Active => canTrigger;

        public AggressiveVentingBuff() : base()
        {
            MyTimer = new DownCountBuffTimer(CallBack, 30, autoReset: false, useDefaultStrategy: false);
            MyTimer.ApplyStrategy(new SpanTimerDisplayStrategy(
                new SpanTimerDisplayStrategy.BuffTimeSpan(1, 4),
                new SpanTimerDisplayStrategy.BuffTimeSpan(10, 13),
                new SpanTimerDisplayStrategy.BuffTimeSpan(27, 30),
                new SpanTimerDisplayStrategy.BuffTimeSpan(42, 45)));
        }

        public void CallBack(BuffTimer timer, RainWorldGame game)
        {
            canTrigger = true;
            if(autoTriggered)
            {
                (MyTimer as DownCountBuffTimer).ResetLimit(0, 30);
                autoTriggered = false;
            }
        }

        public override bool Trigger(RainWorldGame game)
        {
            foreach(var player in game.Players)
            {
                if (player.realizedCreature == null)
                    continue;
                var p = player.realizedCreature as Player;

                if(p.room == null)
                    continue;

                if(TemperatureModule.TryGetTemperatureModule(p, out var temperature))
                {
                    if(temperature.temperature > 0f)
                    {
                        float rad = temperature.temperature * 20f;
                        var heat = temperature.temperature * 0.5f;
                        temperature.AddTemperature(-temperature.temperature);
                        CreateAggressiveVentingParticle(p.room, p.DangerPos, rad);

                        foreach (var crit in p.room.updateList.Where((u) => u is Creature && u.room != null && !(u is Player)).Select((u) => u as Creature))
                        {
                            if (Vector2.Distance(p.DangerPos, crit.DangerPos) > rad)
                                continue;
                            if (TemperatureModule.TryGetTemperatureModule(crit, out var module))
                            {
                                module.AddTemperature(heat);
                            }
                        }
                    }
                }
            }

            canTrigger = false;
            if (autoTriggered)
                (MyTimer as DownCountBuffTimer).ResetLimit(0, 45);
            MyTimer.Reset();

            return false;
        }

        public override void Update(RainWorldGame game)
        {
            base.Update(game);

            foreach (var player in game.Players)
            {
                if (player.realizedCreature == null)
                    continue;
                var p = player.realizedCreature as Player;

                if (p.room == null)
                    continue;

                if (TemperatureModule.TryGetTemperatureModule(p, out var temperature))
                {
                    if (temperature.temperature > temperature.extinguishPoint)
                    {
                        autoTriggered = true;
                        BuffPoolManager.Instance.TriggerBuff(ID, true);
                        return;
                    }
                }
            }
        }

        public void CreateAggressiveVentingParticle(Room room, Vector2 pos, float rad)
        {
            var emitter = new ParticleEmitter(room);
            emitter.lastPos = emitter.pos = pos;

            emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter,100));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", FakeCreatureEntry.Turbulent.name, 8, 0.1f, 4f, new Color(0.3f, 0.5f, 10f))));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight", 8, alpha: 0.05f, scale: 6f, constCol: Color.black)));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam(FlameThrowerBuffEntry.flameVFX1, "StormIsApproaching.AdditiveDefault", 8, alpha: 0.1f, scale: 2f)));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam(FlameThrowerBuffEntry.flameVFX0, "StormIsApproaching.AdditiveDefault", 8)));

            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 40, 44));
            emitter.ApplyParticleModule(new SetRandomPos(emitter, 0f));
            emitter.ApplyParticleModule(new SetRandomRotation(emitter, 0f, 360f));

            emitter.ApplyParticleModule(new ColorOverLife(emitter, (p, l) =>
            {
                if (l < 0.1f)
                    return Color.Lerp(RoomFlame.flameCol_0, RoomFlame.flameCol_1, l * 10f);
                else if (l < 0.25f)
                    return Color.Lerp(RoomFlame.flameCol_1, RoomFlame.flameCol_2, (l - 0.1f) / 0.15f);
                else if (l < 0.4f)
                    return Color.Lerp(RoomFlame.flameCol_2, RoomFlame.flameCol_3, (l - 0.25f) / 0.15f);
                else if (l < 0.8f)
                    return Color.Lerp(RoomFlame.flameCol_3, RoomFlame.flameCol_4, (l - 0.4f) / 0.4f);
                else
                    return Color.Lerp(RoomFlame.flameCol_4, RoomFlame.flameCol_5, (l - 0.8f) / 0.2f);
            }));

            emitter.ApplyParticleModule(new ScaleOverLife(emitter, (p, l) =>
            {
                if (l < 0.2f)
                    return Mathf.Lerp(0.25f, 4f, l / 0.2f);
                else
                    return Mathf.Lerp(4f, 6f, (l - 0.2f) / 2f);
            }));

            emitter.ApplyParticleModule(new AlphaOverLife(emitter, (p, l) =>
            {
                if (l >= 0.3f)
                    return Mathf.Lerp(1f, 0f, (l - 0.3f) / 0.7f) * 0.05f;
                return 0.05f;
            }));

            emitter.ApplyParticleModule(new VelocityOverLife(emitter, (p, l) =>
            {
                Vector2 dir = Custom.DegToVec(p.randomParam1 * 360f);
                return dir * rad * 2f * (1f - l) / 40f;
            }));

            ParticleSystem.ApplyEmitterAndInit(emitter);
        }
    }

    internal class AggressiveVentingBuffData : BuffData
    {
        public override BuffID ID => AggressiveVentingBuffEntry.aggresiveVentingBuffID;
    }

    internal class AggressiveVentingBuffEntry : IBuffEntry
    {
        public static BuffID aggresiveVentingBuffID = new BuffID("AggressiveVenting", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<AggressiveVentingBuff, AggressiveVentingBuffData, AggressiveVentingBuffEntry>(aggresiveVentingBuffID);
        }
    }
}
