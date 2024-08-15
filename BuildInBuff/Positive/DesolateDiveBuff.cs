using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static BuiltinBuffs.Positive.DesolateDiveBuff;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive
{
    internal class DesolateDiveBuff : Buff<DesolateDiveBuff, DesolateDiveBuffData>, PlayerUtils.IOWnPlayerUtilsPart
    {
        public override BuffID ID => DesolateDiveBuffEntry.desolateDiveBuffID;

        public DesolateDiveBuff()
        {
            PlayerUtils.AddPart(this);
        }

        public override void Destroy()
        {
            base.Destroy();
            PlayerUtils.RemovePart(this);
        }


        public PlayerUtils.PlayerModuleGraphicPart InitGraphicPart(PlayerUtils.PlayerModule module)
        {
            return null;
        }

        public PlayerUtils.PlayerModulePart InitPart(PlayerUtils.PlayerModule module)
        {
            return new DesolatePlayerModule();
        }

        public class DesolatePlayerModule : PlayerUtils.PlayerModulePart
        {
            public int cd;
            public bool Allowed => cd == 0;

            public override void Update(Player player, bool eu)
            {
                base.Update(player, eu);
                if (cd > 0)
                    cd--;
            }

            public void Triggered()
            {
                cd = 5;
            }
        }
    }

    internal class DesolateDiveBuffData : BuffData
    {
        public override BuffID ID => DesolateDiveBuffEntry.desolateDiveBuffID;
    }

    internal class DesolateDiveBuffEntry : IBuffEntry
    {
        public static BuffID desolateDiveBuffID = new BuffID("DesolateDive", true);
        static float DiveVelThreshold = 15f;

        public static string desolateDiveSpike;
        public static string desolateDiveHalfCircle;
        public static SoundID desolateDive = new SoundID("DesolateDive", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<DesolateDiveBuff, DesolateDiveBuffData, DesolateDiveBuffEntry>(desolateDiveBuffID);
        }

        public static void LoadAssets()
        {
            desolateDiveSpike = Futile.atlasManager.LoadImage(desolateDiveBuffID.GetStaticData().AssetPath + Path.DirectorySeparatorChar + "DesolateDiveSpike").elements[0].name;
            desolateDiveHalfCircle = Futile.atlasManager.LoadImage(desolateDiveBuffID.GetStaticData().AssetPath + Path.DirectorySeparatorChar + "DesolateDiveHalfCircle").elements[0].name;
            BuffSounds.LoadSound(desolateDive, desolateDiveBuffID.GetStaticData().AssetPath, new BuffSoundGroupData(), new BuffSoundData("hero_quake_spell_impact"));
        }

        public static void HookOn()
        {
            On.Player.TerrainImpact += Player_TerrainImpact;
            On.Player.Collide += Player_Collide;
        }

        private static void Player_Collide(On.Player.orig_Collide orig, Player self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            if (self.bodyChunks[myChunk].vel.magnitude / 1.4f > DiveVelThreshold)
            {
                float speed = self.bodyChunks[myChunk].vel.magnitude / 1.4f;

                self.room.AddObject(new ShockWave(self.bodyChunks[myChunk].pos, speed * 4, speed / 96f, 4));

                self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Death, self.mainBodyChunk);
                foreach (var obj in self.room.updateList)
                {
                    if (obj is Creature creature && creature != self)
                    {
                        if ((creature.DangerPos - self.DangerPos).magnitude < speed * 4f)
                            creature.stun += Mathf.CeilToInt(speed * 2);
                    }
                }
            }
            orig.Invoke(self, otherObject, myChunk, otherChunk);
        }

        private static void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player self, int chunk, RWCustom.IntVector2 direction, float speed, bool firstContact)
        {
            if (firstContact && self.room != null && speed > DiveVelThreshold && direction.y < 0 && self.input[0].y < 0 &&  PlayerUtils.TryGetModulePart<DesolatePlayerModule>(self, DesolateDiveBuff.Instance, out var moudle) && moudle.Allowed)
            {
                Vector2 pos = Helper.GetContactPos(self.bodyChunks[chunk].pos, Vector2.down, self.room);
                if (self.room.BeingViewed)
                {
                    CreateDiveEffec(pos, self.room);
                    self.room.game.cameras[0].ScreenMovement(pos, Vector2.down * 25f, 0.1f);
                }

                self.room.AddObject(new ShockWave(self.bodyChunks[chunk].pos, 800f, 0.015f, 6));
               
                self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Death, self.mainBodyChunk);
                self.room.PlaySound(desolateDive,0f, 0.45f, 1f);
                for(int j = self.room.updateList.Count-1 ; j>=0;j--)
                {
                    var obj = self.room.updateList[j];
                    if(obj is PhysicalObject p)
                    {
                        Vector2 delta = Vector2.zero;
                        for(int i = 0;i < p.bodyChunks.Length; i++)
                        {
                            delta += (p.bodyChunks[i].pos - self.DangerPos);
                        }
                        delta /= p.bodyChunks.Length;
                        float dist = delta.magnitude;

                        if (dist < desolateRad * 2f)
                        {
                            if (p is Creature creature)
                            {
                                if (creature is Player)
                                    continue;
                                creature.stun += Mathf.CeilToInt(speed * 2);
                                creature.SetKillTag(self.abstractCreature);
                                creature.Violence(null, null, creature.mainBodyChunk, null, Creature.DamageType.Blunt, 1f, 0f);
                            }

                            Vector2 force = delta.normalized * Custom.LerpMap(dist, 0f, desolateRad, 20f, 0f) + Vector2.up * 8f;

                            foreach (var bodyChunk in p.bodyChunks)
                                bodyChunk.vel += force;
                        }
                    }
                    
                }
                moudle.Triggered();
            }
            orig.Invoke(self, chunk, direction, speed, firstContact);
        }

        static float desolateRad = 160f;
        static float ovalScale = 0.05f;
        public static void CreateDiveEffec(Vector2 pos, Room room)
        {
            var emitter1 = new ParticleEmitter(room);
            emitter1.lastPos = emitter1.pos = pos;
            //StormIsApproaching.AdditiveDefault
            emitter1.ApplyEmitterModule(new SetEmitterLife(emitter1, 40, false));
            emitter1.ApplyParticleSpawn(new BurstSpawnerModule(emitter1, Random.Range(20, 30)));

            emitter1.ApplyParticleModule(new AddElement(emitter1, new Particle.SpriteInitParam(desolateDiveSpike, "StormIsApproaching.AdditiveDefault")));
            emitter1.ApplyParticleModule(new AddElement(emitter1, new Particle.SpriteInitParam(FlameThrowerBuffEntry.flameVFX1, "StormIsApproaching.AdditiveDefault", alpha: 0.2f, scale: 4f)));
            emitter1.ApplyParticleModule(new SetMoveType(emitter1, Particle.MoveType.Global));

            emitter1.ApplyParticleModule(new SetConstColor(emitter1, Color.white));
            emitter1.ApplyParticleModule(new AlphaOverLife(emitter1, (p, l) =>
            {
                return Mathf.Sin(Mathf.Pow(l, 0.3f) * Mathf.PI) * 0.3f;
            }));
            emitter1.ApplyParticleModule(new SetRandomLife(emitter1, 10, 12));
            emitter1.ApplyParticleModule(new SetRandomPos(emitter1, 0f));
            emitter1.ApplyParticleModule(new SetCustomRotation(emitter1, (p) =>
            {
                var dir = Custom.DegToVec(Mathf.Lerp(-90f, 90f, p.randomParam1));
                dir.y *= ovalScale;
                return Custom.VecToDeg(dir);
            }));
            emitter1.ApplyParticleModule(new ScaleOverLife(emitter1, (p, l) =>
            {
                return new Vector2(0.6f, Mathf.Sin(Mathf.Pow(l, 0.3f) * Mathf.PI) * Mathf.Lerp(0f, 4f, Mathf.InverseLerp(0f, 90f, Mathf.Abs(p.setRotation))) * 0.5f);
            }));
            emitter1.ApplyParticleModule(new PositionOverLife(emitter1, (p, l) =>
            {
                Vector2 dir = Custom.DegToVec(p.setRotation) * (l * desolateRad * Mathf.Lerp(ovalScale, 1f, Mathf.InverseLerp(0f, 90f, Mathf.Abs(p.setRotation))) + p.scaleXY.x * 80f);
                return dir + p.emitter.pos;
            }));
            ParticleSystem.ApplyEmitterAndInit(emitter1);


            var emitter2 = new ParticleEmitter(room);
            emitter2.lastPos = emitter2.pos = pos;
            //StormIsApproaching.AdditiveDefault
            emitter2.ApplyEmitterModule(new SetEmitterLife(emitter2, 40, false));
            emitter2.ApplyParticleSpawn(new BurstSpawnerModule(emitter2, 1));

            emitter2.ApplyParticleModule(new AddElement(emitter2, new Particle.SpriteInitParam(desolateDiveHalfCircle, "StormIsApproaching.AdditiveDefault")));
            emitter2.ApplyParticleModule(new SetMoveType(emitter2, Particle.MoveType.Global));

            emitter2.ApplyParticleModule(new SetConstColor(emitter2, Color.white));
            emitter2.ApplyParticleModule(new SetCustomRotation(emitter2, (p) => 0f));
            emitter2.ApplyParticleModule(new AlphaOverLife(emitter2, (p, l) =>
            {
                return Mathf.Sin((1f - Mathf.Pow(1f - l, 2f)) * Mathf.PI);
            }));
            emitter2.ApplyParticleModule(new SetRandomLife(emitter2, 7, 7));
            emitter2.ApplyParticleModule(new SetRandomPos(emitter2, 0f));
            emitter2.ApplyParticleModule(new ScaleOverLife(emitter2, (p, l) =>
            {
                return 5f * (Mathf.Sin((1f - Mathf.Pow(1f - l, 2f)) * Mathf.PI));
            }));

            ParticleSystem.ApplyEmitterAndInit(emitter2);

            //向上波
            var emitter3 = new ParticleEmitter(room);
            emitter3.lastPos = emitter3.pos = pos;
            //StormIsApproaching.AdditiveDefault
            emitter3.ApplyEmitterModule(new SetEmitterLife(emitter3, 40, false));
            emitter3.ApplyParticleSpawn(new BurstSpawnerModule(emitter3, 20));

            emitter3.ApplyParticleModule(new AddElement(emitter3, new Particle.SpriteInitParam(FlameThrowerBuffEntry.flameVFX1, "StormIsApproaching.AdditiveDefault")));
            emitter3.ApplyParticleModule(new SetMoveType(emitter3, Particle.MoveType.Global));

            emitter3.ApplyParticleModule(new SetConstColor(emitter3, Color.white));
            emitter3.ApplyParticleModule(new SetCustomRotation(emitter3, (p) => 0f));  
            emitter3.ApplyParticleModule(new SetRandomLife(emitter3, 13, 15));
            emitter3.ApplyParticleModule(new SetCustomPos(emitter3, (p) =>
            {
                float l2 = 0f;
                float y = (l2) * desolateRad * 1.5f * Mathf.Lerp(0.95f, 1.05f, p.randomParam1);
                float x = Mathf.Sin(l2 * Mathf.PI * 8f) * desolateRad * 0.2f + Mathf.Lerp(-0.25f, 0.25f, p.randomParam2) * desolateRad;
                return new Vector2(x, y) + p.emitter.pos;
            }));
            emitter3.ApplyParticleModule(new AlphaOverLife(emitter3, (p, l) =>
            {
                return Mathf.Sin((1f - Mathf.Pow(1f - l, 2f)) * Mathf.PI);
            }));
            emitter3.ApplyParticleModule(new PositionOverLife(emitter3, (p, l) =>
            {
                float l2 = 1f - Mathf.Pow(1f - l, 3f);
                float y = (l2) * desolateRad * 3f * Mathf.Lerp(0.95f, 1.05f, p.randomParam1);
                float x = Mathf.Sin(l2 * Mathf.PI * 8f) * desolateRad * 0.2f + Mathf.Lerp(-0.25f, 0.25f, p.randomParam2) * desolateRad;
                return new Vector2(x, y) + p.emitter.pos;
            }));

            emitter3.ApplyParticleModule(new TrailDrawer(emitter3, 0, 20)
            {
                alpha = (p, l, a) => (l / (float)a) * p.alpha * 0.1f,
                gradient = (p, l, a) => Color.white * Mathf.Sin((l / (float)a) * Mathf.PI),
                width = (p, i, max) =>  20f
            });

            ParticleSystem.ApplyEmitterAndInit(emitter3);

            //石头
            var emitter4 = new ParticleEmitter(room);
            emitter4.pos = emitter4.lastPos = pos;
            emitter4.ApplyParticleSpawn(new BurstSpawnerModule(emitter4, Random.Range(5, 10)));
            emitter4.ApplyParticleModule(new SetMoveType(emitter4, Particle.MoveType.Global));
            emitter4.ApplyParticleModule(new AddElement(emitter4, new Particle.SpriteInitParam("Pebble1", string.Empty)));

            emitter4.ApplyParticleModule(new SetRandomPos(emitter4, 40));
            emitter4.ApplyParticleModule(new SetRandomVelocity(emitter4, Custom.DegToVec(-45 + 0f) * 20, Custom.DegToVec(45 + 0f) * 40));
            emitter4.ApplyParticleModule(new VelocityOverLife(emitter4, (particle, time)
                => particle.vel * 0.99f + Vector2.down * emitter4.room.gravity));
            emitter4.ApplyParticleModule(new RotationOverLife(emitter4, (particle, time) =>
                particle.rotation + Mathf.Sign(particle.vel.x) * Custom.LerpMap(particle.vel.magnitude, 5, 20, 10, 80) / 40f * (1 - time)));
            emitter4.ApplyParticleModule(new SetRandomScale(emitter4, 1f, 0.15f));
            emitter4.ApplyParticleModule(new SetRandomRotation(emitter4, 0, 360));
            emitter4.ApplyParticleModule(new SetConstColor(emitter4, new Color(0.01f, 0.01f, 0.01f)));
            emitter4.ApplyParticleModule(new SimpleParticlePhysic(emitter4, true, false));
            emitter4.ApplyParticleModule(new AlphaOverLife(emitter4, ((particle, time) =>
                particle.alpha = Mathf.Pow(1 - time, 0.5f))));
            emitter4.ApplyParticleModule(new SetRandomLife(emitter4, 80, 160));
            emitter4.ApplyParticleModule(new Gravity(emitter4, 0.9f));

            ParticleSystem.ApplyEmitterAndInit(emitter4);
        }
    }
}
