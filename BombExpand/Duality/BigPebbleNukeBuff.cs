﻿using BuiltinBuffs.Positive;
using MonoMod.Cil;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static BuiltinBuffs.Positive.StagnantForcefieldPlayerModule;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Duality
{
    internal class BigPebbleNukeBuffEntry : IBuffEntry
    {
        public static Color RadiatCol = Color.green * 0.7f + Color.blue * 0.3f;
        public static BuffID BigPebbleNukeID = new BuffID("BigPebbleNuke", true);
        public static SoundID nukeSound = new SoundID("BigPebbleNuke", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<BigPebbleNukeBuffEntry>(BigPebbleNukeID);
        }

        public static void LoadAssets()
        {
            BuffSounds.LoadSound(nukeSound, BigPebbleNukeID.GetStaticData().AssetPath, new BuffSoundGroupData(), new BuffSoundData("bigpebblenuke1A", 0.25f));
        }

        public static void HookOn()
        {
            IL.ScavengerBomb.Explode += ScavengerBomb_Explode1;
            On.ScavengerBomb.Explode += ScavengerBomb_Explode;
        }


        private static void ScavengerBomb_Explode(On.ScavengerBomb.orig_Explode orig, ScavengerBomb self, BodyChunk hitChunk)
        {
            self.explodeColor = RadiatCol;
            orig.Invoke(self, hitChunk);

            foreach(var field in self.room.updateList.Where(u => u is RadiationField).Select(u => u as RadiationField))
            {
                if(Vector2.Distance(field.pos, self.firstChunk.pos) < field.rad)
                {
                    field.StackField();
                    return;
                }
            }

            self.room.AddObject(new RadiationField(self, self.room, self.firstChunk.pos, self.thrownBy, 300f, 40 * 10, 1f));
            if (self.room.game.cameras[0].room == self.room)
                BuffPostEffectManager.AddEffect(new BurstRadialBlurEffect(0, 0.17f, 0.05f, 0.075f, (self.firstChunk.pos - self.room.game.cameras[0].pos) / Custom.rainWorld.screenSize));
        }
        private static void ScavengerBomb_Explode1(MonoMod.Cil.ILContext il)
        {
            ILCursor c1 = new ILCursor(il);
            if (c1.TryGotoNext(MoveType.After,
                (i) => i.MatchNewobj<Explosion>(),
                (i) => i.MatchCallvirt<Room>("AddObject")))
            {
                if (c1.TryGotoPrev(MoveType.After,
                    (i) => i.MatchLdcR4(280f)))
                {
                    c1.EmitDelegate<Func<float, float>>((orig) =>
                    {
                        return orig * 2f;
                    });
                }

                if (c1.TryGotoPrev(MoveType.After,
                    (i) => i.MatchLdcR4(2f)))
                {
                    c1.EmitDelegate<Func<float, float>>((orig) =>
                    {
                        return orig * 15f;
                    });
                }

                if (c1.TryGotoPrev(MoveType.After,
                    (i) => i.MatchLdcR4(6.2f)))
                {
                    c1.EmitDelegate<Func<float, float>>((orig) =>
                    {
                        return orig * 2f;
                    });
                }

                if (c1.TryGotoNext(MoveType.After, (i) => i.MatchNewobj<ShockWave>()))
                {
                    if (c1.TryGotoPrev(MoveType.After, (i) => i.MatchLdcI4(5)))
                    {
                        c1.EmitDelegate<Func<int, int>>((orig) =>
                        {
                            return 40;
                        });
                    }

                    if (c1.TryGotoPrev(MoveType.After, (i) => i.MatchLdcR4(330f)))
                    {
                        c1.EmitDelegate<Func<float, float>>((orig) =>
                        {
                            return orig * 4f;
                        });
                    }
                }

                if(c1.TryGotoNext(MoveType.After, (i) => i.MatchLdsfld<SoundID>("Bomb_Explode")))
                {
                    c1.EmitDelegate<Func<SoundID, SoundID>>((orig) =>
                    {
                        return nukeSound;
                    });
                }
            }
        }

        public class RadiationField : UpdatableAndDeletable
        {
            public float rad;
            int setLife;
            int life;
            float dmgPerFrame;

            public Vector2 pos;
            AbstractCreature killTag;

            LightSource lightSource;
            ParticleEmitter emitter;
            PhysicalObject owner;

            public RadiationField(PhysicalObject owner, Room room, Vector2 pos, Creature killTag, float rad, int life, float dmgPerSec)
            {
                this.rad = rad;
                this.owner = owner;
                this.setLife = this.life = life;
                dmgPerFrame = dmgPerSec / 40f;
                this.room = room;
                this.pos = pos;

                this.killTag = killTag?.abstractCreature;

                lightSource = new LightSource(pos, true, RadiatCol, this);
                room.AddObject(lightSource);
                CreateRadiatParticle();
            }

            public override void Update(bool eu)
            {
                base.Update(eu);

                if (life > 0)
                {
                    life--;
                    if (life == 0)
                        Destroy();
                }

                float lifeFac = 0f;
                float exLightFac = 1f;
                if (life > setLife - 10)
                {
                    lifeFac = 1f - (setLife - 10 - life) / 10f;
                }
                else if (life > 80)
                    lifeFac = 1f;
                else
                    lifeFac = life / 80f;

                if(life > setLife - 5)
                {
                    exLightFac = 1f - (setLife - life) / 5f + 1f;
                }

                lightSource.setAlpha = lifeFac + (UnityEngine.Random.value * 2f - 1f) * 0.1f * lifeFac;
                lightSource.setRad = rad * lifeFac * 2f * exLightFac;

                var lst = room.updateList.Where(u => u is Creature).Select(u => u as Creature).ToList();
                foreach (var creature in lst)
                {
                    if (creature is Player)
                        continue;

                    if(Vector2.Distance(creature.DangerPos, pos) < rad)
                    {
                        if(killTag != null)
                            creature.SetKillTag(killTag);
                        creature.Violence(owner.firstChunk, null, creature.mainBodyChunk, null, Creature.DamageType.Explosion, dmgPerFrame, dmgPerFrame);
                    }
                }

                if (Random.value < 0.3f * lifeFac)
                {
                    Vector2 randPos = Custom.RNV() * Random.value * rad + pos;
                    if (!room.GetTile(randPos).IsSolid())
                    {
                        room.AddObject(new ShockWave(randPos, 60f, 0.25f, 5));
                    }
                }
            }

            public override void Destroy()
            {
                base.Destroy();
                lightSource.Destroy();
                emitter.Die();
            }

            public void StackField()
            {
                rad += 45000f / rad;
                life = setLife;

                if(emitter != null)
                {
                    foreach(var module in emitter.EmitterModules)
                    {
                        if(module is SetEmitterLife setEmitterLife)
                        {
                            setEmitterLife.life = setEmitterLife.setLife;
                        }
                    }
                    emitter.SpawnModule.maxParitcleCount = Mathf.CeilToInt(rad * 260f / 300f);
                    (emitter.SpawnModule as BurstSpawnerModule).emitted = false;
                }
            }

            void CreateRadiatParticle()
            {
                emitter = new ParticleEmitter(room);

                emitter.pos = pos;
                emitter.ApplyEmitterModule(new SetEmitterLife(emitter, setLife - 20, false));

                emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, 260));

                emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight", alpha: 0.5f)));
                emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("pixel", "", constCol: Color.white)));
                emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Relative));
                emitter.ApplyParticleModule(new SetRandomLife(emitter, setLife - 20, setLife - 40));
                emitter.ApplyParticleModule(new SetConstColor(emitter, RadiatCol));
                emitter.ApplyParticleModule(new SetRandomScale(emitter, 2f, 2.5f));
                emitter.ApplyParticleModule(new PositionOverLife(emitter,
                    (p, l) =>
                    {
                        Vector2 dir = Custom.DegToVec(p.randomParam1 * 360f);
                        float radParam = (1f - Mathf.Pow(p.randomParam2, 4)) * 0.4f + 0.6f;

                        return dir * rad * radParam;
                    }));

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
                        return 1f;
                    }));
                emitter.ApplyParticleModule(new KillInTerrain(emitter));
                emitter.ApplyParticleModule(new StagnantForceFieldBlink(emitter));

                ParticleSystem.ApplyEmitterAndInit(emitter);
            }
        }

        public class KillInTerrain : EmitterModule, IParticleUpdateModule
        {
            public KillInTerrain(ParticleEmitter emitter) : base(emitter)
            {
            }

            public void ApplyUpdate(Particle particle)
            {
                if (emitter.room.GetTile(particle.pos).IsSolid())
                    particle.Die();
            }
        }
    }
}
