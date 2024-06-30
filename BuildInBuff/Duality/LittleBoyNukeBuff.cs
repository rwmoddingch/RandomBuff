using BuiltinBuffs.Positive;
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
    internal class LittleBoyNukeBuffEntry : IBuffEntry
    {
        public static Color RadiatCol = Color.green * 0.6f + Color.blue * 0.4f;
        public static BuffID littelBoyNukeID = new BuffID("LittleBoyNuke", true);
        public static SoundID nukeSound = new SoundID("LittleBoyNuke", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<LittleBoyNukeBuffEntry>(littelBoyNukeID);
        }

        public static void LoadAssets()
        {
            BuffSounds.LoadSound(nukeSound, littelBoyNukeID.GetStaticData().AssetPath, new BuffSoundGroupData(), new BuffSoundData("littleboynuke1A", 0.4f));
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
            self.room.AddObject(new RadiationField(self.room, self.firstChunk.pos, self.thrownBy, 300f, 40 * 10, 3f));
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
            float rad;
            int setLife;
            int life;
            float dmgPerFrame;

            Vector2 pos;
            Creature killTag;

            LightSource lightSource;
            ParticleEmitter emitter;

            public RadiationField(Room room, Vector2 pos, Creature killTag, float rad, int life, float dmgPerSec)
            {
                this.rad = rad;
                this.setLife = this.life = life;
                dmgPerFrame = dmgPerSec / 40f;
                this.room = room;
                this.pos = pos;
                this.killTag = killTag;

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


                foreach (var creature in room.updateList.Where(u => u is Creature).Select(u => u as Creature))
                {
                    if(Vector2.Distance(creature.DangerPos, pos) < rad)
                    {
                        creature.Violence(null, null, creature.mainBodyChunk, null, Creature.DamageType.Explosion, dmgPerFrame, dmgPerFrame);
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
