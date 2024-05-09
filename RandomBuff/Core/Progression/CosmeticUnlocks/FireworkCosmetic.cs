using MoreSlugcats;
using RandomBuffUtils;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RandomBuffUtils.ParticleSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace RandomBuff.Core.Progression.CosmeticUnlocks
{
    internal class FireworkCosmetic : CosmeticUnlock
    {
        public override CosmeticUnlockID UnlockID => CosmeticUnlockID.FireWork;

        public override string IconElement => "BuffCosmetic_Firework";

        public override SlugcatStats.Name BindCat => MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer;

        static FireworkCosmetic()
        {
            IL.Player.ClassMechanicsArtificer += Player_ClassMechanicsArtificer;
        }

        private static void Player_ClassMechanicsArtificer(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            BuffPlugin.Log("Player_ClassMechanicsArtificer");
            while(c.TryGotoNext(MoveType.After,
                (i) => i.MatchLdsfld<SoundID>("Fire_Spear_Explode")))
            {
                BuffPlugin.Log("Player_ClassMechanicsArtificer 1");
                c.GotoNext(MoveType.After, (i) => i.MatchCallvirt<Room>("PlaySound"));
                BuffPlugin.Log("Player_ClassMechanicsArtificer 2");
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<Player>>(PyroJumped);
                BuffPlugin.Log("Player_ClassMechanicsArtificer 3");
            }
        }

        public override void StartGame(RainWorldGame game)
        {
            base.StartGame(game);
            BuffPlugin.Log("FireworkCosmetic enabled");
        }

        public static void PyroJumped(Player player)
        {
            if (player.room == null)
                return;

            BuffPlugin.Log("Player PyroJumped");
            var game = player.room.game;
            var emitter = new ParticleEmitter(game.cameras[0].room);
            emitter.pos = game.Players[0].realizedCreature.DangerPos;

            emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 2, false));
            emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, 1));

            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight")));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "")));

            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetRandomPos(emitter, 0f));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 70, 80));
            emitter.ApplyParticleModule(new SetRandomColor(emitter, 0f, 0.4f, 1f, 0.5f));
            emitter.ApplyParticleModule(new SetRandomVelocity(emitter, new Vector2(-1f, 10f), new Vector2(1f, 10f)));
            emitter.ApplyParticleModule(new SetRandomScale(emitter, 1f, 1.5f));

            emitter.ApplyParticleModule(new ScaleOverLife(emitter, (particle, lifeParam) =>
            {
                return particle.setScaleXY * (Mathf.Sin(lifeParam * 10f) * 0.3f + 0.7f);
            }));
            emitter.ApplyParticleModule(new ColorOverLife(emitter, (particle, lifeParam) =>
            {
                return Color.Lerp(particle.setColor, Color.white, (Mathf.Sin(lifeParam * 10f) * 0.5f + 0.5f));
            }));
            emitter.ApplyParticleModule(new VelocityOverLife(emitter, (particle, lifeParam) =>
            {
                float sin = Mathf.Sin(lifeParam * 10f);
                return new Vector2(particle.setVel.x + sin * (1f - lifeParam), particle.setVel.y * (1f - lifeParam));
            }));

            emitter.ApplyParticleModule(new DefaultDrawer(emitter, new int[1] { 0 }));
            emitter.ApplyParticleModule(new TrailDrawer(emitter, 1, 20)
            {
                alpha = (p, i, a) => 1f - i / (float)a,
                gradient = (p, i, a) => Color.Lerp(Color.white, p.setColor, i / (float)a),
                width = (p, i, a) => (1f - i / (float)a) * p.setScaleXY.x
            });


            ParticleSystem.ApplyEmitterAndInit(emitter);

            int range = 60;
            for (int angle = 0; angle < 360; angle += range)
            {
                CreateSubParticle(angle, range, 3f, 0f, emitter);
            }
        }

        static void CreateSubParticle(float angle, float range, float vel, float hue, ParticleEmitter owner)
        {
            Vector2 velA = Custom.DegToVec(angle) * vel;
            Vector2 velB = Custom.DegToVec(range + angle) * vel;

            owner.OnParticleDieEvent += CreateEmitter;

            void CreateEmitter(Particle particle)
            {
                var emitter = new ParticleEmitter(particle.emitter.room);
                emitter.pos = particle.pos;

                emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 10, false));
                emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, 5));

                emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight")));
                emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "")));

                emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
                emitter.ApplyParticleModule(new SetRandomPos(emitter, 0f));
                emitter.ApplyParticleModule(new SetRandomLife(emitter, 200, 400));
                emitter.ApplyParticleModule(new SetRandomColor(emitter, hue, hue + 0.2f, 1f, 0.5f));
                emitter.ApplyParticleModule(new SetRandomVelocity(emitter, velA, velB));
                emitter.ApplyParticleModule(new SetRandomScale(emitter, 1f, 1.2f));

                emitter.ApplyParticleModule(new ConstantAcc(emitter, new Vector2(0f, -2f)));
                emitter.ApplyParticleModule(new SimpleParticlePhysic(emitter, true, false));
                emitter.ApplyParticleModule(new ColorOverLife(emitter, (p, lifeParam) =>
                {
                    Color result = Color.Lerp(p.setColor, Color.white, (Mathf.Sin(lifeParam * 3f) * 0.5f + 0.5f));
                    result.a = 1f - lifeParam;
                    return result;
                }));
                emitter.ApplyParticleModule(new ScaleOverLife(emitter, (p, lifeParam) =>
                {
                    return p.setScaleXY * (Mathf.Sin(lifeParam * 10f) * 0.3f + 0.7f) * (1f - lifeParam);
                }));


                emitter.ApplyParticleModule(new DefaultDrawer(emitter, new int[1] { 0 }));
                emitter.ApplyParticleModule(new TrailDrawer(emitter, 1, 20)
                {
                    alpha = (p, i, a) => 1f - i / (float)a,
                    alphaModifyOverLife = (p, l) => 1f - l,
                    gradient = (p, i, a) => Color.Lerp(Color.white, p.setColor, i / (float)a),
                    width = (p, i, a) => (1f - i / (float)a) * p.setScaleXY.x
                });


                ParticleSystem.ApplyEmitterAndInit(emitter);
            }
        }
    }
}
