using Mono.Cecil;
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
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Duality
{
    internal class ClusterBombBuffEntry : IBuffEntry
    {
        public static BuffID clusterBombBuffID = new BuffID("ClusterBomb", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ClusterBombBuffEntry>(clusterBombBuffID);
        }

        public static void HookOn()
        {
            On.Explosion.ctor += Explosion_ctor;
        }

        private static void Explosion_ctor(On.Explosion.orig_ctor orig, Explosion self, Room room, PhysicalObject sourceObject, Vector2 pos, int lifeTime, float rad, float force, float damage, float stun, float deafen, Creature killTagHolder, float killTagHolderDmgFactor, float minStun, float backgroundNoise)
        {
            orig.Invoke(self, room, sourceObject, pos, lifeTime, rad, force, damage, stun, deafen, killTagHolder, killTagHolderDmgFactor, minStun, backgroundNoise);

            BuffUtils.Log("ClusterBomb", $"{damage}");
            if(damage > 1f)
            {
                int count = (int)Mathf.Clamp(damage * 2, 2, 6);
                
                CreateClusterBombs(room, count, sourceObject, room.MiddleOfTile(room.GetTilePosition(pos)), lifeTime, rad * 0.8f, force * 0.5f, damage * 0.1f, stun * 0.5f, deafen * 0.5f, killTagHolder, killTagHolderDmgFactor, minStun * 0.5f, backgroundNoise * 0.5f, Mathf.Lerp(1f, 2f, Mathf.InverseLerp(1f, 10f, damage)));
            }
        }

        public static void CreateClusterBombs(Room room, int count, PhysicalObject sourceObject, Vector2 pos, int lifeTime, float rad, float force, float damage, float stun, float deafen, Creature killTagHolder, float killTagHolderDmgFactor, float minStun, float backgroundNoise, float sizeFac)
        {
            var emitter = new ParticleEmitter(room);

            IntVector2 tilePos = room.GetTilePosition(pos);
            if(room.GetTile(pos).Solid)
            {
                for(int i = 0;i < 8; i++)
                {
                    if (!room.GetTile(tilePos + Custom.eightDirections[i]).Solid)
                    {
                        pos = room.MiddleOfTile(tilePos + Custom.eightDirections[i]);
                        break;
                    }
                }
            }
            emitter.lastPos = emitter.pos = pos;

            emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 120, false));
            emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, count));

            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Circle20", "", 5, scale: 0.05f)));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "", 8)));

            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetRandomPos(emitter, 0f));
            emitter.ApplyParticleModule(new SetRandomScale(emitter, 4f * sizeFac, 5f * sizeFac));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 20, 120));
            emitter.ApplyParticleModule(new SetSphericalVelocity(emitter, force * 3, force * 3 + 1f));
            emitter.ApplyParticleModule(new ConstantAcc(emitter, new Vector2(0f, -9f)));
            emitter.ApplyParticleModule(new SetConstColor(emitter, Color.black));

            emitter.ApplyParticleModule(new SimpleParticlePhysic(emitter, true, false, 0.5f));

            emitter.ApplyParticleModule(new DefaultDrawer(emitter, new int[1] { 0 }));
            emitter.ApplyParticleModule(new TrailDrawer(emitter, 1, 20)
            {
                alpha = (p, i, a) => 1f - i / (float)a,
                alphaModifyOverLife = (p, l) => 1f - l,
                gradient = (p, i, a) => Color.Lerp(Color.white, p.setColor, i / (float)a),
                width = (p, i, a) => (1f - i / (float)a) * p.setScaleXY.x * 0.1f
            });

            emitter.OnParticleDieEvent += (p) =>
            {
                room.AddObject(new SootMark(room, p.pos, 20f * damage, true));
                room.AddObject(new Explosion(room, sourceObject, p.pos, 7, rad, force, damage, stun, deafen, killTagHolder, killTagHolderDmgFactor, minStun, backgroundNoise));
                room.AddObject(new Explosion.ExplosionLight(p.pos, 280f * damage, 1f, 7, Color.black));
                room.AddObject(new Explosion.ExplosionLight(p.pos, 230f * damage, 1f, 3, new Color(1f, 1f, 1f)));
                //room.AddObject(new ExplosionSpikes(room, p.pos, 14, 30f * damage, 9f, 7f * damage, 170f * damage, Color.black));
                room.AddObject(new ShockWave(p.pos, 330f * damage, 0.0035f * damage, 5, false));

                //for (int i = 0; i < 25; i++)
                //{
                //    Vector2 a = Custom.RNV();
                //    if (room.GetTile(p.pos + a * 20f).Solid)
                //    {
                //        if (!room.GetTile(p.pos - a * 20f).Solid)
                //        {
                //            a *= -1f;
                //        }
                //        else
                //        {
                //            a = Custom.RNV();
                //        }
                //    }
                //    for (int j = 0; j < 3; j++)
                //    {
                //        room.AddObject(new Spark(p.pos + a * Mathf.Lerp(30f, 60f, Random.value), a * Mathf.Lerp(7f, 38f, Random.value) + Custom.RNV() * 20f * Random.value, Color.Lerp(Color.black, new Color(1f, 1f, 1f), Random.value), null, 11, 28));
                //    }
                //    //room.AddObject(new Explosion.FlashingSmoke(p.pos + a * 40f * Random.value, a * Mathf.Lerp(1f, 5f, Mathf.Pow(Random.value, 2f)), 1f + 0.05f * Random.value, new Color(1f, 1f, 1f), Color.black, Random.Range(3, 11)));
                //}
                room.ScreenMovement(new Vector2?(p.pos), default(Vector2), 1.3f * damage);
                room.PlaySound(SoundID.Bomb_Explode, p.pos, 0.5f + Random.value * 0.2f, 1f + Random.value * 0.2f);
            };

            ParticleSystem.ApplyEmitterAndInit(emitter);
        }
    }
}
